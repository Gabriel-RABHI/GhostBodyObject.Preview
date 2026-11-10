using System.Globalization;
using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialNine
{
    using static SalesRepositoryCollections;

    // The model of the whole tutorial: the sales of the concerts of a hall, which several tills change at the same time.

    // ----- The values of the model, first -----

    /// <summary>The title of a show or of a concert.</summary>
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>The name of a person or of a hall.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>An amount of money, added and subtracted, never below zero.</summary>
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;

        public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>A number of tickets, added and subtracted.</summary>
    [ValueObject<int>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VTickets { }

    /// <summary>A moment of the sales.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    /// <summary>The text of a notice on the door of a hall.</summary>
    [ValueObject<GhostString>]
    internal interface VMessage { }

    /// <summary>
    /// The other tills of the box office, for the lessons: a commit that falls between two instructions of a transaction. A transaction
    /// belongs to its execution context: each call runs on a thread of its own, in a context of its own, and the transaction of the caller
    /// stays open.
    /// </summary>
    internal static class AnotherTill
    {
        /// <summary>Runs <paramref name="work"/>, and waits for its end: its exception is thrown here.</summary>
        public static void Runs(Action work)
        {
            var end = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ExecutionContext.SuppressFlow())
            {
                new Thread(() =>
                {
                    try
                    {
                        work();
                        end.SetResult();
                    } catch (Exception failure)
                    {
                        end.SetException(failure);
                    }
                }).Start();
            }
            end.Task.GetAwaiter().GetResult();
        }

        /// <summary>Commits <paramref name="change"/> in a transaction of its own, and waits for its commit: the conflict of that commit is thrown here.</summary>
        public static void Commits(SalesRepository sales, Action change) => Runs(() =>
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            change();
            transaction.Commit();
        });
    }

    // ----- The entities -----

    /// <summary>A hall, which a concert designates.</summary>
    [Entity(1, 1, 1), Root, Enumerable]
    internal interface MHall
    {
        [Property(1)]
        VName Name { get; set; }
    }

    /// <summary>A show of the season: an ordinary entity, without any declaration of concurrency.</summary>
    [Entity(6, 1, 3), Root, Enumerable]
    internal interface MShow
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VTickets Sold { get; set; }

        [Property(3)]
        VName Manager { get; set; }
    }

    /// <summary>The sales of a concert. <c>[Mergeable]</c>: the concurrent changes of its values merge rather than conflict.</summary>
    [Entity(2, 1, 8), Root, Removable, Enumerable, Mergeable]
    internal interface MConcert
    {
        [Property(1)]
        VTitle Title { get; set; }

        // [Delta]: each transaction adds its own change to the last commit. Lesson 1.
        [Property(2), Delta]
        VTickets Sold { get; set; }

        [Property(3), Delta]
        VMoney Takings { get; set; }

        // [Max] and [Min]: the latest moment, the lowest price of the concurrent changes. Lesson 2.
        [Property(4), Max]
        VInstant LastSale { get; set; }

        [Property(5), Min]
        VMoney LowestPrice { get; set; }

        // A [Delta] that stays in its bounds: the tickets that remain never go below zero. Lesson 3.
        [Property(6), Delta(Min = 0)]
        VTickets Remaining { get; set; }

        // Values and links without a rule of merge. Lesson 4.
        [Property(7)]
        VName Manager { get; set; }

        [Property(8), Weak]
        Option<MHall> Hall { get; set; }
    }

    // The notices and the posters of Lessons 5 and 6 and the machines of Lesson 7 are declared with their lessons.
    [GhostRepository(1), IncludeEntityModel(nameof(MHall)), IncludeEntityModel(nameof(MShow)), IncludeEntityModel(nameof(MConcert)), IncludeEntityModel(nameof(MNotice)), IncludeEntityModel(nameof(MPoster)), IncludeEntityModel(nameof(MKiosk))]
    internal interface MSalesRepository
    {
    }

    /// <summary>
    /// Lesson 1: add the sales.
    /// <para>
    /// A sale changes the same entity as every other sale: on an ordinary entity, each till conflicts with all the others (Tutorial One,
    /// Lesson 16), and each conflict is a sale worked twice. Yet two sales do not contradict each other: both happened, and they add up. A type
    /// <c>[Mergeable]</c> says so. Its Setters stay ordinary Setters, and the transaction reads and writes as usual; at its commit, when another
    /// transaction changed the entity meanwhile, GBO merges the two changes rather than conflict.
    /// </para>
    /// <para>
    /// A merge has three points: the value at the opening of the transaction, its <b>epoch</b>; the value of the last commit, the <b>head</b>;
    /// and the <b>new</b> value of the transaction. <c>[Delta]</c> writes <c>head + (new - epoch)</c>: the last commit, and what the transaction
    /// changed. The value committed may differ from the value the transaction wrote; that is the point of it.
    /// </para>
    /// </summary>
    public sealed class Lesson01AddTheSales
    {
        private static readonly DateTime Opening = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

        private static void Open(SalesRepository sales)
        {
            using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
            new Show { Title = "Tosca", Sold = 10, Manager = "Iris" };
            new Concert { Title = "Carmen", Sold = 10, Takings = 400m, LastSale = Opening, LowestPrice = 40m, Remaining = 500, Manager = "Iris" };
            transaction.Commit();
        }

        private static Concert Carmen() => ConcertCollection.Single();

        /// <summary>On a show, an ordinary entity, two sales conflict: the later commit writes nothing, and its sale is lost until it runs again.</summary>
        [Fact]
        public void ConflictOnAnOrdinaryCounter()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-ordinary", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                ShowCollection.Single().Sold += 2;
                AnotherTill.Commits(sales, static () => ShowCollection.Single().Sold += 3);

                // TryCommit: the same commit as Commit(), its conflict given back as a value.
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Change, kind);
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(13, ShowCollection.Single().Sold);
        }

        /// <summary>Two tills sell at the same time: each one adds its sale to what the other one committed.</summary>
        [Fact]
        public void AddTheSalesOfTwoTills()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-add", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            using (var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The epoch of this transaction: 10 tickets. It writes 12.
                var carmen = Carmen();
                carmen.Sold += 2;
                carmen.Takings += 80m;

                // Another till commits 13 first: the head moves.
                AnotherTill.Commits(sales, static () =>
                {
                    var concert = Carmen();
                    concert.Sold += 3;
                    concert.Takings += 120m;
                });

                // The transaction still reads its own version: its 12.
                Assert.Equal(12, carmen.Sold);

                // No conflict: the commit writes head + (new - epoch), 13 + (12 - 10).
                transaction.Commit();
            }
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(15, Carmen().Sold);
                Assert.Equal(600m, Carmen().Takings);
            }
        }

        /// <summary>Four tills on a <c>[Delta]</c>: every sale counted, no conflict, nothing run again.</summary>
        [Fact]
        public async Task SellFromFourTillsWithoutAConflict()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-four", StoreAccessMode.ReadWrite);
            using var sales = new SalesRepository(store);
            Open(sales);
            int conflicts = 0;
            var tills = Enumerable.Range(0, 4).Select(till => Task.Run(() =>
            {
                for (int sale = 0; sale < 250; sale++)
                {
                    using var transaction = sales.OpenTransaction(StoreAccessMode.ReadWrite);
                    var carmen = Carmen();
                    carmen.Sold += 1;
                    carmen.Takings += 40m;
                    if (!transaction.TryCommit(out _))
                        Interlocked.Increment(ref conflicts);
                }
            }));
            await Task.WhenAll(tills);
            Assert.Equal(0, conflicts);
            using (sales.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(1_010, Carmen().Sold);
                Assert.Equal(40_400m, Carmen().Takings);
            }
        }
    }
}
