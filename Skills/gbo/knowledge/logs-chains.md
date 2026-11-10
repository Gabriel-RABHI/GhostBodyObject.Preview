# Records, Logs, subscriptions and chains

**Prerequisite:** [../SKILL.md](../SKILL.md). A Log keeps **Records**, facts, in the order of their commits: appended, never changed, never removed, no query, no index. It is read from a point onwards by a **subscription**. A **chain** makes several Stores one transaction: the models and the Log of what happened to them, all or nothing. For a whole system designed around a Log, see also [mutation-sourcing.md](mutation-sourcing.md).

## Declare Records and a Log

```csharp
[Record(31, 1, 3)]                                    // type identifier (unique across the chain), version (1..16383), highest property id
internal interface RParcelRegistered
{
    [Property(1)] ParcelRef Parcel { get; set; }      // a fact names entities by identity, never by a link (GBO020)
    [Property(2)] GhostString TrackingNumber { get; set; }
    [Property(3)] GhostString Recipient { get; set; }
}

[Record(32, 1, 2)]
internal interface RParcelScanned
{
    [Property(1)] ParcelRef Parcel { get; set; }
    [Property(2)] GhostString Depot { get; set; }
}

[GhostLog(8), IncludeRecordModel(nameof(RParcelRegistered)), IncludeRecordModel(nameof(RParcelScanned))]
internal interface MCarrierLog { }
```

- `MCarrierLog` gives `CarrierLog`, `CarrierLogTransaction`, `CarrierLogSubscription`, `ICarrierRecord`, `ICarrierRecordView`, and `CarrierLogCollections.CarrierCollection` (the Records of the open Log transaction): the prefix is the name without `M` and without its suffix `Log`, `GhostLog` or `LogStore`.
- A Record holds the values of an entity (numbers, decimals, dates, enums, `Option<T>`, `GhostString`, `GhostSpan<T>`, explicit structs, converters), and `*Ref` identities, also as `GhostSpan<XRef>` elements and fields of its structs. No link, no identity of its own, never `[Root]`, `[Removable]` nor `[Enumerable]` (GBO011); `Option<XRef>` is refused: `XRef` and `IsNull`.
- A list in a Record: a `GhostSpan` of values or identities (parallel spans of one length), a `GhostSpan` of a stored struct of 32 bytes at most, or one Record per item after its head in the same transaction (the item has texts).
- A `*Ref` is recognized by the name of its model (`CustomerRef` → the `MCustomer` with `[Entity]` and `[WithIdentity]`): if several namespaces of the compilation declare one, declare the Record in the namespace of the one it means (GBO002 otherwise).
- A Log is always **Fixed**: its Records live forever.

## Write Records

```csharp
using var store = TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite);
using var log = new CarrierLog(store);

var scan = new ParcelScanned { Parcel = parcel, Depot = "Lyon" };   // detached: anywhere, any thread, outside any transaction
using (var tx = log.OpenTransaction())                               // a Log transaction is always a write
{
    scan.Add();                                                      // takes its place after the Records added before it; frozen
    tx.Commit();                                                     // appends them together; a rollback appends nothing
}
```

- **Detached**, a Record is written as an object of C#; **added**, by `Add()` in a transaction of its Log open in the context, it is frozen: its Setters throw. `Add()` returns it: `record.Add().ToView()` gives its View at once. `ToView()` is refused on a detached Record, and after its transaction closes.
- The order of the Log is the order of the `Add`, then the order of the commits. Two Log transactions never conflict. One Log transaction at a time per context.
- A Record never added is never written. A Record is added once.

## Read: subscriptions

```csharp
using var end = new CancellationTokenSource();
var subscription = log.SubscribeFrom(0, end.Token)                   // from a transaction number, included; 0 for the first
    .OnTransactionStart(board, static (tx, b) => b.Start(tx))         // optional
    .Handle<ParcelScannedView, Board>(board, static (tx, e, b) => b.Scanned(e.Parcel, e.Depot.ToString()))
    .Handle<ParcelRegisteredView>(static (tx, e) => LogStoreSubscription.Continue)
    .OnTransactionEnd(board, static (tx, b) => b.End(tx))             // optional: the position moves here
    .OnReplayEnd(board, static (last, b) => b.Ready(last))            // optional: caught up, the last transaction number
    .Run();
```

