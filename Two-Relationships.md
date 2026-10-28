# Tutorial Two: Relationships

## Links, constraints and deterministic garbage collection

Tutorial One worked with one entity type. A real model is a graph: an album has an artist, an artist has albums, a playlist plays the tracks of many albums. This tutorial shows how the entities of a model hold each other, and what GBO guarantees about them:

- **The links.** References, strong and weak; compositions, which own their parts; collections of references; reverse properties, which answer the question "who points to me?".
- **The constraints.** No link ever designates an entity that does not exist. A removal that would break a link is refused before anything changes.
- **The deterministic garbage collection.** An entity that nothing holds any more is removed at the very instruction that releases it, with everything it owns.

None of this takes code of yours. You declare the nature of each link in the model; GBO keeps every link right, at each instruction, in the transaction and at the commit. GBO provide the benefits of the Rust programming language using the data structure model : it avoid null reference errors and free the memory deterministically as soon as references that retain objects are broken. 

Each lesson is a test class of the project `GhostBodyObject.TutorialTwo`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorial One: the transactions, the collections, `Option<T>`, the Views and the static lambdas are used here without being explained again.

## What you will build

A music label and a streaming service: artists, their albums and their tracks; the recordings and the lyrics that tracks share; the playlists and the favorites of the listeners; the festivals that book artists.

The model grows with the tutorial. Each lesson, or group of lessons, declares its own model, enriched with the concepts of the lesson: thirteen small models, from the three links of Lesson 1 to the whole catalog of Lesson 20, and the capabilities of Lesson 21.

> **Tip: Twelve models in one project**
>
> Each model of this tutorial lives in a namespace of its own, with its own Repository. The generator makes the collections of a Repository, `AlbumCollection`, `TrackCollection`..., visible in the whole project: twelve models give twelve `AlbumCollection`. So each lesson starts its namespace with one line, which chooses the collections of its own Repository:
>
> ```csharp
> namespace TutorialTwo.Catalog
> {
>     using static CatalogRepositoryCollections;
> ```
>
> C# looks in the namespace before the project. An application with one Repository needs no such line.

Every link of the tutorial, and the lesson that shows it:

| Declaration in the model                                       | Nature                          | What GBO guarantees                                                                | Lessons |
| -------------------------------------------------------------- | ------------------------------- | ---------------------------------------------------------------------------------- | ------- |
| `[Strong] MArtist Artist`                                      | Strong reference                | The target cannot be removed while the reference designates it: `ON DELETE RESTRICT` | 1, 2    |
| `[Weak] Option<MAlbum> Previous`                               | Weak reference                  | Set to none when its target is removed: `ON DELETE SET NULL`                        | 3       |
| `IEntityCollection<MTrack> Tracks`, `MRelease Release`         | Composition                     | The Owner holds its parts; they are removed with it: `ON DELETE CASCADE`            | 4 to 8  |
| A strong reference to a part                                   | Retention of a chain            | The part, its Owner and their Owners, up to the Root, cannot be removed             | 9       |
| `[Orphanable]` on a part                                       | Orphan                          | A retained part survives its Owner, until its last retention                       | 10      |
| `[Strong] MRecording Recording`, to a type neither Root nor part | Shared object                 | The target lives while a strong reference designates it                            | 11, 12  |
| `[Strong] IEntityCollection<MArtist> Lineup`                   | Collection of strong references | Each target is retained                                                            | 13      |
| `[Weak] IEntityCollection<MAlbum> Favorites`                   | Collection of weak references   | A removed target leaves the collection                                             | 14      |
| `[ReverseWeakReference(...)]`, `[ReverseStrongReference(...)]` | Reverse property                | The entities that point to this one, up to date at each instruction                | 15      |
| `[ComponentFor(nameof(MArtist))]` on a type                    | Component                       | At most one of a type per entity, which does not know it; removed with its entity  | 21      |
| `[SystemFor(nameof(MArtist))]` on an interface                 | System                          | The entities that have its components, with their Views                            | 21      |

## Part 1: References between Roots

### Lesson 1: Link two entities

A link is a property whose type is another entity of the model. Reading it gives the target, in the transaction: there is no join to write, no foreign key to set, no lazy loading to configure.

The first model has three Roots: a genre, an artist, and an album that designates both, and the album that came before it.

```csharp
[Entity(1, 1, 1)]
[Root]
internal interface MGenre
{
    [Property(1)]
    string Name { get; set; }
}

[Entity(2, 1, 1)]
[Root]
[Removable]
[Enumerable]
internal interface MArtist
{
    [Property(1)]
    string Name { get; set; }
}

[Entity(3, 1, 5)]
[Root]
[Removable]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    int Year { get; set; }

    // A link is a property whose type is an entity of the model. [Strong]: the album retains its artist (Lesson 2). A T, not
    // an Option<T>: an album always has an artist, given to its constructor.
    [Property(3)]
    [Strong]
    MArtist Artist { get; set; }

    // An Option<T>: an album may have no genre.
    [Property(4)]
    [Strong]
    Option<MGenre> Genre { get; set; }

    // [Weak]: the album does not retain the previous one (Lesson 3). A weak link is always an Option<T>: its target may
    // disappear.
    [Property(5)]
    [Weak]
    Option<MAlbum> Previous { get; set; }
}

[GhostRepository]
[IncludeEntityModel(nameof(MGenre))]
[IncludeEntityModel(nameof(MArtist))]
[IncludeEntityModel(nameof(MAlbum))]
internal interface MCatalogRepository
{
}
```

Every link declares its nature: `[Strong]` retains its target, `[Weak]` does not (Lessons 2 and 3); a link with neither is a composition, which owns its target (Lesson 4). The type of the property says its cardinality:

| Type        | Cardinality                                                                                     |
| ----------- | ----------------------------------------------------------------------------------------------- |
| `T`         | Required: a parameter of the constructor, never empty. `new Album()` does not compile.         |
| `Option<T>` | Optional: empty until it is set. `Any()`, `Get()` and `TryGet` read it, as in Tutorial One.     |

```csharp
[Fact]
public void CreateAnAlbumWithItsArtist()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-link", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };

        // The artist is a T: the constructor of Album requires it. new Album() does not compile.
        var low = new Album(bowie) { Title = "Low", Year = 1977 };

        // Reading the link gives the Body of the artist: the very same object.
        Assert.Same(bowie, low.Artist);

        // The genre is an Option: empty, until it is set.
        Assert.False(low.Genre.Any());
        low.Genre = new Genre { Name = "Art rock" };
        Assert.Equal("Art rock", low.Genre.Get().Name);
        transaction.Commit();
    }

    // In a new transaction, the links lead to the entities of the Store.
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var low = AlbumCollection.Single();
        Assert.Equal(("David Bowie", "Art rock"), (low.Artist.Name, low.Genre.Get().Name));

        // A View follows the links too, without creating any Body (Tutorial One, Lesson 10).
        var view = AlbumViewCollection.Single();
        Assert.Equal("David Bowie", view.Artist.Name);
    }
}
```

A link designates the entity itself, not a copy of it: a change of the artist is seen through every album that designates it. Setting a link designates another entity; the first one is unchanged.

```csharp
[Fact]
public void SeeTheTargetAsItIs()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-target", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = new Album(bowie) { Title = "Low" };
    var heroes = new Album(bowie) { Title = "Heroes" };

    bowie.Name = "Bowie";
    Assert.Equal(("Bowie", "Bowie"), (low.Artist.Name, heroes.Artist.Name));

    // Setting a link changes the target, not the entity it designated.
    heroes.Artist = new Artist { Name = "Brian Eno" };
    Assert.Equal(("Bowie", "Brian Eno"), (low.Artist.Name, heroes.Artist.Name));
    transaction.Commit();
}
```

A required link is never empty: the constructor and the Setter refuse `null`, before anything is written.

```csharp
[Fact]
public void RefuseAnEmptyRequiredLink()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-required", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    Assert.Throws<ArgumentNullException>(() => new Album(null!));
    var low = new Album(new Artist { Name = "David Bowie" }) { Title = "Low" };
    Assert.Throws<ArgumentNullException>(() => low.Artist = null!);

    // Nothing was created by the refused constructor: one album, one artist.
    Assert.Equal((1, 1), (AlbumCollection.Count(), ArtistCollection.Count()));
    transaction.Commit();
}
```

> **Note: Coming from Entity Framework?**
>
> In Entity Framework, a relationship is a navigation property and a foreign key: you `Include` the related entities, or configure lazy loading, and the key and the navigation must agree.
>
> In GBO, a link is its target. Reading `low.Artist` gives the artist, in the transaction, with nothing to load first; setting it designates another artist. There is no key to keep in step, and no query to write.

### Lesson 2: A strong reference protects its target

An album designates its artist by a `[Strong]` reference: while it does, the artist cannot be removed. Its `Remove()` throws at the call, as `ON DELETE RESTRICT` does in SQL, and **changes nothing**: the transaction goes on, and commits. No reference ever designates an entity that does not exist any more.

```csharp
[Fact]
public void RefuseTheRemovalOfAnArtistThatAnAlbumDesignates()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-refuse", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = new Album(bowie) { Title = "Low" };

    var refused = Assert.Throws<InvalidOperationException>(bowie.Remove);
    Assert.Contains("cannot be removed", refused.Message);

    // The refusal comes before any write: the artist and its album are there, and the transaction is alive.
    Assert.Same(bowie, low.Artist);
    Assert.Null(transaction.CondemnedBy);
    transaction.Commit();
}
```

The artist can be removed once nothing designates it: another artist set on the album, or the album removed first. Each one releases its reference.

```csharp
[Fact]
public void RemoveTheArtistOnceNothingDesignatesIt()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-release", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        new Album(bowie) { Title = "Low" };
        new Album(bowie) { Title = "Heroes" };
        transaction.Commit();
    }
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = ArtistCollection.Single();
        var low = AlbumCollection.Single(static a => a.Title == "Low");
        var heroes = AlbumCollection.Single(static a => a.Title == "Heroes");

        // Setting another artist releases the reference to Bowie: one album still designates him.
        low.Artist = new Artist { Name = "Brian Eno" };
        Assert.Throws<InvalidOperationException>(bowie.Remove);

        // The removal of an album releases its references: nothing designates Bowie any more.
        heroes.Remove();
        bowie.Remove();
        Assert.Equal(["Brian Eno"], ArtistCollection.Select(static a => a.Name).ToList());
        transaction.Commit();
    }
}
```

GBO counts the strong references to each entity: the count is exact at each instruction, never computed later. So the order of the instructions matters:

```csharp
[Fact]
public void RemoveInTheOrderOfTheReferences()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-order", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = new Album(bowie) { Title = "Low" };

    // GBO checks each instruction against the state of the transaction at that instruction, not against the state at the
    // commit: the artist is refused while the album is there.
    Assert.Throws<InvalidOperationException>(bowie.Remove);
    low.Remove();
    bowie.Remove();
    Assert.Equal(0, ArtistCollection.Count());
    transaction.Commit();
}
```

> **Tip: Checked at each instruction**
>
> In SQL, a constraint may be deferred to the commit. In GBO, each instruction is checked against the state of the transaction at that instruction: remove the album first, then its artist, never the reverse.

A genre is a Root that is not `[Removable]`: it has no `Remove()`, and nothing ever removes it. Nothing has to protect it, so GBO keeps no count for it: a million albums of one genre cost the genre nothing.

