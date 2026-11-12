using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.Audience
{
    using static AudienceRepositoryCollections;

    /// <summary>
    /// Lesson 15: who points to me? The reverse properties.
    /// <para>
    /// A link reads in one direction: from the listener to its favorite albums. The other direction, the listeners of an
    /// album, is a reverse property: <c>[ReverseWeakReference(nameof(MListener.Favorites))] IEntityCollection&lt;MListener&gt; Fans</c>,
    /// without <c>[Property(n)]</c> nor Setter. GBO keeps the set of the referrers of each album up to date at each
    /// instruction: a Setter, an <c>Add</c>, a <c>Remove</c>, the removal of a listener. Reading it costs no search: no query
    /// over all the listeners, the set is there.
    /// </para>
    /// <para>
    /// The referrers of a weak reference are kept anyway: the removal of the target needs them, to forget the references. The
    /// referrers of a strong reference are kept only when a reverse property declares them, <c>[ReverseStrongReference]</c>:
    /// keeping them costs a write at each new reference. A declared reverse property keeps every referrer, even of a Root that
    /// is not Removable, as a genre: the list of the entities that point to it. The generator checks its path: a field of the
    /// other type, of the same nature, that designates this one (error GBO028 otherwise).
    /// </para>
    /// </summary>
    public sealed class Lesson15WhoPointsToMe
    {
        /// <summary>The fans of an album: the listeners whose favorites hold it, kept at each instruction.</summary>
        [Fact]
        public void ReadTheFansOfAnAlbum()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-fans", StoreAccessMode.ReadWrite);
            using var audience = new AudienceRepository(store);
            using var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite);
            var low = new Album { Title = "Low" };
            var (ada, bob, eve) = (new Listener { Name = "Ada" }, new Listener { Name = "Bob" }, new Listener { Name = "Eve" });
            ada.Favorites.Add(low);
            bob.Favorites.Add(low);
            Assert.Equal([ada, bob], low.Fans.ToList());

            eve.Favorites.Add(low);
            bob.Favorites.Remove(low);
            ada.Remove();
            Assert.Same(eve, low.Fans.Single());
            Assert.True(low.Fans.Contains(eve));
            transaction.Commit();
        }

        /// <summary>The albums of a genre, a Root that is not Removable: kept because the reverse property declares them.</summary>
        [Fact]
        public void ReadTheAlbumsOfAGenre()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-genre", StoreAccessMode.ReadWrite);
            using var audience = new AudienceRepository(store);
            using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var (rock, jazz) = (new Genre { Name = "Rock" }, new Genre { Name = "Jazz" });
                new Album { Title = "Low", Genre = rock };
                new Album { Title = "Heroes", Genre = rock };
                var kind = new Album { Title = "Kind of Blue", Genre = rock };

                // A Setter moves the album from the set of one genre to the set of the other.
                kind.Genre = jazz;
                Assert.Equal((2, 1), (rock.Albums.Count(), jazz.Albums.Count()));
                transaction.Commit();
            }
            using (audience.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var rock = GenreCollection.Single(static g => g.Name == "Rock");
                Assert.Equal(["Heroes", "Low"], rock.Albums.Select(static a => a.Title).Order().ToList());

                // The Views read the reverse properties as well, without any Body.
                var view = GenreViewCollection.Single(static g => g.Name == "Jazz");
                Assert.Equal("Kind of Blue", view.Albums.Single().Title);
            }
        }
    }
}
