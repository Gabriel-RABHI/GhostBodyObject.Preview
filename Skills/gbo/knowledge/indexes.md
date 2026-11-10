# Indexes: rarely, and for a reason

**Prerequisite:** [../SKILL.md](../SKILL.md). In GBO the data is in memory and a query over the Views reads tens of millions of entities per second without allocating. Most questions need **no index**. An index costs at every write of an indexed property, at the start of the Repository (built from the entities when the Store does not have it), and in a Replay; it is a projection of the entities, always rebuildable.

## Decide first: no index

Answer in this order; an index comes last.

| The question | Without an index |
|---|---|
| The entity a message, an API or a Record names | `[WithIdentity]` and `ref.Resolve()`: an identity index the engine always keeps |
| The lines of an order, the albums of an artist | The composition: `order.Lines` |
| The orders of a customer, the albums of a genre | A reverse property: `[ReverseStrongReference(nameof(MOrder.Customer))]` |
| The rating of a critic for a movie | The edge of the pair: `critic.RatingEdges.To(movie)` |
| The devices that have a battery and no maintenance | A system: it reads only the entities of its rarest required component |
| A report, a total, a count over most entities | A query over the Views: an index would read as much |
| A filter over a type of a few hundred thousand entities, not on a hot path | A query over the Views: milliseconds |
| In Mutation Sourcing, a check in `Decide` | A query over the Views, or a counter: `Decide` runs once per Mutation, `Apply` never queries |

**Add an index only when** a frequent question reads **a few entities among very many** and no path above reaches them, or for one of these needs that a scan cannot serve well:

| Need | Index |
|---|---|
| Words of texts, whatever case, accents and order; ranking; typos; autocomplete | **Text index** |
| A value that must be **unique** among all entities, cluster-wide, at the write and at the commit (email, tracking number, card) | **Unique value index**, or **unique hash index** |
| An entity by an **external key** (a serial, a SKU, a legacy `Guid`) on a hot path | **Hash index** (exact key), or a value index |
| A **range or an order** over a large set: the orders of a day, the 20 most recent, a price band, birthdays | **Value index** |

Measure before and after: keep an index for a measured gain on a real question.

## Text indexes

```csharp
[TextIndex(1)]                                  // its number, stable in the Repository (shared with the value and hash indexes)
internal interface MProductTextIndex { }

[TextIndex(2, KeepAccents = true, Separators = ",", MinLength = 2)]   // the rules of the index
internal interface MTagTextIndex { }

[Entity(1, 1, 3), Root, Removable, Enumerable]
internal interface MProduct
{
    [Property(1), TextIndexed(nameof(MProductTextIndex))] GhostString Name { get; set; }
    [Property(2), TextIndexed(nameof(MProductTextIndex), TextIndexing.Commit)] GhostString Description { get; set; }
    [Property(3), TextIndexed(nameof(MTagTextIndex))] GhostString Tags { get; set; }
}
```

- A `string` or `GhostString` property of an entity (not a Record), GBO032 otherwise. Several properties of several types may name one index (one search box for products and brands).
- `TextIndexing.Immediate` (default): terms cut by the Setter; `TextIndexing.Commit`: cut once, at the first search of the transaction or at the commit, for long texts built in steps. Both see the changes of the transaction.
- Rules: `MinLength` 2..4; accents folded unless `KeepAccents`; `Separators`: a term is a run of characters that are neither separators nor spaces (`fair-trade` stays one word).

| Generated (`ProductTextIndex`) | Result |
|---|---|
| `Search(query, TextSearch options = Default)` | The Views found (an index of one type) |
| `Search<TView>(query, ...)`, `Search<TBody>(query, ...)` | The Views or the Bodies of one type of the index |
| `Count(query, ...)` | Without reading them |
| `Terms(prefix)` | The terms of the prefix, each once, in order: autocomplete |

