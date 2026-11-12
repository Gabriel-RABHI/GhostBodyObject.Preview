using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T006
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has. A value object is a value of a Record as of an entity.

    /// <summary>The name of a person or of a hotel.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>What a charge or the line of a bill is for.</summary>
    [ValueObject<GhostString>]
    internal interface VLabel { }

    /// <summary>The number of a room.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VRoomNumber { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    /// <summary>An amount in cents: never negative, added and subtracted.</summary>
    [ValueObject<long>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VCents { }

    internal partial struct Cents
    {
        private static bool IsValid(long cents) => cents >= 0;
    }
}

namespace Tutorials.T006.Guests
{
    using static GuestRepositoryCollections;

    // The first model, of Lessons 1 to 5: the guests of a hotel, and their addresses.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The street of an address, its number included.</summary>
    [ValueObject<GhostString>]
    internal interface VStreet { }

    /// <summary>The city of an address.</summary>
    [ValueObject<GhostString>]
    internal interface VCity { }

    /// <summary>The postal code of an address.</summary>
    [ValueObject<GhostString>]
    internal interface VPostalCode { }

    /// <summary>A phone number.</summary>
    [ValueObject<GhostString>]
    internal interface VPhone { }

    // [Record(type, version, maxPropertyIdentifier)]: a Record type, as [Entity] declares an entity type. Its interface starts with R:
    // RAddress gives the class Address, to create the value, and the struct AddressView, to read it. Its number is one of the model: an
    // entity type and a Record type never take the same one. Two models may take the same numbers, as those of this tutorial do, as long
    // as their Records never meet in one bag (Lesson 10).
    /// <summary>A postal address: several values that go together, without an identity of their own.</summary>
    [Record(3, 1, 3)]
    internal interface RAddress
    {
        [Property(1)]
        VStreet Street { get; set; }

        [Property(2)]
        VCity City { get; set; }

        [Property(3)]
        VPostalCode PostalCode { get; set; }
    }

    /// <summary>Someone to call: a Record holds Records too.</summary>
    [Record(4, 1, 3)]
    internal interface RContact
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        VPhone Phone { get; set; }

        [Property(3)]
        RAddress Address { get; set; }
    }

    [Entity(1, 1, 6), Root, Removable, Enumerable]
    internal interface MGuest
    {
        [Property(1)]
        VName Name { get; set; }

        /// <summary>Where the guest lives: a Record is a property, never absent, an empty address until written.</summary>
        [Property(2)]
        RAddress Home { get; set; }

        /// <summary>Where the invoices go, when it is not home: an optional Record.</summary>
        [Property(3)]
        Option<RAddress> Billing { get; set; }

        /// <summary>Whom to call: a Record that holds a Record.</summary>
        [Property(4)]
        RContact Emergency { get; set; }

        /// <summary>A link to another entity, for the comparison of Lesson 2: a Record has none.</summary>
        [Property(5), Weak]
        Option<MHotel> Favorite { get; set; }

        /// <summary>How many times the home changed: kept by a rule of yours (Lesson 4).</summary>
        [Property(6)]
        int HomeChanges { get; set; }
    }

    [Entity(2, 1, 2), Root, Enumerable]
    internal interface MHotel
    {
        [Property(1)]
        VName Name { get; set; }

        /// <summary>The same Record type, in another entity: a value that several places reuse.</summary>
        [Property(2)]
        RAddress Location { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MGuest)), IncludeEntityModel(nameof(MHotel)), IncludeRecordModel(nameof(RAddress)), IncludeRecordModel(nameof(RContact))]
    internal interface MGuestRepository
    {
    }

    // A business rule of the developer, for Lesson 4: called on a real change of the Record only. It keeps HomeChanges, a property of the
    // model, never a field of the Body (Tutorial T001, Lesson 11).
    internal partial class Guest
    {
        partial void OnHomeChanged() => HomeChanges++;
    }

    /// <summary>
    /// Lesson 1: declare a Record.
    /// <para>
    /// A value object holds one value: a city, an amount. An address is several of them that always go together: a street, a city, a
    /// postal code. A Record declares them once, as an entity is declared: an interface, its properties numbered, <c>[Record]</c> in
    /// place of <c>[Entity]</c>. Any entity holds it as one property.
    /// </para>
    /// <para>
    /// GBO writes two types: the class <c>Address</c>, to create a value and to write it, and the struct <c>AddressView</c>, to read it.
    /// An <c>Address</c> is created anywhere, on any thread, outside any transaction: it belongs to nothing. Assigned to a guest, its
    /// values are copied into the guest, and saved with it.
    /// </para>
    /// </summary>
    public sealed class Lesson01DeclareARecord
    {
        /// <summary>An address, created outside any transaction, assigned to a guest, and read back after the commit.</summary>
        [Fact]
        public void HoldARecordInAnEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-hold", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);

            // No transaction is open: a Record needs none to be born.
            var home = new Address { Street = "12 rue de Rivoli", City = "Paris", PostalCode = "75001" };

            using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Guest { Name = "Ada", Home = home };
                transaction.Commit();
            }

            using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var ada = GuestCollection.Single();
                Assert.True(ada.Home.City == "Paris" && ada.Home.Street == "12 rue de Rivoli");
                Assert.True(GuestViewCollection.Single().Home.PostalCode == "75001");
            }
        }

        /// <summary>A Record property is never absent: until written, it is the default Record, each field at its default, its texts empty.</summary>
        [Fact]
        public void ReadTheDefaultRecordOfAPropertyNeverWritten()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-default", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var ritz = new Hotel { Name = "Ritz" };
            Assert.True(ritz.Location.City.IsEmpty && ritz.Location.Street.IsEmpty);
        }

        /// <summary>One Record type, in two entity types: a value that several places reuse.</summary>
        [Fact]
        public void ReuseARecordInSeveralEntities()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-reuse", StoreAccessMode.ReadWrite);
            using var hotel = new GuestRepository(store);
            using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

            var address = new Address { Street = "15 place Vendome", City = "Paris", PostalCode = "75001" };
            var ritz = new Hotel { Name = "Ritz", Location = address };
            var ada = new Guest { Name = "Ada", Home = address };

            Assert.True(ritz.Location.City == "Paris" && ada.Home.City == "Paris");
            transaction.Commit();
        }
    }
}
