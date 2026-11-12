using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Lineups
{
    using static FestivalRepositoryCollections;

    // The eighth model: a festival books artists, and shows posters, which the editions of next years may show again.

    [Entity(1, 1, 1), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 3), Root, Removable]
    internal interface MFestival
    {
        [Property(1)]
        VName Name { get; set; }

        // A collection of strong references to Roots: each booked artist is retained.
        [Property(2), Strong]
        IEntityCollection<MArtist> Lineup { get; }

        // A collection of strong references to shared objects: each poster lives while a festival shows it.
        [Property(3), Strong]
        IEntityCollection<MPoster> Posters { get; }
    }

    [Entity(3, 1, 1), Enumerable]
    internal interface MPoster
    {
        [Property(1)]
        VName Design { get; set; }
    }

    [GhostRepository(5), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MFestival)), IncludeEntityModel(nameof(MPoster))]
    internal interface MFestivalRepository
    {
    }

    /// <summary>
    /// Lesson 13: a collection of strong references.
    /// <para>
    /// <c>[Strong] IEntityCollection&lt;T&gt;</c> is a collection of references, not of parts: it designates entities that it
    /// does not own. Each of them is retained, as by a strong reference of Lesson 2: a booked artist cannot be removed. Its
    /// <c>Add</c> retains, its <c>Remove</c> releases. Of shared objects, it has a <c>New</c>, and each object lives while a
    /// collection, or a reference, retains it.
    /// </para>
    /// <para>
    /// A collection of references is a set, as the collections of parts (Lesson 8): an entity is in it once, and any number of
    /// them fit.
    /// </para>
    /// </summary>
    public sealed class Lesson13ACollectionOfStrongReferences
    {
        /// <summary>A booked artist cannot be removed; taken out of the lineup, it can.</summary>
        [Fact]
        public void RetainEveryArtistOfALineup()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-lineup", StoreAccessMode.ReadWrite);
            using var festivals = new FestivalRepository(store);
            using var transaction = festivals.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var eno = new Artist { Name = "Brian Eno" };
            var montreux = new Festival { Name = "Montreux" };
            montreux.Lineup.Add(bowie);
            montreux.Lineup.Add(eno);

            Assert.Throws<InvalidOperationException>(bowie.Remove);
            montreux.Lineup.Remove(bowie);
            bowie.Remove();

            // The removal of the festival releases its whole lineup.
            montreux.Remove();
            eno.Remove();
            Assert.Equal(0, ArtistCollection.Count());
            transaction.Commit();
        }

        /// <summary>A poster born in a festival, shown again by the next edition, removed with the last one that shows it.</summary>
        [Fact]
        public void SharePostersBetweenEditions()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-posters", StoreAccessMode.ReadWrite);
            using var festivals = new FestivalRepository(store);
            using var transaction = festivals.OpenTransaction(StoreAccessMode.ReadWrite);
            var first = new Festival { Name = "Montreux 1977" };
            var poster = first.Posters.New();
            poster.Design = "Blue lake";
            var second = new Festival { Name = "Montreux 1978" };
            second.Posters.Add(poster);

            first.Remove();
            Assert.Equal(1, PosterCollection.Count());
            second.Posters.Remove(poster);
            Assert.Equal(0, PosterCollection.Count());
            transaction.Commit();
        }
    }
}
