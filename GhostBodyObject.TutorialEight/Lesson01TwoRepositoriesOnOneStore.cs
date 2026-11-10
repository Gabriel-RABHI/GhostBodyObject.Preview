using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialEight.Carrier
{
    using static CarrierRepositoryCollections;
    using static TrackingRepositoryCollections;

    // The model of the tutorial: the parcels of a carrier, in two Repositories of their own, and the Log of what happened to them
    // (Lesson 4). The Repository of the parcels holds what the carrier works with; the Repository of the tracking holds what its
    // customers read: a page for each parcel, a board for each depot.

    // The values of the model, first: each value says what it is by its type, a value object (Tutorial One, Lesson 19). A count of
    // steps or of parcels names no value: it stays a number.

    /// <summary>The tracking number of a parcel, "PX-1001": a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VTrackingNumber { }

    /// <summary>The name of a person, the recipient of a parcel.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>The name of a depot of the carrier.</summary>
    [ValueObject<GhostString>]
    internal interface VDepot { }

    /// <summary>What the customer reads of a parcel: "Registered", "In transit at Lyon", "Delivered".</summary>
    [ValueObject<GhostString>]
    internal interface VStatusLabel { }

    internal enum ParcelStatus
    {
        Registered,
        InTransit,
        Delivered
    }

    [Entity(1, 1, 4), Root, Removable, WithIdentity, Enumerable]
    internal interface MParcel
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(2)]
        VName Recipient { get; set; }

        [Property(3)]
        ParcelStatus Status { get; set; }

        [Property(4)]
        VDepot Depot { get; set; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MParcel))]
    internal interface MCarrierRepository
    {
    }

    // The page of a parcel on the site of the carrier: its identity derived from the one of its parcel, TrackingPageRef.Of(parcel).
    [Entity(11, 1, 4), Root, Removable, WithIdentity, Enumerable]
    internal interface MTrackingPage
    {
        [Property(1)]
        VTrackingNumber TrackingNumber { get; set; }

        [Property(2)]
        VName Recipient { get; set; }

        // What the customer reads: "Registered", "In transit at Lyon", "Delivered".
        [Property(3)]
        VStatusLabel Status { get; set; }

        [Property(4)]
        int Steps { get; set; }
    }

    // The board of a depot: the parcels there now.
    [Entity(12, 1, 2), Root, Removable, Enumerable]
    internal interface MDepotBoard
    {
        [Property(1)]
        VDepot Depot { get; set; }

        [Property(2)]
        int Parcels { get; set; }
    }

    [GhostRepository(2), IncludeEntityModel(nameof(MTrackingPage)), IncludeEntityModel(nameof(MDepotBoard))]
    internal interface MTrackingRepository
    {
    }

    /// <summary>
    /// Lesson 1: two Repositories on one Store.
    /// <para>
    /// A Store is not limited to one Repository: the modules of an application, each with its model, may share one, in one directory,
    /// in one sequence of commits. Each Repository binds its model to the Store when it is created, reads and writes the types of its
    /// model, and knows nothing of the others.
    /// </para>
    /// <para>
    /// One Store is not one transaction: each Repository has its own, committed apart, and one may succeed when the other fails. A chain
    /// of Stores gives them one transaction (Lesson 2).
    /// </para>
    /// <para>
    /// A number is one type in a Store, whatever the name of the type and the Repository that declares it: the numbers of a model are
    /// the truth of its data, its names are not. A Repository that declares the number of a type of another one, even strictly the same, is
    /// refused when it is bound, nothing written, and the Repositories of the Store go on; so is an entity on the number of a Record, a Record on the number
    /// of an entity, and an index on the number of an index of another Repository.
    /// </para>
    /// </summary>
    public sealed class Lesson01TwoRepositoriesOnOneStore
    {
        /// <summary>Two Repositories bound to one Store: each one writes and reads the types of its model.</summary>
        [Fact]
        public void BindTwoRepositoriesToOneStore()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-shared", StoreAccessMode.ReadWrite);
            using var carrier = new CarrierRepository(store);
            using var site = new TrackingRepository(store);

            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-0101", Recipient = "Ada", Depot = "Lyon" };
                transaction.Commit();
            }
            using (var transaction = site.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new DepotBoard { Depot = "Lyon", Parcels = 1 };
                transaction.Commit();
            }

            // One Store holds both models; each Repository reads its own.
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Ada", ParcelViewCollection.Single().Recipient);
            using (site.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Lyon", DepotBoardViewCollection.Single().Depot);
        }

        /// <summary>One Store, two transactions: each Repository commits apart, and a chain needs two Stores.</summary>
        [Fact]
        public void CommitEachRepositoryApart()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-apart", StoreAccessMode.ReadWrite);
            using var carrier = new CarrierRepository(store);
            using var site = new TrackingRepository(store);

            // Each Repository takes its own transaction: the parcel is committed, the count of its depot is not.
            using (var parcelTransaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            using (var boardTransaction = site.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-0102", Recipient = "Ada", Depot = "Lyon" };
                new DepotBoard { Depot = "Lyon", Parcels = 1 };
                parcelTransaction.Commit();
                boardTransaction.Rollback();
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, ParcelViewCollection.Count());
            using (site.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, DepotBoardViewCollection.Count());

            // Nothing makes these two one: a chain takes two Stores (Lesson 2).
            Assert.Throws<InvalidOperationException>(() => carrier.Chain(site));
        }

        /// <summary>A number is one type in a Store: a third Repository takes a number of its own, or it is refused, under any name, in any version, even with the same definition.</summary>
        [Fact]
        public void RefuseTheNumberOfATypeOfAnotherRepository()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-numbers", StoreAccessMode.ReadWrite);
            using var carrier = new CarrierRepository(store);
            using var site = new TrackingRepository(store);

            // The number 11 is the page of a parcel, in the Repository of the tracking: the invoices cannot take it.
            Assert.Contains("type 11", Assert.Throws<InvalidOperationException>(() => new Invoicing.NumberTaken.InvoicingRepository(store)).Message);

            // A name changes nothing: the same number under the name of the page, in a higher version, is still a type of another Repository.
            Assert.Contains("type 11", Assert.Throws<InvalidOperationException>(() => new Invoicing.NameTaken.InvoicingRepository(store)).Message);

            // Nor does a definition: the board of a depot, strictly the same, in another Repository, is still one number for two models.
            Assert.Contains("type 12", Assert.Throws<InvalidOperationException>(() => new Invoicing.TypeTaken.InvoicingRepository(store)).Message);

            // Refused before anything is written: the Repositories of the Store go on.
            using (var transaction = site.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new DepotBoard { Depot = "Nice" };
                transaction.Commit();
            }

            // A number of its own, and the third Repository joins the Store.
            using var invoicing = new Invoicing.InvoicingRepository(store);
            using (var transaction = invoicing.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Invoicing.Invoice { Lines = 2 };
                transaction.Commit();
            }
            using (var transaction = invoicing.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(2, transaction.Enumerate<Invoicing.Invoice>().Single().Lines);
        }

        /// <summary>A number keeps its nature: a Record never takes the number of an entity, nor an entity the number of a Record.</summary>
        [Fact]
        public void RefuseANumberOfAnotherNature()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-nature", StoreAccessMode.ReadWrite);
            using var site = new TrackingRepository(store);

            // The number 12 is an entity, the board of a depot: a Record of the invoices cannot take it, in any version.
            Assert.Contains("type 12", Assert.Throws<InvalidOperationException>(() => new Invoicing.NatureTaken.InvoicingRepository(store)).Message);
        }

        /// <summary>An index number is one index in a Store: two Repositories number their indexes apart.</summary>
        [Fact]
        public void RefuseTheNumberOfAnIndexOfAnotherRepository()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-indexes", StoreAccessMode.ReadWrite);
            using var carrier = new CarrierRepository(store);

            // The index 1 finds a parcel by its tracking number (Lesson 7): an index of the invoices cannot take this number.
            Assert.Contains("number 1", Assert.Throws<InvalidOperationException>(() => new Invoicing.IndexTaken.InvoicingRepository(store)).Message);
        }

        /// <summary>Opened again, a Store keeps what each Repository wrote: they bind in any order, and the numbers of those that are not bound stay taken.</summary>
        [Fact]
        public void BindAgainInAnyOrder()
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-01-reopened");
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "shared", StoreAccessMode.ReadWrite))
            using (var carrier = new CarrierRepository(store))
            using (var site = new TrackingRepository(store))
            {
                using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    new Parcel(ParcelRef.New()) { TrackingNumber = "PX-0103", Recipient = "Ada", Depot = "Lyon" };
                    transaction.Commit();
                }
                using (var transaction = site.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    new DepotBoard { Depot = "Lyon", Parcels = 1 };
                    transaction.Commit();
                }
            }

            // The other order: each Repository finds its entities, and the carrier its index, which no bind of another Repository touches.
            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "shared", StoreAccessMode.ReadWrite))
            using (var site = new TrackingRepository(store))
            using (var carrier = new CarrierRepository(store))
            {
                using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                    Assert.Equal("Ada", ParcelTrackingIndex.Is("PX-0103").Single().Recipient);
                using (site.OpenTransaction(StoreAccessMode.ReadOnly))
                    Assert.Equal(1, DepotBoardViewCollection.Single().Parcels);
            }

            // The carrier alone: the tracking is not bound, and the Store remembers its numbers.
            using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "shared", StoreAccessMode.ReadWrite))
            using (var carrier = new CarrierRepository(store))
                Assert.Throws<InvalidOperationException>(() => new Invoicing.NumberTaken.InvoicingRepository(store));
        }
    }
}

