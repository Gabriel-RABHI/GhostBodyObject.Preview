# Transactions and queries

**Prerequisite:** the rules of [../SKILL.md](../SKILL.md); the model is declared ([modeling/README.md](modeling/README.md)).

## Open, work, commit

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("library", StoreAccessMode.ReadWrite);
using var library = new LibraryRepository(store);

using (var tx = library.OpenTransaction(StoreAccessMode.ReadWrite))
{
    new Book { Title = "Dune", Year = 1965, Price = 9.90m };       // created in the ambient transaction
    BookCollection.First(static b => b.Title == "Dune").Price = 12.50m;
    tx.Commit();                                                    // writes, then closes the transaction
}

using (library.OpenTransaction(StoreAccessMode.ReadOnly))            // reads what was committed before it opened
{
    decimal total = BookViewCollection.Sum(static b => b.Price);
}
```

| Member | Meaning |
|---|---|
| `repo.OpenTransaction(StoreAccessMode mode, TransactionConflictLevel level = Optimistic, bool replay = false, CancellationToken token = default, bool ignoreLocks = false)` | Opens the ambient transaction of the Repository in this execution context |
| `repo.OpenTransaction(GhostLease lease)` | A ReadWrite transaction of a lease ([modeling/concurrency.md](modeling/concurrency.md)) |
| `tx.Commit()` | Writes, closes; on failure nothing is written: `TransactionConflictException` (a conflict) or `InvalidOperationException` (condemned) |
| `tx.TryCommit(out TransactionConflictKind kind)` | The same, a conflict returned as `false` without exception, the transaction closed |
| `tx.Rollback()`, `Dispose()` | Closes without writing: `using` rolls back every path that does not commit |
| `tx.Status` | `Alive`, `Commiting`, `Closed`: a transaction is used once |
| `tx.CondemnedBy` | The exception that condemned it, or null |
| `tx.AccessMode`, `ConflictLevel`, `IsReplay`, `Epoch` | |
| `XRepositoryTransaction.Current`, `.Require()` | The ambient transaction of the Repository, or null; or throws |
| `tx.Enumerate<TBody>()`, `tx.EnumerateViews<TView>()` | Every entity of a type, `[Enumerable]` or not |

## The rules of a transaction

1. **Ambient**: the transaction of its execution context (an `AsyncLocal`): code at any depth finds it without receiving it; it follows `await`, even onto another thread. A `Task.Run` started inside it inherits it; one started from outside has its own context.
2. **One transaction of a Repository at a time in a context**: no nesting; a second `OpenTransaction` throws. Transactions of different Repositories may be open together (a chain opens them for you).
3. **Not shared by threads**: two threads writing in one transaction throw. Parallel work uses one transaction per thread.
4. **ReadOnly refuses every change**: a Setter, a birth, a `Remove()` throw; its `Commit()` just closes it.
5. **MVCC**: it reads the Store as it was at its opening (its epoch), plus its own changes, visible at once, even to a query being enumerated and to the indexes. To see later commits, open a new transaction.
6. **No Unit of Work**: nothing to add, attach, track or save. One entity is always one and the same Body in a transaction.
7. **Out of a transaction**, a collection, a birth, `ref.Resolve()` throw `InvalidOperationException` ("No XRepositoryTransaction is open in this context."). A Body that leaks out of its transaction must not be used to resolve links.

## Conflicts and retries

The first commit wins. The second finds what changed since its opening and writes nothing. GBO never retries: the application runs the whole unit of work again in a new transaction, which reads the current state. Write the unit of work as a method that reads and writes through the ambient transaction, and retry it:

```csharp
static bool Rename(CatalogRepository catalog, ArtistRef artist, string name)
{
    for (int attempt = 0; attempt < 3; attempt++)
    {
        using var tx = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
        if (!artist.ResolveOrNone().TryGet(out var body))
            return false;                                   // a business refusal: a result
        body.Name = name;
        if (tx.TryCommit(out _))
            return true;                                    // otherwise a conflict: the loop runs it again on the new state
    }
    return false;
}
```

- Prefer `TryCommit` for an expected conflict; `Commit()` throws `TransactionConflictException` (`Kind`, `TypeId`, `ConflictingTransactionId`, `Epoch`).
- Write the retried code with `GetOrNew`: the same code is right at the first try and at the retry.
- Reduce conflicts by the model (aggregates, components, `[Mergeable]`, see [modeling/concurrency.md](modeling/concurrency.md)), by routing commands by aggregate to fixed threads, or by a single writer (the Mutation pump).
- **Exclusive** (`TransactionConflictLevel.Exclusive`): takes the write token at the opening, never conflicts, makes every other commit of the cluster wait: for bulk loads and migrations; bound its wait with a `CancellationToken` (`OperationCanceledException`).

## Views and Bodies

| | Body (`Book`) | View (`BookView`) |
|---|---|---|
| Kind | `sealed partial class` | `readonly partial struct` |
| Kept by the transaction | Yes, one per entity | No, allocates nothing |
| Writes | Setters, `New`, `Remove()` | None |
| Reads | The current version | The current version, even after a change by the Body |
| Collections | `BookCollection` | `BookViewCollection` |
| Conversion | `body.ToView()` | `view.ToBody()` |

**Search with Views, change with Bodies.** A query over `BookCollection` in a ReadWrite transaction creates and keeps a Body per entity it meets: search with the Views, then take the Bodies of the few to change.

```csharp
foreach (var cheap in BookViewCollection.Where(10m, static (b, price) => b.Price < price).ToList())
    cheap.ToBody().Price += 1m;