```csharp
[Fact]
public void NeverRemoveAGenre()
{
    // The generator writes Remove() for a [Removable] Root only.
    Assert.NotNull(typeof(Artist).GetMethod("Remove"));
    Assert.Null(typeof(Genre).GetMethod("Remove"));

    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-genre", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var rock = new Genre { Name = "Rock" };
    var bowie = new Artist { Name = "David Bowie" };
    for (int year = 1970; year < 1980; year++)
        new Album(bowie) { Title = $"Album {year}", Year = year, Genre = rock };
    Assert.Equal(10, AlbumCollection.Count(static a => a.Genre.Any()));
    transaction.Commit();
}
```

### Lesson 3: A weak reference forgets its target

An album designates the previous album of its artist by a `[Weak]` reference: it does not retain it. When the previous album is removed, the transaction that removes it sets every weak reference to it to none, **at that very instruction**, as `ON DELETE SET NULL` does in SQL. The commit writes the albums that changed: a weak reference never designates an entity that does not exist, in the transaction or in the Store.

```csharp
[Fact]
public void SetAWeakReferenceToNoneWhenItsTargetIsRemoved()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-forget", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        var low = new Album(bowie) { Title = "Low", Year = 1977 };
        new Album(bowie) { Title = "Heroes", Year = 1977, Previous = low };
        transaction.Commit();
    }
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var heroes = AlbumCollection.Single(static a => a.Title == "Heroes");
        Assert.Equal("Low", heroes.Previous.Get().Title);

        // A weak reference does not protect its target: the removal is allowed, and forgets the reference at once.
        heroes.Previous.Get().Remove();
        Assert.False(heroes.Previous.Any());
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var heroes = AlbumCollection.Single();
        Assert.False(heroes.Previous.Any());
    }
}
```

A strong reference and a weak one, side by side: the strong one refuses the removal, the weak one forgets its target.

```csharp
[Fact]
public void CompareAStrongAndAWeakReference()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-compare", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = new Album(bowie) { Title = "Low" };
    var heroes = new Album(bowie) { Title = "Heroes", Previous = low };

    // The album retains its artist: refused. The previous album does not retain anything: removed, and forgotten.
    Assert.Throws<InvalidOperationException>(bowie.Remove);
    low.Remove();
    Assert.Equal(Option.None, heroes.Previous);
    transaction.Commit();
}
```

> **Warning: A weak link is an Option**
>
> A weak reference may become empty at any removal of its target: it is always an `Option<T>`. `[Weak]` on a `T` stops the build, with the error **GBO017**.

## Part 2: Aggregates

### Lesson 4: Parts are born in their Owner

A link with neither `[Strong]` nor `[Weak]` is a **composition**: the Owner holds its parts, as an order holds its lines. An artist owns its albums; an album owns its tracks, its release and its booklet.

```csharp
[Entity(1, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MArtist
{
    [Property(1)]
    string Name { get; set; }

    // A collection of parts: IEntityCollection<T>, without Setter. Its parts are born in it.
    [Property(2)]
    IEntityCollection<MAlbum> Albums { get; }
}

[Entity(2, 1, 6)]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    int Year { get; set; }

    // A required composition, a T: the release of the album is born with it, and never leaves it.
    [Property(3)]
    MRelease Release { get; }

    [Property(4)]
    IEntityCollection<MTrack> Tracks { get; }

    // An optional composition, an Option<T>: a slot, that holds a booklet or none.
    [Property(5)]
    Option<MBooklet> Booklet { get; }

    // A second collection of the same parts: the tracks put forward (Lesson 6).
    [Property(6)]
    IEntityCollection<MTrack> Highlights { get; }

    // [OwnedFrom]: the Owner of the part, read, never set. It takes no [Property(n)]: the composition that holds the
    // part gives it.
    [OwnedFrom]
    MArtist Artist { get; }
}

[Entity(3, 1, 2)]
[Enumerable]
internal interface MRelease
{
    [Property(1)]
    string Label { get; set; }

    [Property(2)]
    string CatalogNumber { get; set; }
}

[Entity(4, 1, 2)]
[Enumerable]
internal interface MTrack
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    int Seconds { get; set; }

    [OwnedFrom]
    MAlbum Album { get; }
}

[Entity(5, 1, 1)]
[Enumerable]
internal interface MBooklet
{
    [Property(1)]
    int Pages { get; set; }
}
```

A part is an entity that is not Root, and that a composition of its Owner holds. Each form of composition has its own form of birth:

| Composition                                 | Holds                 | A part is born                                      |
| ------------------------------------------- | --------------------- | --------------------------------------------------- |
| `IEntityCollection<MTrack> Tracks { get; }` | Any number of parts   | By `album.Tracks.New()`                             |
| `Option<MBooklet> Booklet { get; }`         | One part, or none: a slot | By `album.Booklet.New()`                        |
| `MRelease Release { get; }`                 | Exactly one part      | With its Owner, by the constructor of the album     |
| `[OwnedFrom] MArtist Artist { get; }`       | (The Owner of the part) | Read, never set                                   |

A part has no public constructor: born out of any Owner, nothing would hold it, and GBO would remove it at once. So `new Album()` does not compile: an album is born in its artist.

```csharp
[Fact]
public void CreateAnAggregate()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-aggregate", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        var low = bowie.Albums.New();
        low.Title = "Low";
        low.Year = 1977;

        // The release, a required part, was born with the album.
        low.Release.Label = "RCA";
        low.Release.CatalogNumber = "PL 12030";

        var speed = low.Tracks.New();
        (speed.Title, speed.Seconds) = ("Speed of Life", 166);
        var sound = low.Tracks.New();
        (sound.Title, sound.Seconds) = ("Sound and Vision", 183);

        // Each part reads its Owner.
        Assert.Same(low, speed.Album);
        Assert.Same(bowie, speed.Album.Artist);
        Assert.Equal((1, 2), (bowie.Albums.Count(), low.Tracks.Count()));
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var low = ArtistCollection.Single().Albums.Single();
        Assert.Equal(("Low", "RCA", 349), (low.Title, low.Release.Label, low.Tracks.Sum(static t => t.Seconds)));
    }
}
```

A slot holds one part, or none: `New` fills it, and refuses a slot already full.

```csharp
[Fact]
public void FillTheSlotOfAnOptionalPart()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-slot", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var low = new Artist { Name = "David Bowie" }.Albums.New();

    Assert.False(low.Booklet.Any());
    low.Booklet.New().Pages = 12;
    Assert.Equal(12, low.Booklet.Get().Pages);

    // The slot holds a booklet already: Remove() it first (Lesson 5).
    Assert.Throws<InvalidOperationException>(() => low.Booklet.New());
    transaction.Commit();
}
```

The Views of an aggregate read its parts, and their Owners, without creating any Body:

```csharp
[Fact]
public void ReadAnAggregateWithViews()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-views", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var low = new Artist { Name = "David Bowie" }.Albums.New();
        low.Title = "Low";
        for (int i = 1; i <= 11; i++)
            low.Tracks.New().Seconds = 180;
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var artist = ArtistViewCollection.Single();
        var low = artist.Albums.Single();
        Assert.Equal(("Low", 11, 1980), (low.Title, low.Tracks.Count(), low.Tracks.Sum(static t => t.Seconds)));
        Assert.Equal("David Bowie", low.Artist.Name);
    }
}
```

> **Note: An aggregate**
>
> An Owner and its parts are one **aggregate**, in the sense of Domain-Driven Design: they are created, changed, removed and committed together. Lesson 18 shows that an aggregate is also a unit of conflict between concurrent transactions.

### Lesson 5: Removing an Owner removes its parts

A part lives in its Owner. Taken out of its composition, by `Remove`, it is removed: there is no part without an Owner (Lesson 10 shows the exception). The Body of a removed entity still reads its last values; writing to it throws.

```csharp
[Fact]
public void RemoveAPartFromItsOwner()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-part", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var low = new Artist { Name = "David Bowie" }.Albums.New();
    var speed = low.Tracks.New();
    speed.Title = "Speed of Life";
    low.Tracks.New().Title = "Breaking Glass";
    low.Booklet.New();

    Assert.True(low.Tracks.Remove(speed));
    Assert.True(low.Booklet.Remove());
    Assert.Equal((1, 0), (TrackCollection.Count(), BookletCollection.Count()));

    // The removed track still reads its last values; writing to it throws.
    Assert.Equal("Speed of Life", speed.Title);
    Assert.Throws<InvalidOperationException>(() => speed.Title = "Speed");
    transaction.Commit();
}
```

Its Owner removed, a part is removed with it, and its own parts, **at the same instruction**: the cascade of `ON DELETE CASCADE` in SQL.

```csharp
[Fact]
public void RemoveAWholeAggregate()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-cascade", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        foreach (string title in new[] { "Low", "Heroes", "Lodger" })
        {
            var album = bowie.Albums.New();
            album.Title = title;
            album.Booklet.New();
            for (int i = 0; i < 10; i++)
                album.Tracks.New();
        }
        new Artist { Name = "Brian Eno" }.Albums.New().Tracks.New();
        transaction.Commit();
    }
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        ArtistCollection.Single(static a => a.Name == "David Bowie").Remove();

        // The cascade is done when Remove() returns: only the aggregate of Brian Eno remains.
        Assert.Equal((1, 1, 1, 1, 0), (ArtistCollection.Count(), AlbumCollection.Count(), ReleaseCollection.Count(),
            TrackCollection.Count(), BookletCollection.Count()));
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal((1, 1), (AlbumCollection.Count(), TrackCollection.Count()));
}
```

> **Note: Nothing passes later**
>
> There is no collector that runs some time after: when `Remove()` returns, the whole aggregate is gone, from every collection and every query of the transaction.

### Lesson 6: Move a part

A part has one Owner, but may be in several compositions of it: a track in the `Tracks` of its album, and in its `Highlights`. `Add` places it there, and returns `false` when it is there already; `Remove` takes it out, and removes it only when it leaves the last composition of its Owner.

```csharp
[Fact]
public void PutAPartInTwoCompositions()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-two", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var low = new Artist { Name = "David Bowie" }.Albums.New();
    var sound = low.Tracks.New();
    sound.Title = "Sound and Vision";

    Assert.True(low.Highlights.Add(sound));
    Assert.False(low.Highlights.Add(sound));

    // Out of the highlights, the track is still in the tracks: it lives.
    low.Highlights.Remove(sound);
    Assert.Equal((1, 0, 1), (low.Tracks.Count(), low.Highlights.Count(), TrackCollection.Count()));
    transaction.Commit();
}
```

`MoveTo` gives a part to another Owner, in one operation: it leaves every composition of its old Owner, and enters the new one. It is never without an Owner, not even between two instructions. `Add` refuses a part of another Owner: moving it is the work of `MoveTo`.

```csharp
[Fact]
public void MoveAPartToAnotherOwner()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-move", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var heroes = bowie.Albums.New();
    var track = low.Tracks.New();
    low.Highlights.Add(track);

    track.MoveTo(heroes.Tracks);
    Assert.Same(heroes, track.Album);
    Assert.Equal((0, 0, 1), (low.Tracks.Count(), low.Highlights.Count(), heroes.Tracks.Count()));

    // A whole album, with its parts, to another artist.
    var eno = new Artist { Name = "Brian Eno" };
    heroes.MoveTo(eno.Albums);
    Assert.Same(eno, track.Album.Artist);

    // Add refuses a part of another Owner: MoveTo moves it.
    Assert.Throws<InvalidOperationException>(() => low.Tracks.Add(track));
    transaction.Commit();
}
```

