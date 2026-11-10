# Designing a system in Mutation Sourcing

**Prerequisite:** [../SKILL.md](../SKILL.md), [modeling/README.md](modeling/README.md) for the models, [logs-chains.md](logs-chains.md) for Logs, chains and Replay.

Mutation Sourcing merges the Command and the Event of Event Sourcing into one object, the **Mutation**: a Record that carries the **Intention** of the application and the **Decision** of the domain, recorded whole in the Log. The Mutation carries its own code: it decides, adds itself to the Log, and applies itself.

| Architecture | The path of the data |
|---|---|
| Event Sourcing | Command → Event(s); Event → Write model; Event → Read model |
| Mutation Sourcing | Mutation → Write model and Read model |

**Choose it for a business domain** that needs history, audit, replay and simple concurrency: no command classes, no event classes, no handler registry, no copy of fields from a command to an event, one unit of code per use case, a single writer (no conflicts on one server), and models rebuilt by a Replay (a migration with no script). The Log is the truth; every model is a projection.

## The architecture

```
Application threads ──Post──► GhostMutationPump (one queue, one thread, batches of ≤256 or 1 ms)
        ▲                          │ Submit(): Decide → Add → Apply, in one transaction of the chain
        │ Wait / Status / Failure  ▼
        │              Write model ─► Read model ─► Mutation Log (file, Fixed) [─► Jobs Log]
        │              (in memory, Deduced)  (in memory, Deduced)
        └──────── ReadOnly transactions on any thread (MVCC: never wait for the pump)
At start: the models are behind the Log → Replay: subscription .HandleMutations() → only Apply runs
```

```csharp
var write = TransactionalMemoryStore.NewInMemoryRepositoryStore("shop-write", StoreAccessMode.ReadWrite);
var read = TransactionalMemoryStore.NewInMemoryRepositoryStore("shop-read", StoreAccessMode.ReadWrite);
var mutations = TransactionalMemoryStore.NewFileLogStore(directory, "mutations", StoreAccessMode.ReadWrite);
write.AddChildStore(read).AddChildStore(mutations);
write.FlushPolicy = FlushPolicy.FastOnDisk;          // on the master, before the first Repository
write.LockPolicy = StoreLockPolicy.Retain;           // the only writer process
var shop = new WriteModelRepository(write);
var readModel = new ReadModelRepository(read);
var log = new ShopMutationLog(mutations);
shop.Chain(readModel);
readModel.Chain(log);

if (write.IsBehindLog)                               // the models in memory: replay the Log first
    Replay(shop, log);

var pump = new GhostMutationPump(shop) { Exclusive = true };   // one server: the only writer, no conflicts
```

The names of the in-memory Stores stay the same from one opening to the next. Dispose the pump first, then the Log, the Repositories, the Stores.

## Step 1: one Mutation per use case

- **Name it after its action**, a noun: `QuoteCreation`, `LineAddition`, `OrderPlacement`, `CustomerRegistration`, `AccountClosure`. The same word says the request ("a quote creation requested") and the fact ("quote creation no. 17"). Never the imperative (`CreateQuote`: a replay would replay an order) nor the participle (`QuoteCreated`: false before it exists).
- **One Mutation per command of the application.** A request with many effects is one Mutation and its sub-Mutations, never several events.
- **Type identifiers**: a range per area, unique across the chain (`Chain()` checks): Mutations of the lines 2200-2399.

## Step 2: the Intention, public

What the application knows and asks, written once by whoever composes the Mutation, outside any transaction, frozen once posted:

| Put in the Intention | Example |
|---|---|
| The target, by identity | `QuoteRef Quote`, `CustomerRef Customer` |
| **The identity of what it creates**, drawn by the application with `XRef.New()` | `OrderRef Order`, `QuoteLineRef Line`: a retry never creates two orders; the application finds its result by this identity, `ReadOrderRef.Of(order)` |
| The values entered | `GhostString Description`, `int Quantity`, `int LineNo` (the place asked) |
| The time and the user | `DateTime At`, `Guid User`: `Decide` never reads the clock |
| A list | `GhostSpan<ProductRef> Products` and `GhostSpan<int> Quantities` of one length; a `GhostSpan` of a stored struct ≤ 32 bytes |