```

Links, slots and reverse properties of a View give Views; edges and systems give Views.

## The operators

Every GBO sequence (entity collections, parts, references, reverse properties, edges, systems, index results, groups, the Records of a Log transaction) is a struct with a struct enumerator and the same operators, written by GBO:

| Group | Operators |
|---|---|
| Counts, tests | `Count()`, `Count(pred)`, `Any()`, `Any(pred)`, `All(pred)`, `Contains(item)` |
| Elements | `First`, `FirstOrNone`, `Last`, `LastOrNone`, `Single`, `SingleOrNone`, with or without predicate; `...OrNone` give an `Option<T>`; the others throw on none (`Single` on more than one; `SingleOrNone` too on more than one) |
| Deferred | `Where`, `Select`, `Skip`, `Take`, `Distinct`, `DistinctBy`, `Order`, `OrderDescending`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending` (stable), `GroupBy` (`Key`, `Items`, the operators) |
| Aggregates | `Sum`, `Min`, `MinOrNone`, `Max`, `MaxOrNone`, `MinBy`, `MinByOrNone`, `MaxBy`, `MaxByOrNone`, `Average`, `AverageOrNone`, `Aggregate(seed, ...)` |
| Conversions | `ToList()`, `ToArray()`, `ToHashSet()`, `ToDictionary(key)`, `ToDictionary(key, value)` |
| Leave GBO | `AsEnumerable()`: then any `System.Linq` operator, allocating |

**Static lambdas and states.** A lambda captures nothing (GBO013): the values it needs are passed before it, 1 to 4, and received after the item. Mark lambdas `static`: the compiler then refuses a capture. A state may be a `ref struct`, a text value object, a `GhostString`, a `ReadOnlySpan<char>`, for the operators that end a query (`Count`, `Any`, `All`, `First`, `Single`, `Last`, `Min`, `Max`, `Sum`, `Average`, `Aggregate`, their `OrNone`): `UserViewCollection.Count(name, static (u, n) => u.Name == n)`; the deferred ones (`Where`, `Select`, `OrderBy`) keep their states in the sequence they return, and take values only (REQUIREMENT-053). A `Select` or an `OrderBy` may return a text read in place, a text value object or a `ReadOnlyGhostString`: `BookViewCollection.OrderBy(static b => b.Title).Select(static b => b.Title)` reads each text when the enumeration reaches it and compares the keys in place, as `Select(static b => b.Title).Order()` does; its `ToList`, `ToArray`, `ToHashSet` copy them into strings. `DistinctBy`, `MinBy`, `MaxBy`, `ToDictionary` by a text take `b.Title.ToString()`.

```csharp
int before = 1950;
BookCollection.Count(before, static (book, year) => book.Year < year);
BookCollection.Count(1950, 1970, static (b, from, to) => b.Year >= from && b.Year < to);
BookCollection.Count(IsBargain);                                     // a static method group captures nothing
var others = BookCollection.Select(static b => BookCollection.Count(b.Author, static (o, a) => o.Author == a)).ToList();  // a query in a query
```

- `System.Linq` on a GBO sequence is GBO014; the query syntax of C# (`from ... select`) is accepted.
- A lambda is plain C#: it calls any method, any computed property, even runs another query (each inner query reads again: for N items, N queries; `GroupBy` groups once).
- A lambda may change the entities it reads, or create others: an entity created during an enumeration is enumerated too.
- `ref struct` values (`GhostString`, `GhostSpan<T>`) cannot be captured: compare in place (`b.Name == "Dune"`); a text projects or orders in place (`Select(static b => b.Name)`, `OrderBy(static b => b.Name)`), a `GhostSpan<T>` by `.ToArray()`, a text for a key `b.Name.ToString()`.

## Absence: `Option<T>`

`FirstOrNone`, `SingleOrNone`, `MinOrNone`..., `ref.ResolveOrNone()`, optional links and slots give an `Option<T>`: `Any()`, `TryGet(out x)` (the normal way), `GetOr(fallback)`, `Match(some, none)`, `Select`, `Where`, and `Get()` only where the absence is a bug.

```csharp
string title = BookViewCollection.FirstOrNone(static b => b.Year > 2000).Match(static b => b.Title.ToString(), static () => "none");
```

