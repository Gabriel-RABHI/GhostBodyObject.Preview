using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Catalog;

namespace Tutorials.T011.Billing;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogModule))]
public interface MBillingModule;

[Entity(1301, 1, 7), Root, Removable, Enumerable]
internal interface MInvoice
{
    [Property(1), Strong] MUserAccount Customer { get; set; }
    [Property(2), Strong] IEntityCollection<MProduct> Products { get; }
    [Property(3), Weak] IEntityCollection<MProduct> Suggestions { get; }
    [Property(4), Weak] Option<MProduct> Favorite { get; set; }
    [Property(5)] IEntityCollection<MInvoiceLine> Lines { get; }
    [Property(6)] MInvoiceHeader Heading { get; }
    [Property(7)] Option<MInvoiceFooter> Footer { get; }
}

[Entity(1302, 1, 1), Enumerable, Orphanable]
internal interface MInvoiceLine
{
    [Property(1)] int Quantity { get; set; }
    [OwnedFrom] Option<MInvoice> Invoice { get; }
    [ReverseStrongReference(nameof(MArchive.Line))] IEntityCollection<MArchive> Archives { get; }
}

[Entity(1303, 1, 2), Root, Removable, Enumerable]
internal interface MArchive
{
    [Property(1), Strong] Option<MInvoiceLine> Line { get; set; }
    [Property(2), Strong] Option<MLabel> Label { get; set; }
}

[Entity(1304, 1, 1), Enumerable]
internal interface MLabel
{
    [Property(1)] string Text { get; set; }
}

[Entity(1305, 1, 1)]
internal interface MInvoiceHeader
{
    [Property(1)] string Reference { get; set; }
    [OwnedFrom] MInvoice Invoice { get; }
}

[Entity(1306, 1, 1), Enumerable]
internal interface MInvoiceFooter
{
    [Property(1)] string Text { get; set; }
}
