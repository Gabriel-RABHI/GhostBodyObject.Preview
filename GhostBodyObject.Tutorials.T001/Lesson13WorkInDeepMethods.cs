using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 13: query and change from deep methods, even inside a query.
    /// <para>
    /// In Entity Framework, a method that reads or writes needs the <c>DbContext</c>: it receives it as a parameter, or by
    /// dependency injection, and its changes wait for <c>SaveChanges()</c>. In GBO, the transaction is ambient: a method at
    /// any depth of the calls queries <c>BookCollection</c>, changes books and creates new ones directly. It receives nothing,
    /// and everything it does is visible at once (Lesson 3). The code that opened the transaction keeps the decision to commit.
    /// </para>
    /// <para>
    /// The lambda of a query is such a method too: it may change the books it reads, or create others. A book created while
    /// <c>BookCollection</c> is enumerated is enumerated too, as everything else is visible at once.
    /// </para>
    /// </summary>
    public sealed class Lesson13WorkInDeepMethods
    {
        // The end of the year of the library, in methods that call each other, as your services would. None of them receives
        // a transaction, a context or a repository.

        private static void CloseTheYear(Year year)
        {
            DiscountTheBooksBefore(1960, 10);
            ReprintTheBooksBefore(1950, year);
        }

        // Queries and changes.
        private static void DiscountTheBooksBefore(Year year, Percent rate)
        {
            foreach (var book in BookCollection.Where(year, static (book, before) => book.Year < before))
                book.Price -= rate.Of(book.Price);
        }

        // Queries and creates.
        private static void ReprintTheBooksBefore(Year before, Year year)
        {
            foreach (var book in BookCollection.Where(before, static (book, limit) => book.Year < limit))
                Reprint(book, year);
        }

        // Creates, deeper again.
        private static Book Reprint(Book book, Year year) => new() { Title = book.Title, Author = book.Author, Year = year, Price = book.Price };

        // Change or create, and say so: to be called in the lambda of a query.
        private static bool Lend(Book book)
        {
            book.Availability = Availability.Borrowed;
            return true;
        }

        private static bool Reprinted(Book book, Year year)
        {
            Reprint(book, year);
            return true;
        }

        /// <summary>Methods deep in the calls query, change and create books; the test, which opened the transaction, commits.</summary>
        [Fact]
        public void ChangeTheBooksFromDeepMethods()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-deep", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
                new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
                new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };

                // Three levels of calls below, the methods find this transaction by themselves.
                CloseTheYear(2026);

                // Their changes are visible here at once: two books discounted, one reprinted.
                Assert.Equal([9.90m, 7.65m, 6.21m, 6.21m], BookCollection.Select(static book => book.Price).ToList());
                Assert.Equal(2, BookCollection.Count(static book => book.Title == "1984"));

                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(4, BookCollection.Count());
            }
        }

        /// <summary>A query whose lambda changes the books it reads, then one whose lambda creates books, enumerated too.</summary>
        [Fact]
        public void ChangeAndCreateBooksInsideAQuery()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-query", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
            new Book { Title = "1984", Author = "George Orwell", Year = 1949 };

            // The lambda lends each old book it meets: the query changes the books while it reads them.
            Assert.Equal(2, BookCollection.Count(static book => book.Year < 1960 && Lend(book)));
            Assert.Equal(["Foundation", "1984"], BookCollection.Where(static book => book.Availability == Availability.Borrowed).Select(static book => book.Title).ToList());

            // The lambda reprints each book older than 1950, in 2026. The reprint is created during the enumeration, so the
            // same query meets it too, later: it is from 2026, and is not reprinted again.
            Assert.Equal(1, BookCollection.Count(2026, static (book, year) => book.Year < 1950 && Reprinted(book, year)));
            Assert.Equal(4, BookCollection.Count());
        }
    }
}
