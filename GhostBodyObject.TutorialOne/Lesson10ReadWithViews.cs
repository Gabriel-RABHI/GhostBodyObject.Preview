using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 10: read many entities with Views.
    /// <para>
    /// A <c>Book</c>, a Body, is an object: the transaction keeps each one it gives, so that one entity is always the same
    /// object. A <c>BookView</c> is a read-only struct: the transaction keeps nothing of it, and it allocates nothing. To read
    /// or search many books, use the Views, <c>BookViewCollection</c>; to change the few you found, take their Bodies, with
    /// <c>ToBody()</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson10ReadWithViews
    {
        /// <summary>The same queries over the Views as over the Bodies.</summary>
        [Fact]
        public void SearchWithViews()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-search", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
                new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
                new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // A View has the properties of the book, to read only: it has no setter.
                BookView dune = BookViewCollection.First(static book => book.Title == "Dune");
                Assert.Equal("Frank Herbert", dune.Author);
                Assert.Equal(1965, dune.Year);

                Assert.Equal(2, BookViewCollection.Count(10m, static (book, price) => book.Price < price));
                Assert.Equal(["Foundation", "Dune", "Neuromancer"], BookViewCollection.OrderBy(static book => book.Year).Select(static book => book.Title).ToList());
            }
        }

        /// <summary>Search with the Views, then change the books found through their Bodies.</summary>
        [Fact]
        public void ChangeTheBooksFoundWithTheirBodies()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-change", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                new Book { Title = "Foundation", Price = 8.50m };
                new Book { Title = "Neuromancer", Price = 10.90m };
                transaction.Commit();
            }

            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The Views find the cheap books; only those become Bodies, to be changed.
                foreach (var cheap in BookViewCollection.Where(10m, static (book, price) => book.Price < price).ToList())
                    cheap.ToBody().Price += 1m;

                // A View reads the current version of its book: it sees the change made through the Body.
                BookView dune = BookViewCollection.First(static book => book.Title == "Dune");
                Assert.Equal(10.90m, dune.Price);

                // ToView goes the other way, from a Body to its View.
                Book foundation = BookCollection.First(static book => book.Title == "Foundation");
                Assert.Equal(9.50m, foundation.ToView().Price);

                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal([10.90m, 9.50m, 10.90m], BookViewCollection.Select(static book => book.Price).ToList());
            }
        }
    }
}
