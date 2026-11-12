using System.Runtime.InteropServices;
using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Journal
{
    // The second Log of the journal, of Lesson 4: the routes planned for the parcels. A Log holds the Record types it includes; each
    // Log has its Store.

    // The values of this Log alone, first; the others are those of the tutorial.

    internal enum ParcelSize
    {
        Letter,
        Small,
        Large
    }

    /// <summary>An amount of money: never negative.</summary>
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;
    }

    /// <summary>The number of a depot on a route.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VDepotNumber { }

    /// <summary>A note on a route.</summary>
    [ValueObject<GhostString>]
    internal interface VNote { }

    // A struct of your own, stored as its bytes are: its layout declared, explicit, a [FieldOffset] on each field, every byte covered
    // by a field, 32 bytes at most. Here 16 bytes, two doubles.
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal readonly struct Position(double latitude, double longitude)
    {
        [FieldOffset(0)]
        public readonly double Latitude = latitude;

        [FieldOffset(8)]
        public readonly double Longitude = longitude;
    }

    [Record(11, 1, 7)]
    internal interface RRoutePlanned
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        // An enum, a value object: the values of an entity are the values of a Record.
        [Property(2)]
        ParcelSize Size { get; set; }

        [Property(3)]
        VMoney Cost { get; set; }

        // A GhostSpan: an array of values, the numbers of the depots of the route, in the Record; of value objects, as of numbers.
        [Property(4)]
        GhostSpan<DepotNumber> Stops { get; set; }

        [Property(5)]
        Position Destination { get; set; }

        // An Option: a promised day of delivery, or none.
        [Property(6)]
        Option<VInstant> Promised { get; set; }

        // A text value object: a text read in place by the View, without a copy, as a GhostString.
        [Property(7)]
        VNote Note { get; set; }
    }

    [GhostLog(2), IncludeRecordModel(nameof(RRoutePlanned))]
    internal interface MRouteLog
    {
    }

    /// <summary>
    /// Lesson 4: the values of a Record.
    /// <para>
    /// A Record takes the values of an entity: the numbers, the decimals, the dates, the enums, the <c>Option</c>s, the structs of a
    /// declared layout, the texts, and the arrays of values, <c>GhostSpan</c>. It has no link: a Record is a fact, read long after the
    /// entities it names have changed or gone. It names them by their identity instead, a generated <c>*Ref</c>, alone or in a
    /// <c>GhostSpan</c> (Lesson 9).
    /// </para>
    /// <para>
    /// A View reads the values where the Log keeps them: a text of a <c>GhostString</c> as a <c>ReadOnlyGhostString</c>, an array as a
    /// <c>ReadOnlyGhostSpan</c>, without a copy. A subscription that reads millions of Records then allocates nothing for them.
    /// </para>
    /// </summary>
    public sealed class Lesson04TheValuesOfARecord
    {
        private static readonly DateTime Friday = new(2026, 10, 23, 18, 0, 0, DateTimeKind.Utc);

        /// <summary>Every kind of value, written in a Record, then read in its View.</summary>
        [Fact]
        public void WriteAndReadEveryKindOfValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-values", StoreAccessMode.ReadWrite);
            using var log = new RouteLog(store);
            using (var transaction = log.OpenTransaction())
            {
                // An array is given as a C# array, then changed in place by Set, while the Record is detached.
                var route = new RoutePlanned
                {
                    TrackingNumber = "PX-4001",
                    Size = ParcelSize.Large,
                    Cost = 12.90m,
                    Stops = new DepotNumber[] { 69, 21, 75 },
                    Destination = new Position(48.8566, 2.3522),
                    Promised = Instant.From(Friday),
                    Note = "Leave at the reception"
                };
                route.Stops.Set(1, 71);
                route.Add();
                transaction.Commit();
            }

            var routes = Routes(log);
            var planned = Assert.Single(routes);
            Assert.Equal((ParcelSize.Large, 12.90m), (planned.Size, planned.Cost));
            Assert.Equal([69, 71, 75], planned.Stops.ToArray());
            Assert.Equal(2.3522, planned.Destination.Longitude);
            Assert.Equal(Friday, planned.Promised.Get());

            // A text value object, as a ReadOnlyGhostString, compares to a string without a copy; ToString makes the copy, when one is needed.
            Assert.True(planned.Note == "Leave at the reception");
            Assert.StartsWith("Leave", planned.Note.ToString());
        }

        /// <summary>The values not written are their defaults: an Option is absent, an array empty.</summary>
        [Fact]
        public void ReadTheValuesNotWrittenAsTheirDefaults()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-defaults", StoreAccessMode.ReadWrite);
            using var log = new RouteLog(store);
            using (var transaction = log.OpenTransaction())
            {
                new RoutePlanned { TrackingNumber = "PX-4002" }.Add();
                transaction.Commit();
            }
            var planned = Assert.Single(Routes(log));
            Assert.False(planned.Promised.Any());
            Assert.True(planned.Stops.IsEmpty);
            Assert.Equal(ParcelSize.Letter, planned.Size);
        }

        /// <summary>An array of an added Record is frozen as its other values.</summary>
        [Fact]
        public void FreezeTheArraysOfARecordOnceAdded()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-frozen", StoreAccessMode.ReadWrite);
            using var log = new RouteLog(store);
            using var transaction = log.OpenTransaction();
            var route = new RoutePlanned { TrackingNumber = "PX-4003", Stops = new DepotNumber[] { 69, 75 } }.Add();
            Assert.Throws<InvalidOperationException>(() => route.Stops.Set(0, 13));
            Assert.Throws<InvalidOperationException>(() => route.Stops = new DepotNumber[] { 13 });
            Assert.Equal([69, 75], route.Stops.ToArray());
            transaction.Commit();
        }

        // The Views of the routes of the Log, in its order.
        private static List<RoutePlannedView> Routes(RouteLog log)
        {
            var routes = new List<RoutePlannedView>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<RoutePlannedView, List<RoutePlannedView>>(routes, static (_, e, routes) =>
                {
                    routes.Add(e);
                    return LogStoreSubscription.Continue;
                })
                .Run();
            end.Cancel();
            return routes;
        }
    }
}
