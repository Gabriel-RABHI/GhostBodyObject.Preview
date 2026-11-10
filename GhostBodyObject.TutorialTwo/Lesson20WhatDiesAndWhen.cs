using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Catalog
{
    using static CatalogRepositoryCollections;

    // The last model: every link of the tutorial in one catalog. Genres, eternal; artists, their [Orphanable] albums, their
    // tracks; recordings and lyrics, shared; the playlists that retain tracks; the favorites that do not; the festivals that
    // book artists.

    [Entity(1, 1, 1), Root, Enumerable]
    internal interface MGenre
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 2), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(3, 1, 3), Orphanable, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2), Strong]
        Option<MGenre> Genre { get; set; }

        [Property(3)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        Option<MArtist> Artist { get; }
    }

    [Entity(4, 1, 2), Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2), Strong]
        MRecording Recording { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    [Entity(5, 1, 2), Enumerable]
    internal interface MRecording
    {
        [Property(1)]
        VSeconds Seconds { get; set; }

        [Property(2), Strong]
        Option<MLyrics> Lyrics { get; set; }
    }

    [Entity(6, 1, 1), Enumerable]
    internal interface MLyrics
    {
        [Property(1)]
        VFreeText Text { get; set; }
    }

    [Entity(7, 1, 2), Root, Removable, Enumerable]
    internal interface MPlaylist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MEntry> Entries { get; }
    }

    [Entity(8, 1, 1)]
    internal interface MEntry
    {
        [Property(1), Strong]
        MTrack Track { get; set; }
    }

    [Entity(9, 1, 2), Root, Removable, Enumerable]
    internal interface MListener
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2), Weak]
        IEntityCollection<MAlbum> Favorites { get; }
    }

    [Entity(10, 1, 2), Root, Removable, Enumerable]
    internal interface MFestival
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2), Strong]
        IEntityCollection<MArtist> Lineup { get; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MGenre)), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MTrack)), IncludeEntityModel(nameof(MRecording)), IncludeEntityModel(nameof(MLyrics)), IncludeEntityModel(nameof(MPlaylist)), IncludeEntityModel(nameof(MEntry)), IncludeEntityModel(nameof(MListener)), IncludeEntityModel(nameof(MFestival))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 20: what dies, and when.
    /// <para>
    /// An entity lives while something holds it: a Root lives until its <c>Remove()</c>; a part, while its Owner holds it; a
    /// shared object, while a strong reference designates it; an orphan, while a strong reference retains it or one of its
    /// parts. GBO applies this rule at each instruction, to the entities that the instruction touches, by its counts and by the
    /// cascade of the Owners: it never walks the whole graph, and nothing passes later. When an instruction returns, what it
    /// released is gone, and what it refused is untouched.
    /// </para>
    /// <para>
    /// This is a deterministic garbage collection: the moment of each death is known, written in the code. The garbage
    /// collector of .NET frees an object some time after its last reference, when it decides to; GBO removes an entity at the
    /// instruction that releases its last retention, as Rust drops a value at the end of its last owner. A removal costs what
    /// it removes, an aggregate, never the size of the Store; and every refusal comes before any change.
    /// </para>
    /// </summary>
    public sealed class Lesson20WhatDiesAndWhen
    {
        /// <summary>One catalog, and four instructions: each one refuses, removes, or keeps, what the links say.</summary>
        [Fact]
        public void FollowEveryRetentionOfACatalog()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-20-catalog", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var rock = new Genre { Name = "Rock" };

            // David Bowie: Low, two tracks; Lodger, one track. The lyrics of "Sound and Vision", shared with a cover by Brian Eno.
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            low.Title = "Low";
            low.Genre = rock;
            var sound = low.Tracks.New(new Recording.Init());
            sound.Title = "Sound and Vision";
            var lyrics = sound.Recording.Lyrics.New();
            low.Tracks.New(new Recording.Init()).Title = "Speed of Life";
            var lodger = bowie.Albums.New();
            lodger.Title = "Lodger";
            lodger.Tracks.New(new Recording.Init()).Title = "Boys Keep Swinging";
            var eno = new Artist { Name = "Brian Eno" };
            var green = eno.Albums.New();
            green.Title = "Another Green World";
            green.Tracks.New(new Recording.Init()).Recording.Lyrics.Set(lyrics);

            // A playlist retains "Sound and Vision"; a listener likes Low and Lodger, weakly; a festival books Bowie.
            var favorites = new Playlist { Name = "Favorites" };
            var entry = favorites.Entries.New(sound);
            var ada = new Listener { Name = "Ada" };
            ada.Favorites.Add(low);
            ada.Favorites.Add(lodger);
            var montreux = new Festival { Name = "Montreux" };
            montreux.Lineup.Add(bowie);
            Assert.Equal((2, 3, 4, 4, 1), Census());

            // 1. Bowie is booked: the removal is refused, and nothing changes.
            Assert.Throws<InvalidOperationException>(bowie.Remove);
            Assert.Equal((2, 3, 4, 4, 1), Census());

            // 2. Out of the lineup, Bowie is removed. Lodger dies with its track and its recording, which nothing else plays; Low
            // survives, an orphan, whole, since the playlist plays one of its tracks; the lyrics live, through the cover; the
            // favorites of Ada lose Lodger.
            montreux.Lineup.Remove(bowie);
            bowie.Remove();
            Assert.Equal((1, 2, 3, 3, 1), Census());
            Assert.False(low.Artist.Any());
            Assert.Same(low, ada.Favorites.Single());

            // 3. The entry removed: the orphan dies, with its tracks and their recordings; the lyrics live still, through the cover.
            favorites.Entries.Remove(entry);
            Assert.Equal((1, 1, 1, 1, 1), Census());
            Assert.Equal(0, ada.Favorites.Count());

            // 4. Eno removed: its album, its track, its recording, and the lyrics, which nothing designates any more.
            eno.Remove();
            Assert.Equal((0, 0, 0, 0, 0), Census());

            // The genre is eternal; the playlist, the listener and the festival are Roots, untouched.
            Assert.Equal((1, 1, 1, 1), (GenreCollection.Count(), PlaylistCollection.Count(),
                ListenerCollection.Count(), FestivalCollection.Count()));
            transaction.Commit();
        }

        // The artists, the albums, the tracks, the recordings and the lyrics of the transaction.
        private static (int Artists, int Albums, int Tracks, int Recordings, int Lyrics) Census()
            => (ArtistCollection.Count(), AlbumCollection.Count(), TrackCollection.Count(),
                RecordingCollection.Count(), LyricsCollection.Count());
    }
}
