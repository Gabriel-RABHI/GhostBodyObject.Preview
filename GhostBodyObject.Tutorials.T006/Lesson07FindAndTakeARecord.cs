using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T006.Stays
{
    /// <summary>
    /// Lesson 7: find and take a Record.
    /// <para>
    /// A Record has no identity: it is found by its type, by its position, or by its value. <c>IndexOf&lt;T&gt;</c> gives the position of
    /// the first Record of a type; <c>IndexOf(record)</c> and <c>Contains(record)</c> look for a value. Equal means the same type and the
    /// same values, in the same version of the type, compared exactly as they are kept: the same moment of another <c>Kind</c> is another value.
    /// </para>
    /// <para>
    /// Finding reads the bag from its start: a bag is a short list that an entity carries, not a table to search.
    /// </para>
    /// </summary>
    public sealed class Lesson07FindAndTakeARecord
    {
        /// <summary>By its value: the same type and the same values are equal, wherever the Record comes from.</summary>
        [Fact]
        public void FindARecordByItsValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-value", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
            stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

            Assert.True(stay.Charges.Contains(new Charge { Label = "Breakfast", Amount = 2_500 }));
            Assert.False(stay.Charges.Contains(new Charge { Label = "Breakfast", Amount = 2_000 }));

            Assert.True(stay.Charges.IndexOf(new Charge { Label = "Breakfast", Amount = 2_500 }, out int at));
            Assert.Equal(1, at);

            // The View of the bag searches as the bag does.
            Assert.True(stay.ToView().Charges.Contains(new Charge { Label = "Room", Amount = 18_000 }));

            // Compared exactly as they are kept: the same moment of another Kind is another value.
            var noon = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
            stay.Charges.Add(new Charge { Label = "Parking", At = noon });
            Assert.False(stay.Charges.Contains(new Charge { Label = "Parking", At = DateTime.SpecifyKind(noon, DateTimeKind.Unspecified) }));
        }

        /// <summary>By its type: the position of the first Record of a type, in a bag that holds several.</summary>
        [Fact]
        public void FindARecordByItsType()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-type", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

            Assert.True(stay.Requests.IndexOf<BreakfastView>(out int at) && at == 1);
            Assert.False(stay.Requests.IndexOf<WakeUpView>(out _));

            // A position gives its Record only when it is of the type asked.
            Assert.False(stay.Requests.TryGetAt(0, out BreakfastView _));
            Assert.True(stay.Requests.TryGetAt(1, out BreakfastView breakfast) && breakfast.Persons == 2);
        }

        /// <summary>Take a Record of a type: find it, read it, remove it. Its View stays a value after the removal.</summary>
        [Fact]
        public void TakeARecordOutOfABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-take", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

            Assert.True(stay.Requests.IndexOf<BreakfastView>(out int at));
            Assert.True(stay.Requests.TryGetAt(at, out BreakfastView breakfast));
            stay.Requests.RemoveAt(at);

            Assert.Equal(2, breakfast.Persons.Value);
            Assert.Equal(1, stay.Requests.Count());
        }

        /// <summary>Take the Records in their order: read the first one, remove it, until the bag is empty.</summary>
        [Fact]
        public void TakeTheRecordsInTheirOrder()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-queue", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
            stay.Requests.Add(new CleanRoom { Room = 14 });

            var rooms = new List<int>();
            while (stay.Requests.Count() > 0)
            {
                if (stay.Requests.TryGetAt(0, out CleanRoomView clean))
                    rooms.Add(clean.Room.Value);
                stay.Requests.RemoveAt(0);
            }

            Assert.Equal([12, 14], rooms);
        }
    }
}
