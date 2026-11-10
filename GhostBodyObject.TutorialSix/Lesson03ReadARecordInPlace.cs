using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Guests
{
    using static GuestRepositoryCollections;

    /// <summary>
    /// Lesson 3: read a Record in place.
    /// <para>
    /// The property of a Record gives its View, on a Body as on a View: an <c>AddressView</c>, read-only. Reading it copies nothing and
    /// allocates nothing, at any depth: <c>guest.Home.City</c> reads the city where it is, as <c>guest.Name</c> reads the name. This is
    /// where a Record is strong: a query over thousands of guests reads their addresses without a copy, faster than in a part (Lesson 14).
    /// </para>
    /// <para>
    /// The View of a Record is a value, unlike the View of an entity, which reads its entity as it is now in its transaction. It keeps
    /// what it read: a later change of the guest does not change it, and it stays readable after its transaction, on another thread, for
    /// as long as you hold it.
    /// </para>
    /// </summary>
    public sealed class Lesson03ReadARecordInPlace
    {
        /// <summary>A query reads the Records of its entities through their Views, as it reads their other values.</summary>
        [Fact]
        public void QueryTheEntitiesByARecord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-query", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
                new Guest { Name = "Bob", Home = new Address { City = "Lyon" } };
                new Guest { Name = "Eve", Home = new Address { City = "Paris" } };
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(2, GuestViewCollection.Count(static guest => guest.Home.City == "Paris"));
                Assert.Equal(["Ada", "Eve"], GuestViewCollection.Where(static guest => guest.Home.City == "Paris").Select(static guest => guest.Name.ToString()).ToArray());
            }
        }

        /// <summary>Reading a Record allocates nothing, at any depth: it is read where it is.</summary>
        [Fact]
        public void ReadWithoutAnAllocation()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-allocation", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Guest { Name = "Ada", Home = new Address { City = "Paris" }, Emergency = new Contact { Address = new Address { City = "London" } } };
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var ada = GuestViewCollection.Single();
                int letters = ada.Home.City.Length;

                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1_000; i++)
                    letters += ada.Home.City.Length + ada.Emergency.Address.City.Length;

                Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
                Assert.Equal(11_005, letters);
            }
        }

        /// <summary>The View of an entity follows its transaction; the View of a Record keeps what it read.</summary>
        [Fact]
        public void KeepTheValueThatAViewRead()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-value", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
            var guest = ada.ToView();
            var before = ada.Home;

            ada.Home = new Address { City = "Lyon" };
            ada.Name = "A name long enough to change the guest a lot more";

            Assert.True(guest.Home.City == "Lyon" && before.City == "Paris");
        }

        /// <summary>A View of a Record stays readable after its transaction, and on another thread: it travels.</summary>
        [Fact]
        public async Task ReadAViewAfterItsTransactionOnAnotherThread()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-travel", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
                transaction.Commit();
            }

            AddressView home;
            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
                home = GuestViewCollection.Single().Home;

            Assert.True(await Task.Run(() => home.City == "Paris"));
        }
    }
}
