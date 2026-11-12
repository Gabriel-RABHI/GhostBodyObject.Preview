using GhostBodyObject.Runtime.Logical.Repository;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Catalog;
using Tutorials.T011.Comments;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public async Task BindEachExecutionContextToItsOwnStoreAcrossAwait()
    {
        using var first = TransactionalMemoryStore.NewInMemoryRepositoryStore("first-context", StoreAccessMode.ReadWrite);
        using var second = TransactionalMemoryStore.NewInMemoryRepositoryStore("second-context", StoreAccessMode.ReadWrite);
        using var a = new CommerceRepository(first);
        using var b = new CommerceRepository(second);
        await Task.WhenAll(Write(a, "First"), Write(b, "Second"));
        using (a.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.Equal("First", ProductCollection.Single().Name);
            Assert.Throws<InvalidOperationException>(() => b.OpenTransaction(StoreAccessMode.ReadOnly));
        }
        using (b.OpenTransaction(StoreAccessMode.ReadOnly))
            Assert.Equal("Second", ProductCollection.Single().Name);
        Assert.Throws<InvalidOperationException>(() => new Product(ProductRef.New()));

        static async Task Write(CommerceRepository repository, string name)
        {
            using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
            await Task.Yield();
            Assert.Same(tx, CatalogModuleTransaction.Current);
            new Product(ProductRef.New()) { Name = name };
            tx.Commit();
        }
    }

    [Fact]
    public void RollBackEveryModuleWhenADeletionHookFails()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("failed-hook", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var product = new Product(ProductRef.New());
            var user = new UserAccount(UserAccountRef.New());
            Discussion.Add(product, user, "protected");
            tx.Commit();
        }
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            ProductCollection.Single().Name = "Uncommitted";
            Assert.Throws<InvalidOperationException>(ProductCollection.Single().Remove);
            Assert.Throws<InvalidOperationException>(tx.Commit);
        }
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.NotEqual("Uncommitted", ProductCollection.Single().Name);
            Assert.Equal("protected", CommentCollection.Single().Text);
        }
    }

    [Fact]
    public void RecoverCommittedLinksAndDiscardAnInterruptedTransaction()
    {
        string directory = Path.Combine(Path.GetTempPath(), "gbo-composition-" + Guid.NewGuid().ToString("N"));
        try
        {
            Recover(directory);
        }
        finally
        {
            // Segment mappings are released by their memory owners' finalizers after the Store closes.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Directory.Delete(directory, recursive: true);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void Recover(string directory)
        {
            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "commerce", StoreAccessMode.ReadWrite))
            using (var repository = new CommerceRepository(store))
            {
                using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    var product = new Product(ProductRef.New()) { Name = "Saved" };
                    var user = new UserAccount(UserAccountRef.New());
                    Discussion.Add(product, user, "Persisted");
                    user.NewRating(product).Stars = 4;
                    tx.Commit();
                }
                using (repository.OpenTransaction(StoreAccessMode.ReadWrite))
                    ProductCollection.Single().Remove(); // Interrupted before commit.
            }
            using var recovered = TransactionalMemoryStore.NewFileRepositoryStore(directory, "commerce", StoreAccessMode.ReadWrite);
            using var rebound = new CommerceRepository(recovered);
            using var read = rebound.OpenTransaction(StoreAccessMode.ReadOnly);
            Assert.Equal("Saved", ProductCollection.Single().Name);
            Assert.Equal("Persisted", CommentCollection.Single().Text);
            Assert.Equal(4, RatingCollection.Single().Stars);
            Assert.Empty(DeletionNoticeCollection);
        }
    }
}
