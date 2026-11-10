using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 4: read entities.
    /// <para>
    /// <c>BookCollection</c> enumerates the books of the transaction open in this context, in the order of their creation. It
    /// is a static property that the generator writes for each <c>[Enumerable]</c> type, usable anywhere in the project, with
    /// <c>foreach</c> as with any collection.
    /// </para>
    /// </summary>
    public sealed class Lesson04ReadEntities
    {
        /// <summary>The books, read one by one with <c>foreach</c>, in the order of their creation.</summary>
        [Fact]
        public void ReadEachBookWithForeach()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-foreach", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
                new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
                new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984 };
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var lines = new List<string>();
                foreach (var book in BookCollection)
                    lines.Add($"{book.Title}, {book.Author}, {book.Year}");

                Assert.Equal(["Dune, Frank Herbert, 1965", "Foundation, Isaac Asimov, 1951", "Neuromancer, William Gibson, 1984"], lines);
            }
        }

        /// <summary>A property never set has its default value: an empty text, never null, 0, the first value of an enum.</summary>
        [Fact]
        public void ReadTheDefaultValues()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-defaults", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);

            var book = new Book();

            // GBO has no null texts: a text never set is empty.
            Assert.Equal("", book.Title);
            Assert.Equal(0, book.Year);
            Assert.Equal(0m, book.Price);
            Assert.Equal(Availability.Available, book.Availability);
        }
    }
}
