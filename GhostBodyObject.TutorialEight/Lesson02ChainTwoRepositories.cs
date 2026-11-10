using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialEight.Carrier
{
    using static CarrierRepositoryCollections;
    using static TrackingRepositoryCollections;

    /// <summary>
    /// Lesson 2: chain two Repositories.
    /// <para>
    /// Two Repositories take two transactions, on one Store (Lesson 1) or on two: a parcel changed and its page not yet, or the page and not
    /// the parcel, when one of them fails. A <b>chain</b> of Stores makes them one: each model takes its Store, <c>parcels.AddChildStore(tracking)</c>
    /// chains the Store of the tracking after the Store of the parcels, before their first transaction, then
    /// <c>carrier.Chain(trackingRepository)</c> chains the Repositories.
    /// </para>
    /// <para>
    /// Each transaction of the first Repository of the chain then opens a transaction of the second one, ambient, in the same mode:
    /// <c>TrackingRepositoryTransaction.Current</c>. The code writes both models as one, and the commit writes both Stores together, or
    /// neither (Lesson 3). The second Repository has no transaction of its own: it opens and commits with the first one.
    /// </para>
    /// </summary>
    public sealed class Lesson02ChainTwoRepositories
    {
        /// <summary>A parcel and its tracking page, in two Stores, in one transaction.</summary>
        [Fact]
        public void WriteTwoModelsInOneTransaction()
        {
            // The Stores, chained before their first transaction; then their Repositories, chained in the same order.
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-parcels", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-tracking", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            carrier.Chain(site);

            ParcelRef parcel;
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The transaction of the tracking, opened with the one of the parcels: both models are written in it.
                Assert.NotNull(TrackingRepositoryTransaction.Current);
                parcel = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-1001", Recipient = "Ada", Depot = "" }.Ref;

                // The page of the parcel takes an identity derived from the one of the parcel: found from it, in any Store.
                new TrackingPage(TrackingPageRef.Of(parcel)) { TrackingNumber = "PX-1001", Recipient = "Ada", Status = "Registered" };
                transaction.Commit();
            }

            // A ReadOnly transaction of the chain reads both models: the parcel, and its page from the identity of the parcel.
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("PX-1001", parcel.Resolve().TrackingNumber);
                Assert.Equal("Registered", TrackingPageRef.Of(parcel).Resolve().Status);
                Assert.Equal(1, TrackingPageViewCollection.Count());
            }
        }

        /// <summary>The second Repository of a chain opens with the first one only, and has no commit of its own.</summary>
        [Fact]
        public void OpenTheSecondRepositoryWithTheFirstOnly()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-first", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-second", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            carrier.Chain(site);

            // Alone, the tracking would be read or written apart from the parcels: refused.
            Assert.Throws<InvalidOperationException>(() => site.OpenTransaction(StoreAccessMode.ReadOnly));
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new DepotBoard { Depot = "Lyon" };
                Assert.Throws<InvalidOperationException>(() => TrackingRepositoryTransaction.Current!.Commit());
                transaction.Commit();
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Lyon", DepotBoardViewCollection.Single(static board => board.Parcels == 0).Depot);
        }

        /// <summary>The Stores are chained before the Repositories, and a Repository is chained once.</summary>
        [Fact]
        public void ChainTheStoresFirst()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-apart", StoreAccessMode.ReadWrite);
            using var tracking = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-apart-tracking", StoreAccessMode.ReadWrite);
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);

            // The Stores are not chained: the Repositories cannot be.
            Assert.Throws<InvalidOperationException>(() => carrier.Chain(site));

            // Apart, each Repository has its own transactions, its own commits: the Store of the parcels knows nothing of the other.
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Null(TrackingRepositoryTransaction.Current);
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-1002", Recipient = "Bob", Depot = "" };
                transaction.Commit();
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, ParcelViewCollection.Count());
        }
    }
}
