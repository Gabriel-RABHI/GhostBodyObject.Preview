# Tutorial Six: The Log

## The Log and the Records

The tutorials before kept the state of the data: a book, its title and its year, as they are now. A change replaced the value before it, and nothing told what happened, nor when, nor in what order. Many applications need that too: the steps of a parcel, the movements of an account, the decisions of a workflow, the messages between two services, the history that an audit reads. And many of them need it as their very memory: a model that is lost, changed or damaged is given again by what happened to it.

This is the work of the **Log**. A Log keeps **Records**, the facts, in the order they happened: appended, never changed, never removed. This tutorial shows how it works:

- **The Records and the Log.** A Record is declared as an entity, by an interface, `[Record]` in place of `[Entity]`; a Log by an interface `[GhostLog]` that includes its Record types. Its transactions append: `new ParcelScanned { ... }.Add()`, then `Commit()`.
- **The subscriptions.** A Log is read from any of its transactions, by a handler for each Record type: `log.SubscribeFrom(0).Handle<ParcelScannedView>(...)`. A subscription delivers the Log as it is, then follows its new commits, on a thread of its own.
- **The replay.** A model chained with its Log is written with it, in one transaction. The model may then live in memory only: a replay of the Log gives it again at each opening, through the same handlers as the commands; a model on disk is rebuilt from it on request.

Each lesson is a test class of the project `GhostBodyObject.TutorialSix`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorials One to Five: the Stores, the transactions, the Views and the Bodies, the static lambdas, the identities and the partial declarations are used here without being explained again.

## What you will build

A parcel carrier. First its journal, Records alone: a parcel registered, scanned at a depot, delivered, and the routes planned for the parcels. Then its model, the parcels as they are now, chained with the Log of what happened to them, and rebuilt from it.

Two models, each in a namespace of its own, as in the tutorials before: `Journal` for Lessons 1 to 8, with two Logs, `ParcelLog` and `RouteLog`; `Carrier` for Lessons 9 to 11, a Repository of the parcels and their Log, `CarrierLog`.

Every declaration of the tutorial, and the lesson that shows it:

| Declaration in the model | What GBO guarantees | Lessons |
| ------------------------ | ------------------- | ------- |
| `[Record(n, version, max)]` on `MParcelRegistered` | A Record type, its number stable in the Log: the class `ParcelRegistered`, created anywhere, frozen once added; its View `ParcelRegisteredView` | 1, 3 |
| `[GhostLog]` and `[IncludeRecordModel]` on `MParcelLog` | A Log of these Record types: the class `ParcelLog`, its transactions, its subscriptions; `ParcelLogCollections.ParcelCollection`, the Records of a transaction | 1, 2 |
| An enum, a `decimal`, a `GhostSpan<T>`, a struct of an explicit layout, an `Option<T>`, a `GhostString` in a Record | The values of a Record, read in place by its View, without a copy | 4 |
| A `*Ref` in a Record | The identity of an entity, never a link: a fact names what it is about | 9 |
| `AddChildStore` and `repository.Chain(log)` | The Records and the entities in one transaction, or neither | 9 |

## Part 1: Records and the Log

### Lesson 1: Declare a Record and a Log

A Record is declared as an entity: an interface whose name starts with `M`, its properties numbered by `[Property(n)]`, and `[Record(type, version, maxPropertyIdentifier)]` in place of `[Entity]`. Its number is stable: the Log stores it with each Record. `MParcelRegistered` gives the class `ParcelRegistered`, and its View `ParcelRegisteredView`.

The Log is an interface with `[GhostLog]`, which includes its Record types by `[IncludeRecordModel]`, as a Repository includes its entity types. `MParcelLog` gives the class `ParcelLog`, and `ParcelLogCollections.ParcelCollection`, the Records of its transaction:

```csharp
// [Record(type, version, maxPropertyIdentifier)]: a Record type, as [Entity] declares an entity type. Its number is stable: the
// Log stores it with each Record. The interface starts with M: MParcelRegistered gives the class ParcelRegistered.
[Record(1, 1, 4)]
internal interface MParcelRegistered
{
    [Property(1)]
    string TrackingNumber { get; set; }

    [Property(2)]
    string Recipient { get; set; }

    [Property(3)]
    int Grams { get; set; }

    [Property(4)]
    DateTime Registered { get; set; }
}

[Record(2, 1, 3)]
internal interface MParcelScanned
{
    [Property(1)]
    string TrackingNumber { get; set; }

    // The depot where the parcel was scanned.
    [Property(2)]
    string Depot { get; set; }

    [Property(3)]
    DateTime Scanned { get; set; }
}

[Record(3, 1, 2)]
internal interface MParcelDelivered
{
    [Property(1)]
    string TrackingNumber { get; set; }

    [Property(2)]
    DateTime Delivered { get; set; }
}

// [GhostLog]: a Log, and the Record types it holds, each by [IncludeRecordModel]. MParcelLog gives the class ParcelLog, opened on
// a Store of its own, and ParcelLogCollections.ParcelCollection, the Records of its transaction.
[GhostLog]
[IncludeRecordModel(nameof(MParcelRegistered))]
[IncludeRecordModel(nameof(MParcelScanned))]
[IncludeRecordModel(nameof(MParcelDelivered))]
internal interface MParcelLog
{
}
```

A Log opens on a Store of its own, a **Log Store**, in memory here, on disk in Lesson 5. Its transactions are ambient, as the transactions of a Repository: a Record created, then added by `Add()`, belongs to the transaction open in this context. The commit appends the Records of the transaction to the Log, together, after the Records of the commits before:

```csharp
[Fact]
public void AppendTheRecordsOfACommit()
{
    // A Log Store in memory, and the Log of the model on it.
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-append", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);

    // A transaction of the Log, ambient as the transactions of a Repository: a Record created then added, Add(), belongs to it.
    using (var transaction = log.OpenTransaction())
    {
        new ParcelRegistered { TrackingNumber = "PX-1001", Recipient = "Ada", Grams = 1200, Registered = Monday }.Add();
        new ParcelScanned { TrackingNumber = "PX-1001", Depot = "Lyon", Scanned = Monday.AddHours(3) }.Add();
        transaction.Commit();
    }

    // The Log holds both Records, in the order they were added. Lesson 2 shows how it is read.
    Assert.Equal(["registered PX-1001", "scanned PX-1001 at Lyon"], Read(log));
}
```

Until the commit, the Log has none of them: `ParcelCollection` gives the Records added in the transaction, in the order of their `Add`. A transaction that does not commit, rolled back or disposed, appends nothing:

```csharp
[Fact]
public void AppendNothingWithoutACommit()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-nothing", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    using (var transaction = log.OpenTransaction())
    {
        var registered = new ParcelRegistered { TrackingNumber = "PX-1002", Recipient = "Bob", Grams = 300, Registered = Monday }.Add();
        var scanned = new ParcelScanned { TrackingNumber = "PX-1002", Depot = "Lyon", Scanned = Monday.AddHours(1) }.Add();

        // ParcelCollection: the Records added in this transaction, in the order of their Add. The Log has none of them yet.
        Assert.Equal<IParcelRecord>([registered, scanned], ParcelCollection.ToArray());
        Assert.Empty(Read(log));
        transaction.Rollback();
    }

    // A transaction disposed without a commit is rolled back, as a transaction of a Repository.
    using (log.OpenTransaction())
        new ParcelDelivered { TrackingNumber = "PX-1002", Delivered = Monday.AddDays(1) }.Add();
    Assert.Empty(Read(log));
}
```

