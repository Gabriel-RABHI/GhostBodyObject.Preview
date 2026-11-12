using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 8: query with static lambdas.
    /// <para>
    /// The collections of GBO have the operators of LINQ, written by GBO for them: <c>Count</c>, <c>Where</c>, <c>Select</c>,
    /// <c>OrderBy</c>, <c>GroupBy</c>, <c>Sum</c>... Their lambdas follow one rule: a lambda captures nothing. A lambda that
    /// reads a variable from outside, a local, a parameter or <c>this</c>, makes .NET allocate an object at each call; GBO
    /// refuses it at compile time, with the error GBO013. The value a lambda needs is passed before it, as its state, and
    /// reaches it as a parameter, after the item: up to 4 values.
    /// </para>
    /// <para>
    /// LINQ itself, <c>System.Linq</c>, is refused on a collection of GBO, with the error GBO014: it would allocate too. For an
    /// operator that GBO does not have, <c>AsEnumerable()</c> leaves GBO explicitly.
    /// </para>
    /// </summary>
    public sealed class Lesson08Query
    {
        /// <summary>Counts and tests: a lambda without state, then with one.</summary>
        [Fact]
        public void CountAndTestBooks()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-count", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
            new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
            new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
            new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };

            Assert.Equal(6, BookCollection.Count());

            // "static" makes sure the lambda captures nothing: the compiler refuses a static lambda that would.
            Assert.Equal(2, BookCollection.Count(static book => book.Author == "Isaac Asimov"));

            // A value from outside is a state: passed before the lambda, received after the book. This line would not compile:
            //     BookCollection.Count(book => book.Year < before);   // error GBO013: the lambda captures "before"
            Year before = 1950;
            Assert.Equal(2, BookCollection.Count(before, static (book, year) => book.Year < year));

            // Two values of state, in their order.
            Assert.Equal(3, BookCollection.Count(1950, 1970, static (book, from, to) => book.Year >= from && book.Year < to));

            Assert.False(BookCollection.Any(static book => book.Year > 2000));
            Assert.True(BookCollection.All(static book => book.Price > 5m));
        }

        /// <summary>Filter, project, order and slice: each operator gives a new collection, read only when it is enumerated.</summary>
        [Fact]
        public void FilterOrderAndProjectBooks()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-filter", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
            new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
            new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
            new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };

            // Where keeps the books that meet the condition; Select computes a value from each one; ToList reads them.
            var recent = BookCollection.Where(1960, static (book, year) => book.Year >= year).Select(static book => book.Title).ToList();
            Assert.Equal(["Dune", "Neuromancer"], recent);

            // OrderBy orders by a key, ThenBy by a second key among equal ones; Take keeps the first ones.
            var byAuthor = BookCollection.OrderBy(static book => book.Author).ThenBy(static book => book.Year).Select(static book => book.Title).ToList();
            Assert.Equal(["Brave New World", "Dune", "1984", "I, Robot", "Foundation", "Neuromancer"], byAuthor);
            var dearest = BookCollection.OrderByDescending(static book => book.Price).Take(2).Select(static book => book.Title).ToList();
            Assert.Equal(["Neuromancer", "Dune"], dearest);
        }

        /// <summary>Aggregate the books: sums, averages, extremes, groups and dictionaries.</summary>
        [Fact]
        public void AggregateBooks()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-aggregate", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
            new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
            new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
            new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };

            Assert.Equal(51.60m, BookCollection.Sum(static book => book.Price));
            Assert.Equal(8.60m, BookCollection.Average(static book => book.Price.Value));
            Assert.Equal((1932, 1984), (BookCollection.Min(static book => book.Year), BookCollection.Max(static book => book.Year)));

            // MaxBy gives the book of the greatest key, where Max gives the key.
            Assert.Equal("Neuromancer", BookCollection.MaxBy(static book => book.Price).Title);

            // A text is read in place: a key that GroupBy or ToDictionary keeps is its string, ToString().

            // GroupBy gives one group per author, with its key and its books, in the order of their first book.
            var authors = BookCollection.GroupBy(static book => book.Author.ToString()).Select(static group => $"{group.Key}: {group.Count()}").ToList();
            Assert.Equal(["Frank Herbert: 1", "Isaac Asimov: 2", "William Gibson: 1", "George Orwell: 1", "Aldous Huxley: 1"], authors);

            // ToDictionary finds a book by a key: two books of the same key would throw.
            var byTitle = BookCollection.ToDictionary(static book => book.Title.ToString());
            Assert.Equal(1965, byTitle["Dune"].Year);
        }
    }
}
