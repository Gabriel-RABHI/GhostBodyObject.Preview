using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Stays
{
    /// <summary>
    /// Lesson 10: a bag of any Record, the messenger.
    /// <para>
    /// <c>RecordBag</c>, without types, holds any Record: a messenger for several services. The stay leaves its requests in one place;
    /// housekeeping reads the rooms to clean, the kitchen the breakfasts, and each one leaves the others. <c>Count()</c> counts them all;
    /// <c>Enumerate&lt;T&gt;()</c> and <c>Count&lt;T&gt;()</c> see one type; a copy of the bag keeps every Record, those that its reader
    /// cannot read too: a service forwards what it does not read.
    /// </para>
    /// <para>
    /// A bag tells its Records apart by their number and their version, never by their names. Records that travel together in a bag take
    /// distinct numbers, across models: nothing refuses two types of one number, and each one reads as the other.
    /// </para>
    /// <para>
    /// A Record can carry such a bag: one message that holds others, sent as a whole.
    /// </para>
    /// </summary>
    public sealed class Lesson10ABagOfAnyRecord
    {
        /// <summary>Each service reads the types it knows, in one bag.</summary>
        [Fact]
        public void LetEachServiceReadItsTypes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-services", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
            stay.Requests.Add(new WakeUp { Room = 12, At = new DateTime(2026, 11, 3, 7, 0, 0, DateTimeKind.Utc) });

            // The bag counts everything; each service sees its own.
            Assert.Equal(3, stay.Requests.Count());
            Assert.Equal(1, stay.Requests.Count<CleanRoomView>());
            Assert.Equal([2], stay.Requests.Enumerate<BreakfastView>().Select(static breakfast => breakfast.Persons.Value).ToArray());
        }

        /// <summary>A service takes its Records out, and leaves the others to the next one.</summary>
        [Fact]
        public void TakeYourTypesAndLeaveTheOthers()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-leave", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
            stay.Requests.Add(new CleanRoom { Room = 14 });

            Assert.Equal(2, stay.Requests.RemoveAll<CleanRoomView>());
            Assert.Equal(1, stay.Requests.Count());
            Assert.True(stay.Requests.TryGetAt(0, out BreakfastView _));
        }

        /// <summary>A Record that carries a bag: one message for several services, created before any transaction.</summary>
        [Fact]
        public void CarryMessagesInARecord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-carry", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);

            var plan = new DayPlan { Day = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc) };
            plan.Instructions.Add(new CleanRoom { Room = 12 });
            plan.Instructions.Add(new Breakfast { Room = 12, Persons = 2 });

            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(plan);

            Assert.True(stay.Requests.TryGetAt(0, out DayPlanView received));
            Assert.Equal(2, received.Instructions.Count());
            Assert.Equal(1, received.Instructions.Count<CleanRoomView>());
        }

        /// <summary>Handlers over a bag of any Record: the types of a service, in one walk.</summary>
        [Fact]
        public void WalkABagOfAnyRecordByAHandlerPerType()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-walk", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
            stay.Requests.Add(new WakeUp { Room = 14 });

            // The kitchen does not know the wake-up calls: skipped, and still in the bag.
            var rooms = new List<int>();
            stay.Requests.Enumerate(rooms,
                static (List<int> rooms, CleanRoomView clean) => rooms.Add(clean.Room.Value),
                static (List<int> rooms, BreakfastView breakfast) => rooms.Add(breakfast.Room.Value));

            Assert.Equal([12, 12], rooms);
            Assert.Equal(3, stay.Requests.Count());
        }

        /// <summary>A copy of the bag keeps every Record, in its order: a service forwards what it does not read.</summary>
        [Fact]
        public void ForwardWhatYouDoNotRead()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-forward", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Requests.Add(new CleanRoom { Room = 12 });
            stay.Requests.Add(new WakeUp { Room = 14 });

            var forwarded = stay.ToView().Requests.ToBody();
            Assert.Equal(0, forwarded.Count<BreakfastView>());
            Assert.True(forwarded.Count() == 2 && forwarded.TryGetAt(1, out WakeUpView call) && call.Room == 14);
        }

        /// <summary>A bag knows a Record by its number: a comment of the reviews and a charge both take 2, so a comment counts as a charge.</summary>
        [Fact]
        public void TakeDistinctNumbersForTheRecordsThatTravelTogether()
        {
            var messages = new RecordBag();
            messages.Add(new Reviews.Comment { Author = "Ada" });

            Assert.Equal(1, messages.Count<ChargeView>());
        }
    }
}
