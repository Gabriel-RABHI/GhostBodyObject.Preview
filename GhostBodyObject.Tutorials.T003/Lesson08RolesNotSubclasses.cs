using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Parties
{
    using static PartyRepositoryCollections;

    // The model of Lessons 8 to 10: the parties of a business, companies and people, and the roles they play. A party may be a
    // customer, a supplier, an employee, any of them at once, and its roles come and go over the years; a party can be blocked.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>An amount of money: never negative, added and subtracted.</summary>
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;
    }

    /// <summary>The term of payment of a supplier, in days.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VPaymentTerm { }

    internal partial struct PaymentTerm
    {
        private static bool IsValid(int days) => days >= 0;
    }

    /// <summary>The name of a product offered by a supplier.</summary>
    [ValueObject<GhostString>]
    internal interface VProductName { }

    /// <summary>The badge of an employee.</summary>
    [ValueObject<GhostString>]
    internal interface VBadge { }

    [Entity(1, 1, 1), Root, Removable, Enumerable]
    internal interface MParty
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 1), ComponentFor(nameof(MParty)), Enumerable]
    internal interface CCustomer
    {
        [Property(1)]
        VMoney CreditLimit { get; set; }
    }

    /// <summary>The supplier role, and the offers of the supplier: parts of the role, which leave with it.</summary>
    [Entity(3, 1, 2), ComponentFor(nameof(MParty)), Enumerable]
    internal interface CSupplier
    {
        [Property(1)]
        VPaymentTerm PaymentDays { get; set; }

        [Property(2)]
        IEntityCollection<MOffer> Offers { get; }
    }

    [Entity(4, 1, 2), Enumerable]
    internal interface MOffer
    {
        [Property(1)]
        VProductName Product { get; set; }

        [Property(2)]
        VMoney Price { get; set; }

        [OwnedFrom]
        CSupplier Supplier { get; }
    }

    [Entity(5, 1, 1), ComponentFor(nameof(MParty)), Enumerable]
    internal interface CEmployee
    {
        [Property(1)]
        VBadge Badge { get; set; }
    }

    [Entity(6, 1, 1), ComponentFor(nameof(MParty))]
    internal interface CBlocked
    {
        [Property(1)]
        VReason Reason { get; set; }
    }

    /// <summary>An order: it retains the customer role of the party that placed it (Lesson 10).</summary>
    [Entity(7, 1, 2), Root, Removable, Enumerable]
    internal interface MOrder
    {
        [Property(1)]
        VMoney Amount { get; set; }

        [Property(2), Strong]
        CCustomer Customer { get; set; }
    }

    /// <summary>The parties that are both customer and supplier: their balances can be netted.</summary>
    [SystemFor(nameof(MParty)), Enumerable]
    internal interface STradingPartner
    {
        MParty Party { get; }

        CCustomer Customer { get; }

        CSupplier Supplier { get; }
    }

    /// <summary>The customers that are not blocked, and their badge when they are employees too: the staff discount.</summary>
    [SystemFor(nameof(MParty)), ExcludeComponent(nameof(CBlocked)), Enumerable]
    internal interface SActiveCustomer
    {
        MParty Party { get; }

        CCustomer Customer { get; }

        Option<CEmployee> Employee { get; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MParty)), IncludeEntityModel(nameof(CCustomer)), IncludeEntityModel(nameof(CSupplier)), IncludeEntityModel(nameof(MOffer)), IncludeEntityModel(nameof(CEmployee)), IncludeEntityModel(nameof(CBlocked)), IncludeEntityModel(nameof(MOrder)), IncludeEntityModel(nameof(STradingPartner)), IncludeEntityModel(nameof(SActiveCustomer))]
    internal interface MPartyRepository
    {
    }

    /// <summary>
    /// Lesson 8: roles, not subclasses.
    /// <para>
    /// With inheritance, a customer is a subclass of a party: <c>class Customer : Party</c>. A party that is also a supplier needs a
    /// third class, or two objects for one party; a supplier that becomes a customer changes of class, so of identity, and its history
    /// with it. Each combination of roles is a class, and each new role is a change of every class.
    /// </para>
    /// <para>
    /// With components, a party is one entity, and its roles are its capabilities: <c>party.Customer</c>, <c>party.Supplier</c>,
    /// <c>party.Employee</c>. A party plays any of them at once, gains one and loses another, and stays the same party, with the same
    /// identity and the same links. Each role has its own values, and its own parts: the offers of a supplier leave with its role. A
    /// system joins the roles that a question needs: the parties that are customers and suppliers at once.
    /// </para>
    /// </summary>
    public sealed class Lesson08RolesNotSubclasses
    {
        /// <summary>A supplier becomes a customer too, then stops supplying: one party all along, its offers leaving with its role.</summary>
        [Fact]
        public void GainAndLoseRoles()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-roles", StoreAccessMode.ReadWrite);
            using var business = new PartyRepository(store);
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var acme = new Party { Name = "Acme" };
                var supplier = acme.Supplier.New();
                supplier.PaymentDays = 30;
                supplier.Offers.New().Product = "Steel";
                transaction.Commit();
            }
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The supplier buys from us as well: a second role, the same party.
                var acme = PartyCollection.Single();
                acme.Customer.New().CreditLimit = 50_000m;
                Assert.True(acme.Supplier.Any() && acme.Customer.Any());
                transaction.Commit();
            }
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // It stops supplying: the role leaves with its offers; the party and its other role stay.
                var acme = PartyCollection.Single();
                acme.Supplier.Remove();
                Assert.Empty(OfferCollection);
                Assert.Equal(50_000m, acme.Customer.Get().CreditLimit);
                transaction.Commit();
            }
        }

        /// <summary>The systems over the roles: the trading partners, and the active customers with their staff badge.</summary>
        [Fact]
        public void JoinTheRolesThatAQuestionNeeds()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-systems", StoreAccessMode.ReadWrite);
            using var business = new PartyRepository(store);
            using (var transaction = business.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var acme = new Party { Name = "Acme" };
                acme.Customer.New();
                acme.Supplier.New();
                var ada = new Party { Name = "Ada" };
                ada.Customer.New();
                ada.Employee.New().Badge = "E-17";
                var shady = new Party { Name = "Shady Ltd" };
                shady.Customer.New();
                shady.Blocked.New().Reason = "Unpaid invoices";
                new Party { Name = "Globex" }.Supplier.New();
                transaction.Commit();
            }
            using (business.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(["Acme"], TradingPartnerSystemCollection.Select(static s => s.Party.Name).ToList());
                Assert.Equal(["Acme", "Ada"], ActiveCustomerSystemCollection.Select(static s => s.Party.Name).Order().ToList());
                Assert.Equal("E-17", ActiveCustomerSystemCollection.Single(static s => s.Employee.Any()).Employee.Get().Badge);
            }
        }
    }
}
