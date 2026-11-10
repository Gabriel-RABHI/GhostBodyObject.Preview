using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 17: pessimistic transactions.
    /// <para>
    /// The commit of Lesson 16 compares what a transaction writes, never what it reads. A decision taken on what it read, and written elsewhere,
    /// is not protected. The library keeps one book on its shelves; two readers borrow two books at the same time. Each transaction reads two
    /// books available, and writes its own one. They write two different books: both commit, and the shelves are empty. Each decision was right
    /// on what it read.
    /// </para>
    /// <para>
    /// The <c>Pessimistic</c> level adds the reads. The transaction notes each entity type that it consults, and its commit conflicts, of the kind
    /// <c>Collection</c>, when any entity of such a type changed since it opened. The rule holds, at a price: the level watches a type, not a
    /// shelf, and a change of any book refuses the commit. It suits a decision over a whole type that rarely changes.
    /// </para>
    /// </summary>
    public sealed class Lesson17PessimisticTransactions
    {
        // Two books on the shelves.
        private static void Shelve(LibraryRepository library)
        {
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Price = 9.90m };
            new Book { Title = "Foundation", Price = 8.50m };
            transaction.Commit();
        }

        // The rule of the library: a book leaves the shelves when another one stays on them. The decision reads every book, and writes one.
        private static bool Borrow(string title)
        {
            if (BookCollection.Count(static book => book.Availability == Availability.Available) < 2)
                return false;
            BookCollection.First(title, static (book, title) => book.Title == title).Availability = Availability.Borrowed;
            return true;
        }

        /// <summary>At the <c>Optimistic</c> level, both decisions commit: each one is right on what it read, and the shelves are empty.</summary>
        [Fact]
        public async Task EmptyTheShelvesAtTheOptimisticLevel()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-optimistic", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            Shelve(library);
            var firstDecided = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();

            // Each transaction reads two books available, and borrows its own one.
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                Assert.True(Borrow("Dune"));
                firstDecided.SetResult();
                await secondCommitted.Task;

                // Dune and Foundation are two entities: nothing conflicts.
                transaction.Commit();
            });
            await firstDecided.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                Assert.True(Borrow("Foundation"));
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(0, BookCollection.Count(static book => book.Availability == Availability.Available));
            }
        }

        /// <summary>At the <c>Pessimistic</c> level, the later commit finds that the books it read changed: <c>Collection</c>. Run again, its decision refuses.</summary>
        [Fact]
        public async Task KeepABookOnTheShelvesAtThePessimisticLevel()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-pessimistic", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            Shelve(library);
            var firstDecided = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();

            // The level is chosen when the transaction opens.
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Pessimistic);
                Assert.True(Borrow("Dune"));
                firstDecided.SetResult();
                await secondCommitted.Task;

                // A book changed since this transaction read the books: its commit throws, and writes nothing.
                var conflict = Assert.Throws<TransactionConflictException>(transaction.Commit);
                Assert.Equal(TransactionConflictKind.Collection, conflict.Kind);
            });
            await firstDecided.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                Assert.True(Borrow("Foundation"));
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            // The application runs the work again: one book available, and the decision refuses.
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Pessimistic))
            {
                Assert.False(Borrow("Dune"));
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(1, BookCollection.Count(static book => book.Availability == Availability.Available));
            }
        }

        /// <summary>The level watches a type: a new price of the other book refuses the commit, though that book stays on the shelves and the rule holds.</summary>
        [Fact]
        public async Task ConflictOnAnyChangeOfATypeConsulted()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-type", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            Shelve(library);
            var firstDecided = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Pessimistic);
                Assert.True(Borrow("Dune"));
                firstDecided.SetResult();
                await secondCommitted.Task;
                var conflict = Assert.Throws<TransactionConflictException>(transaction.Commit);
                Assert.Equal(TransactionConflictKind.Collection, conflict.Kind);
            });
            await firstDecided.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.First(static book => book.Title == "Foundation").Price = 10.00m;
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;
        }

        /// <summary>A transaction that read no book has nothing to check: it adds one, and commits over a book changed meanwhile.</summary>
        [Fact]
        public async Task CheckOnlyWhatTheTransactionConsulted()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-read", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            Shelve(library);
            var firstAdded = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Pessimistic);
                new Book { Title = "Hyperion", Price = 11.00m };
                firstAdded.SetResult();
                await secondCommitted.Task;
                transaction.Commit();
            });
            await firstAdded.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.First(static book => book.Title == "Foundation").Price = 10.00m;
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(3, BookCollection.Count());
            }
        }
    }
}
