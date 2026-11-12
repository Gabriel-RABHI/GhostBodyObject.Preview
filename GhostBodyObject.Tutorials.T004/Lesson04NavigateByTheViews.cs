using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T004.Reviews
{
    using static ReviewRepositoryCollections;

    /// <summary>
    /// Lesson 4: navigate by the Views.
    /// <para>
    /// The edges are navigated by the Views: the View of an end has the same members as its Body, which give Views, without creating
    /// any Body. Counting the ratings of a movie, summing their stars, listing its critics, finding the rating of a pair, ranking
    /// every movie: Views, read where they are.
    /// </para>
    /// <para>
    /// The Bodies are for the few edges to write: a search by the Views finds them, <c>ToBody()</c> gives their Body. A birth takes
    /// its other end by its View as well: <c>cid.NewRating(alienView, 4)</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson04NavigateByTheViews
    {
        private static ReviewRepository NewSite(TransactionalMemoryStore store)
        {
            var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var (ada, bob, cid) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" }, new Critic { Name = "Cid" });
            var (dune, alien, heat) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" }, new Movie { Title = "Heat" });
            ada.NewRating(dune, 5);
            bob.NewRating(dune, 4);
            cid.NewRating(dune, 0);
            ada.NewRating(alien, 4);
            bob.NewRating(heat, 5);
            transaction.Commit();
            return reviews;
        }

        /// <summary>The ratings of a movie, by its View: their count, their stars, their critics, the rating of a pair.</summary>
        [Fact]
        public void ReadTheRatingsOfAMovieByItsView()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-read", StoreAccessMode.ReadWrite);
            using var reviews = NewSite(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadOnly);
            var dune = MovieViewCollection.Single(static m => m.Title == "Dune");
            Assert.Equal(3, dune.RatingEdges.Count());
            Assert.Equal(9, dune.RatingEdges.Sum(static r => r.Stars.Value));
            Assert.Equal(["Ada", "Bob", "Cid"], dune.RatingEdges.Sources.Select(static c => c.Name).Order().ToList());

            // The rating of a pair, from either end, its other end given by its View.
            var bob = CriticViewCollection.Single(static c => c.Name == "Bob");
            Assert.Equal(4, dune.RatingEdges.From(bob).Get().Stars);
            Assert.Equal(4, bob.RatingEdges.To(dune).Get().Stars);
        }

        /// <summary>Every movie ranked by its average rating: a pass over the Views, which creates no Body.</summary>
        [Fact]
        public void RankTheMovies()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rank", StoreAccessMode.ReadWrite);
            using var reviews = NewSite(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadOnly);
            var ranking = MovieViewCollection
                .OrderByDescending(static m => (double)m.RatingEdges.Sum(static r => r.Stars.Value) / m.RatingEdges.Count())
                .Select(static m => m.Title).ToList();
            Assert.Equal(["Heat", "Alien", "Dune"], ranking);
        }

        /// <summary>The few ratings to remove, found by the Views, removed by their Bodies; a new rating, its movie given by its View.</summary>
        [Fact]
        public void WriteTheFewByTheirBody()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-write", StoreAccessMode.ReadWrite);
            using var reviews = NewSite(store);
            using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The moderation removes the ratings without a star.
                var dune = MovieViewCollection.Single(static m => m.Title == "Dune");
                foreach (var empty in dune.RatingEdges.Where(1, static (rating, least) => rating.Stars < least).ToList())
                    empty.ToBody().Remove();

                // Cid rates Alien, found by its View.
                var alien = MovieViewCollection.Single(static m => m.Title == "Alien");
                CriticCollection.Single(static c => c.Name == "Cid").NewRating(alien, 4);
                transaction.Commit();
            }
            using (reviews.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(2, MovieViewCollection.Single(static m => m.Title == "Dune").RatingEdges.Count());
                Assert.Equal(8, MovieViewCollection.Single(static m => m.Title == "Alien").RatingEdges.Sum(static r => r.Stars.Value));
            }
        }
    }
}
