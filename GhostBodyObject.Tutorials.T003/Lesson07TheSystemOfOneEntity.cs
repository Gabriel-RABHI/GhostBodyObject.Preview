using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Devices
{
    using static DeviceRepositoryCollections;

    // A system is a partial struct: your methods join its members, as they join a Body.
    internal readonly partial struct FirmwareCameraSystem
    {
        /// <summary>The camera runs a firmware older than <paramref name="version"/>, and can take the update: wired, or charged enough.</summary>
        public bool CanUpdateTo(FirmwareVersion version) => version.CompareTo(Firmware.Version) > 0 && (!Battery.Any() || Battery.Get().Level >= 50);
    }

    /// <summary>
    /// Lesson 7: the system of one entity, and your code in it.
    /// <para>
    /// <c>Of(entity)</c> gives the system of one entity, for its Body or its View: the system when the entity has its capabilities, none
    /// otherwise. A capability that the transaction gives or takes is seen at the next instruction, by the collection of the system as by
    /// <c>Of</c>: there is no structure to refresh.
    /// </para>
    /// <para>
    /// A system is a <c>readonly partial struct</c>: your methods join its members, and read its Views. A rule of the business, such as
    /// which cameras can take a firmware update, is written once, in the system that it is about.
    /// </para>
    /// </summary>
    public sealed class Lesson07TheSystemOfOneEntity
    {
        /// <summary>The system of one camera: none without a firmware; seen at once when the transaction gives it one.</summary>
        [Fact]
        public void GiveTheSystemOfOneEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-of", StoreAccessMode.ReadWrite);
            using var devices = new DeviceRepository(store);
            using var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite);
            var hall = new Camera { Name = "Hall camera" };
            Assert.False(FirmwareCameraSystem.Of(hall).Any());

            // The firmware attached: the camera is in the system at the next instruction, by its Body and by its View.
            hall.Firmware.New().Version = "2.1";
            Assert.True(FirmwareCameraSystem.Of(hall).TryGet(out var system));
            Assert.Equal("2.1", system.Firmware.Version);
            Assert.True(FirmwareCameraSystem.Of(hall.ToView()).Any());
            Assert.Single(FirmwareCameraSystemCollection);

            // And out of it once the firmware is removed.
            hall.Firmware.Remove();
            Assert.Empty(FirmwareCameraSystemCollection);
            transaction.Commit();
        }

        /// <summary>A rule written in the system: the cameras that can take an update, wired or charged enough.</summary>
        [Fact]
        public void WriteARuleInTheSystem()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-rule", StoreAccessMode.ReadWrite);
            using var devices = new DeviceRepository(store);
            using (var transaction = devices.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Camera { Name = "Wired camera" }.Firmware.New().Version = "2.0";
                var charged = new Camera { Name = "Charged camera" };
                charged.Firmware.New().Version = "2.0";
                charged.Battery.New().Level = 80;
                var empty = new Camera { Name = "Empty camera" };
                empty.Firmware.New().Version = "2.0";
                empty.Battery.New().Level = 10;
                var recent = new Camera { Name = "Recent camera" };
                recent.Firmware.New().Version = "3.0";
                transaction.Commit();
            }
            using (devices.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var ready = FirmwareCameraSystemCollection.Where("3.0", static (s, version) => s.CanUpdateTo(version))
                    .Select(static s => s.Camera.Name).Order().ToList();
                Assert.Equal(["Charged camera", "Wired camera"], ready);
            }
        }
    }
}
