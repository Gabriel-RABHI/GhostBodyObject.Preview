using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Guests
{
    /// <summary>
    /// Lesson 4: change a Record by its copy.
    /// <para>
    /// A Record is replaced, never edited in place: <c>guest.Home.City = "Lyon"</c> does not compile, because <c>guest.Home</c> is a View.
    /// To replace it whole, assign a new value. To change one field, take a copy of it, <c>ToBody()</c>, change the copy, and assign it:
    /// nothing changes in the guest before the assignment.
    /// </para>
    /// <para>
    /// The steps say what happens: the value is copied out, then copied back. This is why a Record is slower to change than a field of an
    /// entity, and why it suits a value that is read often and changed rarely.
    /// </para>
    /// </summary>
    public sealed class Lesson04ChangeARecordByItsCopy
    {
        /// <summary>Assign a new value: the old one is replaced, whole.</summary>
        [Fact]
        public void ReplaceARecordWhole()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-replace", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris", PostalCode = "75001" } };
            ada.Home = new Address { City = "Lyon" };

            // Whole: the fields that the new value does not give are empty, not kept.
            Assert.True(ada.Home.City == "Lyon" && ada.Home.Street.IsEmpty);

            // The default value empties it.
            ada.Home = default;
            Assert.True(ada.Home.City.IsEmpty);
        }

        /// <summary>One field: a copy, changed, then assigned. The guest does not change before the assignment.</summary>
        [Fact]
        public void ChangeOneFieldByACopy()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-copy", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris" } };

            var home = ada.Home.ToBody();
            home.Street = "14 rue de Rivoli";
            Assert.True(ada.Home.Street == "12 rue de Rivoli");

            ada.Home = home;
            Assert.True(ada.Home.Street == "14 rue de Rivoli" && ada.Home.City == "Paris");
        }

        /// <summary>The copy belongs to you: a later change of it does not follow into the guest that received it.</summary>
        [Fact]
        public void KeepYourCopyApartFromTheHost()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-apart", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var address = new Address { City = "Paris" };
            var ada = new Guest { Name = "Ada", Home = address };

            address.City = "Lyon";
            var bob = new Guest { Name = "Bob", Home = address };

            Assert.True(ada.Home.City == "Paris" && bob.Home.City == "Lyon");
        }

        /// <summary>The same value is no change: no write, and no call of your <c>OnHomeChanged</c>.</summary>
        [Fact]
        public void SeeNoChangeInTheSameValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-same", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
            Assert.Equal(1, ada.HomeChanges);

            ada.Home = new Address { City = "Paris" };
            Assert.Equal(1, ada.HomeChanges);

            ada.Home = new Address { City = "Lyon" };
            Assert.Equal(2, ada.HomeChanges);
        }
    }
}
