using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Sharing
{
    using static CatalogRepositoryCollections;

    // The seventh model: a track designates its recording, which several tracks may share, on an album and on a compilation;
    // a recording designates its lyrics, which a cover version shares with the original.

    [Entity(1, 1, 2), Root, Removable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(2, 1, 2)]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        MArtist Artist { get; }
    }

    [Entity(3, 1, 2)]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        // A strong reference to a type that no composition holds, and that is not Root: a shared object. Required, a T.
        [Property(2), Strong]
        MRecording Recording { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    /// <summary>A recording: not Root, held by no composition. It is born by its first strong reference.</summary>
    [Entity(4, 1, 2), Enumerable]
    internal interface MRecording
    {
        [Property(1)]
        VSeconds Seconds { get; set; }

        // A shared object may designate another one: an Option, a strong slot (Lesson 12).
        [Property(2), Strong]
        Option<MLyrics> Lyrics { get; set; }
    }

    [Entity(5, 1, 1), Enumerable]
    internal interface MLyrics
    {
        [Property(1)]
        VFreeText Text { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MTrack)), IncludeEntityModel(nameof(MRecording)), IncludeEntityModel(nameof(MLyrics))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 11: a shared object lives as long as its strong references.
    /// <para>
    /// A recording is neither a Root nor a part: no composition holds it, and nothing removes it explicitly. It is a shared
    /// object, retained by the strong references that designate it: the tracks of an album, and of the compilations that take
    /// it again. It is born by its first strong reference, and removed at the instruction that releases its last one. As a
    /// part, it has no public constructor: born out of any reference, nothing would retain it.
    /// </para>
    /// <para>
    /// The track requires its recording, a <c>T</c>: its birth takes a descriptor of it, <c>Recording.Init</c>, which creates a
    /// new recording, or designates an existing one, by an implicit conversion from it.
    /// </para>
    /// </summary>
    public sealed class Lesson11ASharedObjectLivesWithItsReferences
    {
        /// <summary>A recording born with its track; taken again by a compilation; removed with the last track that plays it.</summary>
        [Fact]
        public void ShareARecordingBetweenTwoAlbums()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-share", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = bowie.Albums.New();
            var best = bowie.Albums.New();

            // A new recording, born with the track: Recording.Init describes it.
            var original = low.Tracks.New(new Recording.Init());
            original.Recording.Seconds = 183;

            // The compilation takes the same recording again: an existing recording converts to its descriptor.
            var again = best.Tracks.New(original.Recording);
            Assert.Same(original.Recording, again.Recording);
            Assert.Equal(1, RecordingCollection.Count());

            // The original album removed: the compilation still retains the recording.
            bowie.Albums.Remove(low);
            Assert.Equal(183, again.Recording.Seconds);

            // The compilation removed: nothing retains the recording, removed by the same instruction.
            bowie.Albums.Remove(best);
            Assert.Equal(0, RecordingCollection.Count());
            transaction.Commit();
        }

        /// <summary>A track that designates another recording releases the old one: removed when it was its last reference.</summary>
        [Fact]
        public void ReleaseARecordingByASetter()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-release", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var low = new Artist { Name = "David Bowie" }.Albums.New();
            var track = low.Tracks.New(new Recording.Init());
            var old = track.Recording;

            // NewRecording, written by the generator for a required strong reference to a shared object: a new recording,
            // designated at once; the old one, released, is removed.
            var remaster = track.NewRecording();
            Assert.Same(remaster, track.Recording);
            Assert.Equal(1, RecordingCollection.Count());
            Assert.Throws<InvalidOperationException>(() => old.Seconds = 1);
            transaction.Commit();
        }
    }
}
