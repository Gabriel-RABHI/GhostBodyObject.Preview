using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Capabilities
{
    using static CapabilitiesRepositoryCollections;

    // The model of the components: an artist gains capabilities, a tour with its concerts, a badge, a retirement, without knowing
    // them; the reviews are a capability of the artists and of the albums; a system joins the artists on tour and their badge.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The city of a concert.</summary>
    [ValueObject<GhostString>]
    internal interface VCity { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    /// <summary>The reason of a retirement.</summary>
    [ValueObject<GhostString>]
    internal interface VReason { }

    /// <summary>The stars of a review, from 0 to 5.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VRating { }

    internal partial struct Rating
    {
        private static bool IsValid(int stars) => stars is >= 0 and <= 5;
    }

    [Entity(1, 1, 1), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 1), Root, Removable, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }
    }

    // A capability of an artist: a tour, and its concerts, parts of the component.
    [Entity(3, 1, 1), ComponentFor(nameof(MArtist)), Enumerable]
    internal interface CTour
    {
        [Property(1)]
        IEntityCollection<MConcert> Concerts { get; }
    }

    [Entity(4, 1, 2), Enumerable]
    internal interface MConcert
    {
        [Property(1)]
        VCity City { get; set; }

        [Property(2)]
        VInstant Date { get; set; }

        [OwnedFrom]
        CTour Tour { get; }
    }

    [Entity(5, 1, 1), ComponentFor(nameof(MArtist))]
    internal interface CVerified
    {
        [Property(1)]
        VInstant Since { get; set; }
    }

    [Entity(6, 1, 1), ComponentFor(nameof(MArtist))]
    internal interface CRetired
    {
        [Property(1)]
        VReason Reason { get; set; }
    }

    // A capability of two entity types: what the listeners review.
    [Entity(7, 1, 1), ComponentFor(nameof(MArtist)), ComponentFor(nameof(MAlbum)), Enumerable]
    internal interface CReviews
    {
        [Property(1)]
        IEntityCollection<MReview> Items { get; }
    }

    [Entity(8, 1, 2)]
    internal interface MReview
    {
        [Property(1)]
        VFreeText Text { get; set; }

        [Property(2)]
        VRating Stars { get; set; }

        [OwnedFrom]
        CReviews Reviews { get; }
    }

    // The artists on tour, maybe verified, and not retired.
    [SystemFor(nameof(MArtist)), ExcludeComponent(nameof(CRetired)), Enumerable]
    internal interface SOnTour
    {
        MArtist Artist { get; }

        CTour Tour { get; }

        Option<CVerified> Verified { get; }
    }

    [GhostRepository(8), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(CTour)), IncludeEntityModel(nameof(MConcert)), IncludeEntityModel(nameof(CVerified)), IncludeEntityModel(nameof(CRetired)), IncludeEntityModel(nameof(CReviews)), IncludeEntityModel(nameof(MReview)), IncludeEntityModel(nameof(SOnTour))]
    internal interface MCapabilitiesRepository
    {
    }

    /// <summary>
    /// Lesson 21: capabilities, the components and the systems.
    /// <para>
    /// An artist may tour, be verified, retire. These are not values of every artist: they are capabilities that some artists have,
    /// each with its own values, and that come and go. A component declares one: an <c>[Entity]</c> with
    /// <c>[ComponentFor(nameof(MArtist))]</c>, its interface starting with <c>C</c>. The artist does not know its components: the
    /// slot of a component on its entity, <c>artist.Tour</c>, is written with the component, and a new capability changes nothing of
    /// the artist. An entity has at most one component of each type; the component reads its entity, <c>tour.Artist</c>; it is
    /// removed with its entity, never the reverse.
    /// </para>
    /// <para>
    /// A system joins an entity and its components: an interface <c>[SystemFor(nameof(MArtist))]</c>, its interface starting with
    /// <c>S</c>, whose properties are the entity, its required components, <c>T</c>, and its optional ones, <c>Option&lt;T&gt;</c>;
    /// <c>[ExcludeComponent]</c> leaves out the entities that have a component. Its members are Views, and GBO reads only the
    /// entities that may belong to it: those of its rarest required component.
    /// </para>
    /// <para>
    /// Tutorial Three is about the components and the systems: their links, the capabilities shared by several types, your interfaces
    /// over them, the roles that replace the subclasses, and their concurrency.
    /// </para>
    /// </summary>
    public sealed class Lesson21CapabilitiesAndSystems
    {
        /// <summary>A capability comes and goes: the slot of the component on its entity, at most one of a type, and its parts.</summary>
        [Fact]
        public void GiveAnArtistACapability()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-tour", StoreAccessMode.ReadWrite);
            using var label = new CapabilitiesRepository(store);
            using var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "Bowie" };
            Assert.False(bowie.Tour.Any());

            // New attaches the component to the artist; the component reads its artist.
            var tour = bowie.Tour.New();
            tour.Concerts.New().City = "Berlin";
            tour.Concerts.New().City = "London";
            Assert.Same(bowie, tour.Artist);
            Assert.Same(tour, bowie.Tour.Get());

            // One component of a type per entity: a second one is refused, and nothing changes.
            Assert.Throws<InvalidOperationException>(() => bowie.Tour.New());
            Assert.Equal(2, bowie.Tour.Get().Concerts.Count());

            // The tour ends: the component and its concerts are removed, the artist stays.
            Assert.True(bowie.Tour.Remove());
            Assert.False(bowie.Tour.Any());
            Assert.Empty(ConcertCollection);
            Assert.Single(ArtistCollection);
            transaction.Commit();
        }

        /// <summary>An artist removed takes its capabilities with it, and their parts; removing a capability never removes its artist.</summary>
        [Fact]
        public void RemoveTheCapabilitiesWithTheirArtist()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-removal", StoreAccessMode.ReadWrite);
            using var label = new CapabilitiesRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "Bowie" };
                bowie.Tour.New().Concerts.New().City = "Berlin";
                bowie.Verified.New().Since = new DateTime(1972, 6, 6);
                bowie.Reviews.New().Items.New().Text = "Timeless";
                transaction.Commit();
            }
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ArtistCollection.Single().Remove();
                Assert.Empty(TourCollection);
                Assert.Empty(ConcertCollection);
                Assert.Empty(ReviewsCollection);
                transaction.Commit();
            }
        }

        /// <summary>A capability of two entity types: the reviews of an artist and of an album, one collection for all of them.</summary>
        [Fact]
        public void ShareACapabilityBetweenTwoTypes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-reviews", StoreAccessMode.ReadWrite);
            using var label = new CapabilitiesRepository(store);
            using var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "Bowie" };
            var low = new Album { Title = "Low" };
            bowie.Reviews.New().Items.New().Stars = 5;
            var ofLow = low.Reviews.New();
            ofLow.Items.New().Stars = 4;

            // Of several entity types, the component reads its entity as an Option of each type.
            Assert.Same(low, ofLow.Album.Get());
            Assert.False(ofLow.Artist.Any());

            // "Everything that can be reviewed" is the collection of the component, whatever the types of their entities.
            Assert.Equal(2, ReviewsCollection.Count());
            Assert.Equal(9, ReviewsCollection.Sum(static r => r.Items.Sum(static i => i.Stars.Value)));
            transaction.Commit();
        }

        /// <summary>A system: the artists on tour, maybe verified, not retired; the system of one artist, by Of.</summary>
        [Fact]
        public void JoinTheArtistsAndTheirCapabilities()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-21-system", StoreAccessMode.ReadWrite);
            using var label = new CapabilitiesRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "Bowie" };
                bowie.Tour.New().Concerts.New().City = "Berlin";
                bowie.Verified.New();
                new Artist { Name = "Eno" }.Tour.New();
                var reed = new Artist { Name = "Reed" };
                reed.Tour.New();
                reed.Retired.New().Reason = "Rest";
                new Artist { Name = "Iggy" };
                transaction.Commit();
            }
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The members of a system are Views: the enumeration creates no Body.
                Assert.Equal(["Bowie", "Eno"], OnTourSystemCollection.Select(static s => s.Artist.Name).Order().ToList());
                Assert.Equal(["Bowie"], OnTourSystemCollection.Where(static s => s.Verified.Any()).Select(static s => s.Artist.Name).ToList());

                // The capabilities that the transaction gives or takes are seen at the next instruction.
                var iggy = ArtistCollection.Single(static a => a.Name == "Iggy");
                iggy.Tour.New();
                ArtistCollection.Single(static a => a.Name == "Eno").Retired.New();
                Assert.Equal(["Bowie", "Iggy"], OnTourSystemCollection.Select(static s => s.Artist.Name).Order().ToList());

                // The system of one artist, when it has the capabilities of the system.
                Assert.True(OnTourSystem.Of(iggy).Any());
                Assert.False(OnTourSystem.Of(ArtistCollection.Single(static a => a.Name == "Reed")).Any());
                transaction.Commit();
            }
        }
    }
}
