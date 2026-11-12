using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 3: create entities in a transaction.
    /// <para>
    /// Every read and every write happens in a transaction. A transaction is opened by the Repository, and becomes the
    /// transaction of this Repository in the current execution context, until it is closed: you never pass it to the objects.
    /// <c>new Book()</c> creates a book in it; <c>Commit()</c> writes the books in the Store. On a Store on disk, the same code
    /// makes them persistent.
    /// </para>
    /// <para>
    /// GBO is not a Unit of Work, as Entity Framework is. In Entity Framework, a <c>DbContext</c> collects your changes and sends
    /// them to the database at <c>SaveChanges()</c>: until then, a query on a <c>DbSet</c> asks the database, does not find the
    /// entities you added, and filters on the values stored before your changes. In GBO, the transaction is the memory you
    /// work in: each creation, change or removal is visible at once to every read and every query of the transaction, as in a
    /// collection of .NET objects. <c>Commit()</c> makes the changes durable, and visible to the transactions that open after it.
    /// </para>
    /// </summary>
    public sealed class Lesson03CreateEntities
    {
        /// <summary>Two books created in a transaction that writes, then counted in a transaction that reads.</summary>
        [Fact]
        public void CreateBooksAndCommitThem()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-create", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);

            // A transaction that writes: StoreAccessMode.ReadWrite.
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // A new Book belongs to the transaction open in this context: there is nothing else to call.
                new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
                new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };

                // The first enumeration of the collection: the new books are already there. In Entity Framework, a query would not
                // find them before SaveChanges(); in GBO, there is nothing to save first: every change is visible at once.
                Assert.Equal(2, BookCollection.Count());

                // Commit writes the changes of the transaction in the Store, and closes the transaction.
                transaction.Commit();
            }

            // A transaction that reads: StoreAccessMode.ReadOnly. It sees what was committed before it opened.
            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(2, BookCollection.Count());
            }
        }

        /// <summary>The same code on a Store on disk: closed, then opened again, the Store gives back the books.</summary>
        [Fact]
        public void FindTheBooksAfterTheStoreReopens()
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-03-library");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);

            // The Store on disk of Lesson 2, and exactly the code of the test above.
            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadWrite))
            using (var library = new LibraryRepository(store))
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
                new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
                transaction.Commit();
            }

            // The Store is closed: the books are in the files only. Opened again, here to read only, it gives them back.
            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadOnly))
            using (var library = new LibraryRepository(store))
            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(2, BookCollection.Count());
            }
        }

        /// <summary>A book needs a transaction: out of one, <c>new Book()</c> throws.</summary>
        [Fact]
        public void CreateNothingOutOfATransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-no-transaction", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);

            var error = Assert.Throws<InvalidOperationException>(() => new Book { Title = "Dune" });
            Assert.Equal("No LibraryRepositoryTransaction is open in this context.", error.Message);
        }
    }
}
