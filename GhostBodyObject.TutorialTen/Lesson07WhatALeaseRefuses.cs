using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTen
{
    using static Lesson06HoldASeat;

    /// <summary>
    /// Lesson 7: what a lease refuses.
    /// <para>
    /// A lease is refused at once, never queued: no wait, no deadlock. While it is active, a write of another context is refused at its instruction,
    /// <c>GhostLockedException</c>, the transaction intact, and a removal with it. Compare first, as for a bound of a value: <c>IsLocked</c> says it,
    /// and <c>TryLock</c> answers <c>false</c>. The transactions that read see the entity as usual: a lease stops no reader.
    /// </para>
    /// <para>
    /// A lock taken after a transaction opened, which its instruction could not see, fails its commit, of the kind <c>Lock</c>. The transactions of
    /// a lease follow one another: a second one, while one is open, is refused. A lease is taken in a transaction that writes.
    /// </para>
    /// </summary>
    public sealed class Lesson07WhatALeaseRefuses
    {
        /// <summary>A seat held by another till: refused at once, at the instruction, and the transaction writes the rest.</summary>
        [Fact]
        public void RefuseASeatThatAnotherTillHolds()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-refuse", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            Hold(boxOffice, 1);
            AnotherTill.Commits(boxOffice, static () =>
            {
                // Compare first: IsLocked says it, and TryLock refuses it without an exception.
                Assert.True(SeatNumber(1).IsLocked);
                Assert.False(SeatNumber(1).TryLock(Five, out _));

                // A write or a removal that did not compare is refused at its instruction, nothing written, the transaction intact.
                Assert.Throws<GhostLockedException>(() => SeatNumber(1).ToBody().Buyer = "Bob");
                Assert.Throws<GhostLockedException>(() => SeatNumber(1).ToBody().Remove());
                SeatNumber(2).ToBody().Buyer = "Bob";
            });
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("", SeatNumber(1).Buyer.ToString());
                Assert.Equal("Bob", SeatNumber(2).Buyer.ToString());
            }
        }

        /// <summary>A lock taken after this transaction opened: its write went through, and its commit conflicts, of the kind <c>Lock</c>.</summary>
        [Fact]
        public void ConflictOnALockTakenMeanwhile()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-meanwhile", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            var seat = SeatNumber(1).ToBody();
            AnotherTill.Commits(boxOffice, static () => Assert.True(SeatNumber(1).TryLock(Five, out _)));

            // The instruction reads the seat as the transaction saw it, free: the commit meets the lock.
            seat.Buyer = "Ada";
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.Lock, kind);
        }

        /// <summary>The transactions of a lease follow one another: a second one is refused while one is open.</summary>
        [Fact]
        public void OpenOneTransactionOfALeaseAtATime()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-one", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            var lease = Hold(boxOffice, 1);
            using (boxOffice.OpenTransaction(lease))
                Assert.Throws<InvalidOperationException>(() => AnotherTill.Runs(() => boxOffice.OpenTransaction(lease).Dispose()));
        }

        /// <summary>A lease is a write: a transaction that reads refuses it.</summary>
        [Fact]
        public void RefuseALeaseInATransactionThatReads()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-read", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Throws<InvalidOperationException>(() => SeatNumber(1).TryLock(Five, out _));
        }
    }
}
