using GhostBodyObject.Runtime.Logical.Index;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace Tutorials.T005.Shop
{
    // The email of a customer: unique, whatever its case. A string is found by its exact value, never by a range.
    [ValueIndex(nameof(MCustomer), 9, Unique = true), ValueIndexComponent(1, nameof(MCustomer.Email), IgnoreCase = true)]
    internal interface MCustomerEmailIndex
    {
    }

    // The loyalty card of a customer: unique, and absent for the customers without one.
    [ValueIndex(nameof(MCustomer), 10, Unique = true), ValueIndexComponent(1, nameof(MCustomer.Card))]
    internal interface MCustomerCardIndex
    {
    }

    /// <summary>
    /// Lesson 9: a unique value.
    /// <para>
    /// <c>Unique = true</c> gives a value to one entity only, as a constraint <c>UNIQUE</c> of SQL. The Setter refuses a value that
    /// another entity has, in the Store or in the transaction: it throws <c>GhostUniquenessException</c>, and the entity keeps its
    /// previous value; the transaction goes on. A value freed, by a change or a removal, may be taken at the next instruction. An
    /// absent value is never refused: many customers have no card.
    /// </para>
    /// <para>
    /// Two concurrent transactions may give the same value, each alone in its own: the first commit wins, the second is refused by a
    /// <c>TransactionConflictException</c> of the kind <c>Uniqueness</c>. Its retry sees the value taken, as for any conflict
    /// (Tutorial T001, Lesson 16).
    /// </para>
    /// </summary>
    public sealed class Lesson09AUniqueValue
    {
        /// <summary>A customer found by its email, whatever its case; an email taken refused, the previous one kept.</summary>
        [Fact]
        public void RefuseAValueTaken()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-unique", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Customer { Name = "Ada", Email = "ada@example.com" };
                new Customer { Name = "Bob", Email = "bob@example.com" };
                transaction.Commit();
            }
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Equal("Ada", CustomerEmailIndex.Is("ADA@Example.com").Single().Name);

                // The email of Ada, whatever its case, refused to Bob, whose email stays.
                var bob = CustomerEmailIndex.Is("bob@example.com").Bodies().Single();
                Assert.Throws<GhostUniquenessException>(() => bob.Email = "Ada@Example.com");
                Assert.Equal("bob@example.com", bob.Email);

                // Ada takes another email: the old one is free, and Bob takes it.
                CustomerEmailIndex.Is("ada@example.com").Bodies().Single().Email = "ada@lovelace.org";
                bob.Email = "ada@example.com";
                transaction.Commit();
            }
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal("Bob", CustomerEmailIndex.Is("ada@example.com").Single().Name);
                Assert.Equal("Ada", CustomerEmailIndex.Is("ada@lovelace.org").Single().Name);
            }
        }

        /// <summary>Many customers without a card; a card given once.</summary>
        [Fact]
        public void NeverRefuseAnAbsentValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-absent", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Customer { Name = "Ada", Email = "ada@example.com" };
            var bob = new Customer { Name = "Bob", Email = "bob@example.com" };
            Assert.Equal(2, CustomerCardIndex.IsNone().Count());

            ada.Card = CardNumber.From(1001);
            Assert.Throws<GhostUniquenessException>(() => bob.Card = CardNumber.From(1001));
            Assert.False(bob.Card.Any());
            bob.Card = CardNumber.From(1002);
            Assert.Equal("Bob", CustomerCardIndex.Is(1002).Single().Name);
            transaction.Commit();
        }

        /// <summary>Two transactions give the same email: the second commit is refused, its retry sees the email taken.</summary>
        [Fact]
        public async Task RefuseAValueTakenMeanwhile()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-concurrent", StoreAccessMode.ReadWrite);
            using var shop = new ShopRepository(store);
            var firstChanged = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
                new Customer { Name = "Ada", Email = "hello@example.com" };
                firstChanged.SetResult();
                await secondCommitted.Task;
                return Assert.Throws<TransactionConflictException>(() => transaction.Commit());
            });
            await firstChanged.Task;
            await Task.Run(() =>
            {
                using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
                new Customer { Name = "Bob", Email = "Hello@Example.com" };
                transaction.Commit();
            });
            secondCommitted.SetResult();
            Assert.Equal(TransactionConflictKind.Uniqueness, (await first).Kind);

            // The retry of the first one: the email is taken, its Setter refuses it at once.
            await Task.Run(() =>
            {
                using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
                Assert.Throws<GhostUniquenessException>(() => new Customer { Name = "Ada", Email = "hello@example.com" });
            });
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal("Bob", CustomerEmailIndex.Is("hello@example.com").Single().Name);
        }
    }
}