- `Run()` delivers, **on the calling thread**, every transaction committed from `from` (the replay), calls `OnReplayEnd`, then returns; the subscription then **stays active** and delivers each new commit on a thread of its own, until a handler returns `LogStoreSubscription.Cancel`, the token is cancelled, the Log is disposed, or a handler throws. `subscription.Completion` (a `Task`) tells how it ended.
- Each handler takes the transaction number, the View of the Record, then its states (1 to 4, passed before it): it captures nothing. A Record type without a handler is skipped. Handlers are never called together: a projection needs no lock.
- A View reads the Record where the Log keeps it, without copy, and stays readable after its delivery.
- **Position and restart**: GBO keeps no position. A subscriber saves the last transaction it completed (at `OnTransactionEnd`, in the same transaction as its projection when it writes a model), and restarts with `SubscribeFrom(position + 1)`: exactly once.
- **A handler that throws** ends the subscription: during `Run()` the exception propagates from `Run()`; after, it faults `Completion`. An exception is a failure, never a way to stop: return `Cancel`.
- A transaction without Records (a commit of a chain that wrote none) is not delivered.
- **One reader of several Logs of one Store**, for messages, Jobs and Commands: each Log registers its handlers on it, and each Record reaches its handler in the order of the Store, whatever its Log.

```csharp
var replay = journal.Replay(from: 0);                                   // journal: the Log Store
sales.On(replay).Handle<OrderPlacedView, Board>(board, static (tx, e, b) => b.Placed(e));
billing.On(replay).Handle<InvoiceSentView, Board>(board, static (tx, e, b) => b.Sent(e))
    .OnTransactionEnd(board, static (tx, b) => b.Done(tx));             // the frame handlers belong to the reader
replay.Run();                                                           // Completion on the reader
```

- **Two Logs of one Store chained to a Repository**, `repository.Chain(sales); repository.Chain(billing);`: one commit writes the Records of both, in the order of their `Add`, across the Logs; two Logs alone commit by turns. Each Log keeps its own collection in the transaction.
- **In tests**: `log.Store.DeliverTransactions(subscription)` delivers on the calling thread what the subscription has not received yet.

## Chains of Stores

```csharp
using var parcels = TransactionalMemoryStore.NewInMemoryRepositoryStore("parcels", StoreAccessMode.ReadWrite);
using var tracking = parcels.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("tracking", StoreAccessMode.ReadWrite));
using var journal = tracking.AddChildStore(TransactionalMemoryStore.NewFileLogStore(directory, "journal", StoreAccessMode.ReadWrite));
using var carrier = new CarrierRepository(parcels);   // the master
using var site = new TrackingRepository(tracking);
using var log = new CarrierLog(journal);
carrier.Chain(site);                                   // after the Stores, in the same order
site.Chain(log);

using (var tx = carrier.OpenTransaction(StoreAccessMode.ReadWrite))
{
    // TrackingRepositoryTransaction.Current and CarrierLogTransaction.Current are open, ambient
    var ada = Parcels.Register("PX-3001", "Ada");      // adds its Record and applies it to both models
    tx.Commit();                                       // writes the three Stores, or none
}
```

- **The Stores are chained first** (`AddChildStore` returns the child, to chain the next), before their first transaction; then the Repositories and the Log, in the same order (`Chain` checks it, and checks the type identifiers are unique across the chain). A Repository chains one child: a Repository or a Log; a Log may chain an output Log (`mutations.Chain(jobs)`).
- **Only the master opens transactions**: a transaction of the first Repository opens those of the others, in the same mode, ambient; a ReadWrite one, except a Replay, opens the Log transaction; a ReadOnly one opens none. A chained Repository or Log opened alone, or committed alone, throws.
- **All or nothing**: a rollback, a condemned Store, or a conflict in any Store writes no Store.
- Every Store of a chain has the same access mode and persistence, **except** volatile Repository Stores before the persistent Logs that end the chain (a Mutation Log, then its output Logs: `mutations.AddChildStore(jobsStore)`): they are rebuilt from their Log at each opening. A ReadWrite chain never changes: the same Stores, the same order, the same names, from one opening to the next.
- Dispose the Log and the Repositories first, then the Stores.
- A projection in another Store takes the identity of its source: `TrackingPageRef.Of(parcelRef)`.

## A StoreSystem

A system declares the Stores of an application, their roles and their models; the generator writes its class, which opens and chains them in the order of the engine (REQUIREMENT-060). Its transaction is the transaction of the chain.

```csharp
[StoreSystem(1)]
internal interface MParcelSystem
{
    [SystemStore(1, StoreSystemKind.Projection), IncludeRepository(nameof(MCarrierRepository)), IncludeRepository(nameof(MTrackingRepository))]
    ISystemStore Parcels { get; }

    [SystemStore(2, StoreSystemKind.ReplayableLog), IncludeLog(nameof(MCarrierLog))]
    ISystemStore Journal { get; }
}

// The other part of the class: the handlers of the replay, the developer's; for a Log of Mutations, HandleMutations().
internal partial class ParcelSystem
{
    protected override void OnReplay(LogStoreReplay replay)
        => CarrierLog.On(replay).Handle<ParcelRegisteredView>(static (_, e) => Parcels.Apply(e)).Handle<ParcelScannedView>(static (_, e) => Parcels.Apply(e));
}

using var parcels = new ParcelSystem(new StoreSystemOptions { Directory = directory, VolatileProjections = [1] });
parcels.Start();                                       // behind its Log, replays it, then Live
using (var tx = parcels.OpenTransaction())             // the transaction of CarrierRepository, the master
{
    var ada = Parcels.Register("PX-3001", "Ada");
    tx.Commit();                                       // the Repositories of the Store and the Log, or none
}
```

