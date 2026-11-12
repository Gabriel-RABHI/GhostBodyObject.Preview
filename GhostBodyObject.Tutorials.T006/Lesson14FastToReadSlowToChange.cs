using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T006.Bills
{
    using static BillRepositoryCollections;

    // A business rule of the developer: called at each change of the lines. It keeps LinesChanges, a property of the model.
    internal partial class RecordedBill
    {
        partial void OnLinesChanged() => LinesChanges++;
    }

    /// <summary>
    /// Lesson 14: fast to read, slow to change.
    /// <para>
    /// The Records of an entity live inside it. This is what makes them fast to read: the entity read, its Records are there, with nothing
    /// else to reach. It is also what makes them slow to change: a change of one Record writes its entity, and all that the entity holds.
    /// The larger the bag, the more one change costs; a change of a part allocates the same at any size, and leaves the other lines alone.
    /// </para>
    /// <para>
    /// So a Record suits what is read often and changed rarely, and a bag stays short. When many Records change together, change them
    /// once: fill a free bag, or the copy of a bag, then assign it.
    /// </para>
    /// </summary>
    public sealed class Lesson14FastToReadSlowToChange
    {
        /// <summary>The Records of every bill, read in place through the Views of their entities.</summary>
        [Fact]
        public void ReadEveryRecordOfEveryEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-read", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                for (int i = 0; i < 100; i++)
                {
                    var bill = new RecordedBill { Guest = "Ada" };
                    bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
                    bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
                }
                transaction.Commit();
            }

            using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                long total = 0;
                foreach (var bill in RecordedBillViewCollection)
                    foreach (var line in bill.Lines.Enumerate<BillLineView>())
                        total += line.Amount.Value;
                Assert.Equal(340_000, total);
            }
        }

        /// <summary>Each <c>Add</c> is a change of the entity; a free bag, filled then assigned, is one.</summary>
        [Fact]
        public void WriteManyRecordsAtOnce()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-once", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

            var oneByOne = new RecordedBill { Guest = "Ada" };
            for (int i = 0; i < 100; i++)
                oneByOne.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
            Assert.Equal(100, oneByOne.LinesChanges);

            var lines = new RecordBag<BillLineView>();
            for (int i = 0; i < 100; i++)
                lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
            var atOnce = new RecordedBill { Guest = "Ada", Lines = lines };
            Assert.Equal(1, atOnce.LinesChanges);

            Assert.Equal(100, atOnce.Lines.Count());
        }

        /// <summary>Several Records changed at once: the copy of the bag, changed, then assigned.</summary>
        [Fact]
        public void ChangeManyRecordsAtOnce()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-many", StoreAccessMode.ReadWrite);
            using var bills = new BillRepository(store);
            using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

            var bill = new RecordedBill { Guest = "Ada" };
            bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
            bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
            int changes = bill.LinesChanges;

            var lines = bill.ToView().Lines.ToBody();
            lines.RemoveAll<BillLineView>();
            lines.Add(new BillLine { Label = "Menu", Amount = 3_200 });
            bill.Lines = lines;

            Assert.Equal(changes + 1, bill.LinesChanges);
            Assert.Equal(1, bill.Lines.Count());
        }
    }
}
