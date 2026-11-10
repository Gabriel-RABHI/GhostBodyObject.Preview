# Patterns: complete models for common domains

Part of [the modeling knowledge](README.md). Each pattern is a model that compiles as written (Fixed schema; the Repository interface and its `[IncludeEntityModel]` lines are left out after the first one). Copy the shape, not the names.

The shop (pattern 1) types its values as value objects, as every business model should ([values.md](values.md#value-objects-the-general-rule)). The other patterns keep .NET types to show their structure in fewer lines: in a real model, a price is a `Money`, a quantity a `Quantity`, a level a `Level`, a flag that says a state an enum.

## 1. A shop: Roots that designate each other, an aggregate, reference data, value objects

```csharp
internal enum OrderStatus { Pending, Paid, Shipped, Delivered }

// The value objects of the shop: structs of explicit layout, stored as their bytes, with their operators; no converter.
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct Money(decimal amount) : IAdditionOperators<Money, Money, Money>, IAdditiveIdentity<Money, Money>, IComparable<Money>
{
    [FieldOffset(0)] public readonly decimal Amount = amount;
    public static Money Zero => default;
    public static Money AdditiveIdentity => default;               // with IAdditionOperators: Sum adds amounts
    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static Money operator *(Money price, Quantity quantity) => new(price.Amount * quantity.Units);
    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct Quantity(int units)
{
    [FieldOffset(0)] public readonly int Units = units;
}

// A code, a value object over a text: stored by its converter, found by the generator, the only one of Sku.
public readonly record struct Sku(string Value);

public readonly struct SkuConverter : IGhostConverter<Sku, string>
{
    public static string ToStored(in Sku value) => value.Value ?? "";
    public static Sku FromStored(in string stored) => new(stored);
    public static bool PreservesOrder => false;
}

[Entity(1, 1, 1)] [Root] [Enumerable]                       // reference data: never removed, references to it cost nothing
internal interface MCountry { [Property(1)] GhostString Code { get; set; } }

[Entity(2, 1, 4), Root, Removable, WithIdentity, Enumerable]
internal interface MCustomer
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2), Strong] MCountry Country { get; set; }
    [Property(3), Weak] IEntityCollection<MProduct> WishList { get; }      // a product removed leaves every wish list
    [Property(4)] MAddress Billing { get; }                                  // a required part: a value object with texts

    [ReverseStrongReference(nameof(MOrder.Customer))]
    IEntityCollection<MOrder> Orders { get; }                                // read by the code: declared
}

[Entity(3, 1, 3)]
internal interface MAddress
{
    [Property(1)] GhostString Street { get; set; }
    [Property(2)] GhostString City { get; set; }
    [Property(3)] GhostString PostalCode { get; set; }
    [OwnedFrom] MCustomer Customer { get; }
}

[Entity(4, 1, 3), Root, Removable, WithIdentity, Enumerable]
internal interface MProduct
{
    [Property(1)] Sku Sku { get; set; }
    [Property(2)] VMoney Price { get; set; }
    [Property(3)] bool Discontinued { get; set; }                            // no longer sold: never removed while lines designate it
}

[Entity(5, 1, 4), Root, Removable, WithIdentity, Enumerable]
internal interface MOrder                                                    // an aggregate of its own, not a part of the customer
{
    [Property(1), Strong] MCustomer Customer { get; set; }
    [Property(2)] OrderStatus Status { get; set; }
    [Property(3)] DateTime Placed { get; set; }
    [Property(4)] IEntityCollection<MOrderLine> Lines { get; }
}

[Entity(6, 1, 3)]
internal interface MOrderLine                                                // the link with values, owned by the order
{
    [Property(1), Strong] MProduct Product { get; set; }
    [Property(2)] Quantity Quantity { get; set; }
    [Property(3)] VMoney UnitPrice { get; set; }                              // the price at the time of the order, copied
    [OwnedFrom] MOrder Order { get; }
}

internal partial class Order
{
    // Computed, never stored: Sum adds the amounts by the operators of Money.
    public Money Total => Lines.Sum(static line => line.UnitPrice * line.Quantity);
}

[GhostRepository(9), IncludeEntityModel(nameof(MCountry)), IncludeEntityModel(nameof(MCustomer)), IncludeEntityModel(nameof(MAddress)), IncludeEntityModel(nameof(MProduct)), IncludeEntityModel(nameof(MOrder)), IncludeEntityModel(nameof(MOrderLine))]
internal interface MShopRepository { }
```

Births: `var ada = new Customer(CustomerRef.New(), france) { Name = "Ada" };` (the identity first, then the required links); the required address is born with its customer, its fields set next: `ada.Billing.City = "Lyon";`. An order: `var order = new Order(OrderRef.New(), ada) { Placed = now }; order.Lines.New(product).Quantity = new Quantity(3);`. The customer cannot be removed while an order designates it; a product while a line sells it.

## 2. A document store: a tree, shared tags, versions

```csharp
[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MWorkspace
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] IEntityCollection<MFolder> Folders { get; }
}

[Entity(2, 1, 3), Enumerable]
internal interface MFolder                                                   // a part of two Owner types: a tree
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] IEntityCollection<MFolder> Folders { get; }
    [Property(3)] IEntityCollection<MDocument> Documents { get; }
    [OwnedFrom] Option<MWorkspace> Workspace { get; }
    [OwnedFrom] Option<MFolder> Parent { get; }
}

[Entity(3, 1, 4), WithIdentity, Enumerable]
internal interface MDocument
{
    [Property(1)] GhostString Title { get; set; }
    [Property(2)] GhostString Body { get; set; }
    [Property(3), Strong] IEntityCollection<MTag> Tags { get; }            // shared objects: a tag lives while a document carries it
    [Property(4)] IEntityCollection<MRevision> Revisions { get; }
    [OwnedFrom] MFolder Folder { get; }
}

[Entity(4, 1, 1), Enumerable]
internal interface MTag { [Property(1)] GhostString Label { get; set; } }   // neither Root nor composed: a shared object

[Entity(5, 1, 3)]
internal interface MRevision
{
    [Property(1)] int Number { get; set; }                                   // the order of the revisions: a property
    [Property(2)] DateTime At { get; set; }
    [Property(3)] GhostString Body { get; set; }
    [OwnedFrom] MDocument Document { get; }
}
```

`folder.MoveTo(other.Folders)` refuses a cycle; removing a folder removes its subtree; `doc.Tags.New().Label = "draft"` creates a tag, `other.Tags.Add(tag)` shares it, and the last `Remove` deletes it. A search by the words of the documents is a text index on `Title` and `Body` ([../indexes.md](../indexes.md)).

## 3. Mail: an attachment shared by many messages

```csharp
[Entity(1, 1, 3), Root, Removable, Enumerable]
internal interface MMessage
{
    [Property(1)] GhostString Subject { get; set; }
    [Property(2), Strong] IEntityCollection<MAttachment> Attachments { get; }
    [Property(3), Weak] Option<MMessage> InReplyTo { get; set; }            // a history: forgotten when the original goes
}

[Entity(2, 1, 2)]
internal interface MAttachment
{
    [Property(1)] GhostString FileName { get; set; }
    [Property(2)] GhostSpan<byte> Content { get; set; }
}
```

A forward adds the attachment to the new message **before** the draft is deleted: take the new retention first.

## 4. A school: a link with values owned by one end

```csharp
[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MStudent
{
    [Property(1)] GhostString Name { get; set; }
    [ReverseStrongReference(nameof(MEnrollment.Student))]
    IEntityCollection<MEnrollment> Enrollments { get; }
    [Property(2)] Option<int> Card { get; set; }                             // absent until given
}

[Entity(2, 1, 2), Root, Removable, Enumerable]
internal interface MCourse
{
    [Property(1)] GhostString Title { get; set; }
    [Property(2)] IEntityCollection<MEnrollment> Enrollments { get; }
}

[Entity(3, 1, 3)]
internal interface MEnrollment
{
    [Property(1), Strong] MStudent Student { get; set; }                    // a student enrolled cannot be removed
    [Property(2)] DateOnly Date { get; set; }
    [Property(3)] Option<decimal> Grade { get; set; }
    [OwnedFrom] MCourse Course { get; }
}
```

## 5. An organization: a hierarchy of Roots

```csharp
[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MEmployee
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2), Strong] Option<MEmployee> Manager { get; set; }          // a manager with reports cannot be removed: move them first
    [ReverseStrongReference(nameof(MEmployee.Manager))]
    IEntityCollection<MEmployee> Reports { get; }
}
```

## 6. A fleet of devices: capabilities, a system, merged telemetry

```csharp
[Entity(1, 1, 4), Root, Removable, WithIdentity, Enumerable, Mergeable]
internal interface MDevice
{
    [Property(1)] GhostString Serial { get; set; }
    [Property(2), Delta] long Messages { get; set; }                       // incremented by every ingestion, without conflict
    [Property(3), Max] DateTime LastSeen { get; set; }
    [Property(4), Max] double PeakTemperature { get; set; }
}

[Entity(2, 1, 1), ComponentFor(nameof(MDevice)), Enumerable]
internal interface CBattery { [Property(1)] int Level { get; set; } }

[Entity(3, 1, 2), ComponentFor(nameof(MDevice)), Enumerable]
internal interface CLocation { [Property(1)] GhostString Building { get; set; } [Property(2)] int Floor { get; set; } }

[Entity(4, 1, 1), ComponentFor(nameof(MDevice))]
internal interface CMaintenance { [Property(1)] GhostString Reason { get; set; } }

[SystemFor(nameof(MDevice)), ExcludeComponent(nameof(CMaintenance)), Enumerable]
internal interface SToCharge { MDevice Device { get; } CBattery Battery { get; } Option<CLocation> Location { get; } }
```

`ToChargeSystemCollection.Where(20, static (s, t) => s.Battery.Level < t)` reads only the devices that have a battery. The serial is the key of the ingestion: a unique hash index on `Serial` ([../indexes.md](../indexes.md)).

## 7. A social graph: symmetric edges, an edge with a link

```csharp
[Entity(1, 1, 1), Root, Removable, WithIdentity, Enumerable]
internal interface MPerson { [Property(1)] GhostString Name { get; set; } }

[Entity(2, 1, 1), Edge(nameof(MPerson), nameof(MPerson), Symmetric = true), Removable, Enumerable]
internal interface EFriendship { [Property(1)] DateTime Since { get; set; } }

[Entity(3, 1, 1), Root, Removable, Enumerable]
internal interface MGroup { [Property(1)] GhostString Name { get; set; } }

[Entity(4, 1, 1), Root, Enumerable]
internal interface MRole { [Property(1)] GhostString Name { get; set; } }

[Entity(5, 1, 2), Edge(nameof(MPerson), nameof(MGroup)), Removable, Enumerable]
internal interface EMembership
{
    [Property(1), Strong] MRole Role { get; set; }
    [Property(2)] DateTime Since { get; set; }
}
```

`ada.NewFriendship(bob, since: day)`, `ada.FriendshipEdges.Others`, `person.MembershipEdges.GetOrNew(club, member).Since = day`.

## 8. Bookings with a capacity: a bounded counter

```csharp
[Entity(1, 1, 2), Root, Removable, WithIdentity, Enumerable]
internal interface MShow
{
    [Property(1)] GhostString Title { get; set; }
    [Property(2)] IEntityCollection<MSeatCategory> Categories { get; }
}

[Entity(2, 1, 2), WithIdentity, Mergeable]
internal interface MSeatCategory
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2), Delta(Min = 0)] int Remaining { get; set; }              // concurrent bookings merge; never below 0
    [OwnedFrom] MShow Show { get; }
}

[Entity(3, 1, 3), Root, Removable, WithIdentity, Enumerable]
internal interface MBooking                                                  // a fact that outlives nothing: a Root that retains its category
{
    [Property(1), Strong] MSeatCategory Category { get; set; }
    [Property(2)] int Seats { get; set; }
    [Property(3)] GhostString Holder { get; set; }
}
```

The booking code checks `category.Remaining >= seats` then writes `category.Remaining -= seats`; a concurrent booking that would go below zero fails its commit with `Bound`, and its retry sees "full".

## 9. A ledger: an encapsulated balance, movements as facts

```csharp
[Entity(1, 1, 2), Root, Removable, WithIdentity, Enumerable]
internal interface MAccount
{
    [Property(1)] GhostString Owner { get; set; }
    [Property(2), Private] decimal Balance { get; set; }                   // changed by the methods of the partial class only
}

[Record(101, 1, 4)]
internal interface RTransferred                                              // the history: a Record of a Log, kept forever
{
    [Property(1)] AccountRef From { get; set; }
    [Property(2)] AccountRef To { get; set; }
    [Property(3)] decimal Amount { get; set; }
    [Property(4)] DateTime At { get; set; }
}
```

A transfer that must stay designated by others (a dispute, a statement line) is a Root with two `[Strong]` references to its accounts, never an edge: it must outlive nothing, and refuse the removal of its accounts. In Mutation Sourcing, the transfer is a Mutation whose `Apply` changes both balances ([../mutation-sourcing.md](../mutation-sourcing.md)).

## 10. An ordered list: a quote and its lines

```csharp
[Entity(1, 1, 2), Root, Removable, WithIdentity, Enumerable]
internal interface MQuote
{
    [Property(1)] GhostString Reference { get; set; }
    [Property(2)] IEntityCollection<MQuoteLine> Lines { get; }
}

[Entity(2, 1, 3), WithIdentity]
internal interface MQuoteLine
{
    [Property(1)] int Position { get; set; }                                 // the business order
    [Property(2)] GhostString Label { get; set; }
    [Property(3)] decimal Amount { get; set; }
    [OwnedFrom] MQuote Quote { get; }
}

internal partial class Quote
{
    public void Renumber() { int n = 1; foreach (var line in Lines.OrderBy(static l => l.Position).ToList()) line.Position = n++; }
}
```