A Record goes to the Log of a transaction: without one, `Add` is refused. A thread has one transaction of a Log at a time, as one transaction of a Repository:

```csharp
[Fact]
public void AddARecordInATransactionOnly()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-01-transaction", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);

    // Without a transaction, a Record has no Log to go to.
    var lost = new ParcelDelivered { TrackingNumber = "PX-1003", Delivered = Monday };
    Assert.Throws<InvalidOperationException>(() => lost.Add());

    // One transaction of the Log at a time on a thread, as for a Repository.
    using (log.OpenTransaction())
        Assert.Throws<InvalidOperationException>(() => log.OpenTransaction());
}
```

A Log has nothing else: no query, no index, no change, no removal. It is read from a point onwards, by a subscription. The lesson reads it so, in its helper, which Lesson 2 explains:

```csharp
// Every Record of the Log, from its first transaction, as a line of text (Lesson 2).
private static List<string> Read(ParcelLog log)
{
    var lines = new List<string>();
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(0, end.Token)
        .Handle<ParcelRegisteredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"registered {e.TrackingNumber}"))
        .Handle<ParcelScannedView, List<string>>(lines, static (_, e, lines) => Add(lines, $"scanned {e.TrackingNumber} at {e.Depot}"))
        .Handle<ParcelDeliveredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"delivered {e.TrackingNumber}"))
        .Run();
    end.Cancel();
    return lines;
}

private static LogStoreSubscription Add(List<string> lines, string line)
{
    lines.Add(line);
    return LogStoreSubscription.Continue;
}
```

### Lesson 2: Read the Log

A **subscription** reads a Log: `log.SubscribeFrom(transaction)`, then a handler for each Record type to read, `Handle<TView>`, then `Run()`. Run delivers, on the calling thread, every transaction committed from the one asked, 0 for the first: each Record to the handler of its type, in the order of the Log, as a View, the same View as for an entity. The handler takes the number of the transaction of its Record, and its View; it answers `LogStoreSubscription.Continue` for the next Record, or `Cancel` to stop there.

A handler captures nothing: what it needs is passed before it, its state, one to four values, as the static lambdas of the queries of Tutorial One. The subscription keeps the handler and its state, and calls it for each Record without allocating.

After Run, the subscription stays active, and delivers each new commit (Lesson 6): until a handler cancels it, or until the token given to `SubscribeFrom` is cancelled. A subscription that only reads what is there cancels its token after Run.

The journey of a parcel, in three commits:

```csharp
// The journey of a parcel, in three commits: registered and scanned, scanned again, delivered.
private static void Journey(ParcelLog log)
{
    using (var transaction = log.OpenTransaction())
    {
        new ParcelRegistered { TrackingNumber = "PX-2001", Recipient = "Ada", Grams = 800, Registered = Monday }.Add();
        new ParcelScanned { TrackingNumber = "PX-2001", Depot = "Lyon", Scanned = Monday.AddHours(1) }.Add();
        transaction.Commit();
    }
    using (var transaction = log.OpenTransaction())
    {
        new ParcelScanned { TrackingNumber = "PX-2001", Depot = "Paris", Scanned = Monday.AddHours(6) }.Add();
        transaction.Commit();
    }
    using (var transaction = log.OpenTransaction())
    {
        new ParcelDelivered { TrackingNumber = "PX-2001", Delivered = Monday.AddHours(9) }.Add();
        transaction.Commit();
    }
}
```

Every Record reaches the handler of its type, in the order of the Log. The Records of one commit share the number of its transaction; each commit after it has a greater one:

```csharp
[Fact]
public void DeliverEachRecordInTheOrderOfTheLog()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-order", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Journey(log);

    var lines = new List<string>();
    var transactions = new List<ulong>();
    using var end = new CancellationTokenSource();
    // Each handler takes the number of the transaction of its Record, the View of the Record, then its state: two values here.
    log.SubscribeFrom(0, end.Token)
        .Handle<ParcelRegisteredView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
            Add(lines, transactions, transaction, $"{e.TrackingNumber} registered for {e.Recipient}"))
        .Handle<ParcelScannedView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
            Add(lines, transactions, transaction, $"{e.TrackingNumber} scanned at {e.Depot}"))
        .Handle<ParcelDeliveredView, List<string>, List<ulong>>(lines, transactions, static (transaction, e, lines, transactions) =>
            Add(lines, transactions, transaction, $"{e.TrackingNumber} delivered"))
        .Run();
    end.Cancel();

    Assert.Equal(["PX-2001 registered for Ada", "PX-2001 scanned at Lyon", "PX-2001 scanned at Paris", "PX-2001 delivered"], lines);
    // The two Records of the first commit share its number; each commit after it has a greater one.
    Assert.True(transactions[0] == transactions[1] && transactions[1] < transactions[2] && transactions[2] < transactions[3]);
}
```

A Record of a type without a handler is skipped: a subscription reads only the types it handles, and costs nothing for the others:

```csharp
[Fact]
public void SkipTheRecordsOfATypeWithoutHandler()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-skip", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Journey(log);

    var depots = new List<string>();
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(0, end.Token)
        .Handle<ParcelScannedView, List<string>>(depots, static (_, e, depots) =>
        {
            depots.Add(e.Depot);
            return LogStoreSubscription.Continue;
        })
        .Run();
    end.Cancel();
    Assert.Equal(["Lyon", "Paris"], depots);
}
```

A handler that answers `Cancel` ends the subscription: the Records after its own are not delivered. `Completion`, a task, tells how a subscription ended: a handler that cancels ends it successfully.

```csharp
[Fact]
public void StopAtTheCancelOfAHandler()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-cancel", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Journey(log);

    // The first depot of the parcel: the subscription stops there, without a token to cancel.
    var first = new List<string>();
    var subscription = log.SubscribeFrom(0)
        .Handle<ParcelScannedView, List<string>>(first, static (_, e, first) =>
        {
            first.Add(e.Depot);
            return LogStoreSubscription.Cancel;
        })
        .Run();
    Assert.Equal(["Lyon"], first);

    // Completion: the end of the subscription, as a task. A handler that cancels ends it successfully.
    Assert.True(subscription.Completion.IsCompletedSuccessfully);
}
```

A View reads its Record where the Log keeps it: nothing is copied, and the View stays readable after its delivery, as long as the Log is open:

```csharp
[Fact]
public void KeepAViewAfterItsDelivery()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-02-view", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Journey(log);

    var scans = new List<ParcelScannedView>();
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(0, end.Token)
        .Handle<ParcelScannedView, List<ParcelScannedView>>(scans, static (_, e, scans) =>
        {
            scans.Add(e);
            return LogStoreSubscription.Continue;
        })
        .Run();
    end.Cancel();

    // The Views read the Records where the Log keeps them: nothing was copied.
    Assert.Equal(["Lyon", "Paris"], scans.Select(static scan => scan.Depot));
    Assert.Equal(TimeSpan.FromHours(5), scans[1].Scanned - scans[0].Scanned);
}
```

### Lesson 3: The life of a Record

A Record lives in two times. **Created**, it is detached: it belongs to no transaction and to no Log yet. It can be created anywhere, on any thread, outside any transaction, and written as many times as needed, as an object of C#. **Added**, by `Add()` in a transaction of its Log, it takes its place in that transaction, after the Records added before it: the order of the Log is the order of the `Add`, not of the creation.

