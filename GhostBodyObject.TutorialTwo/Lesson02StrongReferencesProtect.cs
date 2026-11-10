using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.References
{
    using static CatalogRepositoryCollections;

    /// <summary>
    /// Lesson 2: a strong reference protects its target.
    /// <para>
    /// An album designates its artist by a <c>[Strong]</c> reference: while it does, the artist cannot be removed. Its
    /// <c>Remove()</c> throws at the call, as <c>ON DELETE RESTRICT</c> does in SQL, and changes nothing: the transaction goes
    /// on, and commits. No reference ever designates an entity that does not exist any more.
    /// </para>
    /// <para>
    /// GBO counts the strong references to each entity: the count is exact at each instruction, never computed later. A
    /// Root that is not Removable, such as a genre, keeps no count: nothing removes it, so nothing has to protect it, and a
    /// million albums of one genre cost it nothing.
    /// </para>
    /// </summary>
    public sealed class Lesson02StrongReferencesProtect
    {
        /// <summary>The removal of an artist that an album designates is refused, and nothing changes.</summary>
        [Fact]
        public void RefuseTheRemovalOfAnArtistThatAnAlbumDesignates()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-refuse", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = new Album(bowie) { Title = "Low" };

            var refused = Assert.Throws<InvalidOperationException>(bowie.Remove);
            Assert.Contains("cannot be removed", refused.Message);

            // The refusal comes before any write: the artist and its album are there, and the transaction is alive.
            Assert.Same(bowie, low.Artist);
            Assert.Null(transaction.CondemnedBy);
            transaction.Commit();
        }

        /// <summary>The artist can be removed once nothing designates it: another artist set, or the album removed first.</summary>
        [Fact]
        public void RemoveTheArtistOnceNothingDesignatesIt()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-release", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                new Album(bowie) { Title = "Low" };
                new Album(bowie) { Title = "Heroes" };
                transaction.Commit();
            }
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = ArtistCollection.Single();
                var low = AlbumCollection.Single(static a => a.Title == "Low");
                var heroes = AlbumCollection.Single(static a => a.Title == "Heroes");

                // Setting another artist releases the reference to Bowie: one album still designates Bowie.
                low.Artist = new Artist { Name = "Brian Eno" };
                Assert.Throws<InvalidOperationException>(bowie.Remove);

                // The removal of an album releases its references: nothing designates Bowie any more.
                heroes.Remove();
                bowie.Remove();
                Assert.Equal(["Brian Eno"], ArtistCollection.Select(static a => a.Name).ToList());
                transaction.Commit();
            }
        }

        /// <summary>The order of the instructions matters: the album first, then its artist; never the reverse.</summary>
        [Fact]
        public void RemoveInTheOrderOfTheReferences()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-order", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = new Album(bowie) { Title = "Low" };

            // GBO checks each instruction against the state of the transaction at that instruction, not against the state at the
            // commit: the artist is refused while the album is there.
            Assert.Throws<InvalidOperationException>(bowie.Remove);
            low.Remove();
            bowie.Remove();
            Assert.Equal(0, ArtistCollection.Count());
            transaction.Commit();
        }

        /// <summary>A genre is a Root that is not Removable: it has no <c>Remove()</c>, and nothing ever removes it.</summary>
        [Fact]
        public void NeverRemoveAGenre()
        {
            // The generator writes Remove() for a [Removable] Root only.
            Assert.NotNull(typeof(Artist).GetMethod("Remove"));
            Assert.Null(typeof(Genre).GetMethod("Remove"));

            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-genre", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var rock = new Genre { Name = "Rock" };
            var bowie = new Artist { Name = "David Bowie" };
            for (int year = 1970; year < 1980; year++)
                new Album(bowie) { Title = $"Album {year}", Year = year, Genre = rock };
            Assert.Equal(10, AlbumCollection.Count(static a => a.Genre.Any()));
            transaction.Commit();
        }
    }
}
