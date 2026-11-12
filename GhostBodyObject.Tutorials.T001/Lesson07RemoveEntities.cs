using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 7: remove entities.
    /// <para>
    /// A type marked <c>[Removable]</c> has a method <c>Remove()</c>. A removed book leaves the collection of its transaction
    /// at once, and the Store at the commit.
    /// </para>
    /// </summary>
    public sealed class Lesson07RemoveEntities
    {
        /// <summary>A book removed, then gone after the commit.</summary>
        [Fact]
        public void RemoveABook()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-remove", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune" };
                new Book { Title = "Foundation" };
                transaction.Commit();
            }

            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                BookCollection.First(static book => book.Title == "Dune").Remove();

                // Gone from the transaction at once: its queries no longer find it, before any commit (Lesson 3).
                Assert.Equal(1, BookCollection.Count());

                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Foundation", BookCollection.Single().Title);
            }
        }
    }
}
