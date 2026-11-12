using GhostBodyObject.Runtime.Logical.Model;

namespace Tutorials.T011.Catalog.Read;

// A different role has different types and a different ambient fragment slot.
[GhostRepository(Fragment = true)]
public interface MCatalogReadModule;

[Entity(1601, 1, 1), Root, Enumerable]
public interface MProductSummary
{
    [Property(1)] string Name { get; set; }
}
