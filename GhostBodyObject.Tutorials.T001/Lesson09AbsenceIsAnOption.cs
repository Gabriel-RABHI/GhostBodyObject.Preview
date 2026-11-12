using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 9: an absence is an Option, never null.
    /// <para>
    /// GBO gives no null. An operator that may find nothing, <c>FirstOrNone</c>, <c>SingleOrNone</c>, <c>MinOrNone</c>..., gives
    /// an <c>Option&lt;T&gt;</c>: zero or one item, which you cannot read without handling its absence. An operator that must
    /// find a value, <c>First</c>, <c>Single</c>, <c>Min</c>..., throws when there is none. An Option is a struct: it
    /// allocates nothing.
    /// </para>
    /// </summary>
    public sealed class Lesson09AbsenceIsAnOption
    {
        /// <summary>A book found, or not, and the ways to read an Option.</summary>
        [Fact]
        public void FindABookOrNone()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-option", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };

            Option<Book> dune = BookCollection.FirstOrNone(static book => book.Title == "Dune");
            Option<Book> hamlet = BookCollection.FirstOrNone(static book => book.Title == "Hamlet");

            // Any tells whether the item is there.
            Assert.True(dune.Any());
            Assert.False(hamlet.Any());

            // TryGet gives the item when it is there, as Dictionary.TryGetValue.
            if (dune.TryGet(out var book))
                Assert.Equal(1965, book.Year);

            // GetOr gives the item, or a fallback that you choose: never an implicit default value.
            Assert.Equal(0, hamlet.Select(static found => found.Year).GetOr(0));

            // Match handles both cases.
            Assert.Equal("not in the library", hamlet.Match(static found => found.Title.ToString(), static () => "not in the library"));

            // Get gives the item, and throws if it is absent: use it when the absence is a bug.
            Assert.Equal("Dune", dune.Get().Title);
            Assert.Throws<InvalidOperationException>(() => hamlet.Get());
        }

        /// <summary>The operators that must find a value throw when there is none, or when there is more than one.</summary>
        [Fact]
        public void ThrowWhenAValueIsDueAndMissing()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-throw", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
            new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };

            // First: no book of Shakespeare.
            Assert.Throws<InvalidOperationException>(() => BookCollection.First(static book => book.Author == "William Shakespeare"));

            // Single: two books of Asimov. SingleOrNone throws too: more than one is never "none".
            Assert.Throws<InvalidOperationException>(() => BookCollection.Single(static book => book.Author == "Isaac Asimov"));
            Assert.Throws<InvalidOperationException>(() => BookCollection.SingleOrNone(static book => book.Author == "Isaac Asimov"));

            // Max over no book throws; MaxOrNone gives none.
            var none = BookCollection.Where(static book => book.Year > 2000);
            Assert.Throws<InvalidOperationException>(() => none.Max(static book => book.Year));
            Assert.False(none.MaxOrNone(static book => book.Year).Any());
        }
    }
}