```csharp
[Fact]
public async Task CreateARecordAnywhereAndAddItLater()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-detached", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);

    // A scanner prepares its Records on a thread of its own, without any transaction: they are detached, and can be changed.
    var (lyon, paris) = await Task.Run(static () =>
    {
        var lyon = new ParcelScanned { TrackingNumber = "PX-3001", Depot = "Lyon", Scanned = Monday };
        var paris = new ParcelScanned { TrackingNumber = "PX-3001", Depot = "Pari", Scanned = Monday.AddHours(5) };
        paris.Depot = "Paris";
        return (lyon, paris);
    });

    // The transaction takes them in the order of their Add: Paris first here.
    using (var transaction = log.OpenTransaction())
    {
        Assert.Empty(ParcelCollection);
        paris.Add();
        lyon.Add();
        Assert.Equal<IParcelRecord>([paris, lyon], ParcelCollection.ToArray());
        transaction.Commit();
    }
    Assert.Equal(["Paris", "Lyon"], Depots(log));
}
```

Once added, a Record is a **fact**: it is frozen. A write of it is refused, in its transaction and after it; it keeps its values, and the Log holds them as they were at the `Add`:

```csharp
[Fact]
public void FreezeARecordOnceAdded()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-frozen", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    ParcelScanned scanned;
    using (var transaction = log.OpenTransaction())
    {
        scanned = new ParcelScanned { TrackingNumber = "PX-3002", Depot = "Lyon", Scanned = Monday };
        scanned.Depot = "Lille";
        scanned.Add();
        Assert.Throws<InvalidOperationException>(() => scanned.Depot = "Paris");
        transaction.Commit();
    }
    Assert.Throws<InvalidOperationException>(() => scanned.Depot = "Paris");

    // The Record keeps its values, and the Log holds the value written before its Add.
    Assert.Equal("Lille", scanned.Depot);
    Assert.Equal(["Lille"], Depots(log));
}
```

A Record is added once. Its View, the same View that a subscription delivers, is given once it is added: `Add()` returns the Record, and `Add().ToView()` gives its View at once. A Record never added is never in the Log:

```csharp
[Fact]
public void AddARecordOnceAndReadItsView()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-03-view", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    using (var transaction = log.OpenTransaction())
    {
        // Detached: it has no View yet, and the commit leaves it out.
        var forgotten = new ParcelScanned { TrackingNumber = "PX-3003", Depot = "Nice", Scanned = Monday };
        Assert.Throws<InvalidOperationException>(() => forgotten.ToView());

        // Add returns the Record: its View at once, as a subscription would deliver it.
        var view = new ParcelScanned { TrackingNumber = "PX-3003", Depot = "Lyon", Scanned = Monday }.Add().ToView();
        Assert.Equal("Lyon", view.Depot);

        var added = ParcelCollection.ToArray().OfType<ParcelScanned>().Single();
        Assert.Throws<InvalidOperationException>(() => added.Add());
        transaction.Commit();
    }
    Assert.Equal(["Lyon"], Depots(log));
}
```

### Lesson 4: The values of a Record

A Record takes the values of an entity: the numbers, the decimals, the dates, the enums, the `Option<T>`, the texts, the arrays of values, `GhostSpan<T>`, and the structs of your own, their layout declared. It has **no link**: a Record is a fact, read long after the entities it names have changed or gone. It names them by their identity instead, a generated `*Ref` (Lesson 9).

The lesson declares a second Log, `RouteLog`, for the routes planned for the parcels: a Log holds the Record types it includes, each Log on its own Store.

```csharp
internal enum ParcelSize
{
    Letter,
    Small,
    Large
}

// A struct of your own, stored as its bytes are: its layout declared, explicit, a [FieldOffset] on each field, every byte covered
// by a field, 32 bytes at most. Here 16 bytes, two doubles.
[StructLayout(LayoutKind.Explicit, Size = 16)]
internal readonly struct Position(double latitude, double longitude)
{
    [FieldOffset(0)]
    public readonly double Latitude = latitude;

    [FieldOffset(8)]
    public readonly double Longitude = longitude;
}

[Record(11, 1, 7)]
internal interface MRoutePlanned
{
    [Property(1)]
    string TrackingNumber { get; set; }

    // An enum, a decimal: the values of an entity are the values of a Record.
    [Property(2)]
    ParcelSize Size { get; set; }

    [Property(3)]
    decimal Cost { get; set; }

    // A GhostSpan: an array of values, the numbers of the depots of the route, in the Record.
    [Property(4)]
    GhostSpan<int> Stops { get; set; }

    [Property(5)]
    Position Destination { get; set; }

    // An Option: a promised day of delivery, or none.
    [Property(6)]
    Option<DateTime> Promised { get; set; }

    // A GhostString: a text read in place by the View, without a copy.
    [Property(7)]
    GhostString Note { get; set; }
}

[GhostLog]
[IncludeRecordModel(nameof(MRoutePlanned))]
internal interface MRouteLog
{
}
```

A `GhostSpan<T>` is given as a C# array, and changed in place by `Set` while the Record is detached. The View reads each value where the Log keeps it: a `GhostString` as a `ReadOnlyGhostString`, which compares to a string without a copy; an array as a `ReadOnlyGhostSpan<T>`. A subscription that reads millions of Records allocates nothing for them; `ToString` and `ToArray` make the copy, when one is needed.

```csharp
[Fact]
public void WriteAndReadEveryKindOfValue()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-values", StoreAccessMode.ReadWrite);
    using var log = new RouteLog(store);
    using (var transaction = log.OpenTransaction())
    {
        // An array is given as a C# array, then changed in place by Set, while the Record is detached.
        var route = new RoutePlanned
        {
            TrackingNumber = "PX-4001",
            Size = ParcelSize.Large,
            Cost = 12.90m,
            Stops = new[] { 69, 21, 75 },
            Destination = new Position(48.8566, 2.3522),
            Promised = Friday,
            Note = "Leave at the reception"
        };
        route.Stops.Set(1, 71);
        route.Add();
        transaction.Commit();
    }

    var routes = Routes(log);
    var planned = Assert.Single(routes);
    Assert.Equal((ParcelSize.Large, 12.90m), (planned.Size, planned.Cost));
    Assert.Equal([69, 71, 75], planned.Stops.ToArray());
    Assert.Equal(2.3522, planned.Destination.Longitude);
    Assert.Equal(Friday, planned.Promised.Get());

    // A ReadOnlyGhostString compares to a string without a copy; ToString makes the copy, when one is needed.
    Assert.True(planned.Note == "Leave at the reception");
    Assert.StartsWith("Leave", planned.Note.ToString());
}
```

The values not written are their defaults: an `Option<T>` is absent, an array is empty, an enum is its first value:

```csharp
[Fact]
public void ReadTheValuesNotWrittenAsTheirDefaults()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-defaults", StoreAccessMode.ReadWrite);
    using var log = new RouteLog(store);
    using (var transaction = log.OpenTransaction())
    {
        new RoutePlanned { TrackingNumber = "PX-4002" }.Add();
        transaction.Commit();
    }
    var planned = Assert.Single(Routes(log));
    Assert.False(planned.Promised.Any());
    Assert.True(planned.Stops.IsEmpty);
    Assert.Equal(ParcelSize.Letter, planned.Size);
}
```

An array of an added Record is frozen as its other values:

```csharp
[Fact]
public void FreezeTheArraysOfARecordOnceAdded()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-04-frozen", StoreAccessMode.ReadWrite);
    using var log = new RouteLog(store);
    using var transaction = log.OpenTransaction();
    var route = new RoutePlanned { TrackingNumber = "PX-4003", Stops = new[] { 69, 75 } }.Add();
    Assert.Throws<InvalidOperationException>(() => route.Stops.Set(0, 13));
    Assert.Throws<InvalidOperationException>(() => route.Stops = new[] { 13 });
    Assert.Equal([69, 75], route.Stops.ToArray());
    transaction.Commit();
}
```

