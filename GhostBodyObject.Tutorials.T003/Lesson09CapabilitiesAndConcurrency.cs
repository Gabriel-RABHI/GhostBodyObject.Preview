using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace Tutorials.T003.Parties
{
    using static PartyRepositoryCollections;

    /// <summary>
    /// Lesson 9: capabilities and concurrency.
    /// <para>
    /// A component is an aggregate of its own: its values and its parts conflict only with themselves. The sales team raises the credit
    /// limit of a customer while the legal team renames the party: two transactions, two aggregates, both commit. The roles of one party
    /// change in parallel, which composition is for: with inheritance, the credit limit and the name would be fields of one object, and
    /// the second commit would conflict.
    /// </para>
    /// <para>
    /// Giving or taking a capability is a change of the entity's set of capabilities: two transactions that give or take capabilities of
    /// the same party at the same time conflict at the second commit, which retries. A party never ends up with two customer roles.
    /// Written with <c>GetOrNew()</c>, the retry runs the same code: it finds the role that the other transaction gave.
    /// </para>
    /// </summary>
    public sealed class Lesson09CapabilitiesAndConcurrency
    {
        /// <summary>The credit limit of the customer role and the name of the party, changed by two transactions at once: both commit.</summary>
        [Fact]
        public async Task ChangeARoleAndItsPartyConcurrently()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-parallel", StoreAccessMode.ReadWrite);
            using var business = new PartyRepository(store);
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Party { Name = "Acme" }.Customer.New().CreditLimit = 10_000m;
                transaction.Commit();
            }
            var firstChanged = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var sales = Task.Run(async () =>
            {
                using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
                CustomerCollection.Single().CreditLimit = 25_000m;
                firstChanged.SetResult();
                await secondCommitted.Task;
                transaction.Commit();
            });
            await firstChanged.Task;
            await Task.Run(() =>
            {
                using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
                PartyCollection.Single().Name = "Acme Corporation";
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await sales;
            using (business.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var acme = PartyCollection.Single();
                Assert.Equal("Acme Corporation", acme.Name);
                Assert.Equal(25_000m, acme.Customer.Get().CreditLimit);
            }
        }

        /// <summary>Two transactions make the same party a customer: the second commit throws; retried, the same code finds the role.</summary>
        [Fact]
        public async Task ConflictOnTheCapabilitiesOfOneEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-conflict", StoreAccessMode.ReadWrite);
            using var business = new PartyRepository(store);
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Party { Name = "Acme" };
                transaction.Commit();
            }
            var firstGiven = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
                GrantCredit(5_000m);
                firstGiven.SetResult();
                await secondCommitted.Task;
                Assert.Throws<TransactionConflictException>(transaction.Commit);
            });
            await firstGiven.Task;
            await Task.Run(() =>
            {
                using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
                GrantCredit(8_000m);
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            // The retry runs the same code: GetOrNew finds the role that the other transaction gave, and changes it.
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Equal(8_000m, CustomerCollection.Single().CreditLimit);
                GrantCredit(5_000m);
                transaction.Commit();
            }
            using (business.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(5_000m, CustomerCollection.Single().CreditLimit);
        }

        // The credit limit of the party, in its customer role, or in a new one: the same code for the first try and for the retry.
        private static void GrantCredit(Money limit) => PartyCollection.Single().Customer.GetOrNew().CreditLimit = limit;
    }
}
