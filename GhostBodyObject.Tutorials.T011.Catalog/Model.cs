using GhostBodyObject.Runtime.Logical.Model;

namespace Tutorials.T011.Catalog;

// An anchor assembly: it knows none of the modules that extend its entities.
[GhostRepository(Fragment = true)]
public interface MCatalogModule;

[Entity(1101, 1, 1), Root, Removable, Enumerable, WithIdentity]
public interface MProduct
{
    [Property(1)] string Name { get; set; }
}

[Entity(1102, 1, 1), Root, Removable, Enumerable, WithIdentity]
public interface MUserAccount
{
    [Property(1)] string Name { get; set; }
}

[HashIndex(nameof(MProduct), 110), HashIndexComponent(1, nameof(MProduct.Name))]
public interface MProductNames;

[Record(1103, 1, 1)]
public interface RAnnotation
{
    [Property(1)] string Text { get; set; }
}