### Lesson 7: Build a tree

A part may have several types of Owner. The library of a listener holds folders; a folder holds playlists, and other folders. A folder lives in a listener, at the top of the library, or in another folder:

```csharp
[Entity(1, 1, 2)]
[Root]
[Removable]
internal interface MListener
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MFolder> Folders { get; }
}

[Entity(2, 1, 3)]
[Enumerable]
internal interface MFolder
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MFolder> Folders { get; }

    [Property(3)]
    IEntityCollection<MPlaylist> Playlists { get; }

    // Two types compose a folder: each [OwnedFrom] views the Owner of one type, an Option. Exactly one is present.
    [OwnedFrom]
    Option<MListener> Listener { get; }

    [OwnedFrom]
    Option<MFolder> Parent { get; }
}

[Entity(3, 1, 1)]
[Enumerable]
internal interface MPlaylist
{
    [Property(1)]
    string Name { get; set; }

    [OwnedFrom]
    MFolder Folder { get; }
}
```

The model says it by the compositions themselves: `MListener.Folders` and `MFolder.Folders`. The folder reads its Owner by an `[OwnedFrom] Option<T>` for each type, exactly one of which is present. A chain of Owners always leads to a Root: the generator checks it, with the error **GBO006** otherwise.

```csharp
[Fact]
public void BuildATreeOfFolders()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-tree", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Listener { Name = "Ada" };
    var music = ada.Folders.New();
    music.Name = "Music";
    var jazz = music.Folders.New();
    jazz.Name = "Jazz";
    jazz.Playlists.New().Name = "Late night";

    Assert.Same(ada, music.Listener.Get());
    Assert.False(music.Parent.Any());
    Assert.Same(music, jazz.Parent.Get());
    Assert.False(jazz.Listener.Any());
    Assert.Equal("Jazz", jazz.Playlists.Single().Folder.Name);
    transaction.Commit();
}
```

A tree has no cycle: `MoveTo` refuses to place a folder under itself, or under one of its subfolders, before it changes anything. The removal of a folder removes its whole subtree.

```csharp
[Fact]
public void MoveAndRemoveASubtree()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-move", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Listener { Name = "Ada" };
    var music = ada.Folders.New();
    var jazz = music.Folders.New();
    var bebop = jazz.Folders.New();
    bebop.Playlists.New();
    var archive = ada.Folders.New();

    // Under its own subfolder: a cycle, refused before anything changes.
    Assert.Throws<InvalidOperationException>(() => music.MoveTo(bebop.Folders));
    Assert.Same(ada, music.Listener.Get());

    // From the top of the library into a folder: its Owner changes of type.
    music.MoveTo(archive.Folders);
    Assert.Same(archive, music.Parent.Get());
    Assert.False(music.Listener.Any());

    // The archive removed, with its whole subtree.
    ada.Folders.Remove(archive);
    Assert.Equal((0, 0), (FolderCollection.Count(), PlaylistCollection.Count()));
    transaction.Commit();
}
```

### Lesson 8: A collection is a set

A radio station and its broadcasts, as many as the years give:

```csharp
[Entity(1, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MStation
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MBroadcast> Broadcasts { get; }
}

[Entity(2, 1, 2)]
internal interface MBroadcast
{
    // The business order of the broadcasts is a property: the collection has none.
    [Property(1)]
    int Day { get; set; }

    [Property(2)]
    string Title { get; set; }

    [OwnedFrom]
    MStation Station { get; }
}
```

A collection is a set, not a list: it has no position, and holds a part once. It enumerates in the order that is the fastest to read, which GBO chooses: not the order of creation, nor any order your code may rely on. The business order is a property, read by `OrderBy`.

```csharp
[Fact]
public void OrderACollectionByAProperty()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-order", StoreAccessMode.ReadWrite);
    using var radio = new RadioRepository(store);
    using var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite);
    var station = new Station { Name = "Jazz FM" };
    foreach (int day in new[] { 3, 1, 2 })
    {
        var broadcast = station.Broadcasts.New();
        (broadcast.Day, broadcast.Title) = (day, $"Day {day}");
    }

    Assert.Equal(["Day 1", "Day 2", "Day 3"], station.Broadcasts.OrderBy(static b => b.Day).Select(static b => b.Title).ToList());
    transaction.Commit();
}
```

A collection has no limit of size, and the same code holds three parts or a hundred thousand: `Count()` stays exact and immediate, and `Contains` finds a part without reading the others.

```csharp
[Fact]
public void HoldAsManyPartsAsNeeded()
{
    const int count = 100_000;
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-large", StoreAccessMode.ReadWrite);
    using var radio = new RadioRepository(store);
    using (var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var station = new Station { Name = "Jazz FM" };
        for (int day = 0; day < count; day++)
            station.Broadcasts.New().Day = day;
        var middle = station.Broadcasts.Single(static b => b.Day == count / 2);
        Assert.Equal(count, station.Broadcasts.Count());
        Assert.True(station.Broadcasts.Contains(middle));
        transaction.Commit();
    }
    using (var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var station = StationCollection.Single();
        Assert.Equal(count, station.Broadcasts.Count());
        Assert.Equal((long)count * (count - 1) / 2, station.Broadcasts.Sum(static b => (long)b.Day));

        // A removal in the middle: the count follows at once.
        station.Broadcasts.Remove(station.Broadcasts.Single(static b => b.Day == count / 2));
        Assert.Equal(count - 1, station.Broadcasts.Count());
        transaction.Commit();
    }
}
```

> **Tip: Never rely on the order of a collection**
>
> A `List<T>` keeps the order of its `Add`; a collection of GBO does not. When the order matters, it is a property of your model: a day, a position, a date. Then `OrderBy` gives it, whatever the collection.

## Part 3: Retention across aggregates

### Lesson 9: A reference to a part retains its chain

A playlist does not own the tracks it plays: they belong to the albums of their artists. Each entry of a playlist designates its track by a strong reference: it retains it. A remix designates its original track, on the same album or on another one. The artist and the album are those of Lesson 4, with fewer properties:

```csharp
[Entity(3, 1, 2)]
[Enumerable]
internal interface MTrack
{
    [Property(1)]
    string Title { get; set; }

    // A remix designates its original track, on the same album or on another one.
    [Property(2)]
    [Strong]
    Option<MTrack> RemixOf { get; set; }

    [OwnedFrom]
    MAlbum Album { get; }
}

[Entity(4, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MPlaylist
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MEntry> Entries { get; }
}

[Entity(5, 1, 1)]
[Enumerable]
internal interface MEntry
{
    [Property(1)]
    [Strong]
    MTrack Track { get; set; }
}
```

The track cannot be removed. Nor can its album, nor its artist: their removal would take the track with them, by the cascade of Lesson 5. A part requires its Owner, and its Owner requires its own: **retaining a track retains the chain of its Owners, up to the Root.** Each removal is refused at its call, by an exception that names the chain, from the removed entity to the retained one; nothing changes.

```csharp
[Fact]
public void RefuseTheRemovalOfTheChainOfARetainedTrack()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-chain", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var sound = low.Tracks.New();
    var favorites = new Playlist { Name = "Favorites" };
    var entry = favorites.Entries.New(sound);

    // The exception names the chain: Artist → Album → Track, retained by the entry.
    Assert.Contains("Artist 1 → Album 1 → Track 1", Assert.Throws<InvalidOperationException>(bowie.Remove).Message);
    Assert.Throws<InvalidOperationException>(() => bowie.Albums.Remove(low));
    Assert.Throws<InvalidOperationException>(() => low.Tracks.Remove(sound));
    Assert.Null(transaction.CondemnedBy);

    // The entry removed, nothing retains the track: the whole aggregate goes.
    favorites.Entries.Remove(entry);
    bowie.Remove();
    Assert.Equal((0, 0), (AlbumCollection.Count(), TrackCollection.Count()));
    transaction.Commit();
}
```

A strong reference from the removed aggregate itself retains nothing: an album whose remix designates its original track, on the same album, is removed with both. And `MoveTo` changes the Owner of a retained track: it never has to be without one.

```csharp
[Fact]
public void RetainNothingFromInsideAndMoveTheRetainedTrack()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-inside", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var original = low.Tracks.New();
    low.Tracks.New().RemixOf = original;

    // The remix and its original leave together: a reference from the removed album retains nothing.
    bowie.Albums.Remove(low);
    Assert.Equal(0, TrackCollection.Count());

    // A retained track can still change of album: it keeps an Owner, and the album it left can go.
    var heroes = bowie.Albums.New();
    var best = bowie.Albums.New();
    var track = heroes.Tracks.New();
    new Playlist { Name = "Favorites" }.Entries.New(track);
    track.MoveTo(best.Tracks);
    bowie.Albums.Remove(heroes);
    Assert.Same(best, track.Album);
    transaction.Commit();
}
```

### Lesson 10: An orphan survives its Owner

In Lesson 9, a retained track refused the removal of its artist. Declared `[Orphanable]`, an album may survive its Owner instead. Its `[OwnedFrom]` is then an `Option<T>`: an orphan has no Owner.

```csharp
[Entity(2, 1, 2)]
[Orphanable]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    IEntityCollection<MTrack> Tracks { get; }

    [OwnedFrom]
    Option<MArtist> Artist { get; }
}
```

Removing the artist removes the albums that nothing retains. Each album that a playlist plays survives, whole, with all its tracks, without an artist: it is an **orphan**. The history stays readable: the playlist still plays the album, its other tracks and its title.

```csharp
[Fact]
public void KeepTheRetainedAlbumsOfARemovedArtist()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-orphan", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        var low = bowie.Albums.New();
        low.Title = "Low";
        var sound = low.Tracks.New();
        low.Tracks.New();
        bowie.Albums.New().Title = "Lodger";
        new Playlist { Name = "Favorites" }.Entries.New(sound);

        bowie.Remove();

        // Low survives, with its two tracks, and no artist; Lodger is gone.
        var orphan = AlbumCollection.Single();
        Assert.Equal(("Low", 2), (orphan.Title, orphan.Tracks.Count()));
        Assert.False(orphan.Artist.Any());
        transaction.Commit();
    }
    using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var entry = PlaylistCollection.Single().Entries.Single();
        Assert.Equal("Low", entry.Track.Album.Title);
    }
}
```

An orphan lives while something retains it. At the instruction that removes its last retention, the last entry of a playlist that designates one of its tracks, it is removed, with its subtree.

```csharp
[Fact]
public void RemoveAnOrphanWithItsLastRetention()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-collect", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var (sound, speed) = (low.Tracks.New(), low.Tracks.New());
    var favorites = new Playlist { Name = "Favorites" };
    var first = favorites.Entries.New(sound);
    var second = favorites.Entries.New(speed);
    bowie.Remove();

    // One retention left: the orphan lives.
    favorites.Entries.Remove(first);
    Assert.Equal(1, AlbumCollection.Count());

    // The last one: the orphan and its tracks are removed by this very instruction.
    favorites.Entries.Remove(second);
    Assert.Equal((0, 0), (AlbumCollection.Count(), TrackCollection.Count()));
    transaction.Commit();
}
```

Taken out of its artist while retained, an `[Orphanable]` album becomes an orphan instead of being removed. `Add` places an orphan under another Owner: held again, its retention no longer matters.

