using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Stays
{
    using static StayRepositoryCollections;

    /// <summary>
    /// Lesson 8: change a bag.
    /// <para>
    /// Six writes, each one a change of the entity that holds the bag: <c>Add</c> at the end; <c>RemoveAt</c> a position;
    /// <c>Remove</c> the first Record equal to a value; <c>RemoveAll</c> of a value, or of a type; <c>ReplaceAt</c> a position;
    /// <c>Clear</c>. A removal moves the Records after it down by one place.
    /// </para>
    /// <para>
    /// A Record of a bag is a value, as a Record property: it is not edited in place. To change one, take its copy, change it, and put it
    /// back: <c>ToBody()</c>, then <c>ReplaceAt</c>. This is for the rare correction: what changes often, one by one, is an entity (Lesson 13).
    /// </para>
    /// </summary>
    public sealed class Lesson08ChangeABag
    {
        /// <summary>Remove by position, by value, every equal value, every Record of a type, then all.</summary>
        [Fact]
        public void RemoveRecordsFromABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-remove", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            foreach (int room in new[] { 12, 14, 12, 16, 12 })
                stay.Requests.Add(new CleanRoom { Room = room });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

            // A position: the Records after it move down by one place.
            stay.Requests.RemoveAt(1);
            Assert.True(stay.Requests.TryGetAt(1, out CleanRoomView next) && next.Room == 12);

            // The first equal value; false when none is.
            Assert.True(stay.Requests.Remove(new CleanRoom { Room = 16 }));
            Assert.False(stay.Requests.Remove(new CleanRoom { Room = 99 }));

            // Every equal value: their number.
            Assert.Equal(3, stay.Requests.RemoveAll(new CleanRoom { Room = 12 }));

            // Every Record of a type.
            stay.Requests.Add(new CleanRoom { Room = 14 });
            Assert.Equal(1, stay.Requests.RemoveAll<BreakfastView>());
            Assert.Equal(1, stay.Requests.Count());

            stay.Requests.Clear();
            Assert.True(stay.Requests.Count() == 0 && stay.Requests.Size == 0);
        }

        /// <summary>A position takes another value, of any type that the bag accepts; a position out of the bag throws.</summary>
        [Fact]
        public void ReplaceARecordOfABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-replace", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.ReplaceAt(0, new Breakfast { Room = 12, Persons = 2 });

            Assert.Equal(1, stay.Requests.Count());
            Assert.True(stay.Requests.TryGetAt(0, out BreakfastView breakfast) && breakfast.Persons == 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => stay.Requests.ReplaceAt(1, new CleanRoom { Room = 14 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => stay.Requests.RemoveAt(1));
        }

        /// <summary>One field of a Record of a bag: its copy, changed, then put back.</summary>
        [Fact]
        public void ChangeARecordOfABagByItsCopy()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-copy", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

            Assert.True(stay.Charges.TryGetAt(0, out ChargeView room));
            var corrected = room.ToBody();
            corrected.Amount = 16_000;
            stay.Charges.ReplaceAt(0, corrected);

            Assert.True(stay.Charges.TryGetAt(0, out ChargeView changed) && changed.Amount == 16_000);

            // The View read before is a value: it keeps the old amount.
            Assert.Equal(18_000, room.Amount.Value);
        }

        /// <summary>A bag is written in a transaction that writes, as its entity is: a read-only transaction refuses it.</summary>
        [Fact]
        public void RefuseAWriteInAReadOnlyTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-readonly", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Stay { Guest = "Ada" };
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var stay = StayCollection.Single();
                Assert.Throws<InvalidOperationException>(() => stay.Charges.Add(new Charge { Label = "Room" }));
            }
        }
    }
}