### Lesson 5: A Log on disk

`TransactionalMemoryStore.NewFileLogStore(directory, name)` opens a Log Store on disk, as `NewFileRepositoryStore` opens a Repository Store in Tutorial One. A commit returns once its Records are written: a Log opened again, by this process or by another one, holds every transaction committed, and only them. Its new commits go after them.

```csharp
[Fact]
public void KeepTheRecordsOverARestart()
{
    // A directory of its own for the Log of the lesson, emptied at each run.
    string directory = Path.Combine(AppContext.BaseDirectory, "lesson-05-journal");
    if (Directory.Exists(directory))
        Directory.Delete(directory, recursive: true);

    using (var store = TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite))
    using (var log = new ParcelLog(store))
    using (var transaction = log.OpenTransaction())
    {
        new ParcelRegistered { TrackingNumber = "PX-5001", Recipient = "Ada", Grams = 500, Registered = Monday }.Add();
        new ParcelScanned { TrackingNumber = "PX-5001", Depot = "Lyon", Scanned = Monday.AddHours(2) }.Add();
        transaction.Commit();
    }

    // The process ends there, or later: the Store opened again on the same directory finds the Log as it was committed.
    using (var store = TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite))
    using (var log = new ParcelLog(store))
    {
        Assert.Equal(["PX-5001 registered", "PX-5001 at Lyon"], Read(log));
        using (var transaction = log.OpenTransaction())
        {
            new ParcelDelivered { TrackingNumber = "PX-5001", Delivered = Monday.AddDays(1) }.Add();
            transaction.Commit();
        }
        Assert.Equal(["PX-5001 registered", "PX-5001 at Lyon", "PX-5001 delivered"], Read(log));
    }
}
```

The Log is the memory of what happened, kept for good. A projection of it, a model, a screen, a report, can be lost, changed, rebuilt: the Log stays, and gives it again (Lessons 10 and 11).

## Part 2: Subscriptions

### Lesson 6: Follow the Log

A subscription does not stop at the end of the Log. Run delivers, on the calling thread, every transaction committed until then: the **replay**. `OnReplayEnd` is called once it is done, before Run returns, with the number of the last transaction found: the subscriber has caught up with the Log. The subscription then stays active, and delivers each new commit on a thread of its own, at once: a screen, a cache, a report, a service, kept up to date by the Log, without a poll and without a query.

The board of the lesson keeps the last depot of each parcel; its methods are the handlers, and it is their state:

```csharp
// The board of the depots: the projection of the Log that the subscription keeps, written by its handlers only.
private sealed class Board
{
    public Dictionary<string, string> Depots { get; } = [];

    public bool IsReady { get; private set; }

    public ulong ReadyAt { get; private set; }

    public LogStoreSubscription Scanned(string trackingNumber, string depot)
    {
        Depots[trackingNumber] = depot;
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Delivered(string trackingNumber)
    {
        Depots.Remove(trackingNumber);
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Ready(ulong last)
    {
        (IsReady, ReadyAt) = (true, last);
        return LogStoreSubscription.Continue;
    }
}
```

The board knows the Log as it was at Run, then each commit after it. To read the board at a known point, the test delivers on its own thread what the subscription has not received yet, after the delivery in progress, if any: `log.Store.DeliverTransactions(subscription)`. An application waits for nothing: its subscription delivers the commits as they come.

```csharp
[Fact]
public void DeliverTheNewCommitsAfterTheReplay()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-follow", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Scan(log, "PX-6001", "Lyon");

    // The board is the state of the handlers: the last depot of each parcel, and the end of the replay.
    var board = new Board();
    using var end = new CancellationTokenSource();
    var subscription = log.SubscribeFrom(0, end.Token)
        .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
        .Handle<ParcelDeliveredView, Board>(board, static (_, e, board) => board.Delivered(e.TrackingNumber))
        .OnReplayEnd(board, static (last, board) => board.Ready(last))
        .Run();

    // Run returns after the replay: the board knows the Log as it was, and the number of its last transaction.
    Assert.True(board.ReadyAt > 0);
    Assert.Equal("Lyon", board.Depots["PX-6001"]);

    // A new commit: the subscription delivers it on its own thread. To read the board at a known point, a test delivers on its
    // thread what the subscription has not received yet, after the delivery in progress, if any; an application waits for nothing.
    Scan(log, "PX-6001", "Paris");
    Scan(log, "PX-6002", "Lille");
    using (var transaction = log.OpenTransaction())
    {
        new ParcelDelivered { TrackingNumber = "PX-6002", Delivered = Monday.AddHours(4) }.Add();
        transaction.Commit();
    }
    log.Store.DeliverTransactions(subscription);
    Assert.Equal(["PX-6001 at Paris"], board.Depots.Select(static depot => $"{depot.Key} at {depot.Value}"));

    // The token cancelled: the subscription ends, its Completion cancelled; the commits after it reach it no more.
    end.Cancel();
    Assert.True(subscription.Completion.IsCanceled);
    Scan(log, "PX-6001", "Nice");
    Assert.Equal("Paris", board.Depots["PX-6001"]);
}
```

Its handlers are never called together: the replay and each delivery after it are delivered one at a time, and the board needs no lock. The token cancelled, the subscription ends: its `Completion` is cancelled, and the commits after it reach it no more.

The replay of an empty Log ends at once, with the transaction 0; the first commit is delivered after it:

```csharp
[Fact]
public void EndTheReplayOfAnEmptyLog()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-empty", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    var board = new Board();
    using var end = new CancellationTokenSource();
    var subscription = log.SubscribeFrom(0, end.Token)
        .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
        .OnReplayEnd(board, static (last, board) => board.Ready(last))
        .Run();
    Assert.Equal((true, 0ul), (board.IsReady, board.ReadyAt));

    Scan(log, "PX-6003", "Lyon");
    log.Store.DeliverTransactions(subscription);
    Assert.Equal("Lyon", board.Depots["PX-6003"]);
    end.Cancel();
}
```

A token cancelled before Run delivers nothing. Without a token, a subscription follows its Log until the Log is disposed: it ends then, successfully:

```csharp
[Fact]
public void EndWithItsLogOrBeforeItsStart()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-06-end", StoreAccessMode.ReadWrite);
    var log = new ParcelLog(store);
    Scan(log, "PX-6004", "Lyon");

    var board = new Board();
    var cancelled = log.SubscribeFrom(0, new CancellationToken(canceled: true))
        .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
        .Run();
    Assert.Empty(board.Depots);
    Assert.True(cancelled.Completion.IsCanceled);

    // Without a token, the subscription follows the Log until the Log is disposed: it ends then, successfully.
    var following = log.SubscribeFrom(0)
        .Handle<ParcelScannedView, Board>(board, static (_, e, board) => board.Scanned(e.TrackingNumber, e.Depot))
        .Run();
    Assert.False(following.Completion.IsCompleted);
    log.Dispose();
    Assert.True(following.Completion.IsCompletedSuccessfully);
}
```

### Lesson 7: Frame the transactions

The Records of a commit belong together: a parcel registered and its first scan, a round of a driver, a day of a depot. A subscription delivers them together, framed: `OnTransactionStart` before the first Record of each transaction, `OnTransactionEnd` after its last one, each with the number of the transaction. A subscriber applies a transaction as a whole there: it opens its work at the start, and closes it at the end.

