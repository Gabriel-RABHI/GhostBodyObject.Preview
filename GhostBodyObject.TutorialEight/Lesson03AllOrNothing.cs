using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialEight.Carrier
{
    using static CarrierRepositoryCollections;
    using static TrackingRepositoryCollections;

    // A rule of the tracking (Tutorial Two, Lesson 16): a board never counts fewer than no parcel. It runs after the write, and condemns
    // the transaction when it throws (Tutorial Two, Lesson 17).
    internal sealed partial class DepotBoard
    {
        partial void OnParcelsChanged()
        {
            if (Parcels < 0)
                throw new InvalidOperationException($"The board of {Depot} counts {Parcels} parcels.");
        }
    }

    /// <summary>
    /// Lesson 3: all or nothing.
    /// <para>
    /// A transaction of a chain is one transaction, whatever the count of its Stores: what the code wrote in any of them is committed in
    /// all of them, or in none. A rollback writes nothing in any Store. A condemned transaction of any Store of the chain, by a rule of its
    /// model, refuses the commit of the whole chain. A conflict in any Store, two transactions that changed the same board, fails the
    /// commit of the whole chain, as a conflict of one Store fails its transaction in Tutorial One: the parcel of the transaction that
    /// failed is not changed either.
    /// </para>
    /// </summary>
    public sealed class Lesson03AllOrNothing
    {
        /// <summary>A transaction of the chain rolled back: nothing in any Store.</summary>
        [Fact]
        public void WriteNothingOfARollback()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-rollback", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-rollback-tracking", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            carrier.Chain(site);

            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var parcel = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-2001", Recipient = "Ada", Depot = "" };
                new TrackingPage(TrackingPageRef.Of(parcel.Ref)) { TrackingNumber = "PX-2001", Recipient = "Ada", Status = "Registered" };
                transaction.Rollback();
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal((0, 0), (ParcelViewCollection.Count(), TrackingPageViewCollection.Count()));
        }

        /// <summary>A rule of the tracking condemns its transaction: the commit of the chain is refused, and writes nothing.</summary>
        [Fact]
        public void RefuseTheCommitOfAChainCondemnedInOneStore()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-condemned", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-condemned-tracking", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            carrier.Chain(site);

            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-2002", Recipient = "Bob", Status = ParcelStatus.InTransit, Depot = "Lyon" };

                // A bug: the parcel leaves a depot whose board counts none. The rule throws after the write: the tracking is condemned.
                var rule = Assert.Throws<InvalidOperationException>(() => new DepotBoard { Depot = "Lyon" }.Parcels--);
                Assert.Same(rule, TrackingRepositoryTransaction.Current!.CondemnedBy);

                // The commit of the chain is refused, the rule given as its cause.
                Assert.Same(rule, Assert.Throws<InvalidOperationException>(transaction.Commit).InnerException);
            }
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal((0, 0), (ParcelViewCollection.Count(), DepotBoardViewCollection.Count()));
        }

        /// <summary>Two transactions change the same board, each for its own parcel: the second commit conflicts, and keeps neither change.</summary>
        [Fact]
        public async Task ConflictOnAnyStoreOfTheChain()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-conflict", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-conflict-tracking", StoreAccessMode.ReadWrite));
            using var carrier = new CarrierRepository(parcels);
            using var site = new TrackingRepository(tracking);
            carrier.Chain(site);
            ParcelRef first, second;
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                first = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-2003", Recipient = "Cy", Depot = "" }.Ref;
                second = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-2004", Recipient = "Di", Depot = "" }.Ref;
                new DepotBoard { Depot = "Lyon" };
                transaction.Commit();
            }

            // The first writer scans its parcel at Lyon, then waits; the second one scans its own parcel at Lyon, and commits first
            // (Tutorial One, Lesson 15).
            var written = new TaskCompletionSource();
            var committed = new TaskCompletionSource();
            var firstWriter = Task.Run(async () =>
            {
                using var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite);
                Arrive(first, "Lyon");
                written.SetResult();
                await committed.Task;
                return Assert.Throws<TransactionConflictException>(transaction.Commit);
            });
            await written.Task;
            await Task.Run(() =>
            {
                using var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite);
                Arrive(second, "Lyon");
                transaction.Commit();
            });
            committed.SetResult();
            await firstWriter;

            // The board changed since the first transaction opened: its commit failed, its parcel unchanged with it.
            using (carrier.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("", first.Resolve().Depot);
                Assert.Equal("Lyon", second.Resolve().Depot);
                Assert.Equal(1, DepotBoardViewCollection.Single(static board => board.Depot == "Lyon").Parcels);
            }
        }

        // A parcel arrives at a depot: the parcel, and the board of the depot, one more parcel there.
        private static void Arrive(ParcelRef parcel, Depot depot)
        {
            var body = parcel.Resolve();
            body.Status = ParcelStatus.InTransit;
            body.Depot = depot;
            DepotBoardCollection.Single(depot, static (board, depot) => board.Depot == depot).Parcels++;
        }
    }
}
