using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSeven.Carrier
{
    using static CarrierRepositoryCollections;

    /// <summary>
    /// Lesson 11: rebuild a model.
    /// <para>
    /// A model on disk and its Log on disk: opened again, the model is there, ready, without a replay. It is rebuilt on request only:
    /// <c>RebuildFromLog()</c> on the Store of the model, once the Stores are chained and before the first transaction, empties the
    /// model and keeps the Log; the model is then behind its Log, and the replay of Lesson 10 brings it back, through the handlers as
    /// they are now. A handler changed, a projection added, a model damaged: the Log gives the model again.
    /// </para>
    /// <para>
    /// The Log holds the Records, not the model: a change that no Record tells is lost at a rebuild. In a model rebuilt from its Log,
    /// every change comes from a handler of a Record, and from nothing else.
    /// </para>
    /// </summary>
    public sealed class Lesson11RebuildAModel
    {
        /// <summary>A model on disk, rebuilt from its Log on request: the change that no Record tells is gone.</summary>
        [Fact]
        public void RebuildAModelOnDiskOnRequest()
        {
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-11-carrier");
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            List<string> expected;
            using (var chain = new FileChain(directory, rebuild: false))
            {
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    var ada = Parcels.Register("PX-11001", "Ada");
                    Parcels.Scan(ada, "Lyon");
                    transaction.Commit();
                }
                expected = Parcels.Snapshot(chain.Repository);
            }

            // Opened again: the model is on disk, ready. A transaction changes a parcel without a Record: the Log does not know it.
            using (var chain = new FileChain(directory, rebuild: false))
            {
                Assert.False(chain.Parcels.IsBehindLog);
                Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    ParcelCollection.Single(static parcel => parcel.TrackingNumber == "PX-11001").Depot = "Nowhere";
                    transaction.Commit();
                }
                Assert.Equal(["PX-11001 InTransit Nowhere 1"], Parcels.Snapshot(chain.Repository));
            }

            // Rebuilt: the model empty, behind its Log, then replayed. The parcel is where its Records say it is.
            using (var chain = new FileChain(directory, rebuild: true))
            {
                Assert.True(chain.Parcels.IsBehindLog);
                Assert.Empty(Parcels.Snapshot(chain.Repository));
                Replay.Run(chain.Repository, chain.Log, group: 1000);
                Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
            }

            // The rebuilt model is on disk: the next opening finds it ready.
            using (var chain = new FileChain(directory, rebuild: false))
            {
                Assert.False(chain.Parcels.IsBehindLog);
                Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
            }
        }

        // The chain of the lesson: the parcels on disk, their Log on disk after them; rebuilt from the Log when asked.
        private sealed class FileChain : IDisposable
        {
            public FileChain(string directory, bool rebuild)
            {
                Parcels = TransactionalMemoryStore.NewFileRepositoryStore(directory, "parcels", StoreAccessMode.ReadWrite);
                Journal = Parcels.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
                if (rebuild)
                    Parcels.RebuildFromLog();
                Repository = new CarrierRepository(Parcels);
                Log = new CarrierLog(Journal);
                Repository.Chain(Log);
            }

            public TransactionalMemoryStore Parcels { get; }

            public TransactionalMemoryStore Journal { get; }

            public CarrierRepository Repository { get; }

            public CarrierLog Log { get; }

            public void Dispose()
            {
                Log.Dispose();
                Repository.Dispose();
                Parcels.Dispose();
                Journal.Dispose();
            }
        }
    }
}
