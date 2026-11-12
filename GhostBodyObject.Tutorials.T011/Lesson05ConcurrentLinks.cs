using GhostBodyObject.Runtime.Logical.Repository;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;
using Tutorials.T011.Billing;
using Tutorials.T011.Catalog;
using Tutorials.T011.Comments;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public async Task RefuseADeletionWhenAnotherModuleRetainsItsTargetAfterTheSnapshot()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("concurrent-retention", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            new Product(ProductRef.New());
            new UserAccount(UserAccountRef.New());
            tx.Commit();
        }
        var removed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deletion = Task.Run(async () =>
        {
            using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
            ProductCollection.Single().Remove();
            removed.SetResult();
            await held.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Throws<TransactionConflictException>(tx.Commit);
        });
        await removed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
            new Invoice(UserAccountCollection.Single()).Products.Add(ProductCollection.Single());
            tx.Commit();
        }
        finally { held.SetResult(); }
        await deletion;
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
            Assert.Same(ProductCollection.Single(), InvoiceCollection.Single().Products.Single());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeepOneComponentOrEdgeWhenBothTransactionsCreateIt(bool edge)
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("concurrent-slot", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            new Product(ProductRef.New());
            new UserAccount(UserAccountRef.New());
            tx.Commit();
        }
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var committed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loser = Task.Run(async () =>
        {
            using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
            if (edge) UserAccountCollection.Single().NewRating(ProductCollection.Single());
            else ProductCollection.Single().CommentThread.New();
            ready.SetResult();
            await committed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Throws<TransactionConflictException>(tx.Commit);
        });
        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
            if (edge) UserAccountCollection.Single().NewRating(ProductCollection.Single());
            else ProductCollection.Single().CommentThread.New();
            tx.Commit();
        }
        finally { committed.SetResult(); }
        await loser;
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
            Assert.Equal(1, edge ? RatingCollection.Count() : CommentThreadCollection.Count());
    }

    [Fact]
    public void BindFragmentsForALeaseTransaction()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed-lease", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        GhostLease lease;
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            new Product(ProductRef.New());
            tx.Commit();
        }
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            Assert.True(ProductViewCollection.Single().TryLock(TimeSpan.FromMinutes(1), out lease!));
            tx.Commit();
        }
        using (var tx = repository.OpenTransaction(lease))
        {
            Assert.Same(tx, CatalogModuleTransaction.Current);
            ProductCollection.Single().Name = "Leased";
            lease.Release();
            tx.Commit();
        }
        Assert.Null(CatalogModuleTransaction.Current);
    }
}
