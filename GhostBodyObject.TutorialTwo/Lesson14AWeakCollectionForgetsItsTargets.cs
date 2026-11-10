using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Audience
{
    using static AudienceRepositoryCollections;

    // The ninth model: the listeners, their favorite albums, the album they play now; and the reverse properties of Lesson 15,
    // the fans of an album and the albums of a genre.

    [Entity(1, 1, 1), Root, Enumerable]
    internal interface MGenre
    {
        [Property(1)]
        VName Name { get; set; }

        // The albums that designate this genre, by their strong reference Genre (Lesson 15).
        [ReverseStrongReference(nameof(MAlbum.Genre))]
        IEntityCollection<MAlbum> Albums { get; }
    }

    [Entity(2, 1, 2), Root, Removable, Enumerable]
    internal interface MAlbum
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2), Strong]
        Option<MGenre> Genre { get; set; }

        // The listeners whose favorites hold this album (Lesson 15).
        [ReverseWeakReference(nameof(MListener.Favorites))]
        IEntityCollection<MListener> Fans { get; }
    }

    [Entity(3, 1, 3), Root, Removable, Enumerable]
    internal interface MListener
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2), Weak]
        Option<MAlbum> NowPlaying { get; set; }

        // A collection of weak references: it holds albums, and retains none.
        [Property(3), Weak]
        IEntityCollection<MAlbum> Favorites { get; }
    }

    [GhostRepository(6), IncludeEntityModel(nameof(MGenre)), IncludeEntityModel(nameof(MAlbum)), IncludeEntityModel(nameof(MListener))]
    internal interface MAudienceRepository
    {
    }

    /// <summary>
    /// Lesson 14: a collection of weak references forgets its targets.
    /// <para>
    /// The favorites of a listener are a <c>[Weak] IEntityCollection&lt;MAlbum&gt;</c>: a collection of references that retains
    /// nothing. An album in the favorites of a million listeners can still be removed; its removal takes it out of every
    /// favorites, at that instruction, as it sets every weak reference to it to none (Lesson 3). The count of each collection
    /// stays exact: a collection never holds a removed entity.
    /// </para>
    /// </summary>
    public sealed class Lesson14AWeakCollectionForgetsItsTargets
    {
        /// <summary>An album removed: out of every favorites, and no longer played; in the transaction and after the commit.</summary>
        [Fact]
        public void TakeARemovedAlbumOutOfEveryFavorites()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-favorites", StoreAccessMode.ReadWrite);
            using var audience = new AudienceRepository(store);
            using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var low = new Album { Title = "Low" };
                var heroes = new Album { Title = "Heroes" };
                for (int i = 0; i < 100; i++)
                {
                    var listener = new Listener { Name = $"Listener {i}", NowPlaying = low };
                    listener.Favorites.Add(low);
                    listener.Favorites.Add(heroes);
                }
                transaction.Commit();
            }
            using (var transaction = audience.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                AlbumCollection.Single(static a => a.Title == "Low").Remove();
                Assert.All(ListenerCollection, static listener =>
                {
                    Assert.Equal(["Heroes"], listener.Favorites.Select(static a => a.Title).ToList());
                    Assert.False(listener.NowPlaying.Any());
                });
                transaction.Commit();
            }
            using (audience.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(100, ListenerCollection.Count(static l => l.Favorites.Count() == 1));
        }
    }
}