```csharp
[Fact]
public void PlaceAnOrphanUnderAnotherArtist()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-adopt", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var favorites = new Playlist { Name = "Favorites" };
    var entry = favorites.Entries.New(low.Tracks.New());

    // Taken out of its artist while retained: an [Orphanable] album becomes an orphan, instead of being removed.
    bowie.Albums.Remove(low);
    Assert.False(low.Artist.Any());

    var eno = new Artist { Name = "Brian Eno" };
    eno.Albums.Add(low);
    Assert.Same(eno, low.Artist.Get());

    // Its Owner holds it now: the end of its retention removes nothing.
    favorites.Entries.Remove(entry);
    Assert.Equal(1, AlbumCollection.Count());
    transaction.Commit();
}
```

> **Note: A deterministic garbage collection**
>
> The garbage collector of .NET frees an object some time after its last reference, when it decides to. GBO removes an entity at the instruction that releases its last retention, as Rust drops a value at the end of its last owner. The moment of each death is known, written in your code, and tested as any other line.

## Part 4: Shared objects

### Lesson 11: A shared object lives with its references

A recording is neither a Root nor a part: no composition holds it, and nothing removes it explicitly. The tracks of an album designate it, and so do the tracks of the compilations that take it again. It is a **shared object**: a type that is not Root, that no composition holds, and that a strong reference designates.

```csharp
[Entity(3, 1, 2)]
internal interface MTrack
{
    [Property(1)]
    string Title { get; set; }

    // A strong reference to a type that no composition holds, and that is not Root: a shared object. Required, a T.
    [Property(2)]
    [Strong]
    MRecording Recording { get; set; }

    [OwnedFrom]
    MAlbum Album { get; }
}

[Entity(4, 1, 2)]
[Enumerable]
internal interface MRecording
{
    [Property(1)]
    int Seconds { get; set; }

    // A shared object may designate another one: an Option, a strong slot (Lesson 12).
    [Property(2)]
    [Strong]
    Option<MLyrics> Lyrics { get; set; }
}

[Entity(5, 1, 1)]
[Enumerable]
internal interface MLyrics
{
    [Property(1)]
    string Text { get; set; }
}
```

A shared object is retained by the strong references that designate it. It is born by its first strong reference, and removed at the instruction that releases its last one. As a part, it has no public constructor: born out of any reference, nothing would retain it.

The track requires its recording, a `T`: the birth of the track takes a descriptor of it, `Recording.Init`. A new descriptor creates a new recording; an existing recording converts to its descriptor, and is designated.

```csharp
[Fact]
public void ShareARecordingBetweenTwoAlbums()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-share", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    var best = bowie.Albums.New();

    // A new recording, born with the track: Recording.Init describes it.
    var original = low.Tracks.New(new Recording.Init());
    original.Recording.Seconds = 183;

    // The compilation takes the same recording again: an existing recording converts to its descriptor.
    var again = best.Tracks.New(original.Recording);
    Assert.Same(original.Recording, again.Recording);
    Assert.Equal(1, RecordingCollection.Count());

    // The original album removed: the compilation still retains the recording.
    bowie.Albums.Remove(low);
    Assert.Equal(183, again.Recording.Seconds);

    // The compilation removed: nothing retains the recording, removed by the same instruction.
    bowie.Albums.Remove(best);
    Assert.Equal(0, RecordingCollection.Count());
    transaction.Commit();
}
```

For a required strong reference to a shared object, the generator writes a method that creates a new object and designates it at once: `NewRecording()`. The old recording is released, and removed when it was its last reference.

```csharp
[Fact]
public void ReleaseARecordingByASetter()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-release", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var low = new Artist { Name = "David Bowie" }.Albums.New();
    var track = low.Tracks.New(new Recording.Init());
    var old = track.Recording;

    // NewRecording, written by the generator for a required strong reference to a shared object: a new recording,
    // designated at once; the old one, released, is removed.
    var remaster = track.NewRecording();
    Assert.Same(remaster, track.Recording);
    Assert.Equal(1, RecordingCollection.Count());
    Assert.Throws<InvalidOperationException>(() => old.Seconds = 1);
    transaction.Commit();
}
```

> **Tip: Reference counting, exact and immediate**
>
> A shared object of GBO behaves as an `Rc<T>` of Rust, or a COM object: it lives as long as its strong references. Unlike a reference count in your own code, GBO keeps it right whatever happens: a Setter, a removal, a cascade, a rollback.

### Lesson 12: Take the new retention first

An optional strong reference to a shared object, `[Strong] Option<MLyrics> Lyrics`, is a **strong slot**:

| Operation   | Effect                                                   |
| ----------- | -------------------------------------------------------- |
| `New()`     | Creates the object, and designates it                    |
| `Set(x)`    | Designates an existing object, and retains it           |
| `Clear()`   | Releases the object: removed when it was its last retention |

```csharp
[Fact]
public void ShareLyricsThroughAStrongSlot()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-slot", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var album = new Artist { Name = "David Bowie" }.Albums.New();
    var original = album.Tracks.New(new Recording.Init()).Recording;
    var cover = album.Tracks.New(new Recording.Init()).Recording;

    original.Lyrics.New().Text = "I will be king";
    cover.Lyrics.Set(original.Lyrics.Get());
    Assert.Same(original.Lyrics.Get(), cover.Lyrics.Get());

    original.Lyrics.Clear();
    Assert.Equal(1, LyricsCollection.Count());
    cover.Lyrics.Clear();
    Assert.Equal(0, LyricsCollection.Count());
    transaction.Commit();
}
```

Every release removes the object when it was its last retention, at that instruction. So the order of the instructions matters. To pass a shared object from one holder to another, take the new retention before releasing the old one:

```csharp
[Fact]
public void TakeTheNewRetentionBeforeReleasingTheOldOne()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-order", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var album = new Artist { Name = "David Bowie" }.Albums.New();
    var demo = album.Tracks.New(new Recording.Init()).Recording;
    var final = album.Tracks.New(new Recording.Init()).Recording;
    var lyrics = demo.Lyrics.New();

    final.Lyrics.Set(lyrics);
    demo.Lyrics.Clear();
    Assert.Same(lyrics, final.Lyrics.Get());
    transaction.Commit();
}
```

Released first, the object is removed, and the new reference to it is refused: there is nothing left to designate.

```csharp
[Fact]
public void LoseASharedObjectReleasedFirst()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-lost", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var album = new Artist { Name = "David Bowie" }.Albums.New();
    var demo = album.Tracks.New(new Recording.Init()).Recording;
    var final = album.Tracks.New(new Recording.Init()).Recording;
    var lyrics = demo.Lyrics.New();

    demo.Lyrics.Clear();
    Assert.Contains("removed", Assert.Throws<InvalidOperationException>(() => final.Lyrics.Set(lyrics)).Message);
    Assert.False(final.Lyrics.Any());
    transaction.Commit();
}
```

> **Warning: Retain, then release**
>
> As you clone an `Rc` in Rust before you drop the first one, take the new retention of a shared object before you release the old one. The other order removes it.

Each kind of entity has its own birth:

| Entity        | Lives                                         | Is born                                                                        |
| ------------- | --------------------------------------------- | ------------------------------------------------------------------------------ |
| Root          | Until its `Remove()`, if it is `[Removable]`  | By its constructor: `new Artist { ... }`                                       |
| Part          | While its Owner holds it                      | In its composition: `artist.Albums.New()`; a required part, with its Owner     |
| Shared object | While a strong reference designates it        | By its first strong reference: `new Recording.Init()`, `NewRecording()`, `Lyrics.New()` |

### Lesson 13: A collection of strong references

A festival books artists, and shows posters, which the editions of the next years may show again:

```csharp
[Entity(2, 1, 3)]
[Root]
[Removable]
internal interface MFestival
{
    [Property(1)]
    string Name { get; set; }

    // A collection of strong references to Roots: each booked artist is retained.
    [Property(2)]
    [Strong]
    IEntityCollection<MArtist> Lineup { get; }

    // A collection of strong references to shared objects: each poster lives while a festival shows it.
    [Property(3)]
    [Strong]
    IEntityCollection<MPoster> Posters { get; }
}

[Entity(3, 1, 1)]
[Enumerable]
internal interface MPoster
{
    [Property(1)]
    string Design { get; set; }
}
```

`[Strong] IEntityCollection<T>` is a collection of references, not of parts: it designates entities that it does not own. Each of them is retained, as by a strong reference of Lesson 2: a booked artist cannot be removed. `Add` retains, `Remove` releases.

```csharp
[Fact]
public void RetainEveryArtistOfALineup()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-lineup", StoreAccessMode.ReadWrite);
    using var festivals = new FestivalRepository(store);
    using var transaction = festivals.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "David Bowie" };
    var eno = new Artist { Name = "Brian Eno" };
    var montreux = new Festival { Name = "Montreux" };
    montreux.Lineup.Add(bowie);
    montreux.Lineup.Add(eno);

    Assert.Throws<InvalidOperationException>(bowie.Remove);
    montreux.Lineup.Remove(bowie);
    bowie.Remove();

    // The removal of the festival releases its whole lineup.
    montreux.Remove();
    eno.Remove();
    Assert.Equal(0, ArtistCollection.Count());
    transaction.Commit();
}
```

A collection of strong references to shared objects also has a `New`, and each object lives while a collection, or a reference, retains it.

```csharp
[Fact]
public void SharePostersBetweenEditions()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-posters", StoreAccessMode.ReadWrite);
    using var festivals = new FestivalRepository(store);
    using var transaction = festivals.OpenTransaction(StoreAccessMode.ReadWrite);
    var first = new Festival { Name = "Montreux 1977" };
    var poster = first.Posters.New();
    poster.Design = "Blue lake";
    var second = new Festival { Name = "Montreux 1978" };
    second.Posters.Add(poster);

    first.Remove();
    Assert.Equal(1, PosterCollection.Count());
    second.Posters.Remove(poster);
    Assert.Equal(0, PosterCollection.Count());
    transaction.Commit();
}
```

A collection of references is a set, as the collections of parts (Lesson 8): an entity is in it once, and any number of them fit.

## Part 5: Weak links and reverse properties

### Lesson 14: A weak collection forgets its targets

The listeners, their favorite albums, and the album they play now:

```csharp
[Entity(3, 1, 3)]
[Root]
[Removable]
[Enumerable]
internal interface MListener
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    [Weak]
    Option<MAlbum> NowPlaying { get; set; }

    // A collection of weak references: it holds albums, and retains none.
    [Property(3)]
    [Weak]
    IEntityCollection<MAlbum> Favorites { get; }
}
```

The favorites of a listener are a `[Weak] IEntityCollection<MAlbum>`: a collection of references that retains nothing. An album in the favorites of a million listeners can still be removed. Its removal takes it out of every favorites, at that instruction, as it sets every weak reference to it to none (Lesson 3). The count of each collection stays exact: a collection never holds a removed entity.

```csharp
[Fact]
public void TakeARemovedAlbumOutOfEveryFavorites()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-favorites", StoreAccessMode.ReadWrite);
    using var audience = new AudienceRepository(store);
    using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var low = new Album { Title = "Low" };
        var heroes = new Album { Title = "Heroes" };
        for (int i = 0; i < 100; i++)
        {
            var listener = new Listener { Name = $"Listener {i}", NowPlaying = low };
            listener.Favorites.Add(low);
            listener.Favorites.Add(heroes);
        }
        transaction.Commit();
    }
    using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        AlbumCollection.Single(static a => a.Title == "Low").Remove();
        Assert.All(ListenerCollection, static listener =>
        {
            Assert.Equal(["Heroes"], listener.Favorites.Select(static a => a.Title).ToList());
            Assert.False(listener.NowPlaying.Any());
        });
        transaction.Commit();
    }
    using (audience.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(100, ListenerCollection.Count(static l => l.Favorites.Count() == 1));
}
```

