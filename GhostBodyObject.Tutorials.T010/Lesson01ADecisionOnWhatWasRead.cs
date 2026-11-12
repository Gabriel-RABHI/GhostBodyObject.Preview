using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T010
{
    using static BoxOfficeRepositoryCollections;

    // The model of the whole tutorial: the box office of a concert hall, where several tills work at the same time.

    // ----- The values of the model, first -----

    /// <summary>The name of a person or of a hall.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>The title of a concert.</summary>
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>The reference of an order.</summary>
    [ValueObject<GhostString>]
    internal interface VReference { }

    /// <summary>A number of tickets, added and subtracted.</summary>
    [ValueObject<int>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VTickets { }

    /// <summary>A count of visits, added and subtracted.</summary>
    [ValueObject<int>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VVisits { }

    /// <summary>The number of a seat.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VSeatNumber { }

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
        public static void Commits(BoxOfficeRepository boxOffice, Action change) => Runs(() =>
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            change();
            transaction.Commit();
        });
    }

    // ----- The entities of the claims -----

    /// <summary>Whether an usher works tonight: a state, an enum, not a bool.</summary>
    internal enum Duty
    {
        Off,
        On
    }

    /// <summary>A hall, and the ushers who work in it, its parts.</summary>
    [Entity(1, 1, 2), Root, Enumerable]
    internal interface MHall
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MUsher> Ushers { get; }
    }

    [Entity(2, 1, 2)]
    internal interface MUsher
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        Duty Duty { get; set; }
    }

    // The orders of Lesson 3 and the seats of Lesson 6 are declared with their lessons.
    [GhostRepository(1), IncludeEntityModel(nameof(MHall)), IncludeEntityModel(nameof(MUsher)), IncludeEntityModel(nameof(MLine)), IncludeEntityModel(nameof(MBasket)),
        IncludeEntityModel(nameof(MCustomer)), IncludeEntityModel(nameof(MOrder)), IncludeEntityModel(nameof(MCounter)), IncludeEntityModel(nameof(MSeat))]
    internal interface MBoxOfficeRepository
    {
    }

    /// <summary>
    /// Lesson 1: a decision on what was read.
    /// <para>
    /// A commit compares what a transaction writes, never what it reads (Tutorial T001, Lesson 16). A decision taken on what it read, and
    /// written elsewhere, is not protected. A hall keeps one usher on duty; two ushers ask to leave at the same time. Each transaction reads two
    /// ushers on duty, and writes its own one. They write two different entities: both commit, and the hall has nobody left. Each decision was
    /// right on what it read: no check in the code can fix this.
    /// </para>
    /// <para>
    /// <c>Claim()</c> ties a decision to the entity that it rests on. The commit conflicts, of the kind <c>Claim</c>, when that entity changed
    /// since the opening of the transaction, and two transactions that claim one entity conflict. This is a <c>SELECT ... FOR UPDATE</c> without
    /// any wait: nothing is frozen, the others go on, and the transaction that claims finds out at its commit.
    /// </para>
    /// </summary>
    public sealed class Lesson01ADecisionOnWhatWasRead
    {
        // Two halls, two ushers on duty in each one.
        internal static void Open(BoxOfficeRepository boxOffice)
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            Staff(new Hall { Name = "Main hall" }, "Ada", "Bob");
            Staff(new Hall { Name = "Small hall" }, "Cleo", "Dan");
            transaction.Commit();
        }

        private static void Staff(Hall hall, string first, string second)
        {
            foreach (var name in new[] { first, second })
            {
                var usher = hall.Ushers.New();
                usher.Name = name;
                usher.Duty = Duty.On;
            }
        }

        internal static Hall MainHall() => HallCollection.First(static hall => hall.Name == "Main hall");

        internal static Hall SmallHall() => HallCollection.First(static hall => hall.Name == "Small hall");

        // The rule of the hall: an usher leaves when another one stays on duty. The decision reads every usher of the hall, and
        // writes only the one who leaves.
        internal static bool Leave(Hall hall, string name)
        {
            int onDuty = 0;
            Usher? leaving = null;
            foreach (var usher in hall.Ushers)
            {
                if (usher.Duty == Duty.On)
                    onDuty++;
                if (usher.Name == name)
                    leaving = usher;
            }
            if (onDuty < 2)
                return false;
            leaving!.Duty = Duty.Off;
            return true;
        }

        // The same decision, which claims the hall that it reads.
        internal static bool LeaveWithAClaim(Hall hall, string name)
        {
            hall.Claim();
            return Leave(hall, name);
        }

        internal static int OnDuty(Hall hall)
        {
            int onDuty = 0;
            foreach (var usher in hall.Ushers)
                if (usher.Duty == Duty.On)
                    onDuty++;
            return onDuty;
        }

        /// <summary>Without a claim, both decisions commit: each one is right on what it read, and the rule of the hall is broken.</summary>
        [Fact]
        public void LeaveAHallEmpty()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-empty", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(Leave(MainHall(), "Ada"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(Leave(MainHall(), "Bob")));

                // Ada and Bob are two entities: nothing conflicts.
                Assert.True(transaction.TryCommit(out _));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, OnDuty(MainHall()));
        }

        /// <summary>Both decisions claim the hall: the later commit conflicts, of the kind <c>Claim</c>. Run again, its decision refuses.</summary>
        [Fact]
        public void KeepAnUsherOnDutyWithAClaim()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-claim", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(LeaveWithAClaim(MainHall(), "Ada"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(LeaveWithAClaim(MainHall(), "Bob")));
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Claim, kind);
            }

            // The work runs again, on the last commit: one usher on duty, and the decision refuses.
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.False(LeaveWithAClaim(MainHall(), "Ada"));
                Assert.True(transaction.TryCommit(out _));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, OnDuty(MainHall()));
        }
    }
}
