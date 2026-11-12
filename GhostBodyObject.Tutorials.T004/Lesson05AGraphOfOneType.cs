using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T004.Network
{
    using static RoadMapRepositoryCollections;

    // The model of Lessons 5 and 6: a road map. A road goes from a city to a city, in one direction: a city has the roads that
    // leave it and those that arrive at it. Two cities may be linked by several roads, a highway and a national road. A city has an
    // identity (Tutorial T002, Lesson 19): the walks of Lesson 6 remember the cities they met by it.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>A distance, in kilometers: never negative, added along a route.</summary>
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

    // An edge between two entities of one type: its ends are named From and To.
    /// <summary>A road, in one direction; several between two cities.</summary>
    [Entity(2, 1, 2), Edge(nameof(MCity), nameof(MCity), Multiple = true), Removable, Enumerable]
    internal interface ERoad
    {
        [Property(1)]
        VKilometers Distance { get; set; }

        [Property(2)]
        RoadKind Kind { get; set; }
    }

    [GhostRepository(2), IncludeEntityModel(nameof(MCity)), IncludeEntityModel(nameof(ERoad))]
    internal interface MRoadMapRepository
    {
    }

    /// <summary>
    /// Lesson 5: a graph of one type.
    /// <para>
    /// The roads of a road map link cities to cities: an edge whose two ends are of one type, <c>[Edge(nameof(MCity), nameof(MCity))]</c>.
    /// Its ends are named <c>From</c> and <c>To</c>. A city has two members: <c>RoadEdges</c>, the roads that leave it, whose other
    /// ends are their <c>Targets</c>; <c>IncomingRoadEdges</c>, those that arrive at it, whose other ends are their <c>Sources</c>.
    /// </para>
    /// <para>
    /// A road goes in one direction: a way back is another road. Two cities may be linked by several roads, <c>Multiple = true</c>:
    /// <c>paris.RoadEdges.To(lyon)</c> is then the sequence of the roads from Paris to Lyon, and the operators choose among them.
    /// </para>
    /// </summary>
    public sealed class Lesson05AGraphOfOneType
    {
        /// <summary>A one-way road: it leaves Paris, and arrives at Lyon.</summary>
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

        /// <summary>A way back is another road: each city has one road that leaves it, and one that arrives.</summary>
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

        /// <summary>A highway and a national road from Paris to Lyon: the sequence of the pair, and the road chosen among them.</summary>
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

        private static City NewCity(Name name) => new(CityRef.New()) { Name = name };
    }
}
