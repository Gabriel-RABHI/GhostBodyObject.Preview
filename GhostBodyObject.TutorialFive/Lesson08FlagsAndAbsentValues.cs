using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    // The options of an order, a [Flags] enum: the index finds an exact combination, and each flag alone.
    [ValueIndex(nameof(MOrder), 7), ValueIndexComponent(1, nameof(MOrder.Options))]
    internal interface MOrderOptionsIndex
    {
    }

    // The day of shipping, an Option<VDate>: the orders not shipped yet are found as well.
    [ValueIndex(nameof(MOrder), 8), ValueIndexComponent(1, nameof(MOrder.Shipped))]
    internal interface MOrderShippedIndex
    {
    }

    /// <summary>
    /// Lesson 8: flags, and absent values.
    /// <para>
    /// A <c>[Flags]</c> enum holds several flags at once. Its index finds an exact combination, <c>Is(Gift | Express)</c>, and each
    /// flag: <c>Has(Gift)</c>, the orders that have it, whatever their other flags; <c>One(Gift | Express)</c>, those that have one of
    /// the flags at least; <c>All(Gift | Express)</c>, those that have both; <c>None(Fragile)</c>, those that have none. The last three
    /// give a set of orders, in no particular order.
    /// </para>
    /// <para>
    /// An <c>Option&lt;T&gt;</c> may be absent: its index finds the absent values too, <c>IsNone()</c>, the orders not shipped yet. A
    /// range never holds an absent value: <c>From(day)</c> gives the orders shipped from a day on, never those not shipped.
    /// </para>
    /// </summary>
    public sealed class Lesson08FlagsAndAbsentValues
    {
        private static List<OrderNumber> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();

        private static List<OrderNumber> Numbers(IndexSetResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).OrderBy(static n => n).ToList();

        /// <summary>An exact combination of flags, a flag alone, one of several, all of them, none.</summary>
        [Fact]
        public void FindTheFlags()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-flags", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            new Order { Number = 1, Options = OrderOptions.None };
            new Order { Number = 2, Options = OrderOptions.Gift };
            new Order { Number = 3, Options = OrderOptions.Gift | OrderOptions.Express };
            new Order { Number = 4, Options = OrderOptions.Express | OrderOptions.Fragile };
            new Order { Number = 5, Options = OrderOptions.Gift | OrderOptions.Express | OrderOptions.Fragile };

            Assert.Equal([3], Numbers(OrderOptionsIndex.Is(OrderOptions.Gift | OrderOptions.Express)));
            Assert.Equal([2, 3, 5], Numbers(OrderOptionsIndex.Has(OrderOptions.Gift)));
            Assert.Equal([2, 3, 4, 5], Numbers(OrderOptionsIndex.One(OrderOptions.Gift | OrderOptions.Express)));
            Assert.Equal([3, 5], Numbers(OrderOptionsIndex.All(OrderOptions.Gift | OrderOptions.Express)));
            Assert.Equal([1, 2, 3], Numbers(OrderOptionsIndex.None(OrderOptions.Fragile)));

            // A flag taken off an order: the order leaves the flag at the next instruction.
            OrderNumberIndex.Is(5).Bodies().Single().Options &= ~OrderOptions.Gift;
            Assert.Equal([2, 3], Numbers(OrderOptionsIndex.Has(OrderOptions.Gift)));
            transaction.Commit();
        }

        /// <summary>The orders not shipped yet; those shipped on a day, and from a day on.</summary>
        [Fact]
        public void FindTheAbsentValues()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-absent", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            var monday = new DateOnly(2026, 10, 19);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Order { Number = 1, Shipped = Date.From(monday) };
                new Order { Number = 2 };
                new Order { Number = 3, Shipped = Date.From(monday.AddDays(2)) };
                new Order { Number = 4 };
                transaction.Commit();
            }
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Equal([2, 4], Numbers(OrderShippedIndex.IsNone()));
                Assert.Equal([1], Numbers(OrderShippedIndex.Is(monday)));
                Assert.Equal([1, 3], Numbers(OrderShippedIndex.From(monday)));
                Assert.Empty(Numbers(OrderShippedIndex.Before(monday)));

                // Shipping an order: it leaves the absent values.
                foreach (var order in OrderShippedIndex.IsNone().Bodies())
                    order.Shipped = Date.From(monday.AddDays(3));
                Assert.Equal(0, OrderShippedIndex.IsNone().Count());
                Assert.Equal([3, 2, 4], Numbers(OrderShippedIndex.From(monday.AddDays(1))));
                transaction.Commit();
            }
        }
    }
}
