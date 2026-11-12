using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Catalog;

namespace Tutorials.T011.Comments;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogModule))]
public interface MCommentModule;

// Adding a private type here changes the generated manifest, not the application declaration.
[Entity(1201, 1, 2), ComponentFor(nameof(MProduct)), ComponentFor(nameof(MUserAccount)), Enumerable]
internal interface CCommentThread
{
    [Property(1)] IEntityCollection<MComment> Comments { get; }
    [Property(2)] RecordBag<RAnnotation> Notes { get; set; }
}

[Entity(1202, 1, 3), Enumerable]
internal interface MComment
{
    [Property(1), TextIndexed(nameof(MCommentWords))] string Text { get; set; }
    [Property(2), Strong] MUserAccount Author { get; set; }
    [Property(3), Weak] Option<MProduct> Mention { get; set; }
    [OwnedFrom] CCommentThread Thread { get; }
}

[Entity(1203, 1, 1), Edge(nameof(MUserAccount), nameof(MProduct)), Removable, Enumerable]
internal interface ERating
{
    [Property(1)] int Stars { get; set; }
}

[Entity(1204, 1, 0), Edge(nameof(MUserAccount), nameof(MUserAccount), Symmetric = true), Removable, Enumerable]
internal interface EFriendship;

[Entity(1205, 1, 1), Edge(nameof(MProduct), nameof(MProduct), Multiple = true), Removable, Enumerable]
internal interface ERecommendation
{
    [Property(1)] string Reason { get; set; }
}

// Public workflow, private component and parts. No reference to the composition assembly.
public static class Discussion
{
    public static void Add(Product product, UserAccount author, string text)
        => product.CommentThread.GetOrNew().Comments.New(author).Text = text;

    public static int Count(Product product) => product.CommentThread.TryGet(out var thread) ? thread.Comments.Count() : 0;
}

[Entity(1206, 1, 2), Root, Removable, Enumerable]
internal interface MModeration
{
    [Property(1), Strong] Option<CCommentThread> Thread { get; set; }
    [Property(2), Strong] Option<ERating> Rating { get; set; }
}

[Entity(1207, 1, 1), Root, Enumerable]
internal interface MDeletionNotice
{
    [Property(1)] string Text { get; set; }
}

[TextIndex(120)]
internal interface MCommentWords;

[ValueIndex(nameof(ERating), 121), ValueIndexComponent(1, nameof(ERating.Stars))]
internal interface MRatingStars;

[SystemFor(nameof(MProduct)), Enumerable]
internal interface SDiscussedProduct
{
    MProduct Product { get; }
    CCommentThread Thread { get; }
}

internal partial class Comment
{
    partial void OnDeleting()
    {
        if (Text == "protected")
            throw new InvalidOperationException("Moderation refused deletion.");
        new DeletionNotice { Text = Text };
    }
}
