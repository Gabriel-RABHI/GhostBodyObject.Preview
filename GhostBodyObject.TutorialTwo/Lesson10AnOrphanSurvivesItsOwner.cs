using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Orphans
{
    using static StreamingRepositoryCollections;

    // The sixth model: the one of Lesson 9, where an album is [Orphanable]: it may survive its artist.

    [Entity(1, 1, 2), Root, Removable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    // [Orphanable]: an album that a strong reference retains survives the removal of its artist, whole, without Owner. Its
    // [OwnedFrom] is then an Option: an orphan has none.
    [Entity(2, 1, 2), Orphanable, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        Option<MArtist> Artist { get; }
    }

    [Entity(3, 1, 1), Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

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

    [Entity(5, 1, 1)]
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
    /// Lesson 10: an orphan survives its Owner.
    /// <para>
    /// In Lesson 9, a retained track refused the removal of its artist. Declared <c>[Orphanable]</c>, an album may survive its
    /// Owner instead: removing the artist removes the albums that nothing retains; each album that a playlist plays survives,
    /// whole, with all its tracks, without an artist. It is an orphan. The history stays readable: the playlist plays the
    /// album, its other tracks and its title.
    /// </para>
    /// <para>
    /// An orphan lives while something retains it. At the instruction that removes its last retention, the last entry of a
    /// playlist that designates one of its tracks, it is removed, with its subtree: nothing passes later to collect it. This is
    /// the deterministic garbage collection of GBO: an entity dies at the instruction that releases its last retention, as an
    /// object of Rust at the end of its last owner, not some time later, as an object of .NET.
    /// </para>
    /// </summary>
    public sealed class Lesson10AnOrphanSurvivesItsOwner
    {
        /// <summary>The artist removed: the album in a playlist survives without artist; the other one is removed.</summary>
        [Fact]
        public void KeepTheRetainedAlbumsOfARemovedArtist()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-orphan", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using (var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                var low = bowie.Albums.New();
                low.Title = "Low";
                var sound = low.Tracks.New();
                low.Tracks.New();
                bowie.Albums.New().Title = "Lodger";
                new Playlist { Name = "Favorites" }.Entries.New(sound);

                bowie.Remove();

                // Low survives, with its two tracks, and no artist; Lodger is gone.
                var orphan = AlbumCollection.Single();
                Assert.Equal("Low", orphan.Title);
                Assert.Equal(2, orphan.Tracks.Count());
                Assert.False(orphan.Artist.Any());
                transaction.Commit();
            }
            using (streaming.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var entry = PlaylistCollection.Single().Entries.Single();
                Assert.Equal("Low", entry.Track.Album.Title);
            }
        }

        /// <summary>The orphan dies at the instruction that removes its last retention.</summary>
        [Fact]
        public void RemoveAnOrphanWithItsLastRetention()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-collect", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var (sound, speed) = (low.Tracks.New(), low.Tracks.New());
            var favorites = new Playlist { Name = "Favorites" };
            var first = favorites.Entries.New(sound);
            var second = favorites.Entries.New(speed);
            bowie.Remove();

            // One retention left: the orphan lives.
            favorites.Entries.Remove(first);
            Assert.Equal(1, AlbumCollection.Count());

            // The last one: the orphan and its tracks are removed by this very instruction.
            favorites.Entries.Remove(second);
            Assert.Equal((0, 0), (AlbumCollection.Count(), TrackCollection.Count()));
            transaction.Commit();
        }

        /// <summary>An orphan placed under another artist by <c>Add</c>: it has an Owner again, and its retention no longer matters.</summary>
        [Fact]
        public void PlaceAnOrphanUnderAnotherArtist()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-adopt", StoreAccessMode.ReadWrite);
            using var streaming = new StreamingRepository(store);
            using var transaction = streaming.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var favorites = new Playlist { Name = "Favorites" };
            var entry = favorites.Entries.New(low.Tracks.New());

            // Taken out of its artist while retained: an [Orphanable] album becomes an orphan, instead of being removed.
            bowie.Albums.Remove(low);
            Assert.False(low.Artist.Any());

            var eno = new Artist { Name = "Brian Eno" };
            eno.Albums.Add(low);
            Assert.Same(eno, low.Artist.Get());

            // Its Owner holds it now: the end of its retention removes nothing.
            favorites.Entries.Remove(entry);
            Assert.Equal(1, AlbumCollection.Count());
            transaction.Commit();
        }
    }
}
