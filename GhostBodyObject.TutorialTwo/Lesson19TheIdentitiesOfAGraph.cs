using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Identities
{
    using static CatalogRepositoryCollections;

    // The eleventh model: the catalog of Lesson 4, whose entities have a business identity, [WithIdentity]: an ArtistRef, an
    // AlbumRef, a ReleaseRef, a TrackRef, that an event, a message or another system keeps.

    [Entity(1, 1, 2), Root, Removable, WithIdentity]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(2, 1, 3), WithIdentity, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        MRelease Release { get; }

        [Property(3)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        MArtist Artist { get; }
    }

    [Entity(3, 1, 1), WithIdentity]
    internal interface MRelease
    {
        [Property(1)]
        VName Label { get; set; }
    }

    [Entity(4, 1, 1), WithIdentity, Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MRelease)), IncludeEntityModel(nameof(MTrack))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 19: the identities of a graph.
    /// <para>
    /// An entity with a business identity, <c>[WithIdentity]</c>, is born with it: every birth of its type takes it, first,
    /// and GBO never draws one. A Root by its constructor, <c>new Artist(artistRef)</c>; a part by its <c>New</c>,
    /// <c>album.Tracks.New(trackRef)</c>; a required part by its descriptor, <c>new Release.Init(releaseRef)</c>.
    /// <c>ArtistRef.New()</c> draws a new identity, anywhere in the code, out of any transaction as well: the code decides
    /// where each identity comes from. An identity forgotten does not compile.
    /// </para>
    /// <para>
    /// This is what an event-sourced application needs. A command draws every identity of the graph it creates, and writes
    /// them in its event; applying the event creates the graph with them. Replayed, in a Replay transaction, the event creates
    /// the same graph, of the same identities: there, <c>ArtistRef.New()</c> throws, since every identity must come from the
    /// event. A null identity, or one given twice, is refused before anything is born.
    /// </para>
    /// </summary>
    public sealed class Lesson19TheIdentitiesOfAGraph
    {
        // The event of an album released: every identity of its graph, drawn by the command.
        private sealed record AlbumReleased(ArtistRef Artist, string Name, AlbumRef Album, string Title, ReleaseRef Release, TrackRef[] Tracks);

        // The application of the event: each birth takes its identity from it. The command and the Replay run this same code.
        private static void Apply(AlbumReleased released)
        {
            var artist = new Artist(released.Artist) { Name = released.Name };
            var album = artist.Albums.New(released.Album, new Release.Init(released.Release));
            album.Title = released.Title;
            foreach (var track in released.Tracks)
                album.Tracks.New(track);
        }

        /// <summary>The graph created by a command, then by the Replay of its event in another Store: the same identities.</summary>
        [Fact]
        public void CreateTheSameGraphByACommandAndByAReplay()
        {
            // The command draws the identities, out of any transaction.
            var released = new AlbumReleased(ArtistRef.New(), "David Bowie", AlbumRef.New(), "Low", ReleaseRef.New(), [TrackRef.New(), TrackRef.New()]);

            // One transaction of the model at a time in this flow: the command writes its Store, then the Replay writes the other one.
            using (var commandStore = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-command", StoreAccessMode.ReadWrite))
            using (var command = new CatalogRepository(commandStore))
            {
                using (var transaction = command.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    Apply(released);
                    transaction.Commit();
                }
                AssertTheGraph(command, released);
            }

            using var replayStore = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-replay", StoreAccessMode.ReadWrite);
            using var replay = new CatalogRepository(replayStore);
            using (var transaction = replay.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
            {
                Apply(released);
                transaction.Commit();
            }
            AssertTheGraph(replay, released);
        }

        // In both Stores, each identity resolves to its entity, in the same place of the graph.
        private static void AssertTheGraph(CatalogRepository catalog, AlbumReleased released)
        {
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var album = released.Album.Resolve();
                Assert.Equal("Low", album.Title);
                Assert.Equal((released.Artist, released.Release), (album.Artist.Ref, album.Release.Ref));
                Assert.Same(album, released.Tracks[1].Resolve().Album);
            }
        }

        /// <summary>In a Replay, no identity is drawn: <c>New()</c> throws there, and works again everywhere else.</summary>
        [Fact]
        public void DrawNoIdentityInAReplay()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-draw", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (catalog.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
                Assert.Throws<InvalidOperationException>(() => ArtistRef.New());
            using (catalog.OpenTransaction(StoreAccessMode.ReadWrite))
                Assert.False(ArtistRef.New().IsNull);
            Assert.False(ArtistRef.New().IsNull);
        }

        /// <summary>A null identity, or one given twice, is refused before anything is born.</summary>
        [Fact]
        public void RefuseANullOrARepeatedIdentity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-refuse", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var artist = new Artist(ArtistRef.New()) { Name = "David Bowie" };
            var track = TrackRef.New();

            Assert.Throws<ArgumentException>(() => new Artist(default));
            Assert.Throws<ArgumentException>(() => new Release.Init(default));
            var album = artist.Albums.New(AlbumRef.New(), new Release.Init(ReleaseRef.New()));
            album.Tracks.New(track);
            Assert.Throws<InvalidOperationException>(() => album.Tracks.New(track));

            Assert.Equal((1, 1), (AlbumCollection.Count(), TrackCollection.Count()));
            Assert.Null(transaction.CondemnedBy);
            transaction.Commit();
        }
    }
}
