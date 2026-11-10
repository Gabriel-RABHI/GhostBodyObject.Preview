using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSeven.Journal
{
    using static CounterRepositoryCollections;

    /// <summary>A counter of a depot, where parcels are handed in: a small model that writes in the two Logs of the journal.</summary>
    [Entity(41, 1, 1), Root, Enumerable]
    internal interface MCounter
    {
        [Property(1)]
        VDepot Depot { get; set; }
    }

    [GhostRepository(5), IncludeEntityModel(nameof(MCounter))]
    internal interface MCounterRepository
    {
    }

    /// <summary>
    /// Lesson 12: several Logs on one Store.
    /// <para>
    /// A Store takes several Repositories; a Log Store takes several Logs. Messages of several services, Jobs, Commands: each Log has its Records
    /// and its schema, and they share one journal, in one order. A Record type belongs to one Log, as to one model: two Logs never hold the same
    /// type. A Log of Mutations keeps a Store of its own.
    /// </para>
    /// <para>
    /// One reader of the Store, <c>journal.Replay(from)</c>: each Log registers its handlers on it, <c>On(replay)</c>, and each Record reaches its
    /// handler in the order of the Store, whatever its Log.
    /// </para>
    /// </summary>
    public sealed class Lesson12SeveralLogsOnOneStore
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        // What the reader saw, in its order.
        private static LogStoreSubscription Note(List<string> seen, string text)
        {
            seen.Add(text);
            return LogStoreSubscription.Continue;
        }

        /// <summary>Two Logs written by turns, read as one journal: the order of their commits, whatever the Log.</summary>
        [Fact]
        public void ReadSeveralLogsInTheOrderOfTheirCommits()
        {
            using var journal = TransactionalMemoryStore.NewInMemoryLogStore("lesson-12-journal", StoreAccessMode.ReadWrite);
            using var parcels = new ParcelLog(journal);
            using var routes = new RouteLog(journal);
            using (var transaction = parcels.OpenTransaction())
            {
                new ParcelRegistered { TrackingNumber = "PX-1201", Recipient = "Ada", Grams = 800, Registered = Monday }.Add();
                transaction.Commit();
            }
            using (var transaction = routes.OpenTransaction())
            {
                new RoutePlanned { TrackingNumber = "PX-1201" }.Add();
                transaction.Commit();
            }
            using (var transaction = parcels.OpenTransaction())
            {
                new ParcelScanned { TrackingNumber = "PX-1201", Depot = "Lyon", Scanned = Monday.AddHours(1) }.Add();
                transaction.Commit();
            }

            var seen = new List<string>();
            var replay = journal.Replay(from: 0);
            parcels.On(replay)
                .Handle<ParcelRegisteredView, List<string>>(seen, static (_, e, seen) => Note(seen, $"registered {e.TrackingNumber}"))
                .Handle<ParcelScannedView, List<string>>(seen, static (_, e, seen) => Note(seen, $"scanned {e.TrackingNumber}"));
            routes.On(replay)
                .Handle<RoutePlannedView, List<string>>(seen, static (_, e, seen) => Note(seen, $"planned {e.TrackingNumber}"));
            replay.Run();

            Assert.Equal(["registered PX-1201", "planned PX-1201", "scanned PX-1201"], seen);
        }

        /// <summary>A subscription of one Log still reads its Log alone: the Records of the other one are skipped.</summary>
        [Fact]
        public void ReadOneLogAloneOfTheStore()
        {
            using var journal = TransactionalMemoryStore.NewInMemoryLogStore("lesson-12-alone", StoreAccessMode.ReadWrite);
            using var parcels = new ParcelLog(journal);
            using var routes = new RouteLog(journal);
            using (var transaction = routes.OpenTransaction())
            {
                new RoutePlanned { TrackingNumber = "PX-1202" }.Add();
                transaction.Commit();
            }
            using (var transaction = parcels.OpenTransaction())
            {
                new ParcelScanned { TrackingNumber = "PX-1202", Depot = "Lille", Scanned = Monday }.Add();
                transaction.Commit();
            }

            var seen = new List<string>();
            parcels.SubscribeFrom(0).Handle<ParcelScannedView, List<string>>(seen, static (_, e, seen) => Note(seen, $"scanned {e.TrackingNumber}")).Run();
            Assert.Equal(["scanned PX-1202"], seen);
        }

        /// <summary>
        /// Two Logs chained to one Repository: one commit writes the entities and the Records of both, in the order of their <c>Add</c>, across the Logs.
        /// </summary>
        [Fact]
        public void KeepTheOrderOfTheAddOverSeveralLogs()
        {
            using var counters = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-counters", StoreAccessMode.ReadWrite);
            using var journal = counters.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-12-chained", StoreAccessMode.ReadWrite));
            using var repository = new CounterRepository(counters);
            using var parcels = new ParcelLog(journal);
            using var routes = new RouteLog(journal);
            repository.Chain(parcels);
            repository.Chain(routes);

            using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Counter { Depot = "Lyon" };
                new ParcelRegistered { TrackingNumber = "PX-1203", Recipient = "Bob", Grams = 300, Registered = Monday }.Add();
                new RoutePlanned { TrackingNumber = "PX-1203" }.Add();
                new ParcelScanned { TrackingNumber = "PX-1203", Depot = "Lyon", Scanned = Monday }.Add();
                transaction.Commit();
            }

            var (seen, transactions) = (new List<string>(), new List<ulong>());
            var replay = journal.Replay();
            parcels.On(replay)
                .Handle<ParcelRegisteredView, List<string>>(seen, static (_, e, seen) => Note(seen, $"registered {e.TrackingNumber}"))
                .Handle<ParcelScannedView, List<string>>(seen, static (_, e, seen) => Note(seen, $"scanned {e.TrackingNumber}"))
                .OnTransactionEnd(transactions, static (id, transactions) =>
                {
                    transactions.Add(id);
                    return LogStoreSubscription.Continue;
                });
            routes.On(replay)
                .Handle<RoutePlannedView, List<string>>(seen, static (_, e, seen) => Note(seen, $"planned {e.TrackingNumber}"));
            replay.Run();

            Assert.Equal(["registered PX-1203", "planned PX-1203", "scanned PX-1203"], seen);
            Assert.Single(transactions);
            using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Single(CounterCollection);
        }
    }
}
