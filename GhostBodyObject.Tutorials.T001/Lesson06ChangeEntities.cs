using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 6: change entities.
    /// <para>
    /// In a transaction that writes, a book changes as any object: set its properties. The transaction sees the change at once;
    /// the Store, after <c>Commit()</c>. During a transaction, one entity is always one and the same object: however you find
    /// a book again, you get the same <c>Book</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson06ChangeEntities
    {
        /// <summary>A price changed in a transaction, then read again after the commit.</summary>
        [Fact]
        public void ChangeABookAndCommit()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-change", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                new Book { Title = "Foundation", Price = 8.50m };
                transaction.Commit();
            }

            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // First finds the first book that meets the condition. Lesson 8 explains the word "static".
                var dune = BookCollection.First(static book => book.Title == "Dune");
                dune.Price = 12.50m;

                // The transaction sees the change at once, even in a query that filters on the new value, with nothing to save
                // first (Lesson 3); and it finds the same object again.
                Assert.Equal(1, BookCollection.Count(static book => book.Price > 10m));
                Assert.Equal(12.50m, BookCollection.First(static book => book.Title == "Dune").Price);
                Assert.Same(dune, BookCollection.First(static book => book.Title == "Dune"));

                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(12.50m, BookCollection.First(static book => book.Title == "Dune").Price);
                Assert.Equal(8.50m, BookCollection.First(static book => book.Title == "Foundation").Price);
            }
        }
    }
}
