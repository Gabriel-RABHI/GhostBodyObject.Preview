using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialThree.Devices
{
    using static DeviceRepositoryCollections;

    /// <summary>A device that has a place in the building: a C# interface over the capability Location, unknown to the model.</summary>
    internal interface ILocated
    {
        Name Name { get; }

        Option<Location> Place { get; }
    }

    /// <summary>A device that may run on a battery.</summary>
    internal interface IPowered
    {
        Name Name { get; }

        Option<Battery> Power { get; }
    }

    // The Bodies are partial classes: each type of device implements the interfaces of the capabilities it may have. A member of the
    // name of a component would hide its slot: the interface names its own, Place and Power.
    internal partial class Camera : ILocated, IPowered
    {
        public Option<Location> Place => this.Location;

        public Option<Battery> Power => this.Battery;
    }

    internal partial class DoorLock : ILocated, IPowered
    {
        public Option<Location> Place => this.Location;

        public Option<Battery> Power => this.Battery;
    }

    internal partial class Thermostat : ILocated
    {
        public Option<Location> Place => this.Location;
    }

    /// <summary>
    /// Lesson 5: your interfaces over the capabilities.
    /// <para>
    /// The model has no inheritance: no <c>Device</c> base type, no reference typed by an interface. The polymorphism is in the
    /// components, and in your code: the Bodies are partial classes, and a C# interface adds to them what the devices share, here
    /// <c>ILocated</c> and <c>IPowered</c>. Your code writes one method for every device that implements them.
    /// </para>
    /// <para>
    /// Each type chooses the capabilities it may have: a thermostat is wired, so it is not <c>IPowered</c>. Unlike a base class, a type
    /// implements as many of these interfaces as it has capabilities. The interface exists in C# only: neither the model, nor the Store,
    /// nor the engine of GBO know it.
    /// </para>
    /// </summary>
    public sealed class Lesson05YourInterfacesOverTheCapabilities
    {
        /// <summary>One method for every located device, and one for every powered device, whatever their types.</summary>
        [Fact]
        public void WriteOneMethodForEveryDevice()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-interfaces", StoreAccessMode.ReadWrite);
            using var devices = new DeviceRepository(store);
            using var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite);
            var camera = new Camera { Name = "Hall camera" };
            var door = new DoorLock { Name = "Front door" };
            var thermostat = new Thermostat { Name = "Office thermostat" };
            (camera.Location.New().Floor, thermostat.Location.New().Floor) = (1, 2);
            (camera.Battery.New().Level, door.Battery.New().Level) = (12, 80);

            List<ILocated> located = [camera, door, thermostat];
            Assert.Equal(["Hall camera: floor 1", "Front door: no place", "Office thermostat: floor 2"], located.Select(Describe).ToList());

            List<IPowered> powered = [camera, door];
            Assert.Equal(["Hall camera"], powered.Where(static device => NeedsCharge(device, 20)).Select(static device => device.Name.ToString()).ToList());
            transaction.Commit();
        }

        // Your code over the capabilities: it knows the interfaces, not the types of devices.
        private static string Describe(ILocated device) => device.Place.TryGet(out var place) ? $"{device.Name}: floor {place.Floor}" : $"{device.Name}: no place";

        private static bool NeedsCharge(IPowered device, ChargeLevel threshold) => device.Power.TryGet(out var battery) && battery.Level < threshold;
    }
}
