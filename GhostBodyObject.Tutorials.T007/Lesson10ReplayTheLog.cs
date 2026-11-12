using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Carrier
{
    // The replay of the Log into the model behind it: the same handlers as the commands, in Replay transactions, one for each `group`
    // transactions of the Log, each committed through the last transaction of the Log it holds. Lessons 10 and 11.
    internal sealed class Replay(CarrierRepository repository, int group)
    {
        private CarrierRepositoryTransaction? _transaction;
        private int _delivered;

        // The Replay transactions committed.
        public int Commits { get; private set; }

        public static Replay Run(CarrierRepository repository, CarrierLog log, int group)
        {
            var replay = new Replay(repository, group);
            log.SubscribeFrom(0)
                .OnTransactionStart(replay, static (_, replay) => replay.Start())
                .Handle<ParcelRegisteredView>(static (_, e) => Parcels.Apply(e))
                .Handle<ParcelScannedView>(static (_, e) => Parcels.Apply(e))
                .Handle<ParcelDeliveredView>(static (_, e) => Parcels.Apply(e))
                .OnTransactionEnd(replay, static (transaction, replay) => replay.End(transaction))
                .OnReplayEnd(replay, static (last, replay) => replay.Finish(last))
                .Run();
            return replay;
        }

        // A transaction of the Log starts: a Replay transaction is open for it, ambient, the one of the transactions before or a new one.
        private LogStoreSubscription Start()
        {
            _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
            return LogStoreSubscription.Continue;
        }

        private LogStoreSubscription End(ulong transaction)
        {
            if (++_delivered % group == 0)
                Commit(transaction);
            return LogStoreSubscription.Continue;
        }

        // The end of the Log: the model commits through its last transaction, even one without Records, never delivered; it is then
        // ready, and the subscription ends.
        private LogStoreSubscription Finish(ulong last)
        {
            if (_transaction != null || repository.Store.IsBehindLog)
            {
                _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
                Commit(last);
            }
            return LogStoreSubscription.Cancel;
        }

        private void Commit(ulong through)
        {
            _transaction!.Commit(through);
            _transaction = null;
            Commits++;
        }
    }

    /// <summary>
    /// Lesson 10: replay the Log.
    /// <para>
    /// The Log holds everything the model is made of: the model can live in memory only, and the Log on disk. At each opening, the
    /// model is empty, and <b>behind its Log</b>: <c>IsBehindLog</c>, until it reaches <c>LogTarget</c>, the last transaction of the Log.
    /// A transaction of the commands is refused meanwhile: it would change a model that is not there yet.
    /// </para>
    /// <para>
    /// A <b>Replay</b> brings the model there: a subscription from the start of the Log gives each Record to the same handler as its
    /// command, in a Replay transaction, <c>OpenTransaction(StoreAccessMode.ReadWrite, replay: true)</c>, which opens no transaction of
    /// the Log: the Records are there already. Its commit, <c>Commit(through)</c>, says which transaction of the Log the model reached; a
    /// Replay transaction may hold many of them, for fewer commits. The one through the last transaction of the Log makes the model
    /// ready: the commands go on.
    /// </para>
    /// <para>
    /// A replay gives the same model each time, as the handlers read the values of the Records only. <c>ParcelRef.New()</c> is refused
    /// in a Replay transaction: a new identity would differ at each replay; the identities come from the Records, drawn once by the
    /// commands (Lesson 9).
    /// </para>
    /// </summary>
    public sealed class Lesson10ReplayTheLog
    {
        /// <summary>A model in memory, its Log on disk: replayed at each opening, by transactions of the Log alone or by groups of them.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void RebuildAModelInMemoryAtEachOpening(int group)
        {
            string directory = Path.Combine(AppContext.BaseDirectory, $"lesson-10-journal-{group}");
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            // The first opening: an empty Log, an empty model. Six commands, in four commits.
            List<string> expected;
            using (var chain = new InMemoryChain(directory))
            {
                ParcelRef ada, bob;
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    ada = Parcels.Register("PX-10001", "Ada");
                    transaction.Commit();
                }
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    bob = Parcels.Register("PX-10002", "Bob");
                    transaction.Commit();
                }
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    Parcels.Scan(ada, "Lyon");
                    Parcels.Scan(bob, "Lyon");
                    transaction.Commit();
                }
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    Parcels.Scan(ada, "Paris");
                    Parcels.Deliver(bob);
                    transaction.Commit();
                }
                expected = Parcels.Snapshot(chain.Repository);
            }
            Assert.Equal(["PX-10001 InTransit Paris 2", "PX-10002 Delivered Lyon 1"], expected);

            // The second opening: the model is empty, behind its Log; the commands wait for the replay.
            using (var chain = new InMemoryChain(directory))
            {
                Assert.True(chain.Parcels.IsBehindLog);
                Assert.True(chain.Parcels.LogTarget > 0);
                Assert.Empty(Parcels.Snapshot(chain.Repository));
                Assert.Throws<InvalidOperationException>(() => chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite));

                // The replay: four Replay commits for four transactions of the Log, or one for all of them.
                var replay = Replay.Run(chain.Repository, chain.Log, group);
                Assert.Equal(group == 1 ? 4 : 1, replay.Commits);
                Assert.False(chain.Parcels.IsBehindLog);
                Assert.Equal(expected, Parcels.Snapshot(chain.Repository));

                // Ready: the commands go on, in the model and in the Log.
                using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
                {
                    Parcels.Register("PX-10003", "Cy");
                    transaction.Commit();
                }
                Assert.Equal("PX-10003 Registered  0", Parcels.Snapshot(chain.Repository)[^1]);
                Assert.Equal((7, "registered PX-10003"), (Parcels.Read(chain.Log).Count, Parcels.Read(chain.Log)[^1]));
            }
        }

        /// <summary>A Replay transaction takes its identities from the Records: a new one is refused.</summary>
        [Fact]
        public void RefuseANewIdentityInAReplay()
        {
            using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-identity", StoreAccessMode.ReadWrite);
            using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-10-identity-journal", StoreAccessMode.ReadWrite));
            using var repository = new CarrierRepository(parcels);
            using var log = new CarrierLog(journal);
            repository.Chain(log);
            using var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
            Assert.Null(CarrierLogTransaction.Current);
            Assert.Throws<InvalidOperationException>(() => ParcelRef.New());
        }

        // The chain of the lesson: the parcels in memory, and their Log on disk, after them.
        private sealed class InMemoryChain : IDisposable
        {
            public InMemoryChain(string directory)
            {
                Parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-parcels", StoreAccessMode.ReadWrite);
                Journal = Parcels.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
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