- **The roles**: `Projection`, derived from the replayable Log, rebuilt by a replay, first in the chain; `State`, written by the application, never replayed, aligned with the Log; `ReplayableLog`, one at most; `Log`, an output Log, one per Store after the first.
- **Several Repositories on one Store** share its transaction: the first is chained, the others join it, `first.Join(other)`.
- **The replay at `Start()`**: behind its Log, the system replays it in replay transactions of `ReplayGroup` transactions of the Log, by the handlers of `OnReplay`, the reads going on, then is `Live`. Without `OnReplay`, or with a handler that fails, it closes, `Error`. The handlers never frame the transactions: the system does.
- **The evolution of the model**, decided by the system before any bind writes (REQUIREMENT-055): a change that the Ghosts absorb (a value added) is written as before; a Projection changed so that its Ghosts cannot take it alone (a link removed, a type become Removable), or `ReplayNumber` raised, rebuilds the Projections and replays them; a State in that case, or `MigrationNumber` raised with a State, is a migration: the binary raises `MigrationNumber` and the developer writes the step in `private partial void Migrate({System}Migration migration)`, which the generator declares and the compiler requires; with equal numbers the system is `Blocked`. The step runs in the exclusive transaction of the master, before `Start` returns: `foreach (var old in migration.Versions<ParcelView_v1>()) migration.Upgrade(old).Depot = ...`, each step written towards the current model, `if (migration.From < n)`. Its end is checked (no Ghost left in a changed version, no required link at none, no value of no member); a failure writes nothing and blocks the system, the exception inner. A link that the new version removes is released by the engine once the step ends: the counter and the reverse set of its target; the parts of a removed composition that the step did not move are removed, so the step moves those to keep, `part.ToBody().MoveTo(owner.NewCollection)`, after `migration.Upgrade(old)` of their Owner; a component whose entity type left is removed by its move up; a type become Removable is moved up and counted again by the engine, the step writing nothing. A newer model in a Store blocks an older node, at its start or at the transaction that brings it, its commit rejected (`TransactionConflictKind.Schema`). Each Store keeps the record of the system, its numbers and its journal: `system.Record(store)`.
- **The typed change feed of a Repository**, `repository.Feed()` (REQUIREMENT-061): its handlers by type, with state values, `Handle<OrderView, TState>(state, static (change, order, state) => ...)` for `Living`, `Created` and `Changed`, `Removed<OrderView, TState>` by an `EntityKey` (`EntityKey.Of(view)` at the creation), `TransactionEnd`; `Start()` gives each living entity, then each published change, each entity once per transaction, on the thread of the listener of the Store. The View is valid during the call; a link needs a transaction that the handler opens. A feed by state: after transactions lost to it, each living entity again. A handler that throws stops it, `Failed`. Dispose it.
- **The history of the versions**, which a migration reads: `ModelWriter.WriteHistory(StoreSchema.Read(store)[i])` writes each version of each entity type below its latest one, `M{Type}_v{n}` in `{Namespace}.VersionHistory`; the file added to the project, the generator writes `{Type}View_v{n}`. Never edit it: the system checks it at its start against the Store, a difference `Error`.
- **Out of `Live`**: in `Replay`, a read transaction only; in `Init`, `Error` or `Closed`, none.

## Event Sourcing on GBO

A command checks against the Write model, draws the identities of what it creates, creates each Record complete, adds it, and gives its View at once to **the same handler that the Replay uses**; the handler changes the Write and the Read models; one commit writes the models and the Log.

