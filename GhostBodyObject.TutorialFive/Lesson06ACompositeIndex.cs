using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    // A composite index: the status, then the day the order was placed. DatePrecision.Day keeps the day of the date: the orders of a
    // day share it, and are ordered among themselves as they were created.
    [ValueIndex(nameof(MOrder), 3), ValueIndexComponent(1, nameof(MOrder.Status)), ValueIndexComponent(2, nameof(MOrder.Placed), DatePrecision.Day)]
    internal interface MOrderStatusDayIndex
    {
    }

    /// <summary>
    /// Lesson 6: a composite index.
    /// <para>
    /// An index of several components orders its entities by the first, then by the second for equal firsts, as the words of a
    /// dictionary: the status, then the day. <c>Is</c> fixes a component and gives the conditions of the next one:
    /// <c>OrderStatusDayIndex.Is(OrderStatus.Paid)</c> gives the paid orders, by day; <c>.From(day)</c>, the paid orders from a day on;
    /// <c>.Is(day)</c>, those of a day. A range ends the search: there is no condition after it.
    /// </para>
    /// <para>
    /// The components are read in their order, never skipped: the orders of a period, of any status, are the search of another index,
    /// the one of Lesson 5. A date at the day takes a <c>DateOnly</c>: the whole day. It takes a <c>DateTime</c> too: an instant inside
    /// a day, whose orders are then compared one by one to it. The index keeps the day, the result is exact.
    /// </para>
    /// </summary>
    public sealed class Lesson06ACompositeIndex
    {
        private static readonly DateOnly Monday = new(2026, 10, 19);

        // Three days of orders, every status, one order every three hours.
        private static ShopRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            for (int i = 0; i < 24; i++)
                new Order { Number = 100 + i, Status = (OrderStatus)(i % 4), Placed = Monday.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Utc).AddHours(3 * i) };
            transaction.Commit();
            return shop;
        }

        private static List<OrderNumber> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();

        /// <summary>The orders of a status, by day; of a status and a day; of a status from a day on.</summary>
        [Fact]
        public void FixTheComponentsInTheirOrder()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-order", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

            // The paid orders, day by day: the struct of the next component is the result of its prefix as well.
            var paid = OrderStatusDayIndex.Is(OrderStatus.Paid);
            Assert.Equal([101, 105, 109, 113, 117, 121], Numbers(paid));
            Assert.Equal(6, paid.Count());

            // A status and a day; a status from a day on.
            Assert.Equal([109, 113], Numbers(paid.Is(Monday.AddDays(1))));
            Assert.Equal([109, 113, 117, 121], Numbers(paid.From(Monday.AddDays(1))));
            Assert.Equal([121, 117, 113, 109, 105, 101], Numbers(paid.Descending()));

            // Two statuses: by status, then by day.
            Assert.Equal([100, 104, 108, 112, 116, 120, 101, 105, 109, 113, 117, 121], Numbers(OrderStatusDayIndex.Range(OrderStatus.Pending, OrderStatus.Paid)));
        }

        /// <summary>An instant inside a day: the index reads the day, and keeps the orders from the instant on.</summary>
        [Fact]
        public void CompareAnInstantInsideADay()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-instant", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
            var shipped = OrderStatusDayIndex.Is(OrderStatus.Shipped);

            // The shipped orders of Tuesday: 06:30 and 18:30. From Tuesday noon: only 18:30, then those of Wednesday.
            Assert.Equal([110, 114], Numbers(shipped.Is(Monday.AddDays(1))));
            var tuesdayNoon = Monday.AddDays(1).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
            Assert.Equal([114, 118, 122], Numbers(shipped.From(tuesdayNoon)));

            // The same search by a query of the Views reads every order, and finds the same ones.
            Assert.Equal([114, 118, 122], OrderViewCollection.Where(tuesdayNoon, static (order, noon) => order.Status == OrderStatus.Shipped && order.Placed >= noon)
                .Select(static order => order.Number).ToList());
        }
    }
}
