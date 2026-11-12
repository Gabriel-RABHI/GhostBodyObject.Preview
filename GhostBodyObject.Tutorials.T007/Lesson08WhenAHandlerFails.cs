using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Journal
{
    /// <summary>
    /// Lesson 8: when a handler fails.
    /// <para>
    /// A handler that throws ends its subscription: the Records after its own are not delivered. During the replay, its exception
    /// propagates from <c>Run</c>; after it, on the thread of the subscription, it faults <c>Completion</c>, which gives it to whoever
    /// waits for the subscription. In both cases, Completion holds it.
    /// </para>
    /// <para>
    /// An exception is a failure, never a way to stop: a subscriber that has what it wants answers <c>Cancel</c> (Lesson 2). After a
    /// failure, the subscriber starts again from the transaction after the last one it completed, as Lesson 7 does: its position moves at
    /// the end of each transaction, never in the middle of one.
    /// </para>
    /// </summary>
    public sealed class Lesson08WhenAHandlerFails
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>A failure in the replay propagates from Run; the subscription ends with it.</summary>
        [Fact]
        public void GiveAFailureOfTheReplayToRun()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-replay", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Scan(log, "PX-8001", "Lyon");

            var subscription = log.SubscribeFrom(0)
                .Handle<ParcelScannedView>(static (_, e) => throw new FormatException($"The depot {e.Depot} is unknown."));
            Assert.Throws<FormatException>(() => subscription.Run());
            Assert.IsType<FormatException>(subscription.Completion.Exception!.InnerException);
        }

        /// <summary>A failure after the replay faults Completion: whoever waits for the subscription receives it.</summary>
        [Fact]
        public async Task FaultTheCompletionOnAFailureAfterTheReplay()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-later", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            var subscription = log.SubscribeFrom(0)
                .Handle<ParcelScannedView>(static (_, e) => e.Depot == "Atlantis" ? throw new FormatException($"The depot {e.Depot} is unknown.") : LogStoreSubscription.Continue)
                .Run();
            Scan(log, "PX-8002", "Lyon");
            Scan(log, "PX-8002", "Atlantis");

            // The thread of the subscription meets the failure; the task of its Completion gives it.
            await Assert.ThrowsAsync<FormatException>(() => subscription.Completion);
        }

        /// <summary>After a failure, the subscriber starts again after the last transaction it completed.</summary>
        [Fact]
        public void StartAgainAfterTheLastTransactionCompleted()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-again", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Scan(log, "PX-8003", "Lyon");
            Scan(log, "PX-8003", "Atlantis");
            Scan(log, "PX-8003", "Paris");

            // The depots known: Atlantis is not one of them yet. The subscription fails at its transaction, after completing the first.
            var tracker = new Tracker(["Lyon", "Paris"]);
            Assert.Throws<FormatException>(() => Follow(log, 0, tracker));
            Assert.Equal(["Lyon"], tracker.Depots);

            // The depot added to the known ones, the subscriber starts again after its position: the transaction that failed, then the others.
            tracker.Known.Add("Atlantis");
            Follow(log, tracker.Position + 1, tracker);
            Assert.Equal(["Lyon", "Atlantis", "Paris"], tracker.Depots);
        }

        private static void Follow(ParcelLog log, ulong from, Tracker tracker)
        {
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(from, end.Token)
                .OnTransactionStart(tracker, static (_, tracker) => tracker.Start())
                .Handle<ParcelScannedView, Tracker>(tracker, static (_, e, tracker) => tracker.Scanned(e.Depot))
                .OnTransactionEnd(tracker, static (transaction, tracker) => tracker.Completed(transaction))
                .Run();
            end.Cancel();
        }

        private static void Scan(ParcelLog log, TrackingNumber trackingNumber, Depot depot)
        {
            using var transaction = log.OpenTransaction();
            new ParcelScanned { TrackingNumber = trackingNumber, Depot = depot, Scanned = Monday }.Add();
            transaction.Commit();
        }

        // The depots that a parcel went through, among the known ones, and the last transaction completed: the position of the subscriber.
        private sealed class Tracker(List<string> known)
        {
            private readonly List<string> _pending = [];

            public List<string> Known => known;

            public List<string> Depots { get; } = [];

            public ulong Position { get; private set; }

            // The start of a transaction: nothing pending, even after a failure in the middle of one.
            public LogStoreSubscription Start()
            {
                _pending.Clear();
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription Scanned(string depot)
            {
                if (!known.Contains(depot))
                    throw new FormatException($"The depot {depot} is unknown.");
                _pending.Add(depot);
                return LogStoreSubscription.Continue;
            }

            // The end of a transaction: its depots kept, and the position moved, together.
            public LogStoreSubscription Completed(ulong transaction)
            {
                Depots.AddRange(_pending);
                _pending.Clear();
                Position = transaction;
                return LogStoreSubscription.Continue;
            }
        }
    }
}
