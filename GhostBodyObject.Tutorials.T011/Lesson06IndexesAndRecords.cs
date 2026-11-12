using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Catalog;
using Tutorials.T011.Comments;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public void ComposeIndexesSystemsAndRecordsWithoutChangingTheirLocalApis()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed-indexes", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var product = new Product(ProductRef.New()) { Name = "Book" };
            var user = new UserAccount(UserAccountRef.New());
            Discussion.Add(product, user, "Useful reference");
            product.CommentThread.Get().Notes.Add(new Annotation { Text = "Imported Record" });
            user.NewRating(product).Stars = 5;
            Assert.Equal(product.Ref, ProductNames.Is("Book").Single().Ref);
            Assert.Single(RatingStars.Is(5));
            Assert.Equal("Useful reference", CommentWords.Search<CommentView>("useful").Single().Text);
            Assert.Single(DiscussedProductSystemCollection);
            tx.Commit();
        }
        // Feed handlers are registered without an ambient transaction, using the composed model's ranks.
        var seen = new List<string>();
        using var feed = repository.Feed();
        feed.Handle<ProductView, List<string>>(seen, static (_, view, values) => values.Add(view.Name.ToString()));
        feed.Handle<CommentView, List<string>>(seen, static (_, view, values) => values.Add(view.Text.ToString()));
        feed.Start();
        Assert.Contains("Book", seen);
        Assert.Contains("Useful reference", seen);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var product = ProductNames.Is("Book").Single().ToBody();
            Assert.Equal("Imported Record", product.CommentThread.Get().Notes.Enumerate<AnnotationView>().Single().Text);
            product.Name = "Reference";
            Assert.Empty(ProductNames.Is("Book"));
            Assert.Single(ProductNames.Is("Reference"));
            product.Remove();
            Assert.Empty(RatingStars.Is(5));
            Assert.Empty(CommentWords.Search<CommentView>("useful"));
            Assert.Empty(DiscussedProductSystemCollection);
            tx.Commit();
        }
    }
}
