using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    using static ShopRepositoryCollections;

    // The orders of a customer, by the instant they were placed: the index behind the collections of Lesson 11.
    [ValueIndex(nameof(MOrder), 12), ValueIndexComponent(1, nameof(MOrder.Customer)), ValueIndexComponent(2, nameof(MOrder.Placed))]
    internal interface MOrderCustomerIndex
    {
    }

    // Your collections, in your partial declarations (Tutorial One, Lesson 11): members of the customer, computed by the index at each
    // read. GBO stores nothing for them.
    internal partial class Customer
    {
        /// <summary>The orders of the customer, from the oldest.</summary>
        public OrderCustomerIndex.WithCustomer Orders => OrderCustomerIndex.Is(this);

        /// <summary>The orders of the customer placed from <paramref name="since"/> on.</summary>
        public ValueIndexResult<OrderView, Order, ShopRepositoryTag> OrdersSince(DateTime since) => OrderCustomerIndex.Is(this).From(since);

        /// <summary>The last order of the customer, if it has one.</summary>
        public Option<OrderView> LastOrder => Orders.Descending().FirstOrNone();
    }

    internal readonly partial struct CustomerView
    {
        /// <summary>The same collection on the View, for the queries over the Views.</summary>
        public OrderCustomerIndex.WithCustomer Orders => OrderCustomerIndex.Is(this);
    }

    // A collection of your own, of the whole shop: a search named once, used everywhere.
    internal static class Backlog
    {
        /// <summary>The paid orders placed before <paramref name="day"/>, still not shipped: the oldest first.</summary>
        public static ValueIndexResult<OrderView, Order, ShopRepositoryTag> Late(DateOnly day) => OrderStatusDayIndex.Is(OrderStatus.Paid).Before(day);
    }

    /// <summary>
    /// Lesson 11: your own collections.
    /// <para>
    /// A collection of the model (Tutorial Two) is a set that your code fills, by <c>Add</c> and <c>Remove</c>. An index gives another
    /// kind of collection: the entities of a value, computed at each read, that your code never fills. Write it as a member of your
    /// partial declarations: <c>customer.Orders</c>, the orders whose customer is this one, by the index of the customer and the
    /// instant. It is always up to date, at the next instruction; it is in the order of its index, read backward, counted, combined;
    /// and GBO stores nothing for it.
    /// </para>
    /// <para>
    /// The View has it too, <c>CustomerView.Orders</c>, for the queries over the Views: the customers of two orders at least, without
    /// a Body. A search of the whole shop is a collection as well, named once in a static class: <c>Backlog.Late(day)</c>.
    /// </para>
    /// <para>
    /// Which one to choose? A collection of the model when the code decides what it holds: the favorites of a listener, the parts of an
    /// aggregate. A collection of an index when the values decide: the orders of a customer, of a status, of a period; it can never
    /// disagree with them.
    /// </para>
    /// </summary>
    public sealed class Lesson11YourOwnCollections
    {
        private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

        private static ShopRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Customer { Name = "Ada", Email = "ada@example.com" };
            var bob = new Customer { Name = "Bob", Email = "bob@example.com" };
            new Customer { Name = "Cid", Email = "cid@example.com" };
            new Order { Number = 1, Customer = ada, Placed = Monday, Status = OrderStatus.Delivered };
            new Order { Number = 2, Customer = bob, Placed = Monday.AddHours(1), Status = OrderStatus.Paid };
            new Order { Number = 3, Customer = ada, Placed = Monday.AddDays(1), Status = OrderStatus.Paid };
            new Order { Number = 4, Customer = ada, Placed = Monday.AddDays(2), Status = OrderStatus.Pending };
            transaction.Commit();
            return shop;
        }

        /// <summary>The orders of a customer, in their order, backward, since a day, the last one; a new order at once.</summary>
        [Fact]
        public void ReadTheOrdersOfACustomer()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-orders", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = CustomerEmailIndex.Is("ada@example.com").Bodies().Single();
            Assert.Equal([1, 3, 4], ada.Orders.Result.Select(static order => order.Number).ToList());
            Assert.Equal(3, ada.Orders.Count());
            Assert.Equal([4, 3, 1], ada.Orders.Descending().Select(static order => order.Number).ToList());
            Assert.Equal([3, 4], ada.OrdersSince(Monday.AddDays(1)).Select(static order => order.Number).ToList());
            Assert.Equal(4, ada.LastOrder.Get().Number);

            // A new order of Ada: in ada.Orders at the next instruction, without any Add.
            new Order { Number = 5, Customer = ada, Placed = Monday.AddDays(3) };
            Assert.Equal(4, ada.Orders.Count());
            Assert.Equal(5, ada.LastOrder.Get().Number);
            transaction.Commit();
        }

        /// <summary>The collection on the View, in a query: the customers of two orders at least; a customer without any.</summary>
        [Fact]
        public void QueryTheCollectionsOfTheViews()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-views", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
            Assert.Equal(["Ada"], CustomerViewCollection.Where(static customer => customer.Orders.Count() >= 2).Select(static customer => customer.Name).ToList());
            Assert.Equal(["Cid"], CustomerViewCollection.Where(static customer => customer.Orders.Count() == 0).Select(static customer => customer.Name).ToList());
        }

        /// <summary>A search of the whole shop, named once: the late orders.</summary>
        [Fact]
        public void NameASearchOfTheShop()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-backlog", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            var thursday = DateOnly.FromDateTime(Monday).AddDays(3);
            Assert.Equal([2, 3], Backlog.Late(thursday).Select(static order => order.Number).ToList());

            // Shipped, an order leaves the backlog.
            foreach (var order in Backlog.Late(thursday).Bodies())
                order.Status = OrderStatus.Shipped;
            Assert.Equal(0, Backlog.Late(thursday).Count());
            transaction.Commit();
        }
    }
}
