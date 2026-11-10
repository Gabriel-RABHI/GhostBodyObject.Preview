using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialEight.Carrier
{
    using static CarrierRepositoryCollections;
    using static TrackingRepositoryCollections;

    // The Log of the carrier (Tutorial Seven), the last Store of the chain: what happened to the parcels.

    [Record(21, 1, 3)]
    internal interface RParcelRegistered
    {
        [Property(1)]
        ParcelRef Parcel { get; set; }

        [Property(2)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(3)]
        VName Recipient { get; set; }
    }

    [Record(22, 1, 2)]
    internal interface RParcelScanned
    {
        [Property(1)]
        ParcelRef Parcel { get; set; }

        [Property(2)]
        VDepot Depot { get; set; }
    }

    [Record(23, 1, 1)]
    internal interface RParcelDelivered
    {
        [Property(1)]
        ParcelRef Parcel { get; set; }
    }

    [GhostLog(4), IncludeRecordModel(nameof(RParcelRegistered)), IncludeRecordModel(nameof(RParcelScanned)), IncludeRecordModel(nameof(RParcelDelivered))]
    internal interface MCarrierLog
    {
    }

    // The commands of the carrier and the handlers of their Records (Tutorial Seven, Lesson 9). Each handler changes both models, the
    // parcels and their tracking: in the ambient transactions of the chain, at a command and at a replay alike.
    internal static class Parcels
    {
        public static ParcelRef Register(TrackingNumber trackingNumber, Name recipient)
        {
            var registered = new ParcelRegistered { Parcel = ParcelRef.New(), TrackingNumber = trackingNumber, Recipient = recipient }.Add();
            Apply(registered.ToView());
            return registered.Parcel;
        }

        public static void Scan(ParcelRef parcel, Depot depot) => Apply(new ParcelScanned { Parcel = parcel, Depot = depot }.Add().ToView());

        public static void Deliver(ParcelRef parcel) => Apply(new ParcelDelivered { Parcel = parcel }.Add().ToView());

        public static LogStoreSubscription Apply(ParcelRegisteredView e)
        {
            new Parcel(e.Parcel) { TrackingNumber = e.TrackingNumber, Recipient = e.Recipient, Status = ParcelStatus.Registered, Depot = "" };
            new TrackingPage(TrackingPageRef.Of(e.Parcel)) { TrackingNumber = e.TrackingNumber, Recipient = e.Recipient, Status = "Registered" };
            return LogStoreSubscription.Continue;
        }

        public static LogStoreSubscription Apply(ParcelScannedView e)
        {
            var parcel = e.Parcel.Resolve();
            Leave(parcel.Depot);
            parcel.Status = ParcelStatus.InTransit;
            parcel.Depot = e.Depot;
            // The board of the depot, or a new one at the first parcel there.
            var board = DepotBoardCollection.FirstOrNone(e.Depot, static (board, depot) => board.Depot == depot).TryGet(out var found) ? found : new DepotBoard { Depot = e.Depot };
            board.Parcels++;
            var page = TrackingPageRef.Of(e.Parcel).Resolve();
            page.Status = $"In transit at {e.Depot}";
            page.Steps++;
            return LogStoreSubscription.Continue;
        }

        public static LogStoreSubscription Apply(ParcelDeliveredView e)
        {
            var parcel = e.Parcel.Resolve();
            Leave(parcel.Depot);
            parcel.Status = ParcelStatus.Delivered;
            parcel.Depot = "";
            var page = TrackingPageRef.Of(e.Parcel).Resolve();
            page.Status = "Delivered";
            page.Steps++;
            return LogStoreSubscription.Continue;
        }

        // A parcel leaves its depot, if it is at one: one parcel fewer on its board.
        private static void Leave(Depot depot)
        {
            if (depot.Length > 0)
                DepotBoardCollection.Single(depot, static (board, depot) => board.Depot == depot).Parcels--;
        }

        // The pages and the boards of the tracking, one line each, in order.
        public static List<string> Tracking()
        {
            var lines = new List<string>();
            foreach (var page in TrackingPageViewCollection)
                lines.Add($"{page.TrackingNumber}: {page.Status}, {page.Steps} steps");
            foreach (var board in DepotBoardViewCollection)
                lines.Add($"{board.Depot}: {board.Parcels}");
            lines.Sort(StringComparer.Ordinal);
            return lines;
        }
    }

    /// <summary>
    /// Lesson 4: the chain and its Log.
    /// <para>
    /// A chain may hold any count of Repositories, and end with a Log: <c>parcels.AddChildStore(tracking).AddChildStore(journal)</c>,
    /// then <c>carrier.Chain(site)</c> and <c>site.Chain(log)</c>. A transaction of the chain then opens the three, ambient: the commands
    /// add their Records to the Log, and their handlers change both models, in one commit of the three Stores.
    /// </para>
    /// <para>
    /// The Log is the memory of the chain: with it, the models may live in memory, rebuilt at each opening by a replay (Tutorial Seven,
    /// Lesson 10). One replay gives both models again, the parcels and their tracking: the handlers write them in the Replay transactions
    /// of the chain, as they write them at a command.
    /// </para>
    /// </summary>
    public sealed class Lesson04TheChainAndItsLog
    {
        /// <summary>A command: its Records, the parcels and the tracking, in one commit of the three Stores.</summary>
        [Fact]
        public void CommitACommandInTheThreeStores()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-parcels", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-tracking", StoreAccessMode.ReadWrite));
            using var journal = tracking.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-journal", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            using var log = new CarrierLog(journal);
            carrier.Chain(site);
            site.Chain(log);

            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.NotNull(TrackingRepositoryTransaction.Current);
                Assert.NotNull(CarrierLogTransaction.Current);
                var ada = Parcels.Register("PX-3001", "Ada");
                Parcels.Scan(ada, "Lyon");
                Parcels.Scan(ada, "Paris");
                transaction.Commit();
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Paris", ParcelViewCollection.Single(static parcel => parcel.TrackingNumber == "PX-3001").Depot);
                Assert.Equal(["Lyon: 0", "PX-3001: In transit at Paris, 2 steps", "Paris: 1"], Parcels.Tracking());
            }
        }

        /// <summary>Both models in memory, the Log on disk: one replay rebuilds both at each opening.</summary>
        [Fact]
        public void ReplayBothModelsFromTheLog()
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-04-journal");
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            List<string> expected;
            using (var chain = new Chain(directory))
            {
                ParcelRef ada, bob;
                using (var transaction = chain.Carrier.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    (ada, bob) = (Parcels.Register("PX-3002", "Ada"), Parcels.Register("PX-3003", "Bob"));
                    Parcels.Scan(ada, "Lyon");
                    Parcels.Scan(bob, "Lyon");
                    transaction.Commit();
                }
                using (var transaction = chain.Carrier.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    Parcels.Deliver(bob);
                    transaction.Commit();
                }
                using (chain.Carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                    expected = Parcels.Tracking();
            }
            Assert.Equal(["Lyon: 1", "PX-3002: In transit at Lyon, 1 steps", "PX-3003: Delivered, 2 steps"], expected);

            // Opened again: both models empty, behind the Log; one replay, through the last transaction of the Log.
            using (var chain = new Chain(directory))
            {
                Assert.True(chain.Parcels.IsBehindLog);
                using (var replay = chain.Carrier.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
                {
                    chain.Log.SubscribeFrom(0)
                        .Handle<ParcelRegisteredView>(static (_, e) => Parcels.Apply(e))
                        .Handle<ParcelScannedView>(static (_, e) => Parcels.Apply(e))
                        .Handle<ParcelDeliveredView>(static (_, e) => Parcels.Apply(e))
                        .OnReplayEnd(replay, static (last, replay) =>
                        {
                            replay.Commit(last);
                            return LogStoreSubscription.Cancel;
                        })
                        .Run();
                }
                Assert.False(chain.Parcels.IsBehindLog);
                using (chain.Carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                    Assert.Equal(expected, Parcels.Tracking());
            }
        }

        // The chain of the lesson: the two models in memory, the Log on disk, after them.
        private sealed class Chain : IDisposable
        {
            public Chain(string directory)
            {
                Parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-replay-parcels", StoreAccessMode.ReadWrite);
                Tracking = Parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-replay-tracking", StoreAccessMode.ReadWrite));
                Journal = Tracking.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
                Carrier = new CarrierRepository(Parcels);
                Site = new TrackingRepository(Tracking);
                Log = new CarrierLog(Journal);
                Carrier.Chain(Site);
                Site.Chain(Log);
            }

            public TransactionalMemoryStore Parcels { get; }

            public TransactionalMemoryStore Tracking { get; }

            public TransactionalMemoryStore Journal { get; }

            public CarrierRepository Carrier { get; }

            public TrackingRepository Site { get; }

            public CarrierLog Log { get; }

            public void Dispose()
            {
                Log.Dispose();
                Site.Dispose();
                Carrier.Dispose();
                Parcels.Dispose();
                Tracking.Dispose();
                Journal.Dispose();
            }
        }
    }
}
