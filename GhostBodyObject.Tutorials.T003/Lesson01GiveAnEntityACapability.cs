using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has.

    /// <summary>The name of a device, a technician, a party: a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>Why a device is under maintenance, why a party is blocked.</summary>
    [ValueObject<GhostString>]
    internal interface VReason { }

    /// <summary>The charge of a battery, in percent, from 0 to 100.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VChargeLevel { }

    internal partial struct ChargeLevel
    {
        private static bool IsValid(int percent) => percent is >= 0 and <= 100;
    }
}

namespace Tutorials.T003.Capabilities
{
    using static FacilityRepositoryCollections;

    // The first model of the tutorial: the door locks of a building, and the capabilities that some of them have. A lock may run on a
    // battery, keep a log of its alarms, be under maintenance. None of them is a value of every lock: each one is a component, an entity
    // of its own, that the lock gains and loses without knowing it.
    //
    // Each lesson, or group of lessons, declares its own model, in its own namespace: the using static at the top of the namespace
    // chooses the collections of its own Repository, as in Tutorial T002.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The room of a lock.</summary>
    [ValueObject<GhostString>]
    internal interface VRoom { }

    /// <summary>What an alarm reports: "Forced", "Left open".</summary>
    [ValueObject<GhostString>]
    internal interface VAlarmKind { }