// The invoices of the carrier: a third module, in a Repository of its own. Its type takes the free number 13. The models after it are the
// mistakes that a bind refuses, each one in a namespace of its own.
namespace TutorialEight.Invoicing
{
    [Entity(13, 1, 1), Root, Removable]
    internal interface MInvoice
    {
        // A count of lines names no value: it stays a number.
        [Property(1)]
        int Lines { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MInvoice))]
    internal interface MInvoicingRepository
    {
    }
}

namespace TutorialEight.Invoicing.NumberTaken
{
    // The number of the pages of the tracking.
    [Entity(11, 1, 1), Root, Removable]
    internal interface MInvoice
    {
        [Property(1)]
        int Lines { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MInvoice))]
    internal interface MInvoicingRepository
    {
    }
}

namespace TutorialEight.Invoicing.NameTaken
{
    // The number and the name of the pages of the tracking, in a higher version, the second number: another Repository all the same.
    [Entity(11, 2, 5), Root, Removable]
    internal interface MTrackingPage
    {
        [Property(5)]
        int Lines { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MTrackingPage))]
    internal interface MInvoicingRepository
    {
    }
}

namespace TutorialEight.Invoicing.TypeTaken
{
    // The board of a depot, strictly the same: one number for two models. Under a number of its own, it would be a type of its own.
    [ValueObject<GhostString>]
    internal interface VDepot { }

