# Relationships, lifetimes and births

Part of [the modeling knowledge](README.md). Every link of a GBO model is a property whose type is another entity model: reading it gives the target, in the transaction, with no join, key or loading. GBO keeps every link right at each instruction: no link ever designates an entity that does not exist, a refused operation changes nothing, and an entity is removed at the very instruction that releases its last retention.

## The table of links

| Declaration | Nature | Generated (Body) | When the target goes | SQL |
|---|---|---|---|---|
| `MRelease Release { get; }` | Required composition: exactly one part, born with its Owner | `Release Release { get; }` | Removed with the Owner; never moved nor replaced | Owned type |
| `Option<MBooklet> Booklet { get; }` | Optional composition: a slot | A slot: `Any`, `Get`, `TryGet`, `GetOr`, `New(...)`, `GetOrNew(...)`, `TryGetOrNew(...)`, `Remove()` | Removed with the Owner | `CASCADE` |
| `IEntityCollection<MTrack> Tracks { get; }` | Collection of parts, 0..N | `New(...)`, `Add(part)`, `Remove(part)`, `Contains`, `Count()`, the operators | Removed with the Owner | `CASCADE` |
| `[OwnedFrom] MAlbum Album { get; }` | The Owner of a part, no `[Property]` | `Album Album { get; }` | | |
| `[Strong] MArtist Artist { get; set; }` | Required strong reference | A constructor parameter and a Setter | Removal refused | `RESTRICT`, `NOT NULL` |
| `[Strong] Option<MGenre> Genre { get; set; }` | Optional strong reference | `Option<Genre>` with a Setter; a strong slot (`New`, `Set`, `Clear`) when the target is a shared object | Removal refused | `RESTRICT` |
| `[Strong] IEntityCollection<MArtist> Lineup { get; }` | Collection of strong references | `Add`, `Remove`; `New` when the target is a shared object | Removal refused | |
| `[Weak] Option<MAlbum> Previous { get; set; }` | Weak reference | `Option<Album>` with a Setter | Set to none at that instruction | `SET NULL` |
| `[Weak] IEntityCollection<MAlbum> Favorites { get; }` | Collection of weak references | `Add`, `Remove` | Leaves every such collection at that instruction | |
| `[ReverseStrongReference(nameof(MAlbum.Genre))] IEntityCollection<MAlbum> Albums { get; }` | Who designates me by that strong reference | A read-only collection | Up to date at each instruction | |
| `[ReverseWeakReference(nameof(MListener.Favorites))] IEntityCollection<MListener> Fans { get; }` | Who designates me by that weak reference or collection | A read-only collection | Up to date at each instruction | |

The type says the cardinality: `T` is required (never empty, a parameter of the birth, `new Album()` does not compile when `Album` needs an artist), `Option<T>` is 0 or 1, `IEntityCollection<T>` is 0..N. A required link refuses `null` (`ArgumentNullException`) before anything is written.

## Compositions

A part has one Owner, an entity (not a property). Each composition form has its own birth:

```csharp
var bowie = new Artist { Name = "David Bowie" };   // a Root
var low = bowie.Albums.New();                      // a part, born in its collection
low.Release.Label = "RCA";                         // a required part, born with its Owner
low.Booklet.New().Pages = 12;                      // a slot: New fills it, refuses a full slot
var speed = low.Tracks.New();                      // a part of a part
Assert.Same(low, speed.Album);                     // [OwnedFrom]
```

| Operation | Effect |
|---|---|
| `owner.Parts.New(...)` | A new part, its required links as parameters (`order.Lines.New(product)`) |
| `owner.Slot.New(...)` | Fills an empty slot; throws on a full one: `Remove()` it first |
| `owner.Parts.Add(part)` | Places a part the same Owner already holds by another composition (a track in `Tracks` and in `Highlights`), or an orphan; returns false when already there; throws for a part of another Owner |
| `owner.Parts.Remove(part)` | Takes it out; the part is **removed** when it leaves the last composition of its Owner (an `[Orphanable]` retained part becomes an orphan); returns false when absent |
| `owner.Slot.Remove()` | Removes the part of the slot |
| `part.MoveTo(other.Parts)` | Moves atomically to another Owner, leaving every composition of the old one; never without an Owner; refuses a cycle (a folder under its own subfolder); a target slot must be empty; a part of a required composition `T` cannot move |
| Removal of the Owner | Removes its parts, their parts, at the same instruction |

