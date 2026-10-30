# Tutorial Four: Edges

## Edges and graph oriented model

Tutorial Two linked entities by references: an album designates its artist, an order owns its lines. Tutorial Three gave entities capabilities that they do not know. Many relations are neither: a critic rates a movie with stars, a city is linked to another by a road of a length, a person belongs to a group with a role. The stars, the length and the role belong to neither end: they are the values of the relation itself, and the relation must vanish with either end. A rating without its movie, or without its critic, means nothing.

These relations are **edges**: an entity of its own, born between its two ends, removed with either of them, one per pair of ends unless its type allows several, found by that pair. A set of edges is a graph: the roads of a map, the friendships of a network, the ratings of a site. This tutorial shows how it works:

- **The edges.** A relation that carries values: `ada.NewRating(dune, stars: 5)`, read from both ends, found by its pair; one per pair, or several; removed with either end, never the reverse.
- **The navigation by the Views.** The edges of an end, their other ends, the edge of a pair: read by the Views, without creating any Body; the Bodies for the few to write.
- **The graphs.** The roads between cities, in one direction; your walks over them, from the cities within two roads to the shortest route.
- **The graph oriented model.** Symmetric relations, the links of an edge, the concurrency of edges, and the choice between an edge and a reference.

None of this takes code of yours, but your walks: you declare the edges in the model, and GBO keeps each one with its two ends, at each instruction, in the transaction and at the commit.

Each lesson is a test class of the project `GhostBodyObject.TutorialFour`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorials Two and Three: the references, the parts, the cascades, the identities, the static lambdas, `GetOrNew` and the Views are used here without being explained again.

## What you will build

A site of movie reviews: critics, movies, the ratings that critics give and the screenings of the movies they see. A road map: cities, and the roads between them, some with a toll. A social network: people, their friendships, their memberships in groups, with a role; the certificates of the memberships and the notices of the friendships. Then a forum and a bank, to choose the link of a relation.

Four models, each in a namespace of its own with its own Repository, as in Tutorials Two and Three: `Reviews` for Lessons 1 to 4, `Network` for Lessons 5 and 6, `Social` for Lessons 7 to 9, `Choices` for Lesson 10.

