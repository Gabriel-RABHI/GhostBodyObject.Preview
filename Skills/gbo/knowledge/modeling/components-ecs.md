# Components and systems: the composition model (ECS)

Part of [the modeling knowledge](README.md). GBO takes the Entity Component System of video games and applies it to any data model: **the entity is an anchor**; its **capabilities** are components that it does not know; a **system** joins an entity and a combination of its components. This replaces inheritance: no base class, no subtype, no nullable columns for the cases that most entities do not have.

## When to use a component

| Signal | Example |
|---|---|
| Some entities of the type have it, others not | A battery on wireless locks only; a web page on products sold online |
| It comes and goes during the life of the entity | On sale, under maintenance, verified, blocked, on tour |
| It has values of its own, set and cleared together | A sale: rate and end date; a maintenance: reason and technician |
| Several unrelated types share it, without a common base | Everything that runs on a battery; everything that can be commented |
| A role of a party | Customer, supplier, employee of one `MParty` |
| It changes at another pace, by other people, than the entity | The credit limit of a customer (sales) vs its name (legal); the stock of a product vs its description |
| A property that most entities leave empty | Make it a component: it costs nothing to the entities that do not have it |
| A flag `IsX`, `HasX`, `NeedX` and its satellite fields | `IsTemplate` and six `Template*` fields: a component `CTemplate`, whose presence is the flag |
| Fields meaningful in one state, or for one kind, only | The data of a generated quote, `CGeneration`; the pricing of a product line, `CPricing` |
| A separate concern that the entity must not know | Templates, validations, pricing, audit, synchronization with an ERP: each in its own namespace, its rules in its own partial classes |

A value that every entity has for its whole life, as its own responsibility, is a property; a structure every entity has, created and removed with it, is a part.

## When not to use a component

| The candidate | Then |
|---|---|
| An entity may have several of it | Parts of a component, or a collection of parts |
| It must be named from outside by its own identity | A part with `[WithIdentity]`, or a Root: a component has no identity, a Record names it by the `*Ref` of its entity |
| It moves from one entity to another | A part (`MoveTo`) or a Root |
| Every entity has it, for its whole life, as its own responsibility | A property, or a part |

## Sub-components

A component is never the component of a component (GBO029). An aspect of an aspect is one of three forms:

| The need | Form |
|---|---|
| The aspect owns sub-structures | Parts of the component: `CSupplier.Offers`, `CAlarmLog.Alarms` |
| A second aspect that only makes sense with the first | A second component of the same entity, and a system that joins them: `CPricing` and `CDiscounts` on a line, `SDiscountedLine` |
| An aspect of each part of the entity | A component of the part type: `[ComponentFor(nameof(MQuoteLine))] CPricing`, removed with its line |
| A list of entities inside an aspect, with aspects of their own | Parts of the component, and components of those parts, as deep as the concepts need: `CSupplier.Offers` (`MOffer`), `[ComponentFor(nameof(MOffer))] CPromotion`, its `Tiers`; removing the role removes the whole subtree, the party stays |

## Declaring a component

```csharp
[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MDoorLock
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] GhostString Room { get; set; }
}

[Entity(2, 1, 2)]
[ComponentFor(nameof(MDoorLock))]      // repeatable: [ComponentFor(nameof(MCamera))] too
[Enumerable]                           // BatteryCollection: everything that runs on a battery
internal interface CBattery
{
    [Property(1)] int Level { get; set; }
    [Property(2)] DateTime Replaced { get; set; }
}

[Entity(3, 1, 1), ComponentFor(nameof(MDoorLock))]
internal interface CAlarmLog
{
    [Property(1)] IEntityCollection<MAlarm> Alarms { get; }   // a component may own parts
}

[Entity(5, 1, 2), ComponentFor(nameof(MDoorLock))]
internal interface CMaintenance
{
    [Property(1)] GhostString Reason { get; set; }
    [Property(2), Strong] MTechnician Technician { get; set; } // a required link: a parameter of New
}
```

- The interface starts with `C`; `CBattery` gives the Body `Battery`, and a **slot** on each entity type it attaches to: `door.Battery`.
- The entity declares nothing: a new capability changes nothing of the entity, not even its version.
- A component is never `[Root]`, `[Removable]`, `[WithIdentity]` nor `[Orphanable]`, never the target of a composition, never the component of a component (GBO029).
- The Repository includes it: `[IncludeEntityModel(nameof(CBattery))]` (or by namespace); nothing designates it, so a deduced Repository cannot find it by its relations.

## The slot

| Member | Effect |
|---|---|
| `door.Battery.Any()`, `Get()`, `TryGet(out b)`, `GetOr(...)` | Reads as an `Option<Battery>` |
| `door.Battery.New(...)` | Attaches a new component; its required links are parameters (`door.Maintenance.New(technician)`); refused if the entity has one already, nothing changed |
| `door.Battery.GetOrNew(...)` | The component it has, or a new one; throws, before any write, when the existing one was born with other parameters |
| `door.Maintenance.TryGetOrNew(ada, out var m)` | `false` and the existing component when its parameters differ: a business case handled at once |
| `door.Battery.Remove()` | Removes the component and its parts; the entity stays; returns false when none |
| `battery.DoorLock` | The entity of the component: a `T` for one entity type; for several types, an `Option` per type (`battery.Camera`, `battery.DoorLock`), exactly one set |
| `doorView.Battery` | On a View, an `Option` of the View of the component |

- **At most one component of a type per entity.**
- **A component lives with its entity**: removed with it, with its parts, at the same instruction; the reverse never happens: removing a capability never removes its entity.
- **A component never changes of entity**: there is no `MoveTo`. Giving a battery to another lock is creating one there with the same values, and removing the old one.
- **Links to a component**: a strong reference to a component retains it **and its entity** (a work order about a maintenance keeps the lock); a weak one forgets it when it goes.

