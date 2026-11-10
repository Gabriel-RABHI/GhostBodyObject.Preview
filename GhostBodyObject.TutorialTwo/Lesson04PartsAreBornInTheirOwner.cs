using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Compositions
{
    using static CatalogRepositoryCollections;

    // The second model: an artist owns its albums, an album owns its tracks, its release and its booklet. A link without
    // [Strong] nor [Weak] is a composition: the Owner holds its parts, an aggregate, as an order holds its lines.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The number of a release in the catalog of its label, "PL 12030".</summary>
    [ValueObject<GhostString>]
    internal interface VCatalogNumber { }

    /// <summary>A count of pages.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VPageCount { }

    internal partial struct PageCount
    {
        private static bool IsValid(int pages) => pages >= 0;
    }

    /// <summary>An artist, a Root: the Owner of its albums.</summary>
    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }

        // A collection of parts: IEntityCollection<T>, without Setter. Its parts are born in it.
        [Property(2)]
        IEntityCollection<MAlbum> Albums { get; }
    }

    /// <summary>An album, a part: it is not Root, it lives in its artist.</summary>
    [Entity(2, 1, 6), Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VYear Year { get; set; }

        // A required composition, a T: the release of the album is born with it, and never leaves it.
        [Property(3)]
        MRelease Release { get; }

        [Property(4)]
        IEntityCollection<MTrack> Tracks { get; }

        // An optional composition, an Option<T>: a slot, that holds a booklet or none.
        [Property(5)]
        Option<MBooklet> Booklet { get; }

        // A second collection of the same parts: the tracks put forward (Lesson 6).
        [Property(6)]
        IEntityCollection<MTrack> Highlights { get; }

        // [OwnedFrom]: the Owner of the part, read, never set. It takes no [Property(n)]: the composition that holds the
        // part gives it.
        [OwnedFrom]
        MArtist Artist { get; }
    }

    [Entity(3, 1, 2), Enumerable]
    internal interface MRelease
    {
        [Property(1)]
        VName Label { get; set; }

        [Property(2)]
        VCatalogNumber CatalogNumber { get; set; }
    }

    [Entity(4, 1, 2), Enumerable]
    internal interface MTrack
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VSeconds Seconds { get; set; }

        [OwnedFrom]
        MAlbum Album { get; }
    }

    [Entity(5, 1, 1), Enumerable]
    internal interface MBooklet
    {
        [Property(1)]
        VPageCount Pages { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MRelease)), IncludeEntityModel(nameof(MTrack)), IncludeEntityModel(nameof(MBooklet))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 4: parts are born in their Owner.
    /// <para>
    /// A part is an entity that is not Root and that a composition of its Owner holds: an album in its artist, a track in its
    /// album. It has no public constructor: born out of any Owner, nothing would hold it, and GBO would remove it at once. So
    /// <c>new Album()</c> does not compile; a part is born in its composition, by <c>New</c>: <c>artist.Albums.New()</c>,
    /// <c>album.Booklet.New()</c>. A required part, a <c>T</c>, is born with its Owner, by its constructor.
    /// </para>
    /// <para>
    /// A part knows its Owner: <c>[OwnedFrom]</c> reads it. The parts and their Owner are one aggregate: they are created,
    /// changed, removed and committed together.
    /// </para>
    /// </summary>
    public sealed class Lesson04PartsAreBornInTheirOwner
    {
        /// <summary>An album born in its artist, with its release; its tracks born in it; each part knows its Owner.</summary>
        [Fact]
        public void CreateAnAggregate()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-aggregate", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                var low = bowie.Albums.New();
                low.Title = "Low";
                low.Year = 1977;

                // The release, a required part, was born with the album.
                low.Release.Label = "RCA";
                low.Release.CatalogNumber = "PL 12030";

                var speed = low.Tracks.New();
                speed.Title = "Speed of Life";
                speed.Seconds = 166;
                var sound = low.Tracks.New();
                sound.Title = "Sound and Vision";
                sound.Seconds = 183;

                // Each part reads its Owner.
                Assert.Same(low, speed.Album);
                Assert.Same(bowie, speed.Album.Artist);
                Assert.Equal((1, 2), (bowie.Albums.Count(), low.Tracks.Count()));
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var low = ArtistCollection.Single().Albums.Single();
                Assert.Equal("Low", low.Title);
                Assert.Equal("RCA", low.Release.Label);
                Assert.Equal(349, low.Tracks.Sum(static t => t.Seconds));
            }
        }

        /// <summary>A slot holds one part, or none: <c>New</c> fills it, and refuses a slot already full.</summary>
        [Fact]
        public void FillTheSlotOfAnOptionalPart()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-slot", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var low = new Artist { Name = "David Bowie" }.Albums.New();

            Assert.False(low.Booklet.Any());
            low.Booklet.New().Pages = 12;
            Assert.Equal(12, low.Booklet.Get().Pages);

            // The slot holds a booklet already: Remove() it first (Lesson 5).
            Assert.Throws<InvalidOperationException>(() => low.Booklet.New());
            transaction.Commit();
        }

        /// <summary>The Views of an aggregate read its parts, without creating any Body.</summary>
        [Fact]
        public void ReadAnAggregateWithViews()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-views", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var low = new Artist { Name = "David Bowie" }.Albums.New();
                low.Title = "Low";
                for (int i = 1; i <= 11; i++)
                    low.Tracks.New().Seconds = 180;
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var artist = ArtistViewCollection.Single();
                var low = artist.Albums.Single();
                Assert.Equal("Low", low.Title);
                Assert.Equal((11, 1980), (low.Tracks.Count(), low.Tracks.Sum(static t => t.Seconds)));
                Assert.Equal("David Bowie", low.Artist.Name);
            }
        }
    }
}
