using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTen
{
    using static BoxOfficeRepositoryCollections;
    using static Lesson03ARuleOverTheParts;

    /// <summary>
    /// Lesson 5: what marks a point.
    /// <para>
    /// Every change of an entity, a value, a link, a birth, a removal, marks the nearest point above it, at any depth, once per transaction. The
    /// marking stops there: a point is the unit of its domain, and the points above it see nothing of it. So points nest: a line marks its order,
    /// the order its customer, and the lines of two orders of one customer never meet. A point high in a tree puts its whole domain in series:
    /// place it at the height of the rule.
    /// </para>
    /// <para>
    /// A move to another Owner marks both domains. A write of mergeable values only marks nothing: a counter below a point stays a counter. A claim
    /// and a lease mark nothing either. The conflict is of the kind <c>Domain</c> when the transaction only marked the point, <c>Change</c> when it
    /// changed the point itself.
    /// </para>
    /// </summary>
    public sealed class Lesson05WhatMarksAPoint
    {
        private static Customer Bo() => CustomerCollection.Single();

        private static Line LineOf(string reference, string concert)
        {
            foreach (var line in OrderOf(reference).Lines)
                if (line.Concert == concert)
                    return line;
            throw new InvalidOperationException("No line.");
        }

        // How much a change raises the versions of the customer and of its two orders: what it marked.
        private static (ulong Customer, ulong First, ulong Second) Raised(BoxOfficeRepository boxOffice, Action change)
        {
            var before = Versions(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                change();
                transaction.Commit();
            }
            var after = Versions(boxOffice);
            return (after.Customer - before.Customer, after.First - before.First, after.Second - before.Second);
        }

        private static (ulong Customer, ulong First, ulong Second) Versions(BoxOfficeRepository boxOffice)
        {
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                return (Bo().ConcurrencyVersion, OrderOf("B-1").ConcurrencyVersion, OrderOf("B-2").ConcurrencyVersion);
        }

        /// <summary>A change marks the nearest point above it, and nothing higher: a line its order, an order its customer.</summary>
        [Fact]
        public void MarkTheNearestPoint()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-nearest", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);

            // A value of a line, a birth, a removal: the order.
            Assert.Equal((0UL, 1UL, 0UL), Raised(boxOffice, static () => LineOf("B-1", "Carmen").Tickets = 4));
            Assert.Equal((0UL, 1UL, 0UL), Raised(boxOffice, static () => OrderOf("B-1").Lines.New().Concert = "Aida"));
            Assert.Equal((0UL, 1UL, 0UL), Raised(boxOffice, static () => OrderOf("B-1").Lines.Remove(LineOf("B-1", "Aida"))));

            // A value of an order: the order, which changed, and the customer, the point above it.
            Assert.Equal((1UL, 1UL, 0UL), Raised(boxOffice, static () => OrderOf("B-1").Delivery = Delivery.ByPost));

            // A value of the customer: the customer.
            Assert.Equal((1UL, 0UL, 0UL), Raised(boxOffice, static () => Bo().Name = "Bo Lind"));
        }

        /// <summary>Ten changes of one domain mark its point once: the version says that it changed, not how much.</summary>
        [Fact]
        public void MarkOncePerTransaction()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-once", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            Assert.Equal((0UL, 1UL, 0UL), Raised(boxOffice, static () =>
            {
                for (int tickets = 1; tickets <= 10; tickets++)
                    LineOf("B-1", "Carmen").Tickets = tickets;
            }));
        }

        /// <summary>A line moved to another order leaves one domain and enters another one: both are marked.</summary>
        [Fact]
        public void MarkBothDomainsOfAMove()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-move", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            Assert.Equal((0UL, 1UL, 1UL), Raised(boxOffice, static () => LineOf("B-1", "Tosca").MoveTo(OrderOf("B-2").Lines)));
        }

        /// <summary>A counter below a point: its <c>[Delta]</c> merges, and marks nothing. Any other write of the same part would.</summary>
        [Fact]
        public void MarkNothingOfAMergeableValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-mergeable", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            Assert.Equal((0UL, 0UL, 0UL), Raised(boxOffice, static () =>
            {
                foreach (var counter in OrderOf("B-1").Counters)
                    counter.Visits += 1;
            }));
        }

        /// <summary>Points nest: the lines of two orders of one customer never meet, and the values of the two orders do, in the domain of the customer.</summary>
        [Fact]
        public void KeepTheLinesOfTwoOrdersApartAndTheirValuesTogether()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-nested", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                LineOf("B-1", "Carmen").Tickets = 4;
                AnotherTill.Commits(boxOffice, static () => LineOf("B-2", "Carmen").Tickets = 4);
                Assert.True(transaction.TryCommit(out _));
            }
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                OrderOf("B-1").Delivery = Delivery.ByPost;
                AnotherTill.Commits(boxOffice, static () => OrderOf("B-2").Delivery = Delivery.ByPost);
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Domain, kind);
            }
        }

        /// <summary>The kind tells what the transaction did: it changed the point itself, and a change of its domain is a <c>Change</c>.</summary>
        [Fact]
        public void ConflictAsAChangeOnAPointThatTheTransactionChanged()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-kind", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            using var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite);
            OrderOf("B-1").Delivery = Delivery.ByPost;
            AnotherTill.Commits(boxOffice, static () => LineOf("B-1", "Carmen").Tickets = 4);
            Assert.False(transaction.TryCommit(out var kind));
            Assert.Equal(TransactionConflictKind.Change, kind);
        }
    }
}
