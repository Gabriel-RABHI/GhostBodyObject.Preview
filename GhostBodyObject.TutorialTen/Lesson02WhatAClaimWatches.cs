using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTen
{
    using static BoxOfficeRepositoryCollections;
    using static Lesson01ADecisionOnWhatWasRead;

    /// <summary>
    /// Lesson 2: what a claim watches.
    /// <para>
    /// A claim watches one entity, and nothing else. The <c>Pessimistic</c> level of Tutorial One, Lesson 17, also protects this decision, by watching a whole
    /// type: a change of any usher of any hall refuses the commit. A claim lets the other hall work. It is taken on a View or on a Body, in a
    /// transaction that writes, and it costs one write: it goes on what the decision depends on, not on everything read.
    /// </para>
    /// <para>
    /// It protects only those who claim. A change of an usher writes the usher, not the hall, and a code that changes an usher without claiming the
    /// hall goes through. When every change of an aggregate must count, without anyone asking, the model says so: Lesson 3. And an entity that a
    /// transaction claims never merges in it: the transaction decided on its values, and no merge changes them behind it.
    /// </para>
    /// </summary>
    public sealed class Lesson02WhatAClaimWatches
    {
        /// <summary>The <c>Pessimistic</c> level watches the type: an usher of the other hall refuses the commit, though the rule of this hall holds.</summary>
        [Fact]
        public void ConflictOnAnyUsherAtThePessimisticLevel()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-pessimistic", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Pessimistic);
            Assert.True(Leave(MainHall(), "Ada"));
            AnotherTill.Commits(boxOffice, static () => Assert.True(Leave(SmallHall(), "Dan")));
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.Collection, kind);
        }

        /// <summary>A claim watches the hall: the other hall is not concerned, and both commit.</summary>
        [Fact]
        public void LeaveTheOtherHallAlone()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-other", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(LeaveWithAClaim(MainHall(), "Ada"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(LeaveWithAClaim(SmallHall(), "Dan")));
                Assert.True(transaction.TryCommit(out _));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(1, OnDuty(MainHall()));
                Assert.Equal(1, OnDuty(SmallHall()));
            }
        }

        /// <summary>A claim on a View needs no Body. A change of the entity claimed conflicts too: the decision rests on a read that changed.</summary>
        [Fact]
        public void ConflictWithAChangeOfTheEntityClaimed()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-change", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            HallViewCollection.First(static hall => hall.Name == "Main hall").Claim();
            AnotherTill.Commits(boxOffice, static () => MainHall().Name = "Grand hall");
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.Claim, kind);
        }

        /// <summary>A claim protects only those who claim: a decision that does not claim changes an usher, not the hall, and nothing conflicts.</summary>
        [Fact]
        public void MissADecisionThatDoesNotClaim()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-miss", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(LeaveWithAClaim(MainHall(), "Ada"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(Leave(MainHall(), "Bob")));
                Assert.True(transaction.TryCommit(out _));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, OnDuty(MainHall()));
        }

        /// <summary>A claim is a write: a transaction that reads has none to give.</summary>
        [Fact]
        public void RefuseAClaimInATransactionThatReads()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-read", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Throws<InvalidOperationException>(() => MainHall().Claim());
        }
    }
}
