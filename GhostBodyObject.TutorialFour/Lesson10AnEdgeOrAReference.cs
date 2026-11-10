using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFour.Choices
{
    using static ForumRepositoryCollections;

    // The model of Lesson 10: a forum and a bank, three relations between two entities, each with the link it needs. A like has no
    // value: a collection of weak references. A reaction has a kind: an edge. A transfer between two accounts is a fact that must
    // not vanish with an account: a Root with two strong references.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The kind of a reaction: "Insightful".</summary>
    [ValueObject<GhostString>]
    internal interface VReactionKind { }

    /// <summary>An amount of money: never negative.</summary>
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;
    }

    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MUser
    {
        [Property(1)]
        VName Name { get; set; }

        /// <summary>The posts the user likes: no value, a collection of weak references.</summary>
        [Property(2), Weak]
        IEntityCollection<MPost> Likes { get; }
    }

    [Entity(2, 1, 1), Root, Removable, Enumerable]
    internal interface MPost
    {
        [Property(1)]
        VFreeText Text { get; set; }

        /// <summary>The users who like the post: the reverse of their likes.</summary>
        [ReverseWeakReference(nameof(MUser.Likes))]
        IEntityCollection<MUser> LikedBy { get; }
    }

    /// <summary>A reaction of a user to a post, of a kind: an edge.</summary>
    [Entity(3, 1, 1), Edge(nameof(MUser), nameof(MPost)), Removable, Enumerable]
    internal interface EReaction
    {
        [Property(1)]
        VReactionKind Kind { get; set; }
    }

    [Entity(4, 1, 1), Root, Removable, Enumerable]
    internal interface MAccount
    {
        [Property(1)]
        VName Owner { get; set; }
    }

    /// <summary>A transfer between two accounts: a fact of the business, which retains its accounts.</summary>
    [Entity(5, 1, 3), Root, Removable, Enumerable]
    internal interface MTransfer
    {
        [Property(1)]
        VMoney Amount { get; set; }

        [Property(2), Strong]
        MAccount From { get; set; }

        [Property(3), Strong]
        MAccount To { get; set; }
    }

    [GhostRepository(4), IncludeEntityModel(nameof(MUser)), IncludeEntityModel(nameof(MPost)), IncludeEntityModel(nameof(EReaction)), IncludeEntityModel(nameof(MAccount)), IncludeEntityModel(nameof(MTransfer))]
    internal interface MForumRepository
    {
    }

    /// <summary>
    /// Lesson 10: an edge, or a reference.
    /// <para>
    /// A relation between two entities is an edge when three answers are yes: it carries values; it belongs to neither of its two
    /// ends; it must vanish with either. A like has no value: a collection of references, and its reverse property, give everything
    /// it needs. A reaction has a kind: an edge. A transfer between two accounts carries an amount, but it must not vanish with an
    /// account: the history of the bank needs it, and other entities designate it. It is a Root, with a strong reference to each
    /// account, which retains them.
    /// </para>
    /// <para>
    /// What the removal of an end does follows from the choice: the like leaves the collection, the reaction leaves with its post, the
    /// transfer refuses the removal of its account.
    /// </para>
    /// </summary>
    public sealed class Lesson10AnEdgeOrAReference
    {
        private static ForumRepository NewForum(TransactionalMemoryStore store)
        {
            var forum = new ForumRepository(store);
            using var transaction = forum.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new User { Name = "Ada" };
            var post = new Post { Text = "Edges are entities." };
            ada.Likes.Add(post);
            ada.NewReaction(post, kind: "Insightful");
            var (savings, current) = (new Account { Owner = "Ada" }, new Account { Owner = "Bob" });
            new Transfer(savings, current) { Amount = 120m };
            transaction.Commit();
            return forum;
        }

        /// <summary>Each relation, read from both of its ends, as its link reads.</summary>
        [Fact]
        public void ChooseTheLinkOfEachRelation()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-choose", StoreAccessMode.ReadWrite);
            using var forum = NewForum(store);
            using var transaction = forum.OpenTransaction(StoreAccessMode.ReadOnly);
            var ada = UserCollection.Single();
            var post = PostCollection.Single();
            Assert.Equal(["Ada"], post.LikedBy.Select(static u => u.Name).ToList());
            Assert.Equal("Insightful", post.ReactionEdges.From(ada).Get().Kind);
            Assert.Equal(120m, TransferCollection.Single(static t => t.From.Owner == "Ada").Amount);
        }

        /// <summary>The post removed: the like leaves the collection, the reaction leaves with it; an account with a transfer stays.</summary>
        [Fact]
        public void RemoveAnEndOfEachRelation()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-remove", StoreAccessMode.ReadWrite);
            using var forum = NewForum(store);
            using var transaction = forum.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = UserCollection.Single();
            PostCollection.Single().Remove();
            Assert.Equal(0, ada.Likes.Count());
            Assert.Equal(0, ReactionCollection.Count());

            // The transfer retains its accounts: the removal is refused, and nothing changes.
            var savings = AccountCollection.Single(static a => a.Owner == "Ada");
            Assert.Throws<InvalidOperationException>(savings.Remove);
            Assert.Equal(1, TransferCollection.Count());
            transaction.Commit();
        }
    }
}
