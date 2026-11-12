using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T004.Network
{
    using static RoadMapRepositoryCollections;

    /// <summary>
    /// Lesson 6: walk the graph.
    /// <para>
    /// The edges of an end give its neighbours, one road away. A walk of the graph is your code, over these neighbours: the cities
    /// within two roads of Paris, a walk in breadth; the shortest route from Paris to Marseille, Dijkstra's algorithm, which follows
    /// the city of the shortest known distance first. Both read the Views of the cities and of the roads: a walk over a whole map creates no Body. They
    /// remember the cities they met by their identity, <c>CityRef</c>, a value that a <c>HashSet</c> and a <c>Dictionary</c> hold.
    /// </para>
    /// <para>
    /// A rule of the walk is a condition in your code: a route without tolls skips the roads with one.
    /// </para>
    /// </summary>
    public sealed class Lesson06WalkTheGraph
    {
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

        /// <summary>The cities within two roads of Paris: a walk in breadth, a road at a time.</summary>
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

        /// <summary>The shortest route from Paris to Marseille: through Lyon, on two roads with a toll.</summary>
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

        /// <summary>The same route without tolls: longer, through Dijon and Grenoble.</summary>
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

        /// <summary>A route: its cities in their order, and its length.</summary>
        private sealed record Route(List<string> Cities, Kilometers Distance);

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

        private static City NewCity(Name name) => new(CityRef.New()) { Name = name };
    }
}
