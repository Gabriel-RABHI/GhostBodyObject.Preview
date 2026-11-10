# Reference: pitfalls, traps for agents, and the review checklist

Part of [gbo](../../SKILL.md). Each line was observed in the tutorials, the tests or the ports built on GBO.

## Traps: what an agent tends to write, and what GBO wants

| Written by habit | In GBO |
|---|---|
| `context.Orders.Add(order); context.SaveChanges();` | `new Order(...) { ... };` then `tx.Commit();` |
| `await tx.CommitAsync()` | `tx.Commit()` or `tx.TryCommit(out kind)`: synchronous |
| `public class Order { public Guid Id; public Guid CustomerId; }` | `[Entity(...)] [Root] [WithIdentity] interface MOrder { [Property(n)] [Strong] MCustomer Customer { get; set; } }` |
| `orders.Where(o => o.Total > min)` | `OrderViewCollection.Where(min, static (o, m) => o.Total > m)` |
| `using System.Linq; collection.Select(...).ToList()` on GBO | The GBO operators; `.AsEnumerable()` only to leave GBO on purpose |
| `FirstOrDefault()`, `?? `, `!= null` | `FirstOrNone()`, `GetOr(...)`, `TryGet(out x)` |
| `int? Card` | `Option<int> Card` |
| `string? Note` for an absent text | `GhostString Note`, empty when absent |
| `List<OrderLine> Lines` | `IEntityCollection<MOrderLine> Lines { get; }` |
| `Order.Lines[0]`, an index into a collection | A `Position` property and `OrderBy`; a collection is a set |
| `class Customer : Party` | Components on `MParty`; systems to query them |
| `[ValueObject]`, `[Owned]` | A stored struct (the first choice), a converter, an enum, or a required part |
| `decimal Price`, `string Email`, `bool IsActive`, `double Weight` | Value objects: `Money Price`, `Email Email` (converter), `Activity Activity` (enum), `Weight Weight` |
| `public int PriceChanges { get; private set; }` in a partial Body | Never: a stored property maintained by `On...Changed`, a Record, a local variable, or a property with a body |
| `try { tx.Commit(); } catch (TransactionConflictException) { retry }` | `if (!tx.TryCommit(out _))` in a retry loop |
| `try { customer.Email = email; } catch (GhostUniquenessException) { ... }` | `if (CustomerEmailIndex.Is(email).Any()) return false;` then write |
| A validation that throws in `OnEmailChanged()` | Check in a method before the write; `On...Changed` only for derived values |
| A Repository injected into a service, passed to methods | Nothing to pass: the transaction is ambient |
| `Task.WhenAll(items.Select(i => Task.Run(() => Change(i))))` inside one transaction | One transaction per thread; or one writer |
| `DateTime.Now` in an event handler, an `Apply`, a `Decide` | The time comes with the command or the Intention (`At`) |
| `Guid.NewGuid()` as the identity of an entity | `XRef.New()`, outside any Replay |
| `Unsafe.BitCast<Guid, ProductRef>(...)`, `Guid ProductId` in a Record | `ProductRef Product`, `GhostSpan<ProductRef>` |
| `client.Home.ToBody().City = "Lyon";` alone | `var home = client.Home.ToBody(); home.City = "Lyon"; client.Home = home;`: a Record property is a value, its copy changes nothing until assigned |
| Order lines, or any part changed one by one, in a `RecordBag` | Parts: a Record has no identity, no link, no conflict of its own |
| An index on every searched property | Navigation, reverse properties, Views first ([../indexes.md](../indexes.md)) |

## Pitfalls of the API

