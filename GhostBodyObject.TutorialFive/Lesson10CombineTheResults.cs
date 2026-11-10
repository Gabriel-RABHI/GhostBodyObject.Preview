using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Shop
{
    // The notes of the orders, in a text index: an index of the Repository, numbered with its value indexes.
    [TextIndex(11)]
    internal interface MOrderTextIndex
    {
    }

    /// <summary>
    /// Lesson 10: combine the results of the indexes.
    /// <para>
    /// A result of an index is a set of entities: it combines with another result of the same type, of another value index or of a
    /// text search. <c>a.And(b)</c> gives the entities of both, <c>a.Or(b)</c> those of either, <c>a.AndNot(b)</c> those of the first
    /// but not of the second. A combination reads each index once, never an entity that none of them gives: the paid orders that are
    /// gifts, of a note that says "door".
    /// </para>
    /// <para>
    /// A combination is a set: its entities come in no particular order, the order of none of its indexes. It counts, gives its
    /// Bodies for the few to change, and combines again. A composite index (Lesson 6) reads its components in their order; two
    /// separate indexes, combined, answer the conditions on any of them.
    /// </para>
    /// </summary>
    public sealed class Lesson10CombineTheResults
    {
        private static ShopRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            var placed = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);
            new Order { Number = 1, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.Gift, Note = "Leave at the door." };
            new Order { Number = 2, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.None, Note = "Call before delivery." };
            new Order { Number = 3, Status = OrderStatus.Shipped, Placed = placed, Options = OrderOptions.Gift, Note = "Gift wrap, please." };
            new Order { Number = 4, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.Gift | OrderOptions.Express, Note = "Call me, then leave at the door." };
            new Order { Number = 5, Status = OrderStatus.Pending, Placed = placed, Options = OrderOptions.Express, Note = "Back door." };
            transaction.Commit();
            return shop;
        }

        private static List<OrderNumber> Numbers(IndexSetResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).OrderBy(static n => n).ToList();

        /// <summary>The paid orders that are gifts, either, or not.</summary>
        [Fact]
        public void CombineTwoIndexes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-two", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
            var paid = OrderStatusDayIndex.Is(OrderStatus.Paid);
            var gifts = OrderOptionsIndex.Has(OrderOptions.Gift);
            Assert.Equal([1, 4], Numbers(paid.And(gifts)));
            Assert.Equal([1, 2, 3, 4], Numbers(paid.Or(gifts)));
            Assert.Equal([2], Numbers(paid.AndNot(gifts)));
            Assert.Equal(2, paid.And(gifts).Count());
        }

        /// <summary>A text search combined with value indexes: the orders of a word, of a status, of an option.</summary>
        [Fact]
        public void CombineATextSearch()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-text", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
            var door = OrderTextIndex.Search("door");
            Assert.Equal([1, 4, 5], door.Select(static order => order.Number).ToList());
            Assert.Equal([1, 4], Numbers(OrderStatusDayIndex.Is(OrderStatus.Paid).And(door)));
            Assert.Equal([5], Numbers(door.AndNot(OrderOptionsIndex.Has(OrderOptions.Gift))));

            // A combination combines again: the paid orders of a door, but not those to call first.
            Assert.Equal([1], Numbers(OrderStatusDayIndex.Is(OrderStatus.Paid).And(door).AndNot(OrderTextIndex.Search("call"))));
        }

        /// <summary>The Bodies of a combination, for the few to change.</summary>
        [Fact]
        public void ChangeTheEntitiesOfACombination()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-change", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The paid gifts ship.
                foreach (var order in OrderStatusDayIndex.Is(OrderStatus.Paid).And(OrderOptionsIndex.Has(OrderOptions.Gift)).Bodies())
                    order.Status = OrderStatus.Shipped;
                transaction.Commit();
            }
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal([1, 3, 4], OrderStatusDayIndex.Is(OrderStatus.Shipped).Result.Select(static order => order.Number).ToList());
                Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
            }
        }
    }
}
