using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Compositions
{
    using static CatalogRepositoryCollections;

    /// <summary>
    /// Lesson 6: a part in several compositions of its Owner, and a part that moves.
    /// <para>
    /// A part has one Owner, but may be in several compositions of it: a track in the <c>Tracks</c> of its album, and in its
    /// <c>Highlights</c>. <c>Add</c> places it there; <c>Remove</c> takes it out, and removes it only when it leaves the last
    /// composition of its Owner.
    /// </para>
    /// <para>
    /// <c>MoveTo</c> gives a part to another Owner, in one operation: it leaves every composition of its old Owner, and enters
    /// the new one. It is never without an Owner, not even between two instructions. <c>Add</c> refuses a part of another
    /// Owner: moving it is the work of <c>MoveTo</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson06MoveAPart
    {
        /// <summary>A track in two collections of its album: removed from one, it stays in the other.</summary>
        [Fact]
        public void PutAPartInTwoCompositions()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-two", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var low = new Artist { Name = "David Bowie" }.Albums.New();
            var sound = low.Tracks.New();
            sound.Title = "Sound and Vision";

            Assert.True(low.Highlights.Add(sound));
            Assert.False(low.Highlights.Add(sound));

            // Out of the highlights, the track is still in the tracks: it lives.
            low.Highlights.Remove(sound);
            Assert.Equal((1, 0, 1), (low.Tracks.Count(), low.Highlights.Count(), TrackCollection.Count()));
            transaction.Commit();
        }

        /// <summary>A track moved to another album leaves every collection of the first; an album moved to another artist.</summary>
        [Fact]
        public void MoveAPartToAnotherOwner()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-move", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var heroes = bowie.Albums.New();
            var track = low.Tracks.New();
            low.Highlights.Add(track);

            track.MoveTo(heroes.Tracks);
            Assert.Same(heroes, track.Album);
            Assert.Equal((0, 0, 1), (low.Tracks.Count(), low.Highlights.Count(), heroes.Tracks.Count()));

            // A whole album, with its parts, to another artist.
            var eno = new Artist { Name = "Brian Eno" };
            heroes.MoveTo(eno.Albums);
            Assert.Same(eno, track.Album.Artist);

            // Add refuses a part of another Owner: MoveTo moves it.
            Assert.Throws<InvalidOperationException>(() => low.Tracks.Add(track));
            transaction.Commit();
        }
    }
}
