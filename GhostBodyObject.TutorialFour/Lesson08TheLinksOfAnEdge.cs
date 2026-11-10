using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFour.Social
{
    using static SocialRepositoryCollections;

    /// <summary>
    /// Lesson 8: the links of an edge.
    /// <para>
    /// An edge is an entity: it has the links of any entity (Tutorial Two). A required link, a <c>T</c>, is given to its birth, after
    /// its other end: <c>ada.NewMembership(club, member)</c>; a strong one retains its target, the role. <c>GetOrNew</c> and
    /// <c>TryGetOrNew</c> compare it: Ada a member of the club as an admin, where the club has Ada as a member, is a case of the
    /// business, which the code handles at once.
    /// </para>
    /// <para>
    /// Other entities link to an edge as to any entity. A strong reference to an edge retains it, and its two ends through it: a
    /// certificate of membership keeps the membership, the person and the group, until it is revoked. A weak reference to an edge is
    /// set to none when the edge goes: a notice of a friendship forgets it when the friendship ends.
    /// </para>
    /// </summary>
    public sealed class Lesson08TheLinksOfAnEdge
    {
        /// <summary>A membership born with its role, which it retains; another role given by its Setter.</summary>
        [Fact]
        public void GiveAnEdgeARequiredLink()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-required", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Person(PersonRef.New()) { Name = "Ada" };
            var club = new Group { Name = "Chess club" };
            var (member, admin) = (new Role { Name = "Member" }, new Role { Name = "Admin" });

            // The role is a T of the membership: a parameter of its birth, after the group.
            var membership = ada.NewMembership(club, member, since: new DateTime(2026, 1, 1));
            Assert.Throws<InvalidOperationException>(member.Remove);

            // Ada as an admin, a member already: TryGetOrNew gives the membership, and false.
            if (!ada.MembershipEdges.TryGetOrNew(club, admin, out var found))
                found.Role = admin;
            Assert.Same(membership, found);
            Assert.Same(admin, club.MembershipEdges.From(ada).Get().Role);

            // The member role is free.
            member.Remove();
            transaction.Commit();
        }

        /// <summary>A certificate retains the membership, and Ada and the club through it; revoked, it frees them.</summary>
        [Fact]
        public void RetainAnEdgeAndItsEnds()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-retain", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Person(PersonRef.New()) { Name = "Ada" };
            var club = new Group { Name = "Chess club" };
            var membership = ada.NewMembership(club, new Role { Name = "Member" });
            var certificate = new Certificate(membership) { Title = "Member of the chess club" };

            // Neither the membership nor its ends can leave while the certificate designates it: the exception names the edge.
            Assert.Throws<InvalidOperationException>(membership.Remove);
            Assert.Contains("Membership", Assert.Throws<InvalidOperationException>(club.Remove).Message);
            Assert.Throws<InvalidOperationException>(ada.Remove);

            // The certificate revoked: the club can go, with the membership.
            certificate.Remove();
            club.Remove();
            Assert.Equal(0, MembershipCollection.Count());
            transaction.Commit();
        }

        /// <summary>A notice of a friendship forgets it when the friendship ends.</summary>
        [Fact]
        public void ForgetARemovedEdge()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-forget", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
            var (ada, bob) = (new Person(PersonRef.New()) { Name = "Ada" }, new Person(PersonRef.New()) { Name = "Bob" });
            var notice = new Notice { Text = "Ada and Bob are friends", Friendship = ada.NewFriendship(bob) };
            Assert.True(notice.Friendship.Any());

            ada.FriendshipEdges.Remove(bob);
            Assert.False(notice.Friendship.Any());
            transaction.Commit();
        }
    }
}
