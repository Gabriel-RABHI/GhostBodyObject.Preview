using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Catalog.Contracts;

namespace Tutorials.T011.Billing.Mutations;

[GhostLog(Fragment = true), IncludeModule(typeof(MCommerceContracts))]
public interface MBillingMutations;

[Record(1421, 1, 1), Mutation]
internal interface RPurchase
{
    [Property(1)] RCheckout Command { get; set; }
}

internal partial class Purchase
{
    private partial void Decide(Checkout command)
    {
        // Billing sees the public intention only. Its handler is private to Comments.
        new PublishComment { Product = command.Product, Author = command.Customer, Text = "Purchased" }.Submit();
        if (command.FailAfterComment)
            throw new InvalidOperationException("Purchase refused after the sub-Mutation.");
    }
}

internal readonly partial struct PurchaseView
{
    internal partial void Apply()
    {
        var invoice = new Invoice(Command.Customer.Resolve());
        invoice.Products.Add(Command.Product.Resolve());
    }
}
