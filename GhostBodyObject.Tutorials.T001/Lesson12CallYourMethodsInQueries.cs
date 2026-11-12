using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 12: call your own methods in the queries.
    /// <para>
    /// In Entity Framework, a LINQ query on a <c>DbSet</c> is not run: it is translated to SQL, and run by the database. A
    /// method of your own in a <c>Where</c> has no translation, and the query throws when it runs. In GBO, a query runs in
    /// your process, over the objects themselves: its lambda is plain C#, and may call any method, read any computed
    /// property, and even run another query.
    /// </para>
    /// <para>
    /// The collections are ambient: <c>BookCollection</c> finds the transaction of the context by itself. A lambda has no
    /// context to capture to query the books, so it stays static, and allocates nothing (Lesson 8).
    /// </para>
    /// </summary>
    public sealed class Lesson12CallYourMethodsInQueries
    {
        // A rule of the library, in a plain static method.
        private static bool IsBargain(Book book) => book.Price < 8m;

        // A method that runs a query itself: it receives no context, no transaction, no collection.
        private static int CountBooksOf(AuthorName author) => BookCollection.Count(author, static (book, name) => book.Author == name);

        /// <summary>A method of your own, in the lambda of a query, or given to it directly.</summary>
        [Fact]
        public void CallAMethodInAQuery()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-method", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
            new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
            new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
            new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };

            // The method, called in the lambda: Entity Framework could not translate it.
            Assert.Equal(3, BookCollection.Count(static book => IsBargain(book)));

            // A static method can be given to the operator directly: it captures nothing either.
            Assert.Equal(3, BookCollection.Count(IsBargain));

            // The computed property of Lesson 11, a method of the object, in a query too.
            Assert.Equal(["Foundation", "I, Robot", "1984", "Brave New World"], BookCollection.Where(static book => book.IsClassic).Select(static book => book.Title).ToList());
        }

        /// <summary>A query in the lambda of a query: directly, or through a method that runs it.</summary>
        [Fact]
        public void RunAQueryInAQuery()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-inner", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };
            new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984 };

            // The books of the authors who wrote more than one: for each book, the method counts the books of its author.
            Assert.Equal(["Foundation", "I, Robot"], BookCollection.Where(static book => CountBooksOf(book.Author) > 1).Select(static book => book.Title).ToList());

            // The same inner query, written in the lambda: BookCollection is ambient, and the author goes to the inner lambda
            // as its state.
            var others = BookCollection.Select(static book => $"{book.Title}: {BookCollection.Count(book.Author, static (other, author) => other.Author == author) - 1} other")
                .ToList();
            Assert.Equal(["Dune: 0 other", "Foundation: 1 other", "I, Robot: 1 other", "Neuromancer: 0 other"], others);

            // Each inner query reads the books again: for N books, N inner queries. GBO reads them in memory, fast; for a
            // large collection, GroupBy (Lesson 8) groups them once.
        }
    }
}
