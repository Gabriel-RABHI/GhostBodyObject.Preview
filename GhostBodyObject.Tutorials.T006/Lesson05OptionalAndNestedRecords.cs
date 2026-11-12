using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T006.Guests
{
    /// <summary>
    /// Lesson 5: an optional Record, and a Record in a Record.
    /// <para>
    /// <c>Option&lt;RAddress&gt;</c> may be absent, which the empty default of a plain Record cannot say: a guest without a billing
    /// address is not a guest with an empty one.
    /// </para>
    /// <para>
    /// A Record holds Records, to any depth: it is read straight through, <c>guest.Emergency.Address.City</c>, and changed level by level:
    /// each level is copied out, then assigned back to the level above.
    /// </para>
    /// </summary>
    public sealed class Lesson05OptionalAndNestedRecords
    {
        /// <summary>An optional Record: absent, present, then absent again.</summary>
        [Fact]
        public void TellAnAbsentRecordFromAnEmptyOne()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-option", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada" };
            Assert.False(ada.Billing.Any());

            ada.Billing = new Address { City = "Geneva" };
            Assert.True(ada.Billing.TryGet(out var billing) && billing.City == "Geneva");

            // An empty address is an address: present, and empty.
            ada.Billing = new Address();
            Assert.True(ada.Billing.Any());

            ada.Billing = Option.None;
            Assert.False(ada.Billing.Any());
        }

        /// <summary>A Record in a Record is read straight through.</summary>
        [Fact]
        public void ReadARecordInARecord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-read", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest {
                Name = "Ada",
                Emergency = new Contact { Name = "Charles", Phone = "+33 1 40 20 50 50", Address = new Address { City = "London" } }
            };

            Assert.True(ada.Emergency.Name == "Charles" && ada.Emergency.Address.City == "London");
            Assert.True(ada.ToView().Emergency.Address.City == "London");
        }

        /// <summary>A change at depth 2: the inner value copied and changed, assigned to the copy of the outer one, assigned to the guest.</summary>
        [Fact]
        public void ChangeARecordInARecord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-change", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest {
                Name = "Ada",
                Emergency = new Contact { Name = "Charles", Address = new Address { City = "London" } }
            };

            var emergency = ada.Emergency.ToBody();
            var address = emergency.Address.ToBody();
            address.City = "Cambridge";
            emergency.Address = address;
            ada.Emergency = emergency;

            Assert.True(ada.Emergency.Name == "Charles" && ada.Emergency.Address.City == "Cambridge");
        }
    }
}
