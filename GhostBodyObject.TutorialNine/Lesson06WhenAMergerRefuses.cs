using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialNine
{
    using static SalesRepositoryCollections;

    /// <summary>A poster of a concert: its Merger does what no Merger may do, for this lesson to show the refusal.</summary>
    [Entity(4, 1, 3), Root, Enumerable, Mergeable]
    internal interface MPoster
    {
        [Property(1)]
        VTitle Concert { get; set; }

        [Property(2)]
        VMessage Caption { get; set; }

        [Property(3), Delta]
        VTickets Printed { get; set; }
    }

    internal sealed partial class Poster
    {
        // A [Delta] belongs to the engine: a Merger that writes it fails the commit, as a Merger that throws.
        private bool Merge(PosterView epoch, PosterView head)
        {
            Printed = head.Printed;
            return true;
        }
    }

    /// <summary>
    /// Lesson 6: when a Merger refuses.
    /// <para>
    /// A Merger that cannot choose returns <c>false</c>: the commit fails as a conflict, of the kind <c>Change</c>, nothing written, and the
    /// application decides, as for any conflict. This is an answer, part of the rule of the type.
    /// </para>
    /// <para>
    /// A Merger that fails is another matter: it threw, or did what a Merger may not. It writes only the values without a rule of merge,
    /// and reads only its entity: writing a <c>[Delta]</c>, a <c>[Max]</c>, a <c>[Min]</c> or a link, or reading a link, is refused. The commit is
    /// canceled by a <c>GhostMergeException</c>, nothing written, its cause in <c>InnerException</c> when the Merger threw. It is a defect of
    /// the code, not a conflict: <c>TryCommit</c> throws it too.
    /// </para>
    /// </summary>
    public sealed class Lesson06WhenAMergerRefuses
    {
        private static Notice Carmen() => NoticeCollection.First(static notice => notice.Concert == "Carmen");

        private static Poster ThePoster() => PosterCollection.Single();

        /// <summary>Two rooms at the same time: the Merger of Lesson 5 refuses, and the commit fails as a conflict.</summary>
        [Fact]
        public void FailTheCommitWhenTheMergerRefuses()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-refuse", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Notice { Concert = "Carmen", Message = "Tickets at the door", Room = "Main hall" };
                transaction.Commit();
            }
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Room = "Small hall";
                AnotherTill.Commits(sales, static () => Carmen().Room = "Foyer");
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Change, kind);
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Foyer", Carmen().Room.ToString());
        }

        /// <summary>A Merger that writes a <c>[Delta]</c>: not a conflict, a defect. The commit throws, even by <c>TryCommit</c>, nothing written.</summary>
        [Fact]
        public void FailTheCommitOfAMergerThatDoesWhatItMayNot()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-defect", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Poster { Concert = "Carmen", Caption = "Bizet", Printed = 100 };
                transaction.Commit();
            }
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ThePoster().Caption = "Georges Bizet";
                AnotherTill.Commits(sales, static () => ThePoster().Caption = "Bizet, 1875");
                Assert.Throws<GhostMergeException>(() => transaction.TryCommit(out _));
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Bizet, 1875", ThePoster().Caption.ToString());
        }
    }
}