**Keep the Intention minimal**: it is a durable schema, stored forever, versioned by property numbers. Values only used to validate on a screen stay out. Personal data in a Log that is never erased is a decision.

## Step 3: the Decision, `[Private]`

Everything the domain decided, so that `Apply` never decides again:

| Put in the Decision | Example |
|---|---|
| Numbers and counters allocated | `DraftNumber`, `FinalNumber`: from a counter on a Root (`company.NextDraftNumber()`) |
| Computed values | Prices, totals, VAT, rounding, normalizations, a reference text, an expiry date `At.AddMonths(1)` |
| What a search found | The pricing applied, the group of a new line, the lines to renumber with their new numbers (a `GhostSpan<LineNumber>` of `{ QuoteLineRef Line; int Number; int Reserved }`) |
| Identities drawn by the domain | The lines of a copy, the identity of a Job |

`[Private]` generates these members `private` in the Body and the View: only the partial classes of the Mutation read and write them; the application cannot. The Decision is reset when the pump submits a Mutation again.

```csharp
[Record(34, 1, 5), Mutation]
internal interface ROrderPlacement
{
    [Property(1)] OrderRef Order { get; set; }                     // the Intention
    [Property(2)] CustomerRef Customer { get; set; }
    [Property(3)] GhostSpan<ProductRef> Products { get; set; }
    [Property(4)] GhostSpan<int> Quantities { get; set; }
    [Property(5), Private] GhostSpan<decimal> UnitPrices { get; set; }   // the Decision
}

[GhostLog(11)]
[IncludeRecordModel(nameof(ROrderPlacement))]
// ... one line per Mutation
internal interface MShopMutationLog { }
```

## Step 4: `Decide`, in the partial class of the Body

```csharp
internal sealed class ShopRefused(string reason) : Exception(reason);    // the refusal of the domain: its own type

internal partial class OrderPlacement
{
    private partial void Decide()
    {
        int count = Products.Length;
        if (count is 0 or > 40 || count != Quantities.Length || !Customer.ResolveOrNone().Any())
            throw new ShopRefused("An order takes 1 to 40 lines, for a customer.");
        if (Order.IsNull || Order.ResolveOrNone().Any())
            throw new ShopRefused("The order exists already.");
        var prices = new decimal[count];
        for (int i = 0; i < count; i++)
        {
            if (!Products[i].ResolveOrNone().TryGet(out var product) || Quantities[i] <= 0 || product.Stock < Quantities[i])
                throw new ShopRefused("A line takes a product in stock.");
            prices[i] = product.Price;                                   // decided: the price at the time of the order
        }
        UnitPrices = prices;
    }
}
```

| `Decide` may | `Decide` never |
|---|---|
| Read the models (Write and Read), navigate, query the Views, use an index | Act outside the transaction: no file, network, mail, service call (it may run several times, and for a Mutation never accepted) |
| Write its Decision | Write the models (that is `Apply`) |
| Draw identities (`XRef.New()`) into its Decision or into the Intention of sub-Mutations | Read the clock or a random value for a business decision without writing the result in the Decision |
| Compose and `Submit()` sub-Mutations | Change its Intention (its Setters refuse once posted) |
| Create Jobs: `new PdfRendering { ... }.Add()` | Catch the failure of a sub-Mutation |
| Refuse by `throw` of an exception of the domain, a status that cannot move asked by `status.CanTransitionTo(target)` ([modeling/values.md](modeling/values.md)) | Return a result code |

