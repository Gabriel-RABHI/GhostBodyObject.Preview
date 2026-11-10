using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Retention
{
    using static StreamingRepositoryCollections;

    // The fifth model: the catalog of Lesson 4, and the playlists of the listeners, whose entries designate tracks by a strong
    // reference. A track is a part of another aggregate: a playlist does not own it, it retains it.

    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(2, 1, 2), Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        MArtist Artist { get; }
    }

    [Entity(3, 1, 2), Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        // A remix designates its original track, on the same album or on another one.
        [Property(2), Strong]
        Option<MTrack> RemixOf { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    [Entity(4, 1, 2), Root, Removable, Enumerable]
    internal interface MPlaylist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MEntry> Entries { get; }
    }

    [Entity(5, 1, 1), Enumerable]
    internal interface MEntry
    {
        [Property(1), Strong]
        MTrack Track { get; set; }
    }

    [GhostRepository(4), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MTrack)), IncludeEntityModel(nameof(MPlaylist)), IncludeEntityModel(nameof(MEntry))]
    internal interface MStreamingRepository
    {
    }

    /// <summary>
    /// Lesson 9: a strong reference to a part retains its whole chain.
    /// <para>
    /// An entry of a playlist designates a track by a strong reference: the track cannot be removed. Nor can its album, nor its
    /// artist: their removal would take the track with them, by the cascade of Lesson 5. A part requires its Owner, and its
    /// Owner requires its own: retaining a track retains the chain of its Owners, up to the Root. Each removal is refused at its
    /// call, by an exception that names the chain, from the removed entity to the retained one; nothing changes.
    /// </para>
    /// <para>
    /// A strong reference from the removed aggregate itself retains nothing: an album whose remix designates its original
    /// track, on the same album, is removed with both. And <c>MoveTo</c> changes the Owner of a retained track: it never has to
    /// be without one.
    /// </para>
    /// </summary>
    public sealed class Lesson09AReferenceToAPartRetainsItsChain
    {
        /// <summary>A track in a playlist: neither it, nor its album, nor its artist can be removed; the playlist entry first.</summary>
        [Fact]
        public void RefuseTheRemovalOfTheChainOfARetainedTrack()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-chain", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var sound = low.Tracks.New();
            var favorites = new Playlist { Name = "Favorites" };
            var entry = favorites.Entries.New(sound);

            // The exception names the chain: Artist → Album → Track, retained by the entry.
            Assert.Contains("Artist 1 → Album 1 → Track 1", Assert.Throws<InvalidOperationException>(bowie.Remove).Message);
            Assert.Throws<InvalidOperationException>(() => bowie.Albums.Remove(low));
            Assert.Throws<InvalidOperationException>(() => low.Tracks.Remove(sound));
            Assert.Null(transaction.CondemnedBy);

            // The entry removed, nothing retains the track: the whole aggregate goes.
            favorites.Entries.Remove(entry);
            bowie.Remove();
            Assert.Equal((0, 0), (AlbumCollection.Count(), TrackCollection.Count()));
            transaction.Commit();
        }

        /// <summary>A strong reference inside the removed aggregate retains nothing; a retained track moves to another album.</summary>
        [Fact]
        public void RetainNothingFromInsideAndMoveTheRetainedTrack()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-inside", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var original = low.Tracks.New();
            low.Tracks.New().RemixOf = original;

            // The remix and its original leave together: a reference from the removed album retains nothing.
            bowie.Albums.Remove(low);
            Assert.Equal(0, TrackCollection.Count());

            // A retained track can still change of album: it keeps an Owner, and the album it left can go.
            var heroes = bowie.Albums.New();
            var best = bowie.Albums.New();
            var track = heroes.Tracks.New();
            new Playlist { Name = "Favorites" }.Entries.New(track);
            track.MoveTo(best.Tracks);
            bowie.Albums.Remove(heroes);
            Assert.Same(best, track.Album);
            transaction.Commit();
        }
    }
}
