using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialOne
{
    /// <summary>
    /// Lesson 18: exclusive transactions.
    /// <para>
    /// A long transaction that changes many books risks a conflict at its commit (Lesson 16), and then all its work is to do
    /// again. An exclusive transaction never conflicts: it takes the right to write when it opens, not when it commits, and
    /// keeps it until it closes. It reads the last version of each book, and no other commit can come before its own: the
    /// other commits wait for it. The transactions that read do not wait.
    /// </para>
    /// <para>
    /// It is exclusive on the whole farm of servers that share the Store (Lesson 2), not on this server only: the right to
    /// write that it takes is the one of the farm, and no server commits in the shared storage space while it is open.
    /// </para>
    /// <para>
    /// It suits the work that must succeed without being replayed: a bulk load, a migration, a maintenance. Its price is its
    /// guarantee: while it is open, no server of the farm commits. Keep it short, and bound its wait for the right to write with a
    /// <c>CancellationToken</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson18ExclusiveTransactions
    {
        /// <summary>
        /// An exclusive transaction and a transaction of another thread change the same book: the exclusive one commits first,
        /// the other one waits for it, then conflicts.
        /// </summary>
        [Fact]
        public async Task CommitAnExclusiveTransactionFirst()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-18-exclusive", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Dune", Price = 9.90m };
                transaction.Commit();
            }
            var exclusiveOpened = new TaskCompletionSource();
            var otherChanged = new TaskCompletionSource();

            // The exclusive transaction: TransactionConflictLevel.Exclusive, when it opens.
            var exclusive = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive);
                BookCollection.Single().Price = 12.50m;
                exclusiveOpened.SetResult();

                // It waits here for the other one to change the book too: nothing can be committed before it meanwhile.
                await otherChanged.Task;
                transaction.Commit();
            });
            await exclusiveOpened.Task;

            // Another transaction changes the same book. Its commit waits for the exclusive one to close, then finds the book
            // changed since it opened.
            var other = Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
                BookCollection.Single().Price = 11.00m;
                otherChanged.SetResult();
                Assert.Throws<TransactionConflictException>(transaction.Commit);
            });
            await Task.WhenAll(exclusive, other);

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(12.50m, BookCollection.Single().Price);
            }
        }

        /// <summary>The wait of an exclusive transaction for the right to write, bounded by a <c>CancellationToken</c>.</summary>
        [Fact]
        public async Task BoundTheWaitOfAnExclusiveTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-18-wait", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            var firstOpened = new TaskCompletionSource();
            var firstMayClose = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive);
                firstOpened.SetResult();
                await firstMayClose.Task;
            });
            await firstOpened.Task;

            // A second exclusive transaction waits for the first one to close; here, 100 milliseconds at most.
            await Task.Run(() =>
            {
                using var patience = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                Assert.ThrowsAny<OperationCanceledException>(() => library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive, cancellationToken: patience.Token));
            });

            // A transaction that reads never waits.
            await Task.Run(() =>
            {
                using var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly);
                Assert.Equal(0, BookCollection.Count());
            });

            firstMayClose.SetResult();
            await first;
        }
    }
}
