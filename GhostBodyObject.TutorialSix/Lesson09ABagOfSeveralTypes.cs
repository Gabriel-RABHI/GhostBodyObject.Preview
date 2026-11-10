using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Stays
{
    using static StayRepositoryCollections;

    /// <summary>
    /// Lesson 9: a bag of several types.
    /// <para>
    /// <c>RecordBag&lt;RCheckedIn, RRoomChanged, RCheckedOut&gt;</c> holds these types, and no other, up to eight. The constraint is
        /// checked by the compiler: adding another type does not compile, and asking for it stops the build with GBO050. It is a constraint
    /// of the code: converted to <c>RecordBag</c>, the same bag takes any Record (Lesson 11).
    /// </para>
    /// <para>
    /// Its Records stay in the order of their additions, whatever their types: this is what a list per type would lose. One walk,
    /// <c>Enumerate(h1, h2, h3)</c>, gives each Record to the handler of its type. The handlers are <c>static</c> lambdas, which capture
    /// nothing: up to four values are passed before them, as in every query of GBO.
    /// </para>
    /// </summary>
    public sealed class Lesson09ABagOfSeveralTypes
    {
        private static readonly DateTime Monday = new(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);

        private static StayRepository NewHotel(TransactionalMemoryStore store)
        {
            var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
            var stay = new Stay { Guest = "Ada" };
            stay.History.Add(new CheckedIn { Room = 12, At = Monday });
            stay.History.Add(new RoomChanged { From = 12, To = 14, At = Monday.AddHours(2) });
            stay.History.Add(new RoomChanged { From = 14, To = 16, At = Monday.AddHours(20) });
            stay.History.Add(new CheckedOut { At = Monday.AddDays(2) });
            transaction.Commit();
            return hotel;
        }

        /// <summary>Each type is counted and read on its own; the bag counts them all.</summary>
        [Fact]
        public void ReadEachTypeOfABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-types", StoreAccessMode.ReadWrite);
            using var hotel = NewHotel(store);
            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var history = StayViewCollection.Single().History;
                Assert.Equal(4, history.Count());
                Assert.Equal(2, history.Count<RoomChangedView>());
                Assert.Equal([14, 16], history.Enumerate<RoomChangedView>().Select(static change => change.To.Value).ToArray());
            }
        }

        /// <summary>One walk, in the order of the Records, each one to the handler of its type; the state is passed, never captured.</summary>
        [Fact]
        public void WalkABagByAHandlerPerType()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-walk", StoreAccessMode.ReadWrite);
            using var hotel = NewHotel(store);
            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var story = new List<string>();
                StayViewCollection.Single().History.Enumerate(story,
                    static (List<string> story, CheckedInView entry) => story.Add($"in {entry.Room.Value}"),
                    static (List<string> story, RoomChangedView change) => story.Add($"to {change.To.Value}"),
                    static (List<string> story, CheckedOutView leaving) => story.Add("out"));

                Assert.Equal(["in 12", "to 14", "to 16", "out"], story);
            }
        }

        /// <summary>A handler for one type: the Records of the others are skipped.</summary>
        [Fact]
        public void SkipTheTypesWithoutAHandler()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-skip", StoreAccessMode.ReadWrite);
            using var hotel = NewHotel(store);
            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var rooms = new List<int>();
                StayViewCollection.Single().History.Enumerate(rooms, static (List<int> rooms, RoomChangedView change) => rooms.Add(change.To.Value));
                Assert.Equal([14, 16], rooms);
            }
        }
    }
}
