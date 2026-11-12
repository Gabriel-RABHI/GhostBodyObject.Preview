using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Rules
{
    using static LabelRepositoryCollections;

    // The tenth model: the catalog of Lesson 4, with the duration of each album, kept by your code, and the notes of an archive.

    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(2, 1, 3), Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        // The sum of the durations of the tracks, kept by the rules of the partial classes below.
        [Property(2)]
        VSeconds Seconds { get; set; }

        [Property(3)]
        IEntityCollection<MTrack> Tracks { get; }

        [OwnedFrom]
        MArtist Artist { get; }
    }

    [Entity(3, 1, 2), Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VSeconds Seconds { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    /// <summary>A note of the archive of the label: what was removed, and when.</summary>
    [Entity(4, 1, 1), Root, Removable, Enumerable]
    internal interface MNote
    {
        [Property(1)]
        VFreeText Text { get; set; }
    }

    [GhostRepository(7), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MTrack)), IncludeEntityModel(nameof(MNote))]
    internal interface MLabelRepository
    {
    }

    // The rules of the album, in its partial class, as in Tutorial T001, Lesson 11.
    internal partial class Album
    {
        /// <summary>The most tracks an album may hold: a rule of the label (Lesson 17).</summary>
        public const int MaxTracks = 3;

        /// <summary>Computes the duration of the album again, from its tracks.</summary>
        internal void Recount() => Seconds = Tracks.Sum(static t => t.Seconds);

        /// <summary>A new track, refused before anything is written when the album is full (Lesson 17).</summary>
        public Track AddTrack(Title title, Seconds seconds)
        {
            if (Tracks.Count() >= MaxTracks)
                throw new InvalidOperationException($"The album {Title} holds {MaxTracks} tracks at most.");
            var track = Tracks.New();
            track.Title = title;
            track.Seconds = seconds;
            return track;
        }

        // For each collection, the generator declares two partial methods, called after a part joins it, or leaves it.
        partial void OnTracksAdded(Track item) => Recount();

        partial void OnTracksRemoved(Track item) => Recount();

        // OnDeleting: called once before the removal of the album, the album still readable, its artist as well.
        partial void OnDeleting() => new Note { Text = $"{Title}, by {Artist.Name}, removed" };
    }

    internal partial class Track
    {
        partial void OnSecondsChanged() => Album.Recount();

        // A rule checked after the change: its exception condemns the transaction (Lesson 17).
        partial void OnTitleChanged()
        {
            if (Title.Length == 0)
                throw new InvalidOperationException("A track has a title.");
        }
    }

    /// <summary>
    /// Lesson 16: your rules on the links.
    /// <para>
    /// The partial methods of Tutorial T001, <c>On...Changed</c>, exist for the links too: <c>On...Added</c> and
    /// <c>On...Removed</c> for a collection, called after a part joins it or leaves it; <c>On...Changed</c> for a reference and
    /// a slot. Each one runs after the change, once the operation of GBO is complete: it sees the state that the operation
    /// leaves. Here, an album keeps its duration, the sum of the durations of its tracks.
    /// </para>
    /// <para>
    /// <c>OnDeleting</c> runs once for each entity that a removal takes, from the Owners to their parts, after every check of
    /// the removal and before its writes: the entity, its parts and its Owners still read. It may create other entities, a note
    /// of an archive here; it cannot retain an entity that is being removed.
    /// </para>
    /// <para>
    /// GBO never calls your code to keep the links right: the counts, the cascades and the sets of referrers are its own work,
    /// done whatever your code does. Your code keeps your rules.
    /// </para>
    /// </summary>
    public sealed class Lesson16YourRulesOnTheLinks
    {
        /// <summary>The duration of an album follows its tracks: added, changed, removed.</summary>
        [Fact]
        public void KeepTheDurationOfAnAlbum()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-duration", StoreAccessMode.ReadWrite);
            using var label = new LabelRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var low = new Artist { Name = "David Bowie" }.Albums.New();
                low.Title = "Low";
                low.AddTrack("Speed of Life", 166);
                var sound = low.AddTrack("Sound and Vision", 183);
                Assert.Equal(349, low.Seconds);

                sound.Seconds = 185;
                Assert.Equal(351, low.Seconds);
                low.Tracks.Remove(sound);
                Assert.Equal(166, low.Seconds);
                transaction.Commit();
            }
            using (label.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(166, AlbumCollection.Single().Seconds);
        }

        /// <summary>The removal of an artist calls OnDeleting for each of its albums: one note each, written by the same transaction.</summary>
        [Fact]
        public void ArchiveEveryRemovedAlbum()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-deleting", StoreAccessMode.ReadWrite);
            using var label = new LabelRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                bowie.Albums.New().Title = "Low";
                bowie.Albums.New().Title = "Heroes";
                transaction.Commit();
            }
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ArtistCollection.Single().Remove();
                transaction.Commit();
            }
            using (label.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(0, AlbumCollection.Count());
                Assert.Equal(["Heroes, by David Bowie, removed", "Low, by David Bowie, removed"],
                    NoteCollection.Select(static n => n.Text).Order().ToList());
            }
        }
    }
}
