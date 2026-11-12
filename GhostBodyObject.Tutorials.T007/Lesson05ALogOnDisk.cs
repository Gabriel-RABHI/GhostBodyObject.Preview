using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T007.Journal
{
    /// <summary>
    /// Lesson 5: a Log on disk.
    /// <para>
    /// <c>TransactionalMemoryStore.NewFileLogStore(directory, name)</c> opens a Log Store on disk, as <c>NewFileRepositoryStore</c> opens
    /// a Repository Store in Tutorial T001. A commit returns once its Records are written: a Log opened again, by this process or by
    /// another one, holds every transaction committed, and only them. Its new commits go after them.
    /// </para>
    /// <para>
    /// The Log is the memory of what happened, kept for good: a projection of it, a model, a screen, a report, can be lost, changed,
    /// rebuilt; the Log stays, and gives it again (Lessons 10 and 11).
    /// </para>
    /// </summary>
    public sealed class Lesson05ALogOnDisk
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);

        /// <summary>A Log closed, then opened again: its Records are there, and the new ones go after them.</summary>
        [Fact]
        public void KeepTheRecordsOverARestart()
        {
            // A directory of its own for the Log of the lesson, emptied at each run.
            string directory = Path.Combine(AppContext.BaseDirectory, "lesson-05-journal");
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);

            using (var store = TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite))
            using (var log = new ParcelLog(store))
            using (var transaction = log.OpenTransaction())
            {
                new ParcelRegistered { TrackingNumber = "PX-5001", Recipient = "Ada", Grams = 500, Registered = Monday }.Add();
                new ParcelScanned { TrackingNumber = "PX-5001", Depot = "Lyon", Scanned = Monday.AddHours(2) }.Add();
                transaction.Commit();
            }

            // The process ends there, or later: the Store opened again on the same directory finds the Log as it was committed.
            using (var store = TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite))
            using (var log = new ParcelLog(store))
            {
                Assert.Equal(["PX-5001 registered", "PX-5001 at Lyon"], Read(log));
                using (var transaction = log.OpenTransaction())
                {
                    new ParcelDelivered { TrackingNumber = "PX-5001", Delivered = Monday.AddDays(1) }.Add();
                    transaction.Commit();
                }
                Assert.Equal(["PX-5001 registered", "PX-5001 at Lyon", "PX-5001 delivered"], Read(log));
            }
        }

        private static List<string> Read(ParcelLog log)
        {
            var lines = new List<string>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token)
                .Handle<ParcelRegisteredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"{e.TrackingNumber} registered"))
                .Handle<ParcelScannedView, List<string>>(lines, static (_, e, lines) => Add(lines, $"{e.TrackingNumber} at {e.Depot}"))
                .Handle<ParcelDeliveredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"{e.TrackingNumber} delivered"))
                .Run();
            end.Cancel();
            return lines;
        }

        private static LogStoreSubscription Add(List<string> lines, string line)
        {
            lines.Add(line);
            return LogStoreSubscription.Continue;
        }
    }
}
