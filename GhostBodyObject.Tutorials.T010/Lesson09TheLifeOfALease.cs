using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T010
{
    using static Lesson06HoldASeat;

    /// <summary>
    /// Lesson 9: the life of a lease.
    /// <para>
    /// A lease lives by its duration. <c>lease.Remaining</c> tells what is left; <c>lease.Renew(duration)</c>, in one of its transactions, gives a new
    /// duration by its commit. A lease never released expires by itself: a holder that dies leaves no lock for ever. Durations count on the steady clock
    /// of each server, never on an hour compared between two servers, and the other tills see a lease end a little later than its holder does.
    /// </para>
    /// <para>
    /// A lease that ended is <b>fenced</b>: its transaction commits only if the lock is still its own. Its entities written by another till, taken by another
    /// lease, or its duration over at the commit: the commit fails, of the kind <c>LeaseLost</c>, nothing written. There are never two holders of one seat.
    /// This protects what GBO writes, not the work outside: <c>lease.Id</c> does.
    /// </para>
    /// </summary>
    public sealed class Lesson09TheLifeOfALease
    {
        /// <summary>The clock of a test, which moves only when the test moves it: a lease counts on it, and expires without a wait.</summary>
        private sealed class TestClock : TimeProvider
        {
            private long _ticks = TimeSpan.TicksPerDay;

            public override long TimestampFrequency => TimeSpan.TicksPerSecond;

            public override long GetTimestamp() => Interlocked.Read(ref _ticks);

            public void Advance(TimeSpan delay) => Interlocked.Add(ref _ticks, delay.Ticks);
        }

        /// <summary>A payment takes longer than planned: a transaction of the lease renews it, by its commit.</summary>
        [Fact]
        public void RenewALease()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-renew", StoreAccessMode.ReadWrite);
            var clock = new TestClock();
            store.TimeProvider = clock;
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            clock.Advance(TimeSpan.FromMinutes(4));
            Assert.Equal(TimeSpan.FromMinutes(1), lease.Remaining);
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                lease.Renew(TimeSpan.FromMinutes(10));
                transaction.Commit();
            }
            Assert.Equal(TimeSpan.FromMinutes(10), lease.Remaining);
        }

        /// <summary>A lease that expired: another till wrote the seat, and the commit of the holder is refused, <c>LeaseLost</c>, nothing written.</summary>
        [Fact]
        public void LoseALeaseThatExpired()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-expired", StoreAccessMode.ReadWrite);
            var clock = new TestClock();
            store.TimeProvider = clock;
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);

            // The payment took too long: the lease ended, and the seat is free for another till.
            clock.Advance(Five + TimeSpan.FromSeconds(2));
            Assert.False(lease.IsActive);
            AnotherTill.Commits(boxOffice, static () => SeatNumber(1).ToBody().Buyer = "Bob");

            // The holder comes back: never two holders of one seat.
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                SeatNumber(2).ToBody().Buyer = "Ada";
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.LeaseLost, kind);
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Bob", SeatNumber(1).Buyer.ToString());
                Assert.Equal("", SeatNumber(2).Buyer.ToString());
            }

            // A lease lost opens no transaction.
            Assert.Throws<InvalidOperationException>(() => boxOffice.OpenTransaction(lease));
        }

        /// <summary>Another till took the seat after the expiry: the holder is refused at its instruction, by the lock of the other lease, then at its commit.</summary>
        [Fact]
        public void FenceALeaseThatAnotherTook()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-fence", StoreAccessMode.ReadWrite);
            var clock = new TestClock();
            store.TimeProvider = clock;
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            clock.Advance(Five + TimeSpan.FromSeconds(2));
            AnotherTill.Commits(boxOffice, static () => Assert.True(SeatNumber(1).TryLock(Five, out _)));
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                Assert.Throws<GhostLockedException>(() => SeatNumber(1).ToBody().Buyer = "Ada");
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.LeaseLost, kind);
            }
        }

        /// <summary>The holder froze beyond its lease, in the middle of its transaction: nobody took the seat, and its commit is refused all the same.</summary>
        [Fact]
        public void LoseALeaseThatExpiresDuringItsTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-frozen", StoreAccessMode.ReadWrite);
            var clock = new TestClock();
            store.TimeProvider = clock;
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            using var transaction = boxOffice.OpenTransaction(lease);
            SeatNumber(1).ToBody().Buyer = "Ada";
            clock.Advance(Five);
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.LeaseLost, kind);
        }
    }
}
