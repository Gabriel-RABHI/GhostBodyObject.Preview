# Edges: relations that carry values, and graphs

Part of [the modeling knowledge](README.md). An **edge** is an entity of its own, born between two ends that do not declare it, removed with either end, one per pair of ends unless its type allows several, found at once by that pair. A set of edges is a graph.

## When a relation is an edge

Three answers must be yes:

1. **It carries values**: stars, a length, a role, a date. Without values, it is a collection of references and its reverse property.
2. **It belongs to neither end**: neither the critic nor the movie owns a rating. If one end owns it (the lines of an order), it is a part of that end.
3. **It must vanish with either end**: a rating without its movie means nothing. If it must outlive an end, or others designate it as a fact of the business (a transfer, a contract, a booking), it is a Root with a `[Strong]` reference to each end.

| The relation | The link |
|---|---|
| A like, a follow, a tag, a favorite: no value | `[Weak] IEntityCollection<T>` and `[ReverseWeakReference]` |
| A rating with stars, a membership with a role, a stock line with a quantity, a road with a length | An edge |
| The lines of an order | A composition |
| A transfer between two accounts, an order, a booking | A Root with two `[Strong]` references |
| A like that gains a date | It becomes an edge: the collection replaced by the edge type, the reverse property by the members of the edge |

## Declaring an edge

```csharp
[Entity(3, 1, 2)]
[Edge(nameof(MCritic), nameof(MMovie))]                 // from, to; one edge per pair
[Removable]                                             // the code may remove it; without it, it leaves with an end only
[Enumerable]                                            // RatingCollection
internal interface ERating
{
    [Property(1)] int Stars { get; set; }
    [Property(2)] DateTime At { get; set; }
}

[Entity(4, 1, 1)]
[Edge(nameof(MCritic), nameof(MMovie), Multiple = true)] // several edges per pair: a critic sees a movie several times
internal interface EScreening { [Property(1)] DateTime At { get; set; } }

[Entity(2, 1, 2)]
[Edge(nameof(MCity), nameof(MCity), Multiple = true)]   // a graph of one type, directed
[Removable]
internal interface ERoad { [Property(1)] double Distance { get; set; } [Property(2)] bool Toll { get; set; } }

[Entity(2, 1, 1)]
[Edge(nameof(MPerson), nameof(MPerson), Symmetric = true)] // no direction, one per pair, of one type
[Removable]
internal interface EFriendship { [Property(1)] DateTime Since { get; set; } }

[Entity(5, 1, 2), Edge(nameof(MPerson), nameof(MGroup)), Removable]
internal interface EMembership
{
    [Property(1), Strong] MRole Role { get; set; }      // a required link of the edge: a parameter of its birth
    [Property(2)] DateTime Since { get; set; }
}
```

- The interface starts with `E`; `ERating` gives the Body `Rating`. Include it in the Repository.
- An edge is never `[Root]`, `[Orphanable]` nor `[WithIdentity]`, never a component nor a system, owns no parts, is never composed (GBO031).
- Its ends are entity types of the Repository; they declare nothing: a new kind of edge changes nothing of them.

## The generated API

**Birth**, from the first end: `ada.NewRating(dune, stars: 5, at: day)`: the other end, then the required links, then the values in the order of their identifiers (named or positional, optional), or set next. A value object is a value as any: a number of its type, a text of its writable form, written when it is not empty. A birth takes the other end as a Body or a View: `cid.NewRating(alienView, 4)`.

| Form | On the first end (`ada`) | On the second end (`dune`) | On the edge |
|---|---|---|---|
| Two types, one per pair | `ada.RatingEdges`: `Targets` (the movies), `To(dune)` an `Option`, `Contains(dune)`, `GetOrNew(dune, ...)`, `TryGetOrNew(dune, ..., out r)`, `Remove(dune)` if `[Removable]` | `dune.RatingEdges`: `Sources` (the critics), `From(ada)` an `Option` | `rating.Critic`, `rating.Movie` (named after their types); `rating.Remove()` if `[Removable]` |
| `Multiple = true` | `ada.ScreeningEdges.To(dune)` is a sequence; `Targets` lists each movie once | `From(ada)` a sequence | As above |
| One type, directed | `paris.RoadEdges` (leaving): `Targets`, `To(lyon)`; `paris.IncomingRoadEdges` (arriving): `Sources`, `From(lyon)` | | `road.From`, `road.To` |
| `Symmetric = true` | `ada.FriendshipEdges`: `Others`, `With(bob)` from either end | The same member | `friendship.Other(ada)`; `From`, `To` |