1. **Policies before the first Repository**: `FlushPolicy`, `LockPolicy`, `RebuildFromLog()` are set on the master Store before `new XRepository(store)`, whose constructor opens a transaction.
2. **In-memory Stores are shared by name** in the process: unique names per test; but a chain reopened behind its persistent Log keeps the names of its volatile Stores.
3. **A chained Repository has no transaction of its own**: read the Read model through a ReadOnly transaction of the master.
4. **A C# field or auto-property in a partial Body is an anti-pattern**, to avoid at all costs unless nothing else is possible: never stored, committed, replicated, seen by the Views or by other transactions, nor rebuilt by a Replay; it starts again with each transaction (one Body per entity per transaction), and a rollback does not undo it. A stored property maintained by `On...Changed`, a Record, a local variable, or a property with a body instead. The generator warns of it, GBO047; `#pragma warning disable GBO047` around the field where nothing else is possible.
5. **A unique index and births**: births are checked at the commit; two entities born with `""` collide. Give the value at the birth, or use an `Option<T>`.
6. **A stage `WithX` of a composite index** has `Count`, `Descending`, `Bodies`, `And`/`Or`/`AndNot`: use `.Result` for `Any`, `First`, `Select`...
7. **A string component of a value index** gives equality only; a prefix or a range comes from a text index.
8. **Changing a collection while enumerating it** throws at the next step: enumerate a `ToList()`, or `while (c.FirstOrNone().TryGet(out var x)) c.Remove(x);`.
9. **`ref struct` values** (`GhostString`, `GhostSpan<T>`, their read-only forms) cannot be captured, stored in a field, used as a type argument, put in an `Option` or a tuple, nor held across an `await`: `.ToString()`, `.ToArray()`, or assign them in separate statements.
10. **`string` properties allocate at each read**; `GhostString` does not.
11. **`decimal` equality is by bytes in the Setter**: `1.00m` over `1.0m` is a change.
12. **An enum whose members start at 1** reads 0 at a birth until set.
13. **`ToView()` of a Record** is refused while it is detached, and after its transaction closes: write it complete, `Add()` it, then take its View.
14. **A live subscription after a Replay** would apply the next commands twice: return `Cancel` from `OnReplayEnd`.
15. **A Replay draws nothing**: `XRef.New()` throws in it; every identity is in the Records.
16. **A model rebuilt from its Log** loses every change made without a Record.
17. **`using static` per project**: another project, or two Repositories with types of one name, write `using static Ns.XRepositoryCollections;` inside their namespace.
18. **The order of instructions matters**: each is checked at its instruction, not at the commit: remove the album before its artist; take the new strong reference to a shared object before releasing the old.
19. **A removed Body still reads**, and refuses writes: do not keep using it.
20. **A strong reverse property costs a write per new reference**: declare only those the code reads.
21. **`Pessimistic` conflicts on whole types read**: it is coarse; prefer `[ConcurrencyPoint]` or `Claim()`.
22. **An Exclusive transaction blocks every commit of the cluster**: keep it short.
23. **A tutorial page may lag behind its test project**; the test project and the code are the truth.
24. **Temporary folders of file Stores** are deleted after `GC.Collect()`, with retries: the Segments are unmapped by the GC.
25. **`parts.New(...)` is an extension method** (`XCompositions.New(this PartCollection<X, Tag>, ...)`) written in the namespace of the model: code in another namespace that creates parts writes `using ModelNamespace;`, otherwise `CS1061: PartCollection<...> does not contain a definition for New`.
26. **A tuple assignment with a `GhostString` or `GhostSpan` member does not compile** (`CS9244`, a ref struct cannot be a tuple element): `(album.Title, album.Year) = ("Low", 1977)` fails when `Title` is a `GhostString`; write one statement per property.
27. **A `*Ref` in a Record is recognized by the name of its model**: `CustomerRef` means the interface `MCustomer` that has `[Entity]` and `[WithIdentity]`. When several namespaces of the compilation declare such an `MCustomer`, the Record must be declared in the namespace of the one it means, otherwise GBO002 (or GBO009 for a `GhostSpan<CustomerRef>`). A `*Ref` of another assembly is recognized by its `[GhostIdentity]` attribute.
28. **A converted property indexed by a value index** takes the options of its stored type: a converter to `decimal` requires `Decimals` on its component (GBO036).
29. **A namespace of the model named like a generated type** breaks the generated code: a module `Quotes.Model.Pricing` beside a component `CPricing` (its Body `Pricing`) gives `CS0118: 'Pricing' is a namespace but is used like a type`. Name the modules of concerns in the plural, or by another word.

## The review checklists

The review checklists belong to the roles, shared by the one who writes and the one who reviews: the technical base in [gbo-boilerplate/checklist.md](../../../gbo-boilerplate/checklist.md), the modules in [gbo-module-architect/checklist.md](../../../gbo-module-architect/checklist.md), the model in [../modeling/checklist.md](../modeling/checklist.md) (sections G and J), the code in [gbo-implementer/checklist.md](../../../gbo-implementer/checklist.md) (the big picture and the detail).
