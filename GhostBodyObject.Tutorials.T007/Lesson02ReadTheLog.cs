using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Journal
{
    /// <summary>
    /// Lesson 2: read the Log.
    /// <para>
    /// A Log is read by a <b>subscription</b>: <c>log.SubscribeFrom(transaction)</c>, then a handler for each Record type to read, then
    /// <c>Run()</c>. Run delivers, on the calling thread, every transaction committed from the one asked, 0 for the first: each Record to
    /// the handler of its type, in the order of the Log, as a View, the same View as for an entity. A Record of a type without a handler
    /// is skipped. The handler answers <c>LogStoreSubscription.Continue</c> for the next Record, or <c>Cancel</c> to stop there.
    /// </para>
    /// <para>
    /// A handler captures nothing: what it needs, a list here, is passed before it, its state, as the static lambdas of the queries of
    /// Tutorial T001. The subscription keeps the handler and its state, and calls it for each Record without allocating.
    /// </para>
    /// <para>
    /// After Run, the subscription stays active, and delivers each new commit on a thread of its own (Lesson 6): until its handler
    /// cancels it, or until the token given to <c>SubscribeFrom</c> is cancelled. A subscription that only reads what is there cancels its
    /// token after Run.
    /// </para>
    /// </summary>
    public sealed class Lesson02ReadTheLog
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>Every Record, to the handler of its type, in the order of the Log, with the number of its transaction.</summary>
        [Fact]
        public void DeliverEachRecordInTheOrderOfTheLog()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-order", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Journey(log);

            var lines = new List<string>();
            var transactions = new List<ulong>();
            using var end = new CancellationTokenSource();
            // Each handler takes the number of the transaction of its Record, the View of the Record, then its state: two values here.
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelRegisteredView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
                    Add(lines, transactions, transaction, $"{e.TrackingNumber} registered for {e.Recipient}"))
                .Handle<ParcelScannedView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
                    Add(lines, transactions, transaction, $"{e.TrackingNumber} scanned at {e.Depot}"))
                .Handle<ParcelDeliveredView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
                    Add(lines, transactions, transaction, $"{e.TrackingNumber} delivered"))
                .Run();
            end.Cancel();

            Assert.Equal(["PX-2001 registered for Ada", "PX-2001 scanned at Lyon", "PX-2001 scanned at Paris", "PX-2001 delivered"], lines);
            // The two Records of the first commit share its number; each commit after it has a greater one.
            Assert.True(transactions[0] == transactions[1] && transactions[1] < transactions[2] && transactions[2] < transactions[3]);
        }

        /// <summary>A Record of a type without a handler is skipped: a subscription reads only the types it handles.</summary>
        [Fact]
        public void SkipTheRecordsOfATypeWithoutHandler()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-skip", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Journey(log);

            var depots = new List<string>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, List<string>>(depots, static (_, e, depots) =>
                {
                    depots.Add(e.Depot);
                    return LogStoreSubscription.Continue;
                })
                .Run();
            end.Cancel();
            Assert.Equal(["Lyon", "Paris"], depots);
        }

        /// <summary>A handler that answers Cancel ends the subscription: the Records after its own are not delivered.</summary>
        [Fact]
        public void StopAtTheCancelOfAHandler()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-cancel", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Journey(log);

            // The first depot of the parcel: the subscription stops there, without a token to cancel.
            var first = new List<string>();
            var subscription = log.SubscribeFrom(0)
                .Handle<ParcelScannedView, List<string>>(first, static (_, e, first) =>
                {
                    first.Add(e.Depot);
                    return LogStoreSubscription.Cancel;
                })
                .Run();
            Assert.Equal(["Lyon"], first);

            // Completion: the end of the subscription, as a task. A handler that cancels ends it successfully.
            Assert.True(subscription.Completion.IsCompletedSuccessfully);
        }

        /// <summary>The View of a Record stays readable after its delivery, for as long as you hold it: it is a value (Tutorial T006, Lesson 3).</summary>
        [Fact]
        public void KeepAViewAfterItsDelivery()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-view", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Journey(log);

            var scans = new List<ParcelScannedView>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, List<ParcelScannedView>>(scans, static (_, e, scans) =>
                {
                    scans.Add(e);
                    return LogStoreSubscription.Continue;
                })
                .Run();
            end.Cancel();

            // The Views read the Records where the Log keeps them: nothing was copied.
            Assert.Equal(["Lyon", "Paris"], scans.Select(static scan => scan.Depot.ToString()));
            Assert.Equal(TimeSpan.FromHours(5), scans[1].Scanned - scans[0].Scanned);
        }

        // The journey of a parcel, in three commits: registered and scanned, scanned again, delivered.
        private static void Journey(ParcelLog log)
        {
            using (var transaction = log.OpenTransaction())
            {
                new ParcelRegistered { TrackingNumber = "PX-2001", Recipient = "Ada", Grams = 800, Registered = Monday }.Add();
                new ParcelScanned { TrackingNumber = "PX-2001", Depot = "Lyon", Scanned = Monday.AddHours(1) }.Add();
                transaction.Commit();
            }
            using (var transaction = log.OpenTransaction())
            {
                new ParcelScanned { TrackingNumber = "PX-2001", Depot = "Paris", Scanned = Monday.AddHours(6) }.Add();
                transaction.Commit();
            }
            using (var transaction = log.OpenTransaction())
            {
                new ParcelDelivered { TrackingNumber = "PX-2001", Delivered = Monday.AddHours(9) }.Add();
                transaction.Commit();
            }
        }

        private static LogStoreSubscription Add(List<string> lines, List<ulong> transactions, ulong transaction, string line)
        {
            lines.Add(line);
            transactions.Add(transaction);
            return LogStoreSubscription.Continue;
        }
    }
}
