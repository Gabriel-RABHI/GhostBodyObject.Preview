# Concurrency in the model: aggregates, points, merges, claims, leases

Part of [the modeling knowledge](README.md). GBO has no lock manager: readers never wait, transactions do not block each other while they work, and their commits take their turn for a short moment. The model decides what conflicts with what. **An invariant contained in a domain of concurrency is coded in C#; an invariant across domains needs a tool of the engine.**

## The conflict levels of a transaction

| Level | What the commit checks |
|---|---|
| `Optimistic` (default) | An entity written that another transaction changed or removed since this one opened; an insertion that conflicts; unless a merge resolves it |
| `Pessimistic` | As `Optimistic`, and a change since the opening of any entity of a type that the transaction consulted |
| `Exclusive` | Nothing: it takes the write token of its chain at its opening, on every server sharing the Store, and keeps it until it closes; other commits wait, readers do not; `cancellationToken` bounds its wait |

There is no `Override` level (removed): the last writer never silently wins. `TransactionConflictException.Kind` (or `TryCommit(out kind)`) says what conflicted: `Change`, `Removal`, `Insertion`, `Collection`, `Identity`, `Ancestry`, `Uniqueness`, `Bound`, `Claim`, `Lock`, `LeaseLost`, `Domain`. GBO never retries: the application runs the whole unit of work again in a new transaction ([../transactions-queries.md](../transactions-queries.md)).

## What conflicts by default

| Two concurrent transactions | Conflict? |
|---|---|
| Change the same entity (values or links) | Yes |
| Change two different entities | No |
| Add, remove or move parts of the same Owner (its structure) | Yes: the collection is in the Owner |
| Change the values of two parts of the same Owner | **No**: each writes its part only |
| Change a value of a part, and a value of its Owner | No |
| Give or take a component of the same entity | Yes |
| Change a component and its entity's values | No: a component is an aggregate of its own |
| Change an edge and one of its ends | No |
| Create the edge of the same pair | Yes |
| Add a strong reference to an entity another removes | Yes |
| Give the same value of a unique index to two entities | Yes, `Uniqueness` |
| Give the same value of a non-unique index | No |
| Append Records to the same Log | Never |

## Size the aggregate by the rule it protects

- **An order and its lines** are one aggregate: the total and the limit of lines are checked together; two transactions cannot both add the hundredth line.
- **A customer and its orders** are not: no rule checks them together, and two orders placed at once by one customer would conflict. Make orders Roots that designate their customer: `[Strong] MCustomer Customer`.
- A rule of a method of the Owner that reads its parts is safe against concurrent changes of the **structure** (additions, removals), not against concurrent changes of the **values** of the parts.

## `[ConcurrencyPoint]`: a domain that conflicts as one

```csharp
[Entity(1, 1, 2), Root, Removable, ConcurrencyPoint]
internal interface MRoom
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] IEntityCollection<MReservation> Reservations { get; }
}
```

Every change of an entity of its domain (a value, a link, a birth, a removal, a move of a part below it, at any depth, down to the next point) marks the nearest point above it, once per transaction. Two transactions that change one domain conflict (`Domain` when the point itself was not changed, `Change` when it was), even on different parts. Now a rule over the parts is right under concurrency: no two overlapping reservations of a room.

- The point carries `ConcurrencyVersion`, a hidden counter read only: the ETag of the aggregate, for an optimistic check up to the client (`If-Match`).
- A point high in the tree serializes its whole domain: a `Customer` point serializes all its orders. Nest points (`Customer` and `Order`) so that lines of two orders of one customer do not conflict.
- A model without points pays nothing. A point is never a Record, a component, an edge, nor a `[Mergeable]` type (GBO039).
- A move to another Owner marks both domains; writes of mergeable fields only do not mark.

## `[Mergeable]`: concurrent changes that merge

For counters and statistics shared by many writers, the conflict is replaced by a three-point merge (the value at the opening, the head, the new value):

```csharp
[Entity(1, 1, 7), Root, Removable, Mergeable(RemovalWins = true)]
internal interface MDevice
{
    [Property(1)] GhostString DeviceId { get; set; }
    [Property(2), Delta] long Count { get; set; }        // head + (new − epoch)
    [Property(3), Delta] double Sum { get; set; }
    [Property(4), Min] double Min { get; set; }          // head if untouched, otherwise min(head, new)
    [Property(5), Max] double Max { get; set; }
    [Property(6), Max] DateTime MessageTime { get; set; }
    [Property(7)] DeviceStatus Status { get; set; }       // not mergeable: the three-point rule, then the Merger
}
```

| Attribute | Types | Value written at the commit |
|---|---|---|
| `[Delta]` | Integers, `Int128`, `UInt128`, `Half`, `float`, `double`, `decimal`, `TimeSpan`, and their `Option` | `head + (new − epoch)` |
| `[Delta(Min = 0)]`, `Max = n` | The same; bounds as `long` or `double` constants | The merged value must stay in its bounds: the Setter refuses a value out of them (the transaction intact), the commit refuses a merged one (`Bound`) |
| `[Max]`, `[Min]` | Numbers, dates, times, `TimeSpan`, and their `Option` | The head if the transaction did not change it, otherwise the max (min) of the head and the new value |
| `[Delta]`, `[Max]`, `[Min]` on a value object | A stored struct or a converted type (not to a `string`), and its `Option`, that declares `IAdditionOperators<T, T, T>` and `ISubtractionOperators<T, T, T>` (`[Delta]`, no bounds) or `IComparable<T>` (`[Max]`, `[Min]`) | The same rules, computed by the operators of the type: `[Delta] Money Balance` |

