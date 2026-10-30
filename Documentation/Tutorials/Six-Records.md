# Tutorial Six: Records

## Values of several fields

The tutorials before modeled what has an identity: a book, a customer, a device. Each one is **someone**: it exists on its own, it is found, it is linked, and it changes. Not everything in an application is like that. An address is not someone: it is a value that happens to have several fields. A charge of a stay, a request sent to the kitchen, something that happened: none of these is anyone.

This is the work of the **Records**. A Record is a value of several fields, as an `int` is a value of one: it has no identity, it lives inside what holds it, and it is copied whole from one place to another. [Tutorial Three, Lesson 11](Three-Composition.md#lesson-11-values-that-travel-together) gave a first look at them. This tutorial goes through all of it:

- **A Record is a value.** It is declared as an entity, `[Record]` in place of `[Entity]`, and created anywhere, without a Store. An entity, or another Record, holds it as one property.
- **It is read in place, and replaced whole.** `guest.Home.City` reads the city where it is, and copies nothing. To change it, you copy the value out, change the copy, and assign it back.
- **A `RecordBag` is a list of values.** One property, and its Records in their order: of one type, of several types, or of any type.
- **A Record is not an entity.** It never replaces one: it has no identity, no index, no link, and a change of one Record is a change of its whole host. Part 3 shows when **not** to use a Record.
- **A Record moves.** It is closer to a DTO or a message than to an object of your domain. This is why a Log holds nothing else, and why Tutorial Seven starts from here.

Each lesson is a test class of the project `GhostBodyObject.TutorialSix`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorials One to Five: the transactions, the Views and the Bodies, the static lambdas, the parts and the links, the indexes.

> **Warning: A Record is not a lighter entity**
>
> Records are fast to read, and it is tempting to put a list of them where entities belong: the lines of an order, the rooms of a hotel, the members of a team. Do not. A Record is a value that moves, not an object that lives: slow to change, impossible to index or to link, and a change of one is a change of its whole host. Keep this in mind through Parts 1 and 2, which show what Records do well: [Part 3](#part-3-when-not-to-use-a-record) shows what they do not do.

## What you will build

A hotel. Its guests and their addresses first; then their stays, what each one was charged, what happened during it, and the requests that it sends to the services of the hotel; the reviews of the guests, and the replies to them. Then one restaurant bill, written twice, to see what a Record cannot do; and a booking, a message that a Log keeps.

Five models, each in a namespace of its own with its own Repository, as in the tutorials before: `Guests` for Lessons 1 to 5, `Stays` for Lessons 6 to 11, `Reviews` for Lesson 12, `Bills` for Lessons 13 and 14, `Bookings` for Lesson 15.

**The values of the models come first**, each a value object ([Tutorial One, Lesson 18](One-TheBasics.md#lesson-18-give-each-value-its-type)), a value of a Record as of an entity. Those the models share are declared once, in the namespace of the tutorial; each model declares at its head the values that it alone has:

```csharp
namespace TutorialSix
{
    [ValueObject<GhostString>]
    internal interface VName { }

    [ValueObject<GhostString>]
    internal interface VLabel { }

    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VRoomNumber { }

    [ValueObject<DateTime>(ImplicitConversion = true)]
    internal interface VInstant { }

    [ValueObject<long>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VCents { }

    internal partial struct Cents
    {
        private static bool IsValid(long cents) => cents >= 0;
    }
}
```

Every declaration of the tutorial, and the lesson that shows it:

| Declaration in the model | What GBO guarantees | Lessons |
| ------------------------ | ------------------- | ------- |
| `[Record(n, version, max)]` on `RAddress` | A Record type: the class `Address`, created anywhere, and the struct `AddressView`, to read it | 1 |
| `RAddress Home` in an entity | A value, never absent: the default Record until written; read in place, replaced whole | 1 to 4 |
| `Option<RAddress> Billing` | A value that may be absent | 5 |
| `RAddress Address` in a Record | A Record in a Record, to any depth | 5 |
| `RecordBag<RCharge> Charges` | A list of values of one type, in their order, written directly | 6 to 8, 11 |
| `RecordBag<RCheckedIn, RRoomChanged, RCheckedOut> History` | A list of these types, up to eight, and no other | 9 |
| `RecordBag Requests` | A list of any Record, for several services | 10 |
| `RecordBag<RComment> Replies` in `RComment` | Records of their own type, to any depth | 12 |
| `[IncludeRecordModel]` on a `[GhostLog]` | One Record type, a fact in a Log and a value in a bag | 15 |

## Part 1: A Record is a value

### Lesson 1: Declare a Record

A value object holds one value: a city, an amount. An address is several of them that always go together: a street, a city, a postal code. A Record declares them once, as an entity is declared: an interface, whose name starts with `R`, its properties numbered by `[Property(n)]`, and `[Record(type, version, maxPropertyIdentifier)]` in place of `[Entity]`. Its type number belongs to the model, with those of the entities: an entity type and a Record type never share one. Two models may take the same numbers, as those of this tutorial do, as long as their Records never meet in one bag ([Lesson 10](#lesson-10-a-bag-of-any-record-the-messenger)).

```csharp
// [Record(type, version, maxPropertyIdentifier)]: a Record type, as [Entity] declares an entity type. Its interface starts with R:
// RAddress gives the class Address, to create the value, and the struct AddressView, to read it. Its number is one of the model: an
// entity type and a Record type never take the same one. Two models may take the same numbers, as those of this tutorial do, as long
// as their Records never meet in one bag (Lesson 10).
/// <summary>A postal address: several values that go together, without an identity of their own.</summary>
[Record(3, 1, 3)]
internal interface RAddress
{
    [Property(1)]
    VStreet Street { get; set; }

    [Property(2)]
    VCity City { get; set; }

    [Property(3)]
    VPostalCode PostalCode { get; set; }
}
```

An entity holds it as one property, as it holds a name or a date. The same type serves several entities: this is what a Record is for, a value that several places reuse:

```csharp
[Entity(1, 1, 6), Root, Removable, Enumerable]
internal interface MGuest
{
    [Property(1)]
    VName Name { get; set; }

    /// <summary>Where the guest lives: a Record is a property, never absent, an empty address until written.</summary>
    [Property(2)]
    RAddress Home { get; set; }

    /// <summary>Where the invoices go, when it is not home: an optional Record.</summary>
    [Property(3)]
    Option<RAddress> Billing { get; set; }

    /// <summary>Whom to call: a Record that holds a Record.</summary>
    [Property(4)]
    RContact Emergency { get; set; }

    /// <summary>A link to another entity, for the comparison of Lesson 2: a Record has none.</summary>
    [Property(5), Weak]
    Option<MHotel> Favorite { get; set; }

    /// <summary>How many times the home changed: kept by a rule of yours (Lesson 4).</summary>
    [Property(6)]
    int HomeChanges { get; set; }
}
```

```csharp
[Entity(2, 1, 2), Root, Enumerable]
internal interface MHotel
{
    [Property(1)]
    VName Name { get; set; }

    /// <summary>The same Record type, in another entity: a value that several places reuse.</summary>
    [Property(2)]
    RAddress Location { get; set; }
}
```

GBO writes two types for each Record: the class `Address`, to create a value and to write it, and the struct `AddressView`, to read it. An `Address` is created anywhere, on any thread, outside any transaction: it belongs to nothing. Assigned to a guest, its values are copied into the guest, and saved with it:

```csharp
[Fact]
public void HoldARecordInAnEntity()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-hold", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);

    // No transaction is open: a Record needs none to be born.
    var home = new Address { Street = "12 rue de Rivoli", City = "Paris", PostalCode = "75001" };

    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Guest { Name = "Ada", Home = home };
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var ada = GuestCollection.Single();
        Assert.True(ada.Home.City == "Paris" && ada.Home.Street == "12 rue de Rivoli");
        Assert.True(GuestViewCollection.Single().Home.PostalCode == "75001");
    }
}
```

A Record property is never absent. Until it is written, it reads as the default Record, each field at its default value, its texts empty:

```csharp
[Fact]
public void ReadTheDefaultRecordOfAPropertyNeverWritten()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-default", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ritz = new Hotel { Name = "Ritz" };
    Assert.True(ritz.Location.City.IsEmpty && ritz.Location.Street.IsEmpty);
}
```

One Record type, in two entity types:

```csharp
[Fact]
public void ReuseARecordInSeveralEntities()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-reuse", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var address = new Address { Street = "15 place Vendome", City = "Paris", PostalCode = "75001" };
    var ritz = new Hotel { Name = "Ritz", Location = address };
    var ada = new Guest { Name = "Ada", Home = address };

    Assert.True(ritz.Location.City == "Paris" && ada.Home.City == "Paris");
    transaction.Commit();
}
```

### Lesson 2: An entity or a Record

This is the question of this tutorial. An entity is **someone**: it exists on its own, it is found, it is linked, and two entities of the same values stay two. A Record is **something**: a value, like a number, that exists only in what holds it. Two Records of the same values are the same value, and giving a Record to a second entity gives it a copy.

A hotel that two guests like is one hotel. A link designates it, and when it changes its name, both guests see the new one:

```csharp
[Fact]
public void ShareAnEntityByALink()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-link", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ritz = new Hotel { Name = "Ritz" };
    var ada = new Guest { Name = "Ada", Favorite = ritz };
    var bob = new Guest { Name = "Bob", Favorite = ritz };

    ritz.Name = "The Ritz";
    Assert.True(ada.Favorite.Get().Name == "The Ritz" && bob.Favorite.Get().Name == "The Ritz");
}
```

An address that two guests share is two copies of a value. When one guest moves, the other stays where they are:

```csharp
[Fact]
public void CopyARecordByAnAssignment()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-copy", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris" } };
    var bob = new Guest { Name = "Bob", Home = ada.Home };

    // Ada moves: Bob keeps the value that he received.
    ada.Home = new Address { Street = "3 quai Saint-Antoine", City = "Lyon" };
    Assert.True(ada.Home.City == "Lyon" && bob.Home.City == "Paris");
}
```

A Record has no life of its own. There is no collection of addresses: an address is found through what holds it, and it leaves with its host, with nothing left to clean:

```csharp
[Fact]
public void LiveAndLeaveWithItsHost()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-host", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
        transaction.Commit();
    }
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // A collection of guests exists; a collection of addresses does not: an address is found through what holds it.
        Assert.Equal(1, GuestViewCollection.Count(static guest => guest.Home.City == "Paris"));

        GuestCollection.Single().Remove();
        transaction.Commit();
    }
    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Empty(GuestCollection);
}
```

The whole difference, in one table:

| | An entity | A Record |
| - | --------- | -------- |
| It is | Someone, with an identity | Something: a value, as a number |
| Declared by | `[Entity]`, `MGuest` | `[Record]`, `RAddress` |
| It lives | On its own, in the Store | In its host, an entity or another Record; in your hands until it is assigned |
| It is born | In a transaction | Anywhere, on any thread, without a Store |
| Two of the same values | Are two entities | Are the same value |
| Given to a second holder | A link: both see one entity | A copy: each one has its own |
| It is found | By its collection, an index, a link | Through its host |
| A link to it | `[Strong]`, `[Weak]` | Never |
| An index on it | Yes | Never |
| It changes | Field by field | Replaced whole, by its copy |
| It conflicts | Alone | As a change of its host |
| Its View | Follows its transaction: it reads the entity as it is now | A value: it keeps what it read |
| It leaves | By `Remove()`, with its cascade | With its host, or when it is replaced |

### Lesson 3: Read a Record in place

The property of a Record gives its View, on a Body as on a View: an `AddressView`, read-only. Reading it copies nothing: `guest.Home.City` reads the city where it is, as `guest.Name` reads the name. This is where a Record is strong: a query over thousands of guests reads their addresses without a copy, faster than in a part ([Lesson 14](#lesson-14-fast-to-read-slow-to-change)):

```csharp
[Fact]
public void QueryTheEntitiesByARecord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-query", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
        new Guest { Name = "Bob", Home = new Address { City = "Lyon" } };
        new Guest { Name = "Eve", Home = new Address { City = "Paris" } };
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(2, GuestViewCollection.Count(static guest => guest.Home.City == "Paris"));
        Assert.Equal(["Ada", "Eve"], GuestViewCollection.Where(static guest => guest.Home.City == "Paris").Select(static guest => guest.Name.ToString()).ToArray());
    }
}
```

Reading a Record allocates nothing, at any depth:

```csharp
[Fact]
public void ReadWithoutAnAllocation()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-allocation", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Guest { Name = "Ada", Home = new Address { City = "Paris" }, Emergency = new Contact { Address = new Address { City = "London" } } };
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var ada = GuestViewCollection.Single();
        int letters = ada.Home.City.Length;

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1_000; i++)
            letters += ada.Home.City.Length + ada.Emergency.Address.City.Length;

        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
        Assert.Equal(11_005, letters);
    }
}
```

The View of a Record is a **value**, unlike the View of an entity, which reads its entity as it is now in its transaction. It keeps what it read: a later change of the guest changes the guest, not the value read before:

```csharp
[Fact]
public void KeepTheValueThatAViewRead()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-value", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
    var guest = ada.ToView();
    var before = ada.Home;

    ada.Home = new Address { City = "Lyon" };
    ada.Name = "A name long enough to change the guest a lot more";

    Assert.True(guest.Home.City == "Lyon" && before.City == "Paris");
}
```

It stays readable after its transaction, on another thread, for as long as you hold it:

```csharp
[Fact]
public async Task ReadAViewAfterItsTransactionOnAnotherThread()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-travel", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
        transaction.Commit();
    }

    AddressView home;
    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
        home = GuestViewCollection.Single().Home;

    Assert.True(await Task.Run(() => home.City == "Paris"));
}
```

### Lesson 4: Change a Record by its copy

A Record is replaced, never edited in place. `guest.Home.City = "Lyon"` does not compile: `guest.Home` is a View. A Body of a Record is never bound to its place in an entity, because a value has no place: it is where it was copied.

To replace it whole, assign a new value. Whole means whole: the fields that the new value does not give are empty, not kept:

```csharp
[Fact]
public void ReplaceARecordWhole()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-replace", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris", PostalCode = "75001" } };
    ada.Home = new Address { City = "Lyon" };

    // Whole: the fields that the new value does not give are empty, not kept.
    Assert.True(ada.Home.City == "Lyon" && ada.Home.Street.IsEmpty);

    // The default value empties it.
    ada.Home = default;
    Assert.True(ada.Home.City.IsEmpty);
}
```

To change one field, take a copy, `ToBody()`, change the copy, and assign it. Nothing changes in the guest before the assignment:

```csharp
[Fact]
public void ChangeOneFieldByACopy()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-copy", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada", Home = new Address { Street = "12 rue de Rivoli", City = "Paris" } };

    var home = ada.Home.ToBody();
    home.Street = "14 rue de Rivoli";
    Assert.True(ada.Home.Street == "12 rue de Rivoli");

    ada.Home = home;
    Assert.True(ada.Home.Street == "14 rue de Rivoli" && ada.Home.City == "Paris");
}
```

The copy belongs to you. A later change of it does not follow into the guest that received it:

```csharp
[Fact]
public void KeepYourCopyApartFromTheHost()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-apart", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var address = new Address { City = "Paris" };
    var ada = new Guest { Name = "Ada", Home = address };

    address.City = "Lyon";
    var bob = new Guest { Name = "Bob", Home = address };

    Assert.True(ada.Home.City == "Paris" && bob.Home.City == "Lyon");
}
```

The same value is no change: no write, and no call of OnHomeChanged, a method of yours that GBO calls after each real change of the Record:

```csharp
// A business rule of the developer, for Lesson 4: called on a real change of the Record only. It keeps HomeChanges, a property of the
// model, never a field of the Body (Tutorial One, Lesson 11).
internal partial class Guest
{
    partial void OnHomeChanged() => HomeChanges++;
}
```

```csharp
[Fact]
public void SeeNoChangeInTheSameValue()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-same", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada", Home = new Address { City = "Paris" } };
    Assert.Equal(1, ada.HomeChanges);

    ada.Home = new Address { City = "Paris" };
    Assert.Equal(1, ada.HomeChanges);

    ada.Home = new Address { City = "Lyon" };
    Assert.Equal(2, ada.HomeChanges);
}
```

These steps say what happens: the value is copied out, then copied back. This is why a Record is slower to change than a field of an entity, and why it suits a value that is read often and changed rarely. [Lesson 14](#lesson-14-fast-to-read-slow-to-change) measures it.

### Lesson 5: An optional Record, and a Record in a Record

`Option<RAddress>` may be absent, which the empty default of a plain Record cannot say. A guest without a billing address is not a guest with an empty one:

```csharp
[Fact]
public void TellAnAbsentRecordFromAnEmptyOne()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-option", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest { Name = "Ada" };
    Assert.False(ada.Billing.Any());

    ada.Billing = new Address { City = "Geneva" };
    Assert.True(ada.Billing.TryGet(out var billing) && billing.City == "Geneva");

    // An empty address is an address: present, and empty.
    ada.Billing = new Address();
    Assert.True(ada.Billing.Any());

    ada.Billing = Option.None;
    Assert.False(ada.Billing.Any());
}
```

A Record holds Records, to any depth:

```csharp
/// <summary>Someone to call: a Record holds Records too.</summary>
[Record(4, 1, 3)]
internal interface RContact
{
    [Property(1)]
    VName Name { get; set; }

    [Property(2)]
    VPhone Phone { get; set; }

    [Property(3)]
    RAddress Address { get; set; }
}
```

It is read straight through:

```csharp
[Fact]
public void ReadARecordInARecord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-read", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest {
        Name = "Ada",
        Emergency = new Contact { Name = "Charles", Phone = "+33 1 40 20 50 50", Address = new Address { City = "London" } }
    };

    Assert.True(ada.Emergency.Name == "Charles" && ada.Emergency.Address.City == "London");
    Assert.True(ada.ToView().Emergency.Address.City == "London");
}
```

It is changed level by level: each level is copied out, then assigned back to the level above:

```csharp
[Fact]
public void ChangeARecordInARecord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-change", StoreAccessMode.ReadWrite);
    using var hotel = new GuestRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Guest {
        Name = "Ada",
        Emergency = new Contact { Name = "Charles", Address = new Address { City = "London" } }
    };

    var emergency = ada.Emergency.ToBody();
    var address = emergency.Address.ToBody();
    address.City = "Cambridge";
    emergency.Address = address;
    ada.Emergency = emergency;

    Assert.True(ada.Emergency.Name == "Charles" && ada.Emergency.Address.City == "Cambridge");
}
```

> **Tip: Few Records per entity**
>
> Each Record property is a value that several places reuse, or that goes together. An entity with many of them says something else: its concerns are not separated, and the entities that share a Record type change together when it changes. If a group of fields belongs to one entity only, plain properties are simpler.

## Part 2: The RecordBag

### Lesson 6: A bag of one type

A list of values that an entity owns, in their order: the charges of a stay. A collection of Records does not exist: a list of Records is a `RecordBag`, one property of the entity. `RecordBag<RCharge>` holds this type alone:

```csharp
/// <summary>A charge of a stay: a value that the stay owns, added once, then read with the others.</summary>
[Record(2, 1, 3)]
internal interface RCharge
{
    [Property(1)]
    VLabel Label { get; set; }

    [Property(2)]
    VCents Amount { get; set; }

    [Property(3)]
    VInstant At { get; set; }
}
```

```csharp
[Entity(1, 1, 4), Root, Enumerable]
internal interface MStay
{
    [Property(1)]
    VName Guest { get; set; }

    /// <summary>What the stay was charged: a bag of one type, its Records in their order.</summary>
    [Property(2)]
    RecordBag<RCharge> Charges { get; set; }

    /// <summary>What happened: a bag of these three types, and no other.</summary>
    [Property(3)]
    RecordBag<RCheckedIn, RRoomChanged, RCheckedOut> History { get; set; }

    /// <summary>The requests to the services: a bag of any Record.</summary>
    [Property(4)]
    RecordBag Requests { get; set; }
}
```

Unlike a Record property, a bag is written directly. `stay.Charges.Add(charge)` changes the stay, without an assignment. It is read as a Record, in place: `Enumerate<ChargeView>()` gives the Views of its Records, in their order:

```csharp
[Fact]
public void AddRecordsAndReadThemInTheirOrder()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-add", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var stay = new Stay { Guest = "Ada" };
        Assert.Equal(0, stay.Charges.Count());

        // No assignment: the bag belongs to the stay, and its Add changes the stay.
        stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000, At = Monday });
        stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500, At = Monday.AddHours(1) });
        stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500, At = Monday.AddHours(2) });

        Assert.Equal(3, stay.Charges.Count());
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var stay = StayViewCollection.Single();
        Assert.Equal(["Room", "Breakfast", "Parking"], stay.Charges.Enumerate<ChargeView>().Select(static charge => charge.Label.ToString()).ToArray());

        long total = 0;
        foreach (var charge in stay.Charges.Enumerate<ChargeView>())
            total += charge.Amount.Value;
        Assert.Equal(22_000, total);
    }
}
```

> **Warning: `Add` needs `using GhostBodyObject.Runtime.Logical.Model`**
>
> `Add`, `ReplaceAt`, `Remove`, `RemoveAll(record)`, `IndexOf(record, out index)` and `Contains(record)` of a bag of given types, and the handlers `Enumerate(h1, ...)` of every bag, are extension methods: without this `using`, `stay.Charges.Add(...)` does not compile, and the error says that `RecordBag<ChargeView>` has no `Add`.

The model names the interfaces, your code names their Views. What the model declares `RecordBag<RCharge>` is a `RecordBag<ChargeView>` in your code, and the View of a bag is a `RecordBagView`:

| In the model | Through a Body | Through a View |
| ------------ | -------------- | -------------- |
| `RAddress Home` | `AddressView` | `AddressView` |
| `Option<RAddress> Billing` | `Option<AddressView>` | `Option<AddressView>` |
| `RecordBag<RCharge> Charges` | `RecordBag<ChargeView>` | `RecordBagView<ChargeView>` |
| `RecordBag<RCheckedIn, RRoomChanged, RCheckedOut> History` | `RecordBag<CheckedInView, RoomChangedView, CheckedOutView>` | `RecordBagView<CheckedInView, RoomChangedView, CheckedOutView>` |
| `RecordBag Requests` | `RecordBag` | `RecordBagView` |

`Enumerate<T>()` is a sequence of GBO, with the operators of its queries: `Where`, `Any`, `FirstOrNone`, `Select`, `Count`, `ToArray`:

```csharp
[Fact]
public void QueryTheRecordsOfABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-query", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
    stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

    var charges = stay.ToView().Charges;
    Assert.True(charges.Enumerate<ChargeView>().Any(static charge => charge.Amount > 10_000));
    Assert.Equal(1, charges.Enumerate<ChargeView>().Where(static charge => charge.Amount < 10_000).Count());
    Assert.True(charges.Enumerate<ChargeView>().FirstOrNone(static charge => charge.Label == "Breakfast").TryGet(out var breakfast) && breakfast.Amount == 2_500);
}
```

The positions are those of the additions, from 0 to `Count() - 1`. `TryGetAt` gives the Record of a position; out of the bag, it throws, as a span does:

```csharp
[Fact]
public void ReadARecordByItsPosition()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-position", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
    stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

    Assert.True(stay.Charges.TryGetAt(1, out ChargeView second) && second.Label == "Breakfast");
    Assert.Throws<ArgumentOutOfRangeException>(() => stay.Charges.TryGetAt(2, out ChargeView _));
}
```

`Size` tells what a bag adds to its entity, in bytes:

```csharp
[Fact]
public void TellTheSizeOfABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-size", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    Assert.Equal(0, stay.Charges.Size);

    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
    int one = stay.Charges.Size;
    stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500 });
    Assert.True(one > 0 && stay.Charges.Size > one);
}
```

### Lesson 7: Find and take a Record

A Record has no identity: it is found by its value, by its type, or by its position. **Equal** means the same type and the same values, in the same version of the type, compared exactly as they are kept: the same moment of another `Kind` is another value:

```csharp
[Fact]
public void FindARecordByItsValue()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-value", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });
    stay.Charges.Add(new Charge { Label = "Breakfast", Amount = 2_500 });

    Assert.True(stay.Charges.Contains(new Charge { Label = "Breakfast", Amount = 2_500 }));
    Assert.False(stay.Charges.Contains(new Charge { Label = "Breakfast", Amount = 2_000 }));

    Assert.True(stay.Charges.IndexOf(new Charge { Label = "Breakfast", Amount = 2_500 }, out int at));
    Assert.Equal(1, at);

    // The View of the bag searches as the bag does.
    Assert.True(stay.ToView().Charges.Contains(new Charge { Label = "Room", Amount = 18_000 }));

    // Compared exactly as they are kept: the same moment of another Kind is another value.
    var noon = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    stay.Charges.Add(new Charge { Label = "Parking", At = noon });
    Assert.False(stay.Charges.Contains(new Charge { Label = "Parking", At = DateTime.SpecifyKind(noon, DateTimeKind.Unspecified) }));
}
```

`IndexOf<T>` gives the position of the first Record of a type. A position gives its Record only when it is of the type asked:

```csharp
[Fact]
public void FindARecordByItsType()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-type", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

    Assert.True(stay.Requests.IndexOf<BreakfastView>(out int at) && at == 1);
    Assert.False(stay.Requests.IndexOf<WakeUpView>(out _));

    // A position gives its Record only when it is of the type asked.
    Assert.False(stay.Requests.TryGetAt(0, out BreakfastView _));
    Assert.True(stay.Requests.TryGetAt(1, out BreakfastView breakfast) && breakfast.Persons == 2);
}
```

To take a Record of a type, find it, read it, remove it. Its View stays a value after the removal:

```csharp
[Fact]
public void TakeARecordOutOfABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-take", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

    Assert.True(stay.Requests.IndexOf<BreakfastView>(out int at));
    Assert.True(stay.Requests.TryGetAt(at, out BreakfastView breakfast));
    stay.Requests.RemoveAt(at);

    Assert.Equal(2, breakfast.Persons.Value);
    Assert.Equal(1, stay.Requests.Count());
}
```

To take the Records in their order, read the first one and remove it, until the bag is empty:

```csharp
[Fact]
public void TakeTheRecordsInTheirOrder()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-queue", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
    stay.Requests.Add(new CleanRoom { Room = 14 });

    var rooms = new List<int>();
    while (stay.Requests.Count() > 0)
    {
        if (stay.Requests.TryGetAt(0, out CleanRoomView clean))
            rooms.Add(clean.Room.Value);
        stay.Requests.RemoveAt(0);
    }

    Assert.Equal([12, 14], rooms);
}
```

Finding reads the bag from its start. A bag is a short list that an entity carries, not a table to search.

### Lesson 8: Change a bag

Six writes, each one a change of the entity that holds the bag. `RemoveAt` a position, `Remove` the first Record equal to a value, `RemoveAll` of a value or of a type, `Clear`. A removal moves the Records after it down by one place:

```csharp
[Fact]
public void RemoveRecordsFromABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-remove", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    foreach (int room in new[] { 12, 14, 12, 16, 12 })
        stay.Requests.Add(new CleanRoom { Room = room });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });

    // A position: the Records after it move down by one place.
    stay.Requests.RemoveAt(1);
    Assert.True(stay.Requests.TryGetAt(1, out CleanRoomView next) && next.Room == 12);

    // The first equal value; false when none is.
    Assert.True(stay.Requests.Remove(new CleanRoom { Room = 16 }));
    Assert.False(stay.Requests.Remove(new CleanRoom { Room = 99 }));

    // Every equal value: their number.
    Assert.Equal(3, stay.Requests.RemoveAll(new CleanRoom { Room = 12 }));

    // Every Record of a type.
    stay.Requests.Add(new CleanRoom { Room = 14 });
    Assert.Equal(1, stay.Requests.RemoveAll<BreakfastView>());
    Assert.Equal(1, stay.Requests.Count());

    stay.Requests.Clear();
    Assert.True(stay.Requests.Count() == 0 && stay.Requests.Size == 0);
}
```

`ReplaceAt` puts another value at a position, of any type that the bag accepts:

```csharp
[Fact]
public void ReplaceARecordOfABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-replace", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.ReplaceAt(0, new Breakfast { Room = 12, Persons = 2 });

    Assert.Equal(1, stay.Requests.Count());
    Assert.True(stay.Requests.TryGetAt(0, out BreakfastView breakfast) && breakfast.Persons == 2);

    Assert.Throws<ArgumentOutOfRangeException>(() => stay.Requests.ReplaceAt(1, new CleanRoom { Room = 14 }));
    Assert.Throws<ArgumentOutOfRangeException>(() => stay.Requests.RemoveAt(1));
}
```

A Record of a bag is a value, as a Record property: it is not edited in place. To change one, take its copy, change it, and put it back:

```csharp
[Fact]
public void ChangeARecordOfABagByItsCopy()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-copy", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

    Assert.True(stay.Charges.TryGetAt(0, out ChargeView room));
    var corrected = room.ToBody();
    corrected.Amount = 16_000;
    stay.Charges.ReplaceAt(0, corrected);

    Assert.True(stay.Charges.TryGetAt(0, out ChargeView changed) && changed.Amount == 16_000);

    // The View read before is a value: it keeps the old amount.
    Assert.Equal(18_000, room.Amount.Value);
}
```

This is for the rare correction. Records that are corrected often, one by one, are entities ([Lesson 13](#lesson-13-what-a-record-is-not)).

A bag belongs to its entity: it is written in a transaction that writes:

```csharp
[Fact]
public void RefuseAWriteInAReadOnlyTransaction()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-readonly", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Stay { Guest = "Ada" };
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var stay = StayCollection.Single();
        Assert.Throws<InvalidOperationException>(() => stay.Charges.Add(new Charge { Label = "Room" }));
    }
}
```

### Lesson 9: A bag of several types

What happened during a stay is a list of several kinds of values, whose order matters:

```csharp
/// <summary>What happened to a stay: the three Record types of its history (Lesson 9).</summary>
[Record(3, 1, 2)]
internal interface RCheckedIn
{
    [Property(1)]
    VRoomNumber Room { get; set; }

    [Property(2)]
    VInstant At { get; set; }
}
```

```csharp
[Record(4, 1, 3)]
internal interface RRoomChanged
{
    [Property(1)]
    VRoomNumber From { get; set; }

    [Property(2)]
    VRoomNumber To { get; set; }

    [Property(3)]
    VInstant At { get; set; }
}
```

```csharp
[Record(5, 1, 1)]
internal interface RCheckedOut
{
    [Property(1)]
    VInstant At { get; set; }
}
```

`RecordBag<RCheckedIn, RRoomChanged, RCheckedOut>` holds these types, and no other, up to eight. The constraint is checked by the compiler: adding another type does not compile, and asking for it stops the build with GBO050. It is a constraint of the code: converted to `RecordBag`, the same bag takes any Record ([Lesson 11](#lesson-11-a-free-bag-and-the-copies-of-a-bag)). Its Records stay in the order of their additions, whatever their types: this is what one list per type would lose.

Each type is read on its own, and the bag counts them all:

```csharp
[Fact]
public void ReadEachTypeOfABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-types", StoreAccessMode.ReadWrite);
    using var hotel = NewHotel(store);
    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var history = StayViewCollection.Single().History;
        Assert.Equal(4, history.Count());
        Assert.Equal(2, history.Count<RoomChangedView>());
        Assert.Equal([14, 16], history.Enumerate<RoomChangedView>().Select(static change => change.To.Value).ToArray());
    }
}
```

One walk, `Enumerate(h1, h2, h3)`, gives each Record to the handler of its type, in their order. The handlers are `static` lambdas, which capture nothing: up to four values are passed before them, as in every query of GBO:

```csharp
[Fact]
public void WalkABagByAHandlerPerType()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-walk", StoreAccessMode.ReadWrite);
    using var hotel = NewHotel(store);
    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var story = new List<string>();
        StayViewCollection.Single().History.Enumerate(story,
            static (List<string> story, CheckedInView entry) => story.Add($"in {entry.Room.Value}"),
            static (List<string> story, RoomChangedView change) => story.Add($"to {change.To.Value}"),
            static (List<string> story, CheckedOutView leaving) => story.Add("out"));

        Assert.Equal(["in 12", "to 14", "to 16", "out"], story);
    }
}
```

The types without a handler are skipped:

```csharp
[Fact]
public void SkipTheTypesWithoutAHandler()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-skip", StoreAccessMode.ReadWrite);
    using var hotel = NewHotel(store);
    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var rooms = new List<int>();
        StayViewCollection.Single().History.Enumerate(rooms, static (List<int> rooms, RoomChangedView change) => rooms.Add(change.To.Value));
        Assert.Equal([14, 16], rooms);
    }
}
```

### Lesson 10: A bag of any Record, the messenger

`RecordBag`, without types, holds any Record: a messenger for several services. The stay leaves its requests in one place. Housekeeping reads the rooms to clean, the kitchen the breakfasts, and each one leaves the others:

```csharp
/// <summary>The messages to the services of the hotel: a bag of any Record carries them (Lesson 10).</summary>
[Record(6, 1, 1)]
internal interface RCleanRoom
{
    [Property(1)]
    VRoomNumber Room { get; set; }
}
```

```csharp
[Record(7, 1, 2)]
internal interface RBreakfast
{
    [Property(1)]
    VRoomNumber Room { get; set; }

    [Property(2)]
    VPersons Persons { get; set; }
}
```

```csharp
[Record(8, 1, 2)]
internal interface RWakeUp
{
    [Property(1)]
    VRoomNumber Room { get; set; }

    [Property(2)]
    VInstant At { get; set; }
}
```

No property of the model names them: the Repository includes them by their names, as it includes its entities. A bag of given types needs none of this: its property names them.

```csharp
// The messages are named by no property of the model: the Repository includes them by their names.
[GhostRepository, IncludeEntityModel(nameof(MStay)), IncludeEntityModel(nameof(RCleanRoom)), IncludeEntityModel(nameof(RBreakfast)),
    IncludeEntityModel(nameof(RWakeUp)), IncludeEntityModel(nameof(RDayPlan))]
internal interface MStayRepository
{
}
```

`Count()` counts them all; `Enumerate<T>()` and `Count<T>()` see one type:

```csharp
[Fact]
public void LetEachServiceReadItsTypes()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-services", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
    stay.Requests.Add(new WakeUp { Room = 12, At = new DateTime(2026, 11, 3, 7, 0, 0, DateTimeKind.Utc) });

    // The bag counts everything; each service sees its own.
    Assert.Equal(3, stay.Requests.Count());
    Assert.Equal(1, stay.Requests.Count<CleanRoomView>());
    Assert.Equal([2], stay.Requests.Enumerate<BreakfastView>().Select(static breakfast => breakfast.Persons.Value).ToArray());
}
```

A service takes its Records out, and leaves the others to the next one:

```csharp
[Fact]
public void TakeYourTypesAndLeaveTheOthers()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-leave", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
    stay.Requests.Add(new CleanRoom { Room = 14 });

    Assert.Equal(2, stay.Requests.RemoveAll<CleanRoomView>());
    Assert.Equal(1, stay.Requests.Count());
    Assert.True(stay.Requests.TryGetAt(0, out BreakfastView _));
}
```

A Record can carry such a bag: one message that holds others, sent as a whole:

```csharp
/// <summary>The instructions of a day, one message that carries others: a Record holds a bag.</summary>
[Record(9, 1, 2)]
internal interface RDayPlan
{
    [Property(1)]
    VInstant Day { get; set; }

    [Property(2)]
    RecordBag Instructions { get; set; }
}
```

```csharp
[Fact]
public void CarryMessagesInARecord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-carry", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);

    var plan = new DayPlan { Day = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc) };
    plan.Instructions.Add(new CleanRoom { Room = 12 });
    plan.Instructions.Add(new Breakfast { Room = 12, Persons = 2 });

    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(plan);

    Assert.True(stay.Requests.TryGetAt(0, out DayPlanView received));
    Assert.Equal(2, received.Instructions.Count());
    Assert.Equal(1, received.Instructions.Count<CleanRoomView>());
}
```

The handlers work here too, for the types that a service knows:

```csharp
[Fact]
public void WalkABagOfAnyRecordByAHandlerPerType()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-walk", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new Breakfast { Room = 12, Persons = 2 });
    stay.Requests.Add(new WakeUp { Room = 14 });

    // The kitchen does not know the wake-up calls: skipped, and still in the bag.
    var rooms = new List<int>();
    stay.Requests.Enumerate(rooms,
        static (List<int> rooms, CleanRoomView clean) => rooms.Add(clean.Room.Value),
        static (List<int> rooms, BreakfastView breakfast) => rooms.Add(breakfast.Room.Value));

    Assert.Equal([12, 12], rooms);
    Assert.Equal(3, stay.Requests.Count());
}
```

A service forwards what it does not read: a Record of a type or of a version that it cannot read is counted by `Count()`, skipped by `Enumerate`, never given by `TryGetAt`, and kept by every copy of the bag:

```csharp
[Fact]
public void ForwardWhatYouDoNotRead()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-forward", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Requests.Add(new CleanRoom { Room = 12 });
    stay.Requests.Add(new WakeUp { Room = 14 });

    var forwarded = stay.ToView().Requests.ToBody();
    Assert.Equal(0, forwarded.Count<BreakfastView>());
    Assert.True(forwarded.Count() == 2 && forwarded.TryGetAt(1, out WakeUpView call) && call.Room == 14);
}
```

> **Warning: Records that travel together take distinct numbers**
>
> A bag tells its Records apart by their number and their version, never by their names. The numbers of this tutorial are those of each model: `RCharge` and `RComment` both take 2, which is safe as long as they never meet. In one bag, nothing refuses them, and each one reads as the other. Records that travel together in a bag take distinct numbers, across models:

```csharp
[Fact]
public void TakeDistinctNumbersForTheRecordsThatTravelTogether()
{
    var messages = new RecordBag();
    messages.Add(new Reviews.Comment { Author = "Ada" });

    Assert.Equal(1, messages.Count<ChargeView>());
}
```

### Lesson 11: A free bag, and the copies of a bag

`stay.Charges` is bound to its stay: its writes change the stay. A bag is also **free**: `new RecordBag<ChargeView>()` belongs to nothing, and needs no transaction. Filled, then assigned, it is copied into its host once. This is the way to write many Records at once:

```csharp
[Fact]
public void FillAFreeBagThenAssignIt()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-free", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);

    var charges = new RecordBag<ChargeView>();
    charges.Add(new Charge { Label = "Room", Amount = 18_000 });
    charges.Add(new Charge { Label = "Parking", Amount = 1_500 });

    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
    var stay = new Stay { Guest = "Ada", Charges = charges };

    Assert.Equal(2, stay.Charges.Count());
    transaction.Commit();
}
```

The View of a bag, `stay.ToView().Charges`, a `RecordBagView<ChargeView>`, is a value, as the View of a Record: a later change of the stay does not change it:

```csharp
[Fact]
public void KeepTheValueThatAViewOfABagRead()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-value", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

    var before = stay.ToView().Charges;
    stay.Charges.Add(new Charge { Label = "Parking", Amount = 1_500 });

    Assert.True(before.Count() == 1 && stay.Charges.Count() == 2);
}
```

Its `ToBody()` gives a free copy, to change and to assign, to the same stay or to another one:

```csharp
[Fact]
public void CopyABag()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-copy", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var ada = new Stay { Guest = "Ada" };
    ada.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

    var copy = ada.ToView().Charges.ToBody();
    copy.Add(new Charge { Label = "Parking", Amount = 1_500 });
    Assert.Equal(1, ada.Charges.Count());

    var bob = new Stay { Guest = "Bob", Charges = copy };
    ada.Charges.Clear();

    Assert.True(ada.Charges.Count() == 0 && bob.Charges.Count() == 2);
}
```

A free bag holds any Record too, without a Store:

```csharp
[Fact]
public void HoldAnyRecordInAFreeBag()
{
    var messages = new RecordBag();
    messages.Add(new CleanRoom { Room = 12 });
    messages.Add(new Breakfast { Room = 12, Persons = 2 });

    Assert.Equal(2, messages.Count());
    Assert.Equal(1, messages.Count<BreakfastView>());
}
```

A free bag of several types is named by the Views of its types:

```csharp
[Fact]
public void FillAFreeBagOfSeveralTypes()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-types", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);

    var history = new RecordBag<CheckedInView, RoomChangedView, CheckedOutView>();
    history.Add(new CheckedIn { Room = 12 });
    history.Add(new RoomChanged { From = 12, To = 14 });

    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);
    var stay = new Stay { Guest = "Ada", History = history };

    Assert.True(stay.History.Count() == 2 && stay.History.TryGetAt(1, out RoomChangedView change) && change.To == 14);
}
```

The types of a bag are a constraint of the code. A bag of given types converts to `RecordBag` without a cast: the same bag, which then takes any Record. `new RecordBag<ChargeView>(bag)` puts the types back, and a View does the same between `RecordBagView` and `RecordBagView<ChargeView>`. This is what a method that reads any bag takes:

```csharp
[Fact]
public void TakeTheTypesOffABagAndPutThemBack()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-convert", StoreAccessMode.ReadWrite);
    using var hotel = new StayRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var stay = new Stay { Guest = "Ada" };
    stay.Charges.Add(new Charge { Label = "Room", Amount = 18_000 });

    // Without its types, the bag of the stay takes any Record: the compiler no longer checks it.
    RecordBag any = stay.Charges;
    any.Add(new CleanRoom { Room = 12 });
    Assert.Equal(2, stay.Charges.Count());

    var charges = new RecordBag<ChargeView>(any);
    Assert.Equal(1, charges.Count<ChargeView>());

    // The same for its View: a method that reads any bag takes it, without its types.
    RecordBagView read = stay.ToView().Charges;
    Assert.Equal(1, new RecordBagView<ChargeView>(read).Count<ChargeView>());
}
```

> **Warning: A bag stays in its method**
>
> A `RecordBag` is a `ref struct`, as a `GhostString`: a parameter or a local value, never a field of a class. Its View, a plain struct, a `RecordBagView` or a `RecordBagView<ChargeView>`, may be kept, in a field, in a list, on another thread.

### Lesson 12: Records at any depth

A Record holds a bag of its own type: a comment and its replies, and their replies. All of it stays in the entity:

```csharp
/// <summary>A comment, and the replies to it: a bag of its own type, to any depth.</summary>
[Record(2, 1, 3)]
internal interface RComment
{
    [Property(1)]
    VName Author { get; set; }

    [Property(2)]
    VCommentText Text { get; set; }

    [Property(3)]
    RecordBag<RComment> Replies { get; set; }
}
```

```csharp
[Entity(1, 1, 2), Root, Enumerable]
internal interface MReview
{
    [Property(1)]
    VName Hotel { get; set; }

    [Property(2)]
    RecordBag<RComment> Comments { get; set; }
}
```

The review is read as a whole, in place, by a method that follows the replies:

```csharp
// The comments of a bag, and the replies to them, at any depth.
private static int CountAll(RecordBagView<CommentView> comments)
{
    int count = comments.Count();
    foreach (var comment in comments.Enumerate<CommentView>())
        count += CountAll(comment.Replies);
    return count;
}
```

```csharp
[Fact]
public void ReadARecordAtAnyDepth()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-read", StoreAccessMode.ReadWrite);
    using var hotel = new ReviewRepository(store);

    var reply = new Comment { Author = "Bob", Text = "Which room?" };
    reply.Replies.Add(new Comment { Author = "Ada", Text = "Number 12." });
    var comment = new Comment { Author = "Ada", Text = "A wonderful view." };
    comment.Replies.Add(reply);

    using (var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Review { Hotel = "Ritz" }.Comments.Add(comment);
        transaction.Commit();
    }

    using (hotel.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(3, CountAll(ReviewViewCollection.Single().Comments));
}
```

A bag is written directly through a Body, of an entity or of a Record. A Record taken from a bag is a View, whose bag has no writes: to change its replies, take its copy, add to the bag of the copy, and put it back with `ReplaceAt`:

```csharp
[Fact]
public void ChangeARecordBelowTheBagOfTheEntity()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-change", StoreAccessMode.ReadWrite);
    using var hotel = new ReviewRepository(store);
    using var transaction = hotel.OpenTransaction(StoreAccessMode.ReadWrite);

    var review = new Review { Hotel = "Ritz" };
    review.Comments.Add(new Comment { Author = "Ada", Text = "A wonderful view." });

    Assert.True(review.Comments.TryGetAt(0, out CommentView comment));
    var edited = comment.ToBody();
    edited.Replies.Add(new Comment { Author = "Bob", Text = "Which room?" });
    review.Comments.ReplaceAt(0, edited);

    Assert.Equal(2, CountAll(review.ToView().Comments));

    // The View taken before is a value: it has no reply.
    Assert.Equal(0, comment.Replies.Count());
}
```

A deep change copies each level, and rewrites each bag on its way. A tree that changes often is made of entities ([Tutorial Two, Lesson 7](Two-Relationships.md#lesson-7-build-a-tree)).

### Every function of a bag

| Member | On | What it does |
| ------ | -- | ------------ |
| `new RecordBag()` | | A free bag of any Record |
| `new RecordBag<T1, ...>()` | | A free bag of these types, up to eight, named by their Views |
| `Count()` | Body, View | Its Records, of every type |
| `Count<T>()` | Body, View | Its Records of the type of `T`, in a version that it reads |
| `Size` | Body, View | The bytes that it adds to its entity: 0 when empty |
| `Enumerate<T>()` | Body, View | The Views of the type of `T`, in a version that it reads, in their order, with the operators of a query |
| `Enumerate(h1, ..., h8)` | Body, View | One walk, one handler per type, up to four values before them |
| `TryGetAt<T>(index, out record)` | Body, View | The Record at a position, when it is of the type of `T`; a position out of the bag throws |
| `IndexOf<T>(out index)` | Body, View | The position of the first Record of a type |
| `IndexOf(record, out index)` | Body, View | The position of the first Record equal to a value |
| `Contains(record)` | Body, View | A Record equal to a value |
| `ToBody()` | View | A free copy of the bag |
| `Add(record)` | Body | Copies a Record at the end |
| `RemoveAt(index)` | Body | Removes the Record at a position; a position out of the bag throws |
| `Remove(record)` | Body | Removes the first Record equal to a value; false when none is |
| `RemoveAll(record)` | Body | Removes every Record equal to a value; their number |
| `RemoveAll<T>()` | Body | Removes every Record of a type, in a version that it reads; their number |
| `ReplaceAt(index, record)` | Body | Replaces the Record at a position by a copy of `record`; a position out of the bag throws |
| `Clear()` | Body | Empties the bag |
| `stay.Charges = bag` | Body | Copies a bag in place of the one of its host |
| `RecordBag any = bag` | Body, View | The same bag without its types, by a conversion without a cast |
| `new RecordBag<T1, ...>(bag)`, `new RecordBagView<T1, ...>(view)` | Body, View | The same bag under these types |

## Part 3: When not to use a Record

Parts 1 and 2 showed what a Record does well. This part is the most important one of this tutorial: a Record **never replaces a separate entity**. It is a value that moves, closer to a DTO or to a message: not a small entity, and not a way to save a table.

### Lesson 13: What a Record is not

The lines of a bill look like a list of values, and a bag would hold them. It is the mistake to avoid. The model of this lesson writes one bill twice:

```csharp
/// <summary>The line of a bill, as a Record: the wrong choice, for the comparison.</summary>
[Record(4, 1, 2)]
internal interface RBillLine
{
    [Property(1)]
    VLabel Label { get; set; }

    [Property(2)]
    VCents Amount { get; set; }
}
```

```csharp
/// <summary>A bill whose lines are Records: they live inside it, and it is the only thing that changes.</summary>
[Entity(1, 1, 3), Root, Enumerable]
internal interface MRecordedBill
{
    [Property(1)]
    VName Guest { get; set; }

    [Property(2)]
    RecordBag<RBillLine> Lines { get; set; }

    /// <summary>How many times the lines changed: kept by a rule of yours (Lesson 14).</summary>
    [Property(3)]
    int LinesChanges { get; set; }
}
```

```csharp
/// <summary>The line of a bill, as a part: an entity of its own, in its aggregate.</summary>
[Entity(3, 1, 2), Enumerable]
internal interface MLine
{
    [Property(1)]
    VLabel Label { get; set; }

    [Property(2)]
    VCents Amount { get; set; }
}
```

```csharp
/// <summary>A bill whose lines are parts: each one changes, and is found, and is linked, on its own.</summary>
[Entity(2, 1, 3), Root, Enumerable]
internal interface MBill
{
    [Property(1)]
    VName Guest { get; set; }

    [Property(2)]
    IEntityCollection<MLine> Lines { get; }

    /// <summary>The line that the guest disputes: a link to a part. A Record cannot be the target of a link.</summary>
    [Property(3), Weak]
    Option<MLine> Disputed { get; set; }
}
```

The lines of a bill are changed one by one, by several people, found, disputed. Each of these needs what only an entity has. The charges of Lesson 6 have the same fields, and they are values: each one is added, then read with its stay. What separates the two is not their shape, but what happens to them.

**Its host is its unit of conflict.** A Record has no version of its own. Two transactions that change two different Records of one bag change the same entity, and the second one conflicts:

```csharp
[Fact]
public async Task ConflictOnTheHostOfTwoRecords()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-records", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bill = new RecordedBill { Guest = "Ada" };
        bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
        bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
        transaction.Commit();
    }

    static void Change(int at, long amount)
    {
        var lines = RecordedBillCollection.Single().Lines;
        Assert.True(lines.TryGetAt(at, out BillLineView line));
        var changed = line.ToBody();
        changed.Amount = amount;
        lines.ReplaceAt(at, changed);
    }

    var refused = await TwoTransactions(bills, static () => Change(0, 2_500), static () => Change(1, 500));
    Assert.IsType<TransactionConflictException>(refused);
}
```

Two parts are two entities, and nothing conflicts:

```csharp
[Fact]
public async Task LeaveTwoPartsToTwoTransactions()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-parts", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bill = new Bill { Guest = "Ada" };
        var wine = bill.Lines.New();
        wine.Label = "Wine";
        wine.Amount = 3_000;
        var coffee = bill.Lines.New();
        coffee.Label = "Coffee";
        coffee.Amount = 400;
        transaction.Commit();
    }

    static void Change(string label, long amount) => LineCollection.Single(label, static (line, label) => line.Label == label).Amount = amount;

    var refused = await TwoTransactions(bills, static () => Change("Wine", 2_500), static () => Change("Coffee", 500));
    Assert.Null(refused);

    using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(3_000, LineViewCollection.Sum(static line => line.Amount.Value));
}
```

**It is not found.** A field of a Record cannot be indexed: the search reads every bill, and every line of each one. A part has its index:

```csharp
/// <summary>An index of the lines: a field of a Record cannot be indexed.</summary>
[ValueIndex(nameof(MLine), 1), ValueIndexComponent(1, nameof(MLine.Label))]
internal interface MLineLabelIndex
{
}
```

```csharp
[Fact]
public void FindAPartByAnIndexAndARecordByReadingEverything()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-find", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var bill = new Bill { Guest = "Ada" };
        var wine = bill.Lines.New();
        wine.Label = "Wine";

        var recorded = new RecordedBill { Guest = "Ada" };
        recorded.Lines.Add(new BillLine { Label = "Wine" });
        transaction.Commit();
    }

    using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(1, LineLabelIndex.Is("Wine").Count());

        // No index can do this: every bill is read, and every line of each one.
        Assert.Equal(1, RecordedBillViewCollection.Count(static bill => bill.Lines.Enumerate<BillLineView>().Any(static line => line.Label == "Wine")));
    }
}
```

**It is not linked.** A reference points to an entity. A link follows its part, sees its changes, and is emptied when the part leaves. A Record has no identity to point to: it can only be copied, and the copy knows nothing of the original:

```csharp
[Fact]
public void LinkToAPartAndNeverToARecord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-link", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

    var bill = new Bill { Guest = "Ada" };
    var wine = bill.Lines.New();
    wine.Label = "Wine";
    wine.Amount = 3_000;
    bill.Disputed = wine;

    wine.Amount = 2_500;
    Assert.True(bill.Disputed.Get().Amount == 2_500);

    bill.Lines.Remove(wine);
    Assert.False(bill.Disputed.Any());
}
```

The model refuses what a Record cannot be. Each of these stops the build with GBO049:

| Declared in the model | Why it is refused |
| ---------------------- | ------------------ |
| `[Strong]`, `[Weak]` or `[OwnedFrom]` on a Record | A Record is a value, never a link |
| An index on a Record or on a bag | A Record is found through its host |
| `[Delta]`, `[Max]`, `[Min]` on a Record | A Record is not a number |
| `GhostSpan<RAddress>`, `IEntityCollection<RAddress>` | A list of Records is a bag |
| `Option<RecordBag>` | An empty bag is its absence |

A field of a Record is not indexed either: `[TextIndexed]` in a Record stops the build with GBO032, and an index names the properties of its entity, never a field of a Record (GBO036).

A Record also holds no link to an entity: a field that names an entity is a `*Ref`, an identity, read as a value and never followed ([Tutorial Seven, Lesson 9](Seven-TheLog.md#lesson-9-a-model-and-its-log)).

### Lesson 14: Fast to read, slow to change

The Records of an entity live inside it. This is what makes them fast to read: the entity read, its Records are there, with nothing else to reach. It is also what makes them slow to change: a change of one Record writes its entity, and all that the entity holds. The larger the bag, the more one change costs; a change of a part allocates the same at any size, and leaves the other lines alone.

Measured on the two bills of Lesson 13, a thousand of each, one thread, a Store in memory, each line changed in a transaction of its own (an Intel i9-9900K, .NET 10):

| Lines in the bill | Read a line through the Views, Records / parts | Change one line, Records / parts | Allocated per change, Records / parts |
| ----------------- | ---------------------------------------------- | -------------------------------- | ------------------------------------------ |
| 10 | 173 M/s / 40 M/s | 434,000/s / 479,000/s | 4.0 KB / 3.4 KB |
| 100 | 193 M/s / 42 M/s | 136,000/s / 399,000/s | 17.4 KB / 3.5 KB |
| 1,000 | 196 M/s / 43 M/s | 15,800/s / 105,900/s | 150 KB / 3.3 KB |

Read the two columns together:

- **Reading:** Records are 4 to 5 times faster, at any size.
- **Changing:** a little slower at 10 lines, 3 times slower at 100, 7 times slower at 1,000: one change rewrites the bag, so a short bag keeps up with parts, and a long one falls far behind.

A single Record property, in another measure where all the changes share one transaction: an address of four texts, against the same values in a part: read 1.45 times faster through the Views, 47 M against 32 M residents/s, and changed half as fast, 0.5 M against 1 M residents/s.

So a Record suits what is read often and changed rarely, and a bag stays short: tens of Records, not thousands. Reading them is as cheap as it gets:

```csharp
[Fact]
public void ReadEveryRecordOfEveryEntity()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-read", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using (var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        for (int i = 0; i < 100; i++)
        {
            var bill = new RecordedBill { Guest = "Ada" };
            bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
            bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
        }
        transaction.Commit();
    }

    using (bills.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        long total = 0;
        foreach (var bill in RecordedBillViewCollection)
            foreach (var line in bill.Lines.Enumerate<BillLineView>())
                total += line.Amount.Value;
        Assert.Equal(340_000, total);
    }
}
```

When many Records change together, change them once. Each `Add` is a change of the entity; a free bag, filled then assigned, is one. `LinesChanges` counts them, by a method of yours that GBO calls after each change of the bag:

```csharp
// A business rule of the developer: called at each change of the lines. It keeps LinesChanges, a property of the model.
internal partial class RecordedBill
{
    partial void OnLinesChanged() => LinesChanges++;
}
```

```csharp
[Fact]
public void WriteManyRecordsAtOnce()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-once", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

    var oneByOne = new RecordedBill { Guest = "Ada" };
    for (int i = 0; i < 100; i++)
        oneByOne.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
    Assert.Equal(100, oneByOne.LinesChanges);

    var lines = new RecordBag<BillLineView>();
    for (int i = 0; i < 100; i++)
        lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
    var atOnce = new RecordedBill { Guest = "Ada", Lines = lines };
    Assert.Equal(1, atOnce.LinesChanges);

    Assert.Equal(100, atOnce.Lines.Count());
}
```

The same for the Records that a bag holds already: the copy of the bag, changed, then assigned:

```csharp
[Fact]
public void ChangeManyRecordsAtOnce()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-many", StoreAccessMode.ReadWrite);
    using var bills = new BillRepository(store);
    using var transaction = bills.OpenTransaction(StoreAccessMode.ReadWrite);

    var bill = new RecordedBill { Guest = "Ada" };
    bill.Lines.Add(new BillLine { Label = "Wine", Amount = 3_000 });
    bill.Lines.Add(new BillLine { Label = "Coffee", Amount = 400 });
    int changes = bill.LinesChanges;

    var lines = bill.ToView().Lines.ToBody();
    lines.RemoveAll<BillLineView>();
    lines.Add(new BillLine { Label = "Menu", Amount = 3_200 });
    bill.Lines = lines;

    Assert.Equal(changes + 1, bill.LinesChanges);
    Assert.Equal(1, bill.Lines.Count());
}
```

### Lesson 15: What a Record is for

A Record is a value that moves: closer to a DTO or to a message than to an object of your domain. It needs no Store and no transaction to exist, it belongs to no one, and it is copied whole from where it is to where it goes, on any thread:

```csharp
[Fact]
public async Task CreateAMessageWithoutAStore()
{
    var booked = await Task.Run(static () => new RoomBooked { Guest = "Ada", Room = 12, On = Monday });
    Assert.True(booked.Guest == "Ada" && booked.Room == 12);
}
```

This is why a Log needs it. A Log keeps what happened, in order. A fact is a value: it has no identity, and it never changes. An event, a command, a Mutation: every entry of a Log is a Record, and a `[GhostLog]` holds nothing else.

One Record type may be a fact in a Log and a value in a bag. Its home is the Log, which includes it; a room holds copies of it:

```csharp
/// <summary>A room was booked: a fact, that a Log keeps, and a value, that a room may hold.</summary>
[Record(2, 1, 3)]
internal interface RRoomBooked
{
    [Property(1)]
    VName Guest { get; set; }

    [Property(2)]
    VRoomNumber Room { get; set; }

    [Property(3)]
    VInstant On { get; set; }
}
```

```csharp
// A Log holds Records, and nothing else: Tutorial Seven shows it in detail.
[GhostLog, IncludeRecordModel(nameof(RRoomBooked))]
internal interface MBookingLog
{
}
```

```csharp
[Entity(1, 1, 2), Root, Enumerable]
internal interface MRoom
{
    [Property(1)]
    VRoomNumber Number { get; set; }

    /// <summary>The same Record type: its home is the Log, and the room holds copies of it.</summary>
    [Property(2)]
    RecordBag<RRoomBooked> Bookings { get; set; }
}
```

The same value, as a fact, then as a copy in the room it is about:

```csharp
[Fact]
public void KeepARecordInALogAndInABag()
{
    using var logStore = TransactionalMemoryStore.NewInMemoryLogStore("lesson-15-log", StoreAccessMode.ReadWrite);
    using var log = new BookingLog(logStore);
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-rooms", StoreAccessMode.ReadWrite);
    using var rooms = new RoomRepository(store);

    // Added to its Log, the Record is a fact: frozen, and kept in order.
    using (var transaction = log.OpenTransaction())
    {
        new RoomBooked { Guest = "Ada", Room = 12, On = Monday }.Add();
        transaction.Commit();
    }

    // The View of a Record is a value: the one that the Log delivers is kept after its subscription.
    var delivered = new List<RoomBookedView>();
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(0, end.Token).Handle<RoomBookedView, List<RoomBookedView>>(delivered, static (_, booked, delivered) =>
    {
        delivered.Add(booked);
        return LogStoreSubscription.Continue;
    }).Run();
    end.Cancel();

    using (var transaction = rooms.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Room { Number = 12 }.Bookings.Add(delivered.Single());
        transaction.Commit();
    }

    using (rooms.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.True(RoomViewCollection.Single().Bookings.TryGetAt(0, out RoomBookedView kept));
        Assert.True(kept.Guest == "Ada" && kept.Room == 12);
    }
}
```

A bag never replaces a Log. A bag holds what its entity needs to read with it; the Log keeps everything that happened, in the order of the commits, for those who follow it and for a replay.

## From the need to the Record

| The need | The answer | Lessons |
| -------- | ---------- | ------- |
| A value of a few bytes, without text: a price, a code | A value object, not a Record | [Tutorial One, 18](One-TheBasics.md#lesson-18-give-each-value-its-type) |
| Several values that go together, read and replaced whole, reused in several places: an address | A Record property, few per entity | 1 to 5 |
| A list of such values, owned by the entity, short, read whole: a history, charges | A bag of its types | 6 to 9 |
| Messages of several types for several services | A bag of any Record, `RecordBag` | 10 |
| One message that carries others | A Record that holds a bag | 10 |
| A value that travels: between threads, services, layers | A Record, or its View | 3, 15 |
| Many Records written at once | A free bag, then its assignment | 11, 14 |
| What happened, in order, for good | A Record in a Log: Tutorial Seven | 15 |

**And when not to:**

| If the thing | Then it is not a Record, but | Because |
| ------------ | ---------------------------- | ------- |
| Is changed one by one, by several people: the lines of an order | A part, `IEntityCollection<T>` | Its host is the unit of conflict of a Record (Lesson 13) |
| Is searched by one of its fields | A part or an entity, with an index | A field of a Record is never indexed (Lesson 13) |
| Is referenced by something else | An entity | A Record has no identity, and no link points to it (Lesson 13) |
| Grows without limit | Parts, or a Log | A change rewrites the whole bag (Lesson 14) |
| Changes often, a field at a time | A property, or a part | A change copies it out and back (Lesson 14) |
| Has a life of its own | An entity | A Record lives and leaves with its host (Lesson 2) |

## What you learned

- A **Record** is a value of several fields: declared by `[Record]`, its interface starting with `R`; created anywhere, without a Store; it has no identity, no link and no index, and lives in its host.
- An **entity** is someone, a **Record** is something: two entities of the same values stay two, two Records of the same values are one value, and giving a Record to a second holder gives it a copy.
- A Record property is **never absent**: the default Record until written; `Option<RAddress>` may be absent; a Record holds Records, to any depth.
- A Record is **read in place** through its View, which allocates nothing, and which is a value, unlike the View of an entity: it keeps what it read, after its transaction, on another thread.
- A Record is **replaced whole**: a new value, or its copy, `ToBody()`, changed, then assigned. The same value is no change.
- A **`RecordBag`** is a list of values, written directly through its entity: of one type, of several types up to eight, or of any type. Its Records stay in their order.
- A bag is read by `Enumerate<T>()`, by handlers per type, by position, by type and by value; it is changed by `Add`, `RemoveAt`, `Remove`, `RemoveAll`, `ReplaceAt`, `Clear`.
- A **free bag** is filled without a transaction, then assigned once; the View of a bag, a `RecordBagView`, is a value, and its `ToBody()` a free copy.
- A Record **never replaces an entity**: its host is its unit of conflict, it is neither indexed nor linked, and one change rewrites its bag. It is **fast to read and slow to change**: keep bags short, and change many Records at once.
- A Record **moves**: closer to a DTO or a message than to an object of your domain. Every entry of a Log is a Record.

[Tutorial Seven](Seven-TheLog.md) starts from there: the Log, its Records as facts, their subscriptions, and their replay.
