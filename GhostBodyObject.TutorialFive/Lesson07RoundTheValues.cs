using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    using static ShopRepositoryCollections;

    // The total of an order, by the euro: Decimals = 0 keeps no decimal of a decimal. A decimal in an index always says its decimals.
    [ValueIndex(nameof(MOrder), 4), ValueIndexComponent(1, nameof(MOrder.Total), Decimals = 0)]
    internal interface MOrderTotalIndex
    {
    }

    // The points of a customer, by the thousand: Step = 1000.
    [ValueIndex(nameof(MCustomer), 5), ValueIndexComponent(1, nameof(MCustomer.Points), Step = 1000)]
    internal interface MCustomerPointsIndex
    {
    }

    // The birthday of a customer: two parts of a date, the month then the day of the month, whatever the year.
    [ValueIndex(nameof(MCustomer), 6), ValueIndexComponent(1, nameof(MCustomer.Birthday), DatePart.MonthOfYear), ValueIndexComponent(2, nameof(MCustomer.Birthday), DatePart.DayOfMonth)]
    internal interface MCustomerBirthdayIndex
    {
    }

    /// <summary>
    /// Lesson 7: round the values of an index, and keep the result exact.
    /// <para>
    /// An index may keep less than a value: a date at the day (Lesson 6), a decimal at its <c>Decimals</c>, an integer at its
    /// <c>Step</c>. The index is smaller, and the entities of a rounded value are found together. The result stays exact: the
    /// searches take the true values, and the entities of the rounded value of a bound are compared one by one to it.
    /// <c>OrderTotalIndex.From(19.99m)</c>, on an index by the euro, keeps 19.99 and leaves 19.50.
    /// </para>
    /// <para>
    /// A part of a date is a cycle: <c>DatePart.MonthOfYear</c>, <c>DayOfMonth</c>, <c>DayOfWeek</c>, <c>HourOfDay</c>,
    /// <c>MinuteOfHour</c>. The month then the day of the month give the birthdays, whatever the year, which no precision gives:
    /// <c>CustomerBirthdayIndex.Is(10).Is(22)</c>, the customers born on an October 22.
    /// </para>
    /// </summary>
    public sealed class Lesson07RoundTheValues
    {
        /// <summary>The totals by the euro: the bounds compared to the true values, the order of the euros kept.</summary>
        [Fact]
        public void KeepTheResultExactWithARoundedIndex()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-total", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            decimal[] totals = [9.99m, 10.00m, 10.50m, 19.50m, 19.99m, 20.00m, 25.50m, 49.99m, 50.00m];
            for (int i = 0; i < totals.Length; i++)
                new Order { Number = i + 1, Total = totals[i] };

            static List<Money> Totals(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found)
                => found.Select(static order => order.Total).ToList();
            Assert.Equal([19.99m, 20.00m, 25.50m, 49.99m, 50.00m], Totals(OrderTotalIndex.From(19.99m)));
            Assert.Equal([10.50m, 19.50m, 19.99m], Totals(OrderTotalIndex.Between(10.50m, 20m)));
            Assert.Equal([25.50m], Totals(OrderTotalIndex.Is(25.50m)));
            Assert.Equal(4, OrderTotalIndex.Before(19.99m).Count());
            transaction.Commit();
        }

        /// <summary>The points by the thousand: a bound inside a thousand compared to the true values.</summary>
        [Fact]
        public void RoundAnIntegerByItsStep()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-points", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            foreach (var (name, points) in new[] { ("Ada", 500L), ("Bob", 1_200L), ("Cid", 1_800L), ("Dan", 2_500L), ("Eve", 3_100L) })
                new Customer { Name = name, Email = $"{name.ToLowerInvariant()}@example.com", Points = points };
            Assert.Equal(["Cid", "Dan", "Eve"], CustomerPointsIndex.From(1_500).Select(static c => c.Name).ToList());
            Assert.Equal(["Bob", "Cid"], CustomerPointsIndex.Between(1_000, 2_000).Select(static c => c.Name).ToList());
            transaction.Commit();
        }

        /// <summary>The birthdays of a day, of a month, of a part of a month, whatever the year.</summary>
        [Fact]
        public void FindByThePartsOfADate()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-birthday", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            // Each customer has an email of its own: the index of the emails is unique (Lesson 9).
            new Customer { Name = "Ada", Email = "ada@example.com", Birthday = new DateOnly(1990, 10, 22) };
            new Customer { Name = "Bob", Email = "bob@example.com", Birthday = new DateOnly(2000, 3, 14) };
            new Customer { Name = "Cid", Email = "cid@example.com", Birthday = new DateOnly(1985, 10, 22) };
            new Customer { Name = "Dan", Email = "dan@example.com", Birthday = new DateOnly(1999, 10, 1) };

            // Born on an October 22; in October, by day; in the second half of October.
            Assert.Equal(["Ada", "Cid"], CustomerBirthdayIndex.Is(10).Is(22).Select(static c => c.Name).ToList());
            Assert.Equal(["Dan", "Ada", "Cid"], CustomerBirthdayIndex.Is(10).Result.Select(static c => c.Name).ToList());
            Assert.Equal(["Ada", "Cid"], CustomerBirthdayIndex.Is(10).Range(15, 31).Select(static c => c.Name).ToList());

            // The same birthdays by a query of the Views, which reads every customer.
            Assert.Equal(2, CustomerViewCollection.Count(static c => c.Birthday.Value.Month == 10 && c.Birthday.Value.Day == 22));
            transaction.Commit();
        }
    }
}