**A failure is an exception of the domain**, a type the developer designs (`QuoteRefused(Refusal reason)`), thrown in a straight line: `throw new QuoteRefused(Refusal.NotFound)`. GBO never catches it on its way: the generated `Submit()` only observes it (an exception filter). If nothing was written since the Mutation started (no sub-Mutation applied), the transaction stays alive and the pump goes on with the next Mutation of its batch; if something was written, the transaction is condemned, and the pump runs the batch again without the failed Mutation. Only the pump catches, at its boundary, and hands the exception to the poster in `Failure`. A failure of a sub-Mutation fails every mother that submitted it: no Mutation tests the result of its children. This is the one place where a refusal is an exception; elsewhere, an exception is a bug.

## Step 5: `Apply`, in the partial struct of the View

```csharp
internal readonly partial struct OrderPlacementView
{
    internal partial void Apply()
    {
        var customer = Customer.Resolve();
        var order = new Order(Order, customer) { Status = OrderStatus.Placed };
        var account = CustomerAccountRef.Of(Customer).Resolve();           // the Read model, by derived identity
        var summary = account.Orders.New(OrderSummaryRef.Of(Order));
        var products = Products;
        var quantities = Quantities;
        var prices = UnitPrices;
        for (int i = 0; i < products.Length; i++)
        {
            var product = products[i].Resolve();
            var line = order.Lines.New(product);
            (line.Quantity, line.UnitPrice) = (quantities[i], prices[i]);
            product.Stock -= quantities[i];
            summary.Total += quantities[i] * prices[i];
        }
    }
}
```

`Apply` always runs **as a Replay**, live and at the Replay: `Submit()` calls it under a `ReplayContext`. So it has one form only:

| `Apply` | |
|---|---|
| Reads the Mutation (the Decision for what was decided, the Intention for the data as composed) and the models | Writes the Write model **and** the Read model, by the same code, in the same transaction |
| Is deterministic: a function of the Mutation and of the state of the models at its place in the Log | Never reads the clock, a random number, a configuration |
| Never decides again: no pricing, rounding, search, numbering | Everything that is deduced is in the Decision |
| Navigates by identities (`ref.Resolve()`, `ReadXRef.Of(ref).Resolve()`) and links | Queries no collection and no index: what a search finds is decided in `Decide`; the Replay stays fast (the goal: 100,000 Mutations/s and more) |
| Creates entities with the identities of the Mutation or `XRef.Of`, and Records for the properties and the bags of its models | `XRef.New()`, `Add()` of a Record, `Submit()`: refused by the `ReplayContext` (`InvalidOperationException`) |

A missing target in `Apply` is a bug of the Log: `Resolve()`, which throws.

## Step 6: sub-Mutations

`Decide` composes other Mutations and submits them at once, in the transaction of its mother, before the mother is added: each child is decided, added and applied at the instant of its `Submit()`.

```
mother.Decide()
    child.Submit() → child.Decide() → child.Add() → child.Apply()
mother.Add() → mother.Apply()
Log: child, mother     Live applies: child, mother     Replay: child, mother
```

```csharp
public partial class QuoteDuplication
{
    private partial void Decide()
    {
        if (!Source.ResolveOrNone().TryGet(out var source))
            throw new QuoteRefused(Refusal.NotFound);
        new QuoteCreation { Quote = Copy, Company = source.Company.Get().Ref, Description = source.Description, At = At, User = User }.Submit();
        foreach (var line in source.OrderedLines)
            new LineCopy { Quote = Copy, Line = QuoteLineRef.New(), From = line.Ref, At = At, User = User }.Submit();
    }
}
```

- Children apply before their mother: an effect of the mother that the children need is itself a child, submitted first.
- Sub-Mutations are born in `Decide`, never in `Apply`.
- A sub-Mutation is never posted; when the pump runs a mother again, it decides again and composes new children.

## Step 7: Jobs, the side effects

A Mutation that must act outside the system (mail, PDF, payment, call) creates a **Job** in `Decide`: a Record (not a Mutation: its `Add()` is public) of an **output Log** chained after the Mutation Log (`log.Chain(jobs)` after `AddChildStore`). It exists only if the Mutation is committed (the outbox), is never recreated at a Replay (`Decide` does not run), and is consumed after the commit by a subscribed service. Its identity, drawn in `Decide` and kept in the Decision, makes the service idempotent. A result returns to the domain as a new Mutation posted by the service.

