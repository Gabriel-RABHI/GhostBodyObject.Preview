using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Reviews
{
    using static ReviewRepositoryCollections;

    // The third model, of Lesson 12: the reviews of a hotel, their comments, and the replies to the comments.

    /// <summary>The text of a comment.</summary>
    [ValueObject<GhostString>]
    internal interface VCommentText { }

    /// <summary>A comment, and the replies to it: a bag of its own type, to any depth.</summary>
    [Record(2, 1, 3)]
    internal interface RComment
    {
        [Property(1)]
        VName Author { get; set; }

        [Property(2)]
        VCommentText Text { get; set; }

        [Property(3)]
        RecordBag<RComment> Replies { get; set; }
    }

    [Entity(1, 1, 2), Root, Enumerable]
    internal interface MReview
    {
        [Property(1)]
        VName Hotel { get; set; }

        [Property(2)]
        RecordBag<RComment> Comments { get; set; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MReview)), IncludeRecordModel(nameof(RComment))]
    internal interface MReviewRepository
    {
    }

    /// <summary>
    /// Lesson 12: Records at any depth.
    /// <para>
    /// A Record holds a bag of its own type: a comment and its replies, and their replies. All of it stays in the entity: the review is
    /// read as a whole, in place, by a method that follows the replies.
    /// </para>
    /// <para>
    /// A bag is written directly through a Body, of an entity or of a Record. A Record taken from a bag is a View, whose bag has no writes:
    /// to change its replies, take its copy, add to the bag of the copy, and put it back with <c>ReplaceAt</c>. A deep change copies each
    /// level, and rewrites each bag on its way: a tree that changes often is made of entities (Tutorial Two, Lesson 7).
    /// </para>
    /// </summary>
    public sealed class Lesson12RepliesOfReplies
    {
        // The comments of a bag, and the replies to them, at any depth.
        private static int CountAll(RecordBagView<CommentView> comments)
        {
            int count = comments.Count();
            foreach (var comment in comments.Enumerate<CommentView>())
                count += CountAll(comment.Replies);
            return count;
        }

        /// <summary>Replies of replies, built outside any transaction, and read at any depth.</summary>
        [Fact]
        public void ReadARecordAtAnyDepth()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-read", StoreAccessMode.ReadWrite);
            using var hotel = new ReviewRepository(store);

            var reply = new Comment { Author = "Bob", Text = "Which room?" };
            reply.Replies.Add(new Comment { Author = "Ada", Text = "Number 12." });
            var comment = new Comment { Author = "Ada", Text = "A wonderful view." };
            comment.Replies.Add(reply);

            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Review { Hotel = "Ritz" }.Comments.Add(comment);
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(3, CountAll(ReviewViewCollection.Single().Comments));
        }

        /// <summary>A reply added under a comment: the comment copied, its bag written through the copy, then put back in the review.</summary>
        [Fact]
        public void ChangeARecordBelowTheBagOfTheEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-change", StoreAccessMode.ReadWrite);
            using var hotel = new ReviewRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var review = new Review { Hotel = "Ritz" };
            review.Comments.Add(new Comment { Author = "Ada", Text = "A wonderful view." });

            Assert.True(review.Comments.TryGetAt(0, out CommentView comment));
            var edited = comment.ToBody();
            edited.Replies.Add(new Comment { Author = "Bob", Text = "Which room?" });
            review.Comments.ReplaceAt(0, edited);

            Assert.Equal(2, CountAll(review.ToView().Comments));

            // The View taken before is a value: it has no reply.
            Assert.Equal(0, comment.Replies.Count());
        }
    }
}
