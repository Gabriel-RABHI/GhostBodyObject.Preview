using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Stays
{
    using static StayRepositoryCollections;

    // The second model, of Lessons 6 to 11: the stays of the hotel, what each one was charged, what happened during it, and the requests
    // that it sends to the services of the hotel.

    /// <summary>A number of persons.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VPersons { }

    /// <summary>A charge of a stay: a value that the stay owns, added once, then read with the others.</summary>
    [Record(2, 1, 3)]
    internal interface RCharge
    {
        [Property(1)]
        VLabel Label { get; set; }

        [Property(2)]
        VCents Amount { get; set; }

        [Property(3)]
        VInstant At { get; set; }
    }

    /// <summary>What happened to a stay: the three Record types of its history (Lesson 9).</summary>
    [Record(3, 1, 2)]
    internal interface RCheckedIn
    {
        [Property(1)]
        VRoomNumber Room { get; set; }

        [Property(2)]
        VInstant At { get; set; }
    }

    [Record(4, 1, 3)]
    internal interface RRoomChanged
    {
        [Property(1)]
        VRoomNumber From { get; set; }

        [Property(2)]
        VRoomNumber To { get; set; }

        [Property(3)]
        VInstant At { get; set; }
    }

    [Record(5, 1, 1)]
    internal interface RCheckedOut
    {
        [Property(1)]
        VInstant At { get; set; }
    }

    /// <summary>The messages to the services of the hotel: a bag of any Record carries them (Lesson 10).</summary>
    [Record(6, 1, 1)]
    internal interface RCleanRoom
    {
        [Property(1)]
        VRoomNumber Room { get; set; }
    }

    [Record(7, 1, 2)]
    internal interface RBreakfast
    {
        [Property(1)]
        VRoomNumber Room { get; set; }

        [Property(2)]
        VPersons Persons { get; set; }
    }

    [Record(8, 1, 2)]
    internal interface RWakeUp
    {
        [Property(1)]
        VRoomNumber Room { get; set; }

        [Property(2)]
        VInstant At { get; set; }
    }

    /// <summary>The instructions of a day, one message that carries others: a Record holds a bag.</summary>
    [Record(9, 1, 2)]
    internal interface RDayPlan
    {
        [Property(1)]
        VInstant Day { get; set; }

        [Property(2)]
        RecordBag Instructions { get; set; }
    }

    [Entity(1, 1, 4), Root, Enumerable]
    internal interface MStay
    {
        [Property(1)]
        VName Guest { get; set; }

        /// <summary>What the stay was charged: a bag of one type, its Records in their order.</summary>
        [Property(2)]
        RecordBag<RCharge> Charges { get; set; }

        /// <summary>What happened: a bag of these three types, and no other.</summary>
        [Property(3)]
        RecordBag<RCheckedIn, RRoomChanged, RCheckedOut> History { get; set; }

        /// <summary>The requests to the services: a bag of any Record.</summary>
        [Property(4)]
        RecordBag Requests { get; set; }
    }

    // The messages are named by no property of the model: the Repository includes them by their names.
    [GhostRepository(2), IncludeEntityModel(nameof(MStay)), IncludeRecordModel(nameof(RCleanRoom)), IncludeRecordModel(nameof(RBreakfast)),
        IncludeRecordModel(nameof(RWakeUp)), IncludeRecordModel(nameof(RDayPlan)), IncludeRecordModel(nameof(RCharge)), IncludeRecordModel(nameof(RCheckedIn)), IncludeRecordModel(nameof(RCheckedOut)), IncludeRecordModel(nameof(RRoomChanged))]
    internal interface MStayRepository
    {
    }

    /// <summary>
    /// Lesson 6: a bag of one type.
    /// <para>
    /// A list of values that an entity owns, in their order: the charges of a stay. A collection of Records does not exist: a list of
    /// Records is a <c>RecordBag</c>, one property, <c>RecordBag&lt;RCharge&gt;</c> for this type alone.
    /// </para>
    /// <para>
    /// Unlike a Record property, a bag is written directly: <c>stay.Charges.Add(charge)</c> changes the stay, without an assignment. It
    /// is read as a Record, in place: <c>Enumerate&lt;ChargeView&gt;()</c> gives the Views of its Records, in their order, with the
    /// operators of the queries of GBO. The positions are those of the additions, from 0 to <c>Count() - 1</c>.
    /// </para>
    /// <para>
    /// The model names the interfaces, the code names their Views: <c>RecordBag&lt;RCharge&gt;</c> in the model is a
    /// <c>RecordBag&lt;ChargeView&gt;</c> in the code, and a <c>RecordBagView&lt;ChargeView&gt;</c> through the View of the stay.
    /// </para>
    /// </summary>
    public sealed class Lesson06ABagOfOneType
    {
        private static readonly DateTime Monday = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>Records added to a stay, then read in their order, in the transaction and after it.</summary>
        [Fact]
        public void AddRecordsAndReadThemInTheirOrder()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-add", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var stay = new Stay { Guest = "Ada" };
                Assert.Equal(0, stay.Charges.Count());

                // No assignment: the bag belongs to the stay, and its Add changes the stay.
                stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000, At = Monday });
                stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500, At = Monday.AddHours(1) });
                stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500, At = Monday.AddHours(2) });

                Assert.Equal(3, stay.Charges.Count());
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var stay = StayViewCollection.Single();
                Assert.Equal(["Room", "Breakfast", "Parking"], stay.Charges.Enumerate<ChargeView>().Select(static charge => charge.Label.ToString()).ToArray());

                long total = 0;
                foreach (var charge in stay.Charges.Enumerate<ChargeView>())
                    total += charge.Amount.Value;
                Assert.Equal(22_000, total);
            }
        }

        /// <summary>The Records of a bag answer the operators of a query, in place.</summary>
        [Fact]
        public void QueryTheRecordsOfABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-query", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
            stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

            var charges = stay.ToView().Charges;
            Assert.True(charges.Enumerate<ChargeView>().Any(static charge => charge.Amount > 10_000));
            Assert.Equal(1, charges.Enumerate<ChargeView>().Where(static charge => charge.Amount < 10_000).Count());
            Assert.True(charges.Enumerate<ChargeView>().FirstOrNone(static charge => charge.Label == "Breakfast").TryGet(out var breakfast) && breakfast.Amount == 2_500);
        }

        /// <summary>A position gives a Record, when it is of the type asked; out of the bag, it throws, as a span does.</summary>
        [Fact]
        public void ReadARecordByItsPosition()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-position", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
            stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

            Assert.True(stay.Charges.TryGetAt(1, out ChargeView second) && second.Label == "Breakfast");
            Assert.Throws<ArgumentOutOfRangeException>(() => stay.Charges.TryGetAt(2, out ChargeView _));
        }

        /// <summary><c>Size</c> tells what a bag adds to its entity, in bytes: nothing when it is empty.</summary>
        [Fact]
        public void TellTheSizeOfABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-size", StoreAccessMode.ReadWrite);
            using var hotel = new StayRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var stay = new Stay { Guest = "Ada" };
            Assert.Equal(0, stay.Charges.Size);

            stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
            int one = stay.Charges.Size;
            stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500 });
            Assert.True(one > 0 && stay.Charges.Size > one);
        }
    }
}
