using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Repository;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialTen
{
    using static BoxOfficeRepositoryCollections;
    using static Lesson03ARuleOverTheParts;
    using static Lesson06HoldASeat;

    /// <summary>
    /// Lesson 10: leases, points and exclusive transactions.
    /// <para>
    /// The point is the unit of the locks as it is of the conflicts. <c>TryLock</c> of an entity of a domain locks the nearest point: a part never carries a
    /// lock of its own, the whole domain is held, and <c>IsLocked</c> reads the lock of the point from any of its entities. The mergeable values of a part
    /// stay writable: a counter below a locked order goes on counting.
    /// </para>
    /// <para>
    /// An exclusive transaction (Tutorial One, Lesson 18) checks the leases as any other. Opened with <c>ignoreLocks: true</c>, and only at this level, it writes
    /// through them, and the transaction of the lease, open meanwhile, then fails at its commit. That option is a tool of maintenance: against it, a lease
    /// no longer guarantees anything.
    /// </para>
    /// </summary>
    public sealed class Lesson10LeasesPointsAndExclusiveTransactions
    {
        private static Line LineOf(string reference, string concert)
        {
            foreach (var line in OrderOf(reference).Lines)
                if (line.Concert == concert)
                    return line;
            throw new InvalidOperationException("No line.");
        }

        /// <summary>A lease taken on a line holds its whole order: another line is refused, the other order is free, and the counter goes on.</summary>
        [Fact]
        public void HoldTheDomainOfAPoint()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-domain", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Lesson03ARuleOverTheParts.Open(boxOffice);
            GhostLease? lease;
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(LineOf("B-1", "Carmen").ToView().TryLock(Five, out lease));
                transaction.Commit();
            }
            AnotherTill.Commits(boxOffice, static () =>
            {
                // The lock is the one of the order, seen from every entity of its domain.
                Assert.True(LineOf("B-1", "Tosca").ToView().IsLocked);
                Assert.True(OrderOf("B-1").ToView().IsLocked);
                Assert.Throws<GhostLockedException>(() => LineOf("B-1", "Tosca").Tickets = 4);

                // Another order is another domain, and a mergeable value marks nothing.
                LineOf("B-2", "Tosca").Tickets = 4;
                foreach (var counter in OrderOf("B-1").Counters)
                    counter.Visits += 1;
            });

            // The holder writes the whole domain.
            using (var transaction = boxOffice.OpenTransaction(lease!))
            {
                LineOf("B-1", "Tosca").Tickets = 1;
                lease!.Release();
                transaction.Commit();
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, LineOf("B-1", "Tosca").Tickets);
        }

        /// <summary>An exclusive transaction checks the leases, unless it says otherwise; the transaction of the lease then fails at its commit.</summary>
        [Fact]
        public void PassOverALeaseOnlyWhenAsked()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-ignore", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Lesson06HoldASeat.Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive))
                Assert.Throws<GhostLockedException>(() => SeatNumber(1).ToBody().Buyer = "Bob");
            using (var transaction = boxOffice.OpenTransaction(lease))
            {
                SeatNumber(1).ToBody().Buyer = "Ada";
                AnotherTill.Runs(() =>
                {
                    using var exclusive = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive, ignoreLocks: true);
                    SeatNumber(1).ToBody().Buyer = "Bob";
                    exclusive.Commit();
                });
                Assert.Equal(TransactionConflictKind.Change, Assert.Throws<TransactionConflictException>(transaction.Commit).Kind);
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Bob", SeatNumber(1).Buyer.ToString());
        }
    }
}