```csharp
public partial class QuoteGeneration
{
    private partial void Decide()
    {
        if (!Quote.ResolveOrNone().TryGet(out var quote) || quote.Status != QuoteStatus.Draft)
            throw new QuoteRefused(Refusal.InvalidState);
        FinalNumber = quote.Company.Get().NextFinalNumber();
        Rendering = Guid.NewGuid();                                        // the identity of the Job, in the Decision
        new PdfRendering { Job = Rendering, Quote = Quote, Number = FinalNumber }.Add();
    }
}
```

## Step 8: the pump and the application

```csharp
var quote = QuoteRef.New();                                    // any thread, outside any transaction
var creation = new QuoteCreation { Quote = quote, Company = company, Description = "Heat pump", At = clock.Now, User = user };
pump.Post(creation);
pump.Post(new LineAddition { Quote = quote, Line = QuoteLineRef.New(), Product = product, Quantity = 2, LineNo = 1, At = clock.Now, User = user });

if (pump.Wait(creation) == MutationStatus.Failed)              // wait only when what follows depends on it
{
    if (creation.Failure.Get() is QuoteRefused refused)
        Report(refused.Reason);                                // a refusal of the domain
    else
        ExceptionDispatchInfo.Throw(creation.Failure.Get());   // a bug: thrown again with its stack
}
```

| `GhostMutationPump` | |
|---|---|
| `new GhostMutationPump(masterRepository)` | One queue, one thread; it opens the transactions of the chain of the master |
| `Post(mutation)` | From any thread; `Composed` → `Posted`, frozen; FIFO: the Mutations of a thread run in the order posted |
| `Wait(mutation)` | Blocks until processed; returns `Accepted` or `Failed` |
| `mutation.Status`, `IsProcessed`, `Failure` (`Option<Exception>`) | Never stored |
| `MaxBatchTime` (1 ms), `MaxBatchCount` (256) | A batch is one transaction, one commit for many Mutations; it commits between two Mutations once its time is spent; a batch has no business meaning |
| `Exclusive = true` | Each batch takes the write token at its opening: no conflict ever; for a back end of one server, uniqueness checked in `Decide` |
| `Exclusive = false` (default) | Concurrent and distributed: several writers of the cluster; unique indexes guarantee uniqueness; a conflict runs the batch again, then one Mutation per transaction; a Mutation in conflict `MaxConflicts` times in a row (3) fails by it |
| `Dispose()` | Fails the Mutations still queued (`ObjectDisposedException`) |

The application **never opens a write transaction** and never calls `Submit()` (it is `internal`): it reads the Read model in ReadOnly transactions on any thread (they never wait for the pump), composes, posts, and waits when it must. A line can be posted without waiting for the creation of its quote: the queue keeps the order; if the creation fails, the line fails too.

## Step 9: the Replay

The generator writes, on the subscription of a Log of Mutations, `HandleMutations()`: each Mutation by its `Apply`, under a `ReplayContext`. At the opening, before the pump starts:

```csharp
static void Replay(WriteModelRepository shop, ShopMutationLog log)
{
    var replayer = new Replayer(shop, group: 10_000);              // see logs-chains.md
    log.SubscribeFrom(0)
        .OnTransactionStart(replayer, static (_, r) => r.Open())    // a Replay transaction: replay: true
        .HandleMutations()
        .OnTransactionEnd(replayer, static (id, r) => r.End(id))    // Commit(through) every group
        .OnReplayEnd(replayer, static (last, r) => r.Finish(last))  // commit through the last, then Cancel
        .Run();
}
```

Only `Apply` runs: neither `Decide` nor the Jobs; sub-Mutations are in the Log at their place. The same order as live gives the same state. **The Replay is the migration**: a new model, a new Read model, a fixed `Apply`, rebuilt from the decisions that were taken, even when `Decide` has changed since.