## Removing

`entity.Remove()` exists on a `[Removable]` Root and a `[Removable]` edge; a part is removed by its Owner (`owner.Parts.Remove(part)`, `owner.Slot.Remove()`), a component by its slot, a shared object by its last release. A removed entity leaves every collection and query of the transaction at once; its Body still reads its last values; writing it throws. A removal that a strong reference forbids throws before any change, naming the chain.

## Business code: partial classes and call points

Bodies, Views and systems are partial: add computed properties, methods and rules in your own files. GBO stores the properties of the model only. **Never a C# field or an auto-property in a partial Body** (`public int PriceChanges { get; private set; }`): it is state outside the model, never committed, replicated, seen by the Views nor rebuilt by a Replay, and it starts again at each transaction; avoid it at all costs, unless nothing else is possible ([modeling/values.md](modeling/values.md#computed-and-encapsulated-values)).

| Call point (partial method, generated when written) | Called |
|---|---|
| `partial void On{Property}Changed()` | After a **real** change of the value or link (the same value written again calls nothing); for a `GhostString` or `GhostSpan`, after a change of an element too |
| `partial void On{Collection}Added(T item)`, `On{Collection}Removed(T item)` | After an item enters or leaves a collection (`New`, `Add`, `Remove`, `MoveTo`) |
| `partial void OnDeleting()` | Once per removed entity, after the checks, before the writes, from Owners to parts: it reads them, may create others, never retains a removed one |
| `private bool Merge(TView epoch, TView head)` | On a `[Mergeable]` type, at the commit, under the write token |

Not built: `OnCreated`, `OnValidate`, `OnCommitting`, `OnCommitted`, `OnRolledBack`.

**Refused, or condemned.**

- **Refused**: every operation of GBO (a Setter, a birth, `New`, `Add`, `Remove`, `MoveTo`, a cascade) checks all its refusals before its first write. The exception changes nothing: the transaction goes on and commits. Write your rules the same way: **check, then write**, in a method of the partial class, and return a result rather than throw when the refusal is a business case.
- **Condemned**: an exception thrown after a write (by an `On...Changed`, an `On...Added`) leaves a half-done change: it condemns the transaction, even if your code catches it. The transaction still reads, refuses every write and its commit (`InvalidOperationException`, the cause as inner exception); only a rollback is left. In a chain, one condemned Store condemns the commit of the whole chain.
- So a rule that may refuse never lives in an `On...Changed`. Keep `On...Changed` for derived values (a total kept up to date) and side records.
- **Nothing outside the transaction before the commit**: a call point may be replayed after a conflict; it sends no mail and calls no service; it writes in the transaction only (an outbox: a Record of a chained Log, consumed after the commit).

## The exceptions of GBO

| Exception | When | The transaction |
|---|---|---|
| `InvalidOperationException` | A refusal (removal forbidden, slot full, removed target, ReadOnly, no transaction, nested transaction, chain behind its Log), `Get()` on none, `First()` on empty, a commit of a condemned transaction | Intact for a refusal before a write |
| `ArgumentNullException`, `ArgumentException` | A null required link, a null identity, an identity of another type | Intact |
| `GhostUniquenessException` (`GhostBodyObject.Runtime.Logical.Index`) | A value of a unique index taken; the previous value kept | Intact |
| `GhostLockedException` | A write of an entity locked by a lease of another context | Intact |
| `TransactionConflictException` (`GhostBodyObject.Runtime.Physical.Transactions`) | A commit rejected; `Kind` says why | Closed, nothing written |
| `OperationCanceledException` | An Exclusive transaction cancelled while it waited | Not opened |
| `ObjectDisposedException` | A disposed Repository, Log or Store | |

Never catch them to choose a business path: check first (`ResolveOrNone`, `index.Is(key).Any()`, `slot.Any()`, `TryGetOrNew`), and commit with `TryCommit`.

## Deep methods

A method at any depth queries, changes and creates entities without receiving a context; the code that opened the transaction keeps the decision to commit. Services are plain static methods or classes without a repository field:

```csharp
static void CloseTheYear(int year) { DiscountTheBooksBefore(1960, 0.10m); ReprintTheBooksBefore(1950, year); }

static void DiscountTheBooksBefore(int year, decimal rate)
{
    foreach (var book in BookCollection.Where(year, static (b, before) => b.Year < before))
        book.Price = Math.Round(book.Price * (1 - rate), 2);
}
```

## Several Repositories in one project

The generator writes `global using static Ns.XRepositoryCollections;` in the project of the model. With two Repositories that both have an `[Enumerable]` type of one name, each file chooses inside its namespace: `namespace Shop.Read { using static ReadModelRepositoryCollections; ... }`. Another project (tests, an application) writes the `using static` explicitly.

The birth of a part, `parts.New(...)`, is an extension method generated in the namespace of the model: code in another namespace writes `using ModelNamespace;`.
