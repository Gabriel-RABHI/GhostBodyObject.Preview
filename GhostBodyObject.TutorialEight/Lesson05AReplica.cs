using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Distribution;

namespace TutorialEight.Carrier
{
    // The servers of the lessons of distribution, all in the process of the tests, on the loopback: each one an endpoint and its Stores,
    // in a directory of its own.
    internal static class Cluster
    {
        // An endpoint: it listens on a port that the system chooses, and joins the cluster through the endpoints given, its seeds.
        public static StoreDistributionEndPoint EndPoint(params StoreDistributionEndPoint[] seeds)
            => new(DistributionModel.P2P, new StoreDistributionEndPointConfiguration {
                ListenAddress = "127.0.0.1",
                Others = [.. seeds.Select(static seed => new DistributionEndPoint("127.0.0.1", seed.ListenPort))]
            });

        // The directory of a server, emptied at each run.
        public static string NewDirectory(string name)
        {
            string directory = Path.Combine(AppContext.BaseDirectory, name);
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
            return directory;
        }

        // Waits until the replica holds every transaction of the source: a copy takes the time it takes, the wait has no fixed delay.
        public static async Task CaughtUp(TransactionalMemoryStore replica, TransactionalMemoryStore source)
        {
            while (replica.CurrentEpoch < source.CurrentEpoch)
                await Task.Delay(10);
        }
    }

    /// <summary>
    /// Lesson 5: a replica.
    /// <para>
    /// A Store on a server is a <b>replica</b>: the same Store, the same name, on other servers, each in its own directory, each a whole
    /// copy of it. A <c>StoreDistributionEndPoint</c> connects the servers: it listens on a port, joins the cluster through one or more
    /// endpoints of it, its seeds, and discovers the others. <c>endPoint.Add(store)</c> distributes a Store, before its first transaction:
    /// each commit on one replica is copied to the others, in the order of the commits, and their Repositories read it.
    /// </para>
    /// <para>
    /// A replica opened <c>ReadOnly</c> only reads: a server of the queries, a report, a site, a cache, as many as needed. It never waits
    /// for the copy: its transactions read the last transaction it holds, <c>CurrentEpoch</c>, as a transaction of Tutorial One reads the
    /// last commit. The Repository of a replica reads the model that the writer wrote: it is opened once the replica holds it.
    /// </para>
    /// </summary>
    public sealed class Lesson05AReplica
    {
        /// <summary>The commits of a writer, copied to a replica on another server, which reads them.</summary>
        [Fact]
        public async Task CopyTheCommitsOfAWriterToAReplica()
        {
            // The writer: its endpoint; its Store on disk, distributed before its first transaction; its Repository.
            using var writerEndPoint = Cluster.EndPoint();
            using var writerStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-05-writer"), "parcels", StoreAccessMode.ReadWrite);
            writerEndPoint.Add(writerStore);
            using var writer = new CarrierRepository(writerStore);
            ParcelRef ada;
            using (var transaction = writer.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-4001", Recipient = "Ada", Depot = "" }.Ref;
                transaction.Commit();
            }

            // The replica: another server, joined through the writer; the same Store, ReadOnly, in a directory of its own.
            using var replicaEndPoint = Cluster.EndPoint(writerEndPoint);
            using var replicaStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-05-replica"), "parcels", StoreAccessMode.ReadOnly);
            replicaEndPoint.Add(replicaStore);
            await Cluster.CaughtUp(replicaStore, writerStore);
            using var replica = new CarrierRepository(replicaStore);
            using (replica.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Ada", ada.Resolve().Recipient);

            // Each commit of the writer reaches the replica.
            using (var transaction = writer.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada.Resolve().Status = ParcelStatus.InTransit;
                ada.Resolve().Depot = "Lyon";
                transaction.Commit();
            }
            await Cluster.CaughtUp(replicaStore, writerStore);
            using (replica.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Lyon", ada.Resolve().Depot);
        }

        /// <summary>A replica ReadOnly reads only; the write token of the Store is on the writer.</summary>
        [Fact]
        public async Task ReadOnlyOnAReadOnlyReplica()
        {
            using var writerEndPoint = Cluster.EndPoint();
            using var writerStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-05-only-writer"), "parcels", StoreAccessMode.ReadWrite);
            writerEndPoint.Add(writerStore);
            using var writer = new CarrierRepository(writerStore);
            using var replicaEndPoint = Cluster.EndPoint(writerEndPoint);
            using var replicaStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-05-only-replica"), "parcels", StoreAccessMode.ReadOnly);
            replicaEndPoint.Add(replicaStore);
            await Cluster.CaughtUp(replicaStore, writerStore);
            using var replica = new CarrierRepository(replicaStore);

            Assert.Throws<InvalidOperationException>(() => replica.OpenTransaction(StoreAccessMode.ReadWrite));

            // GetStatus: the write token of the Store, as an endpoint knows it. The writer holds it; the replica knows where it is.
            var status = replicaEndPoint.GetStatus(replicaStore);
            Assert.Equal((false, writerEndPoint.EndPointId), (status.IsWritable, status.OwnerEndPointId));
            Assert.True(writerEndPoint.GetStatus(writerStore).IsWritable);
        }
    }
}