## Step 10: uniqueness, numbering, concurrency

| Need | Exclusive pump (one server) | Concurrent pump (several writers) |
|---|---|---|
| A value unique among entities | Checked in `Decide` by a query over the Views (or one index for a large set) | A unique index; the commit refuses a value taken meanwhile |
| A number without gaps (invoice, draft) | A counter on a Root, incremented in `Decide`, written in the Decision | The same counter, a unique index on the number as the guard, the batch replayed on conflict |
| An identity of a new entity | In the Intention, drawn by the application | The same |

Only serialized Mutations write the models: no other writer, no transaction of the application writes them, the tests included. With a single writer, a counter needs no `[Mergeable]` and an aggregate needs no `[ConcurrencyPoint]`; those tools serve the concurrent execution.

## Step 11: testing

- The tests of the domain **execute Mutations**: compose, post, wait, then read the models in a ReadOnly transaction. Never write the models directly in a test: it would test a state the Log can never produce.
- A refusal: `Assert.Equal(MutationStatus.Failed, pump.Wait(m)); Assert.IsType<QuoteRefused>(m.Failure.Get());`, and the models unchanged.
- **Replay equivalence**: run a scenario, snapshot both models by identity, close, reopen (in-memory models behind a file Log, the same Store names), replay, snapshot again, compare.
- A batch replayed (a failure that condemned it, a conflict) runs `Decide` again: test that a `Decide` has no effect outside the transaction.
- In the domain assembly, a test chain holds the pump (`Submit()` is internal); `InternalsVisibleTo` gives the tests the generated types of an internal model.

## Step 12: evolution

- The Mutation Log is **Fixed**, forever: never renumber a property; a new field reads its default in old Mutations; a changed meaning is a new Mutation type.
- The Write and Read models are rebuilt at each start: they may be **Deduced**, and change freely, provided each `[WithIdentity]` type fixes its `[Entity(typeId)]`.
- Changing `Decide` changes only future decisions; changing `Apply` changes the rebuilt models: the Replay must stay faithful to the decisions recorded.

## Never and always

| Never | Always |
|---|---|
| Keep a command in Event Sourcing because of a limit of GBO | Encode what the command needs in the Mutation (spans of identities, structs, sub-Mutations); report the limit |
| A Mutation that only records an event decided elsewhere | The Mutation decides itself |
| `Apply` that recomputes a price, a number, a search | The Decision holds it |
| `Apply` that queries a collection or an index | Navigation by identities and links; the list decided in `Decide` |
| A file, a mail, a call in `Decide` or `Apply` | A Job, consumed after the commit |
| The application writes the models, or opens a write transaction | It posts Mutations |
| A handler registry, a command class plus an event class | One Mutation: its data, its `Decide`, its `Apply` |
| Commands spread at random over threads | One pump; or, for Event Sourcing, a routing by aggregate |
| A Read model in another assembly reading the Decision | The Read model in the assembly of the Mutations (the Decision is private), or a Log of published Records for outside consumers |

## Mutation Sourcing and Event Sourcing on GBO

| | Event Sourcing | Mutation Sourcing |
|---|---|---|
| The application composes | A .NET command, ephemeral | A Mutation, its Intention |
| Who executes | A handler, called by the application, in its transaction | The pump, one thread, Mutations posted from any thread |
| The Log holds | Events: facts | Mutations: intentions and their decisions |
| Domain code | Handler classes, a projection per event | `Decide` and `Apply` in the Mutation |
| Copies | Command → Event → models | None |
| A request with many effects | One event per effect | One Mutation and its sub-Mutations |
| A refusal | A result, no event | An exception of the domain, given to the poster; nothing added |
| Conflicts | Between concurrent handlers (`TryCommit`, retry) | None with an exclusive pump |
| The Replay | The projections of the events | `Apply` of the Mutations, `HandleMutations()` |
