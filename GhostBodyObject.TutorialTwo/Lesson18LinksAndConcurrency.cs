using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialTwo.Retention
{
    using static StreamingRepositoryCollections;

    /// <summary>
    /// Lesson 18: links and concurrent transactions.
    /// <para>
    /// An aggregate is a unit of conflict: a part added or removed changes its Owner. Two transactions that change the parts
    /// of the same album conflict at the second commit, as two changes of one entity do (Tutorial One, Lesson 16): a rule of
    /// the album, checked by each of them, could not be broken by both together. The parts of two albums never conflict.
    /// </para>
    /// <para>
    /// A link never designates an entity that a concurrent transaction removed. A strong reference added to a track while
    /// another transaction removes it: both change the count of the strong references of the track, and the second commit
    /// throws, writing nothing. The application retries, and sees the track removed.
    /// </para>
    /// </summary>
    public sealed class Lesson18LinksAndConcurrency
    {
        /// <summary>Two transactions add a track to the same album: the second commit throws. Two albums: both commit.</summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConflictOnTheSameAggregate(bool sameAlbum)
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore($"lesson-18-aggregate-{sameAlbum}", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                bowie.Albums.New().Title = "Low";
                bowie.Albums.New().Title = "Heroes";
                transaction.Commit();
            }
            var firstAdded = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
                AlbumCollection.Single(static a => a.Title == "Low").Tracks.New().Title = "Speed of Life";
                firstAdded.SetResult();
                await secondCommitted.Task;
                if (sameAlbum)
                    Assert.Throws<TransactionConflictException>(transaction.Commit);
                else
                    transaction.Commit();
            });
            await firstAdded.Task;
            await Task.Run(() =>
            {
                using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
                AlbumCollection.Single(sameAlbum ? static a => a.Title == "Low" : static a => a.Title == "Heroes").Tracks.New().Title = "Breaking Glass";
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;
            using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(sameAlbum ? 1 : 2, TrackCollection.Count());
        }

        /// <summary>An entry added to a playlist while another transaction removes its album: the second commit throws.</summary>
        [Fact]
        public async Task NeverDesignateATargetRemovedConcurrently()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-18-removed", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Artist { Name = "David Bowie" }.Albums.New().Tracks.New().Title = "Sound and Vision";
                new Playlist { Name = "Favorites" };
                transaction.Commit();
            }
            var entryAdded = new TaskCompletionSource();
            var removalCommitted = new TaskCompletionSource();

            // The first transaction puts the track in the playlist: in its snapshot, the track exists.
            var first = Task.Run(async () =>
            {
                using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
                PlaylistCollection.Single().Entries.New(TrackCollection.Single());
                entryAdded.SetResult();
                await removalCommitted.Task;
                Assert.Throws<TransactionConflictException>(transaction.Commit);
            });

            // The second one removes the album, with its track, and commits first: nothing retained it in its snapshot.
            await entryAdded.Task;
            await Task.Run(() =>
            {
                using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
                ArtistCollection.Single().Remove();
                transaction.Commit();
            });
            removalCommitted.SetResult();
            await first;

            using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal((0, 0), (TrackCollection.Count(), EntryCollection.Count()));
        }
    }
}