- A part has no public constructor and no `Remove()`: its end is a gesture of its Owner.
- A removed Body still reads its last values; writing to it throws.
- A required composition cannot be replaced: change its fields. A replaceable part is an `Option<T>` slot.
- **A required part with its own required links, or its own identity,** is described by its generated `Init` descriptor, a `readonly struct` given to the birth of its Owner: `new Order(customer, new OrderDetails.Init(carrier))`, `artist.Albums.New(albumRef, new Release.Init(releaseRef))`. An `Init` creates nothing until a birth consumes it. The constructor of a Root is the factory of its aggregate: its required links are its parameters, its values an object initializer, `new Order(customer, ...) { Number = 42 }`; every refusal is checked before the first write, so no aggregate is ever half built.
- **Several compositions of the same parts**: `Tracks` and `Highlights` of one album hold the same tracks; a track lives while it is in one of them.
- **A part with several Owner types** builds trees: a folder lives in a listener, or in another folder. Declare one `[OwnedFrom] Option<T>` per Owner type; exactly one is present. A chain of Owners must lead to a Root (GBO006). A cycle of **required** compositions is an error (GBO024); a cycle through an `Option` or a collection is allowed and checked at each placement.

```csharp
[Entity(2, 1, 3)]
internal interface MFolder
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] IEntityCollection<MFolder> Folders { get; }
    [Property(3)] IEntityCollection<MDocument> Documents { get; }
    [OwnedFrom] Option<MWorkspace> Workspace { get; }   // at the top of a workspace
    [OwnedFrom] Option<MFolder> Parent { get; }         // or in another folder
}
```

## Strong references

A `[Strong]` reference has one of two effects, by its target:

- **Target a Removable Root**: its removal is refused while the reference designates it, by an `InvalidOperationException` thrown **before** any change; the transaction goes on. Remove in the order of the references: the album first, then its artist.
- **Target a Root that is not Removable**: nothing to protect, no counter kept: a million albums of one genre cost the genre nothing.
- **Target a non-Root**: it **retains** it (see shared objects, and retention of a chain).

`[Strong]` is required even on a `T` whose target is a Root: a link without attribute is a composition, refused toward a Root (GBO021).

**Retention of a chain.** A strong reference to a part retains the part, its Owner, and their Owners up to the Root: a playlist entry that designates a track forbids the removal of the track, of its album and of its artist; the exception names the chain (`Artist 1 → Album 1 → Track 1`). A strong reference from inside the aggregate being removed retains nothing (a remix of a track on the same album leaves with it).

**`[Orphanable]`.** Declared on a part type, it lets a retained part survive its Owner: removing the artist keeps each album that a playlist plays, whole, without an artist (`[OwnedFrom] Option<MArtist>` empty). The orphan is removed at the release of its last retention, or adopted by `owner.Parts.Add(orphan)`.

## Weak references

`[Weak]` retains nothing. When the target is removed, the transaction that removes it sets every weak reference to none and takes it out of every weak collection, **at that instruction**; the commit writes the referrers. A weak link is always an `Option<T>` or a collection (`[Weak] T` is GBO017). Choose it for a history, a suggestion, a last-viewed item, favorites. The removal of a weak target calls no code of the referrers: a type that must react retains by `[Strong]`, or reacts in the `OnDeleting()` of the target.

## Shared objects

A type that is neither Root nor composed by any property, and that strong references designate, is a shared object: it lives while a strong reference designates it, as an `Rc<T>` of Rust, and is removed at the instruction that releases the last one. It has no public constructor; it is born by its first strong reference:

| Link to the shared object | Birth and life |
|---|---|
| `[Strong] MRecording Recording { get; set; }` (required) | The birth of the holder takes an existing object or a new descriptor: `album.Tracks.New(new Recording.Init())`, `best.Tracks.New(original.Recording)`; the generated `track.NewRecording()` creates a new one, designates it, releases the old |
| `[Strong] Option<MLyrics> Lyrics { get; set; }` (a strong slot) | `New()` creates and designates; `Set(x)` designates an existing one; `Clear()` releases |
| `[Strong] IEntityCollection<MPoster> Posters { get; }` | `New()`, `Add(x)`, `Remove(x)` |

