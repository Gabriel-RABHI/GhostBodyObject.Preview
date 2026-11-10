using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFour.Reviews
{
    using static ReviewRepositoryCollections;

    /// <summary>
    /// Lesson 3: an edge lives with its ends.
    /// <para>
    /// An edge never survives its ends: removing a movie removes its ratings and its screenings, at that instruction; removing a
    /// critic, its own. The other end stays, with its other edges. The reverse never happens: an edge never retains its ends, and its
    /// removal leaves them as they are.
    /// </para>
    /// <para>
    /// The code removes an edge when its type is <c>[Removable]</c>: <c>rating.Remove()</c>, or by the pair,
    /// <c>ada.RatingEdges.Remove(dune)</c>. A screening is not <c>[Removable]</c>: a fact of the history, it has no <c>Remove</c>,
    /// and leaves with its critic or its movie only.
    /// </para>
    /// </summary>
    public sealed class Lesson03AnEdgeLivesWithItsEnds
    {
        /// <summary>A rating removed by itself, or by its pair: its critic and its movie stay.</summary>
        [Fact]
        public void RemoveAnEdge()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-remove", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Critic { Name = "Ada" };
            var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
            var rating = ada.NewRating(dune, 5);
            ada.NewRating(alien, 1);

            rating.Remove();
            Assert.True(ada.RatingEdges.Remove(alien));
            Assert.False(ada.RatingEdges.Remove(alien));

            // The ends stay.
            Assert.Equal(0, ada.RatingEdges.Count());
            Assert.Equal((1, 2), (CriticCollection.Count(), MovieCollection.Count()));
            transaction.Commit();
        }

        /// <summary>A movie removed takes its ratings and its screenings with it; the critics stay, with their other edges.</summary>
        [Fact]
        public void RemoveTheEdgesWithTheirEnd()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-cascade", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var (ada, bob) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" });
                var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
                ada.NewRating(dune, 5);
                bob.NewRating(dune, 4);
                ada.NewRating(alien, 3);
                ada.NewScreening(dune, new DateTime(2026, 10, 1));
                transaction.Commit();
            }
            using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                MovieCollection.Single(static m => m.Title == "Dune").Remove();
                Assert.Equal(["Alien"], RatingCollection.Select(static r => r.Movie.Title).ToList());
                Assert.Equal(0, ScreeningCollection.Count());
                Assert.Equal(0, CriticCollection.Single(static c => c.Name == "Bob").RatingEdges.Count());
                transaction.Commit();
            }
        }

        /// <summary>The screenings, never removed by the code, leave with their critic.</summary>
        [Fact]
        public void KeepTheHistoryUntilAnEndLeaves()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-history", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Critic { Name = "Ada" };
            var dune = new Movie { Title = "Dune" };
            ada.NewScreening(dune, new DateTime(2026, 9, 1));
            ada.NewScreening(dune, new DateTime(2026, 10, 1));

            // Screening has no Remove: screening.Remove() and ada.ScreeningEdges.Remove(dune) do not compile.
            ada.Remove();
            Assert.Equal(0, ScreeningCollection.Count());
            Assert.Equal(0, dune.ScreeningEdges.Count());
            transaction.Commit();
        }
    }
}
