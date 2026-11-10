using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialEight.Carrier
{
    // A tracking number belongs to one parcel, in the whole cluster (Tutorial Five, Lesson 9).
    [ValueIndex(nameof(MParcel), 1, Unique = true), ValueIndexComponent(1, nameof(MParcel.TrackingNumber))]
    internal interface MParcelTrackingIndex
    {
    }

    /// <summary>
    /// Lesson 7: conflicts across servers.
    /// <para>
    /// Two servers are two writers, as two threads of Tutorial One: their transactions run at the same time, each on the last commit its
    /// replica held at its opening. The first commit wins; the second one finds what changed since its opening, on any server, and
    /// conflicts: a <c>TransactionConflictException</c>, its <c>Kind</c> telling what. It writes nothing, on any replica; the application
    /// decides, with a new transaction.
    /// </para>
    /// <para>
    /// A unique value is unique in the cluster: two servers that give the same tracking number to two parcels at the same time do not
    /// see each other's parcel, and both Setters accept it; the second commit conflicts, of the kind <c>Uniqueness</c>. A unique index
    /// guarantees what must be unique, whatever the count of the writers.
    /// </para>
    /// </summary>
    public sealed class Lesson07ConflictsAcrossServers
    {
        /// <summary>The same parcel changed on two servers at the same time: the second commit conflicts.</summary>
        [Fact]
        public async Task ConflictOnAParcelChangedOnAnotherServer()
        {
            using var officeEndPoint = Cluster.EndPoint();
            using var officeStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-07-office"), "parcels", StoreAccessMode.ReadWrite);
            officeEndPoint.Add(officeStore);
            using var office = new CarrierRepository(officeStore);
            ParcelRef ada;
            using (var transaction = office.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-6001", Recipient = "Ada", Depot = "" }.Ref;
                transaction.Commit();
            }
            using var depotEndPoint = Cluster.EndPoint(officeEndPoint);
            using var depotStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-07-depot"), "parcels", StoreAccessMode.ReadWrite);
            depotEndPoint.Add(depotStore);
            await Cluster.CaughtUp(depotStore, officeStore);
            using var depot = new CarrierRepository(depotStore);

            // The office changes the recipient, then waits; the depot scans the parcel and commits first.
            var written = new TaskCompletionSource();
            var committed = new TaskCompletionSource();
            var officeWriter = Task.Run(async () =>
            {
                using var transaction = office.OpenTransaction(StoreAccessMode.ReadWrite);
                ada.Resolve().Recipient = "Ada Lovelace";
                written.SetResult();
                await committed.Task;
                return Assert.Throws<TransactionConflictException>(transaction.Commit).Kind;
            });
            await written.Task;
            await Task.Run(() =>
            {
                using var transaction = depot.OpenTransaction(StoreAccessMode.ReadWrite);
                ada.Resolve().Status = ParcelStatus.InTransit;
                ada.Resolve().Depot = "Lyon";
                transaction.Commit();
            });
            committed.SetResult();
            Assert.Equal(TransactionConflictKind.Change, await officeWriter);

            // The commit of the depot is kept on both servers; the change of the office is on none.
            await Cluster.CaughtUp(officeStore, depotStore);
            using (office.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Ada", ada.Resolve().Recipient);
                Assert.Equal("Lyon", ada.Resolve().Depot);
            }
        }

        /// <summary>The same tracking number given on two servers at the same time: the second commit conflicts, of the kind Uniqueness.</summary>
        [Fact]
        public async Task RefuseAUniqueValueTakenOnAnotherServer()
        {
            using var officeEndPoint = Cluster.EndPoint();
            using var officeStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-07-unique-office"), "parcels", StoreAccessMode.ReadWrite);
            officeEndPoint.Add(officeStore);
            using var office = new CarrierRepository(officeStore);
            using var depotEndPoint = Cluster.EndPoint(officeEndPoint);
            using var depotStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-07-unique-depot"), "parcels", StoreAccessMode.ReadWrite);
            depotEndPoint.Add(depotStore);
            await Cluster.CaughtUp(depotStore, officeStore);
            using var depot = new CarrierRepository(depotStore);

            var written = new TaskCompletionSource();
            var committed = new TaskCompletionSource();
            var officeWriter = Task.Run(async () =>
            {
                using var transaction = office.OpenTransaction(StoreAccessMode.ReadWrite);
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-6002", Recipient = "Ada", Depot = "" };
                written.SetResult();
                await committed.Task;
                return Assert.Throws<TransactionConflictException>(transaction.Commit).Kind;
            });
            await written.Task;
            await Task.Run(() =>
            {
                using var transaction = depot.OpenTransaction(StoreAccessMode.ReadWrite);
                new Parcel(ParcelRef.New()) { TrackingNumber = "PX-6002", Recipient = "Bob", Depot = "" };
                transaction.Commit();
            });
            committed.SetResult();
            Assert.Equal(TransactionConflictKind.Uniqueness, await officeWriter);

            // One parcel of that number, the one of the depot, on both servers.
            await Cluster.CaughtUp(officeStore, depotStore);
            using (office.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Bob", ParcelTrackingIndex.Is("PX-6002").Single().Recipient);
        }
    }
}
