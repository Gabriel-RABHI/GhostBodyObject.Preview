using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T010
{
    using static BoxOfficeRepositoryCollections;

    // ----- The entities of the concurrency points -----

    /// <summary>How the tickets of an order reach the customer.</summary>
    internal enum Delivery
    {
        AtTheDoor,
        ByPost
    }

    /// <summary>A line of a basket or of an order: the tickets of one concert.</summary>
    [Entity(3, 1, 2)]
    internal interface MLine
    {
        [Property(1)]
        VTitle Concert { get; set; }

        [Property(2)]
        VTickets Tickets { get; set; }
    }

    /// <summary>A basket and its lines: an ordinary aggregate.</summary>
    [Entity(4, 1, 2), Root, Enumerable]
    internal interface MBasket
    {
        [Property(1)]
        VName Customer { get; set; }

        [Property(2)]
        IEntityCollection<MLine> Lines { get; }
    }

    /// <summary>A customer and its orders. <c>[ConcurrencyPoint]</c>: a point of its own, above those of its orders (Lesson 5).</summary>
    [Entity(5, 1, 2), Root, Enumerable, ConcurrencyPoint]
    internal interface MCustomer
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MOrder> Orders { get; }
    }

    /// <summary>The same aggregate as a basket, an order. <c>[ConcurrencyPoint]</c>: every change below it counts as a change of the order.</summary>
    [Entity(6, 1, 4), Enumerable, ConcurrencyPoint]
    internal interface MOrder
    {
        [Property(1)]
        VReference Reference { get; set; }

        [Property(2)]
        Delivery Delivery { get; set; }

        [Property(3)]
        IEntityCollection<MLine> Lines { get; }

        [Property(4)]
        IEntityCollection<MCounter> Counters { get; }
    }

    /// <summary>A counter of the visits of the page of an order: a value that many writers add to, and that no rule reads (Lesson 5).</summary>
    [Entity(7, 1, 1), Mergeable]
    internal interface MCounter
    {
        [Property(1), Delta]
        VVisits Visits { get; set; }
    }

    /// <summary>
    /// Lesson 3: a rule over the parts.
    /// <para>
    /// A customer takes six tickets at most, all concerts of an order together. Each line is checked against all the others, and a change of a
    /// line writes that line only: two transactions that raise two lines write nothing in common, both commit, and the rule is broken. The
    /// structure of an aggregate is protected, a line added or removed changes its Owner (Tutorial T002, Lesson 18); the values of its parts
    /// are not.
    /// </para>
    /// <para>
    /// <c>[ConcurrencyPoint]</c> on the Owner makes the aggregate one domain of conflict. Every change below it marks the point, once per
    /// transaction, without anyone asking; two transactions that change one domain conflict, of the kind <c>Domain</c>. A rule read over the parts
    /// now holds, and a domain never touches its neighbors.
    /// </para>
    /// </summary>
    public sealed class Lesson03ARuleOverTheParts
    {
        // A basket of Bo, and two orders of Bo: three tickets for Carmen and two for Tosca in each one.
        internal static void Open(BoxOfficeRepository boxOffice)
        {
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            Book(new Basket { Customer = "Bo" }.Lines);
            var bo = new Customer { Name = "Bo" };
            foreach (var reference in new[] { "B-1", "B-2" })
            {
                var order = bo.Orders.New();
                order.Reference = reference;
                Book(order.Lines);
                order.Counters.New();
            }
            transaction.Commit();
        }

        private static void Book(PartCollection<Line, BoxOfficeRepositoryTag> lines)
        {
            var carmen = lines.New();
            carmen.Concert = "Carmen";
            carmen.Tickets = 3;
            var tosca = lines.New();
            tosca.Concert = "Tosca";
            tosca.Tickets = 2;
        }

        // The rule: six tickets at most, all lines together. It reads every line, and writes one.
        internal static bool AddATicket(PartCollection<Line, BoxOfficeRepositoryTag> lines, string concert)
        {
            int total = 0;
            Line? added = null;
            foreach (var line in lines)
            {
                total += (int)line.Tickets;
                if (line.Concert == concert)
                    added = line;
            }
            if (total >= 6)
                return false;
            added!.Tickets += 1;
            return true;
        }

        internal static int Total(PartCollection<Line, BoxOfficeRepositoryTag> lines)
        {
            int total = 0;
            foreach (var line in lines)
                total += (int)line.Tickets;
            return total;
        }

        internal static Basket TheBasket() => BasketCollection.Single();

        internal static Order OrderOf(string reference) => OrderCollection.First(reference, static (order, reference) => order.Reference == reference);

        /// <summary>Without a point, two lines of one basket change at the same time: each transaction reads five tickets, and the basket holds seven.</summary>
        [Fact]
        public void BreakARuleOfThePartsWithoutAPoint()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-without", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(AddATicket(TheBasket().Lines, "Carmen"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(AddATicket(TheBasket().Lines, "Tosca")));
                Assert.True(transaction.TryCommit(out _));
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(7, Total(TheBasket().Lines));
        }

        /// <summary>With a point, the later commit conflicts, of the kind <c>Domain</c>: its next attempt finds six tickets, and refuses.</summary>
        [Fact]
        public void KeepARuleOfThePartsWithAPoint()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-with", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(AddATicket(OrderOf("B-1").Lines, "Carmen"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(AddATicket(OrderOf("B-1").Lines, "Tosca")));
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Domain, kind);
            }
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(6, Total(OrderOf("B-1").Lines));
                Assert.False(AddATicket(OrderOf("B-1").Lines, "Carmen"));
            }
        }

        /// <summary>Two orders are two domains: the changes of their lines never conflict.</summary>
        [Fact]
        public void ChangeTwoOrdersAtTheSameTime()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-two", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.True(AddATicket(OrderOf("B-1").Lines, "Carmen"));
                AnotherTill.Commits(boxOffice, static () => Assert.True(AddATicket(OrderOf("B-2").Lines, "Carmen")));
                Assert.True(transaction.TryCommit(out _));
            }
        }
    }
}
