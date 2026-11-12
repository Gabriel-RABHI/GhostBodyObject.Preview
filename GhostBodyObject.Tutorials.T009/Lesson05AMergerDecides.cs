using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T009
{
    using static SalesRepositoryCollections;

    /// <summary>The stages of a concert, in their order: a stage only moves on.</summary>
    public enum Stage
    {
        Announced,
        OnSale,
        SoldOut,
        Cancelled
    }

    /// <summary>The notice of a concert on the door of its hall, which each till updates: its values have rules that only your code knows.</summary>
    [Entity(3, 1, 5), Root, Enumerable, Mergeable]
    internal interface MNotice
    {
        [Property(1)]
        VTitle Concert { get; set; }

        [Property(2)]
        VMessage Message { get; set; }

        [Property(3)]
        Stage Stage { get; set; }

        [Property(4), Max]
        VInstant Posted { get; set; }

        [Property(5)]
        VName Room { get; set; }
    }

    internal sealed partial class Notice
    {
        // The Merger of a notice: at the commit, when a value without a rule of merge was changed by both sides to two values. This object
        // holds the values of the transaction; epoch and head, the two other points. It writes what it chooses, and returns true.
        private bool Merge(NoticeView epoch, NoticeView head)
        {
            // Two rooms, changed by both: nothing can choose between them. False fails the commit (Lesson 6).
            if (Room != epoch.Room && head.Room != epoch.Room && Room != head.Room)
                return false;

            // A stage only moves on: the furthest one wins, whatever the order of the commits.
            if (head.Stage > Stage)
                Stage = head.Stage;

            // The latest notice wins. Posted is read as this transaction wrote it: [Max] merges it after the Merger.
            if (head.Posted > Posted)
                Message = head.Message;
            return true;
        }
    }

    /// <summary>
    /// Lesson 5: a Merger decides.
    /// <para>
    /// Two values for one field: the engine cannot choose, and your code can. A <b>Merger</b> is a method of the partial class of the type,
    /// <c>private bool Merge(NoticeView epoch, NoticeView head)</c>. It is the explicit merge, beside the automatic merges of Part 1: the
    /// commit calls it only when a value without a rule of merge is in conflict, once per entity, and never otherwise.
    /// </para>
    /// <para>
    /// It sees the three points: the object itself holds the values of the transaction, the Views hold the epoch and the head. It writes the
    /// values that it chooses, then GBO finishes the merge from them: a value that differs from the epoch keeps the choice of the Merger,
    /// any other takes the head; <c>[Delta]</c>, <c>[Max]</c> and <c>[Min]</c> come last. The commit waits for it, so it stays brief, reads only
    /// these three versions, and gives the same answer every time.
    /// </para>
    /// </summary>
    public sealed class Lesson05AMergerDecides
    {
        private static readonly DateTime Noon = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

        private static void Open(SalesRepository sales)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Notice { Concert = "Carmen", Message = "Tickets at the door", Stage = Stage.OnSale, Posted = Noon, Room = "Main hall" };
            transaction.Commit();
        }

        private static Notice Carmen() => NoticeCollection.First(static notice => notice.Concert == "Carmen");

        /// <summary>Two messages at the same time: the Merger keeps the latest one, whichever commits first.</summary>
        [Theory]
        [InlineData(10, "Ten seats left")]
        [InlineData(30, "Last seats")]
        public void KeepTheLatestMessage(int minutes, string expected)
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore($"lesson-05-latest-{minutes}", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var carmen = Carmen();
                carmen.Message = "Last seats";
                carmen.Posted = Noon.AddMinutes(minutes);

                // The other till posts at 20 minutes, and commits first.
                AnotherTill.Commits(sales, static () =>
                {
                    var notice = Carmen();
                    notice.Message = "Ten seats left";
                    notice.Posted = Noon.AddMinutes(20);
                });
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(expected, Carmen().Message.ToString());

                // Posted is a [Max]: the latest moment, in both cases.
                Assert.Equal(Noon.AddMinutes(Math.Max(minutes, 20)), Carmen().Posted);
            }
        }

        /// <summary>Two stages at the same time: the furthest one wins, in both orders of the commits.</summary>
        [Theory]
        [InlineData(Stage.SoldOut, Stage.Cancelled)]
        [InlineData(Stage.Cancelled, Stage.SoldOut)]
        public void MoveTheStageOn(Stage mine, Stage theirs)
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore($"lesson-05-stage-{mine}", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Stage = mine;
                AnotherTill.Commits(sales, () => Carmen().Stage = theirs);
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(Stage.Cancelled, Carmen().Stage);
        }

        /// <summary>
        /// Without a conflict of values, no Merger: each side keeps what it changed. The Merger would have replaced this message by the older
        /// one of the head, which was posted later.
        /// </summary>
        [Fact]
        public void CallTheMergerOnlyOnAConflict()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-only", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Carmen().Message = "Doors open at seven";
                AnotherTill.Commits(sales, static () =>
                {
                    var notice = Carmen();
                    notice.Stage = Stage.SoldOut;
                    notice.Posted = Noon.AddMinutes(20);
                });
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Doors open at seven", Carmen().Message.ToString());
                Assert.Equal(Stage.SoldOut, Carmen().Stage);
            }
        }
    }
}
