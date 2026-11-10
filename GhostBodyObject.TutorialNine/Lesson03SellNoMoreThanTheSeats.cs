using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialNine
{
    using static SalesRepositoryCollections;

    /// <summary>
    /// Lesson 3: sell no more than the seats.
    /// <para>
    /// A merged value carries no rule: each till sees a ticket left, and two sales of the last ticket merge below zero. A <c>[Delta]</c> takes
    /// bounds for that: <c>[Delta(Min = 0)]</c>, <c>Max</c>, or both. They bound the value of the field, never the change of a transaction.
    /// </para>
    /// <para>
    /// The bounds are checked twice. The Setter refuses a value out of them, <c>GhostBoundException</c>, the transaction intact: compare
    /// first, as the code of a sale does. The commit refuses a merged value out of them, a conflict of the kind <c>Bound</c>, nothing written;
    /// its next attempt reads the head, and finds that there is nothing left. The sales that fit never conflict: a concert sells exactly
    /// its seats to any number of tills, with no conflict but the last ones. Places, a stock, a quota: this is the escrow method, a count of
    /// what remains.
    /// </para>
    /// </summary>
    public sealed class Lesson03SellNoMoreThanTheSeats
    {
        private static void Open(SalesRepository sales, int seats)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Concert { Title = "Carmen", LowestPrice = 40m, Remaining = seats, Manager = "Iris" };
            transaction.Commit();
        }

        private static Concert Carmen() => ConcertCollection.First(static concert => concert.Title == "Carmen");

        /// <summary>
        /// Without a bound, a merged value goes where the changes take it: two refunds, each valid on what it read, leave the takings below zero,
        /// though the rule of <c>Money</c> refuses such a value. A merge reads no rule of a value.
        /// </summary>
        [Fact]
        public void MergeBelowZeroWithoutABound()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-refund", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales, seats: 1);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Takings = 100m;
                transaction.Commit();
            }
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Takings -= 70m;
                AnotherTill.Commits(sales, static () => Carmen().Takings -= 60m);
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(-30m, (decimal)Carmen().Takings);
        }

        /// <summary>The Setter refuses a value below the bound, at once: the transaction goes on, the value unchanged.</summary>
        [Fact]
        public void RefuseAValueOutOfItsBoundsInTheSetter()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-setter", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales, seats: 1);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var carmen = Carmen();
                Assert.Throws<GhostBoundException>(() => carmen.Remaining = -1);
                Assert.Equal(1, carmen.Remaining);

                // The bound itself is in.
                carmen.Remaining = 0;
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, Carmen().Remaining);
        }

        /// <summary>Two sales fit each on their reading, not together: the commit refuses the merged value, <c>Bound</c>. Run again, the sale finds no seat.</summary>
        [Fact]
        public void RefuseAMergedValueOutOfItsBoundsAtTheCommit()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-commit", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales, seats: 2);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // Two seats: this sale of two fits.
                Carmen().Remaining -= 2;

                // Another till sells one first. The merged value is 1 + (0 - 2), below the bound.
                AnotherTill.Commits(sales, static () => Carmen().Remaining -= 1);
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Bound, kind);
            }

            // The next attempt reads the seat that remains: the code of the sale compares, and refuses it.
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.False(Carmen().Remaining >= 2);
        }

        // One sale, run again on a conflict: true when it sold the seat, false when no seat remains.
        private static bool Sell(SalesRepository sales)
        {
            while (true)
            {
                using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
                var carmen = Carmen();
                if (carmen.Remaining < 1)
                    return false;
                carmen.Remaining -= 1;
                carmen.Sold += 1;
                if (transaction.TryCommit(out var kind))
                    return true;
                Assert.Equal(TransactionConflictKind.Bound, kind);
            }
        }

        /// <summary>Eight tills try to sell 800 tickets for 500 seats: 500 are sold, none twice, none too many.</summary>
        [Fact]
        public async Task SellExactlyTheSeatsToConcurrentBuyers()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-exactly", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales, seats: 500);
            int sold = 0, refused = 0;
            var tills = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                for (int buyer = 0; buyer < 100; buyer++)
                    Interlocked.Increment(ref Sell(sales) ? ref sold : ref refused);
            }));
            await Task.WhenAll(tills);
            Assert.Equal(500, sold);
            Assert.Equal(300, refused);
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(0, Carmen().Remaining);
                Assert.Equal(500, Carmen().Sold);
            }
        }
    }
}
