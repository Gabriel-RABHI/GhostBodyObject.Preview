using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Repository;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTen
{
    using static Lesson06HoldASeat;

    /// <summary>
    /// Lesson 8: several seats.
    /// <para>
    /// A family takes three seats, or none. <c>transaction.NewLease(duration)</c> creates a lease, <c>lease.TryAdd(view)</c> adds an entity to it, and
    /// one commit holds them all. <c>TryAdd</c> answers <c>false</c> for an entity that another lease holds: a transaction that meets a <c>false</c>
    /// does not commit, and nothing is held.
    /// </para>
    /// <para>
    /// <c>lease.Views&lt;TView&gt;()</c> gives the entities of a lease, in a transaction, and a transaction of the lease grows it by <c>TryAdd</c>, held by
    /// its commit.
    /// </para>
    /// </summary>
    public sealed class Lesson08SeveralSeats
    {
        /// <summary>A seat refused, and the transaction does not commit: nothing is held.</summary>
        [Fact]
        public void HoldSeveralSeatsAllOrNone()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-none", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            Hold(boxOffice, 2);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var lease = transaction.NewLease(Five);
                Assert.True(lease.TryAdd(SeatNumber(1)));
                Assert.False(lease.TryAdd(SeatNumber(2)));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.False(SeatNumber(1).IsLocked);
        }

        // A lease of two seats, taken by one commit.
        private static GhostLease HoldTwoSeats(BoxOfficeRepository boxOffice)
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            var lease = transaction.NewLease(Five);
            Assert.True(lease.TryAdd(SeatNumber(1)));
            Assert.True(lease.TryAdd(SeatNumber(3)));
            transaction.Commit();
            return lease;
        }

        /// <summary>One commit holds every seat of the lease, and a transaction of the lease reads them back.</summary>
        [Fact]
        public void ReadTheSeatsOfALease()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-views", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = HoldTwoSeats(boxOffice);
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.True(SeatNumber(1).IsLocked && SeatNumber(3).IsLocked);
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                foreach (var seat in lease.Views<SeatView>())
                    seat.ToBody().Buyer = "Ada";
                lease.Release();
                transaction.Commit();
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Ada", SeatNumber(1).Buyer.ToString());
                Assert.Equal("", SeatNumber(2).Buyer.ToString());
                Assert.Equal("Ada", SeatNumber(3).Buyer.ToString());
            }
        }

        /// <summary>A transaction of the lease adds a seat: its commit holds it, for the others as for the first one.</summary>
        [Fact]
        public void GrowALeaseInOneOfItsTransactions()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-grow", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                Assert.True(lease.TryAdd(SeatNumber(2)));
                transaction.Commit();
            }
            Assert.Throws<GhostLockedException>(() => AnotherTill.Commits(boxOffice, static () => SeatNumber(2).ToBody().Buyer = "Bob"));
        }
    }
}
