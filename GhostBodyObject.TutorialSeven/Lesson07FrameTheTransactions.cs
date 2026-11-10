using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSeven.Journal
{
    /// <summary>
    /// Lesson 7: frame the transactions.
    /// <para>
    /// The Records of a commit belong together: a parcel registered and its first scan, a round of a driver, a day of a depot. A
    /// subscription delivers them together, framed: <c>OnTransactionStart</c> before the first Record of each transaction,
    /// <c>OnTransactionEnd</c> after its last one, each with the number of the transaction. A subscriber applies a transaction as a
    /// whole there: it opens its work at the start, and closes it at the end.
    /// </para>
    /// <para>
    /// The number of a transaction is its place in the Log. <c>SubscribeFrom(n)</c> delivers from the transaction <c>n</c>: a subscriber
    /// that remembers the last transaction it handled starts again after it. A start that answers <c>Cancel</c> delivers none of the
    /// Records of its transaction: a subscriber stops at a known point of the Log, and has the state of that moment.
    /// </para>
    /// </summary>
    public sealed class Lesson07FrameTheTransactions
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>Each transaction framed by its start and its end, then the end of the replay.</summary>
        [Fact]
        public void FrameEachTransactionThenTheReplay()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-frame", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Round(log, "Lyon", "PX-7001", "PX-7002");
            Round(log, "Paris", "PX-7003");

            // The rounds of the depots: the scans of a transaction counted together, closed at its end.
            var rounds = new Rounds();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .OnTransactionStart(rounds, static (transaction, rounds) => rounds.Start(transaction))
                .Handle<ParcelScannedView, Rounds>(rounds, static (_, e, rounds) => rounds.Scanned(e.Depot))
                .OnTransactionEnd(rounds, static (transaction, rounds) => rounds.End(transaction))
                .OnReplayEnd(rounds, static (last, rounds) => rounds.Ready(last))
                .Run();
            end.Cancel();

            ulong first = rounds.Closed[0].Transaction;
            Assert.Equal([(first, "Lyon", 2), (first + 1, "Paris", 1)], rounds.Closed);
            Assert.Equal(first + 1, rounds.Last);
        }

        /// <summary>A subscriber that remembers the last transaction it handled starts again after it.</summary>
        [Fact]
        public void StartFromALaterTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-later", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Round(log, "Lyon", "PX-7011");
            Round(log, "Paris", "PX-7012");

            var rounds = new Rounds();
            Follow(log, 0, rounds);
            ulong handled = rounds.Last;

            // Later: two new rounds. From the transaction after the last one handled, only them.
            Round(log, "Lille", "PX-7013");
            Round(log, "Nice", "PX-7014", "PX-7015");
            var later = new Rounds();
            Follow(log, handled + 1, later);
            Assert.Equal([("Lille", 1), ("Nice", 2)], later.Closed.Select(static round => (round.Depot, round.Scans)));
        }

        /// <summary>A start that answers Cancel: the state of the Log at a known point, the transactions after it never delivered.</summary>
        [Fact]
        public void StopAtAKnownTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-stop", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            Round(log, "Lyon", "PX-7021");
            Round(log, "Paris", "PX-7022");
            Round(log, "Lille", "PX-7023");
            var all = new Rounds();
            Follow(log, 0, all);

            // The rounds until the second transaction: the third one is not delivered, and the subscription ends at its start.
            var until = new Rounds();
            var subscription = log.SubscribeFrom(0)
                .OnTransactionStart(until, all.Closed[1].Transaction, static (transaction, until, last) =>
                    transaction > last ? LogStoreSubscription.Cancel : until.Start(transaction))
                .Handle<ParcelScannedView, Rounds>(until, static (_, e, until) => until.Scanned(e.Depot))
                .OnTransactionEnd(until, static (transaction, until) => until.End(transaction))
                .Run();
            Assert.Equal(["Lyon", "Paris"], until.Closed.Select(static round => round.Depot));
            Assert.True(subscription.Completion.IsCompletedSuccessfully);
        }

        // The rounds of the Log from the transaction given, until its end.
        private static void Follow(ParcelLog log, ulong from, Rounds rounds)
        {
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(from, end.Token)
                .OnTransactionStart(rounds, static (transaction, rounds) => rounds.Start(transaction))
                .Handle<ParcelScannedView, Rounds>(rounds, static (_, e, rounds) => rounds.Scanned(e.Depot))
                .OnTransactionEnd(rounds, static (transaction, rounds) => rounds.End(transaction))
                .Run();
            end.Cancel();
        }

        // A round of a depot: its scans, in one commit.
        private static void Round(ParcelLog log, Depot depot, params string[] trackingNumbers)
        {
            using var transaction = log.OpenTransaction();
            foreach (var trackingNumber in trackingNumbers)
                new ParcelScanned { TrackingNumber = trackingNumber, Depot = depot, Scanned = Monday }.Add();
            transaction.Commit();
        }

        // The rounds closed, each a transaction of the Log: its number, its depot, its count of scans; the last transaction handled.
        private sealed class Rounds
        {
            private string _depot = "";
            private int _scans;

            public List<(ulong Transaction, string Depot, int Scans)> Closed { get; } = [];

            public ulong Last { get; private set; }

            public LogStoreSubscription Start(ulong transaction)
            {
                (_depot, _scans) = ("", 0);
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription Scanned(string depot)
            {
                (_depot, _scans) = (depot, _scans + 1);
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription End(ulong transaction)
            {
                Closed.Add((transaction, _depot, _scans));
                Last = transaction;
                return LogStoreSubscription.Continue;
            }

            public LogStoreSubscription Ready(ulong last)
            {
                Last = last;
                return LogStoreSubscription.Continue;
            }
        }
    }
}
