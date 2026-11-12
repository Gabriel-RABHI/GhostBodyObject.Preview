using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T008.Carrier
{
    using static TrackingRepositoryCollections;

    /// <summary>
    /// Lesson 8: distribute a chain, and a part of it.
    /// <para>
    /// <c>endPoint.Add(parcels)</c>, on the first Store of a chain, distributes the whole chain: each commit of the chain is copied with
    /// its three Stores, the parcels, the tracking and the Log, together. The other servers take what they need of it. A writer takes
    /// the whole chain, ReadWrite, and runs the commands. The site of the tracking takes the Store of the tracking alone, ReadOnly: its
    /// pages and its boards, nothing of the parcels nor of the Log. A service of notifications takes the Log alone, ReadOnly, and follows
    /// it by a subscription (Tutorial T007): the commits of the writer reach it as they are copied.
    /// </para>
    /// <para>
    /// Each server holds the Stores it needs, and the cluster copies to each one the transactions of its Stores only: a front end of a
    /// million readers never holds the model it does not read.
    /// </para>
    /// </summary>
    public sealed class Lesson08DistributeAChain
    {
        /// <summary>A writer of the whole chain; a site of the tracking alone; a service that follows the Log alone.</summary>
        [Fact]
        public async Task CopyEachStoreOfTheChainToTheServersThatHoldIt()
        {
            // The writer: the chain of the three Stores, in its directory, distributed by its first Store.
            string writerDirectory = Cluster.NewDirectory("lesson-08-writer");
            using var writerEndPoint = Cluster.EndPoint();
            using var parcels = TransactionalMemoryStore.NewFileRepositoryStore(writerDirectory, "parcels", StoreAccessMode.ReadWrite);
            using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewFileRepositoryStore(writerDirectory, "tracking", StoreAccessMode.ReadWrite));
            using var journal = tracking.AddChildStore(TransactionalMemoryStore.NewFileLogStore(writerDirectory, "journal", StoreAccessMode.ReadWrite));
            writerEndPoint.Add(parcels);
            using var carrier = new CarrierRepository(parcels);
            using var trackingModel = new TrackingRepository(tracking);
            using var log = new CarrierLog(journal);
            carrier.Chain(trackingModel);
            trackingModel.Chain(log);
            ParcelRef ada;
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ada = Parcels.Register("PX-7001", "Ada");
                Parcels.Scan(ada, "Lyon");
                transaction.Commit();
            }

            // The site: the Store of the tracking alone, ReadOnly, its Repository alone.
            using var siteEndPoint = Cluster.EndPoint(writerEndPoint);
            using var siteTracking = TransactionalMemoryStore.NewFileRepositoryStore(Cluster.NewDirectory("lesson-08-site"), "tracking", StoreAccessMode.ReadOnly);
            siteEndPoint.Add(siteTracking);
            await Cluster.CaughtUp(siteTracking, tracking);
            using var site = new TrackingRepository(siteTracking);
            using (site.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("In transit at Lyon", TrackingPageRef.Of(ada).Resolve().Status);

            // The notifications: the Log alone, ReadOnly, followed by a subscription from its start.
            using var notifierEndPoint = Cluster.EndPoint(writerEndPoint);
            using var notifierJournal = TransactionalMemoryStore.NewFileLogStore(Cluster.NewDirectory("lesson-08-notifier"), "journal", StoreAccessMode.ReadOnly);
            notifierEndPoint.Add(notifierJournal);
            using var notifierLog = new CarrierLog(notifierJournal);
            var notices = new List<string>();
            using var end = new CancellationTokenSource();
            var subscription = notifierLog.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, List<string>>(notices, static (_, e, notices) => Notify(notices, $"Your parcel is at {e.Depot}"))
                .Handle<ParcelDeliveredView, List<string>>(notices, static (_, _, notices) => Notify(notices, "Your parcel is delivered"))
                .Run();

            // The next command of the writer reaches the site and the notifications.
            using (var transaction = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Parcels.Deliver(ada);
                transaction.Commit();
            }
            await Cluster.CaughtUp(siteTracking, tracking);
            using (site.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Delivered", TrackingPageRef.Of(ada).Resolve().Status);
                Assert.Equal(0, DepotBoardViewCollection.Single(static board => board.Depot == "Lyon").Parcels);
            }
            await Cluster.CaughtUp(notifierJournal, journal);
            notifierLog.Store.DeliverTransactions(subscription);
            Assert.Equal(["Your parcel is at Lyon", "Your parcel is delivered"], notices);
            end.Cancel();
        }

        private static LogStoreSubscription Notify(List<string> notices, string notice)
        {
            notices.Add(notice);
            return LogStoreSubscription.Continue;
        }
    }
}
