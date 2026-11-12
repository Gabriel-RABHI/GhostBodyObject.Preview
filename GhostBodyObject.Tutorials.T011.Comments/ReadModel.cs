using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Catalog.Read;

namespace Tutorials.T011.Comments.Read;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogReadModule))]
public interface MCommentReadModule;

[Entity(1602, 1, 1), ComponentFor(nameof(MProductSummary))]
internal interface CDiscussionSummary
{
    [Property(1)] int Count { get; set; }
}
