using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T004
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has.

    /// <summary>The name of a critic, a city, a person, a group, a role, a user: a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>The title of a movie, a certificate.</summary>
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>A text written by a person: a notice, a post.</summary>
    [ValueObject<GhostString>]
    internal interface VFreeText { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }
}

namespace Tutorials.T004.Reviews
{
    using static ReviewRepositoryCollections;

    // The first model of the tutorial: a site of movie reviews. A critic rates a movie: the rating carries its stars and its date,
    // and belongs neither to the critic nor to the movie. A critic sees a movie, once or several times: the history of its
    // screenings.
    //
    // Each group of lessons declares its own model, in its own namespace: the using static at the top of the namespace chooses the
    // collections of its own Repository, as in Tutorials T002 and T003.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The year of a movie.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VYear { }

    /// <summary>The stars of a rating, from 0 to 5. A sum of ratings is no rating: it adds their values.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VStars { }

    internal partial struct Stars
    {
        private static bool IsValid(int stars) => stars is >= 0 and <= 5;
    }

    [Entity(1, 1, 1), Root, Removable, Enumerable]
    internal interface MCritic
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 2), Root, Removable, Enumerable]
    internal interface MMovie
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VYear Year { get; set; }
    }

    // An edge: an [Entity] with [Edge], its interface starting with E. It goes from a critic to a movie, and carries its values.
    /// <summary>The rating of a movie by a critic: one per critic and movie, which the code may remove.</summary>
    [Entity(3, 1, 2), Edge(nameof(MCritic), nameof(MMovie)), Removable, Enumerable]
    internal interface ERating
    {
        [Property(1)]
        VStars Stars { get; set; }

        [Property(2)]
        VInstant At { get; set; }
    }

    /// <summary>A screening of a movie by a critic: several per critic and movie (Lesson 2); a fact of the history, never removed by the code (Lesson 3).</summary>
    [Entity(4, 1, 1), Edge(nameof(MCritic), nameof(MMovie), Multiple = true), Enumerable]
    internal interface EScreening
    {
        [Property(1)]
        VInstant At { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MCritic)), IncludeEntityModel(nameof(MMovie)), IncludeEntityModel(nameof(ERating)), IncludeEntityModel(nameof(EScreening))]
    internal interface MReviewRepository
    {
    }

    /// <summary>
    /// Lesson 1: relate two entities with values.
    /// <para>
    /// A critic rates a movie with stars, on a date. The stars are neither a value of the critic, who rates many movies, nor of the
    /// movie, rated by many critics: they are the values of the relation itself. The rating is an edge: an <c>[Entity]</c> with
    /// <c>[Edge(nameof(MCritic), nameof(MMovie))]</c>, whose interface starts with <c>E</c>, <c>ERating</c>. Neither the critic nor
    /// the movie declares anything: the edge gives them its members.
    /// </para>
    /// <para>
    /// <c>ada.NewRating(dune, stars: 5)</c> makes the rating born between its two ends, its values given or set next. The rating
    /// reads its ends, <c>rating.Critic</c> and <c>rating.Movie</c>, named after their types. Each end reads its ratings: the critic
    /// those it gave, <c>ada.RatingEdges</c>, whose other ends are its <c>Targets</c>; the movie those it received,
    /// <c>dune.RatingEdges</c>, whose other ends are its <c>Sources</c>. The rating of a pair is found at once: <c>To(dune)</c> from
    /// the critic, <c>From(ada)</c> from the movie.
    /// </para>
    /// </summary>
    public sealed class Lesson01RelateTwoEntitiesWithValues
    {
        /// <summary>A critic rates a movie: the rating is born between them, and reads both.</summary>
        [Fact]
        public void RateAMovie()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-rate", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using (var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var ada = new Critic { Name = "Ada" };
                var dune = new Movie { Title = "Dune", Year = 2021 };

                // The rating, from the critic to the movie, with its values.
                var rating = ada.NewRating(dune, stars: 5, at: new DateTime(2026, 10, 1));
                Assert.Same(ada, rating.Critic);
                Assert.Same(dune, rating.Movie);

                // Both ends read it at the next instruction.
                Assert.Equal(1, ada.RatingEdges.Count());
                Assert.Equal(1, dune.RatingEdges.Count());
                transaction.Commit();
            }
            using (reviews.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var rating = RatingCollection.Single();
                Assert.Equal("Ada", rating.Critic.Name);
                Assert.Equal("Dune", rating.Movie.Title);
                Assert.Equal(5, rating.Stars);
            }
        }

        /// <summary>The ratings of a critic and of a movie: the other ends, and the rating of a pair, found at once.</summary>
        [Fact]
        public void ReadTheRatingsFromBothEnds()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-read", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var (ada, bob) = (new Critic { Name = "Ada" }, new Critic { Name = "Bob" });
            var (dune, alien) = (new Movie { Title = "Dune" }, new Movie { Title = "Alien" });
            ada.NewRating(dune, 5);
            ada.NewRating(alien, 3);
            bob.NewRating(dune, 4);

            // From the critic: the movies it rated, and its rating of one of them.
            Assert.Equal(["Alien", "Dune"], ada.RatingEdges.Targets.Select(static m => m.Title).Order().ToList());
            Assert.Equal(3, ada.RatingEdges.To(alien).Get().Stars);

            // From the movie: the critics who rated it, and the rating of one of them.
            Assert.Equal(["Ada", "Bob"], dune.RatingEdges.Sources.Select(static c => c.Name).Order().ToList());
            Assert.Equal(4, dune.RatingEdges.From(bob).Get().Stars);

            // A pair without rating.
            Assert.False(bob.RatingEdges.Contains(alien));
            Assert.False(bob.RatingEdges.To(alien).Any());
            transaction.Commit();
        }

        /// <summary>The values of an edge change as those of any entity; its ends never change.</summary>
        [Fact]
        public void ChangeTheValuesOfAnEdge()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-change", StoreAccessMode.ReadWrite);
            using var reviews = new ReviewRepository(store);
            using var transaction = reviews.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Critic { Name = "Ada" };
            var dune = new Movie { Title = "Dune" };
            var rating = ada.NewRating(dune, 3);

            // The critic sees it again, and changes its mind.
            rating.Stars = 5;
            rating.At = new DateTime(2026, 10, 15);
            Assert.Equal(5, dune.RatingEdges.From(ada).Get().Stars);

            // The ends of an edge have no Setter: a rating of another movie is another rating.
            transaction.Commit();
        }
    }
}
