using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Parties
{
    using static PartyRepositoryCollections;

    /// <summary>
    /// Lesson 10: what composes what.
    /// <para>
    /// Every way of composing objects in one model: a party and its roles, components; the offers of a supplier, parts of its role; the
    /// orders of a customer, Roots of their own that retain its role by a strong reference; a block, a component that a system excludes.
    /// What a removal does follows from these declarations, never from code of yours.
    /// </para>
    /// <list type="bullet">
    /// <item>A role removed takes its parts with it: the offers of a supplier.</item>
    /// <item>A role that a strong reference retains cannot leave, nor its party: a customer with orders stays a customer.</item>
    /// <item>A capability given, a block, changes what the systems see at once, and nothing else.</item>
    /// </list>
    /// </summary>
    public sealed class Lesson10WhatComposesWhat
    {
        private static PartyRepository NewBusiness(TransactionalMemoryStore store)
        {
            var business = new PartyRepository(store);
            using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
            var acme = new Party { Name = "Acme" };
            var supplier = acme.Supplier.New();
            supplier.Offers.New().Product = "Steel";
            supplier.Offers.New().Product = "Copper";
            // The customer is a T of the order: given to its constructor.
            new Order(acme.Customer.New()) { Amount = 1_200m };
            transaction.Commit();
            return business;
        }

        /// <summary>The supplier role leaves with its offers; the customer role stays, retained by its order, and so does the party.</summary>
        [Fact]
        public void RemoveWhatTheDeclarationsAllow()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-removal", StoreAccessMode.ReadWrite);
            using var business = NewBusiness(store);
            using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
            var acme = PartyCollection.Single();

            // A part follows its role.
            acme.Supplier.Remove();
            Assert.Empty(OfferCollection);

            // A strong reference retains the role and its party: both removals are refused, and nothing changes.
            Assert.Throws<InvalidOperationException>(() => acme.Customer.Remove());
            Assert.Throws<InvalidOperationException>(acme.Remove);
            Assert.True(acme.Customer.Any());

            // The order closed, the role and the party can go.
            OrderCollection.Single().Remove();
            acme.Remove();
            Assert.Empty(CustomerCollection);
            transaction.Commit();
        }

        /// <summary>A block given to a customer: out of the active customers at once; its order and its role untouched.</summary>
        [Fact]
        public void ChangeWhatTheSystemsSee()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-block", StoreAccessMode.ReadWrite);
            using var business = NewBusiness(store);
            using var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite);
            var acme = PartyCollection.Single();
            Assert.True(ActiveCustomerSystem.Of(acme).Any());

            acme.Blocked.New().Reason = "Unpaid invoices";
            Assert.False(ActiveCustomerSystem.Of(acme).Any());
            Assert.Same(acme.Customer.Get(), OrderCollection.Single().Customer);

            acme.Blocked.Remove();
            Assert.True(ActiveCustomerSystem.Of(acme).Any());
            transaction.Commit();
        }
    }
}
