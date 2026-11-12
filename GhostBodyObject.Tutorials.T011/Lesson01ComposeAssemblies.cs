using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Billing;
using Tutorials.T011.Catalog;
using Tutorials.T011.Comments;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public void CompileModulesSeparatelyAndShareOneTransaction()
    {
        Assert.NotEqual(typeof(Product).Assembly, typeof(Discussion).Assembly);
        Assert.NotEqual(typeof(Product).Assembly, typeof(Invoice).Assembly);
        Assert.NotEqual(typeof(Invoice).Assembly, typeof(CommerceRepository).Assembly);
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var product = new Product(ProductRef.New()) { Name = "Book" };
            var user = new UserAccount(UserAccountRef.New()) { Name = "Ada" };
            Discussion.Add(product, user, "Useful");
            var invoice = new Invoice(user);
            invoice.Products.Add(product);
            Assert.Same(tx, CatalogModuleTransaction.Current);
            Assert.Same(tx, CommentModuleTransaction.Current);
            Assert.Same(tx, BillingModuleTransaction.Current);
            Assert.Throws<InvalidOperationException>(product.Remove);
            Assert.Throws<InvalidOperationException>(user.Remove);
            tx.Commit();
        }
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            var product = ProductCollection.Single();
            Assert.Equal(1, Discussion.Count(product));
            Assert.Equal("Useful", product.ToView().CommentThread.Get().Comments.Single().Text);
            Assert.Equal("Ada", InvoiceCollection.Single().Customer.Name);
        }
        Assert.Null(CatalogModuleTransaction.Current);
    }
}
