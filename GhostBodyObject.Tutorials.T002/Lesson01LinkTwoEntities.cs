using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has.

    /// <summary>The name of an artist, a genre, a listener, a playlist, a folder, a station, a festival: a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>The title of an album, a track, a broadcast.</summary>
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>A text written by a person: the words of a song, a note, a review.</summary>
    [ValueObject<GhostString>]
    internal interface VFreeText { }

    /// <summary>The year of an album.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VYear { }

    /// <summary>A duration, in seconds: never negative, added and subtracted.</summary>
    [ValueObject<int>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VSeconds { }

    internal partial struct Seconds
    {
        private static bool IsValid(int seconds) => seconds >= 0;
    }
}

namespace Tutorials.T002.References
{
    using static CatalogRepositoryCollections;

    // The first model of the tutorial: a music label, its artists, their albums, and the genres of music.
    //
    // Each lesson, or each group of lessons, declares its own model, in its own namespace, enriched with the concepts of the
    // lesson. The generator imports the collections of each Repository in the whole project: thirteen models give thirteen
    // AlbumCollection. The using static at the top of the namespace chooses those of its own Repository, CatalogRepository
    // here; C# looks in the namespace before the project. An application with one Repository needs no such line.

    /// <summary>A genre of music: a Root that is not Removable. Nothing removes it, ever.</summary>
    [Entity(1, 1, 1), Root]
    internal interface MGenre
    {
        [Property(1)]
        VName Name { get; set; }
    }

    /// <summary>An artist of the label: a Root that the code may remove.</summary>
    [Entity(2, 1, 1), Root, Removable, Enumerable]
    internal interface MArtist
    {
        [Property(1)]
        VName Name { get; set; }
    }

    /// <summary>An album, linked to its artist, to a genre, and to the album that came before it.</summary>
    [Entity(3, 1, 5), Root, Removable, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VYear Year { get; set; }

        // A link is a property whose type is an entity of the model. [Strong]: the album retains its artist (Lesson 2). A T, not
        // an Option<T>: an album always has an artist, given to its constructor.
        [Property(3), Strong]
        MArtist Artist { get; set; }

        // An Option<T>: an album may have no genre.
        [Property(4), Strong]
        Option<MGenre> Genre { get; set; }

        // [Weak]: the album does not retain the previous one (Lesson 3). A weak link is always an Option<T>: its target may
        // disappear.
        [Property(5), Weak]
        Option<MAlbum> Previous { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MGenre)), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 1: link two entities.
    /// <para>
    /// A link is a property whose type is another entity of the model. Reading it gives the target, in the transaction:
    /// there is no join to write, no foreign key to set, no lazy loading to configure. Every link declares its nature:
    /// <c>[Strong]</c> retains its target, <c>[Weak]</c> does not (Lessons 2 and 3); a link without either is a
    /// composition, which owns its target (Lesson 4).
    /// </para>
    /// <para>
    /// The type of the property says its cardinality. A <c>T</c> is required: the generator makes it a parameter of the
    /// constructor, and it is never empty. An <c>Option&lt;T&gt;</c> may be empty: <c>Any()</c>, <c>Get()</c> and
    /// <c>TryGet</c> read it, as in Tutorial T001.
    /// </para>
    /// </summary>
    public sealed class Lesson01LinkTwoEntities
    {
        /// <summary>An album, born with its artist; a genre set later; the links read back after the commit.</summary>
        [Fact]
        public void CreateAnAlbumWithItsArtist()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-link", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };

                // The artist is a T: the constructor of Album requires it. new Album() does not compile.
                var low = new Album(bowie) { Title = "Low", Year = 1977 };

                // Reading the link gives the Body of the artist: the very same object.
                Assert.Same(bowie, low.Artist);

                // The genre is an Option: empty, until it is set.
                Assert.False(low.Genre.Any());
                low.Genre = new Genre { Name = "Art rock" };
                Assert.Equal("Art rock", low.Genre.Get().Name);
                transaction.Commit();
            }

            // In a new transaction, the links lead to the entities of the Store.
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var low = AlbumCollection.Single();
                Assert.Equal("David Bowie", low.Artist.Name);
                Assert.Equal("Art rock", low.Genre.Get().Name);

                // A View follows the links too, without creating any Body (Tutorial T001, Lesson 10).
                var view = AlbumViewCollection.Single();
                Assert.Equal("David Bowie", view.Artist.Name);
            }
        }

        /// <summary>A link designates the entity, not a copy of it: a change of the artist is seen through every album.</summary>
        [Fact]
        public void SeeTheTargetAsItIs()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-target", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = new Album(bowie) { Title = "Low" };
            var heroes = new Album(bowie) { Title = "Heroes" };

            bowie.Name = "Bowie";
            Assert.Equal("Bowie", low.Artist.Name);
            Assert.Equal("Bowie", heroes.Artist.Name);

            // Setting a link changes the target, not the entity it designated.
            heroes.Artist = new Artist { Name = "Brian Eno" };
            Assert.Equal("Bowie", low.Artist.Name);
            Assert.Equal("Brian Eno", heroes.Artist.Name);
            transaction.Commit();
        }

        /// <summary>A required link is never empty: the constructor and the Setter refuse null, before anything is written.</summary>
        [Fact]
        public void RefuseAnEmptyRequiredLink()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-required", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            Assert.Throws<ArgumentNullException>(() => new Album(null!));
            var low = new Album(new Artist { Name = "David Bowie" }) { Title = "Low" };
            Assert.Throws<ArgumentNullException>(() => low.Artist = null!);

            // Nothing was created by the refused constructor: one album, one artist.
            Assert.Equal((1, 1), (AlbumCollection.Count(), ArtistCollection.Count()));
            transaction.Commit();
        }
    }
}
