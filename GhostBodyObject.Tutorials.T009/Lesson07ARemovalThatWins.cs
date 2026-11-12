using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T009
{
    using static SalesRepositoryCollections;

    /// <summary>A ticket machine in the foyer. <c>RemovalWins</c>: withdrawn from service, it stays withdrawn, whatever a sale was writing.</summary>
    [Entity(5, 1, 3), Root, Removable, Enumerable, Mergeable(RemovalWins = true)]
    internal interface MKiosk
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2), Delta]
        VTickets Sold { get; set; }

        [Property(3), Weak]
        Option<MHall> Hall { get; set; }
    }

    /// <summary>
    /// Lesson 7: a removal that wins.
    /// <para>
    /// A merge settles values; a removal is another thing. By default, a type <c>[Mergeable]</c> keeps the rule of an ordinary entity: a write of an entity
    /// removed meanwhile conflicts, <c>Removal</c>, a removal of an entity changed meanwhile conflicts, <c>Change</c>.
    /// </para>
    /// <para>
    /// <c>[Mergeable(RemovalWins = true)]</c> changes that, for the values only: the removal wins, in both orders of the commits. A write that
    /// comes after it is dropped, and the rest of its transaction commits; a removal that comes after a write removes the entity all the
    /// same. It serves a removal that is a command, a machine withdrawn from service. It stays off by default: the transaction that lost
    /// its write is told nothing, and a Log chained with it keeps the Record of what it did. A link changed on either side still conflicts.
    /// </para>
    /// </summary>
    public sealed class Lesson07ARemovalThatWins
    {
        private static void Open(SalesRepository sales)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Hall { Name = "Main hall" };
            new Kiosk { Name = "Foyer" };
            new Concert { Title = "Carmen", LowestPrice = 40m, Remaining = 500, Manager = "Iris" };
            transaction.Commit();
        }

        private static Kiosk Foyer() => KioskCollection.Single();

        private static Concert Carmen() => ConcertCollection.Single();

        /// <summary>A concert keeps the rule of an ordinary entity: a sale that meets a removal conflicts, in both orders.</summary>
        [Fact]
        public void ConflictWithARemovalByDefault()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-default", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Remove();
                AnotherTill.Commits(sales, static () => Carmen().Sold += 1);
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Change, kind);
            }
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Sold += 1;
                AnotherTill.Commits(sales, static () => Carmen().Remove());
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Removal, kind);
            }
        }

        /// <summary>A sale that comes after the removal: its write of the machine is dropped, and the rest of its transaction commits.</summary>
        [Fact]
        public void DropAWriteThatComesAfterTheRemoval()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-drop", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Foyer().Sold += 1;
                Carmen().Sold += 1;
                AnotherTill.Commits(sales, static () => Foyer().Remove());
                Assert.True(transaction.TryCommit(out _));
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(0, KioskCollection.Count());
                Assert.Equal(1, Carmen().Sold);
            }
        }

        /// <summary>A removal that comes after a sale removes the machine all the same: both orders give the same result.</summary>
        [Fact]
        public void RemoveAnEntityChangedMeanwhile()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-remove", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Foyer().Remove();
                AnotherTill.Commits(sales, static () => Foyer().Sold += 1);
                Assert.True(transaction.TryCommit(out _));
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, KioskCollection.Count());
        }

        /// <summary>A link keeps its conflict: a removal never hides a link written meanwhile.</summary>
        [Fact]
        public void KeepTheConflictOfALink()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-link", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Foyer().Hall = HallCollection.Single();
                AnotherTill.Commits(sales, static () => Foyer().Remove());
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Removal, kind);
            }
        }
    }
}
