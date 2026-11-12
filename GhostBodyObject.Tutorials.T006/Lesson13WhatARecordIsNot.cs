using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace Tutorials.T006.Bills
{
    using static BillRepositoryCollections;

    // The fourth model, of Lessons 13 and 14: one restaurant bill, written twice. Its lines as Records, which is the mistake that these
    // lessons show, and its lines as parts, which is the model of Tutorial T002.

    /// <summary>The line of a bill, as a Record: the wrong choice, for the comparison.</summary>
    [Record(4, 1, 2)]
    internal interface RBillLine
    {
        [Property(1)]
        VLabel Label { get; set; }

        [Property(2)]
        VCents Amount { get; set; }
    }

    /// <summary>A bill whose lines are Records: they live inside it, and it is the only thing that changes.</summary>
    [Entity(1, 1, 3), Root, Enumerable]
    internal interface MRecordedBill
    {
        [Property(1)]
        VName Guest { get; set; }

        [Property(2)]
        RecordBag<RBillLine> Lines { get; set; }

        /// <summary>How many times the lines changed: kept by a rule of yours (Lesson 14).</summary>
        [Property(3)]
        int LinesChanges { get; set; }
    }

    /// <summary>The line of a bill, as a part: an entity of its own, in its aggregate.</summary>
    [Entity(3, 1, 2), Enumerable]
    internal interface MLine
    {
        [Property(1)]
        VLabel Label { get; set; }

        [Property(2)]
        VCents Amount { get; set; }
    }

    /// <summary>A bill whose lines are parts: each one changes, and is found, and is linked, on its own.</summary>
    [Entity(2, 1, 3), Root, Enumerable]
    internal interface MBill
    {
        [Property(1)]
        VName Guest { get; set; }

        [Property(2)]
        IEntityCollection<MLine> Lines { get; }

        /// <summary>The line that the guest disputes: a link to a part. A Record cannot be the target of a link.</summary>
        [Property(3), Weak]
        Option<MLine> Disputed { get; set; }
    }

    /// <summary>An index of the lines: a field of a Record cannot be indexed.</summary>
    [ValueIndex(nameof(MLine), 1), ValueIndexComponent(1, nameof(MLine.Label))]
    internal interface MLineLabelIndex
    {
    }

    [GhostRepository(4), IncludeEntityModel(nameof(MRecordedBill)), IncludeEntityModel(nameof(MBill)), IncludeEntityModel(nameof(MLine)), IncludeRecordModel(nameof(RBillLine))]
    internal interface MBillRepository
    {
    }

    /// <summary>
    /// Lesson 13: what a Record is not.
    /// <para>
    /// A Record never replaces a separate entity. The lines of a bill look like a list of values, and a bag would hold them. It is the
    /// mistake to avoid: the lines of a bill are changed one by one, by several people, found, disputed. Each of these needs what only an
    /// entity has. The charges of a stay have the same fields, and they are values: each one is added, then read with its stay. What
    /// separates the two is not their shape, but what happens to them.
    /// </para>
    /// <para>
    /// <b>Its host is its unit of conflict.</b> A Record has no version of its own: two transactions that change two different Records of
    /// one bag change the same entity, and the second commit conflicts. Two parts are two entities: nothing conflicts.
    /// </para>
    /// <para>
        /// <b>It is not found.</b> An index on a Record or on a bag stops the build with GBO049, <c>[TextIndexed]</c> in a Record with GBO032, and
    /// an index names the properties of its entity, never a field of a Record (GBO036). A search reads every host.
    /// </para>
    /// <para>
    /// <b>It is not linked.</b> A reference points to an entity. A Record has no identity to point to, and <c>[Strong]</c> or <c>[Weak]</c> on
    /// a Record stops the build with GBO049 too.
    /// </para>
    /// </summary>
    public sealed class Lesson13WhatARecordIsNot
    {
        // Two transactions on two lines of the first bill: the first one changes its line, then waits for the second one to commit. The
        // second one is on another thread: each transaction belongs to its flow.
        private static async Task<Exception?> TwoTransactions(BillRepository bills, Action first, Action second)
        {
            var firstChanged = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var waiting = Task.Run(async () =>
            {
                using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);
                first();
                firstChanged.SetResult();
                await secondCommitted.Task;
                return Record.Exception(transaction.Commit);
            });
            await await Task.WhenAny(firstChanged.Task, waiting);
            await Task.Run(() =>
            {
                using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);
                second();
                transaction.Commit();
            });
            secondCommitted.SetResult();
            return await waiting;
        }

        /// <summary>Two people change two different Records of one bag: they change one entity, and the second commit conflicts.</summary>
        [Fact]
        public async Task ConflictOnTheHostOfTwoRecords()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-records", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bill = new RecordedBill { Guest = "Ada" };
                bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
                bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
                transaction.Commit();
            }

            static void Change(int at, long amount)
            {
                var lines = RecordedBillCollection.Single().Lines;
                Assert.True(lines.TryGetAt(at, out BillLineView line));
                var changed = line.ToBody();
                changed.Amount = amount;
                lines.ReplaceAt(at, changed);
            }

            var refused = await TwoTransactions(bills, static () => Change(0, 2_500), static () => Change(1, 500));
            Assert.IsType<TransactionConflictException>(refused);
        }

        /// <summary>The same change on two parts: two entities, and both commits pass.</summary>
        [Fact]
        public async Task LeaveTwoPartsToTwoTransactions()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-parts", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bill = new Bill { Guest = "Ada" };
                var wine = bill.Lines.New();
                wine.Label = "Wine";
                wine.Amount = 3_000;
                var coffee = bill.Lines.New();
                coffee.Label = "Coffee";
                coffee.Amount = 400;
                transaction.Commit();
            }

            static void Change(string label, long amount) => LineCollection.Single(label, static (line, label) => line.Label == label).Amount = amount;

            var refused = await TwoTransactions(bills, static () => Change("Wine", 2_500), static () => Change("Coffee", 500));
            Assert.Null(refused);

            using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(3_000, LineViewCollection.Sum(static line => line.Amount.Value));
        }

        /// <summary>A part is found by an index, without reading the bills; a Record, by reading every bill.</summary>
        [Fact]
        public void FindAPartByAnIndexAndARecordByReadingEverything()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-find", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var bill = new Bill { Guest = "Ada" };
                var wine = bill.Lines.New();
                wine.Label = "Wine";

                var recorded = new RecordedBill { Guest = "Ada" };
                recorded.Lines.Add(new BillLine { Label = "Wine" });
                transaction.Commit();
            }

            using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(1, LineLabelIndex.Is("Wine").Count());

                // No index can do this: every bill is read, and every line of each one.
                Assert.Equal(1, RecordedBillViewCollection.Count(static bill => bill.Lines.Enumerate<BillLineView>().Any(static line => line.Label == "Wine")));
            }
        }

        /// <summary>A link follows its part, sees its changes, and is emptied when the part leaves. A Record can only be copied.</summary>
        [Fact]
        public void LinkToAPartAndNeverToARecord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-link", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

            var bill = new Bill { Guest = "Ada" };
            var wine = bill.Lines.New();
            wine.Label = "Wine";
            wine.Amount = 3_000;
            bill.Disputed = wine;

            wine.Amount = 2_500;
            Assert.True(bill.Disputed.Get().Amount == 2_500);

            bill.Lines.Remove(wine);
            Assert.False(bill.Disputed.Any());
        }
    }
}
