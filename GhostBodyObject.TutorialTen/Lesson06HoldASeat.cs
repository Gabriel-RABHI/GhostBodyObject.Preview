using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Logical.Repository;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTen
{
    using static BoxOfficeRepositoryCollections;

    /// <summary>A seat of a concert: free until it has a buyer.</summary>
    [Entity(8, 1, 2), Root, Removable, Enumerable]
    internal interface MSeat
    {
        [Property(1)]
        VSeatNumber Number { get; set; }

        [Property(2)]
        VName Buyer { get; set; }
    }

    /// <summary>
    /// Lesson 6: hold a seat.
    /// <para>
    /// A transaction covers no work outside GBO. A payment takes seconds, and a transaction that fails after it has charged the customer for
    /// nothing. A <b>lease</b> holds entities between transactions. <c>view.TryLock(duration, out lease)</c> takes it, by the commit of the
    /// transaction that decides; the work runs outside any transaction; <c>repository.OpenTransaction(lease)</c> opens a transaction that writes the
    /// entities of the lease, and <c>lease.Release()</c> frees them by its commit.
    /// </para>
    /// <para>
    /// The lease is taken by a commit: without that commit, nothing is held. <c>lease.Id</c>, chosen by GBO, is the key that makes the work outside
    /// run once. Between the two transactions the other commits go on, on every server: only the entities of the lease are refused.
    /// </para>
    /// </summary>
    public sealed class Lesson06HoldASeat
    {
        internal static readonly TimeSpan Five = TimeSpan.FromMinutes(5);

        // Three free seats.
        internal static void Open(BoxOfficeRepository boxOffice)
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            for (int number = 1; number <= 3; number++)
                new Seat { Number = number, Buyer = "" };
            transaction.Commit();
        }

        internal static SeatView SeatNumber(int number) => SeatViewCollection.First(number, static (seat, number) => seat.Number == number);

        // The decision: a lease of one seat, taken by this commit.
        internal static GhostLease Hold(BoxOfficeRepository boxOffice, int number)
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            Assert.True(SeatNumber(number).TryLock(Five, out var lease));
            transaction.Commit();
            return lease!;
        }

        /// <summary>A seat held during a payment: the lease is taken by a commit, the seat written by a transaction of the lease, then released.</summary>
        [Fact]
        public void HoldASeatDuringAPayment()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-payment", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);

            // The transaction that decides takes the lease: its commit holds the seat, on every server.
            GhostLease? lease;
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(SeatNumber(1).TryLock(Five, out lease));
                transaction.Commit();
            }

            // The payment, outside any transaction. The provider charges a key once: lease.Id makes a retry harmless.
            var charged = new HashSet<ulong>();
            Assert.True(lease!.IsActive);
            Assert.True(charged.Add(lease.Id));

            // A transaction of the lease writes the seat, and frees it by its commit.
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                SeatNumber(1).ToBody().Buyer = "Ada";
                lease.Release();
                transaction.Commit();
            }
            Assert.False(lease.IsActive);
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Ada", SeatNumber(1).Buyer.ToString());
                Assert.False(SeatNumber(1).IsLocked);
            }
        }

        /// <summary>The lease is taken by a commit: a transaction that does not commit holds nothing, and its lease opens no transaction.</summary>
        [Fact]
        public void TakeNothingWithoutACommit()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-nothing", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            GhostLease? lease;
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
                Assert.True(SeatNumber(1).TryLock(Five, out lease));
            Assert.False(lease!.IsActive);
            Assert.Throws<InvalidOperationException>(() => boxOffice.OpenTransaction(lease));
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.False(SeatNumber(1).IsLocked);
        }
    }
}
