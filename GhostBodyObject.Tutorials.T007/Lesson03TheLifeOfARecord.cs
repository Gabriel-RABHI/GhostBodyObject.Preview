using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Journal
{
    using static ParcelLogCollections;

    /// <summary>
    /// Lesson 3: the life of a Record.
    /// <para>
    /// A Record lives in two times. <b>Created</b>, it is detached: it belongs to no transaction and to no Log yet, and it can be created
    /// anywhere, on any thread, outside any transaction, then written as many times as needed, as an object of C#. <b>Added</b>, by
    /// <c>Add()</c> in a transaction of its Log, it takes its place in that transaction, after the Records added before it: the order of
    /// the Log is the order of the Add, not of the creation.
    /// </para>
    /// <para>
    /// Once added, a Record is a fact: it is frozen, and a write of it is refused, in its transaction and after it. A Record is added
    /// once. Its View, the same View that a subscription delivers, is given once it is added: <c>Add().ToView()</c>.
    /// </para>
    /// </summary>
    public sealed class Lesson03TheLifeOfARecord
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>Records created on another thread, outside any transaction, then added: in the order of their Add.</summary>
        [Fact]
        public async Task CreateARecordAnywhereAndAddItLater()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-detached", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);

            // A scanner prepares its Records on a thread of its own, without any transaction: they are detached, and can be changed.
            var (lyon, paris) = await Task.Run(static () =>
            {
                var lyon = new ParcelScanned { TrackingNumber = "PX-3001", Depot = "Lyon", Scanned = Monday };
                var paris = new ParcelScanned { TrackingNumber = "PX-3001", Depot = "Pari", Scanned = Monday.AddHours(5) };
                paris.Depot = "Paris";
                return (lyon, paris);
            });

            // The transaction takes them in the order of their Add: Paris first here.
            using (var transaction = log.OpenTransaction())
            {
                Assert.Empty(ParcelCollection);
                paris.Add();
                lyon.Add();
                Assert.Equal<IParcelRecord>([paris, lyon], ParcelCollection.ToArray());
                transaction.Commit();
            }
            Assert.Equal(["Paris", "Lyon"], Depots(log));
        }

        /// <summary>An added Record is frozen: a write is refused, in its transaction and after it.</summary>
        [Fact]
        public void FreezeARecordOnceAdded()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-frozen", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            ParcelScanned scanned;
            using (var transaction = log.OpenTransaction())
            {
                scanned = new ParcelScanned { TrackingNumber = "PX-3002", Depot = "Lyon", Scanned = Monday };
                scanned.Depot = "Lille";
                scanned.Add();
                Assert.Throws<InvalidOperationException>(() => scanned.Depot = "Paris");
                transaction.Commit();
            }
            Assert.Throws<InvalidOperationException>(() => scanned.Depot = "Paris");

            // The Record keeps its values, and the Log holds the value written before its Add.
            Assert.Equal("Lille", scanned.Depot);
            Assert.Equal(["Lille"], Depots(log));
        }

        /// <summary>A Record is added once; its View is given once it is added; a Record never added is never in the Log.</summary>
        [Fact]
        public void AddARecordOnceAndReadItsView()
        {
            using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-view", StoreAccessMode.ReadWrite);
            using var log = new ParcelLog(store);
            using (var transaction = log.OpenTransaction())
            {
                // Detached: it has no View yet, and the commit leaves it out.
                var forgotten = new ParcelScanned { TrackingNumber = "PX-3003", Depot = "Nice", Scanned = Monday };
                Assert.Throws<InvalidOperationException>(() => forgotten.ToView());

                // Add returns the Record: its View at once, as a subscription would deliver it.
                var view = new ParcelScanned { TrackingNumber = "PX-3003", Depot = "Lyon", Scanned = Monday }.Add().ToView();
                Assert.Equal("Lyon", view.Depot);

                var added = ParcelCollection.ToArray().OfType<ParcelScanned>().Single();
                Assert.Throws<InvalidOperationException>(() => added.Add());
                transaction.Commit();
            }
            Assert.Equal(["Lyon"], Depots(log));
        }

        // The depots of the Records ParcelScanned of the Log, in its order.
        private static List<string> Depots(ParcelLog log)
        {
            var depots = new List<string>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelScannedView, List<string>>(depots, static (_, e, depots) =>
                {
                    depots.Add(e.Depot);
                    return LogStoreSubscription.Continue;
                })
                .Run();
            end.Cancel();
            return depots;
        }
    }
}