| Query | Finds |
|---|---|
| `coffee mug` | Both words (each word is required) |
| `tea +cups` | Those of `tea`, and those of `cups` |
| `coffee -tea` | Those of `coffee` without `tea` |
| `-mug -pot` | Every entity but those |
| `name:mug` | In the property `Name` only |
| `espr*`, `t?a` | Wildcards |
| `TextSearch.Fuzzy` | Within 1 edit, 2 from 6 letters |
| `TextSearch.Ranked` | The most relevant first (BM25); otherwise the order of the Store |

A search without a text index, `Description.Contains("coffee")` over the Views, reads every text and knows neither words, case nor accents.

## Value indexes

```csharp
[ValueIndex(nameof(MOrder), 3), ValueIndexComponent(1, nameof(MOrder.Status)), ValueIndexComponent(2, nameof(MOrder.Placed), DatePrecision.Day)]
internal interface MOrderStatusDayIndex { }

[ValueIndex(nameof(MOrder), 4)]
[ValueIndexComponent(1, nameof(MOrder.Total), Decimals = 0)]       // required for a decimal
internal interface MOrderTotalIndex { }

[ValueIndex(nameof(MCustomer), 9, Unique = true), ValueIndexComponent(1, nameof(MCustomer.Email), IgnoreCase = true)]
internal interface MCustomerEmailIndex { }

[ValueIndex(nameof(MCustomer), 6), ValueIndexComponent(1, nameof(MCustomer.Birthday), DatePart.MonthOfYear), ValueIndexComponent(2, nameof(MCustomer.Birthday), DatePart.DayOfMonth)]
internal interface MCustomerBirthdayIndex { }
```

- Up to 8 components, positions 1, 2, 3..., keys of 64 bytes at most; each a property of the indexed type: a value, an enum, a date, a string, a reference (`Is(body)` or `Is(view)`), an `Option<T>`, a converted type (by its stored value).
- Options: `DatePrecision.Tick..Year`, `DatePart.MonthOfYear | DayOfMonth | DayOfWeek | HourOfDay | MinuteOfHour` (a cycle), `Step = n` (integers), `Decimals = n` (floats, decimals; required for a `decimal`, a converter to `decimal` included), `IgnoreCase`, `IgnoreAccents` (strings), `RawTicks` (a `DateTime` as its ticks; by default a local time is converted to UTC). **A rounded index stays exact**: the bounds compare the true values of their bucket.
- **A string gives equality only** (hashed): no range, no prefix; a prefix is a text index (`Terms`). No range either on a reference, a `bool`, a `Guid`, a `[Flags]` enum, or a converter without `PreservesOrder`.

| Condition (`OrderPlacedIndex`) | Values |
|---|---|
| `Is(v)` | Equal; on a composite, fixes the component and gives the conditions of the next one (a stage `WithX`) |
| `From(v)`, `Before(v)` | `>= v`, `< v` |
| `Between(a, b)`, `Range(a, b)` | `[a, b)`, `[a, b]` |
| `Every` | Every entity, in key order |
| `IsNone()` | The absent values of an `Option` or an optional reference; a range never holds them |
| `Has(f)`, `One(fs)`, `All(fs)`, `None(fs)` | On a `[Flags]` enum: one flag, at least one, all, none |

| Result | Members |
|---|---|
| `ValueIndexResult<TView, TBody, TTag>` | A GBO sequence of Views in key order: the operators, `Count()`, `Descending()`, `Bodies()`, `And`, `Or`, `AndNot` |
| A stage `WithX` (after `Is` on a composite) | `Count()`, `Descending()`, `Bodies()`, `And`/`Or`/`AndNot`, the next conditions, enumeration; the other operators on `.Result`: `stage.Result.Any()`, `stage.Result.FirstOrNone()` |
| `IndexSetResult` (a combination, `One`/`All`/`None`) | A set without order: the operators, `Count()`, `Bodies()`, `And`, `Or`, `AndNot` |

- **The components are read in order, never skipped**: a condition on the second needs `Is` on the first. Put first what every question fixes (the status), then what it ranges over (the day). For conditions on either, declare two indexes and combine them.
- `Descending()` reads backward: the most recent first, found at once wherever it is: `OrderPlacedIndex.Every.Descending().Take(20)`.

