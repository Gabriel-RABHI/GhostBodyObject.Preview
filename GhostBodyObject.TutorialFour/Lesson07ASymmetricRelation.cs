using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFour.Social
{
    using static SocialRepositoryCollections;

    // The model of Lessons 7 to 9: a social network. People are friends: a friendship has no direction. People belong to groups,
    // each with a role, a link of the membership. A certificate proves a membership, and retains it; a notice tells a friendship,
    // and forgets it when it ends.

    [Entity(1, 1, 1), Root, Removable, Enumerable, WithIdentity]
    internal interface MPerson
    {
        [Property(1)]
        VName Name { get; set; }
    }

    // A symmetric edge: one per pair, seen the same from both ends, of one type.
    /// <summary>A friendship between two people.</summary>
    [Entity(2, 1, 1), Edge(nameof(MPerson), nameof(MPerson), Symmetric = true), Removable, Enumerable]
    internal interface EFriendship
    {
        [Property(1)]
        VInstant Since { get; set; }
    }

    [Entity(3, 1, 1), Root, Removable, Enumerable]
    internal interface MGroup
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(4, 1, 1), Root, Removable, Enumerable]
    internal interface MRole
    {
        [Property(1)]
        VName Name { get; set; }
    }

    // An edge with a required link to a third entity: given to its birth (Lesson 8).
    /// <summary>The membership of a person in a group, with its role.</summary>
    [Entity(5, 1, 2), Edge(nameof(MPerson), nameof(MGroup)), Removable, Enumerable]
    internal interface EMembership
    {
        [Property(1), Strong]
        MRole Role { get; set; }

        [Property(2)]
        VInstant Since { get; set; }
    }

    /// <summary>A certificate of membership: it retains the membership, and its person and its group through it (Lesson 8).</summary>
    [Entity(6, 1, 2), Root, Removable, Enumerable]
    internal interface MCertificate
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2), Strong]
        EMembership Membership { get; set; }
    }

    /// <summary>A notice of a new friendship: it designates the friendship, without retaining it (Lesson 8).</summary>
    [Entity(7, 1, 2), Root, Removable, Enumerable]
    internal interface MNotice
    {
        [Property(1)]
        VFreeText Text { get; set; }

        [Property(2), Weak]
        Option<EFriendship> Friendship { get; set; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MPerson)), IncludeEntityModel(nameof(EFriendship)), IncludeEntityModel(nameof(MGroup)), IncludeEntityModel(nameof(MRole)), IncludeEntityModel(nameof(EMembership)), IncludeEntityModel(nameof(MCertificate)), IncludeEntityModel(nameof(MNotice))]
    internal interface MSocialRepository
    {
    }

    /// <summary>
    /// Lesson 7: a symmetric relation.
    /// <para>
    /// A friendship has no direction: Ada is the friend of Bob as Bob is the friend of Ada. <c>[Edge(..., Symmetric = true)]</c>, between
    /// two entities of one type, gives each end one member, <c>FriendshipEdges</c>, whose other ends are its <c>Others</c>; the edge of
    /// a pair is <c>With(bob)</c>, from either end. A friendship is one per pair: <c>bob.NewFriendship(ada)</c>, after
    /// <c>ada.NewFriendship(bob)</c>, is refused. <c>friendship.Other(ada)</c> gives the friend of Ada.
    /// </para>
    /// <para>
    /// Two steps give the friends of friends; your code ranks the people a person may know by the friends they have in common.
    /// </para>
    /// </summary>
    public sealed class Lesson07ASymmetricRelation
    {
        /// <summary>A friendship, the same from both ends; a second one, in the other direction, refused.</summary>
        [Fact]
        public void BefriendTwoPeople()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-friends", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
            var (ada, bob) = (NewPerson("Ada"), NewPerson("Bob"));
            var friendship = ada.NewFriendship(bob, since: new DateTime(2020, 5, 1));

            Assert.Same(friendship, bob.FriendshipEdges.With(ada).Get());
            Assert.Equal(["Bob"], ada.FriendshipEdges.Others.Select(static p => p.Name).ToList());
            Assert.Equal(["Ada"], bob.FriendshipEdges.Others.Select(static p => p.Name).ToList());
            Assert.Same(bob, friendship.Other(ada));
            Assert.Throws<InvalidOperationException>(() => bob.NewFriendship(ada));

            // The friendship ends, from either end.
            Assert.True(bob.FriendshipEdges.Remove(ada));
            Assert.Equal(0, ada.FriendshipEdges.Count());
            transaction.Commit();
        }

        /// <summary>The friends of the friends of Ada, ranked by the friends they share with Ada, by the Views.</summary>
        [Fact]
        public void SuggestThePeopleToKnow()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-suggest", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var (ada, bob, cid, dan, eve) = (NewPerson("Ada"), NewPerson("Bob"), NewPerson("Cid"), NewPerson("Dan"), NewPerson("Eve"));
                ada.NewFriendship(bob);
                ada.NewFriendship(cid);
                bob.NewFriendship(dan);
                cid.NewFriendship(dan);
                cid.NewFriendship(eve);
                transaction.Commit();
            }
            using (social.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var ada = PersonViewCollection.Single(static p => p.Name == "Ada");
                // Dan is a friend of Bob and of Cid; Eve, of Cid only.
                Assert.Equal(["Dan", "Eve"], PeopleToKnow(ada));
            }
        }

        // The friends of the friends of a person, but the person and its friends, ranked by the friends they share with it: two steps, from
        // the View of a person to the Views of the others.
        private static List<string> PeopleToKnow(PersonView person)
        {
            var friends = new HashSet<PersonRef> { person.Ref };
            foreach (var friend in person.FriendshipEdges.Others)
                friends.Add(friend.Ref);
            var shared = new Dictionary<PersonRef, (string Name, int Count)>();
            foreach (var friend in person.FriendshipEdges.Others)
            {
                foreach (var other in friend.FriendshipEdges.Others)
                {
                    if (friends.Contains(other.Ref))
                        continue;
                    shared[other.Ref] = (other.Name.ToString(), shared.TryGetValue(other.Ref, out var known) ? known.Count + 1 : 1);
                }
            }
            var ranked = new List<(string Name, int Count)>(shared.Values);
            ranked.Sort(static (a, b) => b.Count != a.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Name, b.Name));
            return ranked.ConvertAll(static p => p.Name);
        }

        private static Person NewPerson(Name name) => new(PersonRef.New()) { Name = name };
    }
}
