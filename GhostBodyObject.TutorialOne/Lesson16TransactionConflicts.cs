using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 16: transaction conflicts.
    /// <para>
    /// Two concurrent transactions may change the same book: each one changes its own version of it (Lesson 15). The first
    /// commit wins. The second one finds that the book changed since its transaction opened: it throws a
    /// <c>TransactionConflictException</c>, and writes nothing. GBO never retries by itself: the application decides, with a
    /// new transaction, which reads the current version of the book.
    /// </para>
    /// <para>
    /// Changes of different books never conflict. And the level of conflict is chosen when a transaction opens:
    /// <c>Optimistic</c>, the default, as above; <c>Pessimistic</c>, which also refuses the commit when any entity of a type
    /// that the transaction read was changed since (Lesson 17); and <c>Exclusive</c>, which never conflicts (Lesson 18).
    /// </para>
    /// </summary>
    public sealed class Lesson16TransactionConflicts
    {
        /// <summary>Two transactions change the same book: the second commit throws; a new transaction applies its change again.</summary>
        [Fact]
        public async Task RefuseTheSecondChangeOfTheSameBook()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-conflict", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                transaction.Commit();
            }
            var firstChanged = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();

            // The first transaction changes the price, then waits before its commit.
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.Single().Price = 12.50m;
                firstChanged.SetResult();
                await secondCommitted.Task;

                // The book changed since this transaction opened: its commit throws, and writes nothing.
                Assert.Throws<TransactionConflictException>(transaction.Commit);
            });

            // The second one changes the same price, and commits first: it wins.
            await firstChanged.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.Single().Price = 11.00m;
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            // The application decides: here, it applies the change again in a new transaction, which reads 11.00.
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var dune = BookCollection.Single();
                Assert.Equal(11.00m, dune.Price);
                dune.Price = 12.50m;
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(12.50m, BookCollection.Single().Price);
            }
        }

        /// <summary>Two transactions change two different books at the same time: both commit.</summary>
        [Fact]
        public async Task ChangeTwoBooksAtTheSameTime()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-two-books", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                new Book { Title = "Foundation", Price = 8.50m };
                transaction.Commit();
            }
            var bothChanged = new TaskCompletionSource();
            var firstChanged = new TaskCompletionSource();

            // Both transactions are open, and have changed their book, before the first commit.
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.First(static book => book.Title == "Dune").Price = 12.50m;
                firstChanged.SetResult();
                await bothChanged.Task;
                transaction.Commit();
            });
            var second = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.First(static book => book.Title == "Foundation").Price = 10.00m;
                await firstChanged.Task;
                bothChanged.SetResult();
                await first;
                transaction.Commit();
            });
            await Task.WhenAll(first, second);

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal([12.50m, 10.00m], BookCollection.Select(static book => book.Price).ToList());
            }
        }

    }
}