The rounds of the lesson count the scans of each transaction, and close each round at its end:

```csharp
// The rounds closed, each a transaction of the Log: its number, its depot, its count of scans; the last transaction handled.
private sealed class Rounds
{
    private string _depot = "";
    private int _scans;

    public List<(ulong Transaction, string Depot, int Scans)> Closed { get; } = [];

    public ulong Last { get; private set; }

    public LogStoreSubscription Start(ulong transaction)
    {
        (_depot, _scans) = ("", 0);
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Scanned(string depot)
    {
        (_depot, _scans) = (depot, _scans + 1);
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription End(ulong transaction)
    {
        Closed.Add((transaction, _depot, _scans));
        Last = transaction;
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Ready(ulong last)
    {
        Last = last;
        return LogStoreSubscription.Continue;
    }
}

// A round of a depot: its scans, in one commit.
private static void Round(ParcelLog log, string depot, params string[] trackingNumbers)
{
    using var transaction = log.OpenTransaction();
    foreach (var trackingNumber in trackingNumbers)
        new ParcelScanned { TrackingNumber = trackingNumber, Depot = depot, Scanned = Monday }.Add();
    transaction.Commit();
}
```

```csharp
[Fact]
public void FrameEachTransactionThenTheReplay()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-frame", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Round(log, "Lyon", "PX-7001", "PX-7002");
    Round(log, "Paris", "PX-7003");

    // The rounds of the depots: the scans of a transaction counted together, closed at its end.
    var rounds = new Rounds();
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(0, end.Token)
        .OnTransactionStart(rounds, static (transaction, rounds) => rounds.Start(transaction))
        .Handle<ParcelScannedView, Rounds>(rounds, static (_, e, rounds) => rounds.Scanned(e.Depot))
        .OnTransactionEnd(rounds, static (transaction, rounds) => rounds.End(transaction))
        .OnReplayEnd(rounds, static (last, rounds) => rounds.Ready(last))
        .Run();
    end.Cancel();

    ulong first = rounds.Closed[0].Transaction;
    Assert.Equal([(first, "Lyon", 2), (first + 1, "Paris", 1)], rounds.Closed);
    Assert.Equal(first + 1, rounds.Last);
}
```

The number of a transaction is its place in the Log. `SubscribeFrom(n)` delivers from the transaction `n`: a subscriber that remembers the last transaction it handled starts again after it, and reads only what it has not read yet:

```csharp
// The rounds of the Log from the transaction given, until its end.
private static void Follow(ParcelLog log, ulong from, Rounds rounds)
{
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(from, end.Token)
        .OnTransactionStart(rounds, static (transaction, rounds) => rounds.Start(transaction))
        .Handle<ParcelScannedView, Rounds>(rounds, static (_, e, rounds) => rounds.Scanned(e.Depot))
        .OnTransactionEnd(rounds, static (transaction, rounds) => rounds.End(transaction))
        .Run();
    end.Cancel();
}

[Fact]
public void StartFromALaterTransaction()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-later", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Round(log, "Lyon", "PX-7011");
    Round(log, "Paris", "PX-7012");

    var rounds = new Rounds();
    Follow(log, 0, rounds);
    ulong handled = rounds.Last;

    // Later: two new rounds. From the transaction after the last one handled, only them.
    Round(log, "Lille", "PX-7013");
    Round(log, "Nice", "PX-7014", "PX-7015");
    var later = new Rounds();
    Follow(log, handled + 1, later);
    Assert.Equal([("Lille", 1), ("Nice", 2)], later.Closed.Select(static round => (round.Depot, round.Scans)));
}
```

A start that answers `Cancel` delivers none of the Records of its transaction, and ends the subscription: a subscriber stops at a known point of the Log, and has the state of that moment. The handler takes two values of state here, the rounds and the last transaction to read:

```csharp
[Fact]
public void StopAtAKnownTransaction()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-07-stop", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Round(log, "Lyon", "PX-7021");
    Round(log, "Paris", "PX-7022");
    Round(log, "Lille", "PX-7023");
    var all = new Rounds();
    Follow(log, 0, all);

    // The rounds until the second transaction: the third one is not delivered, and the subscription ends at its start.
    var until = new Rounds();
    var subscription = log.SubscribeFrom(0)
        .OnTransactionStart(until, all.Closed[1].Transaction, static (transaction, until, last) =>
            transaction > last ? LogStoreSubscription.Cancel : until.Start(transaction))
        .Handle<ParcelScannedView, Rounds>(until, static (_, e, until) => until.Scanned(e.Depot))
        .OnTransactionEnd(until, static (transaction, until) => until.End(transaction))
        .Run();
    Assert.Equal(["Lyon", "Paris"], until.Closed.Select(static round => round.Depot));
    Assert.True(subscription.Completion.IsCompletedSuccessfully);
}
```

### Lesson 8: When a handler fails

A handler that throws ends its subscription: the Records after its own are not delivered. During the replay, its exception propagates from `Run`, and `Completion` holds it:

```csharp
[Fact]
public void GiveAFailureOfTheReplayToRun()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-replay", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Scan(log, "PX-8001", "Lyon");

    var subscription = log.SubscribeFrom(0)
        .Handle<ParcelScannedView>(static (_, e) => throw new FormatException($"The depot {e.Depot} is unknown."));
    Assert.Throws<FormatException>(() => subscription.Run());
    Assert.IsType<FormatException>(subscription.Completion.Exception!.InnerException);
}
```

After the replay, on the thread of the subscription, the exception faults `Completion`, which gives it to whoever waits for the subscription:

```csharp
[Fact]
public async Task FaultTheCompletionOnAFailureAfterTheReplay()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-later", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    var subscription = log.SubscribeFrom(0)
        .Handle<ParcelScannedView>(static (_, e) => e.Depot == "Atlantis" ? throw new FormatException($"The depot {e.Depot} is unknown.") : LogStoreSubscription.Continue)
        .Run();
    Scan(log, "PX-8002", "Lyon");
    Scan(log, "PX-8002", "Atlantis");

    // The thread of the subscription meets the failure; the task of its Completion gives it.
    await Assert.ThrowsAsync<FormatException>(() => subscription.Completion);
}
```

An exception is a failure, never a way to stop: a subscriber that has what it wants answers `Cancel` (Lesson 2). After a failure, the subscriber starts again from the transaction after the last one it completed, as Lesson 7 does: its position moves at the end of each transaction, never in the middle of one. The tracker of the lesson keeps the work of a transaction pending until its end, and forgets it at the start of the next one:

```csharp
// The depots that a parcel went through, among the known ones, and the last transaction completed: the position of the subscriber.
private sealed class Tracker(List<string> known)
{
    private readonly List<string> _pending = [];

    public List<string> Known => known;

    public List<string> Depots { get; } = [];

    public ulong Position { get; private set; }

    // The start of a transaction: nothing pending, even after a failure in the middle of one.
    public LogStoreSubscription Start()
    {
        _pending.Clear();
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Scanned(string depot)
    {
        if (!known.Contains(depot))
            throw new FormatException($"The depot {depot} is unknown.");
        _pending.Add(depot);
        return LogStoreSubscription.Continue;
    }

    // The end of a transaction: its depots kept, and the position moved, together.
    public LogStoreSubscription Completed(ulong transaction)
    {
        Depots.AddRange(_pending);
        _pending.Clear();
        Position = transaction;
        return LogStoreSubscription.Continue;
    }
}

private static void Follow(ParcelLog log, ulong from, Tracker tracker)
{
    using var end = new CancellationTokenSource();
    log.SubscribeFrom(from, end.Token)
        .OnTransactionStart(tracker, static (_, tracker) => tracker.Start())
        .Handle<ParcelScannedView, Tracker>(tracker, static (_, e, tracker) => tracker.Scanned(e.Depot))
        .OnTransactionEnd(tracker, static (transaction, tracker) => tracker.Completed(transaction))
        .Run();
    end.Cancel();
}
```

