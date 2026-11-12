using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Devices
{
    using static DeviceRepositoryCollections;

    // The model of Lessons 4 to 7: three kinds of devices, and capabilities shared between them. A camera and a lock may run on a
    // battery; every device has a place in the building, and a firmware; a camera or a lock may be under maintenance. Two systems join
    // a device and its capabilities (Lessons 6 and 7).

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The building of a device.</summary>
    [ValueObject<GhostString>]
    internal interface VBuilding { }

    /// <summary>A floor of a building, 0 the ground floor.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VFloor { }

    /// <summary>The version of a firmware, "2.1": versions compare as their texts.</summary>
    [ValueObject<GhostString>]
    internal interface VFirmwareVersion { }

    [Entity(1, 1, 1), Root, Removable, Enumerable]
    internal interface MCamera
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(2, 1, 1), Root, Removable, Enumerable]
    internal interface MDoorLock
    {
        [Property(1)]
        VName Name { get; set; }
    }

    [Entity(3, 1, 1), Root, Removable, Enumerable]
    internal interface MThermostat
    {
        [Property(1)]
        VName Name { get; set; }
    }

    // [ComponentFor] repeated: the same capability on two types of devices.
    /// <summary>The battery of a camera or of a lock; a thermostat is wired, and has none.</summary>
    [Entity(4, 1, 1), ComponentFor(nameof(MCamera)), ComponentFor(nameof(MDoorLock)), Enumerable]
    internal interface CBattery
    {
        [Property(1)]
        VChargeLevel Level { get; set; }
    }

    /// <summary>The place of a device in the building, whatever the device.</summary>
    [Entity(5, 1, 2), ComponentFor(nameof(MCamera)), ComponentFor(nameof(MDoorLock)), ComponentFor(nameof(MThermostat)), Enumerable]
    internal interface CLocation
    {
        [Property(1)]
        VBuilding Building { get; set; }

        [Property(2)]
        VFloor Floor { get; set; }
    }

    [Entity(6, 1, 1), ComponentFor(nameof(MCamera)), ComponentFor(nameof(MDoorLock)), ComponentFor(nameof(MThermostat)), Enumerable]
    internal interface CFirmware
    {
        [Property(1)]
        VFirmwareVersion Version { get; set; }
    }

    [Entity(7, 1, 1), ComponentFor(nameof(MCamera)), ComponentFor(nameof(MDoorLock)), Enumerable]
    internal interface CMaintenance
    {
        [Property(1)]
        VReason Reason { get; set; }
    }

    // A system: an interface [SystemFor], starting with S. Its properties: the entity, its required components, a T, its optional
    // ones, an Option<T>; [ExcludeComponent] keeps out the entities that have a component (Lesson 6).
    /// <summary>The locks on a battery, maybe located, and not under maintenance.</summary>
    [SystemFor(nameof(MDoorLock)), ExcludeComponent(nameof(CMaintenance)), Enumerable]
    internal interface SPoweredLock
    {
        MDoorLock Lock { get; }

        CBattery Battery { get; }

        Option<CLocation> Location { get; }
    }

    /// <summary>The cameras with a firmware, their battery if they run on one (Lesson 7).</summary>
    [SystemFor(nameof(MCamera)), Enumerable]
    internal interface SFirmwareCamera
    {
        MCamera Camera { get; }

        CFirmware Firmware { get; }

        Option<CBattery> Battery { get; }
    }

    [GhostRepository(2), IncludeEntityModel(nameof(MCamera)), IncludeEntityModel(nameof(MDoorLock)), IncludeEntityModel(nameof(MThermostat)), IncludeEntityModel(nameof(CBattery)), IncludeEntityModel(nameof(CLocation)), IncludeEntityModel(nameof(CFirmware)), IncludeEntityModel(nameof(CMaintenance)), IncludeEntityModel(nameof(SPoweredLock)), IncludeEntityModel(nameof(SFirmwareCamera))]
    internal interface MDeviceRepository
    {
    }

    /// <summary>
    /// Lesson 4: one capability, several types.
    /// <para>
    /// A capability is rarely the property of one type: a camera and a lock may both run on a battery, every device has a place. A
    /// <c>[ComponentFor]</c> repeated attaches the same component to each of these types: <c>camera.Battery</c> and
    /// <c>door.Battery</c> are the same kind of battery. The component reads its entity as an <c>Option</c> of each type,
    /// <c>battery.Camera</c> and <c>battery.DoorLock</c>, one of them set.
    /// </para>
    /// <para>
    /// The collection of the component holds every battery, whatever the device: "everything that runs on a battery" is
    /// <c>BatteryCollection</c>. There is no base class <c>Device</c>, no common table, no type to test: the capability is the type.
    /// </para>
    /// </summary>
    public sealed class Lesson04OneCapabilitySeveralTypes
    {
        /// <summary>A battery on a camera and on a lock: one collection for both, each battery reading its device.</summary>
        [Fact]
        public void ShareACapabilityBetweenTypes()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-shared", StoreAccessMode.ReadWrite);
            using var devices = new DeviceRepository(store);
            using (var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Camera { Name = "Hall camera" }.Battery.New().Level = 30;
                new DoorLock { Name = "Front door" }.Battery.New().Level = 90;
                new Thermostat { Name = "Office thermostat" };
                transaction.Commit();
            }
            using (devices.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // Everything that runs on a battery, whatever the device.
                Assert.Equal(2, BatteryCollection.Count());
                var low = BatteryCollection.Single(static b => b.Level < 50);

                // The battery reads its device: an Option of each type it attaches to, one of them set.
                Assert.Equal("Hall camera", low.Camera.Get().Name);
                Assert.False(low.DoorLock.Any());
            }
        }

        /// <summary>The devices of a floor, of every type, by their capability: a query over the places, not over three types.</summary>
        [Fact]
        public void QueryTheDevicesByTheirCapability()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-query", StoreAccessMode.ReadWrite);
            using var devices = new DeviceRepository(store);
            using (var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Place(new Camera { Name = "Hall camera" }.Location.New(), "North", 1);
                Place(new DoorLock { Name = "Office door" }.Location.New(), "North", 2);
                Place(new Thermostat { Name = "Office thermostat" }.Location.New(), "North", 2);
                new Thermostat { Name = "Spare thermostat" };
                transaction.Commit();
            }
            using (devices.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // The Views of the places of the second floor, and the name of the device of each, whatever its type.
                var second = LocationViewCollection.Where(2, static (place, floor) => place.Floor == floor)
                    .Select(static place => place.DoorLock.Any() ? place.DoorLock.Get().Name : place.Thermostat.Any() ? place.Thermostat.Get().Name : place.Camera.Get().Name)
                    .Order().ToList();
                Assert.Equal(["Office door", "Office thermostat"], second);
            }
        }

        private static void Place(Location location, Building building, Floor floor)
        {
            location.Building = building;
            location.Floor = floor;
        }
    }
}