### Lesson 15: Who points to me?

A link reads in one direction: from the listener to its favorite albums. The other direction, the listeners of an album, is a **reverse property**, declared on the album, without `[Property(n)]` nor Setter:

```csharp
[Entity(2, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    [Strong]
    Option<MGenre> Genre { get; set; }

    // The listeners whose favorites hold this album (Lesson 15).
    [ReverseWeakReference(nameof(MListener.Favorites))]
    IEntityCollection<MListener> Fans { get; }
}

[Entity(1, 1, 1)]
[Root]
[Enumerable]
internal interface MGenre
{
    [Property(1)]
    string Name { get; set; }

    // The albums that designate this genre, by their strong reference Genre (Lesson 15).
    [ReverseStrongReference(nameof(MAlbum.Genre))]
    IEntityCollection<MAlbum> Albums { get; }
}
```

GBO keeps the referrers of each album up to date at each instruction: a Setter, an `Add`, a `Remove`, the removal of a listener. Reading them costs no search: no query over all the listeners, the set is there.

```csharp
[Fact]
public void ReadTheFansOfAnAlbum()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-fans", StoreAccessMode.ReadWrite);
    using var audience = new AudienceRepository(store);
    using var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite);
    var low = new Album { Title = "Low" };
    var (ada, bob, eve) = (new Listener { Name = "Ada" }, new Listener { Name = "Bob" }, new Listener { Name = "Eve" });
    ada.Favorites.Add(low);
    bob.Favorites.Add(low);
    Assert.Equal([ada, bob], low.Fans.ToList());

    eve.Favorites.Add(low);
    bob.Favorites.Remove(low);
    ada.Remove();
    Assert.Same(eve, low.Fans.Single());
    Assert.True(low.Fans.Contains(eve));
    transaction.Commit();
}
```

A reverse property of a strong reference reads the entities that retain this one, even for a Root that is not Removable, such as a genre: the albums of a genre. A Setter moves the album from the set of one genre to the set of the other. The Views read the reverse properties as well.

```csharp
[Fact]
public void ReadTheAlbumsOfAGenre()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-genre", StoreAccessMode.ReadWrite);
    using var audience = new AudienceRepository(store);
    using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var (rock, jazz) = (new Genre { Name = "Rock" }, new Genre { Name = "Jazz" });
        new Album { Title = "Low", Genre = rock };
        new Album { Title = "Heroes", Genre = rock };
        var kind = new Album { Title = "Kind of Blue", Genre = rock };

        // A Setter moves the album from the set of one genre to the set of the other.
        kind.Genre = jazz;
        Assert.Equal((2, 1), (rock.Albums.Count(), jazz.Albums.Count()));
        transaction.Commit();
    }
    using (audience.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var rock = GenreCollection.Single(static g => g.Name == "Rock");
        Assert.Equal(["Heroes", "Low"], rock.Albums.Select(static a => a.Title).OrderBy(static t => t).ToList());

        // The Views read the reverse properties as well, without any Body.
        var view = GenreViewCollection.Single(static g => g.Name == "Jazz");
        Assert.Equal("Kind of Blue", view.Albums.Single().Title);
    }
}
```

The generator checks the path of a reverse property: a property of the other type, of the same nature, that designates this one. Otherwise, the build stops with the error **GBO028**.

> **Tip: Mind the cost**
>
> The referrers of a weak reference are kept anyway: the removal of the target needs them, to forget the references. The referrers of a strong reference are kept only when a reverse property declares them, `[ReverseStrongReference]`: keeping them costs a write at each new reference. Declare the reverse properties that your code reads, and only those.

## Part 6: Your rules, and the concurrency

### Lesson 16: Your rules on the links

The partial methods of Tutorial One, `On...Changed`, exist for the links too: `On...Added` and `On...Removed` for a collection, called after a part joins it or leaves it; `On...Changed` for a reference and a slot. Each one runs after the change, once the operation of GBO is complete: it sees the state that the operation leaves.

Here, an album keeps its duration, the sum of the durations of its tracks; a note of an archive records each removed album:

```csharp
[Entity(2, 1, 3)]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    // The sum of the durations of the tracks, kept by the rules of the partial classes below.
    [Property(2)]
    int Seconds { get; set; }

    [Property(3)]
    IEntityCollection<MTrack> Tracks { get; }

    [OwnedFrom]
    MArtist Artist { get; }
}

[Entity(4, 1, 1)]
[Root]
[Removable]
[Enumerable]
internal interface MNote
{
    [Property(1)]
    string Text { get; set; }
}

// The rules of the album, in its partial class, as in Tutorial One, Lesson 11.
internal partial class Album
{
    /// <summary>The most tracks an album may hold: a rule of the label (Lesson 17).</summary>
    public const int MaxTracks = 3;

    /// <summary>Computes the duration of the album again, from its tracks.</summary>
    internal void Recount() => Seconds = Tracks.Sum(static t => t.Seconds);

    /// <summary>A new track, refused before anything is written when the album is full (Lesson 17).</summary>
    public Track AddTrack(string title, int seconds)
    {
        if (Tracks.Count() >= MaxTracks)
            throw new InvalidOperationException($"The album {Title} holds {MaxTracks} tracks at most.");
        var track = Tracks.New();
        (track.Title, track.Seconds) = (title, seconds);
        return track;
    }

    // For each collection, the generator declares two partial methods, called after a part joins it, or leaves it.
    partial void OnTracksAdded(Track item) => Recount();

    partial void OnTracksRemoved(Track item) => Recount();

    // OnDeleting: called once before the removal of the album, the album still readable, its artist as well.
    partial void OnDeleting() => new Note { Text = $"{Title}, by {Artist.Name}, removed" };
}

internal partial class Track
{
    partial void OnSecondsChanged() => Album.Recount();

    // A rule checked after the change: its exception condemns the transaction (Lesson 17).
    partial void OnTitleChanged()
    {
        if (Title.Length == 0)
            throw new InvalidOperationException("A track has a title.");
    }
}
```

The duration of the album follows its tracks, added, changed and removed, with no line of the test to keep it:

```csharp
[Fact]
public void KeepTheDurationOfAnAlbum()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-duration", StoreAccessMode.ReadWrite);
    using var label = new LabelRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var low = new Artist { Name = "David Bowie" }.Albums.New();
        low.Title = "Low";
        low.AddTrack("Speed of Life", 166);
        var sound = low.AddTrack("Sound and Vision", 183);
        Assert.Equal(349, low.Seconds);

        sound.Seconds = 185;
        Assert.Equal(351, low.Seconds);
        low.Tracks.Remove(sound);
        Assert.Equal(166, low.Seconds);
        transaction.Commit();
    }
    using (label.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(166, AlbumCollection.Single().Seconds);
}
```

`OnDeleting` runs once for each entity that a removal takes, from the Owners to their parts, after every check of the removal and before its writes: the entity, its parts and its Owners still read. It may create other entities, a note of an archive here; it cannot retain an entity that is being removed.

```csharp
[Fact]
public void ArchiveEveryRemovedAlbum()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-deleting", StoreAccessMode.ReadWrite);
    using var label = new LabelRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        bowie.Albums.New().Title = "Low";
        bowie.Albums.New().Title = "Heroes";
        transaction.Commit();
    }
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        ArtistCollection.Single().Remove();
        transaction.Commit();
    }
    using (label.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(0, AlbumCollection.Count());
        Assert.Equal(["Heroes, by David Bowie, removed", "Low, by David Bowie, removed"],
            NoteCollection.Select(static n => n.Text).OrderBy(static t => t).ToList());
    }
}
```

> **Note: GBO keeps the links, your code keeps your rules**
>
> GBO never calls your code to keep the links right: the counts, the cascades and the referrers are its own work, done whatever your code does. Your partial methods keep your rules: a duration, an archive, a limit.

### Lesson 17: Refused, or condemned

An operation can fail in two ways, and only one of them leaves the transaction usable.

**Refused.** Every operation of GBO, a Setter, a birth, `New`, `Add`, `Remove`, `MoveTo`, a cascade, checks all its refusals before its first write: a removal that a strong reference forbids, a slot already full, a removed target. The exception changes nothing: catch it, and the transaction goes on, and commits. Write your own rules the same way: check, then write, as `Album.AddTrack` does.

```csharp
[Fact]
public void GoOnAfterARefusal()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-refused", StoreAccessMode.ReadWrite);
    using var label = new LabelRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var low = new Artist { Name = "David Bowie" }.Albums.New();
        low.Title = "Low";
        for (int i = 1; i <= Album.MaxTracks; i++)
            low.AddTrack($"Track {i}", 180);

        // The rule of the label, checked before the write.
        Assert.Throws<InvalidOperationException>(() => low.AddTrack("One more", 180));

        // A refusal of GBO: a removed track refuses to be written.
        var removed = low.Tracks.First();
        low.Tracks.Remove(removed);
        Assert.Throws<InvalidOperationException>(() => removed.Seconds = 1);

        Assert.Null(transaction.CondemnedBy);
        transaction.Commit();
    }
    using (label.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(Album.MaxTracks - 1, TrackCollection.Count());
}
```

**Condemned.** An exception thrown after a write, by a rule of yours that runs after its change, leaves the changes that the write began, and that nothing undoes. Catching it would let the code commit a state that the rule forbids: so the exception **condemns** the transaction. It still reads, but refuses its writes and its commit; `CondemnedBy` gives the exception. Only a rollback is left.

```csharp
[Fact]
public void CondemnTheTransactionByAnExceptionAfterAWrite()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-condemned", StoreAccessMode.ReadWrite);
    using var label = new LabelRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var track = new Artist { Name = "David Bowie" }.Albums.New().Tracks.New();
        track.Title = "Speed of Life";

        // OnTitleChanged runs after the title is written, and throws: the empty title is written already.
        var thrown = Assert.Throws<InvalidOperationException>(() => track.Title = "");
        Assert.Same(thrown, transaction.CondemnedBy);

        // The transaction still reads; it refuses any write, and its commit.
        Assert.Equal("", track.Title);
        Assert.Throws<InvalidOperationException>(() => track.Seconds = 180);
        Assert.Throws<InvalidOperationException>(transaction.Commit);
    }

    // Nothing was committed.
    using (label.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(0, TrackCollection.Count());
}
```

> **Warning: Check, then write**
>
> A rule that may refuse belongs before the write: in a method of yours, as `AddTrack`, that throws before it changes anything. A rule in an `On...Changed` runs after the change: its exception condemns the transaction, even when your code catches it.

### Lesson 18: Links and concurrency

An aggregate is a unit of conflict: a part added or removed changes its Owner. Two transactions that change the parts of the same album conflict at the second commit, as two changes of one entity do (Tutorial One, Lesson 16): a rule of the album, checked by each of them, could not be broken by both together. The parts of two albums never conflict.

