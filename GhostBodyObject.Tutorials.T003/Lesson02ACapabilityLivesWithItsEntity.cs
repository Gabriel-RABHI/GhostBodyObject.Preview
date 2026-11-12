using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Capabilities
{
    using static FacilityRepositoryCollections;

    /// <summary>
    /// Lesson 2: a capability lives with its entity.
    /// <para>
    /// A component is an aggregate of its own: it may own parts, as the alarm log of a lock owns its alarms. Removing the component
    /// removes it with its parts, at that instruction; the lock stays. Removing the lock removes it with every capability it has, and
    /// their parts: a capability never outlives what has it.
    /// </para>
    /// <para>
    /// The reverse never happens: a capability that leaves does not end what had it. And a component never changes of entity: there is
    /// no <c>MoveTo</c>. A lock and a type of capability are its address; giving a battery to another lock is creating one there.
    /// </para>
    /// </summary>
    public sealed class Lesson02ACapabilityLivesWithItsEntity
    {
        /// <summary>The alarm log of a lock, removed with its alarms; the lock and its battery stay.</summary>
        [Fact]
        public void RemoveACapabilityWithItsParts()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-parts", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var front = new DoorLock { Name = "Front door" };
                front.Battery.New().Level = 80;

                // The parts of a component are born in it, as the parts of any aggregate (Tutorial T002, Lesson 4).
                var log = front.AlarmLog.New();
                log.Alarms.New().Kind = "Forced";
                log.Alarms.New().Kind = "Left open";
                transaction.Commit();
            }
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var front = DoorLockCollection.Single();
                Assert.Equal(2, AlarmCollection.Count());

                // The log is cleared: removed with its alarms, at this instruction.
                Assert.True(front.AlarmLog.Remove());
                Assert.Empty(AlarmCollection);

                // The lock stays, with its other capabilities.
                Assert.Equal(80, front.Battery.Get().Level);
                Assert.Single(DoorLockCollection);
                transaction.Commit();
            }
        }

        /// <summary>A lock removed: its battery, its log and its alarms leave with it.</summary>
        [Fact]
        public void RemoveTheCapabilitiesWithTheirEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-cascade", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var front = new DoorLock { Name = "Front door" };
                front.Battery.New().Level = 80;
                front.AlarmLog.New().Alarms.New().Kind = "Forced";
                new DoorLock { Name = "Back door" }.Battery.New().Level = 60;
                transaction.Commit();
            }
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                DoorLockCollection.Single(static l => l.Name == "Front door").Remove();
                Assert.Equal(60, BatteryCollection.Single().Level);
                Assert.Empty(AlarmCollection);
                transaction.Commit();
            }
        }

        /// <summary>A battery moved to another lock: a new battery there, with the same values, and the old one removed.</summary>
        [Fact]
        public void GiveACapabilityToAnotherEntityByCreatingIt()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-move", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var front = new DoorLock { Name = "Front door" };
            var back = new DoorLock { Name = "Back door" };
            var used = front.Battery.New();
            used.Level = 70;
            used.Replaced = new DateTime(2026, 6, 1);

            var moved = back.Battery.New();
            (moved.Level, moved.Replaced) = (used.Level, used.Replaced);
            front.Battery.Remove();
            Assert.Same(back, BatteryCollection.Single().DoorLock);
            transaction.Commit();
        }
    }
}
