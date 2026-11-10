using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSeven.Carrier
{
    using static CarrierLogCollections;
    using static CarrierRepositoryCollections;

    // The second model, of Lessons 9 to 11: the parcels of the carrier, as they are now, and the Log of what happened to them. Each
    // Record names its parcel by its identity, ParcelRef: a Record has no link (Lesson 4).

    internal enum ParcelStatus
    {
        Registered,
        InTransit,
        Delivered
    }

    [Entity(21, 1, 5), Root, Removable, WithIdentity, Enumerable]
    internal interface MParcel
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(2)]
        VName Recipient { get; set; }

        [Property(3)]
        ParcelStatus Status { get; set; }

        [Property(4)]
        VDepot Depot { get; set; }

        [Property(5)]
        int Scans { get; set; }
    }

    [GhostRepository(4), IncludeEntityModel(nameof(MParcel))]
    internal interface MCarrierRepository
    {
    }

    [Record(31, 1, 3)]
    internal interface RParcelRegistered
    {
        // The identity of the parcel, drawn by the command, in the Record: its replay gives the parcel the same identity.
        [Property(1)]
        ParcelRef Parcel { get; set; }

        [Property(2)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(3)]
        VName Recipient { get; set; }
    }

    [Record(32, 1, 2)]
    internal interface RParcelScanned
    {
        [Property(1)]
        ParcelRef Parcel { get; set; }

        [Property(2)]
        VDepot Depot { get; set; }
    }

    [Record(33, 1, 1)]
    internal interface RParcelDelivered
    {
        [Property(1)]
        ParcelRef Parcel { get; set; }
    }

    [GhostLog(3), IncludeRecordModel(nameof(RParcelRegistered)), IncludeRecordModel(nameof(RParcelScanned)), IncludeRecordModel(nameof(RParcelDelivered))]
    internal interface MCarrierLog
    {
    }

    // The commands of the carrier, and the handlers of their Records. A command adds the Record of what happens, then gives its View to
    // the handler of its type, which changes the parcels; a replay gives the same Views to the same handlers (Lesson 10).
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

        // The handlers: what each Record changes in the model, and nothing else.
        public static LogStoreSubscription Apply(ParcelRegisteredView e)
        {
            new Parcel(e.Parcel) { TrackingNumber = e.TrackingNumber, Recipient = e.Recipient, Status = ParcelStatus.Registered, Depot = "" };
            return LogStoreSubscription.Continue;
        }

        public static LogStoreSubscription Apply(ParcelScannedView e)
        {
            var parcel = e.Parcel.Resolve();
            parcel.Status = ParcelStatus.InTransit;
            parcel.Depot = e.Depot;
            parcel.Scans++;
            return LogStoreSubscription.Continue;
        }

        public static LogStoreSubscription Apply(ParcelDeliveredView e)
        {
            e.Parcel.Resolve().Status = ParcelStatus.Delivered;
            return LogStoreSubscription.Continue;
        }

        // The parcels of the model, one line each, in the order of their tracking numbers.
        public static List<string> Snapshot(CarrierRepository repository)
        {
            using var transaction = repository.OpenTransaction(StoreAccessMode.ReadOnly);
            var lines = new List<string>();
            foreach (var parcel in ParcelViewCollection)
                lines.Add($"{parcel.TrackingNumber} {parcel.Status} {parcel.Depot} {parcel.Scans}");
            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        // The Records of the Log, one line each, in its order.
        public static List<string> Read(CarrierLog log)
        {
            var lines = new List<string>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelRegisteredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"registered {e.TrackingNumber}"))
                .Handle<ParcelScannedView, List<string>>(lines, static (_, e, lines) => Add(lines, $"scanned at {e.Depot}"))
                .Handle<ParcelDeliveredView, List<string>>(lines, static (_, _, lines) => Add(lines, "delivered"))
                .Run();
            end.Cancel();
            return lines;
        }

        private static LogStoreSubscription Add(List<string> lines, string line)
        {
            lines.Add(line);
            return LogStoreSubscription.Continue;
        }
    }

    /// <summary>
    /// Lesson 9: a model and its Log.
    /// <para>
    /// The Log of Part 1 told what happened; a model tells how things are now. Both together: each change of the parcels comes with the
    /// Record that tells it, in <b>one transaction</b>. The Store of the Log is chained after the Store of the Repository,
    /// <c>AddChildStore</c>, and the Log after the Repository, <c>Chain</c>: each ReadWrite transaction of the Repository then opens a
    /// transaction of the Log, ambient, and its commit writes the parcels and the Records together, or neither.
    /// </para>
    /// <para>
    /// A command adds its Record, then gives its View to the handler of its type, which changes the model: <c>Apply(e.Add().ToView())</c>.
    /// The handler is the only code that changes the parcels, from the values of the Record only: the Log then holds everything the model
    /// is made of, and a replay rebuilds it (Lesson 10). Tutorial Eight shows the chains of several Stores.
    /// </para>
    /// </summary>
    public sealed class Lesson09AModelAndItsLog
    {
        /// <summary>A command: its Records and the parcels they change, in one commit.</summary>
        [Fact]
        public void CommitTheRecordsAndTheParcelsTogether()
        {
            // The Store of the parcels, then the Store of the Log after it; the Repository, the Log, and the Log chained to the Repository.
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-parcels", StoreAccessMode.ReadWrite);
            using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-journal", StoreAccessMode.ReadWrite));
            using var repository = new CarrierRepository(parcels);
            using var log = new CarrierLog(journal);
            repository.Chain(log);

            using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The transaction of the Log, opened with the one of the Repository: the commands add their Records to it.
                Assert.NotNull(CarrierLogTransaction.Current);
                var parcel = Parcels.Register("PX-9001", "Ada");
                Parcels.Scan(parcel, "Lyon");
                Assert.Equal(2, CarrierCollection.Count());
                transaction.Commit();
            }
            Assert.Equal(["PX-9001 InTransit Lyon 1"], Parcels.Snapshot(repository));
            Assert.Equal(["registered PX-9001", "scanned at Lyon"], Parcels.Read(log));
        }

        /// <summary>A transaction rolled back writes neither the parcels nor the Records.</summary>
        [Fact]
        public void WriteNeitherWithoutACommit()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-rollback", StoreAccessMode.ReadWrite);
            using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-rollback-journal", StoreAccessMode.ReadWrite));
            using var repository = new CarrierRepository(parcels);
            using var log = new CarrierLog(journal);
            repository.Chain(log);

            using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Parcels.Register("PX-9002", "Bob");
                transaction.Rollback();
            }
            Assert.Empty(Parcels.Snapshot(repository));
            Assert.Empty(Parcels.Read(log));
        }

        /// <summary>The Log of a chain opens and commits with its Repository only; a ReadOnly transaction opens no transaction of it.</summary>
        [Fact]
        public void OpenTheLogThroughItsRepositoryOnly()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-chained", StoreAccessMode.ReadWrite);
            using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-chained-journal", StoreAccessMode.ReadWrite));
            using var repository = new CarrierRepository(parcels);
            using var log = new CarrierLog(journal);
            repository.Chain(log);

            // Chained, the Log has no transaction of its own: a Record written alone would tell a change that the model never had.
            Assert.Throws<InvalidOperationException>(() => log.OpenTransaction());
            using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Parcels.Register("PX-9003", "Cy");
                Assert.Throws<InvalidOperationException>(() => CarrierLogTransaction.Current!.Commit());
                transaction.Commit();
            }

            // A ReadOnly transaction reads the parcels, and changes nothing: no Record to add.
            using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Null(CarrierLogTransaction.Current);
                Assert.Equal("Cy", ParcelViewCollection.Single(static parcel => parcel.TrackingNumber == "PX-9003").Recipient);
            }
        }
    }
}