```csharp
internal static class Parcels
{
    public static ParcelRef Register(string trackingNumber, string recipient)
    {
        var registered = new ParcelRegistered { Parcel = ParcelRef.New(), TrackingNumber = trackingNumber, Recipient = recipient }.Add();
        Apply(registered.ToView());
        return registered.Parcel;
    }

    public static void Scan(ParcelRef parcel, string depot) => Apply(new ParcelScanned { Parcel = parcel, Depot = depot }.Add().ToView());

    // The handlers: the only code that writes the models; they read the Record and the models, nothing else.
    public static LogStoreSubscription Apply(ParcelRegisteredView e)
    {
        new Parcel(e.Parcel) { TrackingNumber = e.TrackingNumber, Recipient = e.Recipient, Status = ParcelStatus.Registered };
        return LogStoreSubscription.Continue;           // so that it serves as a subscription handler too
    }

    public static LogStoreSubscription Apply(ParcelScannedView e)
    {
        var parcel = e.Parcel.Resolve();
        parcel.Status = ParcelStatus.InTransit;
        parcel.Depot = e.Depot;                         // a ReadOnlyGhostString copied once into the entity
        return LogStoreSubscription.Continue;
    }
}
```

**The rules of a handler**: deterministic, it reads its Record and the models only; never the clock, a random number, a configuration, nor `XRef.New()` (it throws in a Replay); everything the command decided (identities, prices, numbers, dates) is in the Record. A missing target is a bug of the Log: `Resolve()`, which throws. A Read projection that needs the Write state before a removal runs before the Write removal.

**A refused command** returns a result before its first Record (`using` rolls back), never an exception. **A conflict** is retried by running the same command again (`TryCommit`). Commands in parallel conflict on shared entities: route them by aggregate to fixed threads, or move to Mutation Sourcing (one writer).

## Replay

A model chained with a persistent Log may live in memory: at each opening it is empty and **behind its Log** (`store.IsBehindLog`, up to `store.LogTarget`), and a ReadWrite transaction that is not a Replay is refused until it catches up. A model on disk is ready at its opening, and rebuilt on request by `store.RebuildFromLog()` on the master Store, after the chaining of the Stores and before the first Repository.

```csharp
internal sealed class Replayer(CarrierRepository repository, int group)
{
    private CarrierRepositoryTransaction? _transaction;
    private int _pending;

    public LogStoreSubscription Open()
    {
        _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);   // opens no Log transaction
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription End(ulong transaction)
    {
        if (++_pending == group)
            Commit(transaction);
        return LogStoreSubscription.Continue;
    }

    public LogStoreSubscription Finish(ulong last)
    {
        if (_transaction != null || repository.Store.IsBehindLog)
        {
            _transaction ??= repository.OpenTransaction(StoreAccessMode.ReadWrite, replay: true);
            Commit(last);                              // through the last transaction of the Log: the chain is ready
        }
        return LogStoreSubscription.Cancel;            // a live subscription would apply the next commands twice
    }

    private void Commit(ulong through)
    {
        _transaction!.Commit(through);                 // the models reached this transaction of the Log
        (_transaction, _pending) = (null, 0);
    }
}

var replayer = new Replayer(carrier, group: 10_000);
log.SubscribeFrom(0)
    .OnTransactionStart(replayer, static (_, r) => r.Open())
    .Handle<ParcelRegisteredView>(static (_, e) => Parcels.Apply(e))
    .Handle<ParcelScannedView>(static (_, e) => Parcels.Apply(e))
    .OnTransactionEnd(replayer, static (tx, r) => r.End(tx))
    .OnReplayEnd(replayer, static (last, r) => r.Finish(last))
    .Run();
```

- A Replay transaction may hold many Log transactions, for fewer commits; `Commit(through)` is refused out of a Replay or on a ready chain.
- In a Replay, `XRef.New()` throws: every identity comes from the Records; `XRef.Of(...)` is allowed.
- **A change that no Record tells is lost at a rebuild**: in a model rebuilt from its Log, every change comes from a handler of a Record.
- The Replay is a migration: a model changed, a projection added, a handler fixed, the Log gives the model again. This is why the models behind a Log may use a Deduced schema.

## CQRS: the Read model

| Read model | How | When |
|---|---|---|
| **Synchronous, chained**: Write → Read → Log in one transaction | The handlers write both models; read it through a ReadOnly transaction of the master | Always consistent; the default |
| **Asynchronous, unchained**: a Repository on its own Store, fed by a live subscription of the Log | `OnTransactionStart` opens a ReadWrite transaction of the Read Repository, the handlers project, `OnTransactionEnd` saves the position in a checkpoint entity and commits | A Read model on other servers, or rebuilt independently; eventual consistency |
| **Remote**: a replica of the Read Store alone, `ReadOnly`, on another server | Distribute the chain; the front end holds only the Store it reads ([infrastructure.md](infrastructure.md)) | Many readers |

## Outbox and side effects

A side effect (a mail, a PDF, a call) never runs inside a transaction that may be replayed or rolled back. Write it as a Record of an **output Log** chained to the transaction (`log.Chain(output)` after `AddChildStore`): it exists only if the commit succeeds. A service subscribed to the output Log performs it after the commit, idempotently, by an identity carried in the Record. A result returns as a new command or Mutation.