## Hash indexes

```csharp
[HashIndex(nameof(MDevice), 5, Unique = true), HashIndexComponent(1, nameof(MDevice.Serial), IgnoreCase = true)]
internal interface MDeviceSerialIndex { }
```

An exact key, every component given, no order nor range; components: a standard value, a `string`, a `GhostString`, a reference, an `Option<T>`, a `GhostSpan<T>` (a binary key, by its bytes). Generated: `Find(key)` (unique: an `Option<TView>`), `Exists(key)`, `Count(key)`, `Is(key)` (a result: `Count()`, `Exists()`, `FirstOrNone()`, `Bodies()`, `And`/`Or`/`AndNot`). `Exists` checks a value before it is given.

## Unique indexes

`Unique = true` on a value or hash index: a `UNIQUE` constraint, cluster-wide.

- **The Setter** refuses a value another entity has, in the Store or in the transaction: `GhostUniquenessException`, the previous value kept, the transaction intact.
- **The commit** refuses a value taken meanwhile by a concurrent transaction, on any server: `TransactionConflictException`, `Kind == Uniqueness`; the retry sees the value taken.
- **An absent value is never refused** (an `Option<T>` empty), as SQL `NULL`.
- **Births are checked at the commit**: two entities born with the default value (`""` for a string) collide. Give the value in the initializer of the birth, or make the property an `Option<T>`.
- A value freed by a change or a removal can be taken at the next instruction; swapping two values passes through an intermediate one.
- Check before the write, never catch the exception to decide: `if (CustomerEmailIndex.Is(email).Any()) return false;` (on its last component, `Is` gives the result itself; on a hash index, `Exists(key)`).
- In Mutation Sourcing with an exclusive pump (a single writer), uniqueness can be checked in `Decide` by a query over the Views; a unique index is the guarantee with several writers.

## Combinations

`a.And(b)`, `a.Or(b)`, `a.AndNot(b)` between results of the same entity type, value or text, each index read once, never an entity none of them gives; the result is a set without order, which combines again: `OrderStatusDayIndex.Is(OrderStatus.Paid).And(OrderTextIndex.Search("door")).AndNot(OrderOptionsIndex.Has(OrderOptions.Gift))`. A composite index answers its prefixes in the order of its next component; combine separate indexes for conditions on any of them.

## Collections over an index

A member of a partial Body or View may return a search: a collection computed at each read, in index order, always up to date, never stored, never filled by code.

```csharp
[ValueIndex(nameof(MOrder), 12), ValueIndexComponent(1, nameof(MOrder.Customer)), ValueIndexComponent(2, nameof(MOrder.Placed))]
internal interface MOrderCustomerIndex { }

internal partial class Customer
{
    public OrderCustomerIndex.WithCustomer Orders => OrderCustomerIndex.Is(this);
    public Option<OrderView> LastOrder => Orders.Descending().FirstOrNone();
}

internal static class Backlog
{
    public static ValueIndexResult<OrderView, Order, ShopRepositoryTag> Late(DateOnly day) => OrderStatusDayIndex.Is(OrderStatus.Paid).Before(day);
}
```

Compare with a reverse property: a reverse property is a set (no order), kept by the engine for a reference; a collection over an index is ordered by a value, for ranges and top-N.

## Indexes and transactions

- Up to date at the next instruction in the transaction that changes the entities, rolled back with it, seen by others at its commit; kept over a restart.
- Concurrent transactions giving the same non-unique value never conflict by the index.
- A search out of a transaction throws.
- An index is built at the start of a Repository whose Store lacks it, rebuilt when its declaration changes, removed when no longer declared: a new index on a large Store lengthens the start.
- In Mutation Sourcing, `Apply` never queries an index: it navigates by identities and links, so the Replay stays fast. Each index slows every `Apply` that writes an indexed property; keep only the indexes a `Decide` or a reader truly needs.
