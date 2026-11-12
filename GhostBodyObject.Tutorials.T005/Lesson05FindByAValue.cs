using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T005.Shop
{
    using static ShopRepositoryCollections;

    // The third model, of Lessons 5 to 12: the orders of a shop and its customers, found by their values. Each lesson declares the
    // value indexes it shows, in its own file: an index is an interface of the model, wherever it is declared.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The e-mail of a customer.</summary>
    [ValueObject<GhostString>]
    internal interface VEmail { }

    /// <summary>A day of the calendar.</summary>
    [ValueObject<DateOnly>(ImplicitConversion = true)]
    internal interface VDate { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    /// <summary>The loyalty points of a customer: never negative, added and subtracted.</summary>
    [ValueObject<long>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VLoyaltyPoints { }

    internal partial struct LoyaltyPoints
    {
        private static bool IsValid(long points) => points >= 0;
    }

    /// <summary>The number of a loyalty card.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VCardNumber { }

    /// <summary>The number of an order.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VOrderNumber { }

    /// <summary>The note of a customer on an order.</summary>
    [ValueObject<GhostString>]
    internal interface VNote { }

    internal enum OrderStatus
    {
        Pending,
        Paid,
        Shipped,
        Delivered
    }

    /// <summary>The options of an order, several at once (Lesson 8).</summary>
    [Flags]
    internal enum OrderOptions
    {
        None = 0,
        Gift = 1,
        Express = 2,
        Fragile = 4
    }

    [Entity(1, 1, 5), Root, Removable, Enumerable]
    internal interface MCustomer
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        VEmail Email { get; set; }

        [Property(3)]
        VDate Birthday { get; set; }

        [Property(4)]
        VLoyaltyPoints Points { get; set; }

        // A loyalty card, absent until given (Lesson 9).
        [Property(5)]
        Option<VCardNumber> Card { get; set; }
    }

    [Entity(2, 1, 8), Root, Removable, Enumerable]
    internal interface MOrder
    {
        [Property(1)]
        VOrderNumber Number { get; set; }

        [Property(2)]
        OrderStatus Status { get; set; }

        [Property(3)]
        VInstant Placed { get; set; }

        [Property(4)]
        VMoney Total { get; set; }

        [Property(5)]
        OrderOptions Options { get; set; }

        // The day of shipping, absent until the order ships (Lesson 8).
        [Property(6)]
        Option<VDate> Shipped { get; set; }

        // The customer of the order; a guest order has none.
        [Property(7), Weak]
        Option<MCustomer> Customer { get; set; }

        // The note of the customer, in a text index (Lesson 10).
        [Property(8), TextIndexed(nameof(MOrderTextIndex))]
        VNote Note { get; set; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MCustomer)), IncludeEntityModel(nameof(MOrder))]
    internal interface MShopRepository
    {
    }

    // A value index: an interface with [ValueIndex], of the entity type it indexes and of its number, among the indexes of the
    // Repository; its components, [ValueIndexComponent], each at its position, from 1. Its name starts with M: MOrderNumberIndex gives
    // OrderNumberIndex. A Repository has the value indexes of the entity types it includes.
    [ValueIndex(nameof(MOrder), 1), ValueIndexComponent(1, nameof(MOrder.Number))]
    internal interface MOrderNumberIndex
    {
    }

    [ValueIndex(nameof(MOrder), 2), ValueIndexComponent(1, nameof(MOrder.Placed))]
    internal interface MOrderPlacedIndex
    {
    }

    /// <summary>
    /// Lesson 5: find entities by a value.
    /// <para>
    /// A value index orders the entities of a type by the values of its components. Declared by an interface with
    /// <c>[ValueIndex(nameof(MOrder), 1)]</c> and <c>[ValueIndexComponent(1, nameof(MOrder.Number))]</c>, it gives the class
    /// <c>OrderNumberIndex</c>: <c>Is(1042)</c> finds the order of a number, without reading the others. On a date,
    /// <c>OrderPlacedIndex</c> finds a period: <c>From(start)</c>, from a value on; <c>Before(end)</c>, below a value;
    /// <c>Between(start, end)</c>, from the first and below the second, as the ranges of C#; <c>Range(first, last)</c>, both
    /// included. <c>Every</c> gives every order.
    /// </para>
    /// <para>
    /// A search gives the Views of the entities in the order of their values, and <c>Descending()</c> backward: the most recent
    /// orders first. <c>Count()</c> counts them; <c>Bodies()</c> gives the Bodies of the few to change. The index is up to date at the
    /// next instruction, in the transaction itself.
    /// </para>
    /// </summary>
    public sealed class Lesson05FindByAValue
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

        // Ten orders, one every four hours from Monday morning.
        private static ShopRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            for (int i = 0; i < 10; i++)
                new Order { Number = 1040 + i, Placed = Monday.AddHours(4 * i), Total = 10m * (i + 1) };
            transaction.Commit();
            return shop;
        }

        private static List<OrderNumber> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();

        /// <summary>The order of a number, read by its View, then changed by its Body.</summary>
        [Fact]
        public void FindAnOrderByItsNumber()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-number", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Equal(30m, OrderNumberIndex.Is(1042).Single().Total);
                Assert.Equal(0, OrderNumberIndex.Is(9999).Count());

                // The Body of the order to change; its new number is found at the next instruction.
                var order = OrderNumberIndex.Is(1042).Bodies().Single();
                order.Status = OrderStatus.Paid;
                order.Number = 2042;
                Assert.Equal(0, OrderNumberIndex.Is(1042).Count());
                Assert.Equal(OrderStatus.Paid, OrderNumberIndex.Is(2042).Single().Status);
                transaction.Commit();
            }
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, OrderNumberIndex.Is(2042).Count());
        }

        /// <summary>The orders of a period: from, before, between, both included; every order, in the order of their dates.</summary>
        [Fact]
        public void ReadTheOrdersOfAPeriod()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-period", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
            var tuesday = Monday.Date.AddDays(1);

            // Tuesday and after; before Tuesday.
            Assert.Equal([1044, 1045, 1046, 1047, 1048, 1049], Numbers(OrderPlacedIndex.From(tuesday)));
            Assert.Equal([1040, 1041, 1042, 1043], Numbers(OrderPlacedIndex.Before(tuesday)));

            // Tuesday only: from its start, below the start of Wednesday.
            Assert.Equal([1044, 1045, 1046, 1047, 1048, 1049], Numbers(OrderPlacedIndex.Between(tuesday, tuesday.AddDays(1))));

            // Between excludes its end, Range includes it.
            Assert.Equal([1040, 1041], Numbers(OrderPlacedIndex.Between(Monday, Monday.AddHours(8))));
            Assert.Equal([1040, 1041, 1042], Numbers(OrderPlacedIndex.Range(Monday, Monday.AddHours(8))));

            // Every order, and their count.
            Assert.Equal(10, OrderPlacedIndex.Every.Count());
            Assert.Equal(550m, OrderPlacedIndex.Every.Sum(static order => order.Total));
        }

        /// <summary>The most recent orders first: the index read backward.</summary>
        [Fact]
        public void ReadTheMostRecentFirst()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-recent", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            Assert.Equal([1049, 1048, 1047], OrderPlacedIndex.Every.Descending().Select(static order => order.Number).Take(3).ToList());

            // A new order, at the head at once.
            new Order { Number = 1050, Placed = Monday.AddDays(3) };
            Assert.Equal(1050, OrderPlacedIndex.Every.Descending().First().Number);

            // The same search by a query of the Views reads every order.
            Assert.Equal(1050, OrderViewCollection.MaxBy(static order => order.Placed).Number);
            transaction.Commit();
        }
    }
}
