using GhostBodyObject.Runtime.Logical.Constants;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T001
{
    /// <summary>
    /// Lesson 5: commit, or not.
    /// <para>
    /// A transaction writes nothing until <c>Commit()</c>. Closed without it, by <c>Rollback()</c> or at the end of its
    /// <c>using</c>, it forgets every change: the Store stays as it was, and on disk, nothing reaches the files. A transaction
    /// is used once: committed or rolled back, it is closed.
    /// </para>
    /// </summary>
    public sealed class Lesson05CommitOrNot
    {
        /// <summary>A transaction closed without <c>Commit()</c> leaves nothing in the Store.</summary>
        [Fact]
        public void ForgetTheChangesOfATransactionNotCommitted()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-forget", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);

            // The end of the using closes the transaction: without Commit, the book is forgotten.
            using (library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune" };
            }

            // Rollback closes it explicitly, with the same effect.
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Foundation" };
                transaction.Rollback();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(0, BookCollection.Count());
            }
        }

        /// <summary>A transaction is committed once: then it is closed.</summary>
        [Fact]
        public void CommitATransactionOnce()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-once", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            new Book { Title = "Dune" };

            // An open transaction is Alive.
            Assert.Equal(GhostTransactionStatus.Alive, transaction.Status);

            transaction.Commit();

            // Committed, it is Closed: a second Commit throws. To write again, open a new transaction.
            Assert.Equal(GhostTransactionStatus.Closed, transaction.Status);
            Assert.Throws<InvalidOperationException>(transaction.Commit);
        }
    }
}
