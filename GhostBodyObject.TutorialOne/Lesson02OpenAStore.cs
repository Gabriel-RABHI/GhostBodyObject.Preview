using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 2: open a Store, in memory or on disk, and its Repository.
    /// <para>
    /// Two objects work together. The Store keeps the data. The Repository binds your model to a Store: it knows the types
    /// Book and BookView, and opens the transactions that read and write them.
    /// </para>
    /// <para>
    /// Whether the data is persistent is only a matter of declaration: the line that creates the Store. In memory, the data
    /// lasts as long as the process; on disk, it is there again when the Store opens again, in this process or in another one.
    /// The Repository, the transactions and every line of your code that reads or writes the books are the same for both.
    /// </para>
    /// <para>
    /// A Store can also be shared, and distributed over a farm of servers: hundreds of servers can share the same storage
    /// space, transparently. There again, your code does not change: each server opens its transactions as this tutorial
    /// does, and reads and writes the same books. A later tutorial shows it.
    /// </para>
    /// </summary>
    public sealed class Lesson02OpenAStore
    {
        /// <summary>A Store in memory, then a Repository on it; both are closed by <c>using</c>, the Repository first.</summary>
        [Fact]
        public void OpenAStoreInMemoryAndItsRepository()
        {
            // A Store in memory keeps its data in the memory of the process: nothing remains when the process stops. It is
            // known by its name in the process: each lesson gives its own. StoreAccessMode.ReadWrite allows transactions that
            // write.
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02", StoreAccessMode.ReadWrite);

            // The Repository of the model, generated from MLibraryRepository, bound to the Store.
            using var library = new LibraryRepository(store);

            // Nothing is read or written out of a transaction: BookCollection, the books of the Repository, refuses to count
            // them. Lesson 3 opens the first transaction.
            Assert.Throws<InvalidOperationException>(() => BookCollection.Count());
        }

        /// <summary>A Store on disk: the same code, but the line that creates the Store.</summary>
        [Fact]
        public void OpenAStoreOnDiskAndItsRepository()
        {
            // The directory of the Store, next to the tutorial. It stays after the lesson: look at the files that GBO writes
            // there. The next run deletes it first.
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-02-library");
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);

            // The only difference with a Store in memory: a directory, and the name of the Store in it.
            using var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadWrite);

            // The same Repository, on a Store on disk.
            using var library = new LibraryRepository(store);

            Assert.True(Directory.Exists(directory));
        }
    }
}
