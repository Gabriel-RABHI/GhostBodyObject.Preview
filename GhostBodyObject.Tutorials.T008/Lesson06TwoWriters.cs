using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T008.Carrier
{
    /// <summary>
    /// Lesson 6: two writers.
    /// <para>
    /// Replicas opened <c>ReadWrite</c> on several servers all write: a depot writes its scans, the back office its registrations, each
    /// on its own replica, with its own Repository, and the code is the code of Tutorial T001. Their commits form one sequence, the same on
    /// every replica: a Store has one <b>write token</b> in the cluster, and a commit takes it first. The token goes to the replica that
    /// commits, once it holds every transaction before: its commit follows them, and the others receive it.
    /// </para>
    /// <para>
    /// The token stays with its last writer until another one commits: a server that writes alone keeps it, and commits without any
    /// exchange. <c>GetStatus</c> tells where it is, and <c>TokenGeneration</c> counts its moves. A transaction opens on the last commit
    /// its replica holds, as in Tutorial T001: a server that changes what another one just changed waits for that commit first, or
    /// conflicts (Lesson 7).
    /// </para>
    /// </summary>
    public sealed class Lesson06TwoWriters
    {
        /// <summary>Two servers write the same Store in turn: the token goes to each one at its commit; both hold every commit.</summary>
        [Fact]
        public async Task MoveTheWriteTokenBetweenTwoWriters()
        {
            using var officeEndPoint = Cluster.EndPoint();
            using var officeStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-06-office"), "parcels", StoreAccessMode.ReadWrite);
            officeEndPoint.Add(officeStore);
            using var office = new CarrierRepository(officeStore);
            ParcelRef ada;
            using (var transaction = office.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada = new Parcel(ParcelRef.New()) { TrackingNumber = "PX-5001", Recipient = "Ada", Depot = "" }.Ref;
                transaction.Commit();
            }

            // The depot: a second writer of the same Store.
            using var depotEndPoint = Cluster.EndPoint(officeEndPoint);
            using var depotStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-06-depot"), "parcels", StoreAccessMode.ReadWrite);
            depotEndPoint.Add(depotStore);
            await Cluster.CaughtUp(depotStore, officeStore);
            using var depot = new CarrierRepository(depotStore);
            ulong generation = officeEndPoint.GetStatus(officeStore).TokenGeneration;

            // The depot scans the parcel: its commit takes the token.
            using (var transaction = depot.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada.Resolve().Status = ParcelStatus.InTransit;
                ada.Resolve().Depot = "Lyon";
                transaction.Commit();
            }
            Assert.True(depotEndPoint.GetStatus(depotStore).IsWritable);

            // The office changes the recipient. Its transaction opens on the last commit its replica holds: it waits for the one of the
            // depot first, or it would change the parcel as it was before it, and conflict (Lesson 7). Its commit takes the token back.
            await Cluster.CaughtUp(officeStore, depotStore);
            using (var transaction = office.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada.Resolve().Recipient = "Ada Lovelace";
                transaction.Commit();
            }
            Assert.True(officeEndPoint.GetStatus(officeStore).IsWritable);
            Assert.True(officeEndPoint.GetStatus(officeStore).TokenGeneration >= generation + 2);

            // Both replicas hold both commits, in the same order.
            await Cluster.CaughtUp(depotStore, officeStore);
            Assert.Equal(officeStore.CurrentEpoch, depotStore.CurrentEpoch);
            foreach (var repository in new[] { office, depot })
                using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
                {
                    Assert.Equal("Ada Lovelace", ada.Resolve().Recipient);
                    Assert.Equal("Lyon", ada.Resolve().Depot);
                }
        }

        /// <summary>Two servers commit at the same time, many times: one sequence of commits, every one of them kept.</summary>
        [Fact]
        public async Task KeepEveryCommitOfWritersInParallel()
        {
            using var officeEndPoint = Cluster.EndPoint();
            using var officeStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-06-parallel-office"), "parcels", StoreAccessMode.ReadWrite);
            officeEndPoint.Add(officeStore);
            using var office = new CarrierRepository(officeStore);
            using var depotEndPoint = Cluster.EndPoint(officeEndPoint);
            using var depotStore = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-06-parallel-depot"), "parcels", StoreAccessMode.ReadWrite);
            depotEndPoint.Add(depotStore);
            await Cluster.CaughtUp(depotStore, officeStore);
            using var depot = new CarrierRepository(depotStore);

            // Each server registers its own parcels, one commit each, at the same time as the other one.
            const int Parcels = 20;
            await Task.WhenAll(Task.Run(() => Register(office, "PX-51", Parcels)), Task.Run(() => Register(depot, "PX-52", Parcels)));

            await Cluster.CaughtUp(depotStore, officeStore);
            await Cluster.CaughtUp(officeStore, depotStore);
            foreach (var repository in new[] { office, depot })
                using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
                    Assert.Equal(2 * Parcels, CarrierRepositoryCollections.ParcelViewCollection.Count());
        }

        private static void Register(CarrierRepository carrier, string prefix, int count)
        {
            for (int i = 0; i < count; i++)
            {
                using var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite);
                new Parcel(ParcelRef.New()) { TrackingNumber = $"{prefix}{i:D3}", Recipient = "Bob", Depot = "" };
                transaction.Commit();
            }
        }
    }
}