    /// <summary>The title of a work order.</summary>
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>A moment, a date and a time.</summary>
    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    /// <summary>A door lock of the building: a Root that the code may remove. It declares nothing of its capabilities.</summary>
    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MDoorLock
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        VRoom Room { get; set; }
    }

    // A component: an [Entity] with [ComponentFor], its interface starting with C. It gives the Body Battery, and the slot
    // door.Battery on every DoorLock.
    /// <summary>The battery of a lock that runs on one.</summary>
    [Entity(2, 1, 2), ComponentFor(nameof(MDoorLock)), Enumerable]
    internal interface CBattery
    {
        [Property(1)]
        VChargeLevel Level { get; set; }

        [Property(2)]
        VInstant Replaced { get; set; }
    }

    /// <summary>The log of the alarms of a lock: its alarms are parts of the component (Lesson 2).</summary>
    [Entity(3, 1, 1), ComponentFor(nameof(MDoorLock))]
    internal interface CAlarmLog
    {
        [Property(1)]
        IEntityCollection<MAlarm> Alarms { get; }
    }

    [Entity(4, 1, 2), Enumerable]
    internal interface MAlarm
    {
        [Property(1)]
        VAlarmKind Kind { get; set; }

        [Property(2)]
        VInstant At { get; set; }

        [OwnedFrom]
        CAlarmLog Log { get; }
    }

    /// <summary>A lock under maintenance, and the technician in charge: a required link, given to the birth of the component (Lesson 3).</summary>
    [Entity(5, 1, 2), ComponentFor(nameof(MDoorLock)), Enumerable]
    internal interface CMaintenance
    {
        [Property(1)]
        VReason Reason { get; set; }

        [Property(2), Strong]
        MTechnician Technician { get; set; }
    }

    [Entity(6, 1, 1), Root, Removable, Enumerable]
    internal interface MTechnician
    {
        [Property(1)]
        VName Name { get; set; }
    }

    /// <summary>A work order: it retains the maintenance it is about, and designates the battery to replace, without retaining it (Lesson 3).</summary>
    [Entity(7, 1, 3), Root, Removable, Enumerable]
    internal interface MWorkOrder
    {
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2), Strong]
        Option<CMaintenance> Maintenance { get; set; }

        [Property(3), Weak]
        Option<CBattery> Battery { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MDoorLock)), IncludeEntityModel(nameof(CBattery)), IncludeEntityModel(nameof(CAlarmLog)), IncludeEntityModel(nameof(MAlarm)), IncludeEntityModel(nameof(CMaintenance)), IncludeEntityModel(nameof(MTechnician)), IncludeEntityModel(nameof(MWorkOrder))]
    internal interface MFacilityRepository
    {
    }

    /// <summary>
    /// Lesson 1: give an entity a capability.
    /// <para>
    /// Some locks run on a battery, others are wired. A battery level is not a value of every lock: it is a capability that some locks
    /// have, with values of its own, that comes and goes. A component declares it: an <c>[Entity]</c> with
    /// <c>[ComponentFor(nameof(MDoorLock))]</c>, whose interface starts with <c>C</c>, <c>CBattery</c>. GBO writes its Body,
    /// <c>Battery</c>, and its slot on every lock, <c>door.Battery</c>. The lock declares nothing: it does not know its components, and
    /// a new kind of capability changes nothing of it.
    /// </para>
    /// <para>
    /// The slot reads as an <c>Option&lt;T&gt;</c>: <c>Any()</c>, <c>Get()</c>, <c>TryGet</c>, <c>GetOr</c>. <c>New()</c> gives the
    /// lock its battery; <c>Remove()</c> takes it away. A lock has one battery at most: a component of a type per entity. The component
    /// reads its entity, <c>battery.DoorLock</c>. <c>GetOrNew()</c> gives the battery that the lock has, or a new one.
    /// </para>
    /// </summary>
    public sealed class Lesson01GiveAnEntityACapability
    {
        /// <summary>A lock gains a battery by its slot; the battery reads its lock; the slot reads the battery back after the commit.</summary>
        [Fact]
        public void GiveALockABattery()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-battery", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var front = new DoorLock { Name = "Front door", Room = "Lobby" };
                Assert.False(front.Battery.Any());

                // New gives the lock its battery: the component is born attached to its entity.
                var battery = front.Battery.New();
                battery.Level = 100;
                battery.Replaced = new DateTime(2026, 9, 1);

                // The slot reads the component, the component reads its entity: the very same objects.
                Assert.Same(battery, front.Battery.Get());
                Assert.Same(front, battery.DoorLock);

                // A wired lock has no battery: its slot is empty.
                var back = new DoorLock { Name = "Back door", Room = "Storage" };
                Assert.False(back.Battery.TryGet(out _));
                transaction.Commit();
            }
            using (facility.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var front = DoorLockCollection.Single(static l => l.Name == "Front door");
                Assert.Equal(100, front.Battery.Get().Level);

                // Everything that runs on a battery: the collection of the component, whatever the lock.
                Assert.Equal(["Front door"], BatteryCollection.Select(static b => b.DoorLock.Name).ToList());
            }
        }

        /// <summary>One battery per lock: a second one is refused, before anything changes; once removed, the slot takes a new one.</summary>
        [Fact]
        public void HaveOneComponentOfATypeAtMost()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-one", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite);
            var front = new DoorLock { Name = "Front door" };
            front.Battery.New().Level = 40;

            // The refusal changes nothing: the transaction goes on.
            Assert.Throws<InvalidOperationException>(() => front.Battery.New());
            Assert.Equal(40, front.Battery.Get().Level);

            // The battery is replaced: the old one removed, a new one born.
            Assert.True(front.Battery.Remove());
            Assert.False(front.Battery.Remove());
            front.Battery.New().Level = 100;
            Assert.Equal(1, BatteryCollection.Count());

            // GetOrNew gives the battery that the lock has, or a new one: one instruction, whether the lock has one or not.
            Assert.Same(front.Battery.Get(), front.Battery.GetOrNew());
            new DoorLock { Name = "Back door" }.Battery.GetOrNew().Level = 100;
            Assert.Equal(2, BatteryCollection.Count());
            transaction.Commit();
        }

        /// <summary>The View of a lock reads its battery as a View, without creating any Body.</summary>
        [Fact]
        public void ReadACapabilityOnAView()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-view", StoreAccessMode.ReadWrite);
            using var facility = new FacilityRepository(store);
            using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new DoorLock { Name = "Front door" }.Battery.New().Level = 15;
                new DoorLock { Name = "Back door" };
                transaction.Commit();
            }
            using (facility.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // The slot of a View is an Option of the View of the component.
                int low = DoorLockViewCollection.Count(20, static (view, threshold) => view.Battery.TryGet(out var battery) && battery.Level < threshold);
                Assert.Equal(1, low);
                Assert.Equal("Front door", BatteryViewCollection.Single().DoorLock.Name);
            }
        }
    }
}