**Take the new retention before releasing the old one**: `final.Lyrics.Set(lyrics); demo.Lyrics.Clear();`. The other order removes the object, and the `Set` that follows is refused ("removed"). Strong references between non-Root types that may live without an Owner can form a cycle that nothing removes (warning GBO027: there is no cycle collector).

## Collections are sets

- A collection holds an entity once, has no position, and enumerates in the order GBO chooses (not the order of creation, not stable): a business order is a property, read by `OrderBy`.
- No size limit: `Count()` is exact and immediate, `Contains` finds without reading the others.
- Adding or removing an item of a collection while enumerating it throws at the next step; changing values is fine. Remove all by `while (c.FirstOrNone().TryGet(out var item)) c.Remove(item);` or enumerate a `ToList()` first.

## Reverse properties

`[ReverseStrongReference(path)]` and `[ReverseWeakReference(path)]` read the other direction without a query, up to date at each instruction, on Bodies and Views. The path names a property of the other type, of the same nature, designating this one (GBO028 otherwise). The referrers of a weak reference are kept anyway (the removal needs them): exposing them is free. Those of a strong reference are kept **only when declared**, at the cost of one write per new reference: declare those the code reads. A product sold by a million lines needs `Lines` only if the code reads the lines of a product.

## Get or make: `GetOrNew`, `TryGetOrNew`

On a slot that holds one object at most (optional composition, strong slot to a shared object, component slot, edge of a pair), beside `New(p)`:

| Instruction | Empty | Holds an object born with the same parameters | Holds one born with other parameters |
|---|---|---|---|
| `New(p)` | Born | Throws | Throws |
| `GetOrNew(p)` | Born | The object | Throws before any write; the transaction goes on |
| `TryGetOrNew(p, out x)` | `true`, born | `true`, the object | `false`, the existing object; nothing written |

The parameters compared are the required links (the same entity, not an equal one) and the identity. Values are set after the call. The same code is right at the first try and at the retry after a conflict: `PartyCollection.Single().Customer.GetOrNew().CreditLimit = limit;`.

## Your rules on the links

The partial class of a Body receives, when written: `partial void On{Collection}Added(T item)`, `On{Collection}Removed(T item)`, `On{Property}Changed()` for a reference or a slot, and `OnDeleting()`. Each runs **after** the operation of GBO is complete. `OnDeleting()` runs once per removed entity, from the Owners to their parts, after every check and before the writes: it reads the entity, its parts and Owners, may create other entities (an archive note), never retains a removed one. GBO never calls your code to keep the links right: counts, cascades and referrers are its own work.

```csharp
internal partial class Album
{
    public const int MaxTracks = 3;
    internal void Recount() => Seconds = Tracks.Sum(static t => t.Seconds);

    public Track AddTrack(string title, int seconds)             // check, then write: a refusal leaves the transaction usable
    {
        if (Tracks.Count() >= MaxTracks)
            throw new InvalidOperationException($"The album {Title} holds {MaxTracks} tracks at most.");
        var track = Tracks.New();
        track.Title = title;
        track.Seconds = seconds;
        return track;
    }

    partial void OnTracksAdded(Track item) => Recount();
    partial void OnTracksRemoved(Track item) => Recount();
    partial void OnDeleting() => new Note { Text = $"{Title}, by {Artist.Name}, removed" };
}
```

## What dies, and when

| Entity | Lives | Is removed |
|---|---|---|
| Root, not Removable | Always | Never |
| Removable Root | Until its `Remove()` | By `Remove()`, refused while a strong reference retains it or one of its parts |
| Part | While its Owner holds it | With its Owner, or taken out of its last composition |
| Orphanable part | While owned, or retained (it or one of its parts) | At the release of its last retention, once without Owner |
| Shared object | While a strong reference designates it | At the release of its last strong reference |
| Component | With its entity | By `entity.Slot.Remove()`, or with its entity |
| Edge | With both ends | By `Remove()` if `[Removable]`, or with either end |

GBO applies this rule at each instruction, to the entities the instruction touches: it never walks the whole graph, and a removal costs what it removes. Every refusal comes before any change. The order of the instructions matters: each is checked against the state of the transaction at that instruction, not at the commit.

## Links and concurrency

- An aggregate is a unit of conflict: two transactions that add parts to the same album conflict at the second commit; parts of two albums never conflict.
- A link never designates an entity removed concurrently: adding a strong reference to a track while another transaction removes it conflicts at the second commit.
- More in [concurrency.md](concurrency.md).
