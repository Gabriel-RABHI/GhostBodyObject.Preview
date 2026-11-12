using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T006.Stays
{
    /// <summary>
    /// Lesson 11: a free bag, and the copies of a bag.
    /// <para>
    /// <c>stay.Charges</c> is bound to its stay: its writes change the stay. A bag is also free: <c>new RecordBag&lt;ChargeView&gt;()</c> belongs
    /// to nothing, and needs no transaction. Filled, then assigned, it is copied into its host once: the way to write many Records at
    /// once.
    /// </para>
    /// <para>
    /// The View of a bag, <c>stay.ToView().Charges</c>, is a value, as the View of a Record: a later change of the stay does not change
    /// it. Its <c>ToBody()</c> gives a free copy, to change and to assign. A <c>RecordBag</c> is a <c>ref struct</c>, as a
    /// <c>GhostString</c>: it stays in its method; its View, a plain struct, may be kept.
    /// </para>
    /// <para>
    /// The types of a bag are a constraint of the code. A bag of given types converts to <c>RecordBag</c> without a cast: the same bag, which
    /// then takes any Record. <c>new RecordBag&lt;ChargeView&gt;(bag)</c> puts the types back; the same for a View, between
    /// <c>RecordBagView</c> and <c>RecordBagView&lt;ChargeView&gt;</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson11AFreeBag
    {
        /// <summary>A free bag, filled outside any transaction, then assigned: copied once into the stay.</summary>
        [Fact]
        public void FillAFreeBagThenAssignIt()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-free", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);

            var charges = new RecordBag<ChargeView>();
            charges.Add(new Charge { Label = "Room", Amount = 18_000 });
            charges.Add(new Charge { Label = "Parking", Amount = 1_500 });

            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
            var stay = new Stay { Guest = "Ada", Charges = charges };

            Assert.Equal(2, stay.Charges.Count());
            transaction.Commit();
        }

        /// <summary>The View of a bag keeps what it read, after a change of its stay.</summary>
        [Fact]
        public void KeepTheValueThatAViewOfABagRead()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-value", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

            var before = stay.ToView().Charges;
            stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500 });

            Assert.True(before.Count() == 1 && stay.Charges.Count() == 2);
        }

        /// <summary>The copy of a bag: changed apart, then assigned; a bag assigned from one stay to another is copied.</summary>
        [Fact]
        public void CopyABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-copy", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Stay { Guest = "Ada" };
            ada.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

            var copy = ada.ToView().Charges.ToBody();
            copy.Add(new Charge { Label = "Parking", Amount = 1_500 });
            Assert.Equal(1, ada.Charges.Count());

            var bob = new Stay { Guest = "Bob", Charges = copy };
            ada.Charges.Clear();

            Assert.True(ada.Charges.Count() == 0 && bob.Charges.Count() == 2);
        }

        /// <summary>A free bag holds any Record too; a bag that a type constrains takes only its types.</summary>
        [Fact]
        public void HoldAnyRecordInAFreeBag()
        {
            var messages = new RecordBag();
            messages.Add(new CleanRoom { Room = 12 });
            messages.Add(new Breakfast { Room = 12, Persons = 2 });

            Assert.Equal(2, messages.Count());
            Assert.Equal(1, messages.Count<BreakfastView>());
        }

        /// <summary>A free bag of several types, named by the Views of its types.</summary>
        [Fact]
        public void FillAFreeBagOfSeveralTypes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-types", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);

            var history = new RecordBag<CheckedInView, RoomChangedView, CheckedOutView>();
            history.Add(new CheckedIn { Room = 12 });
            history.Add(new RoomChanged { From = 12, To = 14 });

            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
            var stay = new Stay { Guest = "Ada", History = history };

            Assert.True(stay.History.Count() == 2 && stay.History.TryGetAt(1, out RoomChangedView change) && change.To == 14);
        }

        /// <summary>The types of a bag are a constraint of the code: a conversion takes them off, a constructor puts them back, on the same bag.</summary>
        [Fact]
        public void TakeTheTypesOffABagAndPutThemBack()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-convert", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

            // Without its types, the bag of the stay takes any Record: the compiler no longer checks it.
            RecordBag any = stay.Charges;
            any.Add(new CleanRoom { Room = 12 });
            Assert.Equal(2, stay.Charges.Count());

            var charges = new RecordBag<ChargeView>(any);
            Assert.Equal(1, charges.Count<ChargeView>());

            // The same for its View: a method that reads any bag takes it, without its types.
            RecordBagView read = stay.ToView().Charges;
            Assert.Equal(1, new RecordBagView<ChargeView>(read).Count<ChargeView>());
        }
    }
}
