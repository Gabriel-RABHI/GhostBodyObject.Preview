using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 14: the rules of a transaction.
    /// <para>
    /// The transaction of a Repository is the one of the current execution context: the code that runs in it, synchronous or
    /// after an <c>await</c>, finds it without receiving it. Three rules follow. Only one transaction of a Repository is open
    /// at a time in a context: they do not nest; other contexts, on other threads, run their own transactions at the same
    /// time (Lesson 15). A transaction that reads refuses every change. And the transaction follows
    /// the code across an <c>await</c>, even when it resumes on another thread.
    /// </para>
    /// </summary>
    public sealed class Lesson14TheRulesOfATransaction
    {
        /// <summary>The transaction of the context, and no second one of the same Repository in it.</summary>
        [Fact]
        public void OpenOneTransactionAtATime()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-one", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);

            // Out of a transaction, Current is null.
            Assert.Null(LibraryRepositoryTransaction.Current);

            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // Current is the transaction open in this context: the one that new Book() and BookCollection use.
                Assert.Same(transaction, LibraryRepositoryTransaction.Current);

                // A second transaction of the same Repository in the same context throws: close the first one before.
                Assert.Throws<InvalidOperationException>(() => library.OpenTransaction(StoreAccessMode.ReadOnly));
            }

            Assert.Null(LibraryRepositoryTransaction.Current);
        }

        /// <summary>A transaction that reads refuses to change a book, to create one, or to remove one; its commit writes nothing.</summary>
        [Fact]
        public void ChangeNothingInATransactionThatReads()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-read", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                transaction.Commit();
            }

            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var dune = BookCollection.Single();
                var error = Assert.Throws<InvalidOperationException>(() => dune.Price = 12.50m);
                Assert.StartsWith("The transaction is ReadOnly", error.Message);
                Assert.Throws<InvalidOperationException>(() => new Book { Title = "Foundation" });
                Assert.Throws<InvalidOperationException>(dune.Remove);

                // Commit has nothing to write: it closes the transaction, as Rollback.
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(9.90m, BookCollection.Single().Price);
            }
        }

        /// <summary>The transaction follows the code across an <c>await</c>.</summary>
        [Fact]
        public async Task FindTheTransactionAfterAnAwait()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-await", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune" };

                // The code resumes after the await, maybe on another thread: the transaction is still the one of its context.
                await Task.Delay(10);
                new Book { Title = "Foundation" };
                Assert.Same(transaction, LibraryRepositoryTransaction.Current);

                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(2, BookCollection.Count());
            }
        }
    }
}