```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public async Task ConflictOnTheSameAggregate(bool sameAlbum)
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore($"lesson-18-aggregate-{sameAlbum}", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "David Bowie" };
        bowie.Albums.New().Title = "Low";
        bowie.Albums.New().Title = "Heroes";
        transaction.Commit();
    }
    var firstAdded = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();
    var first = Task.Run(async () =>
    {
        using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
        AlbumCollection.Single(static a => a.Title == "Low").Tracks.New().Title = "Speed of Life";
        firstAdded.SetResult();
        await secondCommitted.Task;
        if (sameAlbum)
            Assert.Throws<TransactionConflictException>(transaction.Commit);
        else
            transaction.Commit();
    });
    await firstAdded.Task;
    await Task.Run(() =>
    {
        using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
        AlbumCollection.Single(sameAlbum ? static a => a.Title == "Low" : static a => a.Title == "Heroes").Tracks.New().Title = "Breaking Glass";
        transaction.Commit();
    });
    secondCommitted.SetResult();
    await first;
    using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(sameAlbum ? 1 : 2, TrackCollection.Count());
}
```

A link never designates an entity that a concurrent transaction removed. A strong reference added to a track while another transaction removes it: both change the count of the strong references of the track, and the second commit throws, writing nothing. The application retries, and sees the track removed.

```csharp
[Fact]
public async Task NeverDesignateATargetRemovedConcurrently()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-18-removed", StoreAccessMode.ReadWrite);
    using var streaming = new StreamingRepository(store);
    using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Artist { Name = "David Bowie" }.Albums.New().Tracks.New().Title = "Sound and Vision";
        new Playlist { Name = "Favorites" };
        transaction.Commit();
    }
    var entryAdded = new TaskCompletionSource();
    var removalCommitted = new TaskCompletionSource();

    // The first transaction puts the track in the playlist: in its snapshot, the track exists.
    var first = Task.Run(async () =>
    {
        using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
        PlaylistCollection.Single().Entries.New(TrackCollection.Single());
        entryAdded.SetResult();
        await removalCommitted.Task;
        Assert.Throws<TransactionConflictException>(transaction.Commit);
    });

    // The second one removes the album, with its track, and commits first: nothing retained it in its snapshot.
    await entryAdded.Task;
    await Task.Run(() =>
    {
        using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
        ArtistCollection.Single().Remove();
        transaction.Commit();
    });
    removalCommitted.SetResult();
    await first;

    using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal((0, 0), (TrackCollection.Count(), EntryCollection.Count()));
}
```

## Part 7: The identities, and the whole picture

### Lesson 19: The identities of a graph

An entity with a business identity, `[WithIdentity]`, has a reference type that an event, a message or another system keeps: an `ArtistRef`, an `AlbumRef`, a `ReleaseRef`, a `TrackRef`.

```csharp
[Entity(1, 1, 2)]
[Root]
[Removable]
[WithIdentity]
internal interface MArtist
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MAlbum> Albums { get; }
}

[Entity(2, 1, 3)]
[WithIdentity]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    MRelease Release { get; }

    [Property(3)]
    IEntityCollection<MTrack> Tracks { get; }

    [OwnedFrom]
    MArtist Artist { get; }
}

[Entity(3, 1, 1)]
[WithIdentity]
internal interface MRelease
{
    [Property(1)]
    string Label { get; set; }
}

[Entity(4, 1, 1)]
[WithIdentity]
[Enumerable]
internal interface MTrack
{
    [Property(1)]
    string Title { get; set; }

    [OwnedFrom]
    MAlbum Album { get; }
}
```

Such an entity is born with its identity: every birth of its type takes it, first, and GBO never draws one. An identity forgotten does not compile.

| Entity        | Birth                                                  |
| ------------- | ------------------------------------------------------ |
| Root          | `new Artist(artistRef)`                                |
| Part          | `album.Tracks.New(trackRef)`                           |
| Required part | `new Release.Init(releaseRef)`, given to the birth of its Owner |

`ArtistRef.New()` draws a new identity, anywhere in your code, out of any transaction as well: your code decides where each identity comes from.

This is what an event-sourced application needs. A command draws every identity of the graph it creates, and writes them in its event; applying the event creates the graph with them:

```csharp
// The event of an album released: every identity of its graph, drawn by the command.
private sealed record AlbumReleased(ArtistRef Artist, string Name, AlbumRef Album, string Title, ReleaseRef Release, TrackRef[] Tracks);

// The application of the event: each birth takes its identity from it. The command and the Replay run this same code.
private static void Apply(AlbumReleased released)
{
    var artist = new Artist(released.Artist) { Name = released.Name };
    var album = artist.Albums.New(released.Album, new Release.Init(released.Release));
    album.Title = released.Title;
    foreach (var track in released.Tracks)
        album.Tracks.New(track);
}
```

Replayed, in a Replay transaction, the event creates the same graph, with the same identities:

```csharp
[Fact]
public void CreateTheSameGraphByACommandAndByAReplay()
{
    // The command draws the identities, out of any transaction.
    var released = new AlbumReleased(ArtistRef.New(), "David Bowie", AlbumRef.New(), "Low", ReleaseRef.New(), [TrackRef.New(), TrackRef.New()]);

    using var commandStore = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-command", StoreAccessMode.ReadWrite);
    using var command = new CatalogRepository(commandStore);
    using (var transaction = command.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Apply(released);
        transaction.Commit();
    }

    using var replayStore = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-replay", StoreAccessMode.ReadWrite);
    using var replay = new CatalogRepository(replayStore);
    using (var transaction = replay.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
    {
        Apply(released);
        transaction.Commit();
    }

    // In both Stores, each identity resolves to its entity, in the same place of the graph.
    foreach (var catalog in new[] { command, replay })
        using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
        {
            var album = released.Album.Resolve();
            Assert.Equal(("Low", released.Artist, released.Release), (album.Title, album.Artist.Ref, album.Release.Ref));
            Assert.Same(album, released.Tracks[1].Resolve().Album);
        }
}
```

In a Replay, every identity must come from the event: there, `ArtistRef.New()` throws. Everywhere else, it works.

```csharp
[Fact]
public void DrawNoIdentityInAReplay()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-draw", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (catalog.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
        Assert.Throws<InvalidOperationException>(() => ArtistRef.New());
    using (catalog.OpenTransaction(StoreAccessMode.ReadWrite))
        Assert.False(ArtistRef.New().IsNull);
    Assert.False(ArtistRef.New().IsNull);
}
```

A null identity, or one given twice, is refused before anything is born:

```csharp
[Fact]
public void RefuseANullOrARepeatedIdentity()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-refuse", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var artist = new Artist(ArtistRef.New()) { Name = "David Bowie" };
    var track = TrackRef.New();

    Assert.Throws<ArgumentException>(() => new Artist(default));
    Assert.Throws<ArgumentException>(() => new Release.Init(default));
    var album = artist.Albums.New(AlbumRef.New(), new Release.Init(ReleaseRef.New()));
    album.Tracks.New(track);
    Assert.Throws<InvalidOperationException>(() => album.Tracks.New(track));

    Assert.Equal((1, 1), (AlbumCollection.Count(), TrackCollection.Count()));
    Assert.Null(transaction.CondemnedBy);
    transaction.Commit();
}
```

> **Note: Commands and events**
>
> A command decides, an event records. The command draws the identities and checks the rules; the event carries the identities, and its application, the same code in the command and in the Replay, creates the same graph each time. Tutorial Nine builds on this.

### Lesson 20: What dies, and when

The last model holds every link of the tutorial in one catalog. Genres, which nothing removes; artists, their `[Orphanable]` albums and their tracks; recordings and lyrics, shared; the playlists that retain tracks; the favorites, which do not; the festivals that book artists.

```csharp
[Entity(1, 1, 1)]
[Root]
[Enumerable]
internal interface MGenre
{
    [Property(1)]
    string Name { get; set; }
}

[Entity(2, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MArtist
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MAlbum> Albums { get; }
}

[Entity(3, 1, 3)]
[Orphanable]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    [Strong]
    Option<MGenre> Genre { get; set; }

    [Property(3)]
    IEntityCollection<MTrack> Tracks { get; }

    [OwnedFrom]
    Option<MArtist> Artist { get; }
}

[Entity(4, 1, 2)]
[Enumerable]
internal interface MTrack
{
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    [Strong]
    MRecording Recording { get; set; }

    [OwnedFrom]
    MAlbum Album { get; }
}

[Entity(5, 1, 2)]
[Enumerable]
internal interface MRecording
{
    [Property(1)]
    int Seconds { get; set; }

    [Property(2)]
    [Strong]
    Option<MLyrics> Lyrics { get; set; }
}

[Entity(6, 1, 1)]
[Enumerable]
internal interface MLyrics
{
    [Property(1)]
    string Text { get; set; }
}

[Entity(7, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MPlaylist
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    IEntityCollection<MEntry> Entries { get; }
}

[Entity(8, 1, 1)]
internal interface MEntry
{
    [Property(1)]
    [Strong]
    MTrack Track { get; set; }
}

[Entity(9, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MListener
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    [Weak]
    IEntityCollection<MAlbum> Favorites { get; }
}

[Entity(10, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MFestival
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    [Strong]
    IEntityCollection<MArtist> Lineup { get; }
}
```

An entity lives while something holds it:

| Entity                            | Lives                                                                    | Is removed                                                             |
| --------------------------------- | ------------------------------------------------------------------------ | ---------------------------------------------------------------------- |
| A Root not Removable: a genre     | Always                                                                   | Never                                                                  |
| A Removable Root: an artist       | Until its `Remove()`                                                     | By `Remove()`, refused while a strong reference retains it, or one of its parts |
| A part: a track                   | While its Owner holds it                                                 | With its Owner, or taken out of its last composition                   |
| An orphanable part: an album      | While its Owner holds it, or a strong reference retains it or one of its parts | At the release of its last retention, once without Owner         |
| A shared object: a recording      | While a strong reference designates it                                   | At the release of its last strong reference                            |

GBO applies this rule at each instruction, to the entities that the instruction touches: it never walks the whole graph, and nothing passes later. When an instruction returns, what it released is gone, and what it refused is untouched. One catalog, and four instructions:

```csharp
[Fact]
public void FollowEveryRetentionOfACatalog()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-20-catalog", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    var rock = new Genre { Name = "Rock" };

    // David Bowie: Low, two tracks; Lodger, one track. The lyrics of "Sound and Vision", shared with a cover by Brian Eno.
    var bowie = new Artist { Name = "David Bowie" };
    var low = bowie.Albums.New();
    (low.Title, low.Genre) = ("Low", rock);
    var sound = low.Tracks.New(new Recording.Init());
    sound.Title = "Sound and Vision";
    var lyrics = sound.Recording.Lyrics.New();
    low.Tracks.New(new Recording.Init()).Title = "Speed of Life";
    var lodger = bowie.Albums.New();
    lodger.Title = "Lodger";
    lodger.Tracks.New(new Recording.Init()).Title = "Boys Keep Swinging";
    var eno = new Artist { Name = "Brian Eno" };
    var green = eno.Albums.New();
    green.Title = "Another Green World";
    green.Tracks.New(new Recording.Init()).Recording.Lyrics.Set(lyrics);

    // A playlist retains "Sound and Vision"; a listener likes Low and Lodger, weakly; a festival books Bowie.
    var favorites = new Playlist { Name = "Favorites" };
    var entry = favorites.Entries.New(sound);
    var ada = new Listener { Name = "Ada" };
    ada.Favorites.Add(low);
    ada.Favorites.Add(lodger);
    var montreux = new Festival { Name = "Montreux" };
    montreux.Lineup.Add(bowie);
    Assert.Equal((2, 3, 4, 4, 1), Census());

    // 1. Bowie is booked: his removal is refused, and nothing changes.
    Assert.Throws<InvalidOperationException>(bowie.Remove);
    Assert.Equal((2, 3, 4, 4, 1), Census());

    // 2. Out of the lineup, Bowie is removed. Lodger dies with its track and its recording, which nothing else plays; Low
    // survives, an orphan, whole, since the playlist plays one of its tracks; the lyrics live, through the cover; the
    // favorites of Ada lose Lodger.
    montreux.Lineup.Remove(bowie);
    bowie.Remove();
    Assert.Equal((1, 2, 3, 3, 1), Census());
    Assert.False(low.Artist.Any());
    Assert.Same(low, ada.Favorites.Single());

    // 3. The entry removed: the orphan dies, with its tracks and their recordings; the lyrics live still, through the cover.
    favorites.Entries.Remove(entry);
    Assert.Equal((1, 1, 1, 1, 1), Census());
    Assert.Equal(0, ada.Favorites.Count());

    // 4. Eno removed: his album, its track, its recording, and the lyrics, which nothing designates any more.
    eno.Remove();
    Assert.Equal((0, 0, 0, 0, 0), Census());

    // The genre is eternal; the playlist, the listener and the festival are Roots, untouched.
    Assert.Equal((1, 1, 1, 1), (GenreCollection.Count(), PlaylistCollection.Count(),
        ListenerCollection.Count(), FestivalCollection.Count()));
    transaction.Commit();
}

// The artists, the albums, the tracks, the recordings and the lyrics of the transaction.
private static (int Artists, int Albums, int Tracks, int Recordings, int Lyrics) Census()
    => (ArtistCollection.Count(), AlbumCollection.Count(), TrackCollection.Count(),
        RecordingCollection.Count(), LyricsCollection.Count());
```

