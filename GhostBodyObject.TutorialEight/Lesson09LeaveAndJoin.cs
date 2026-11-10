using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialEight.Carrier
{
    using static CarrierRepositoryCollections;

    /// <summary>
    /// Lesson 9: leave and join.
    /// <para>
    /// A server leaves the cluster by the disposal of its endpoint, after its Repositories and its Stores: its commits in progress end,
    /// the write token it holds goes to another writer, or is released, and it leaves once another writer holds its last commit. Nothing
    /// is lost, and the others go on: the next commit takes the token where it is.
    /// </para>
    /// <para>
    /// A server joins at any time, through any endpoint of the cluster, with an empty directory: its replica receives the whole Store,
    /// then each commit after it, as the others. Starting a new replica, a new reader or a new writer, is opening a Store.
    /// </para>
    /// </summary>
    public sealed class Lesson09LeaveAndJoin
    {
        /// <summary>A writer leaves with the token; a late replica joins through any member, and receives the whole Store.</summary>
        [Fact]
        public async Task GoOnWhenAWriterLeavesAndCopyTheStoreToAServerThatJoins()
        {
            using var officeEndPoint = Cluster.EndPoint();
            using var officeStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-09-office"), "parcels", StoreAccessMode.ReadWrite);
            officeEndPoint.Add(officeStore);
            using var office = new CarrierRepository(officeStore);
            Register(office, "PX-8001");

            // The depot writes, then leaves the cluster: its Repository, its Store, then its endpoint. Its token goes to another writer, or is released.
            ulong depotId;
            using (var depotEndPoint = Cluster.EndPoint(officeEndPoint))
            using (var depotStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-09-depot"), "parcels", StoreAccessMode.ReadWrite))
            {
                depotId = depotEndPoint.EndPointId;
                depotEndPoint.Add(depotStore);
                await Cluster.CaughtUp(depotStore, officeStore);
                using (var depot = new CarrierRepository(depotStore))
                {
                    Register(depot, "PX-8002");
                    Assert.True(depotEndPoint.GetStatus(depotStore).IsWritable);
                }
            }

            // The office goes on: it holds the last commit of the depot, and its next commit takes the token.
            Assert.DoesNotContain(depotId, officeEndPoint.KnownEndPoints);
            Register(office, "PX-8003");
            Assert.Equal(officeEndPoint.EndPointId, officeEndPoint.GetStatus(officeStore).OwnerEndPointId);

            // A site joins later, with an empty directory, through the office: its replica receives the whole Store.
            using var siteEndPoint = Cluster.EndPoint(officeEndPoint);
            using var siteStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-09-site"), "parcels", StoreAccessMode.ReadOnly);
            siteEndPoint.Add(siteStore);
            await Cluster.CaughtUp(siteStore, officeStore);
            using var site = new CarrierRepository(siteStore);
            using (site.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var numbers = new List<string>();
                foreach (var parcel in ParcelViewCollection)
                    numbers.Add(parcel.TrackingNumber);
                numbers.Sort(StringComparer.Ordinal);
                Assert.Equal(["PX-8001", "PX-8002", "PX-8003"], numbers);
            }
            Assert.Contains(siteEndPoint.EndPointId, officeEndPoint.KnownEndPoints);
        }

        private static void Register(CarrierRepository carrier, TrackingNumber trackingNumber)
        {
            using var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite);
            new Parcel(ParcelRef.New()) { TrackingNumber = trackingNumber, Recipient = "Ada", Depot = "" };
            transaction.Commit();
        }
    }
}
