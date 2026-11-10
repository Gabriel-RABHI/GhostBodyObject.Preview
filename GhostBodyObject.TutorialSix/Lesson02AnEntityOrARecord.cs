using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Guests
{
    using static GuestRepositoryCollections;

    /// <summary>
    /// Lesson 2: an entity or a Record.
    /// <para>
    /// An entity is someone: it exists on its own, it is found, it is linked, and two entities of the same values stay two. A Record is
    /// something: a value, like a number, that exists only in what holds it. Two Records of the same values are the same value, and
    /// giving a Record to a second entity gives it a copy.
    /// </para>
    /// <para>
    /// A hotel that two guests like is one hotel: when it changes its name, both see the new one. An address that two guests share is
    /// two copies of a value: when one moves, the other stays where they are.
    /// </para>
    /// </summary>
    public sealed class Lesson02AnEntityOrARecord
    {
        /// <summary>A link designates one entity: everyone who holds it sees its changes.</summary>
        [Fact]
        public void ShareAnEntityByALink()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-link", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ritz = new Hotel { Name = "Ritz" };
            var ada = new Guest { Name = "Ada", Favorite = ritz };
            var bob = new Guest { Name = "Bob", Favorite = ritz };

            ritz.Name = "The Ritz";
            Assert.True(ada.Favorite.Get().Name == "The Ritz" && bob.Favorite.Get().Name == "The Ritz");
        }

        /// <summary>A Record has no link: assigned to a second entity, it is copied, and each one then lives its own life.</summary>
        [Fact]
        public void CopyARecordByAnAssignment()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-copy", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris" } };
            var bob = new Guest { Name = "Bob", Home = ada.Home };

            // Ada moves: Bob keeps the value received.
            ada.Home = new Address { Street = "3 quai Saint-Antoine", City = "Lyon" };
            Assert.True(ada.Home.City == "Lyon" && bob.Home.City == "Paris");
        }

        /// <summary>An entity stands alone, in its collection; a Record lives in its host, and leaves with it. Nothing is left to clean.</summary>
        [Fact]
        public void LiveAndLeaveWithItsHost()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-host", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
                transaction.Commit();
            }
            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // A collection of guests exists; a collection of addresses does not: an address is found through what holds it.
                Assert.Equal(1, GuestViewCollection.Count(static guest => guest.Home.City == "Paris"));

                GuestCollection.Single().Remove();
                transaction.Commit();
            }
            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Empty(GuestCollection);
        }
    }
}
