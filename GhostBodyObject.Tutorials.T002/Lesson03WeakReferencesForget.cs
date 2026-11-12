using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T002.References
{
    using static CatalogRepositoryCollections;

    /// <summary>
    /// Lesson 3: a weak reference forgets its target.
    /// <para>
    /// An album designates the previous album of its artist by a <c>[Weak]</c> reference: it does not retain it. When the
    /// previous album is removed, the transaction that removes it sets every weak reference to it to none, at that very
    /// instruction, as <c>ON DELETE SET NULL</c> does in SQL. The referrers are rewritten, and the commit writes them: a weak
    /// reference never designates an entity that does not exist, in the transaction or in the Store.
    /// </para>
    /// <para>
    /// A weak reference is always an <c>Option&lt;T&gt;</c>, since it may become empty: <c>[Weak]</c> on a <c>T</c> stops the
    /// build, with the error GBO017.
    /// </para>
    /// </summary>
    public sealed class Lesson03WeakReferencesForget
    {
        /// <summary>The removal of an album sets the weak references to it to none, in the transaction and in the Store.</summary>
        [Fact]
        public void SetAWeakReferenceToNoneWhenItsTargetIsRemoved()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-forget", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bowie = new Artist { Name = "David Bowie" };
                var low = new Album(bowie) { Title = "Low", Year = 1977 };
                new Album(bowie) { Title = "Heroes", Year = 1977, Previous = low };
                transaction.Commit();
            }
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var heroes = AlbumCollection.Single(static a => a.Title == "Heroes");
                Assert.Equal("Low", heroes.Previous.Get().Title);

                // A weak reference does not protect its target: the removal is allowed, and forgets the reference at once.
                heroes.Previous.Get().Remove();
                Assert.False(heroes.Previous.Any());
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var heroes = AlbumCollection.Single();
                Assert.False(heroes.Previous.Any());
            }
        }

        /// <summary>A strong reference and a weak one to the same album: the strong one refuses, the weak one would forget.</summary>
        [Fact]
        public void CompareAStrongAndAWeakReference()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-compare", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            var bowie = new Artist { Name = "David Bowie" };
            var low = new Album(bowie) { Title = "Low" };
            var heroes = new Album(bowie) { Title = "Heroes", Previous = low };

            // The album retains its artist: refused. The previous album does not retain anything: removed, and forgotten.
            Assert.Throws<InvalidOperationException>(bowie.Remove);
            low.Remove();
            Assert.Equal(Option.None, heroes.Previous);
            transaction.Commit();
        }
    }
}