- **Other values** follow the three-point rule: unchanged by the transaction, the head; unchanged by the head, the new; changed both sides to the same value, that value; changed both sides to different values, the **Merger** decides, or it is a conflict.
- **The Merger**, in the partial class of the Body, runs under the write token, reads only its three versions, and returns `false` to fail the commit:

```csharp
public partial class Device
{
    private bool Merge(DeviceView epoch, DeviceView head)
    {
        if (head.MessageTime > MessageTime)
            Status = head.Status;        // the most recent message wins
        return true;
    }
}
```

- **Links never merge**: a reference, a collection, an Owner changed by both sides is a conflict.
- **`RemovalWins = true`** (off by default): a removal wins over a concurrent write of values, in both commit orders; the write is dropped, the rest of its transaction committed. A careful design leaves it off.
- **A mergeable field carries no invariant but its bounds**: two withdrawals valid on their snapshots can merge below zero; a balance that must never go negative is `[Delta(Min = 0)]`; an invariant that is not a bound stays on an ordinary field.
- An exact sum needs `decimal` or an integer: the sum of `double` depends on the commit order.

**The escrow pattern**: places remaining, stock, quota, capacity:

```csharp
[Entity(2, 1, 2), Mergeable]
internal interface MCategory
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2), Delta(Min = 0)] int Remaining { get; set; }   // 500 at the start; a booking of n takes n
}

if (category.Remaining >= n)       // check, then write: the Setter would refuse below 0
    category.Remaining -= n;       // concurrent bookings merge; the one that would go below 0 fails its commit, Bound
```

A capacity that varies per entity is counted by what remains, `Min = 0`; never a bound read from another field.

## `Claim()`: a decision taken on a read

`Optimistic` does not check reads. A decision taken on something read, that writes elsewhere, is a write skew:

```csharp
ward.Claim();                                     // T1 and T2 both claim the ward they decide on
if (DoctorWardIndex.Is(ward).Count(static d => d.OnCall) >= 2)
    alice.OnCall = false;                         // T2: bob.OnCall = false
```

`Claim()` (on a Body or a View, in a ReadWrite transaction) makes the commit conflict (`Claim`) if the claimed entity changed since the opening, and two transactions that claim it conflict: `SELECT ... FOR UPDATE` without waiting. It costs one write: claim what the decision depends on, not everything read. `[ConcurrencyPoint]` does the same for an aggregate without calling anything; `Claim()` serves when the entities of an invariant are not in one aggregate.

## Leases: a lock across transactions

For external work between two transactions (a payment, a firmware upload), or a long task that short transactions would always beat:

```csharp
GhostLease lease;
using (var tx = devices.OpenTransaction(StoreAccessMode.ReadWrite))
{
    var device = DeviceIdIndex.Find(serial).Get();
    if (!device.TryLock(TimeSpan.FromMinutes(5), out lease))
        return;                                    // held by another context: refused at once, no queue, no deadlock
    tx.Commit();                                   // the lock is taken by this commit, cluster-wide
}

await firmware.SendAsync(serial, image, idempotencyKey: lease.Id);   // outside any transaction

using (var tx = devices.OpenTransaction(lease))
{
    DeviceIdIndex.Find(serial).Get().ToBody().Firmware = image.Version;
    lease.Release();                               // released by this commit; without it, the lease goes on
    tx.Commit();                                   // LeaseLost if the lease expired or was lost: nothing written
}
```

- `txn.NewLease(duration)` and `lease.TryAdd(view)` lock several entities, all or none; `lease.Renew(duration)`, `lease.Remaining`, `lease.IsActive`, `lease.Views<TView>()`; `view.IsLocked` to check before writing.
- A write of another context on a locked entity is refused at the instruction (`GhostLockedException`); a lock taken meanwhile fails a commit (`Lock`). A lock retains its entity against removals. The lock of a concurrency point covers its domain.
- Durations on the monotonic clock of each node, never a wall clock compared: a lease ends at its local deadline or at the ejection of its holder. Use `lease.Id` as the idempotency key of the external work.
- An Exclusive transaction may pass `ignoreLocks: true` to write locked entities; the lease holder then fails at its commit.

## Exclusive transactions

For work that must succeed without being replayed: a bulk load, a migration, a maintenance, a long batch. It never conflicts; its price is that no server of the cluster commits while it is open: keep it short. It is also the mode of a single-writer Mutation pump (`Exclusive = true`, see [../mutation-sourcing.md](../mutation-sourcing.md)).

## Parallel commands

Commands spread at random over threads conflict on the most shared entities. Route them by aggregate: a discriminating `*Ref` (the order, the account) always processed by the same thread; or a single writer (the Mutation pump), which has no conflict at all on one server.

## Which tool for which invariant

| Invariant | Tool |
|---|---|
| Within one entity | Nothing: two writes of one entity conflict |
| The structure of an aggregate (count of lines, a slot full) | Nothing: the structure is in the Owner |
| The values of the parts of an aggregate (sum ≤ ceiling, no overlap) | `[ConcurrencyPoint]` on the Owner |
| A counter or a sum shared by many writers | `[Mergeable]` + `[Delta]` |
| A bounded counter (stock, seats, quota) | `[Delta(Min = 0)]` |
| A latest date, a maximum, a minimum | `[Max]`, `[Min]` |
| A decision on entities of several aggregates | `Claim()` on what the decision reads |
| A value unique among all entities | A unique index (cluster-wide), or a check in `Decide` with a single-writer pump |
| An external effect done once, while nobody else touches the entity | A lease |
| A long batch that must not be beaten | An Exclusive transaction |