## Shared capabilities and polymorphism

A `[ComponentFor]` repeated attaches one component type to several entity types. Its collection holds every entity that has the capability, whatever its type: a question about a capability is a query over its component, not over each type.

```csharp
// Every device of the second floor, whatever its type: a query over the locations.
var second = LocationViewCollection.Where(2, static (place, floor) => place.Floor == floor)
    .Select(static place => place.DoorLock.Any() ? place.DoorLock.Get().Name : place.Camera.Get().Name).ToList();
```

The model has no inheritance and no reference typed by an interface. Polymorphism lives **in your code**: the Bodies are partial classes; a C# interface adds what types share.

```csharp
internal interface IPowered { Option<Battery> Power { get; } }

internal partial class Camera : IPowered { public Option<Battery> Power => this.Battery; }
internal partial class DoorLock : IPowered { public Option<Battery> Power => this.Battery; }

static bool NeedsCharge(IPowered device, int threshold) => device.Power.TryGet(out var b) && b.Level < threshold;
```

Name the members of these interfaces differently from the slots (`Power`, not `Battery`): a member of the name of a component hides its slot (GBO029).

## Systems

A system joins an entity and a combination of its components, declared once, read as Views:

```csharp
[SystemFor(nameof(MDoorLock))]
[ExcludeComponent(nameof(CMaintenance))]   // repeatable: the locks under maintenance are out
[Enumerable]
internal interface SPoweredLock
{
    MDoorLock Lock { get; }                 // the entity: exactly one member of its type
    CBattery Battery { get; }               // a required component: a T
    Option<CLocation> Location { get; }     // an optional component: an Option<T>
}
```

- The interface starts with `S`; `SPoweredLock` gives the `readonly partial struct PoweredLockSystem` and, with `[Enumerable]`, `PoweredLockSystemCollection`. Its members have no `[Property]`, no setter. Include it in the Repository.
- **Its members are Views**: the enumeration creates no Body; `system.Battery.ToBody()` for the few to change.
- `PoweredLockSystem.Of(lockBody)` or `Of(lockView)`: the system of one entity, an `Option`.
- A component given or taken by the transaction is seen at the next instruction: nothing to refresh.
- **It reads what may belong to it**: the entities of its rarest required component, not every entity of its type. A system without a required component reads every entity of its type. Require what the question requires; leave the rest optional.
- **Your rules go in the system**: a `readonly partial struct`, so a business rule about a combination is written once, where it belongs.

```csharp
internal readonly partial struct FirmwareCameraSystem
{
    public bool CanUpdateTo(string version) => string.CompareOrdinal(Firmware.Version.ToString(), version) < 0
        && (!Battery.Any() || Battery.Get().Level >= 50);
}

var ready = FirmwareCameraSystemCollection.Where("3.0", static (s, v) => s.CanUpdateTo(v)).ToList();
```

The generator checks a system: one member of its entity type, the others components of that type, none both present and excluded (GBO030).

## Roles, not subclasses

```csharp
[Entity(1, 1, 1), Root, Removable, Enumerable]
internal interface MParty { [Property(1)] GhostString Name { get; set; } }

[Entity(2, 1, 1), ComponentFor(nameof(MParty)), Enumerable]
internal interface CCustomer { [Property(1)] decimal CreditLimit { get; set; } }

[Entity(3, 1, 2), ComponentFor(nameof(MParty)), Enumerable]
internal interface CSupplier
{
    [Property(1)] int PaymentDays { get; set; }
    [Property(2)] IEntityCollection<MOffer> Offers { get; }   // the offers leave with the role
}

[Entity(7, 1, 2), Root, Removable, Enumerable]
internal interface MOrder
{
    [Property(1)] decimal Amount { get; set; }
    [Property(2), Strong] CCustomer Customer { get; set; }  // an order retains the customer role, and its party
}

[SystemFor(nameof(MParty)), Enumerable]
internal interface STradingPartner { MParty Party { get; } CCustomer Customer { get; } CSupplier Supplier { get; } }
```

A party plays any role at once, gains one, loses another, and stays the same party, with the same identity and links: `acme.Customer.New()`, `acme.Supplier.Remove()`. With inheritance, a supplier that becomes a customer would change of class, so of identity.

## Components and concurrency

- **A component is an aggregate of its own**: its values and parts conflict only with themselves. The sales team raises a credit limit while the legal team renames the party: both commit.
- **Giving or taking a capability changes the capabilities of its entity**: two transactions that give the same party a customer role conflict at the second commit; a party never ends with two. Written with `GetOrNew()`, the retry runs the same code and finds the role the other gave.
- A component can be `[Mergeable]`; it cannot be a `[ConcurrencyPoint]`.

## The path from the need to the component

| Question | Yes → | Declaration |
|---|---|---|
| Does every entity have it, for its whole life? | A property, or parts | `[Property(n)]` |
| Do some entities have it, for a time, with values of its own? | A component | `[ComponentFor(nameof(MX))]`, `CName` |
| Does the capability own things? | Parts of the component | `IEntityCollection<T>` in the component |
| Is it born with a link? | A required link of the component | A `T`, given to `New` |
| Is it shared by several types? | A shared component | `[ComponentFor]` repeated |
| Must your code treat these types alike? | A C# interface | `partial class X : IInterface` |
| Does a question join several capabilities? | A system | `[SystemFor(nameof(MX))]`, `SName`, `[ExcludeComponent]` |
| Is it a role of a party? | A component of the party | `[ComponentFor(nameof(MParty))]` |
| Does it change at another pace than its entity? | A component, an aggregate of its own | `[ComponentFor]` |