A removal costs what it removes, an aggregate, never the size of the Store; and every refusal comes before any change.

## Part 8: Capabilities

### Lesson 21: Capabilities, the components and the systems

An artist may tour, be verified, retire. These are not values of every artist: they are **capabilities** that some artists have, each with values of its own, and that come and go. A **component** declares one: an `[Entity]` with `[ComponentFor(nameof(MArtist))]`, its interface starting with `C`. The reviews are a capability of the artists and of the albums: a component may repeat `[ComponentFor]`. A **system** joins an entity and its components: an interface `[SystemFor(nameof(MArtist))]`, its interface starting with `S`. [Tutorial Three](Three-Composition.md) is about them.

```csharp
[Entity(1, 1, 1)]
[Root]
[Removable]
[Enumerable]
internal interface MArtist
{
    [Property(1)]
    string Name { get; set; }
}

[Entity(2, 1, 1)]
[Root]
[Removable]
[Enumerable]
internal interface MAlbum
{
    [Property(1)]
    string Title { get; set; }
}

// A capability of an artist: a tour, and its concerts, parts of the component.
[Entity(3, 1, 1)]
[ComponentFor(nameof(MArtist))]
[Enumerable]
internal interface CTour
{
    [Property(1)]
    IEntityCollection<MConcert> Concerts { get; }
}

[Entity(4, 1, 2)]
[Enumerable]
internal interface MConcert
{
    [Property(1)]
    string City { get; set; }

    [Property(2)]
    DateTime Date { get; set; }

    [OwnedFrom]
    CTour Tour { get; }
}

[Entity(5, 1, 1)]
[ComponentFor(nameof(MArtist))]
internal interface CVerified
{
    [Property(1)]
    DateTime Since { get; set; }
}

[Entity(6, 1, 1)]
[ComponentFor(nameof(MArtist))]
internal interface CRetired
{
    [Property(1)]
    string Reason { get; set; }
}

// A capability of two entity types: what the listeners review.
[Entity(7, 1, 1)]
[ComponentFor(nameof(MArtist))]
[ComponentFor(nameof(MAlbum))]
[Enumerable]
internal interface CReviews
{
    [Property(1)]
    IEntityCollection<MReview> Items { get; }
}

[Entity(8, 1, 2)]
internal interface MReview
{
    [Property(1)]
    string Text { get; set; }

    [Property(2)]
    int Stars { get; set; }

    [OwnedFrom]
    CReviews Reviews { get; }
}

// The artists on tour, maybe verified, and not retired.
[SystemFor(nameof(MArtist))]
[ExcludeComponent(nameof(CRetired))]
[Enumerable]
internal interface SOnTour
{
    MArtist Artist { get; }

    CTour Tour { get; }

    Option<CVerified> Verified { get; }
}
```

`CTour` gives the Body `Tour`; `SOnTour` gives the struct `OnTourSystem`, and its collection `OnTourSystemCollection`. The artist does not know its components: `MArtist` declares nothing of them, and a new capability changes nothing of the artist.

The **slot** of a component on its entity, `artist.Tour`, is written with the component. It reads as an `Option<T>`, with `Any`, `Get`, `TryGet`, and gives `New` and `Remove`. An entity has at most one component of a type; the component reads its entity, `tour.Artist`. Removing a capability never removes its artist:

```csharp
[Fact]
public void GiveAnArtistACapability()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-tour", StoreAccessMode.ReadWrite);
    using var label = new CapabilitiesRepository(store);
    using var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "Bowie" };
    Assert.False(bowie.Tour.Any());

    // New attaches the component to the artist; the component reads its artist.
    var tour = bowie.Tour.New();
    tour.Concerts.New().City = "Berlin";
    tour.Concerts.New().City = "London";
    Assert.Same(bowie, tour.Artist);
    Assert.Same(tour, bowie.Tour.Get());

    // One component of a type per entity: a second one is refused, and nothing changes.
    Assert.Throws<InvalidOperationException>(() => bowie.Tour.New());
    Assert.Equal(2, bowie.Tour.Get().Concerts.Count());

    // The tour ends: the component and its concerts are removed, the artist stays.
    Assert.True(bowie.Tour.Remove());
    Assert.False(bowie.Tour.Any());
    Assert.Empty(ConcertCollection);
    Assert.Single(ArtistCollection);
    transaction.Commit();
}
```

An artist removed takes its capabilities with it, and their parts, at the same instruction:

```csharp
[Fact]
public void RemoveTheCapabilitiesWithTheirArtist()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-removal", StoreAccessMode.ReadWrite);
    using var label = new CapabilitiesRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "Bowie" };
        bowie.Tour.New().Concerts.New().City = "Berlin";
        bowie.Verified.New().Since = new DateTime(1972, 6, 6);
        bowie.Reviews.New().Items.New().Text = "Timeless";
        transaction.Commit();
    }
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        ArtistCollection.Single().Remove();
        Assert.Empty(TourCollection);
        Assert.Empty(ConcertCollection);
        Assert.Empty(ReviewsCollection);
        transaction.Commit();
    }
}
```

A component of two entity types reads its entity as an `Option` of each type. Its collection, `ReviewsCollection`, holds every review thread, whatever its entity: "everything that can be reviewed".

```csharp
[Fact]
public void ShareACapabilityBetweenTwoTypes()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-reviews", StoreAccessMode.ReadWrite);
    using var label = new CapabilitiesRepository(store);
    using var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite);
    var bowie = new Artist { Name = "Bowie" };
    var low = new Album { Title = "Low" };
    bowie.Reviews.New().Items.New().Stars = 5;
    var ofLow = low.Reviews.New();
    ofLow.Items.New().Stars = 4;

    // Of several entity types, the component reads its entity as an Option of each type.
    Assert.Same(low, ofLow.Album.Get());
    Assert.False(ofLow.Artist.Any());

    // "Everything that can be reviewed" is the collection of the component, whatever the types of their entities.
    Assert.Equal(2, ReviewsCollection.Count());
    Assert.Equal(9, ReviewsCollection.Sum(static r => r.Items.Sum(static i => i.Stars)));
    transaction.Commit();
}
```

A **system** gives, for each entity that has its required components and none of its excluded ones, the Views of the entity and of its components: `Artist`, `Tour`, and `Verified`, an `Option`. A component that the transaction attaches or removes is seen at the next instruction. `Of` gives the system of one entity, or none.

```csharp
[Fact]
public void JoinTheArtistsAndTheirCapabilities()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-system", StoreAccessMode.ReadWrite);
    using var label = new CapabilitiesRepository(store);
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bowie = new Artist { Name = "Bowie" };
        bowie.Tour.New().Concerts.New().City = "Berlin";
        bowie.Verified.New();
        new Artist { Name = "Eno" }.Tour.New();
        var reed = new Artist { Name = "Reed" };
        reed.Tour.New();
        reed.Retired.New().Reason = "Rest";
        new Artist { Name = "Iggy" };
        transaction.Commit();
    }
    using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The members of a system are Views: the enumeration creates no Body.
        Assert.Equal(["Bowie", "Eno"], OnTourSystemCollection.Select(static s => s.Artist.Name).OrderBy(static n => n).ToList());
        Assert.Equal(["Bowie"], OnTourSystemCollection.Where(static s => s.Verified.Any()).Select(static s => s.Artist.Name).ToList());

        // The capabilities that the transaction gives or takes are seen at the next instruction.
        var iggy = ArtistCollection.Single(static a => a.Name == "Iggy");
        iggy.Tour.New();
        ArtistCollection.Single(static a => a.Name == "Eno").Retired.New();
        Assert.Equal(["Bowie", "Iggy"], OnTourSystemCollection.Select(static s => s.Artist.Name).OrderBy(static n => n).ToList());

        // The system of one artist, when it has the capabilities of the system.
        Assert.True(OnTourSystem.Of(iggy).Any());
        Assert.False(OnTourSystem.Of(ArtistCollection.Single(static a => a.Name == "Reed")).Any());
        transaction.Commit();
    }
}
```

The generator checks the components and the systems: a component is neither `[Root]`, `[Removable]`, `[WithIdentity]` nor `[Orphanable]`, and no composition holds it (error **GBO029**); a system has exactly one member of its entity type, the others are components of that type (error **GBO030**); the interface of a component starts with `C`, the one of a system with `S` (error **GBO007**).

> **Tip: A system reads what may belong to it**
>
> A system reads the entities of its rarest required component, not every entity of its type: the artists on tour, among a million artists, cost the artists on tour. A system without a required component reads every entity of its type. Its members are Views: change the few entities you need by `ToBody()`.

> **Tip: A component, or a property?**
>
> A value that every artist has is a property: its name. A capability that some artists have, with values of their own, that comes and goes, is a component: its tour. A component is changed without its entity: a concert added to a tour does not touch the artist, and does not conflict with a change of its name.

## From the need to the link

The lessons went from a link to what it does. This chapter goes the other way: from what your business needs to the link that gives it. It asks its questions in their order, from the most general, what lives on its own, to the most specific, your rules and the concurrency. Each answer is a declaration of the model, and what GBO then guarantees.

The examples come from the applications that every developer meets: a shop, with its customers, orders and products; a document store; an e-mail service; a school.

### 1. What lives on its own?

Before any link, each type of your model is one of three kinds of entity. The question is: who decides its end?

| The need | The kind | The declaration |
| -------- | -------- | --------------- |
| A customer, an order, a product: your code creates them, finds them, and removes some of them | A Root | `[Root]`, and `[Removable]` when your business removes it |
| A country, a currency, a tax rate: reference data that never goes away | A Root that is not Removable | `[Root]` alone: it has no `Remove()`, nothing has to protect it, and a strong reference to it costs nothing (Lesson 2) |
| An order line, the billing address of an invoice: nothing without what holds it | A part | A composition of its Owner (Lesson 4) |
| A file attached to many e-mails: kept while one of them still holds it | A shared object | A type neither Root nor part, designated by strong references (Lesson 11) |