The tracker knows Lyon and Paris only. The subscription fails at the transaction of Atlantis, after completing the first one; the depot added, the tracker starts again after its position:

```csharp
[Fact]
public void StartAgainAfterTheLastTransactionCompleted()
{
    using var store = TransactionalMemoryStore.NewInMemoryLogStore("lesson-08-again", StoreAccessMode.ReadWrite);
    using var log = new ParcelLog(store);
    Scan(log, "PX-8003", "Lyon");
    Scan(log, "PX-8003", "Atlantis");
    Scan(log, "PX-8003", "Paris");

    // The depots known: Atlantis is not one of them yet. The subscription fails at its transaction, after completing the first.
    var tracker = new Tracker(["Lyon", "Paris"]);
    Assert.Throws<FormatException>(() => Follow(log, 0, tracker));
    Assert.Equal(["Lyon"], tracker.Depots);

    // The depot added to the known ones, the subscriber starts again after its position: the transaction that failed, then the others.
    tracker.Known.Add("Atlantis");
    Follow(log, tracker.Position + 1, tracker);
    Assert.Equal(["Lyon", "Atlantis", "Paris"], tracker.Depots);
}
```

## Part 3: The replay

### Lesson 9: A model and its Log

The Log of Part 1 told what happened; a model tells how things are now. Both together: each change of the parcels comes with the Record that tells it, in **one transaction**.

The model of Part 3 is a Repository of the parcels and their Log. Each Record names its parcel by its identity, `ParcelRef`, drawn by the command and kept in the Record:

```csharp
[Entity(21, 1, 5)]
[Root]
[Removable]
[WithIdentity]
[Enumerable]
internal interface MParcel
{
    [Property(1)]
    string TrackingNumber { get; set; }

    [Property(2)]
    string Recipient { get; set; }

    [Property(3)]
    ParcelStatus Status { get; set; }

    [Property(4)]
    string Depot { get; set; }

    [Property(5)]
    int Scans { get; set; }
}

[GhostRepository]
[IncludeEntityModel(nameof(MParcel))]
internal interface MCarrierRepository
{
}

[Record(31, 1, 3)]
internal interface MParcelRegistered
{
    // The identity of the parcel, drawn by the command, in the Record: its replay gives the parcel the same identity.
    [Property(1)]
    ParcelRef Parcel { get; set; }

    [Property(2)]
    string TrackingNumber { get; set; }

    [Property(3)]
    string Recipient { get; set; }
}

[Record(32, 1, 2)]
internal interface MParcelScanned
{
    [Property(1)]
    ParcelRef Parcel { get; set; }

    [Property(2)]
    string Depot { get; set; }
}

[Record(33, 1, 1)]
internal interface MParcelDelivered
{
    [Property(1)]
    ParcelRef Parcel { get; set; }
}

[GhostLog]
[IncludeRecordModel(nameof(MParcelRegistered))]
[IncludeRecordModel(nameof(MParcelScanned))]
[IncludeRecordModel(nameof(MParcelDelivered))]
internal interface MCarrierLog
{
}
```

A command adds its Record, then gives its View to the handler of its type, which changes the model: `Apply(e.Add().ToView())`. The handler is the only code that changes the parcels, and it reads the values of its Record only: the Log then holds everything the model is made of. The class of the commands also reads the model and the Log for the tests of the lesson, `Snapshot` and `Read`:

```csharp
// The commands of the carrier, and the handlers of their Records. A command adds the Record of what happens, then gives its View to
// the handler of its type, which changes the parcels; a replay gives the same Views to the same handlers (Lesson 10).
internal static class Parcels
{
    public static ParcelRef Register(string trackingNumber, string recipient)
    {
        var registered = new ParcelRegistered { Parcel = ParcelRef.New(), TrackingNumber = trackingNumber, Recipient = recipient }.Add();
        Apply(registered.ToView());
        return registered.Parcel;
    }

    public static void Scan(ParcelRef parcel, string depot) => Apply(new ParcelScanned { Parcel = parcel, Depot = depot }.Add().ToView());

    public static void Deliver(ParcelRef parcel) => Apply(new ParcelDelivered { Parcel = parcel }.Add().ToView());

    // The handlers: what each Record changes in the model, and nothing else.
    public static LogStoreSubscription Apply(ParcelRegisteredView e)
    {
        new Parcel(e.Parcel) { TrackingNumber = e.TrackingNumber, Recipient = e.Recipient, Status = ParcelStatus.Registered, Depot = "" };
        return LogStoreSubscription.Continue;
    }

    public static LogStoreSubscription Apply(ParcelScannedView e)
    {
        var parcel = e.Parcel.Resolve();
        parcel.Status = ParcelStatus.InTransit;
        parcel.Depot = e.Depot;
        parcel.Scans++;
        return LogStoreSubscription.Continue;
    }

    public static LogStoreSubscription Apply(ParcelDeliveredView e)
    {
        e.Parcel.Resolve().Status = ParcelStatus.Delivered;
        return LogStoreSubscription.Continue;
    }

    // The parcels of the model, one line each, in the order of their tracking numbers.
    public static List<string> Snapshot(CarrierRepository repository)
    {
        using var transaction = repository.OpenTransaction(StoreAccessMode.ReadOnly);
        var lines = new List<string>();
        foreach (var parcel in ParcelViewCollection)
            lines.Add($"{parcel.TrackingNumber} {parcel.Status} {parcel.Depot} {parcel.Scans}");
        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    // The Records of the Log, one line each, in its order.
    public static List<string> Read(CarrierLog log)
    {
        var lines = new List<string>();
        using var end = new CancellationTokenSource();
        log.SubscribeFrom(0, end.Token)
            .Handle<ParcelRegisteredView, List<string>>(lines, static (_, e, lines) => Add(lines, $"registered {e.TrackingNumber}"))
            .Handle<ParcelScannedView, List<string>>(lines, static (_, e, lines) => Add(lines, $"scanned at {e.Depot}"))
            .Handle<ParcelDeliveredView, List<string>>(lines, static (_, _, lines) => Add(lines, "delivered"))
            .Run();
        end.Cancel();
        return lines;
    }

    private static LogStoreSubscription Add(List<string> lines, string line)
    {
        lines.Add(line);
        return LogStoreSubscription.Continue;
    }
}
```

The Store of the Log is chained after the Store of the Repository, `AddChildStore`, before their first transaction, and the Log after the Repository, `Chain`. Each ReadWrite transaction of the Repository then opens a transaction of the Log, ambient, and its commit writes the parcels and the Records together:

