using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Catalog;
using Tutorials.T011.Catalog.Contracts;
using Tutorials.T011.Comments;
using Tutorials.T011.Comments.Mutations;
using Tutorials.T011.Billing.Mutations;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public void RefuseForeignRecordsAndInvalidSubmissionContexts()
    {
        using var store = TransactionalMemoryStore.NewInMemoryLogStore("standalone-composed-log", StoreAccessMode.ReadWrite);
        using var log = new CommerceLog(store);
        Assert.Throws<InvalidOperationException>(() => new PublishComment().Submit());
        Assert.Throws<ArgumentException>(() => log.SubscribeFrom(0).Handle<AnnotationView>(static (_, _) => LogStoreSubscription.Continue));
        using (var tx = log.OpenTransaction())
        {
            Assert.Same(tx, CommerceContractsTransaction.Current);
            Assert.Throws<InvalidOperationException>(() => log.OpenTransaction());
            Assert.Throws<InvalidOperationException>(() => tx.Ordinal(64999));
            Assert.Throws<ArgumentNullException>(() => tx.Submit(null!));
            using (new ReplayContext())
                Assert.Throws<InvalidOperationException>(() => new PublishComment().Submit());
        }
        Assert.Null(CommerceContractsTransaction.Current);
        Assert.Null(CommentMutationsTransaction.Current);
    }

    [Fact]
    public void DispatchPublicCommandsToPrivateHandlersAndReplayChildrenBeforeParents()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("mutation-state", StoreAccessMode.ReadWrite);
        using var journal = store.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("mutation-log", StoreAccessMode.ReadWrite));
        using var repository = new CommerceRepository(store);
        using var log = new CommerceLog(journal);
        repository.Chain(log);
        var product = ProductRef.New();
        var customer = UserAccountRef.New();
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            new Product(product);
            new UserAccount(customer);
            tx.Commit();
        }
        using (var pump = new GhostMutationPump(repository))
        {
            var command = new Checkout { Product = product, Customer = customer };
            var mutation = pump.Post(command);
            Assert.Equal(MutationStatus.Accepted, pump.Wait(mutation));
            Assert.IsType<Purchase>(mutation);
            Assert.Throws<InvalidOperationException>(() => command.FailAfterComment = true);
            var rejected = pump.Post(new Checkout { Product = product, Customer = customer, FailAfterComment = true });
            Assert.Equal(MutationStatus.Failed, pump.Wait(rejected));
            var empty = pump.Post(new PublishComment { Product = product, Author = customer, Text = " " });
            Assert.Equal(MutationStatus.Failed, pump.Wait(empty));
        }
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.Single(InvoiceCollection);
            Assert.Single(CommentCollection);
        }
        var order = new List<string>();
        log.SubscribeFrom(0)
            .Handle<CommentPublicationView, List<string>>(order, static (_, _, seen) => { seen.Add("comment"); return LogStoreSubscription.Continue; })
            .Handle<PurchaseView, List<string>>(order, static (_, _, seen) => { seen.Add("purchase"); return LogStoreSubscription.Continue; })
            .OnReplayEnd(static _ => LogStoreSubscription.Cancel).Run();
        Assert.Equal(["comment", "purchase"], order);

        // A fresh Store with the same anchors. Only Apply runs: Decide would submit the child twice.
        using var rebuilt = TransactionalMemoryStore.NewInMemoryRepositoryStore("mutation-rebuilt", StoreAccessMode.ReadWrite);
        using var target = new CommerceRepository(rebuilt);
        using (var tx = target.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            new Product(product);
            new UserAccount(customer);
            tx.Commit();
        }
        using (var tx = target.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
        {
            log.SubscribeFrom(0).HandleMutations().OnReplayEnd(static _ => LogStoreSubscription.Cancel).Run();
            tx.Commit();
        }
        using (target.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.Single(InvoiceCollection);
            Assert.Equal("Purchased", CommentCollection.Single().Text);
        }
        Assert.Null(CommerceContractsTransaction.Current);
        Assert.Null(CommentMutationsTransaction.Current);
        Assert.Null(BillingMutationsTransaction.Current);
    }
}
