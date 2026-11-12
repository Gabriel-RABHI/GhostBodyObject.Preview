using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Catalog;
using Tutorials.T011.Catalog.Read;
using Tutorials.T011.Catalog.Contracts;
using Tutorials.T011.Comments;
using Tutorials.T011.Comments.Read;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public void BindDistinctWriteReadAndLogFragmentsInOneChain()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("chained-write", StoreAccessMode.ReadWrite);
        using var readStore = store.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("chained-read", StoreAccessMode.ReadWrite));
        using var logStore = readStore.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("chained-log", StoreAccessMode.ReadWrite));
        using var write = new CommerceRepository(store);
        using var read = new CommerceReadRepository(readStore);
        using var log = new CommerceLog(logStore);
        write.Chain(read);
        read.Chain(log);
        using (var tx = write.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            Assert.Same(tx, CatalogModuleTransaction.Current);
            Assert.Same(CommerceReadRepositoryTransaction.Current, CatalogReadModuleTransaction.Current);
            Assert.Same(CatalogReadModuleTransaction.Current, CommentReadModuleTransaction.Current);
            Assert.Same(CommerceLogTransaction.Current, CommerceContractsTransaction.Current);
            var product = new Product(ProductRef.New()) { Name = "Book" };
            var author = new UserAccount(UserAccountRef.New());
            new PublishComment { Product = product.Ref, Author = author.Ref, Text = "Useful" }.Submit();
            new ProductSummary { Name = product.Name }.DiscussionSummary.GetOrNew().Count = Discussion.Count(product);
            tx.Commit();
        }
        using (write.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            ProductCollection.Single().Name = "Abandoned";
            ProductSummaryCollection.Single().DiscussionSummary.Get().Count = 99;
        }
        using (write.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.Equal("Book", ProductCollection.Single().Name);
            Assert.Equal("Book", ProductSummaryCollection.Single().Name);
            Assert.Equal(1, ProductSummaryCollection.Single().DiscussionSummary.Get().Count);
        }
        Assert.Null(CatalogModuleTransaction.Current);
        Assert.Null(CommentReadModuleTransaction.Current);
        Assert.Null(CommerceContractsTransaction.Current);
    }
}