```csharp
[Fact]
public void CommitTheRecordsAndTheParcelsTogether()
{
    // The Store of the parcels, then the Store of the Log after it; the Repository, the Log, and the Log chained to the Repository.
    using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-parcels", StoreAccessMode.ReadWrite);
    using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-journal", StoreAccessMode.ReadWrite));
    using var repository = new CarrierRepository(parcels);
    using var log = new CarrierLog(journal);
    repository.Chain(log);

    using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The transaction of the Log, opened with the one of the Repository: the commands add their Records to it.
        Assert.NotNull(CarrierLogTransaction.Current);
        var parcel = Parcels.Register("PX-9001", "Ada");
        Parcels.Scan(parcel, "Lyon");
        Assert.Equal(2, CarrierCollection.Count());
        transaction.Commit();
    }
    Assert.Equal(["PX-9001 InTransit Lyon 1"], Parcels.Snapshot(repository));
    Assert.Equal(["registered PX-9001", "scanned at Lyon"], Parcels.Read(log));
}
```

A transaction rolled back writes neither:

```csharp
[Fact]
public void WriteNeitherWithoutACommit()
{
    using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-rollback", StoreAccessMode.ReadWrite);
    using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-rollback-journal", StoreAccessMode.ReadWrite));
    using var repository = new CarrierRepository(parcels);
    using var log = new CarrierLog(journal);
    repository.Chain(log);

    using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Parcels.Register("PX-9002", "Bob");
        transaction.Rollback();
    }
    Assert.Empty(Parcels.Snapshot(repository));
    Assert.Empty(Parcels.Read(log));
}
```

Chained, the Log has no transaction of its own: a Record written alone would tell a change that the model never had. Its transaction opens and commits with the one of its Repository; a ReadOnly transaction changes nothing, and opens none:

```csharp
[Fact]
public void OpenTheLogThroughItsRepositoryOnly()
{
    using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-chained", StoreAccessMode.ReadWrite);
    using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-09-chained-journal", StoreAccessMode.ReadWrite));
    using var repository = new CarrierRepository(parcels);
    using var log = new CarrierLog(journal);
    repository.Chain(log);

    // Chained, the Log has no transaction of its own: a Record written alone would tell a change that the model never had.
    Assert.Throws<InvalidOperationException>(() => log.OpenTransaction());
    using (var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Parcels.Register("PX-9003", "Cy");
        Assert.Throws<InvalidOperationException>(() => CarrierLogTransaction.Current!.Commit());
        transaction.Commit();
    }

    // A ReadOnly transaction reads the parcels, and changes nothing: no Record to add.
    using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Null(CarrierLogTransaction.Current);
        Assert.Equal("Cy", ParcelViewCollection.Single(static parcel => parcel.TrackingNumber == "PX-9003").Recipient);
    }
}
```

Tutorial Seven shows the chains of several Stores.

### Lesson 10: Replay the Log

The Log holds everything the model is made of: the model can live in memory only, and the Log on disk. At each opening, the model is empty, and **behind its Log**: `IsBehindLog`, until it reaches `LogTarget`, the last transaction of the Log. A transaction of the commands is refused meanwhile: it would change a model that is not there yet.

A **replay** brings the model there. A subscription from the start of the Log gives each Record to the same handler as its command, in a **Replay transaction**, `OpenTransaction(StoreAccessMode.ReadWrite, replay: true)`, which opens no transaction of the Log: the Records are there already. Its commit, `Commit(through)`, says which transaction of the Log the model reached. A Replay transaction may hold many transactions of the Log, for fewer commits, through the last of them. The commit through the last transaction of the Log makes the model ready, even one without Records, which no handler receives: `OnReplayEnd` gives its number.

```csharp
// The replay of the Log into the model behind it: the same handlers as the commands, in Replay transactions, one for each `group`
// transactions of the Log, each committed through the last transaction of the Log it holds. Lessons 10 and 11.
internal sealed class Replay(CarrierRepository repository, int group)
{
    private CarrierRepositoryTransaction? _transaction;
    private int _delivered;

    // The Replay transactions committed.
    public int Commits { get; private set; }

    public static Replay Run(CarrierRepository repository, CarrierLog log, int group)
    {
        var replay = new Replay(repository, group);
        log.SubscribeFrom(0)
            .OnTransactionStart(replay, static (_, replay) => replay.Start())
            .Handle<ParcelRegisteredView>(static (_, e) => Parcels.Apply(e))
            .Handle<ParcelScannedView>(static (_, e) => Parcels.Apply(e))
            .Handle<ParcelDeliveredView>(static (_, e) => Parcels.Apply(e))
            .OnTransactionEnd(replay, static (transaction, replay) => replay.End(transaction))
            .OnReplayEnd(replay, static (last, replay) => replay.Finish(last))
            .Run();
        return replay;
    }

    // A transaction of the Log starts: a Replay transaction is open for it, ambient, the one of the transactions before or a new one.
    private LogStoreSubscription Start()
    {
        _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
        return LogStoreSubscription.Continue;
    }

    private LogStoreSubscription End(ulong transaction)
    {
        if (++_delivered % group == 0)
            Commit(transaction);
        return LogStoreSubscription.Continue;
    }

    // The end of the Log: the model commits through its last transaction, even one without Records, never delivered; it is then
    // ready, and the subscription ends.
    private LogStoreSubscription Finish(ulong last)
    {
        if (_transaction != null || repository.Store.IsBehindLog)
        {
            _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
            Commit(last);
        }
        return LogStoreSubscription.Cancel;
    }

    private void Commit(ulong through)
    {
        _transaction!.Commit(through);
        _transaction = null;
        Commits++;
    }
}
```

The lesson replays the Log transaction by transaction, then by groups of 100: four Replay commits for the four transactions of the commands, or one for all of them.

```csharp
[Theory]
[InlineData(1)]
[InlineData(100)]
public void RebuildAModelInMemoryAtEachOpening(int group)
{
    string directory = Path.Combine(AppContext.BaseDirectory, $"lesson-10-journal-{group}");
    if (Directory.Exists(directory))
        Directory.Delete(directory, recursive: true);

    // The first opening: an empty Log, an empty model. Four commands, in four commits.
    List<string> expected;
    using (var chain = new InMemoryChain(directory))
    {
        ParcelRef ada, bob;
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            ada = Parcels.Register("PX-10001", "Ada");
            transaction.Commit();
        }
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            bob = Parcels.Register("PX-10002", "Bob");
            transaction.Commit();
        }
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            Parcels.Scan(ada, "Lyon");
            Parcels.Scan(bob, "Lyon");
            transaction.Commit();
        }
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            Parcels.Scan(ada, "Paris");
            Parcels.Deliver(bob);
            transaction.Commit();
        }
        expected = Parcels.Snapshot(chain.Repository);
    }
    Assert.Equal(["PX-10001 InTransit Paris 2", "PX-10002 Delivered Lyon 1"], expected);

    // The second opening: the model is empty, behind its Log; the commands wait for the replay.
    using (var chain = new InMemoryChain(directory))
    {
        Assert.True(chain.Parcels.IsBehindLog);
        Assert.True(chain.Parcels.LogTarget > 0);
        Assert.Empty(Parcels.Snapshot(chain.Repository));
        Assert.Throws<InvalidOperationException>(() => chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite));

        // The replay: four Replay commits for four transactions of the Log, or one for all of them.
        var replay = Replay.Run(chain.Repository, chain.Log, group);
        Assert.Equal(group == 1 ? 4 : 1, replay.Commits);
        Assert.False(chain.Parcels.IsBehindLog);
        Assert.Equal(expected, Parcels.Snapshot(chain.Repository));

        // Ready: the commands go on, in the model and in the Log.
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            Parcels.Register("PX-10003", "Cy");
            transaction.Commit();
        }
        Assert.Equal("PX-10003 Registered  0", Parcels.Snapshot(chain.Repository)[^1]);
        Assert.Equal((7, "registered PX-10003"), (Parcels.Read(chain.Log).Count, Parcels.Read(chain.Log)[^1]));
    }
}
```

