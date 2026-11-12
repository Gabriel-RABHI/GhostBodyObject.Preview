# Tutorial T004: Edges and graph oriented model

The fourth level of the tutorials of GBO, for a developer who did [Tutorial T003](../GhostBodyObject.Tutorials.T003/README.md). It shows the edges: the relations that carry values between two entities that neither owns, removed with either, one per pair or several; how a graph of edges is navigated by the Views, walked by your code, from a road to the shortest route; and when a relation is an edge rather than a reference.

The domain is a site of movie reviews, a road map, a social network, then a forum and a bank: critics, movies, their ratings and their screenings; cities and their roads; people, their friendships and their memberships in groups; posts, likes, reactions and transfers. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. A model is declared at the top of the first lesson that uses it, in a namespace of its own. Every value of a model is a value object: those that the models share are declared at the head of the first one, in the namespace of the tutorial, the others at the head of their model.

Run them with:

```bash
dotnet test GhostBodyObject.Tutorials.T004
```

## Edges

| Lesson | Model | What it shows |
|---|---|---|
| [1. Relate two entities with values](Lesson01RelateTwoEntitiesWithValues.cs) | `Reviews` | `[Edge]`, an interface starting with `E`; `NewRating(movie, stars)`; the ends of the edge, `rating.Critic`, `rating.Movie`; `RatingEdges` on both ends, `Targets`, `Sources`, `To`, `From`, `Contains`; its values changed |
| [2. One edge per pair, or several](Lesson02OnePerPairOrSeveral.cs) | `Reviews` | A second rating of a pair refused; `GetOrNew`, the rating of the pair or a new one; `Multiple`, several screenings of one pair |
| [3. An edge lives with its ends](Lesson03AnEdgeLivesWithItsEnds.cs) | `Reviews` | `[Removable]`: `Remove()`, `Remove(other)`; the edges removed with either end, never the reverse; an edge without `[Removable]`, the history |
| [4. Navigate by the Views](Lesson04NavigateByTheViews.cs) | `Reviews` | The edges of the View of an end: counted, summed, listed, found by the pair; every movie ranked without a Body; the few written by their Body; a View as the target of a birth |

## Graphs

| Lesson | Model | What it shows |
|---|---|---|
| [5. A graph of one type](Lesson05AGraphOfOneType.cs) | `Network` | An edge between two cities: `From` and `To`; `RoadEdges` and `IncomingRoadEdges`; one direction; several roads between two cities |
| [6. Walk the graph](Lesson06WalkTheGraph.cs) | `Network` | Your algorithms over the edges, by the Views: the cities within two roads, a walk in breadth; the shortest route, Dijkstra's algorithm; a route without tolls |

## The graph oriented model

| Lesson | Model | What it shows |
|---|---|---|
| [7. A symmetric relation](Lesson07ASymmetricRelation.cs) | `Social` | `Symmetric`: `FriendshipEdges`, `Others`, `With`, `Other`; one per pair; the friends of friends, the people to know |
| [8. The links of an edge](Lesson08TheLinksOfAnEdge.cs) | `Social` | A required link given to the birth, compared by `TryGetOrNew`; a strong reference to an edge retains it and its ends; a weak one forgets it |
| [9. Edges and concurrency](Lesson09EdgesAndConcurrency.cs) | `Social` | An edge is an aggregate of its own: it changes in parallel with its ends; two transactions that make the same pair conflict, and the retry finds the edge |
| [10. An edge, or a reference](Lesson10AnEdgeOrAReference.cs) | `Choices` | The three questions: a like, a collection of references; a reaction, an edge; a transfer, a Root that retains its accounts |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
