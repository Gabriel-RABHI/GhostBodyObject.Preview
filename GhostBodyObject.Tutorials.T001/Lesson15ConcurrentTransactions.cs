using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 15: concurrent transactions.
    /// <para>
    /// "One transaction at a time" holds in one execution context only (Lesson 14). GBO runs many transactions at the same
    /// time, on different threads: readers and writers together. A reader never waits for a writer, and writers do not wait
    /// for one another while they work; only their commits take their turn, one after the other, for a short moment.
    /// </para>
    /// <para>
    /// This is MVCC, multi-version concurrency control. A change creates a new version of its entity, private to its
    /// transaction. A commit publishes the new versions of its transaction, all together. A transaction reads the versions
    /// that were committed when it opened: the Store as it was then, whatever is committed after. To see the commits made
    /// since, open a new transaction.
    /// </para>
    /// <para>
    /// Each thread below runs in a <c>Task.Run</c> of its own, so that each one has its own execution context, and its own
    /// transaction. A <c>TaskCompletionSource</c> is a signal: one thread sets it, another one waits for it.
    /// </para>
    /// </summary>
    public sealed class Lesson15ConcurrentTransactions
    {
        /// <summary>A transaction keeps reading the Store as it was when it opened, while another one commits.</summary>
        [Fact]
        public async Task ReadTheStoreAsItWasWhenTheTransactionOpened()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-snapshot", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            var readerOpened = new TaskCompletionSource();
            var writerCommitted = new TaskCompletionSource();

            // The reader opens its transaction while the library is empty, then waits for the writer.
            var reader = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly);
                readerOpened.SetResult();
                await writerCommitted.Task;

                // The book was committed after this transaction opened: this transaction does not see it.
                return BookCollection.Count();
            });

            // The writer commits a book while the reader's transaction is open.
            await readerOpened.Task;
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                new Book { Title = "Dune" };
                transaction.Commit();
            });
            writerCommitted.SetResult();

            Assert.Equal(0, await reader);

            // A transaction opened now sees the book.
            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(1, BookCollection.Count());
            }
        }

        /// <summary>
        /// A thread starts first, and looks until it sees the books of two writers that start after it; then it adds a third
        /// book.
        /// </summary>
        [Fact]
        public async Task WriteFromThreeThreadsAtTheSameTime()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-threads", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);

            // The number of commits of the writers, that the looking thread checks its count against.
            int committed = 0;

            // The thread that looks starts first. It opens a transaction, counts the books, and starts again in a new
            // transaction until it sees both: a transaction sees the commits made before it opened, never those made after.
            var looking = Task.Run(() =>
            {
                while (true)
                {
                    bool bothCommitted = Volatile.Read(ref committed) == 2;
                    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                    if (BookCollection.Count() == 2)
                    {
                        new Book { Title = "Neuromancer" };
                        transaction.Commit();
                        return;
                    }

                    // Not yet: this transaction closes at the end of the turn, and the next turn opens a new one. Had both
                    // writers committed before it opened, it would have seen their books.
                    Assert.False(bothCommitted, "A transaction opened after two commits must see them.");
                }
            });

            // Then the two writers, each in its own thread and its own transaction, at the same time.
            var first = Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                new Book { Title = "Dune" };
                transaction.Commit();
                Interlocked.Increment(ref committed);
            });
            var second = Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                new Book { Title = "Foundation" };
                transaction.Commit();
                Interlocked.Increment(ref committed);
            });

            await Task.WhenAll(looking, first, second);

            // A last transaction sees the three books.
            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(["Dune", "Foundation", "Neuromancer"], BookCollection.Select(static book => book.Title).Order().ToList());
            }
        }
    }
}
