# Identities: `[WithIdentity]` and the `*Ref` types

Part of [the modeling knowledge](README.md). Inside a Repository, entities designate each other by links: the technical identifier of an entity is invisible and never needed. An entity that is known **outside** the Repository (returned by an API, named by a message, recorded by an event or a Mutation, kept by another system) takes a business identity.

## When to declare it

| The need | `[WithIdentity]`? |
|---|---|
| An API returns the entity, a client sends it back | Yes |
| A Record, an event or a Mutation names the entity | Yes: a Record has no link, it holds a `*Ref` |
| A Replay must recreate the entity with the same identity | Yes |
| Another system, a POCO, a cache, a `Dictionary` key keeps it | Yes |
| A walk of a graph remembers the ends it met | Yes, on the ends (`HashSet<CityRef>`) |
| A part that a message designates (an order line on a return slip) | Yes: parts take one too |
| A part addressed only within its Owner, a short list (categories) | No: a local `short` number allocated by the Owner, a property |
| Reference data found by a business code from outside (an ISO country code) | No: a property, a unique index if it is hot |

`[WithIdentity]` is irreversible once published: removing it is refused at the bind, since old events must stay resolvable.

## The generated `XRef`

For `[WithIdentity] interface MOrder`, the generator writes `OrderRef`, a `readonly struct` of 16 bytes (the type identifier, the creation time in milliseconds, 64 random bits):

| Member | Meaning |
|---|---|
| `OrderRef.New()` | A new identity, drawn anywhere, outside any transaction too; **throws in a Replay transaction** of its Repository |
| `order.Ref`, `orderView.Ref` | The identity of an entity |
| `ref.Resolve()` | The `Order` in the ambient transaction of its Repository; throws when none has it |
| `ref.ResolveOrNone()` | `Option<Order>`: the normal way when the absence is possible |
| `ref.TryResolve(out Order? order)` | `bool` |
| `ref.IsNull` | `default(OrderRef)`, the null identity; a drawn identity is never null |
| `==`, `Equals`, `GetHashCode`, `CompareTo` | By value: a key of a `Dictionary` or a `HashSet`; ordered by creation time |
| `ToString()`, `Parse`, `TryParse` (`ISpanFormattable`, `ISpanParsable`) | 32 hexadecimal digits; refuses the text of another type |
| `ReadOrderRef.Of(orderRef)` | The identity of the projection of this entity in another type (a Read model): derived, nothing stored or searched, allowed in a Replay; one projection of one type per source |

There is no implicit conversion between `Order` and `OrderRef`, and no conversion to or from a `Guid`: never cast a `*Ref` to a `Guid` (`Unsafe.BitCast`), store the `*Ref` itself.

## Every birth takes its identity

The engine never draws an identity: an identity forgotten does not compile.

| Entity | Birth |
|---|---|
| Root | `new Artist(artistRef) { Name = ... }` |
| Part | `album.Tracks.New(trackRef)`, `order.Lines.New(lineRef, product)`: the identity first |
| Required part | `new Release.Init(releaseRef)`, given to the birth of its Owner |
| Shared object, optional part (a slot) | The identity first in `New`, `NewX`, `Init`; a component and an edge never have one |

A null identity is refused (`ArgumentException`); one given twice in a transaction is refused before the birth (`InvalidOperationException`); one taken by a concurrent commit fails the commit (`TransactionConflictKind.Identity`). The identity of a removed entity is free again, in the same transaction too.

**Who draws the identity:** the code that decides the birth, outside the Replay. In Event Sourcing, the command draws every identity of the graph it creates and writes them in its Record; applying the Record creates the graph with them. In Mutation Sourcing, the application draws the identity of what it asks to create (Intention), and `Decide` draws the identities it decides (Decision). A Replay reads them back; it never draws.

```csharp
var released = new AlbumReleased(ArtistRef.New(), "David Bowie", AlbumRef.New(), "Low", ReleaseRef.New(), [TrackRef.New(), TrackRef.New()]);

static void Apply(AlbumReleased e)                     // the same code at the command and at the Replay
{
    var artist = new Artist(e.Artist) { Name = e.Name };
    var album = artist.Albums.New(e.Album, new Release.Init(e.Release));
    album.Title = e.Title;
    foreach (var track in e.Tracks)
        album.Tracks.New(track);
}
```

## Where an identity may be stored

| Place | Allowed |
|---|---|
| A property of a Record (event, Mutation, Job) | Yes |
| An element of a `GhostSpan<XRef>` in a Record | Yes |
| A field of a stored struct of a Record | Yes (16 bytes aligned on 8) |
| A property of an entity | **No** (GBO012): a link instead; toward an entity of another Repository of the chain, derive with `XRef.Of(source)`. An `EntityRef`, of any type: **yes** |
| `Option<XRef>` | **No** (GBO002): `XRef` and `IsNull` |
| A C# command, a POCO, a message, an API payload, a dictionary key | Yes, valid outside any transaction, forever: resolution may legitimately fail later (`ResolveOrNone`) |

A Record of another assembly can hold a `*Ref`: it is recognized by its `[GhostIdentity(typeId)]` attribute.

## `EntityRef`, the identity of an entity of any type

`EntityRef` (`GhostBodyObject.Runtime.Logical.Model`) holds the 16 bytes of any `*Ref`, its type in them: `EntityRef.Of(order.Ref)`, `TypeId`, `IsNull`, the equality, the text of its `*Ref`. A property of an entity or of a Record, alone or in a span: no relation can designate an entity of any type, so GBO012 lets it into an entity. It is a value: no counter, no reverse set, its entity free to go. Back to the typed `*Ref`, compare the type first; `XRef.Of` retypes without a check:

```csharp
var back = target.TypeId == Order.TypeId ? OrderRef.Of(target) : default;
```

## Projections and correlation

A Read model entity that projects a Write model entity takes the derived identity: `new ReadQuote(ReadQuoteRef.Of(quoteRef))`, found later by `ReadQuoteRef.Of(quoteRef).Resolve()`. The application finds what its command created by the identity it drew, without reading any result.

## Aggregates identified by a `Guid` elsewhere

A source system that identifies its aggregates by `Guid` has two ways: draw a `*Ref` per aggregate and keep the source `Guid` as a property with a unique index; or keep the `Guid` as the business key without `[WithIdentity]` (a Record then holds the `Guid`, and code finds the entity by the index). An arbitrary `Guid` never becomes a `*Ref`.

## Identities and schemas

The identifier of the type is inside each `*Ref`: in a Deduced schema, a `[WithIdentity]` type must fix its type identifier, `[Entity(typeId)]` (GBO042), or the identities held by a Fixed Log would stop resolving when a type is added. Derived identities (`XRef.Of`) take the type of the projection: a `[WithIdentity]` type of a deduced Read model fixes its identifier as well.