The chain of the lesson keeps the parcels in memory, and their Log on disk, after them:

```csharp
// The chain of the lesson: the parcels in memory, and their Log on disk, after them.
private sealed class InMemoryChain : IDisposable
{
    public InMemoryChain(string directory)
    {
        Parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-parcels", StoreAccessMode.ReadWrite);
        Journal = Parcels.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
        Repository = new CarrierRepository(Parcels);
        Log = new CarrierLog(Journal);
        Repository.Chain(Log);
    }

    public TransactionalMemoryStore Parcels { get; }

    public TransactionalMemoryStore Journal { get; }

    public CarrierRepository Repository { get; }

    public CarrierLog Log { get; }

    public void Dispose()
    {
        Log.Dispose();
        Repository.Dispose();
        Parcels.Dispose();
        Journal.Dispose();
    }
}
```

A replay gives the same model each time, as the handlers read the values of the Records only. `ParcelRef.New()` is refused in a Replay transaction: a new identity would differ at each replay. The identities come from the Records, drawn once by the commands:

```csharp
[Fact]
public void RefuseANewIdentityInAReplay()
{
    using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-identity", StoreAccessMode.ReadWrite);
    using var journal = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("lesson-10-identity-journal", StoreAccessMode.ReadWrite));
    using var repository = new CarrierRepository(parcels);
    using var log = new CarrierLog(journal);
    repository.Chain(log);
    using var transaction = repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
    Assert.Null(CarrierLogTransaction.Current);
    Assert.Throws<InvalidOperationException>(() => ParcelRef.New());
}
```

### Lesson 11: Rebuild a model

A model on disk and its Log on disk: opened again, the model is there, ready, without a replay. It is rebuilt on request only. `RebuildFromLog()` on the Store of the model, once the Stores are chained and before the first transaction, empties the model and keeps the Log. The model is then behind its Log, and the replay of Lesson 10 brings it back, through the handlers as they are now. A handler changed, a projection added, a model damaged: the Log gives the model again.

```csharp
// The chain of the lesson: the parcels on disk, their Log on disk after them; rebuilt from the Log when asked.
private sealed class FileChain : IDisposable
{
    public FileChain(string directory, bool rebuild)
    {
        Parcels = TransactionalMemoryStore.NewFileRepositoryStore(directory, "parcels", StoreAccessMode.ReadWrite);
        Journal = Parcels.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
        if (rebuild)
            Parcels.RebuildFromLog();
        Repository = new CarrierRepository(Parcels);
        Log = new CarrierLog(Journal);
        Repository.Chain(Log);
    }

    public TransactionalMemoryStore Parcels { get; }

    public TransactionalMemoryStore Journal { get; }

    public CarrierRepository Repository { get; }

    public CarrierLog Log { get; }

    public void Dispose()
    {
        Log.Dispose();
        Repository.Dispose();
        Parcels.Dispose();
        Journal.Dispose();
    }
}
```

The Log holds the Records, not the model: a change that no Record tells is lost at a rebuild. The lesson moves a parcel without a Record, then rebuilds the model: the parcel is where its Records say it is.

```csharp
[Fact]
public void RebuildAModelOnDiskOnRequest()
{
    string directory = Path.Combine(AppContext.BaseDirectory, "lesson-11-carrier");
    if (Directory.Exists(directory))
        Directory.Delete(directory, recursive: true);

    List<string> expected;
    using (var chain = new FileChain(directory, rebuild: false))
    {
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            var ada = Parcels.Register("PX-11001", "Ada");
            Parcels.Scan(ada, "Lyon");
            transaction.Commit();
        }
        expected = Parcels.Snapshot(chain.Repository);
    }

    // Opened again: the model is on disk, ready. A transaction changes a parcel without a Record: the Log does not know it.
    using (var chain = new FileChain(directory, rebuild: false))
    {
        Assert.False(chain.Parcels.IsBehindLog);
        Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
        using (var transaction = chain.Repository.OpenTransaction(StoreAccessMode.ReadWrite))
        {
            ParcelCollection.Single(static parcel => parcel.TrackingNumber == "PX-11001").Depot = "Nowhere";
            transaction.Commit();
        }
        Assert.Equal(["PX-11001 InTransit Nowhere 1"], Parcels.Snapshot(chain.Repository));
    }

    // Rebuilt: the model empty, behind its Log, then replayed. The parcel is where its Records say it is.
    using (var chain = new FileChain(directory, rebuild: true))
    {
        Assert.True(chain.Parcels.IsBehindLog);
        Assert.Empty(Parcels.Snapshot(chain.Repository));
        Replay.Run(chain.Repository, chain.Log, group: 1000);
        Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
    }

    // The rebuilt model is on disk: the next opening finds it ready.
    using (var chain = new FileChain(directory, rebuild: false))
    {
        Assert.False(chain.Parcels.IsBehindLog);
        Assert.Equal(expected, Parcels.Snapshot(chain.Repository));
    }
}
```

In a model rebuilt from its Log, every change comes from a handler of a Record, and from nothing else.

## From the need to the Log

| The need | The answer | Lessons |
| -------- | ---------- | ------- |
| Keep what happened, in its order, for good | A Log of Records, `[GhostLog]`, `[Record]` | 1, 5 |
| Prepare a fact anywhere, then record it | A detached Record, `Add()` in a transaction | 3 |
| Read what happened | A subscription, `SubscribeFrom`, `Handle`, `Run` | 2 |
| Keep a screen, a cache, a service up to date | A subscription that follows the Log, `OnReplayEnd` | 6 |
| Apply each commit as a whole | `OnTransactionStart`, `OnTransactionEnd` | 7 |
| Start again where a subscriber stopped | `SubscribeFrom(n)`, its position at the end of each transaction | 7, 8 |
| Change a model and record why, together | A chain, `AddChildStore`, `repository.Chain(log)` | 9 |
| A model in memory, given again at each opening | A replay, `replay: true`, `Commit(through)` | 10 |
| A model on disk, rebuilt by new handlers | `RebuildFromLog()`, then a replay | 11 |

## What you learned

- A **Record** is a fact: declared as an entity, `[Record]` in place of `[Entity]`; created anywhere, on any thread, detached; added by `Add()` in a transaction of its Log; frozen once added.
- A **Log** keeps its Records in the order of their commits, on a Log Store in memory or on disk: a commit appends them together, a transaction that does not commit appends nothing.
- A Record takes the **values** of an entity, read in place by its View; it names the entities by their identity, a `*Ref`, never by a link.
- A **subscription** reads the Log from any of its transactions, each Record to the handler of its type, in order; a handler takes its state before it, and captures nothing.
- A subscription **follows** the Log after its replay, on a thread of its own, its handlers never called together; it ends by `Cancel`, by its token, or with its Log; `Completion` tells how.
- **The frames** of the transactions apply each commit as a whole; the number of a transaction is a position to start again from.
- A **failure** of a handler ends its subscription, given to `Run` or to `Completion`; `Cancel` stops, never an exception.
- A model **chained** with its Log is written with it, in one transaction; a command adds its Record, and its handler changes the model.
- A **replay** gives the model again, in memory at each opening, on disk on request: the same handlers, Replay transactions, identities taken from the Records.

The next tutorial shows the chains of Stores, the transactions over several Stores, and the distribution.

**Next tutorial:** [Tutorial Seven: Chains and Distribution](Seven-ChainsAndDistribution.md)
