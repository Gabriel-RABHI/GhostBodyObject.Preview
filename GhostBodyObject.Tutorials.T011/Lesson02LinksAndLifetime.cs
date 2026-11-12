using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using Tutorials.T011.Billing;
using Tutorials.T011.Catalog;
using Tutorials.T011.Comments;

namespace Tutorials.T011;

public sealed partial class Lesson01ComposeAssemblies
{
    [Fact]
    public void AttachTheSameComponentToAnotherHostTypeAndDeleteItsInternalCycle()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("component-hosts", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        var user = new UserAccount(UserAccountRef.New());
        var thread = user.CommentThread.New();
        thread.Comments.New(user).Text = "Own profile";
        Assert.Same(user, thread.UserAccount.Get());
        Assert.False(thread.Product.Any());
        Assert.Empty(DiscussedProductSystemCollection);
        user.Remove();
        Assert.Empty(UserAccountCollection);
        Assert.Empty(CommentThreadCollection);
        Assert.Empty(CommentCollection);
        Assert.Equal("Own profile", DeletionNoticeCollection.Single().Text);
        tx.Commit();
    }

    [Fact]
    public void CascadeComponentsPartsAndEdgesAndClearWeakReferences()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("cascade", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var product = new Product(ProductRef.New());
            var user = new UserAccount(UserAccountRef.New());
            Discussion.Add(product, user, "Review");
            var comment = product.CommentThread.Get().Comments.Single();
            comment.Mention = product;
            var invoice = new Invoice(user) { Favorite = product };
            invoice.Suggestions.Add(product);
            user.NewRating(product).Stars = 5;
            Assert.Equal(5, user.ToView().RatingEdges.Single().Stars);
            product.Remove();
            Assert.False(invoice.Favorite.Any());
            Assert.Empty(invoice.Suggestions);
            Assert.Empty(CommentCollection);
            Assert.Empty(CommentThreadCollection);
            Assert.Empty(RatingCollection);
            Assert.Equal("Review", DeletionNoticeCollection.Single().Text);
            Assert.Single(UserAccountCollection);
            tx.Commit();
        }
        using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            Assert.Empty(ProductCollection);
            Assert.False(InvoiceCollection.Single().Favorite.Any());
            Assert.Empty(InvoiceCollection.Single().Suggestions);
            Assert.Equal("Review", DeletionNoticeCollection.Single().Text);
        }
    }

    [Fact]
    public void RetainTheHostThroughItsComponentAndItsEdge()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("retention", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        var product = new Product(ProductRef.New());
        var user = new UserAccount(UserAccountRef.New());
        var thread = product.CommentThread.New();
        var rating = user.NewRating(product);
        var moderation = new Moderation { Thread = thread, Rating = rating };
        Assert.Throws<InvalidOperationException>(product.Remove);
        Assert.Throws<InvalidOperationException>(() => product.CommentThread.Remove());
        Assert.Throws<InvalidOperationException>(rating.Remove);
        moderation.Thread = default;
        Assert.Throws<InvalidOperationException>(product.Remove);
        moderation.Rating = default;
        product.Remove();
        Assert.Empty(RatingCollection);
        Assert.Empty(CommentThreadCollection);
        tx.Commit();
    }

    [Fact]
    public void MovePartsKeepOrphansAndReleaseSharedObjects()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("parts", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        var user = new UserAccount(UserAccountRef.New());
        var first = new Invoice(user);
        var second = new Invoice(user);
        Assert.Same(first, first.Heading.Invoice);
        first.Heading.Reference = "A";
        first.Footer.New().Text = "Terms";
        var line = first.Lines.New();
        line.Quantity = 2;
        line.MoveTo(second.Lines);
        Assert.Empty(first.Lines);
        Assert.Same(second, line.Invoice.Get());
        var archive = new Archive { Line = line };
        Assert.Same(archive, line.Archives.Single());
        var label = archive.Label.New();
        label.Text = "Keep";
        var another = new Archive();
        another.Label.Set(label);
        archive.Label.Clear();
        Assert.Single(LabelCollection);
        another.Label.Clear();
        Assert.Empty(LabelCollection);
        first.Remove();
        Assert.Empty(InvoiceFooterCollection);
        second.Remove();
        Assert.False(line.Invoice.Any());
        Assert.Single(InvoiceLineCollection);
        archive.Line = default;
        Assert.Empty(InvoiceLineCollection);
        tx.Commit();
    }

    [Fact]
    public void EnforceUniqueSymmetricAndMultipleEdges()
    {
        using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("edges", StoreAccessMode.ReadWrite);
        using var repository = new CommerceRepository(store);
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        var ada = new UserAccount(UserAccountRef.New());
        var bob = new UserAccount(UserAccountRef.New());
        var product = new Product(ProductRef.New());
        var other = new Product(ProductRef.New());
        var rating = ada.NewRating(product);
        Assert.Throws<InvalidOperationException>(() => ada.NewRating(product));
        Assert.Same(rating, ada.RatingEdges.GetOrNew(product));
        var friendship = ada.NewFriendship(bob);
        Assert.Same(friendship, bob.FriendshipEdges.Edge(ada).Get());
        Assert.Throws<InvalidOperationException>(() => bob.NewFriendship(ada));
        product.NewRecommendation(other).Reason = "First";
        product.NewRecommendation(other).Reason = "Second";
        Assert.Equal(2, RecommendationCollection.Count());
        Assert.True(ada.RatingEdges.Remove(product));
        other.Remove();
        Assert.Empty(RecommendationCollection);
        bob.Remove();
        Assert.Empty(FriendshipCollection);
        Assert.Single(UserAccountCollection);
        tx.Commit();
    }
}
