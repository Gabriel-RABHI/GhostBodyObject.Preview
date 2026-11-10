using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Compositions
{
    using static CatalogRepositoryCollections;

    /// <summary>
    /// Lesson 5: removing an Owner removes its parts.
    /// <para>
    /// A part lives in its Owner. Taken out of its composition, by <c>Remove</c>, it is removed: there is no part without an
    /// Owner (Lesson 10 shows the exception). Its Owner removed, it is removed with it, and its own parts, at the same
    /// instruction: the cascade of <c>ON DELETE CASCADE</c> in SQL. There is no collector that passes later: when
    /// <c>Remove()</c> returns, the whole aggregate is gone.
    /// </para>
    /// <para>
    /// The Body of a removed entity still reads its last values; writing to it throws.
    /// </para>
    /// </summary>
    public sealed class Lesson05RemovingAnOwnerRemovesItsParts
    {
        /// <summary>A track taken out of its album is removed; so is a booklet taken out of its slot.</summary>
        [Fact]
        public void RemoveAPartFromItsOwner()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-part", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var low = new Artist { Name = "David Bowie" }.Albums.New();
            var speed = low.Tracks.New();
            speed.Title = "Speed of Life";
            low.Tracks.New().Title = "Breaking Glass";
            low.Booklet.New();

            Assert.True(low.Tracks.Remove(speed));
            Assert.True(low.Booklet.Remove());
            Assert.Equal((1, 0), (TrackCollection.Count(), BookletCollection.Count()));

            // The removed track still reads its last values; writing to it throws.
            Assert.Equal("Speed of Life", speed.Title);
            Assert.Throws<InvalidOperationException>(() => speed.Title = "Speed");
            transaction.Commit();
        }

        /// <summary>The removal of an artist removes its albums, their releases, their tracks and their booklets, at once.</summary>
        [Fact]
        public void RemoveAWholeAggregate()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-cascade", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                foreach (string title in new[] { "Low", "Heroes", "Lodger" })
                {
                    var album = bowie.Albums.New();
                    album.Title = title;
                    album.Booklet.New();
                    for (int i = 0; i < 10; i++)
                        album.Tracks.New();
                }
                new Artist { Name = "Brian Eno" }.Albums.New().Tracks.New();
                transaction.Commit();
            }
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ArtistCollection.Single(static a => a.Name == "David Bowie").Remove();

                // The cascade is done when Remove() returns: only the aggregate of Brian Eno remains.
                Assert.Equal((1, 1, 1, 1, 0), (ArtistCollection.Count(), AlbumCollection.Count(), ReleaseCollection.Count(),
                    TrackCollection.Count(), BookletCollection.Count()));
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal((1, 1), (AlbumCollection.Count(), TrackCollection.Count()));
        }
    }
}
