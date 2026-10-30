# Tutorial Three: Composition

## Entity Component System and object composition model

Tutorial Two linked entities that know each other: an album designates its artist, an order owns its lines. Many models need something else: capabilities that an entity has **without knowing it**. Artists, albums and tracks can be commented, but none of them has to manage its thread of comments: the thread is a capability added to them, without their knowing it. A new type that can be commented changes nothing of the others; a thread of comments changes nothing of what it comments.

An object model answers with inheritance: a `BatteryPoweredDevice` base class, a `Customer` subclass of `Party`. A base class does not know which classes will derive from it, but each class that derives from it knows it: the classes at the top of a hierarchy know the capabilities they share through their base classes. These capabilities are defined statically, in the code. Each combination of capabilities becomes a class, an object cannot change of class without changing of identity, and each new capability changes the classes that exist.

GBO takes up the composition by components of the **Entity Component System** (ECS), an approach of video games, and applies it to any data model:

- **The entity is an anchor.** An invoice is an entity, with properties of its own: its number, its date.
- **Its capabilities are added to it by components**: a thread of comments, a payment schedule, a dispute. The invoice does not know them; each one has its own values, and comes and goes.
- **A combination of components describes a capability.** An invoice with a payment schedule and without a dispute can be collected. These combinations are the **systems**: a system is an entity and one or more of its components, and it brings the capabilities that are specific to this combination: the entities that have it, and the rules of your business about them.

This tutorial shows how it works:

- **The components.** A capability is an entity of its own, attached to an entity that does not know it: `door.Battery.New()`. At most one of a type per entity; removed with its entity, never the reverse.
- **The shared capabilities.** One component for several types of entities: every device that runs on a battery, whatever its type, without any base class.
- **The systems.** An entity and its components, declared once: the entities that have some components, maybe others, and not some more, read as Views; and your rules about them, written in the system.
- **The roles.** A party plays several roles at once, gains and loses them, and stays the same party: a company may be a customer, a supplier, both, and stop being one of them.
- **The Records.** Values that go together, an address, travel in an entity as one property; a history and an inbox are bags of them.

None of this takes code of yours: you declare the capabilities in the model, and GBO keeps each one with its entity, at each instruction, in the transaction and at the commit.

Each lesson is a test class of the project `GhostBodyObject.TutorialThree`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorial Two: the links, the parts, the cascades, the strong and weak references, the static lambdas and the Views are used here without being explained again.

## What you will build

The technical management of a building: door locks, cameras and thermostats, their batteries, their places in the building, their firmwares and their maintenances. Then the parties of a business: companies and people, the customers, the suppliers and the employees that they are, the offers of the suppliers and the orders of the customers.

Four models, each in a namespace of its own with its own Repository, as in Tutorial Two: `Capabilities` for Lessons 1 to 3, `Devices` for Lessons 4 to 7, `Parties` for Lessons 8 to 10, `Contacts` for Lesson 11.

