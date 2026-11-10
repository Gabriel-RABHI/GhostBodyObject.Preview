using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSeven.Journal
{
    /// <summary>
    /// Lesson 6: follow the Log.
    /// <para>
    /// A subscription does not stop at the end of the Log. Run delivers, on the calling thread, every transaction committed until then:
    /// the <b>replay</b>. <c>OnReplayEnd</c> is called once it is done, before Run returns, with the number of the last transaction
    /// found: the subscriber has caught up with the Log. The subscription then stays active, and delivers each new commit on a thread of
    /// its own, at once: a screen, a cache, a report, a service, kept up to date by the Log, without a poll and without a query.
    /// </para>
    /// <para>
    /// Its handlers are never called together: the replay and each delivery after it are delivered one at a time. A subscription ends
    /// when a handler answers <c>Cancel</c> (Lesson 2), when its token is cancelled, or when its Log is disposed; <c>Completion</c> tells
    /// how it ended.
    /// </para>
    /// </summary>
    public sealed class Lesson06FollowTheLog
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>A board of the parcels, kept up to date by a subscription: the Log as it was, then each new commit.</summary>
        [Fact]
        public void DeliverTheNewCommitsAfterTheReplay()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-follow", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Scan(log, "PX-6001", "Lyon");

            // The board is the state of the handlers: the last depot of each parcel, and the end of the replay.
            var board = new Board();
            using var end = new CancellationTokenSource();
            var subscription = log.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
                .Handle<ParcelDeliveredView, Board>(board, static (_, e, board) => board.Delivered(e.TrackingNumber))
                .OnReplayEnd(board, static (last, board) => board.Ready(last))
                .Run();

            // Run returns after the replay: the board knows the Log as it was, and the number of its last transaction.
            Assert.True(board.ReadyAt > 0);
            Assert.Equal("Lyon", board.Depots["PX-6001"]);

            // A new commit: the subscription delivers it on its own thread. To read the board at a known point, a test delivers on its
            // thread what the subscription has not received yet, after the delivery in progress, if any; an application waits for nothing.
            Scan(log, "PX-6001", "Paris");
            Scan(log, "PX-6002", "Lille");
            using (var transaction = log.OpenTransaction())
            {
                new ParcelDelivered { TrackingNumber = "PX-6002", Delivered = Monday.AddHours(4) }.Add();
                transaction.Commit();
            }
            log.Store.DeliverTransactions(subscription);
            Assert.Equal(["PX-6001 at Paris"], board.Depots.Select(static depot => $"{depot.Key} at {depot.Value}"));

            // The token cancelled: the subscription ends, its Completion cancelled; the commits after it reach it no more.
            end.Cancel();
            Assert.True(subscription.Completion.IsCanceled);
            Scan(log, "PX-6001", "Nice");
            Assert.Equal("Paris", board.Depots["PX-6001"]);
        }

        /// <summary>The replay of an empty Log ends at once, with the transaction 0; the first commit is delivered after it.</summary>
        [Fact]
        public void EndTheReplayOfAnEmptyLog()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-empty", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            var board = new Board();
            using var end = new CancellationTokenSource();
            var subscription = log.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
                .OnReplayEnd(board, static (last, board) => board.Ready(last))
                .Run();
            Assert.Equal((true, 0ul), (board.IsReady, board.ReadyAt));

            Scan(log, "PX-6003", "Lyon");
            log.Store.DeliverTransactions(subscription);
            Assert.Equal("Lyon", board.Depots["PX-6003"]);
            end.Cancel();
        }

        /// <summary>A Log disposed ends its subscriptions; a token cancelled before Run delivers nothing.</summary>
        [Fact]
        public void EndWithItsLogOrBeforeItsStart()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-end", StoreAccessMode.ReadWrite);
            var log = new ParcelLog(store);
            Scan(log, "PX-6004", "Lyon");

            var board = new Board();
            var cancelled = log.SubscribeFrom(0, new CancellationToken(canceled: true))
                .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
                .Run();
            Assert.Empty(board.Depots);
            Assert.True(cancelled.Completion.IsCanceled);

            // Without a token, the subscription follows the Log until the Log is disposed: it ends then, successfully.
            var following = log.SubscribeFrom(0)
                .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
                .Run();
            Assert.False(following.Completion.IsCompleted);
            log.Dispose();
            Assert.True(following.Completion.IsCompletedSuccessfully);
        }

        private static void Scan(ParcelLog log, TrackingNumber trackingNumber, Depot depot)
        {
            using var transaction = log.OpenTransaction();
            new ParcelScanned { TrackingNumber = trackingNumber, Depot = depot, Scanned = Monday }.Add();
            transaction.Commit();
        }

        // The board of the depots: the projection of the Log that the subscription keeps, written by its handlers only.
        private sealed class Board
        {
            public Dictionary<string, string> Depots { get; } = [];

            public bool IsReady { get; private set; }

            public ulong ReadyAt { get; private set; }

            public LogStoreSubscription Scanned(string trackingNumber, string depot)
            {
                Depots[trackingNumber] = depot;
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription Delivered(string trackingNumber)
            {
                Depots.Remove(trackingNumber);
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription Ready(ulong last)
            {
                (IsReady, ReadyAt) = (true, last);
                return LogStoreSubscription.Continue;
            }
        }
    }
}
