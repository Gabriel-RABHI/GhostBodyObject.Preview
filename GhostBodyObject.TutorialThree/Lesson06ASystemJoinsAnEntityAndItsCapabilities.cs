using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialThree.Devices
{
    using static DeviceRepositoryCollections;

    /// <summary>
    /// Lesson 6: a system joins an entity and its capabilities.
    /// <para>
    /// The maintenance team asks for the locks that run on a battery and are not under maintenance, with their place when they have
    /// one. A query over the locks would test three capabilities on each lock. A system says it once: an interface
    /// <c>[SystemFor(nameof(MDoorLock))]</c>, whose interface starts with <c>S</c>, <c>SPoweredLock</c>. Its properties are the lock, the
    /// required component <c>CBattery Battery</c>, a <c>T</c>, and the optional one <c>Option&lt;CLocation&gt; Location</c>;
    /// <c>[ExcludeComponent(nameof(CMaintenance))]</c> keeps out the locks under maintenance.
    /// </para>
    /// <para>
    /// GBO writes the struct <c>PoweredLockSystem</c> and, with <c>[Enumerable]</c>, its collection <c>PoweredLockSystemCollection</c>,
    /// with the operators of the sequences. Its members are Views: a system reads many entities, and creates no Body; <c>ToBody()</c>
    /// gives the Body of the few to change. A system reads only the entities that may belong to it: those of its rarest required
    /// capability, here the locks that run on a battery, not every lock.
    /// </para>
    /// </summary>
    public sealed class Lesson06ASystemJoinsAnEntityAndItsCapabilities
    {
        // Four locks: on a battery and located; on a battery, nowhere; on a battery, under maintenance; wired.
        private static DeviceRepository NewBuilding(TransactionalMemoryStore store)
        {
            var devices = new DeviceRepository(store);
            using var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite);
            var front = new DoorLock { Name = "Front door" };
            front.Battery.New().Level = 15;
            front.Location.New().Floor = 0;
            new DoorLock { Name = "Office door" }.Battery.New().Level = 70;
            var lab = new DoorLock { Name = "Lab door" };
            lab.Battery.New().Level = 5;
            lab.Maintenance.New().Reason = "Jammed bolt";
            new DoorLock { Name = "Server room door" };
            transaction.Commit();
            return devices;
        }

        /// <summary>The locks on a battery, not under maintenance, with their place when they have one.</summary>
        [Fact]
        public void EnumerateTheEntitiesThatHaveTheCapabilities()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-system", StoreAccessMode.ReadWrite);
            using var devices = NewBuilding(store);
            using var transaction = devices.OpenTransaction(StoreAccessMode.ReadOnly);

            // The lab door is under maintenance, the server room door is wired: neither is in the system.
            Assert.Equal(["Front door", "Office door"], PoweredLockSystemCollection.Select(static s => s.Lock.Name).Order().ToList());

            // An optional capability reads as an Option.
            var front = PoweredLockSystemCollection.Single(static s => s.Lock.Name == "Front door");
            Assert.Equal(0, front.Location.Get().Floor);
            Assert.False(PoweredLockSystemCollection.Single(static s => s.Lock.Name == "Office door").Location.Any());
        }

        /// <summary>The operators of the sequences, with their states; the Body of the few to change, by ToBody.</summary>
        [Fact]
        public void QueryASystemAndChangeTheFewItFinds()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-change", StoreAccessMode.ReadWrite);
            using var devices = NewBuilding(store);
            using (var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Assert.Equal(1, PoweredLockSystemCollection.Count(20, static (s, threshold) => s.Battery.Level < threshold));

                // The batteries to replace: the Views find them, their Bodies change them.
                foreach (var low in PoweredLockSystemCollection.Where(20, static (s, threshold) => s.Battery.Level < threshold).ToList())
                    low.Battery.ToBody().Level = 100;
                transaction.Commit();
            }
            using (devices.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, PoweredLockSystemCollection.Count(20, static (s, threshold) => s.Battery.Level < threshold));
        }
    }
}
