using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    /// <summary>
    /// Lesson 12: the indexes and the transactions.
    /// <para>
    /// An index is a part of the data, as the entities: it is up to date at the next instruction in the transaction that changes them
    /// (Lessons 1 and 5), its changes are rolled back with the transaction, and the other transactions see them at its commit only.
    /// Concurrent transactions that give the same value to their entities, the same status or the same day, never conflict by the index:
    /// both commits keep their entities. Only two changes of the same entity conflict, as in Tutorial One.
    /// </para>
    /// <para>
    /// The indexes are kept with the data, over a restart. An index is a projection of the entities: a Repository builds an index that
    /// its Store does not have yet at its start, from the entities already there, before its first transaction.
    /// </para>
    /// </summary>
    public sealed class Lesson12IndexesAndTransactions
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

        /// <summary>A rolled back transaction leaves the indexes as they were; another transaction sees a change at its commit only.</summary>
        [Fact]
        public async Task SeeTheChangesOfACommitOnly()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-commit", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Order { Number = 1, Status = OrderStatus.Paid, Placed = Monday };
                Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
                transaction.Rollback();
            }
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());

            // A writer changes the index, then waits; a reader, on its own thread, looks meanwhile (Tutorial One, Lesson 15).
            var written = new TaskCompletionSource();
            var looked = new TaskCompletionSource();
            var writer = Task.Run(async () =>
            {
                using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
                new Order { Number = 2, Status = OrderStatus.Paid, Placed = Monday };
                written.SetResult();
                await looked.Task;
                transaction.Commit();
            });
            await written.Task;
            int seen = await Task.Run(() =>
            {
                using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
                return OrderStatusDayIndex.Is(OrderStatus.Paid).Count();
            });
            looked.SetResult();
            await writer;

            // The order was not committed when the reader looked; a transaction opened now sees it.
            Assert.Equal(0, seen);
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
        }

        /// <summary>Writers in parallel, each its own orders of the same status and day: every commit keeps its orders.</summary>
        [Fact]
        public void CommitInParallelOnTheSameValues()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-parallel", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            const int Writers = 4, Orders = 100;
            Parallel.For(0, Writers, writer =>
            {
                for (int i = 0; i < Orders; i++)
                {
                    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
                    new Order { Number = writer * Orders + i, Status = OrderStatus.Paid, Placed = Monday };
                    transaction.Commit();
                }
            });
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(Writers * Orders, OrderStatusDayIndex.Is(OrderStatus.Paid).Is(DateOnly.FromDateTime(Monday)).Count());
                Assert.Equal(Writers * Orders, OrderNumberIndex.Between(0, Writers * Orders).Count());
            }
        }

        /// <summary>The indexes over a restart of the Repository.</summary>
        [Fact]
        public void KeepTheIndexesOverARestart()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-restart", StoreAccessMode.ReadWrite);
            using (var shop = new ShopRepository(store))
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Customer { Name = "Ada", Email = "ada@example.com" };
                new Order { Number = 7, Status = OrderStatus.Shipped, Placed = Monday };
                transaction.Commit();
            }
            using (var shop = new ShopRepository(store))
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Ada", CustomerEmailIndex.Is("ADA@example.com").Single().Name);
                Assert.Equal(7, OrderStatusDayIndex.Is(OrderStatus.Shipped).Result.Single().Number);
            }
        }
    }
}
