using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Catalog.Contracts;

namespace Tutorials.T011.Comments.Mutations;

[GhostLog(Fragment = true), IncludeModule(typeof(MCommerceContracts))]
public interface MCommentMutations;

[Record(1411, 1, 2), Mutation]
internal interface RCommentPublication
{
    [Property(1)] RPublishComment Command { get; set; }
    [Property(2), Private] string AcceptedText { get; set; }
}

internal partial class CommentPublication
{
    private partial void Decide(PublishComment command)
    {
        if (string.IsNullOrWhiteSpace(command.Text))
            throw new ArgumentException("A comment needs text.");
        AcceptedText = command.Text.Trim();
    }
}

internal readonly partial struct CommentPublicationView
{
    internal partial void Apply() => Discussion.Add(Command.Product.Resolve(), Command.Author.Resolve(), AcceptedText);
}
