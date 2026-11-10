using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialNine
{
    using static SalesRepositoryCollections;

    /// <summary>
    /// Lesson 4: the other values, and the links.
    /// <para>
    /// A type <c>[Mergeable]</c> changes the rule for all its values. The unit of conflict is no longer the entity, as in Tutorial One, but each
    /// value. A value without <c>[Delta]</c>, <c>[Max]</c> or <c>[Min]</c> follows the rule of the three points: unchanged by the transaction, it
    /// takes the head; unchanged by the head, the new value; changed by both to the same value, that value. Changed by both to two values,
    /// nothing can choose: a conflict, <c>Change</c>, unless a Merger decides (Lesson 5).
    /// </para>
    /// <para>
    /// <b>A link never merges.</b> A value stands alone; a link says something of its target, and of every rule that binds them. A reference, a
    /// collection, an Owner changed by the head since the epoch is a conflict, even for a transaction that changed a <c>[Delta]</c> only. A link
    /// that the transaction alone changed is written as usual.
    /// </para>
    /// </summary>
    public sealed class Lesson04TheOtherValuesAndTheLinks
    {
        private static void Open(SalesRepository sales)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Hall { Name = "Main hall" };
            new Show { Title = "Tosca", Manager = "Iris" };
            new Concert { Title = "Carmen", LowestPrice = 40m, Remaining = 500, Manager = "Iris" };
            transaction.Commit();
        }

        private static Concert Carmen() => ConcertCollection.Single();

        /// <summary>On a show, an ordinary entity, the unit of conflict is the entity: two changes of two values conflict.</summary>
        [Fact]
        public void ConflictOnTwoValuesOfAnOrdinaryEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-ordinary", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            ShowCollection.Single().Manager = "Jon";
            AnotherTill.Commits(sales, static () => ShowCollection.Single().Title = "Tosca (new staging)");
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.Change, kind);
        }

        /// <summary>The same two changes on a concert: each side changed its own value, and both stay.</summary>
        [Fact]
        public void KeepWhatEachSideChanged()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-each", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Manager = "Jon";
                AnotherTill.Commits(sales, static () => Carmen().Title = "Carmen (new staging)");
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Jon", Carmen().Manager.ToString());
                Assert.Equal("Carmen (new staging)", Carmen().Title.ToString());
            }
        }

        /// <summary>Both sides give the same value: nothing to choose, no conflict.</summary>
        [Fact]
        public void AcceptTheSameValueFromBothSides()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-same", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Manager = "Jon";
                AnotherTill.Commits(sales, static () => Carmen().Manager = "Jon");
                Assert.True(transaction.TryCommit(out _));
            }
        }

        /// <summary>Two values for one field, and no Merger on a concert: a conflict, and its counters are not written either.</summary>
        [Fact]
        public void ConflictOnTwoValuesOfOneField()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-two", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var carmen = Carmen();
                carmen.Manager = "Jon";
                carmen.Sold += 1;
                AnotherTill.Commits(sales, static () => Carmen().Manager = "Kay");
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Change, kind);
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Kay", Carmen().Manager.ToString());
                Assert.Equal(0, Carmen().Sold);
            }
        }

        /// <summary>A link changed meanwhile refuses a sale that changes a counter only: a link never merges.</summary>
        [Fact]
        public void NeverMergeALink()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-link", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Sold += 1;
                AnotherTill.Commits(sales, static () => Carmen().Hall = HallCollection.Single());
                Assert.Equal(TransactionConflictKind.Change, Assert.Throws<TransactionConflictException>(transaction.Commit).Kind);
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, Carmen().Sold);
        }

        /// <summary>A link that the transaction alone changed goes through: a counter changed meanwhile merges with it.</summary>
        [Fact]
        public void WriteALinkThatTheTransactionAloneChanged()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-alone", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Hall = HallCollection.Single();
                AnotherTill.Commits(sales, static () => Carmen().Sold += 1);
                Assert.True(transaction.TryCommit(out _));
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.True(Carmen().Hall.Any());
                Assert.Equal(1, Carmen().Sold);
            }
        }
    }
}
