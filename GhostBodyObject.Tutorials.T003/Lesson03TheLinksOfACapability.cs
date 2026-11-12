using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Capabilities
{
    using static FacilityRepositoryCollections;

    /// <summary>
    /// Lesson 3: the links of a capability.
    /// <para>
    /// A component has the links of any entity (Tutorial T002). A required link, a <c>T</c>, is given to its birth: the maintenance of a
    /// lock is born with its technician, <c>door.Maintenance.New(technician)</c>, and a strong link retains its target, the technician.
    /// <c>GetOrNew(technician)</c> gives the maintenance of the lock, or a new one; it compares the technician given with the one of the
    /// maintenance, and refuses another. Where another technician is a case of the business, <c>TryGetOrNew</c> gives the maintenance,
    /// and false: the code decides at once.
    /// </para>
    /// <para>
    /// Other entities link to a component as to any entity. A strong reference to a component retains it, and retains its entity
    /// through it: a work order about the maintenance of a lock keeps the maintenance, and the lock, until it is closed. A weak reference
    /// to a component is set to none when the component goes: the work order forgets the battery it was to replace once the battery is
    /// removed.
    /// </para>
    /// </summary>
    public sealed class Lesson03TheLinksOfACapability
    {
        /// <summary>The maintenance of a lock, born with its technician, who cannot leave while assigned.</summary>
        [Fact]
        public void GiveARequiredLinkToTheBirthOfACapability()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-required", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Technician { Name = "Ada" };
            var front = new DoorLock { Name = "Front door" };

            // The technician is a T of the component: a parameter of its New.
            var maintenance = front.Maintenance.New(ada);
            maintenance.Reason = "Jammed bolt";
            Assert.Same(ada, maintenance.Technician);

            // A strong link retains the technician: the removal is refused, and nothing changes.
            Assert.Throws<InvalidOperationException>(ada.Remove);

            // The maintenance ends: the technician is free.
            front.Maintenance.Remove();
            ada.Remove();
            transaction.Commit();
        }

        /// <summary>The maintenance of a lock, or a new one: the technician given is compared with the one of the maintenance.</summary>
        [Fact]
        public void GetOrGiveTheMaintenanceOfATechnician()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-get-or-new", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var (ada, bob) = (new Technician { Name = "Ada" }, new Technician { Name = "Bob" });
            var front = new DoorLock { Name = "Front door" };

            // The same instruction, whether the lock is under maintenance or not.
            front.Maintenance.GetOrNew(ada).Reason = "Jammed bolt";
            front.Maintenance.GetOrNew(ada).Reason = "Jammed bolt, again";
            Assert.Equal(1, MaintenanceCollection.Count());

            // Bob is not the technician of this maintenance: a case of the business, handled at once. Here, Bob takes it over.
            if (!front.Maintenance.TryGetOrNew(bob, out var maintenance))
                maintenance.Technician = bob;
            Assert.Same(bob, front.Maintenance.Get().Technician);

            // Where another technician is a mistake, GetOrNew refuses it, before any write: the transaction goes on.
            Assert.Throws<InvalidOperationException>(() => front.Maintenance.GetOrNew(ada));
            transaction.Commit();
        }

        /// <summary>A work order retains the maintenance it is about, and the lock through it, until it is closed.</summary>
        [Fact]
        public void RetainACapabilityAndItsEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-retain", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var front = new DoorLock { Name = "Front door" };
            var order = new WorkOrder { Title = "Fix the bolt", Maintenance = front.Maintenance.New(new Technician { Name = "Ada" }) };

            // Neither the maintenance nor its lock can leave while the order designates it: the exception names the chain.
            Assert.Throws<InvalidOperationException>(() => front.Maintenance.Remove());
            var error = Assert.Throws<InvalidOperationException>(front.Remove);
            Assert.Contains("cannot be removed", error.Message);

            // The order is closed: the lock can go, with its maintenance.
            order.Remove();
            front.Remove();
            Assert.Empty(MaintenanceCollection);
            transaction.Commit();
        }

        /// <summary>A work order designates the battery to replace, without retaining it: the battery removed, the order forgets it.</summary>
        [Fact]
        public void ForgetARemovedCapability()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-forget", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var front = new DoorLock { Name = "Front door" };
            var order = new WorkOrder { Title = "Replace the battery", Battery = front.Battery.New() };
            Assert.True(order.Battery.Any());

            front.Battery.Remove();
            Assert.False(order.Battery.Any());
            transaction.Commit();
        }
    }
}
