using GhostBodyObject.Runtime.Logical.Model;

namespace Tutorials.T011.Catalog.Contracts;

[GhostLog(Fragment = true)]
public interface MCommerceContracts;

[Record(1401, 1, 3), Command]
public interface RPublishComment
{
    [Property(1)] ProductRef Product { get; set; }
    [Property(2)] UserAccountRef Author { get; set; }
    [Property(3)] string Text { get; set; }
}

[Record(1402, 1, 3), Command]
public interface RCheckout
{
    [Property(1)] ProductRef Product { get; set; }
    [Property(2)] UserAccountRef Customer { get; set; }
    [Property(3)] bool FailAfterComment { get; set; }
}