- The ends of an edge never change: they have no Setter; a rating of another movie is another rating. Its values change as any entity's.
- **One per pair**: a second `NewRating(dune)` by the same critic is refused before any change. `GetOrNew` gives the edge of the pair, or a new one: one instruction rates a movie the first time and the next ones: `critic.RatingEdges.GetOrNew(movie).Stars = stars;`.
- **An edge lives with its ends**: removing a movie removes its ratings and screenings at that instruction; the other end stays. An edge never retains its ends.
- **An edge has the links of any entity**: a required link `T` is given to its birth after the other end (`ada.NewMembership(club, member)`), and compared by `GetOrNew`/`TryGetOrNew`. Other entities link to an edge: **a strong reference to an edge retains it and its two ends** (a certificate keeps the membership, the person and the group); a weak one forgets it when it goes.

## Navigate by the Views

The View of an end has the same edge members as its Body, giving Views: count, sum, list, find a pair, rank, without creating any Body. Bodies are for the few edges to write.

```csharp
var dune = MovieViewCollection.Single(static m => m.Title == "Dune");
int sum = dune.RatingEdges.Sum(static r => r.Stars);
var ranking = MovieViewCollection
    .Select(static m => (m.Title, Average: (double)m.RatingEdges.Sum(static r => r.Stars) / m.RatingEdges.Count()))
    .OrderBy(static m => -m.Average).ToList();

foreach (var empty in dune.RatingEdges.Where(1, static (r, least) => r.Stars < least).ToList())
    empty.ToBody().Remove();                       // the few to write
```

## Walk a graph

A walk is your code, over the edges of each end, by the Views; remember what you met by the identities of the ends (a `*Ref` is a value a `HashSet` or a `Dictionary` holds), so the ends are `[WithIdentity]`.

```csharp
// Breadth: the cities within `roads` roads of a city.
static List<string> Reachable(CityView from, int roads)
{
    var met = new HashSet<CityRef> { from.Ref };
    var frontier = new List<CityView> { from };
    var reached = new List<string>();
    for (int step = 0; step < roads; step++)
    {
        var next = new List<CityView>();
        foreach (var city in frontier)
            foreach (var target in city.RoadEdges.Targets)
                if (met.Add(target.Ref))
                {
                    next.Add(target);
                    reached.Add(target.Name.ToString());
                }
        frontier = next;
    }
    return reached;
}
```

The shortest route is Dijkstra over `city.RoadEdges`, its distances and `road.To`, with a `PriorityQueue<CityView, double>` and `Dictionary<CityRef, double>`: a walk over a whole map creates no Body. A rule of the walk (avoid tolls) is a condition in your code. The friends of friends are two steps of `FriendshipEdges.Others`.

## Edges and concurrency

- **An edge is an aggregate of its own**: changing the date of a friendship and renaming one of the friends are two aggregates; both commit.
- **Two transactions that create the edge of one pair conflict** at the second commit: a pair never ends with two edges. Written with `GetOrNew`, the retry runs the same code and finds the edge.

## The path from the need to the link

| The question | Yes → | Declaration |
|---|---|---|
| Does the relation carry values, belong to neither end, and vanish with either? | An edge | `[Edge(nameof(MA), nameof(MB))]`, `EName` |
| Must the code take it back? | A removable edge | `[Removable]` |
| Several between one pair? | A multiple edge | `Multiple = true` |
| Without direction? | A symmetric edge | `Symmetric = true`, between one type |
| Is it born with a link? | A required link of the edge | A `T`, given to `NewX` |
| Must something keep it? | A strong reference to the edge | `[Strong] EMembership Membership` |
| Does a question walk several edges? | Your walk, by the Views | `XEdges`, `Targets`, an identity |