    [Entity(12, 1, 2), Root, Removable]
    internal interface MDepotBoard
    {
        [Property(1)]
        VDepot Depot { get; set; }

        [Property(2)]
        int Parcels { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MDepotBoard))]
    internal interface MInvoicingRepository
    {
    }
}

namespace TutorialEight.Invoicing.NatureTaken
{
    // A Record on the number of the boards of the depots, an entity.
    [Record(12, 1, 1)]
    internal interface RInvoiceLine
    {
        [Property(1)]
        int Quantity { get; set; }
    }

    [Entity(13, 1, 1), Root, Removable]
    internal interface MInvoice
    {
        [Property(1)]
        RInvoiceLine Line { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MInvoice)), IncludeRecordModel(nameof(RInvoiceLine))]
    internal interface MInvoicingRepository
    {
    }
}

namespace TutorialEight.Invoicing.IndexTaken
{
    [Entity(13, 1, 1), Root, Removable]
    internal interface MInvoice
    {
        [Property(1)]
        int Lines { get; set; }
    }

    // The number of the index of the tracking numbers, in the Repository of the parcels.
    [ValueIndex(nameof(MInvoice), 1), ValueIndexComponent(1, nameof(MInvoice.Lines))]
    internal interface MInvoiceLinesIndex
    {
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MInvoice))]
    internal interface MInvoicingRepository
    {
    }
}
