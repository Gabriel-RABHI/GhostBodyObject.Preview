using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has. A value object is a value of a Record as of an entity.

    /// <summary>The tracking number of a parcel, "PA-1": a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VTrackingNumber { }

    /// <summary>The name of a person, the recipient of a parcel.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>The name of a depot of the carrier.</summary>
    [ValueObject<GhostString>]
    internal interface VDepot { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    internal partial struct Instant
    {
        /// <summary>The time between two moments: an operator of yours, beside those that GBO writes.</summary>
        public static TimeSpan operator -(Instant later, Instant earlier) => later.Value - earlier.Value;
    }
}

namespace Tutorials.T007.Journal
{
    using static ParcelLogCollections;

    // The first model, of Lessons 1 to 8: the journal of a parcel carrier, what happened to each parcel, in the order it happened.
    // No entity: Records only, and the Log that holds them.

    /// <summary>The weight of a parcel, in grams: a value of this model alone.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VGrams { }

    internal partial struct Grams
    {
        private static bool IsValid(int grams) => grams >= 0;
    }

    // [Record(type, version, maxPropertyIdentifier)]: a Record type, as [Entity] declares an entity type. Its number is stable: the
    // Log stores it with each Record. The interface starts with R: RParcelRegistered gives the class ParcelRegistered.
    [Record(1, 1, 4)]
    internal interface RParcelRegistered
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(2)]
        VName Recipient { get; set; }

        [Property(3)]
        VGrams Grams { get; set; }

        [Property(4)]
        VInstant Registered { get; set; }
    }

    [Record(2, 1, 3)]
    internal interface RParcelScanned
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        // The depot where the parcel was scanned.
        [Property(2)]
        VDepot Depot { get; set; }

        [Property(3)]
        VInstant Scanned { get; set; }
    }

    [Record(3, 1, 2)]
    internal interface RParcelDelivered
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(2)]
        VInstant Delivered { get; set; }
    }

    // [GhostLog]: a Log, and the Record types it holds, each by [IncludeRecordModel]. MParcelLog gives the class ParcelLog, opened on
    // a Store of its own, and ParcelLogCollections.ParcelCollection, the Records of its transaction.
    [GhostLog(1), IncludeRecordModel(nameof(RParcelRegistered)), IncludeRecordModel(nameof(RParcelScanned)), IncludeRecordModel(nameof(RParcelDelivered))]
    internal interface MParcelLog
    {
    }

    /// <summary>
    /// Lesson 1: declare a Record and a Log.
    /// <para>
    /// The tutorials before kept the state of their data: a book, its title and its year, as they are now, and at most a short history in
    /// a bag of its entity. A <b>Log</b> keeps everything that happened: a parcel registered, then scanned at a depot, then delivered, each
    /// fact a <b>Record</b>, appended after the others and never changed. A Record is declared as in Tutorial T006; the Log by an interface
    /// with <c>[GhostLog]</c>, which includes its Record types.
    /// </para>
    /// <para>
    /// A Log opens on a Store of its own, a Log Store, in memory or on disk (Lesson 5). Its transactions append: the Records added in a
    /// transaction are appended to the Log at its commit, together, after the Records of the commits before; a transaction that does not
    /// commit appends nothing. Nothing else: a Log has no query, no index, no change and no removal. It is read from a point onwards, by a
    /// subscription (Lesson 2).
    /// </para>
    /// </summary>
    public sealed class Lesson01DeclareARecordAndALog
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>Records added in a transaction, then committed: the Log holds them.</summary>
        [Fact]
        public void AppendTheRecordsOfACommit()
        {
            // A Log Store in memory, and the Log of the model on it.
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-append", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);

            // A transaction of the Log, ambient as the transactions of a Repository: a Record created then added, Add(), belongs to it.
            using (var transaction = log.OpenTransaction())
            {
                new ParcelRegistered { TrackingNumber = "PX-1001", Recipient = "Ada", Grams = 1200, Registered = Monday }.Add();
                new ParcelScanned { TrackingNumber = "PX-1001", Depot = "Lyon", Scanned = Monday.AddHours(3) }.Add();
                transaction.Commit();
            }

            // The Log holds both Records, in the order they were added. Lesson 2 shows how it is read.
            Assert.Equal(["registered PX-1001", "scanned PX-1001 at Lyon"], Read(log));
        }

        /// <summary>The Records of a transaction, until its commit; a transaction that does not commit appends nothing.</summary>
        [Fact]
        public void AppendNothingWithoutACommit()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-nothing", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            using (var transaction = log.OpenTransaction())
            {
                var registered = new ParcelRegistered { TrackingNumber = "PX-1002", Recipient = "Bob", Grams = 300, Registered = Monday }.Add();
                var scanned = new ParcelScanned { TrackingNumber = "PX-1002", Depot = "Lyon", Scanned = Monday.AddHours(1) }.Add();

                // ParcelCollection: the Records added in this transaction, in the order of their Add. The Log has none of them yet.
                Assert.Equal<IParcelRecord>([registered, scanned], ParcelCollection.ToArray());
                Assert.Empty(Read(log));
                transaction.Rollback();
            }

            // A transaction disposed without a commit is rolled back, as a transaction of a Repository.
            using (log.OpenTransaction())
                new ParcelDelivered { TrackingNumber = "PX-1002", Delivered = Monday.AddDays(1) }.Add();
            Assert.Empty(Read(log));
        }

        /// <summary>A Record is added in a transaction of its Log, one transaction at a time on a thread.</summary>
        [Fact]
        public void AddARecordInATransactionOnly()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-transaction", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);

            // Without a transaction, a Record has no Log to go to.
            var lost = new ParcelDelivered { TrackingNumber = "PX-1003", Delivered = Monday };
            Assert.Throws<InvalidOperationException>(() => lost.Add());

            // One transaction of the Log at a time on a thread, as for a Repository.
            using (log.OpenTransaction())
                Assert.Throws<InvalidOperationException>(() => log.OpenTransaction());
        }

        // Every Record of the Log, from its first transaction, as a line of text (Lesson 2).
        private static List<string> Read(ParcelLog log)
        {
            var lines = new List<string>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelRegisteredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"registered {e.TrackingNumber}"))
                .Handle<ParcelScannedView, List<string>>(lines, static (_, e, lines) => Add(lines, $"scanned {e.TrackingNumber} at {e.Depot}"))
                .Handle<ParcelDeliveredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"delivered {e.TrackingNumber}"))
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
}
