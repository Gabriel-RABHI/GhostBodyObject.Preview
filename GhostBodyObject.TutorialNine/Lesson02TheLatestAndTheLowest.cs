using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialNine
{
    using static SalesRepositoryCollections;

    /// <summary>
    /// Lesson 2: the latest and the lowest.
    /// <para>
    /// Not every value adds up. The moment of the last sale keeps the latest one, whatever the order of the commits; the lowest price paid
    /// keeps the lowest one. <c>[Max]</c> and <c>[Min]</c> say so, on numbers, dates, times and durations, and on value objects that have
    /// an order.
    /// </para>
    /// <para>
    /// Both rules read the three points. A transaction that did not change the value takes the head, so that a correction committed
    /// meanwhile is not undone by the value of its epoch. A transaction that changed it writes the greater, or the lower, of the head and of
    /// its new value. They are rules of merge, not rules of the value: without a concurrent change, nothing merges, and the value written is
    /// the value committed. A code that must never move a date back compares before it writes.
    /// </para>
    /// </summary>
    public sealed class Lesson02TheLatestAndTheLowest
    {
        private static readonly DateTime Opening = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        private static void Open(SalesRepository sales)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Concert { Title = "Carmen", Sold = 10, Takings = 400m, LastSale = Opening, LowestPrice = 40m, Remaining = 500, Manager = "Iris" };
            transaction.Commit();
        }

        private static Concert Carmen() => ConcertCollection.First(static concert => concert.Title == "Carmen");

        /// <summary>Two sales at the same time: the latest moment, the lowest price, each one from the side that holds it.</summary>
        [Fact]
        public void KeepTheLatestSaleAndTheLowestPrice()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-keep", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var carmen = Carmen();
                carmen.LastSale = Opening.AddMinutes(10);
                carmen.LowestPrice = 30m;
                AnotherTill.Commits(sales, static () =>
                {
                    var concert = Carmen();
                    concert.LastSale = Opening.AddMinutes(20);
                    concert.LowestPrice = 35m;
                });
                transaction.Commit();
            }

            // The sale of the other till is the latest, the price of this one the lowest.
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(Opening.AddMinutes(20), Carmen().LastSale);
                Assert.Equal(30m, Carmen().LowestPrice);
            }
        }

        /// <summary>A correction committed meanwhile stays: the transaction did not change the value, and takes the head.</summary>
        [Fact]
        public void KeepACorrectionThatTheTransactionDidNotMake()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-correction", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // This sale changes the count, and neither the moment nor the price.
                Carmen().Sold += 1;

                // The manager corrects both: the moment moves back, the lowest price up.
                AnotherTill.Commits(sales, static () =>
                {
                    var concert = Carmen();
                    concert.LastSale = Opening.AddHours(-1);
                    concert.LowestPrice = 45m;
                });
                transaction.Commit();
            }

            // A plain maximum with the value of the epoch would have undone the correction.
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(Opening.AddHours(-1), Carmen().LastSale);
                Assert.Equal(45m, Carmen().LowestPrice);
                Assert.Equal(11, Carmen().Sold);
            }
        }

        // A sale reported by a till: a message may come late, and the code compares before it writes.
        private static void Report(Concert concert, DateTime sold, Money price)
        {
            if (sold > concert.LastSale)
                concert.LastSale = sold;
            if (price < concert.LowestPrice)
                concert.LowestPrice = price;
        }

        /// <summary><c>[Max]</c> is a rule of merge: alone, a transaction writes what it writes. The code that must not go back compares.</summary>
        [Fact]
        public void WriteAnyValueWithoutAConcurrentChange()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-alone", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);

            // Nothing merges: the earlier moment is written.
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().LastSale = Opening.AddHours(-1);
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(Opening.AddHours(-1), Carmen().LastSale);

            // Report compares: a message that comes late changes nothing.
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Report(Carmen(), Opening.AddHours(2), 42m);
                transaction.Commit();
            }
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Report(Carmen(), Opening.AddHours(1), 41m);
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(Opening.AddHours(2), Carmen().LastSale);
                Assert.Equal(40m, Carmen().LowestPrice);
            }
        }
    }
}
