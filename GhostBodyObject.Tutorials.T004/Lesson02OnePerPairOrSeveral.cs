using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T004.Reviews
{
    using static ReviewRepositoryCollections;

    /// <summary>
    /// Lesson 2: one edge per pair, or several.
    /// <para>
    /// A critic rates a movie once: an edge is one per pair of ends, unless its type says otherwise. A second rating of the same
    /// movie by the same critic is refused, before anything changes, and the transaction goes on. <c>GetOrNew</c> gives the rating
    /// of the pair, or a new one: one instruction rates a movie, whether the critic rated it already or not.
    /// </para>
    /// <para>
    /// A critic may see a movie several times: <c>[Edge(..., Multiple = true)]</c> allows several edges per pair. The screenings of a
    /// pair are then a sequence, <c>ada.ScreeningEdges.To(dune)</c>, and the movies it saw are each listed once.
    /// </para>
    /// </summary>
    public sealed class Lesson02OnePerPairOrSeveral
    {
        /// <summary>One rating per critic and movie: a second one is refused; GetOrNew rates whether a rating exists or not.</summary>
        [Fact]
        public void RateAMovieOnce()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-once", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Critic { Name = "Ada" };
            var dune = new Movie { Title = "Dune" };
            ada.NewRating(dune, 4);

            // The refusal changes nothing: the transaction goes on.
            Assert.Throws<InvalidOperationException>(() => ada.NewRating(dune, 2));
            Assert.Equal(1, RatingCollection.Count());

            // The rating of the pair, or a new one: the same instruction rates a movie the first time, and the next ones.
            Rate(ada, dune, 5);
            Rate(ada, new Movie { Title = "Alien" }, 3);
            Assert.Equal(5, ada.RatingEdges.To(dune).Get().Stars);
            Assert.Equal(2, RatingCollection.Count());
            transaction.Commit();
        }

        /// <summary>Several screenings of one movie by one critic: the pair has a sequence of edges, the movie is listed once.</summary>
        [Fact]
        public void SeeAMovieSeveralTimes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-several", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Critic { Name = "Ada" };
            var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
            ada.NewScreening(dune, new DateTime(2026, 9, 1));
            ada.NewScreening(dune, new DateTime(2026, 10, 1));
            ada.NewScreening(alien, new DateTime(2026, 10, 2));

            Assert.Equal(3, ada.ScreeningEdges.Count());
            Assert.Equal(2, ada.ScreeningEdges.To(dune).Count());
            Assert.Equal(new DateTime(2026, 10, 1), ada.ScreeningEdges.To(dune).Max(static s => s.At));
            Assert.Equal(["Alien", "Dune"], ada.ScreeningEdges.Targets.Select(static m => m.Title).Order().ToList());
            transaction.Commit();
        }

        // Rates the movie: the rating of the pair, or a new one.
        private static void Rate(Critic critic, Movie movie, Stars stars) => critic.RatingEdges.GetOrNew(movie).Stars = stars;
    }
}