**The values of the models come first**, each a value object ([Tutorial One, Lesson 18](One-TheBasics.md#lesson-18-give-each-value-its-type)). Those the three models share are declared once, in the namespace of the tutorial; each model declares at its head the values that it alone has: a `Room` and an `AlarmKind` for the locks, a `Building`, a `Floor` and a `FirmwareVersion` for the devices, a `Money` and a `PaymentTerm` for the parties.

```csharp
namespace TutorialThree
{
    [ValueObject<GhostString>]
    internal interface VName { }

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
```

Every way of composing of the tutorial, and the lesson that shows it:

| Declaration in the model | Nature | What GBO guarantees | Lessons |
| ------------------------ | ------ | ------------------- | ------- |
| `[ComponentFor(nameof(MDoorLock))]` on `CBattery` | Component | At most one of a type per entity, which does not know it; removed with its entity, never the reverse | 1, 2 |
| `IEntityCollection<MAlarm> Alarms` in a component | Parts of a component | Removed with their component | 2 |
| `[Strong] MTechnician Technician` in a component | A required link of a component | Given to its birth, `door.Maintenance.New(technician)` | 3 |
| `[Strong] Option<CMaintenance>`, `[Weak] Option<CBattery>` | A reference to a component | A strong one retains the component and its entity; a weak one forgets it | 3, 10 |
| `[ComponentFor]` repeated | A shared capability | One collection of every entity that has it, whatever its type | 4 |
| `partial class Camera : ILocated` | A C# interface over the capabilities | One method of yours for every type that implements it | 5 |
| `[SystemFor(nameof(MDoorLock))]` | System | The entities that have its capabilities, as Views, seen at each instruction | 6, 7, 8 |
| `[ExcludeComponent(nameof(CMaintenance))]` | An excluded capability | The entities that have it are out of the system | 6, 10 |
| `RAddress Home`, `Option<RAddress> Billing` | A Record property | A value of several fields, read and replaced whole; never absent, or optional | 11 |
| `RecordBag<RMove> Moves`, `RecordBag Inbox` | A bag of Records | Its Records in their order, of these types, or of any type | 11 |

## Part 1: Capabilities

### Lesson 1: Give an entity a capability

Some locks run on a battery, others are wired. A battery level is not a value of every lock: it is a **capability** that some locks have, with values of its own, that comes and goes. A **component** declares it: an `[Entity]` with `[ComponentFor(nameof(MDoorLock))]`, whose interface starts with `C`.

```csharp
// The values of this model alone, first; the others are those of the tutorial.
[ValueObject<GhostString>]
internal interface VRoom { }

/// <summary>What an alarm reports: "Forced", "Left open".</summary>
[ValueObject<GhostString>]
internal interface VAlarmKind { }

[ValueObject<GhostString>]
internal interface VTitle { }

/// <summary>A moment, a date and a time.</summary>
[ValueObject<DateTime>(ImplicitConversion = true)]
internal interface VInstant { }

[Entity(1, 1, 2), Root, Removable, Enumerable]
internal interface MDoorLock
{
    [Property(1)]
    VName Name { get; set; }

    [Property(2)]
    VRoom Room { get; set; }
}

[Entity(2, 1, 2), ComponentFor(nameof(MDoorLock)), Enumerable]
internal interface CBattery
{
    [Property(1)]
    VChargeLevel Level { get; set; }

    [Property(2)]
    VInstant Replaced { get; set; }
}

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

[GhostRepository, IncludeEntityModel(nameof(MDoorLock)), IncludeEntityModel(nameof(CBattery)), IncludeEntityModel(nameof(CAlarmLog)), IncludeEntityModel(nameof(MAlarm)), IncludeEntityModel(nameof(CMaintenance)), IncludeEntityModel(nameof(MTechnician)), IncludeEntityModel(nameof(MWorkOrder))]
internal interface MFacilityRepository
{
}
```

`CBattery` gives the Body `Battery`, and its **slot** on every lock, `door.Battery`. The lock declares nothing: it does not know its components, and a new kind of capability changes nothing of it, not even its version. The slot reads as an `Option<T>`: `Any()`, `Get()`, `TryGet`, `GetOr`. `New()` gives the lock its battery; the battery reads its lock, `battery.DoorLock`:

```csharp
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
```

A lock has **one battery at most**: a component of a type per entity. A second `New()` is refused before anything changes, and the transaction goes on. `Remove()` takes the battery away, and the slot takes a new one. **`GetOrNew()`** gives the battery that the lock has, or a new one: one instruction, whether the lock has one or not:

```csharp
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
```

The View of a lock has the slot too: an `Option` of the View of the component, read without creating any Body.

```csharp
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
```

The generator checks the interfaces: a component starts with `C`, an entity type with `M`, a system with `S` (Lesson 6); otherwise the build stops with the error **GBO007**.

> **Tip: A component, or a property?**
>
> A value that every lock has is a property: its name, its room. A capability that some locks have, with values of its own, that comes and goes, is a component: its battery. A property costs nothing to read; a component is an entity of its own, found by GBO for its entity, and changed without it.

### Lesson 2: A capability lives with its entity

A component is an **aggregate of its own**: it may own parts, as the alarm log of a lock owns its alarms. Removing the component removes it with its parts, at that instruction; the lock stays:

```csharp
[Fact]
public void RemoveACapabilityWithItsParts()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-parts", StoreAccessMode.ReadWrite);
    using var facility = new FacilityRepository(store);
    using (var transaction = facility.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var front = new DoorLock { Name = "Front door" };
        front.Battery.New().Level = 80;

        // The parts of a component are born in it, as the parts of any aggregate (Tutorial Two, Lesson 4).
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
```

Removing the lock removes it with **every capability it has**, and their parts, as a removal removes the parts of an aggregate (Tutorial Two, Lesson 5). A capability never outlives what has it; the reverse never happens: a capability that leaves does not end what had it.

```csharp
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
```

A component **never changes of entity**: there is no `MoveTo`. A lock and a type of capability are its address. Giving a battery to another lock is creating one there, with the same values, and removing the old one:

```csharp
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
```

The generator refuses on a component `[Root]`, `[Removable]`, `[Orphanable]` and `[WithIdentity]`: its life is that of its entity, its removal that of its slot, and its entity and its type address it. A component is never the target of a composition, nor the component of another component. Otherwise, the build stops with the error **GBO029**.

### Lesson 3: The links of a capability

A component has the links of any entity (Tutorial Two). A **required link**, a `T`, is given to its birth: the maintenance of a lock is born with its technician, `door.Maintenance.New(technician)`, and the strong link retains the technician:

```csharp
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
```

**`GetOrNew(technician)`** gives the maintenance of the lock, or a new one, born with the technician. The technician given is compared with the one of the maintenance: another one is refused, before any write, since `GetOrNew` says "make it exist with this technician". Where another technician is a case of the business, not a mistake, **`TryGetOrNew`** gives the maintenance, and false: the code decides at once, here to hand the maintenance over.

| Instruction | No maintenance | The maintenance of Ada | The maintenance of Bob |
| ----------- | -------------- | ---------------------- | ---------------------- |
| `New(ada)` | Born, with Ada | Refused | Refused |
| `GetOrNew(ada)` | Born, with Ada | The maintenance | Refused, the transaction goes on |
| `TryGetOrNew(ada, out var m)` | `true`, born, with Ada | `true`, the maintenance | `false`, the maintenance of Bob, nothing written |

```csharp
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
```

The same three instructions exist wherever an entity holds one object at most, and makes it born: a component, an optional part, a shared object designated by a strong reference.

Other entities link to a component as to any entity. A **strong reference to a component retains it, and retains its entity** through it, as a reference to a part retains its chain (Tutorial Two, Lesson 9): a work order about the maintenance of a lock keeps the maintenance, and the lock, until it is closed.

```csharp
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
```

A **weak reference** to a component is set to none when the component goes: the work order forgets the battery it was to replace once the battery is removed.

```csharp
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
```

## Part 2: Shared capabilities

### Lesson 4: One capability, several types

A capability is rarely the property of one type: a camera and a lock may both run on a battery, every device has a place in the building. A `[ComponentFor]` **repeated** attaches the same component to each of these types:

```csharp
// The values of this model alone, first; the others are those of the tutorial.
[ValueObject<GhostString>]
internal interface VBuilding { }

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

[Entity(4, 1, 1), ComponentFor(nameof(MCamera)), ComponentFor(nameof(MDoorLock)), Enumerable]
internal interface CBattery
{
    [Property(1)]
    VChargeLevel Level { get; set; }
}

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

[GhostRepository, IncludeEntityModel(nameof(MCamera)), IncludeEntityModel(nameof(MDoorLock)), IncludeEntityModel(nameof(MThermostat)), IncludeEntityModel(nameof(CBattery)), IncludeEntityModel(nameof(CLocation)), IncludeEntityModel(nameof(CFirmware)), IncludeEntityModel(nameof(CMaintenance)), IncludeEntityModel(nameof(SPoweredLock)), IncludeEntityModel(nameof(SFirmwareCamera))]
internal interface MDeviceRepository
{
}
```

`camera.Battery` and `door.Battery` are the same kind of battery. A component of several entity types reads its entity as an `Option` of each type, `battery.Camera` and `battery.DoorLock`, one of them set. The collection of the component holds every battery, whatever the device: "everything that runs on a battery" is `BatteryCollection`.

```csharp
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
```

A question about a capability is a query over its component, not over each type that may have it: the devices of the second floor are the places of the second floor.

```csharp
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
```

There is no base class `Device`, no common table, no type to test: the capability is the type.

### Lesson 5: Your interfaces over the capabilities

The model has no inheritance: no `Device` base type, no reference typed by an interface. The polymorphism is in the components, and **in your code**: the Bodies are partial classes, and a C# interface adds to them what the devices share.

```csharp
internal interface ILocated
{
    Name Name { get; }

    Option<Location> Place { get; }
}

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
```

Each type chooses the capabilities it may have: a thermostat is wired, so it is not `IPowered`. Unlike a base class, a type implements as many of these interfaces as it has capabilities, and your code writes one method for every device that implements them:

```csharp
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
```

The interface exists in C# only: neither the model, nor the Store, nor the engine of GBO know it.

> **Tip: Name the members of your interfaces**
>
> The slot of a component on its entity has the name of the component: `Location`. A member of the same name in the partial class would hide it; the generator refuses one in the model with the error **GBO029**. Give the members of your interfaces their own names, `Place` and `Power`, which read the slots: `public Option<Location> Place => this.Location;`.

## Part 3: Systems

### Lesson 6: A system joins an entity and its capabilities

The maintenance team asks for the locks that run on a battery and are not under maintenance, with their place when they have one. A query over the locks would test three capabilities on each lock. A **system** says it once, in the model:

```csharp
[SystemFor(nameof(MDoorLock)), ExcludeComponent(nameof(CMaintenance)), Enumerable]
internal interface SPoweredLock
{
    MDoorLock Lock { get; }

    CBattery Battery { get; }

    Option<CLocation> Location { get; }
}
```

Its interface starts with `S`. Its properties, read only, are:

- **the entity**, exactly one property of its type, `MDoorLock Lock`;
- **a required capability**, a component of the entity type, a `T`: `CBattery Battery`;
- **an optional capability**, an `Option<T>`: `Option<CLocation> Location`;
- **an excluded capability**, `[ExcludeComponent(nameof(CMaintenance))]` on the interface, repeatable.

GBO writes the struct `PoweredLockSystem`, the name of the interface without its `S` and with `System`, and, with `[Enumerable]`, its collection `PoweredLockSystemCollection`. Its members are **Views**: a system reads many entities, and creates no Body.

The lessons of this part start from four locks:

```csharp
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
```

The collection of a system has the operators of the sequences, with their states (Tutorial One). `ToBody()` gives the Body of the few to change:

```csharp
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
```

The generator checks a system: one member of its entity type, the others components of that type, none both present and excluded. Otherwise, the build stops with the error **GBO030**.

> **Tip: A system reads what may belong to it**
>
> A system reads the entities of its rarest required capability, not every entity of its type: the locks on a battery, not every lock; among a million locks, a system of a capability on 1 % of them reads 1 % of them. A system without a required capability reads every entity of its type. Require the capabilities that the question requires; leave the others optional.

### Lesson 7: The system of one entity, and your code in it

The cameras with a firmware, and their battery when they run on one, are a system of the model too:

```csharp
[SystemFor(nameof(MCamera)), Enumerable]
internal interface SFirmwareCamera
{
    MCamera Camera { get; }

    CFirmware Firmware { get; }

    Option<CBattery> Battery { get; }
}
```

`Of(entity)` gives the system of one entity, for its Body or its View: the system when the entity has its capabilities, none otherwise. A capability that the transaction gives or takes is seen at the next instruction, by the collection of the system as by `Of`: there is no structure to refresh.

```csharp
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
```

A system is a `readonly partial struct`: your methods join its members, and read its Views. A rule of the business, such as which cameras can take a firmware update, is written once, in the system that it is about:

```csharp
// A system is a partial struct: your methods join its members, as they join a Body.
internal readonly partial struct FirmwareCameraSystem
{
    /// <summary>The camera runs a firmware older than <paramref name="version"/>, and can take the update: wired, or charged enough.</summary>
    public bool CanUpdateTo(FirmwareVersion version) => version.CompareTo(Firmware.Version) > 0 && (!Battery.Any() || Battery.Get().Level >= 50);
}

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
```

## Part 4: The composition model

### Lesson 8: Roles, not subclasses

With inheritance, a customer is a subclass of a party: `class Customer : Party`. A party that is also a supplier needs a third class, or two objects for one party; a supplier that becomes a customer changes of class, so of identity, and its history with it. Each combination of roles is a class, and each new role is a change of every class.

With components, **a party is one entity, and its roles are its capabilities**:

```csharp
// The values of this model alone, first; the others are those of the tutorial.
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

[ValueObject<GhostString>]
internal interface VProductName { }

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

[Entity(7, 1, 2), Root, Removable, Enumerable]
internal interface MOrder
{
    [Property(1)]
    VMoney Amount { get; set; }

    [Property(2), Strong]
    CCustomer Customer { get; set; }
}

[GhostRepository, IncludeEntityModel(nameof(MParty)), IncludeEntityModel(nameof(CCustomer)), IncludeEntityModel(nameof(CSupplier)), IncludeEntityModel(nameof(MOffer)), IncludeEntityModel(nameof(CEmployee)), IncludeEntityModel(nameof(CBlocked)), IncludeEntityModel(nameof(MOrder)), IncludeEntityModel(nameof(STradingPartner)), IncludeEntityModel(nameof(SActiveCustomer))]
internal interface MPartyRepository
{
}
```

An order retains the customer role of the party that placed it: it is born with it, `new Order(customer)`, and a strong reference keeps it (Lesson 10).

A party plays any role at once, gains one and loses another, and stays the same party, with the same identity and the same links. Each role has its own values, and its own parts: the offers of a supplier leave with its role.

```csharp
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
```

A system joins the roles that a question needs: the parties that are customers and suppliers at once, whose balances can be netted; the customers that are not blocked, with their badge when they are employees too.

```csharp
[SystemFor(nameof(MParty)), Enumerable]
internal interface STradingPartner
{
    MParty Party { get; }

    CCustomer Customer { get; }

    CSupplier Supplier { get; }
}

[SystemFor(nameof(MParty)), ExcludeComponent(nameof(CBlocked)), Enumerable]
internal interface SActiveCustomer
{
    MParty Party { get; }

    CCustomer Customer { get; }

    Option<CEmployee> Employee { get; }
}

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
```

### Lesson 9: Capabilities and concurrency

A component is **an aggregate of its own**: its values and its parts conflict only with themselves. The sales team raises the credit limit of a customer while the legal team renames the party: two transactions, two aggregates, both commit. With inheritance, the credit limit and the name would be fields of one object, and the second commit would conflict.

```csharp
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
```

Giving or taking a capability changes the capabilities of its entity: two transactions that give or take capabilities of the same party at the same time **conflict** at the second commit, which retries. A party never ends up with two customer roles. Written with `GetOrNew()`, the retry runs the same code: it finds the role that the other transaction gave.

```csharp
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
```

### Lesson 10: What composes what

Every way of composing objects, in one model: a party and its roles, components; the offers of a supplier, parts of its role; the orders of a customer, Roots of their own that retain its role by a strong reference; a block, a component that a system excludes. What a removal does follows from these declarations, never from code of yours. The business of this lesson: a party that supplies two offers, and that placed an order as a customer.

```csharp
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
```

A capability given, a block, changes what the systems see at once, and nothing else: the order and the customer role are untouched.

```csharp
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
```

## Part 5: Values that travel together

### Lesson 11: Values that travel together

A value object holds one value: a city, a day. An address is several of them that always go together: a street, a city, a postal code. A **Record** declares them once, an interface starting with `R`, and any entity, or any other Record, holds it as one property: the home of a client, the address that a move left. A Record has no identity and no link: it is a value, read whole, replaced whole.

```csharp
[Record(2, 1, 3)]
internal interface RAddress
{
    [Property(1)]
    VStreet Street { get; set; }

    [Property(2)]
    VCity City { get; set; }

    [Property(3)]
    VPostalCode PostalCode { get; set; }
}

[Record(3, 1, 2)]
internal interface RMove
{
    [Property(1)]
    VDay On { get; set; }

    [Property(2)]
    RAddress From { get; set; }        // a Record holds Records too
}

[Entity(1, 1, 5), Root, Enumerable]
internal interface MClient
{
    [Property(1)]
    VName Name { get; set; }

    [Property(2)]
    RAddress Home { get; set; }        // never absent: an empty address until written

    [Property(3)]
    Option<RAddress> Billing { get; set; }

    [Property(4)]
    RecordBag<RMove> Moves { get; set; }

    [Property(5)]
    RecordBag Inbox { get; set; }
}

// The messages of the inbox are named by no property of the model: the Repository includes them by their names.
[GhostRepository, IncludeEntityModel(nameof(MClient)), IncludeEntityModel(nameof(RCallBack)), IncludeEntityModel(nameof(RSendBrochure))]
internal interface MClientRepository
{
}
```

`RCallBack` and `RSendBrochure` are two more Records, a message for the call center and one for the mailing service, each of one value.

**An address is a value.** A Record is born anywhere, `new Address { … }`, then assigned: its values are copied into the client. To change one of them, copy the address with `ToBody()`, change the copy, assign it back. An address read before keeps its values: it is a value, not an object that changes under your eyes.

```csharp
[Fact]
public void AnAddressIsAValue()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-address", StoreAccessMode.ReadWrite);
    using var clients = new ClientRepository(store);
    using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // A Record is born anywhere, then assigned: its values are copied into the client.
        var ada = new Client { Name = "Ada" };
        Assert.Equal("", ada.Home.City.ToString());
        ada.Home = new Address { Street = "1 rue de Rivoli", City = "Paris", PostalCode = "75001" };
        Assert.False(ada.Billing.Any());
        transaction.Commit();
    }
    using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var ada = ClientCollection.Single();
        var before = ada.Home;
        // To change one of its values: copy the address, change the copy, assign it back.
        var home = ada.Home.ToBody();
        home.City = "Lyon";
        home.PostalCode = "69001";
        ada.Home = home;
        Assert.Equal("Lyon", ada.Home.City.ToString());
        // The address read before is a value: it has not changed.
        Assert.Equal("Paris", before.City.ToString());
        // The copy is a Record of its own: the client sees its next change only when it is assigned again.
        home.Street = "2 quai Saint-Antoine";
        Assert.Equal("1 rue de Rivoli", ada.Home.Street.ToString());
        ada.Billing = home;
        transaction.Commit();
    }
    using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        // Read through the Views, an address is read in place, as any value of its entity.
        Assert.Equal(1, ClientViewCollection.Count(static c => c.Home.City == "Lyon"));
        var ada = ClientViewCollection.Single();
        Assert.True(ada.Billing.TryGet(out var billing) && billing.Street == "2 quai Saint-Antoine" && billing.City == "Lyon");
    }
}
```

**A history is a bag.** A list of values that the entity owns, the moves of a client, is a bag of Records, `RecordBag<RMove>`: written through the client, without an assignment, its Records in the order of their `Add`. `Enumerate<MoveView>()` reads them with the operators of the sequences.

```csharp
[Fact]
public void KeepAHistoryInABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-history", StoreAccessMode.ReadWrite);
    using var clients = new ClientRepository(store);
    using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var ada = new Client { Name = "Ada", Home = new Address { City = "Paris" } };
        // A move: the address left joins the history, then the new one is assigned. The bag of the client changes the client.
        ada.Moves.Add(new Move { On = new DateOnly(2024, 3, 1), From = ada.Home });
        ada.Home = new Address { City = "Lyon" };
        ada.Moves.Add(new Move { On = new DateOnly(2026, 9, 15), From = ada.Home });
        ada.Home = new Address { City = "Nice" };
        transaction.Commit();
    }
    using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var ada = ClientViewCollection.Single();
        Assert.Equal(2, ada.Moves.Count());
        // Enumerate gives the Records of a type in their order, with the operators of the sequences.
        Assert.Equal(["Paris", "Lyon"], ada.Moves.Enumerate<MoveView>().Select(static m => m.From.City.ToString()).ToList());
        Assert.True(ada.Moves.Enumerate<MoveView>().LastOrNone().TryGet(out var last) && last.On == new DateOnly(2026, 9, 15));
        Assert.Equal("Nice", ada.Home.City.ToString());
    }
}
```

**An inbox carries messages for several services.** A bag of any Record, `RecordBag`, holds the messages that the services of the business leave for a client: each service reads the types it knows and leaves the others. A service that knows several types walks the bag once, a handler per type, `static` lambdas whose state comes first. To take a message: `IndexOf<T>` finds the first of its type, `TryGetAt` reads it, `RemoveAt` removes it; the others stay in their order. To take them all, one after the other: the first, while there is one.

```csharp
[Fact]
public void CarryMessagesForSeveralServices()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-inbox", StoreAccessMode.ReadWrite);
    using var clients = new ClientRepository(store);
    using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var ada = new Client { Name = "Ada" };
        ada.Inbox.Add(new SendBrochure { Subject = "Winter offers" });
        ada.Inbox.Add(new CallBack { Reason = "Damaged parcel" });
        ada.Inbox.Add(new SendBrochure { Subject = "New depots" });
        transaction.Commit();
    }
    using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        // The mailing service knows one type: the other messages are skipped.
        var inbox = ClientViewCollection.Single().Inbox;
        Assert.Equal(3, inbox.Count());
        Assert.Equal(["Winter offers", "New depots"], inbox.Enumerate<SendBrochureView>().Select(static b => b.Subject.ToString()).ToList());
        // A service that knows both walks the inbox once, a handler per type; the handlers capture nothing, their state comes first.
        var work = new List<string>();
        inbox.Enumerate(work,
            static (List<string> w, SendBrochureView brochure) => w.Add("mail " + brochure.Subject),
            static (List<string> w, CallBackView call) => w.Add("call " + call.Reason));
        Assert.Equal(["mail Winter offers", "call Damaged parcel", "mail New depots"], work);
    }
    using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The call center takes its message: found by its type, read, then removed; the others stay in their order.
        var ada = ClientCollection.Single();
        Assert.True(ada.Inbox.IndexOf<CallBackView>(out int at) && ada.Inbox.TryGetAt(at, out CallBackView call));
        ada.Inbox.RemoveAt(at);
        Assert.Equal("Damaged parcel", call.Reason.ToString());
        // The mailing service takes the first message, as long as there is one.
        var sent = new List<string>();
        while (ada.Inbox.Count() > 0)
        {
            if (ada.Inbox.TryGetAt(0, out SendBrochureView brochure))
                sent.Add(brochure.Subject.ToString());
            ada.Inbox.RemoveAt(0);
        }
        Assert.Equal(["Winter offers", "New depots"], sent);
        transaction.Commit();
    }
}
```

**A Record is not a part.** The lines of an order, changed one by one, found, referenced, are parts of their aggregate (Tutorial Two): each has its identity, its links, its conflicts of its own. A Record is the value that several places reuse; a bag, a list that its entity reads whole. Keep few Records in an entity: an entity of many Records couples the evolution of all their hosts, and none of their fields is indexed.

## From the need to the component

The lessons went from a component to what it does. This chapter goes the other way: from what your business needs to the composition that gives it, from the most general question to the most specific. The examples come from the applications that every developer meets: a shop, a document store, a school, a fleet of devices.

### 1. Does every entity of the type have it?

| The need | The composition |
| -------- | --------------- |
| The name of a product, the date of an order: every entity has it | A property |
| The lines of an order: every order has them, created and removed with it | Parts (Tutorial Two, Lesson 4) |
| The discount of a product on sale, the web page of a product sold online: some products have it, for a time | A component: `CSale`, `CWebPage` |
| The account of an employee, the badge of a visitor: some people have it | A component: `CEmployee`, `CVisitor` |

> **Tip: An empty property is a component in disguise**
>
> A property that most entities leave empty, or a group of properties that are set together and cleared together, is a component. As a component, it costs nothing to the entities that do not have it, and its values change without changing the entity.

### 2. Is it shared by several types?

**What can be commented: an order, an invoice, a document.** One component, `[ComponentFor(nameof(MOrder))]` and `[ComponentFor(nameof(MInvoice))]`: its collection holds every thread of comments, whatever its entity (Lesson 4).

**What runs on a battery, what has a place, what has a firmware.** One component per capability, attached to each type of device that may have it; your code shares their handling through C# interfaces on the partial classes (Lesson 5).

### 3. Is it a role that changes over time?

**A customer that becomes a supplier; an employee that leaves and comes back.** A role is a component of the party (Lesson 8): the party keeps its identity and its links; the role has its values and its parts, and leaves with them. A role referenced by others, a customer with orders, cannot leave while they designate it (Lesson 10).

### 4. Does a question join several capabilities?

**The products on sale that are sold online; the devices on battery that are not under maintenance.** A system, `[SystemFor]`, with its required, optional and excluded capabilities (Lesson 6). It reads only the entities of its rarest required capability, and sees the changes of the transaction at once. A rule about these entities is a method of its partial struct (Lesson 7).

### 5. Does it conflict with the rest of the entity?

**The stock of a product and its description; the credit limit of a customer and its address.** Values changed by different people, at different paces, are better in different aggregates: a component changes without conflicting with its entity (Lesson 9).

### The whole path

| The question | When the answer is yes | The declaration | Lessons |
| ------------ | ---------------------- | --------------- | ------- |
| Does every entity have it, for its whole life? | A property, or parts | `[Property(n)]` | Tutorial Two |
| Do some entities have it, for a time, with values of its own? | A component | `[ComponentFor(nameof(MX))]`, its interface starting with `C` | 1, 2 |
| Does the capability own things? | Parts of the component | `IEntityCollection<T>` in the component | 2 |
| Is it born with a link? | A required link of the component | A `T`, given to `New` | 3 |
| Is it shared by several types? | A shared component | `[ComponentFor]` repeated | 4 |
| Must your code treat these types alike? | A C# interface | `partial class X : IInterface` | 5 |
| Does a question join several capabilities? | A system | `[SystemFor(nameof(MX))]`, its interface starting with `S` | 6, 7 |
| Is it a role of a party? | A component of the party | `[ComponentFor(nameof(MParty))]` | 8 |
| Does it change at another pace than its entity? | A component, an aggregate of its own | `[ComponentFor]` | 9 |
| Do several values go together, read and replaced whole, reused in several places? | A Record property, few per entity | `RAddress Home`, its interface starting with `R` | 11 |
| Does the entity own a list of such values, read whole? | A bag of these types | `RecordBag<RMove> Moves` | 11 |
| Does it carry messages of several types for several services? | A bag of any Record | `RecordBag Inbox` | 11 |

## What you learned

- A **component** is a capability that an entity gains and loses without knowing it: `[ComponentFor]`, its slot `entity.Component`, `New`, `GetOrNew`, `TryGetOrNew` and `Remove`, at most one of a type per entity.
- A component **lives with its entity**: removed with it, with its parts; it never removes its entity, and never changes of entity.
- A component has **the links of any entity**: a required link given to its `New`; a strong reference to it retains it and its entity; a weak one forgets it.
- One component serves **several types of entities**: its collection holds every entity that has the capability, whatever its type; **C# interfaces** on the partial classes share your code between them, without inheritance.
- A **system** joins an entity and its capabilities, required, optional and excluded: a collection of Views, `Of(entity)`, and your rules in its partial struct; it reads only what may belong to it, and sees each change of the transaction at once.
- **Roles replace subclasses**: a party plays several roles at once, gains and loses them, and stays one party.
- A component is **an aggregate of its own**: it changes in parallel with its entity.
- A **Record** is a value of several fields, held by an entity or by another Record: read whole, changed by its copy, `ToBody()`, then assigned. A **bag** holds Records in their order, of a few types or of any type: a history, an inbox for several services. A Record is never a part of an aggregate.

[Tutorial Four](Four-Edges.md) goes further with the edges and the graph oriented model.
