using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Sharing
{
    using static CatalogRepositoryCollections;

    /// <summary>
    /// Lesson 12: a strong slot, and the order of the retentions.
    /// <para>
    /// An optional strong reference to a shared object, <c>[Strong] Option&lt;MLyrics&gt; Lyrics</c>, is a strong slot. Its
    /// <c>New</c> creates the object and designates it; <c>Set</c> designates an existing one; <c>Clear</c> releases it. Every
    /// release removes the object when it was its last retention, at that instruction.
    /// </para>
    /// <para>
    /// So the order of the instructions matters. To pass a shared object from one holder to another, take the new retention
    /// before releasing the old one, as you clone an <c>Rc</c> in Rust before you drop the first one. Released first, the
    /// object is removed, and the new reference to it is refused: there is nothing left to designate.
    /// </para>
    /// </summary>
    public sealed class Lesson12TakeTheNewRetentionFirst
    {
        /// <summary>The lyrics of a song, born in its recording, shared with a cover version; removed with their last reference.</summary>
        [Fact]
        public void ShareLyricsThroughAStrongSlot()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-slot", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var album = new Artist { Name = "David Bowie" }.Albums.New();
            var original = album.Tracks.New(new Recording.Init()).Recording;
            var cover = album.Tracks.New(new Recording.Init()).Recording;

            original.Lyrics.New().Text = "I will be king";
            cover.Lyrics.Set(original.Lyrics.Get());
            Assert.Same(original.Lyrics.Get(), cover.Lyrics.Get());

            original.Lyrics.Clear();
            Assert.Equal(1, LyricsCollection.Count());
            cover.Lyrics.Clear();
            Assert.Equal(0, LyricsCollection.Count());
            transaction.Commit();
        }

        /// <summary>The new retention first, then the release: the lyrics pass from one recording to the other, and live.</summary>
        [Fact]
        public void TakeTheNewRetentionBeforeReleasingTheOldOne()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-order", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var album = new Artist { Name = "David Bowie" }.Albums.New();
            var demo = album.Tracks.New(new Recording.Init()).Recording;
            var final = album.Tracks.New(new Recording.Init()).Recording;
            var lyrics = demo.Lyrics.New();

            final.Lyrics.Set(lyrics);
            demo.Lyrics.Clear();
            Assert.Same(lyrics, final.Lyrics.Get());
            transaction.Commit();
        }

        /// <summary>The release first: the lyrics are removed at once, and the new reference to them is refused.</summary>
        [Fact]
        public void LoseASharedObjectReleasedFirst()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-lost", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var album = new Artist { Name = "David Bowie" }.Albums.New();
            var demo = album.Tracks.New(new Recording.Init()).Recording;
            var final = album.Tracks.New(new Recording.Init()).Recording;
            var lyrics = demo.Lyrics.New();

            demo.Lyrics.Clear();
            Assert.Contains("removed", Assert.Throws<InvalidOperationException>(() => final.Lyrics.Set(lyrics)).Message);
            Assert.False(final.Lyrics.Any());
            transaction.Commit();
        }
    }
}