**The values of the models come first**, each a value object ([Tutorial One, Lesson 18](One-TheBasics.md#lesson-18-give-each-value-its-type)). Those the four models share are declared once, in the namespace of the tutorial; each model declares at its head the values that it alone has. The values of an edge are given to its birth by their name, as any value: `critic.NewRating(movie, stars: 5)`.

```csharp
namespace TutorialFour
{
    [ValueObject<GhostString>]
    internal interface VName { }

    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>A text written by a person: a notice, a post.</summary>
    [ValueObject<GhostString>]
    internal interface VFreeText { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }
}
```

Every declaration of the tutorial, and the lesson that shows it:

| Declaration in the model | What GBO guarantees | Lessons |
| ------------------------ | ------------------- | ------- |
| `[Edge(nameof(MCritic), nameof(MMovie))]` on `ERating` | An entity between its two ends, which do not declare it; one per pair; removed with either end, never retaining them | 1, 2, 3 |
| `[Edge(..., Multiple = true)]` | Several edges per pair of ends | 2, 5 |
| `[Removable]` on an edge | The code removes it: `Remove()`, `Remove(other)`; without it, it leaves with an end only | 3 |
| `[Edge(nameof(MCity), nameof(MCity))]` | A graph of one type: the edges that leave an entity, and those that arrive | 5, 6 |
| `[Edge(..., Symmetric = true)]` | No direction: one edge per pair, the same from both ends | 7, 9 |
| `[Strong] MRole Role` in an edge | A required link, given to its birth, compared by `GetOrNew` | 8 |
| `[Strong] EMembership`, `[Weak] Option<EFriendship>` | A reference to an edge: a strong one retains it and its ends; a weak one forgets it | 8 |

## Part 1: Edges

### Lesson 1: Relate two entities with values

A critic rates a movie with stars, on a date. The stars are neither a value of the critic, who rates many movies, nor of the movie, rated by many critics: they are the values of the relation itself. The rating is an **edge**: an `[Entity]` with `[Edge(nameof(MCritic), nameof(MMovie))]`, whose interface starts with `E`.

```csharp
// The values of this model alone, first; the others are those of the tutorial.
[ValueObject<int>(ImplicitConversion = true)]
internal interface VYear { }

/// <summary>The stars of a rating, from 0 to 5. A sum of ratings is no rating: it adds their values.</summary>
[ValueObject<int>(ImplicitConversion = true)]
internal interface VStars { }

internal partial struct Stars
{
    private static bool IsValid(int stars) => stars is >= 0 and <= 5;
}

[Entity(1, 1, 1), Root, Removable, Enumerable]
internal interface MCritic
{
    [Property(1)]
    VName Name { get; set; }
}

[Entity(2, 1, 2), Root, Removable, Enumerable]
internal interface MMovie
{
    [Property(1)]
    VTitle Title { get; set; }

    [Property(2)]
    VYear Year { get; set; }
}

[Entity(3, 1, 2), Edge(nameof(MCritic), nameof(MMovie)), Removable, Enumerable]
internal interface ERating
{
    [Property(1)]
    VStars Stars { get; set; }

    [Property(2)]
    VInstant At { get; set; }
}

[Entity(4, 1, 1), Edge(nameof(MCritic), nameof(MMovie), Multiple = true), Enumerable]
internal interface EScreening
{
    [Property(1)]
    VInstant At { get; set; }
}

[GhostRepository, IncludeEntityModel(nameof(MCritic)), IncludeEntityModel(nameof(MMovie)), IncludeEntityModel(nameof(ERating)), IncludeEntityModel(nameof(EScreening))]
internal interface MReviewRepository
{
}
```

Neither the critic nor the movie declares anything: the edge gives them its members, and a new kind of edge changes nothing of them. `ada.NewRating(dune, stars: 5)` makes the rating born between its two ends, from the critic to the movie, its values given in the order of their identifiers, or set next. The rating reads its ends, `rating.Critic` and `rating.Movie`, named after their types:

```csharp
[Fact]
public void RateAMovie()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-rate", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var ada = new Critic { Name = "Ada" };
        var dune = new Movie { Title = "Dune", Year = 2021 };

        // The rating, from the critic to the movie, with its values.
        var rating = ada.NewRating(dune, stars: 5, at: new DateTime(2026, 10, 1));
        Assert.Same(ada, rating.Critic);
        Assert.Same(dune, rating.Movie);

        // Both ends read it at the next instruction.
        Assert.Equal(1, ada.RatingEdges.Count());
        Assert.Equal(1, dune.RatingEdges.Count());
        transaction.Commit();
    }
    using (reviews.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var rating = RatingCollection.Single();
        Assert.Equal("Ada", rating.Critic.Name);
        Assert.Equal("Dune", rating.Movie.Title);
        Assert.Equal(5, rating.Stars);
    }
}
```

Each end reads its ratings: the critic those she gave, `ada.RatingEdges`, whose other ends are its **`Targets`**; the movie those it received, `dune.RatingEdges`, whose other ends are its **`Sources`**. The rating of a pair is found at once, without reading the other ratings: **`To(dune)`** from the critic, **`From(ada)`** from the movie, an `Option`.

```csharp
[Fact]
public void ReadTheRatingsFromBothEnds()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-read", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var (ada, bob) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" });
    var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
    ada.NewRating(dune, 5);
    ada.NewRating(alien, 3);
    bob.NewRating(dune, 4);

    // From the critic: the movies she rated, and her rating of one of them.
    Assert.Equal(["Alien", "Dune"], ada.RatingEdges.Targets.Select(static m => m.Title).Order().ToList());
    Assert.Equal(3, ada.RatingEdges.To(alien).Get().Stars);

    // From the movie: the critics who rated it, and the rating of one of them.
    Assert.Equal(["Ada", "Bob"], dune.RatingEdges.Sources.Select(static c => c.Name).Order().ToList());
    Assert.Equal(4, dune.RatingEdges.From(bob).Get().Stars);

    // A pair without rating.
    Assert.False(bob.RatingEdges.Contains(alien));
    Assert.False(bob.RatingEdges.To(alien).Any());
    transaction.Commit();
}
```

The values of an edge change as those of any entity. Its ends never change: they have no Setter, and a rating of another movie is another rating.

```csharp
[Fact]
public void ChangeTheValuesOfAnEdge()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-change", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Critic { Name = "Ada" };
    var dune = new Movie { Title = "Dune" };
    var rating = ada.NewRating(dune, 3);

    // The critic sees it again, and changes her mind.
    rating.Stars = 5;
    rating.At = new DateTime(2026, 10, 15);
    Assert.Equal(5, dune.RatingEdges.From(ada).Get().Stars);

    // The ends of an edge have no Setter: a rating of another movie is another rating.
    transaction.Commit();
}
```

The generator checks an edge: its interface starts with `E`, its ends are entity types of the Repository, and it is neither `[Root]`, `[Orphanable]` nor `[WithIdentity]`, a component nor a system; otherwise, the build stops with the error **GBO031**.

### Lesson 2: One edge per pair, or several

A critic rates a movie once: an edge is **one per pair** of ends, unless its type says otherwise. A second rating of the same movie by the same critic is refused, before anything changes, and the transaction goes on. **`GetOrNew`** gives the rating of the pair, or a new one: one instruction rates a movie, whether the critic rated it already or not.

```csharp
[Fact]
public void RateAMovieOnce()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-once", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Critic { Name = "Ada" };
    var dune = new Movie { Title = "Dune" };
    ada.NewRating(dune, 4);

    // The refusal changes nothing: the transaction goes on.
    Assert.Throws<InvalidOperationException>(() => ada.NewRating(dune, 2));
    Assert.Equal(1, RatingCollection.Count());

    // The rating of the pair, or a new one: the same instruction rates a movie the first time, and the next ones.
    Rate(ada, dune, 5);
    Rate(ada, new Movie { Title = "Alien" }, 3);
    Assert.Equal(5, ada.RatingEdges.To(dune).Get().Stars);
    Assert.Equal(2, RatingCollection.Count());
    transaction.Commit();
}

// Rates the movie: the rating of the pair, or a new one.
private static void Rate(Critic critic, Movie movie, Stars stars) => critic.RatingEdges.GetOrNew(movie).Stars = stars;
```

A critic may see a movie several times: **`[Edge(..., Multiple = true)]`** allows several edges per pair. The screenings of a pair are then a sequence, `ada.ScreeningEdges.To(dune)`, and the movies she saw are each listed once in her `Targets`.

```csharp
[Fact]
public void SeeAMovieSeveralTimes()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-several", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Critic { Name = "Ada" };
    var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
    ada.NewScreening(dune, new DateTime(2026, 9, 1));
    ada.NewScreening(dune, new DateTime(2026, 10, 1));
    ada.NewScreening(alien, new DateTime(2026, 10, 2));

    Assert.Equal(3, ada.ScreeningEdges.Count());
    Assert.Equal(2, ada.ScreeningEdges.To(dune).Count());
    Assert.Equal(new DateTime(2026, 10, 1), ada.ScreeningEdges.To(dune).Max(static s => s.At));
    Assert.Equal(["Alien", "Dune"], ada.ScreeningEdges.Targets.Select(static m => m.Title).Order().ToList());
    transaction.Commit();
}
```

### Lesson 3: An edge lives with its ends

An edge never survives its ends: removing a movie removes its ratings and its screenings, at that instruction; removing a critic, hers. The other end stays, with its other edges. The reverse never happens: **an edge never retains its ends**, and its removal leaves them as they are.

The code removes an edge when its type is **`[Removable]`**: `rating.Remove()`, or by the pair, `ada.RatingEdges.Remove(dune)`.

```csharp
[Fact]
public void RemoveAnEdge()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-remove", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Critic { Name = "Ada" };
    var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
    var rating = ada.NewRating(dune, 5);
    ada.NewRating(alien, 1);

    rating.Remove();
    Assert.True(ada.RatingEdges.Remove(alien));
    Assert.False(ada.RatingEdges.Remove(alien));

    // The ends stay.
    Assert.Equal(0, ada.RatingEdges.Count());
    Assert.Equal((1, 2), (CriticCollection.Count(), MovieCollection.Count()));
    transaction.Commit();
}

[Fact]
public void RemoveTheEdgesWithTheirEnd()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-cascade", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var (ada, bob) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" });
        var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
        ada.NewRating(dune, 5);
        bob.NewRating(dune, 4);
        ada.NewRating(alien, 3);
        ada.NewScreening(dune, new DateTime(2026, 10, 1));
        transaction.Commit();
    }
    using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        MovieCollection.Single(static m => m.Title == "Dune").Remove();
        Assert.Equal(["Alien"], RatingCollection.Select(static r => r.Movie.Title).ToList());
        Assert.Equal(0, ScreeningCollection.Count());
        Assert.Equal(0, CriticCollection.Single(static c => c.Name == "Bob").RatingEdges.Count());
        transaction.Commit();
    }
}
```

A screening is not `[Removable]`: a fact of the history, it has no `Remove`, and leaves with its critic or its movie only.

```csharp
[Fact]
public void KeepTheHistoryUntilAnEndLeaves()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-history", StoreAccessMode.ReadWrite);
    using var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Critic { Name = "Ada" };
    var dune = new Movie { Title = "Dune" };
    ada.NewScreening(dune, new DateTime(2026, 9, 1));
    ada.NewScreening(dune, new DateTime(2026, 10, 1));

    // Screening has no Remove: screening.Remove() and ada.ScreeningEdges.Remove(dune) do not compile.
    ada.Remove();
    Assert.Equal(0, ScreeningCollection.Count());
    Assert.Equal(0, dune.ScreeningEdges.Count());
    transaction.Commit();
}
```

### Lesson 4: Navigate by the Views

The edges are **navigated by the Views**: the View of an end has the same members as its Body, which give Views, without creating any Body. The lessons of this part start from a small site:

```csharp
private static ReviewRepository NewSite(TransactionalMemoryStore store)
{
    var reviews = new ReviewRepository(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
    var (ada, bob, cid) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" }, new Critic { Name = "Cid" });
    var (dune, alien, heat) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" }, new Movie { Title = "Heat" });
    ada.NewRating(dune, 5);
    bob.NewRating(dune, 4);
    cid.NewRating(dune, 0);
    ada.NewRating(alien, 4);
    bob.NewRating(heat, 5);
    transaction.Commit();
    return reviews;
}
```

Counting the ratings of a movie, summing their stars, listing its critics, finding the rating of a pair: Views, read where they are.

```csharp
[Fact]
public void ReadTheRatingsOfAMovieByItsView()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-read", StoreAccessMode.ReadWrite);
    using var reviews = NewSite(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadOnly);
    var dune = MovieViewCollection.Single(static m => m.Title == "Dune");
    Assert.Equal(3, dune.RatingEdges.Count());
    Assert.Equal(9, dune.RatingEdges.Sum(static r => r.Stars.Value));
    Assert.Equal(["Ada", "Bob", "Cid"], dune.RatingEdges.Sources.Select(static c => c.Name).Order().ToList());

    // The rating of a pair, from either end, its other end given by its View.
    var bob = CriticViewCollection.Single(static c => c.Name == "Bob");
    Assert.Equal(4, dune.RatingEdges.From(bob).Get().Stars);
    Assert.Equal(4, bob.RatingEdges.To(dune).Get().Stars);
}
```

Every movie ranked by its average rating: a pass over the Views of the movies and of their ratings, which creates no Body.

```csharp
[Fact]
public void RankTheMovies()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rank", StoreAccessMode.ReadWrite);
    using var reviews = NewSite(store);
    using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadOnly);
    var ranking = MovieViewCollection
        .OrderByDescending(static m => (double)m.RatingEdges.Sum(static r => r.Stars.Value) / m.RatingEdges.Count())
        .Select(static m => m.Title).ToList();
    Assert.Equal(["Heat", "Alien", "Dune"], ranking);
}
```

The Bodies are for the few edges to write: a search by the Views finds them, `ToBody()` gives their Body. A birth takes its other end by its View as well: `cid.NewRating(alienView, 4)`.

```csharp
[Fact]
public void WriteTheFewByTheirBody()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-write", StoreAccessMode.ReadWrite);
    using var reviews = NewSite(store);
    using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The moderation removes the ratings without a star.
        var dune = MovieViewCollection.Single(static m => m.Title == "Dune");
        foreach (var empty in dune.RatingEdges.Where(1, static (rating, least) => rating.Stars < least).ToList())
            empty.ToBody().Remove();

        // Cid rates Alien, found by its View.
        var alien = MovieViewCollection.Single(static m => m.Title == "Alien");
        CriticCollection.Single(static c => c.Name == "Cid").NewRating(alien, 4);
        transaction.Commit();
    }
    using (reviews.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(2, MovieViewCollection.Single(static m => m.Title == "Dune").RatingEdges.Count());
        Assert.Equal(8, MovieViewCollection.Single(static m => m.Title == "Alien").RatingEdges.Sum(static r => r.Stars.Value));
    }
}
```

## Part 2: Graphs

### Lesson 5: A graph of one type

The roads of a road map link cities to cities: an edge whose two ends are of one type, `[Edge(nameof(MCity), nameof(MCity))]`. Its ends are named **`From`** and **`To`**.

```csharp
// The values of this model alone, first; the others are those of the tutorial.
[ValueObject<double>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
internal interface VKilometers { }

internal partial struct Kilometers
{
    private static bool IsValid(double kilometers) => kilometers >= 0;
}

/// <summary>Whether a road is free or has a toll: a kind, an enum, not a bool.</summary>
internal enum RoadKind
{
    Free,
    Toll
}

[Entity(1, 1, 1), Root, Removable, Enumerable, WithIdentity]
internal interface MCity
{
    [Property(1)]
    VName Name { get; set; }
}

[Entity(2, 1, 2), Edge(nameof(MCity), nameof(MCity), Multiple = true), Removable, Enumerable]
internal interface ERoad
{
    [Property(1)]
    VKilometers Distance { get; set; }

    [Property(2)]
    RoadKind Kind { get; set; }
}

[GhostRepository, IncludeEntityModel(nameof(MCity)), IncludeEntityModel(nameof(ERoad))]
internal interface MRoadMapRepository
{
}
```

A city has two members: **`RoadEdges`**, the roads that leave it, whose other ends are their `Targets`; **`IncomingRoadEdges`**, those that arrive at it, whose other ends are their `Sources`. A road goes in one direction:

```csharp
[Fact]
public void ConnectTwoCitiesInOneDirection()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-direction", StoreAccessMode.ReadWrite);
    using var map = new RoadMapRepository(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadWrite);
    var (paris, lyon) = (NewCity("Paris"), NewCity("Lyon"));
    var road = paris.NewRoad(lyon, distance: 465);
    Assert.Same(paris, road.From);
    Assert.Same(lyon, road.To);

    // Paris has a road that leaves it; Lyon, a road that arrives.
    Assert.Equal(["Lyon"], paris.RoadEdges.Targets.Select(static c => c.Name).ToList());
    Assert.Equal(["Paris"], lyon.IncomingRoadEdges.Sources.Select(static c => c.Name).ToList());
    Assert.Equal(0, lyon.RoadEdges.Count());
    Assert.Equal(0, paris.IncomingRoadEdges.Count());
    transaction.Commit();
}

private static City NewCity(Name name) => new(CityRef.New()) { Name = name };
```

A way back is another road:

```csharp
[Fact]
public void GoBothWays()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-both", StoreAccessMode.ReadWrite);
    using var map = new RoadMapRepository(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadWrite);
    var (paris, lyon) = (NewCity("Paris"), NewCity("Lyon"));
    paris.NewRoad(lyon, 465);
    lyon.NewRoad(paris, 465);
    Assert.Equal(["Lyon"], paris.RoadEdges.Targets.Select(static c => c.Name).ToList());
    Assert.Equal(["Lyon"], paris.IncomingRoadEdges.Sources.Select(static c => c.Name).ToList());
    Assert.Equal(2, RoadCollection.Count());
    transaction.Commit();
}
```

Two cities may be linked by several roads, `Multiple = true`: `paris.RoadEdges.To(lyon)` is the sequence of the roads from Paris to Lyon, and the operators choose among them.

```csharp
[Fact]
public void HaveSeveralRoadsBetweenTwoCities()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-several", StoreAccessMode.ReadWrite);
    using var map = new RoadMapRepository(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadWrite);
    var (paris, lyon) = (NewCity("Paris"), NewCity("Lyon"));
    paris.NewRoad(lyon, distance: 465, kind: RoadKind.Toll);
    paris.NewRoad(lyon, distance: 490);

    Assert.Equal(2, paris.RoadEdges.To(lyon).Count());
    Assert.Equal(465, paris.RoadEdges.To(lyon).Min(static r => r.Distance));
    Assert.Equal(490, paris.RoadEdges.To(lyon).Single(static r => r.Kind == RoadKind.Free).Distance);

    // Lyon is one target, whatever the number of roads to it.
    Assert.Equal(["Lyon"], paris.RoadEdges.Targets.Select(static c => c.Name).ToList());
    transaction.Commit();
}
```

### Lesson 6: Walk the graph

The edges of an end give its neighbours, one road away. A walk of the graph is **your code**, over these neighbours. The map of this lesson:

```csharp
// The map: Paris, Lille, Dijon, Lyon, Grenoble, Marseille, Nice, and the roads between them, some with a toll.
private static RoadMapRepository NewMap(TransactionalMemoryStore store)
{
    var map = new RoadMapRepository(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadWrite);
    var paris = NewCity("Paris");
    var lille = NewCity("Lille");
    var dijon = NewCity("Dijon");
    var lyon = NewCity("Lyon");
    var grenoble = NewCity("Grenoble");
    var marseille = NewCity("Marseille");
    var nice = NewCity("Nice");
    paris.NewRoad(lyon, 465, kind: RoadKind.Toll);
    paris.NewRoad(dijon, 315);
    paris.NewRoad(lille, 225);
    dijon.NewRoad(lyon, 195);
    lyon.NewRoad(marseille, 315, kind: RoadKind.Toll);
    lyon.NewRoad(grenoble, 110);
    grenoble.NewRoad(marseille, 270);
    marseille.NewRoad(nice, 200, kind: RoadKind.Toll);
    transaction.Commit();
    return map;
}
```

The cities within two roads of Paris: a walk in breadth, a road at a time. It reads the Views of the cities, and remembers the cities it met by their identity, `CityRef`, a value that a `HashSet` holds.

```csharp
[Fact]
public void ReachTheCitiesWithinTwoRoads()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-reach", StoreAccessMode.ReadWrite);
    using var map = NewMap(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadOnly);
    var paris = CityViewCollection.Single(static c => c.Name == "Paris");
    Assert.Equal(["Dijon", "Grenoble", "Lille", "Lyon", "Marseille"], Reachable(paris, roads: 2));
    Assert.Equal(["Dijon", "Lille", "Lyon"], Reachable(paris, roads: 1));
}

// The cities within a number of roads of a city, in breadth: each frontier gives the next one, the cities met once.
private static List<string> Reachable(CityView from, int roads)
{
    var met = new HashSet<CityRef> { from.Ref };
    var frontier = new List<CityView> { from };
    var reached = new List<string>();
    for (int step = 0; step < roads; step++)
    {
        var next = new List<CityView>();
        foreach (var city in frontier)
        {
            foreach (var target in city.RoadEdges.Targets)
            {
                if (!met.Add(target.Ref))
                    continue;
                next.Add(target);
                reached.Add(target.Name);
            }
        }
        frontier = next;
    }
    reached.Sort(StringComparer.Ordinal);
    return reached;
}
```

The shortest route from Paris to Marseille: **Dijkstra's algorithm**, which follows the city of the shortest known distance first. It reads the Views of the roads, `city.RoadEdges`, their distances and their targets: a walk over a whole map creates no Body.

```csharp
[Fact]
public void FindTheShortestRoute()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-shortest", StoreAccessMode.ReadWrite);
    using var map = NewMap(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadOnly);
    var paris = CityViewCollection.Single(static c => c.Name == "Paris");
    var marseille = CityViewCollection.Single(static c => c.Name == "Marseille");
    var route = ShortestRoute(paris, marseille, avoidTolls: false).Get();
    Assert.Equal(["Paris", "Lyon", "Marseille"], route.Cities);
    Assert.Equal(780, route.Distance);

    // Nice is reached from Marseille only, never back to Paris.
    Assert.False(ShortestRoute(CityViewCollection.Single(static c => c.Name == "Nice"), paris, avoidTolls: false).Any());
}

private sealed record Route(List<string> Cities, Kilometers Distance);

// The shortest route, by Dijkstra's algorithm: the city of the shortest known distance first, each road that leaves it
// giving a shorter distance to its target remembered. None when no route reaches the target.
private static Option<Route> ShortestRoute(CityView from, CityView to, bool avoidTolls)
{
    var distances = new Dictionary<CityRef, Kilometers> { [from.Ref] = 0 };
    var previous = new Dictionary<CityRef, CityView>();
    var queue = new PriorityQueue<CityView, Kilometers>();
    queue.Enqueue(from, 0);
    while (queue.TryDequeue(out var city, out Kilometers distance))
    {
        if (city.Ref == to.Ref)
            break;
        if (distance > distances[city.Ref])
            continue;
        foreach (var road in city.RoadEdges)
        {
            if (avoidTolls && road.Kind == RoadKind.Toll)
                continue;
            var target = road.To;
            Kilometers through = distance + road.Distance;
            if (distances.TryGetValue(target.Ref, out Kilometers known) && known <= through)
                continue;
            distances[target.Ref] = through;
            previous[target.Ref] = city;
            queue.Enqueue(target, through);
        }
    }
    if (!distances.TryGetValue(to.Ref, out Kilometers total))
        return default;
    var cities = new List<string> { to.Name };
    for (var at = to.Ref; previous.TryGetValue(at, out var before); at = before.Ref)
        cities.Insert(0, before.Name);
    return Option<Route>.Some(new Route(cities, total));
}
```

A rule of the walk is a condition in your code: a route without tolls skips the roads with one.

```csharp
[Fact]
public void AvoidTheTolls()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-tolls", StoreAccessMode.ReadWrite);
    using var map = NewMap(store);
    using var transaction = map.OpenTransaction(StoreAccessMode.ReadOnly);
    var paris = CityViewCollection.Single(static c => c.Name == "Paris");
    var marseille = CityViewCollection.Single(static c => c.Name == "Marseille");
    var route = ShortestRoute(paris, marseille, avoidTolls: true).Get();
    Assert.Equal(["Paris", "Dijon", "Lyon", "Grenoble", "Marseille"], route.Cities);
    Assert.Equal(890, route.Distance);
}
```

## Part 3: The graph oriented model

### Lesson 7: A symmetric relation

A friendship has no direction: Ada is the friend of Bob as Bob is the friend of Ada. **`[Edge(..., Symmetric = true)]`**, between two entities of one type, gives each end one member, `FriendshipEdges`, whose other ends are its **`Others`**; the edge of a pair is **`With(bob)`**, from either end.

```csharp
[Entity(1, 1, 1), Root, Removable, Enumerable, WithIdentity]
internal interface MPerson
{
    [Property(1)]
    VName Name { get; set; }
}

[Entity(2, 1, 1), Edge(nameof(MPerson), nameof(MPerson), Symmetric = true), Removable, Enumerable]
internal interface EFriendship
{
    [Property(1)]
    VInstant Since { get; set; }
}

[Entity(3, 1, 1), Root, Removable, Enumerable]
internal interface MGroup
{
    [Property(1)]
    VName Name { get; set; }
}

[Entity(4, 1, 1), Root, Removable, Enumerable]
internal interface MRole
{
    [Property(1)]
    VName Name { get; set; }
}

[Entity(5, 1, 2), Edge(nameof(MPerson), nameof(MGroup)), Removable, Enumerable]
internal interface EMembership
{
    [Property(1), Strong]
    MRole Role { get; set; }

    [Property(2)]
    VInstant Since { get; set; }
}

[Entity(6, 1, 2), Root, Removable, Enumerable]
internal interface MCertificate
{
    [Property(1)]
    VTitle Title { get; set; }

    [Property(2), Strong]
    EMembership Membership { get; set; }
}

[Entity(7, 1, 2), Root, Removable, Enumerable]
internal interface MNotice
{
    [Property(1)]
    VFreeText Text { get; set; }

    [Property(2), Weak]
    Option<EFriendship> Friendship { get; set; }
}

[GhostRepository, IncludeEntityModel(nameof(MPerson)), IncludeEntityModel(nameof(EFriendship)), IncludeEntityModel(nameof(MGroup)), IncludeEntityModel(nameof(MRole)), IncludeEntityModel(nameof(EMembership)), IncludeEntityModel(nameof(MCertificate)), IncludeEntityModel(nameof(MNotice))]
internal interface MSocialRepository
{
}
```

A friendship is one per pair: `bob.NewFriendship(ada)`, after `ada.NewFriendship(bob)`, is refused. **`friendship.Other(ada)`** gives the friend of Ada.

```csharp
[Fact]
public void BefriendTwoPeople()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-friends", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
    var (ada, bob) = (NewPerson("Ada"), NewPerson("Bob"));
    var friendship = ada.NewFriendship(bob, since: new DateTime(2020, 5, 1));

    Assert.Same(friendship, bob.FriendshipEdges.With(ada).Get());
    Assert.Equal(["Bob"], ada.FriendshipEdges.Others.Select(static p => p.Name).ToList());
    Assert.Equal(["Ada"], bob.FriendshipEdges.Others.Select(static p => p.Name).ToList());
    Assert.Same(bob, friendship.Other(ada));
    Assert.Throws<InvalidOperationException>(() => bob.NewFriendship(ada));

    // The friendship ends, from either end.
    Assert.True(bob.FriendshipEdges.Remove(ada));
    Assert.Equal(0, ada.FriendshipEdges.Count());
    transaction.Commit();
}

private static Person NewPerson(Name name) => new(PersonRef.New()) { Name = name };
```

Two steps give the friends of friends; your code ranks the people a person may know by the friends they have in common:

```csharp
[Fact]
public void SuggestThePeopleToKnow()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-suggest", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var (ada, bob, cid, dan, eve) = (NewPerson("Ada"), NewPerson("Bob"), NewPerson("Cid"), NewPerson("Dan"), NewPerson("Eve"));
        ada.NewFriendship(bob);
        ada.NewFriendship(cid);
        bob.NewFriendship(dan);
        cid.NewFriendship(dan);
        cid.NewFriendship(eve);
        transaction.Commit();
    }
    using (social.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var ada = PersonViewCollection.Single(static p => p.Name == "Ada");
        // Dan is a friend of Bob and of Cid; Eve, of Cid only.
        Assert.Equal(["Dan", "Eve"], PeopleToKnow(ada));
    }
}

// The friends of the friends of a person, but her and her friends, ranked by the friends they share with her: two steps, from
// the View of a person to the Views of the others.
private static List<string> PeopleToKnow(PersonView person)
{
    var friends = new HashSet<PersonRef> { person.Ref };
    foreach (var friend in person.FriendshipEdges.Others)
        friends.Add(friend.Ref);
    var shared = new Dictionary<PersonRef, (string Name, int Count)>();
    foreach (var friend in person.FriendshipEdges.Others)
    {
        foreach (var other in friend.FriendshipEdges.Others)
        {
            if (friends.Contains(other.Ref))
                continue;
            shared[other.Ref] = (other.Name.ToString(), shared.TryGetValue(other.Ref, out var known) ? known.Count + 1 : 1);
        }
    }
    var ranked = new List<(string Name, int Count)>(shared.Values);
    ranked.Sort(static (a, b) => b.Count != a.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Name, b.Name));
    return ranked.ConvertAll(static p => p.Name);
}
```

### Lesson 8: The links of an edge

An edge is an entity: it has **the links of any entity** (Tutorial Two). A required link, a `T`, is given to its birth, after its other end: `ada.NewMembership(club, member)`; a strong one retains its target, the role. `TryGetOrNew` compares it: Ada joining the club as an admin, where the club has her as a member, is a case of the business, which the code handles at once.

```csharp
[Fact]
public void GiveAnEdgeARequiredLink()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-required", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Person(PersonRef.New()) { Name = "Ada" };
    var club = new Group { Name = "Chess club" };
    var (member, admin) = (new Role { Name = "Member" }, new Role { Name = "Admin" });

    // The role is a T of the membership: a parameter of its birth, after the group.
    var membership = ada.NewMembership(club, member, since: new DateTime(2026, 1, 1));
    Assert.Throws<InvalidOperationException>(member.Remove);

    // Ada as an admin, where she is a member: TryGetOrNew gives the membership, and false.
    if (!ada.MembershipEdges.TryGetOrNew(club, admin, out var found))
        found.Role = admin;
    Assert.Same(membership, found);
    Assert.Same(admin, club.MembershipEdges.From(ada).Get().Role);

    // The member role is free.
    member.Remove();
    transaction.Commit();
}
```

Other entities link to an edge as to any entity. A **strong reference to an edge retains it, and its two ends through it**: a certificate of membership keeps the membership, the person and the group, until it is revoked.

```csharp
[Fact]
public void RetainAnEdgeAndItsEnds()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-retain", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Person(PersonRef.New()) { Name = "Ada" };
    var club = new Group { Name = "Chess club" };
    var membership = ada.NewMembership(club, new Role { Name = "Member" });
    var certificate = new Certificate(membership) { Title = "Member of the chess club" };

    // Neither the membership nor its ends can leave while the certificate designates it: the exception names the edge.
    Assert.Throws<InvalidOperationException>(membership.Remove);
    Assert.Contains("Membership", Assert.Throws<InvalidOperationException>(club.Remove).Message);
    Assert.Throws<InvalidOperationException>(ada.Remove);

    // The certificate revoked: the club can go, with the membership.
    certificate.Remove();
    club.Remove();
    Assert.Equal(0, MembershipCollection.Count());
    transaction.Commit();
}
```

A **weak reference** to an edge is set to none when the edge goes: a notice of a friendship forgets it when the friendship ends.

```csharp
[Fact]
public void ForgetARemovedEdge()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-forget", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
    var (ada, bob) = (new Person(PersonRef.New()) { Name = "Ada" }, new Person(PersonRef.New()) { Name = "Bob" });
    var notice = new Notice { Text = "Ada and Bob are friends", Friendship = ada.NewFriendship(bob) };
    Assert.True(notice.Friendship.Any());

    ada.FriendshipEdges.Remove(bob);
    Assert.False(notice.Friendship.Any());
    transaction.Commit();
}
```

### Lesson 9: Edges and concurrency

An edge is **an aggregate of its own**: its values conflict only with themselves. One transaction changes the date of a friendship while another renames one of the friends: two aggregates, both commit.

```csharp
[Fact]
public async Task ChangeAnEdgeAndItsEndsConcurrently()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-parallel", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Person(PersonRef.New()) { Name = "Ada" }.NewFriendship(new Person(PersonRef.New()) { Name = "Bob" });
        transaction.Commit();
    }
    var firstChanged = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();
    var dated = Task.Run(async () =>
    {
        using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
        FriendshipCollection.Single().Since = new DateTime(2019, 6, 1);
        firstChanged.SetResult();
        await secondCommitted.Task;
        transaction.Commit();
    });
    await firstChanged.Task;
    await Task.Run(() =>
    {
        using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
        PersonCollection.Single(static p => p.Name == "Ada").Name = "Ada Lovelace";
        transaction.Commit();
    });
    secondCommitted.SetResult();
    await dated;
    using (social.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var friendship = FriendshipCollection.Single();
        Assert.Equal(new DateTime(2019, 6, 1), friendship.Since);
        Assert.Contains("Ada Lovelace", new[] { friendship.From.Name.ToString(), friendship.To.Name.ToString() });
    }
}
```

Two transactions that make the same pair friends at the same time **conflict** at the second commit, which retries: a pair never ends up with two friendships. Written with `GetOrNew`, the retry runs the same code, and finds the friendship that the other transaction gave.

```csharp
[Fact]
public async Task ConflictOnTheSamePair()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-conflict", StoreAccessMode.ReadWrite);
    using var social = new SocialRepository(store);
    using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Person(PersonRef.New()) { Name = "Ada" };
        new Person(PersonRef.New()) { Name = "Bob" };
        transaction.Commit();
    }
    var firstGiven = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();
    var first = Task.Run(async () =>
    {
        using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
        Befriend("Ada", "Bob", new DateTime(2020, 1, 1));
        firstGiven.SetResult();
        await secondCommitted.Task;
        Assert.Throws<TransactionConflictException>(transaction.Commit);
    });
    await firstGiven.Task;
    await Task.Run(() =>
    {
        using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
        Befriend("Bob", "Ada", new DateTime(2021, 1, 1));
        transaction.Commit();
    });
    secondCommitted.SetResult();
    await first;

    // The retry runs the same code: GetOrNew finds the friendship that the other transaction gave, and dates it.
    using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Befriend("Ada", "Bob", new DateTime(2020, 1, 1));
        transaction.Commit();
    }
    using (social.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(new DateTime(2020, 1, 1), FriendshipCollection.Single().Since);
}

// Makes two people friends since a date: their friendship, or a new one. The same code for the first try and for the retry.
private static void Befriend(Name first, Name second, Instant since)
{
    var person = PersonCollection.Single(first, static (p, name) => p.Name == name);
    var other = PersonCollection.Single(second, static (p, name) => p.Name == name);
    person.FriendshipEdges.GetOrNew(other).Since = since;
}
```

### Lesson 10: An edge, or a reference

A relation between two entities is an edge when three answers are yes: it carries values; it belongs to neither of its two ends; it must vanish with either. The forum and the bank of this lesson have one relation of each kind:

```csharp
// The values of this model alone, first; the others are those of the tutorial.
[ValueObject<GhostString>]
internal interface VReactionKind { }

[ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
internal interface VMoney { }

internal partial struct Money
{
    private static bool IsValid(decimal amount) => amount >= 0;
}

[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MUser
{
    [Property(1)]
    VName Name { get; set; }

    /// <summary>The posts the user likes: no value, a collection of weak references.</summary>
    [Property(2), Weak]
    IEntityCollection<MPost> Likes { get; }
}

[Entity(2, 1, 1), Root, Removable, Enumerable]
internal interface MPost
{
    [Property(1)]
    VFreeText Text { get; set; }

    /// <summary>The users who like the post: the reverse of their likes.</summary>
    [ReverseWeakReference(nameof(MUser.Likes))]
    IEntityCollection<MUser> LikedBy { get; }
}

[Entity(3, 1, 1), Edge(nameof(MUser), nameof(MPost)), Removable, Enumerable]
internal interface EReaction
{
    [Property(1)]
    VReactionKind Kind { get; set; }
}

[Entity(4, 1, 1), Root, Removable, Enumerable]
internal interface MAccount
{
    [Property(1)]
    VName Owner { get; set; }
}

[Entity(5, 1, 3), Root, Removable, Enumerable]
internal interface MTransfer
{
    [Property(1)]
    VMoney Amount { get; set; }

    [Property(2), Strong]
    MAccount From { get; set; }

    [Property(3), Strong]
    MAccount To { get; set; }
}

[GhostRepository, IncludeEntityModel(nameof(MUser)), IncludeEntityModel(nameof(MPost)), IncludeEntityModel(nameof(EReaction)), IncludeEntityModel(nameof(MAccount)), IncludeEntityModel(nameof(MTransfer))]
internal interface MForumRepository
{
}
```

A like has no value: a collection of weak references, and its reverse property, give everything it needs. A reaction has a kind: an edge. A transfer between two accounts carries an amount, but it must not vanish with an account: the history of the bank needs it. It is a Root, with a strong reference to each account.

```csharp
private static ForumRepository NewForum(TransactionalMemoryStore store)
{
    var forum = new ForumRepository(store);
    using var transaction = forum.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new User { Name = "Ada" };
    var post = new Post { Text = "Edges are entities." };
    ada.Likes.Add(post);
    ada.NewReaction(post, kind: "Insightful");
    var (savings, current) = (new Account { Owner = "Ada" }, new Account { Owner = "Bob" });
    new Transfer(savings, current) { Amount = 120m };
    transaction.Commit();
    return forum;
}

[Fact]
public void ChooseTheLinkOfEachRelation()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-choose", StoreAccessMode.ReadWrite);
    using var forum = NewForum(store);
    using var transaction = forum.OpenTransaction(StoreAccessMode.ReadOnly);
    var ada = UserCollection.Single();
    var post = PostCollection.Single();
    Assert.Equal(["Ada"], post.LikedBy.Select(static u => u.Name).ToList());
    Assert.Equal("Insightful", post.ReactionEdges.From(ada).Get().Kind);
    Assert.Equal(120m, TransferCollection.Single(static t => t.From.Owner == "Ada").Amount);
}
```

What the removal of an end does follows from the choice: the like leaves the collection, the reaction leaves with its post, the transfer refuses the removal of its account.

```csharp
[Fact]
public void RemoveAnEndOfEachRelation()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-remove", StoreAccessMode.ReadWrite);
    using var forum = NewForum(store);
    using var transaction = forum.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = UserCollection.Single();
    PostCollection.Single().Remove();
    Assert.Equal(0, ada.Likes.Count());
    Assert.Equal(0, ReactionCollection.Count());

    // The transfer retains its accounts: the removal is refused, and nothing changes.
    var savings = AccountCollection.Single(static a => a.Owner == "Ada");
    Assert.Throws<InvalidOperationException>(savings.Remove);
    Assert.Equal(1, TransferCollection.Count());
    transaction.Commit();
}
```

## From the need to the link

The lessons went from an edge to what it does. This chapter goes the other way: from the relation your business needs to the link that gives it.

### 1. Does the relation carry values?

| The need | The link |
| -------- | -------- |
| A like, a follow, a tag, a favorite: no value | A collection of references, `[Weak] IEntityCollection<T>`, and its reverse property (Tutorial Two, Lessons 14 and 15) |
| A rating with its stars, a stock line with its quantity, a membership with its role | An edge |

> **Tip: A relation that gains a value**
>
> A like that gains a date, a follow that gains a setting of its notifications: the relation becomes an edge. The collection of references is replaced by the edge type, and the reverse property by the members of the edge on its ends.

### 2. Does it belong to one of its ends?

**The lines of an order.** They are born in the order and leave with it only: a composition (Tutorial Two, Lesson 4), not an edge.

**The rating of a movie by a critic, the stock of a product in a warehouse.** Neither end owns them: an edge.

### 3. Must it vanish with either end?

**A rating, a membership, a road.** Without one of its ends, it means nothing: an edge, removed with either.

**A transfer between two accounts, an order, a contract, a booking.** A fact of the business that outlives an end, or that others designate: a Root with strong references, which retain its ends (Lesson 10).

### 4. One per pair, several, or no direction?

| The relation | The declaration |
| ------------ | --------------- |
| A rating, a stock line, a membership: one per pair | `[Edge(nameof(MA), nameof(MB))]` |
| Roads, flights, screenings: several per pair | `Multiple = true` |
| A friendship, a compatibility: no direction | `Symmetric = true`, between one type |

### 5. Does a question go beyond one edge?

**The friends of friends, the cities within two roads, the shortest route, the dependencies of a task.** The edges of an end give its neighbours; your code walks them, by the Views, and remembers what it met by the identities of the entities (Lessons 6 and 7).

### The whole path

| The question | When the answer is yes | The declaration | Lessons |
| ------------ | ---------------------- | --------------- | ------- |
| Does the relation carry values, belong to neither end, and vanish with either? | An edge | `[Edge(nameof(MA), nameof(MB))]`, its interface starting with `E` | 1, 10 |
| Must the code take it back? | A removable edge | `[Removable]` | 3 |
| Several between one pair? | A multiple edge | `Multiple = true` | 2, 5 |
| Without direction? | A symmetric edge | `Symmetric = true` | 7 |
| Is it born with a link? | A required link of the edge | A `T`, given to `NewX` | 8 |
| Must something keep it? | A strong reference to the edge | `[Strong] EMembership` | 8 |
| Does a question walk several edges? | Your walk, by the Views | `RoadEdges`, `Targets`, an identity | 6, 7 |

## What you learned

- An **edge** is a relation that carries values between two entities that neither owns: `[Edge]`, an interface starting with `E`, born by `NewX` between its two ends, which do not declare it.
- An edge is read **from both ends**: `XEdges`, its other ends, `Targets`, `Sources` or `Others`, and the edge of a pair, `To`, `From` or `With`, found at once.
- An edge is **one per pair**, unless `Multiple`; `GetOrNew` gives the edge of a pair, or a new one.
- An edge **lives with its ends**: removed with either, never retaining them; the code removes it when it is `[Removable]`.
- The edges are **navigated by the Views**, without creating any Body; the Bodies write the few found.
- A **graph** is walked by your code, over the edges of each end: in breadth, or by the shortest route.
- A **symmetric** edge has no direction; an edge has **the links of any entity**, and other entities link to it.
- An edge is **an aggregate of its own**: it changes in parallel with its ends.

[Tutorial Five](Five-Indexes.md) goes further with the indexes and the custom collections.