> **Tip: Removed, or no longer sold?**
>
> A product that past orders designate is not removed: it is no longer sold. That is a property, `bool Discontinued`, which your queries read. Keep the removal for what your business really forgets: a draft, a cart, an account closed for good.

### 2. Does it belong to something?

When a thing is created, changed and removed with another one, it is a part of it: the two are one aggregate. The form of the composition follows the number of parts that the Owner holds.

**Any number: an order and its lines.** A collection of parts, without `[Strong]` nor `[Weak]`:

```csharp
// In MOrder: the lines, born in the order, removed with it.
[Property(3)]
IEntityCollection<MOrderLine> Lines { get; }

// In MOrderLine: its order, read, never set.
[OwnedFrom]
MOrder Order { get; }
```

A line is born by `order.Lines.New(product)`, its product given to its birth: a required link (question 3). It is never without an order, and it is removed with it, at the same instruction (Lesson 5).

**Exactly one: an invoice and its billing address.** A required composition, `MAddress BillingAddress { get; }`: the address is born with the invoice, and never leaves it.

**One, or none: a delivery and its proof of delivery.** A slot, `Option<MProofOfDelivery> Proof { get; }`: `delivery.Proof.New()` fills it when the parcel is handed over, and refuses a slot already full.

**The same parts, seen twice: the tasks of a project, and its tasks of the week.** Two compositions of the same type, `Tasks` and `ThisWeek`: a task enters the second by `Add`, and is removed only when it leaves the last one (Lesson 6). A task that changes project moves by `MoveTo`: it is never without a project.

**A hierarchy: the folders of a document store.** A folder lives at the top of a workspace, or in another folder: a part with two types of Owner, and one `[OwnedFrom] Option<T>` for each (Lesson 7). No folder can be placed in its own subtree, and the removal of a folder removes its whole subtree.

> **Tip: An aggregate is what changes together**
>
> Put in an aggregate what your business creates, checks and removes with its Owner: the lines of an order, the pages of a document. Leave out what lives a life of its own: an order does not belong to its customer (question 8 shows why).

### 3. Does it designate something that lives elsewhere?

A link to an entity that is not its part has one more thing to say: what happens when the target goes.

| The need | The link | When the target is removed |
| -------- | -------- | -------------------------- |
| An order line sells a product: the product stays while a line sells it | `[Strong] MProduct Product` | Refused, before any change: `ON DELETE RESTRICT` (Lesson 2) |
| An employee reports to a manager, or to nobody | `[Strong] Option<MEmployee> Manager` | Refused while someone reports to the manager: move the reports first |
| A customer lives in a country | `[Strong] MCountry Country`, to a Root that is not Removable | Never removed: there is nothing to refuse |
| A product replaced by a newer one; the last product that a customer viewed | `[Weak] Option<MProduct> ReplacedBy` | The link becomes none, at that instruction: `ON DELETE SET NULL` (Lesson 3) |

The type gives the cardinality: `T` when the link always exists, given at the birth; `Option<T>` when it may be empty. A weak link is always an `Option<T>`, since its target may go.

> **Tip: Strong by default**
>
> A strong reference keeps your data right: no line ever sells a product that does not exist. Choose `[Weak]` when the link is a convenience, a history or a suggestion, which may disappear without harm.

### 4. Does it designate many?

**A bundle offer and its products.** `[Strong] IEntityCollection<MProduct> Products`: each product of the bundle is retained, and cannot be removed while the bundle offers it (Lesson 13). `Add` retains, `Remove` releases.

**A wish list.** `[Weak] IEntityCollection<MProduct> WishList`: it retains nothing, and a removed product leaves every wish list at once (Lesson 14).

A collection is a set: it holds an entity once, in no order of yours (Lesson 8). When the link carries data of its own, a quantity, a date, a position, the link is an entity, which designates the target:

| The need | Not | But |
| -------- | --- | --- |
| A cart holds three keyboards | A collection of products, which holds a keyboard once | A part `MCartLine`, with `[Strong] MProduct Product` and `int Quantity` |
| A student enrolled in a course, on a date, with a grade | A collection of students | A part `MEnrollment` of the course, with `[Strong] MStudent Student`, `DateTime Date` and `decimal Grade` |
| A playlist plays the same track twice, in its own order | A collection of tracks | A part `MEntry`, with `[Strong] MTrack Track` and `int Position`, read by `OrderBy` |

### 5. Does it designate a part of another aggregate?

**An invoice line bills an order line.** `[Strong] MOrderLine Billed`: the order line cannot be removed, nor its order, nor the Owners of the order, since their removal would take the line with them (Lesson 9). An invoiced order stays; the exception names the chain, from the removed entity to the billed line.

**A supplier leaves the catalog; its products still sold must stay.** When the products are parts of their supplier, `[Orphanable]` lets a product survive it: retained by an order line, it stays, whole, as an orphan, its `[OwnedFrom] Option<MSupplier>` empty (Lesson 10). It is removed at the release of its last retention, or placed under another supplier by `Add`.

### 6. Is it shared, with no owner?

**A file attached to many e-mails.** A forward, a reply to all: the same attachment, in many messages. Each message designates it by a strong reference, `[Strong] IEntityCollection<MAttachment> Attachments`, and the attachment is a shared object: no composition holds it, and nothing removes it explicitly. It is born with its first reference, `message.Attachments.New()`, and removed at the instruction that releases its last one: the deletion of the last message that holds it (Lessons 11 to 13).

**Tags shared by documents.** `[Strong] IEntityCollection<MTag> Tags`: a tag lives while a document carries it, and the last document that drops it removes it.

To pass a shared object from one holder to another, take the new retention first: add the attachment to the message you send before you delete the draft (Lesson 12).

### 7. Must it know who points to it?

**The orders of a customer; the customers who wish for a product.** A reverse property reads the other direction, without a query:

```csharp
// In MCustomer: the orders that designate this customer, by their strong reference Customer.
[ReverseStrongReference(nameof(MOrder.Customer))]
IEntityCollection<MOrder> Orders { get; }

// In MProduct: the customers whose wish list holds this product.
[ReverseWeakReference(nameof(MCustomer.WishList))]
IEntityCollection<MCustomer> WishedBy { get; }
```

Both are up to date at each instruction (Lesson 15). A reverse property of a strong reference costs a write at each new reference: declare it when your code reads it. A product sold by a million lines needs its `Lines` only if your code reads the lines of a product; its removal is refused without them.

### 8. Is it a capability that some entities have?

**A product sold online, a product on sale, a product discontinued.** Some products have a web page, with its slug and its photos; some are on sale, with a rate and an end date. A property of every product would be empty for most of them, and a new capability would change the product type. A component holds each one: `[ComponentFor(nameof(MProduct))]`, `product.OnSale.New()` when the sale starts, `product.OnSale.Remove()` when it ends (Lesson 21).

**What can be commented: an order, an invoice.** One component of two entity types, `[ComponentFor(nameof(MOrder))]` and `[ComponentFor(nameof(MInvoice))]`: its collection holds every thread of comments, whatever its entity.

**The products on sale that are sold online.** A system, `[SystemFor(nameof(MProduct))]`, with the two components as members: it reads only the products on sale, not the whole catalog.

### 9. Your rules, the identities, the concurrency

**Your rules on the links.** An order holds a hundred lines at most: a method of yours checks, then writes, and its refusal leaves the transaction usable (Lesson 17). The total of an order follows its lines: `OnLinesAdded` and `OnLinesRemoved` compute it again (Lesson 16). A removed customer leaves a note in an archive: `OnDeleting`.

**An identity known outside.** An order that an API returns, that a message designates or that an event records takes `[WithIdentity]`: its `OrderRef` is kept outside, and resolves in a transaction. Parts take one too: an order line that a return slip designates. A command draws the identities, its event carries them, and the Replay creates the same graph (Lesson 19).

**The concurrency sets the size of an aggregate.** Adding or removing a part changes its Owner: two transactions that change the parts of the same Owner conflict, and the second retries (Lesson 18). That protects the rules of the Owner: two transactions cannot both add the hundredth line of an order. It also sets the limit of an aggregate:

| The model | Two orders placed at once by one customer |
| --------- | ----------------------------------------- |
| The orders are parts of the customer | Both change the customer: the second conflicts, and retries |
| The orders are Roots, with `[Strong] MCustomer Customer` | Each order is an aggregate of its own, and a unit of conflict of its own |

Choose an aggregate by the rule it protects: an order and its lines, for the total and the limit of lines; not a customer and all its orders, which no rule checks together.

### The whole path

| The question | When the answer is yes | The declaration | Lessons |
| ------------ | ---------------------- | --------------- | ------- |
| Does it live on its own, removed by your code? | A Root | `[Root]`, `[Removable]` | 1, 2 |
| Is it never removed? | Reference data | `[Root]` without `[Removable]` | 2 |
| Is it created and removed with another entity? | A part | `IEntityCollection<T>`, `T` or `Option<T>`, with neither `[Strong]` nor `[Weak]` | 4 to 8 |
| Must the target stay while the link designates it? | A strong reference | `[Strong] T`, `[Strong] Option<T>`, `[Strong] IEntityCollection<T>` | 2, 13 |
| May the target go, and the link forget it? | A weak reference | `[Weak] Option<T>`, `[Weak] IEntityCollection<T>` | 3, 14 |
| Does the link carry data: a quantity, a date, a position? | An entity of the link | A part with a strong reference and its properties | 8, 9 |
| Must a retained part survive its Owner? | An orphan | `[Orphanable]`, with `[OwnedFrom] Option<T>` | 10 |
| Is it shared, and kept while used? | A shared object | A type neither Root nor part, designated by `[Strong]` | 11 to 13 |
| Must it read who points to it? | A reverse property | `[ReverseStrongReference(...)]`, `[ReverseWeakReference(...)]` | 15 |
| Is it known outside the Repository? | A business identity | `[WithIdentity]` | 19 |
| Is it a capability that some entities have, with values of its own? | A component | `[ComponentFor(nameof(MX))]`, its interface starting with `C` | 21 |
| Must a query join an entity and its capabilities? | A system | `[SystemFor(nameof(MX))]`, its interface starting with `S` | 21 |

## What you learned

- A **link** is a property of the type of another entity: reading it gives the target. `T` is required, `Option<T>` optional.
- A **strong reference** retains its target: its removal is refused, before any change. A **weak reference** retains nothing, and is set to none when its target is removed.
- A **composition** owns its parts: they are born in it, move by `MoveTo`, and are removed with their Owner, at the same instruction. A **collection** is a set, of any size.
- A strong reference to a part **retains its whole chain** of Owners; an **`[Orphanable]`** part survives its Owner while something retains it.
- A **shared object** lives as long as its strong references: take the new retention before releasing the old one.
- **Weak collections** forget their removed targets; **reverse properties** read the entities that point to this one, up to date at each instruction.
- Your **partial methods** keep your rules; a rule that refuses before its write leaves the transaction usable, an exception after a write **condemns** it.
- An aggregate is a **unit of conflict**; a link never designates an entity removed by a concurrent transaction.
- An entity `[WithIdentity]` takes its **identity at every birth**: a command and the Replay of its event create the same graph.
- The **garbage collection is deterministic**: each entity dies at the instruction that releases its last retention.
- A **component** is a capability that an entity gains and loses without knowing it, removed with its entity; a **system** joins an entity and its components, and reads only what may belong to it.

[Tutorial Three](Three-Composition.md) goes further with the components and the systems: the composition model of GBO.

**Next tutorial:** [Tutorial Three: Composition](Three-Composition.md)
