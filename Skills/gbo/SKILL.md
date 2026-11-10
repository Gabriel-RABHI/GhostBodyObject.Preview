---
name: gbo
description: Entry point of GBO (GhostBodyObject), the embedded transactional, persistent and distributed object store for .NET 10 whose model is C# interfaces compiled by a source generator. Read it first for any task on an application that keeps, or will keep, its data with GBO, or on code with [Entity], [Record], [GhostRepository], [GhostLog] or a GBO001-GBO048 diagnostic. It finds the path of the mission in a decision tree (build an application, adopt GBO, add a module, add or change a use case, fix a symptom, review) and routes to the role skills gbo-boilerplate, gbo-module-architect, gbo-data-modeler, gbo-implementer, gbo-architecture-reviewer, gbo-model-reviewer and gbo-code-reviewer. It holds the deliverables and the ADR rule, the mental model, the rules that are never broken, what is built, and the index of the knowledge files: modeling, transactions and queries, indexes, Logs and chains, Mutation Sourcing, infrastructure, attributes, generated API, diagnostics, pitfalls.
---

# GBO: the entry point

GBO keeps the data of an application as C# objects in the memory of the process: no database server, no ORM, no mapping, no cache. You declare a model as interfaces; the generator writes the classes; every read and write happens in an **ambient transaction** (MVCC, ACID), on a **Store** in memory, on disk, or replicated over servers. The same code runs in all three.

This knowledge base has two layers. **The role skills** say what to do for a mission, step by step: [gbo-boilerplate](../gbo-boilerplate/SKILL.md), [gbo-module-architect](../gbo-module-architect/SKILL.md), [gbo-data-modeler](../gbo-data-modeler/SKILL.md), [gbo-implementer](../gbo-implementer/SKILL.md), and their reviewers [gbo-architecture-reviewer](../gbo-architecture-reviewer/SKILL.md), [gbo-model-reviewer](../gbo-model-reviewer/SKILL.md), [gbo-code-reviewer](../gbo-code-reviewer/SKILL.md). **The knowledge** (`knowledge/`) says how GBO works; the roles link to its sections and never copy them. Read this file first, find your path, then open the role.

## Find your path

### 1. The mission: the first yes decides

The questions go from the most open mission to the most closed: a wider mission includes the narrower ones, so the first that holds is the path. Judge each one by the request, then confirm it by the repository.

| # | Must you... | The request says | The repository shows | Path |
|---|---|---|---|---|
| P1 | Build a whole application, or the whole data side of one? | An application, a system, a back end, from scratch, several business areas | No project yet, or none that keeps data | [New application](#new-application) |
| P2 | Bring GBO into an application that keeps its data elsewhere? | Migrate, replace the database, port a schema or a model | A `DbContext`, EF Core, SQL scripts, another ORM; no GBO | [Adoption](#adoption) |
| P3 | Add a business area that no module holds? | A new module, area, domain, bounded context | GBO set up; the concepts of the request belong to no module of `Design/modules.md` | [New module](#new-module) |
| P4 | Add a use case: an operation, a command, a feature, the action of a screen? It is a vertical slice; today, its domain layers | Add, allow, let the user... | Its module exists; no Mutation, command or method does it yet | [New use case](#new-use-case) |
| P5 | Change what an existing use case does? | Change, extend, also, from now on, no longer | The use case exists | [Change](#change) |
| P6 | Fix a defect or a symptom? | An exception, a GBO diagnostic, slow, conflicts, lost changes, a Replay that differs | | [Symptom](#symptom) |
| P7 | Review? | Review, audit, check, before merging | | [Review](#review) |
| P8 | Answer a question on GBO? | How, can GBO, why | | [The knowledge](#the-knowledge) |

### 2. Where you are: the deliverables found

Each role starts from what exists: it reads the repository, and never asks what it can see. Its branches are chosen by these signals:

| Signal | Where | Means |
|---|---|---|
| GBO is set up | A project references `GhostBodyObject.Generator` with `OutputItemType="Analyzer"` | The projects exist |
| The technical base is decided | `Design/technical-base.md` | gbo-boilerplate passed its gate |
| The modules are decided | `Design/modules.md` | gbo-module-architect passed its gate |
| The model of a module is decided | `Design/<Module>/classification.md` | gbo-data-modeler passed its gate for it |
| A model exists | Interfaces with `[GhostRepository]`, `[GhostLog]` | |
| Another persistence | `DbContext`, EF Core packages, SQL migrations | Adoption |

A GBO project without `Design/` was built without these skills: the role that needs a missing deliverable first rebuilds it from the code, as the facts, and has it validated.

### 3. The style: read from the code, never chosen here

| The code shows | Style | Knowledge |
|---|---|---|
| Records with `[Mutation]`, a `GhostMutationPump` | Mutation Sourcing | [knowledge/mutation-sourcing.md](knowledge/mutation-sourcing.md) |
| Records and a `[GhostLog]`, handlers that apply them to the models, no `[Mutation]` | Event Sourcing | [knowledge/logs-chains.md](knowledge/logs-chains.md) |
| Repositories written by the application in ReadWrite transactions; a Log for an outbox at most | CRUD | [knowledge/transactions-queries.md](knowledge/transactions-queries.md) |
| Both Mutations and Records applied by handlers | A module in transition: each use case keeps its own form; a new one takes the style of `Design/technical-base.md` | Both |
| Nothing yet | Decided by gbo-boilerplate, in `Design/technical-base.md` | |

Read the style over the whole solution, module by module: the pump or the handlers may live in another project than the model. The style chooses the sections of the knowledge that a role reads; it never changes the path.

### 4. The paths

Each step is a role and its branch, in italics. `→` is a gate: the deliverable is validated by the user, with the report of its reviewer, before the next step starts.

#### New application

1. gbo-boilerplate, *from scratch* → `Design/technical-base.md`, reviewed by gbo-architecture-reviewer.
2. gbo-module-architect, *first map* → `Design/modules.md`, reviewed by gbo-architecture-reviewer.
3. For each module, after the modules it knows: gbo-data-modeler, *first model* → `Design/<Module>/classification.md`, reviewed by gbo-model-reviewer, *table*; then its interfaces, *check*.
4. For each module: gbo-implementer, *first use cases*, reviewed by gbo-code-reviewer and gbo-model-reviewer, *code detail*.

#### Adoption

gbo-boilerplate, *adoption* → gbo-module-architect, *inherited modules* → for each module moved to GBO: gbo-data-modeler, *port* → gbo-implementer, *first use cases*. The reviews as for a new application.

#### New module

gbo-module-architect, *change the map* → gbo-data-modeler, *first model* → gbo-implementer, *first use cases*.

#### New use case

gbo-implementer, *add a use case*; it goes up to gbo-data-modeler, *extend*, when the use case needs what the model lacks.

#### Change

gbo-implementer, *change a use case*; up to gbo-data-modeler, *extend*, when the model changes.

#### Symptom

gbo-implementer, *symptom*.

#### Review

| What is reviewed | Role |
|---|---|
| A whole application | The three below, the architecture first |
| The style, the Stores, durability, servers, projects, the modules | gbo-architecture-reviewer |
| A classification table at its gate, a model, a ported model | gbo-model-reviewer |
| A pull request or a codebase as a whole: transactions, concurrency, rules, side effects, the Log | gbo-code-reviewer |
| Code line by line | gbo-model-reviewer, *code detail* |

### 5. Going up

A path is where you start, not a cage. Each role says when to go up: a use case that needs a concept the model lacks goes up to gbo-data-modeler; a concept that belongs to no module, to gbo-module-architect; a need of durability or of servers, to gbo-boilerplate. To go up: stop, change the deliverable of that role by its own steps, pass its gate (with an ADR when a validated decision is reopened), then come back down.

### 6. One agent or several

The roles fit one agent or several; whether to delegate is decided by the harness and the user, never by a role. Each role states its **contract**: its inputs and its output (deliverables written in the repository, so that an agent that starts cold needs nothing else), its gate, what may run in parallel with it, and the independence its review requires.

**Independence without a second agent**: when the harness gives no independent agent, the same agent reviews in a separate pass, after the deliverable is written, from the deliverable, the code and the checklist only, and the report says that the review was not independent. The user then knows what the gate rests on.

## The deliverables

They live in `Design/` at the root of the solution, or where the instructions of the project (`AGENTS.md`, `CLAUDE.md`) put them:

```
Design/
├── technical-base.md            gbo-boilerplate: the style, the Stores and their policies, the servers, the projects
├── modules.md                   gbo-module-architect: the module map and the use cases of each module
├── decisions/NNNN-title.md      the ADRs that span modules
├── reviews/yyyy-mm-dd-what.md   the reports of the reviewers, kept with the gate they served
└── <Module>/
    ├── classification.md        gbo-data-modeler: the classification table and its decision record
    └── decisions/NNNN-title.md  the ADRs of the module
```

- **A deliverable holds the current state**, each decision with its reason and its rejected alternatives. Every change updates it; it never tells its history.
- **Each decision is *Proposed* or *Validated***. A gate is passed when the user validates the deliverable: its first line then says `Validated by <who> on <yyyy-mm-dd>, review: <the report>`, and its decisions turn *Validated*. A question of the business that the request does not settle (a rule, a volume, a limit) is asked of the user, with a proposed default, never decided in silence.
- **An ADR** (Architecture Decision Record) is written in two cases. A validated decision is reopened: the deliverable is updated, the ADR says why. Or a decision that no deliverable holds, and that a later agent could undo by not knowing it: an index and its measure, a concurrency tool (`[Mergeable]`, `[ConcurrencyPoint]`, `Claim()`, a lease, an Exclusive pump), a durability or lock policy, a rule of these skills set aside (a `#pragma warning disable GBO047`, an `AsEnumerable()`), a limit of GBO met (reported, never worked around). Never for a new entity, a new use case, a fix or a rename: the deliverables and git hold them.
- **An ADR is never edited**: a later one supersedes it. Its form:

```markdown
# 0007 <The decision, as a title>
Status: Accepted | Superseded by NNNN        Date: <yyyy-mm-dd>
Context: <the need, and its measure when there is one>
Decision: <what is decided, in GBO terms>
Rejected: <each alternative, and why>
Consequences: <what it costs, what it constrains>
Reopens: <the deliverable and the decision it changes, or none>
```

- **A review report** ranks its findings, most severe first, one per cause:

```markdown
# Review: <what was reviewed>, <yyyy-mm-dd>, <independent | by its author, in a separate pass>
| # | Severity | Place | Breaks | Consequence | Fix |
|---|---|---|---|---|---|
```

| Severity | Means |
|---|---|
| **Blocking** | Data lost or wrong; a Replay that cannot rebuild the models; a side effect that a rollback cannot undo; a start that fails; a model that cannot evolve (a number reused, an identity named by a Log and lost) |
| **Major** | A flaw that will cost a rewrite: concerns not separated, a module at the wrong level, a rule in the wrong place, conflicts by design, a value without its value object, a decision without its ADR |
| **Minor** | The rest |

- **No change log**: git holds the history, one commit per use case or per decision, its message saying what and why. A package published to others (a library of components) keeps a CHANGELOG for its consumers.

## The knowledge

| The topic | Read |
|---|---|
| Design a model: separate the concerns of each concept; an entity, a part, an `Option`, a component, a sub-component, a system or an edge | [knowledge/modeling/README.md](knowledge/modeling/README.md), the core of this knowledge base |
| Classify the concepts of a domain, audit a model; the classification table | [knowledge/modeling/checklist.md](knowledge/modeling/checklist.md), and the worked audit [knowledge/modeling/example-quote.md](knowledge/modeling/example-quote.md) |
| Values, identities, relations, components and systems, edges, concurrency, schemas, patterns | The other files of [knowledge/modeling/](knowledge/modeling/README.md#the-topic-files) |
| Open transactions, query, change, remove, handle conflicts, write business rules in partial classes | [knowledge/transactions-queries.md](knowledge/transactions-queries.md) |
| Decide whether an index is needed (rarely), declare text, value or hash indexes | [knowledge/indexes.md](knowledge/indexes.md) |
| Records, Logs, subscriptions, the change feed of a Repository, chains of Stores, StoreSystems and their migrations, Event Sourcing, CQRS, Replay | [knowledge/logs-chains.md](knowledge/logs-chains.md) |
| A system in Mutation Sourcing: Intention, Decision, `Decide`, `Apply`, the pump, the Replay | [knowledge/mutation-sourcing.md](knowledge/mutation-sourcing.md) |
| Projects, Stores, durability, distribution over servers, tests | [knowledge/infrastructure.md](knowledge/infrastructure.md) |
| Every attribute, its arguments and its status | [knowledge/reference/attributes.md](knowledge/reference/attributes.md) |
| What the generator writes for each declaration | [knowledge/reference/generated-api.md](knowledge/reference/generated-api.md) |
| A GBO diagnostic to fix | [knowledge/reference/diagnostics.md](knowledge/reference/diagnostics.md) |
| The traps of agents and the pitfalls of the API | [knowledge/reference/pitfalls.md](knowledge/reference/pitfalls.md) |

## The mental model, in twelve lines

1. **A model is a set of interfaces.** `MBook` gives the Body `Book` (a `sealed partial class`, to change), the View `BookView` (a `readonly partial struct`, to read), and with `[Enumerable]` the collections `BookCollection` and `BookViewCollection`. You never implement the interfaces.
2. **A Store keeps the data; a Repository binds the model to it** and opens its transactions. In memory or on disk, only the line that creates the Store changes.
3. **The transaction is ambient**: the one of the current execution context, found by `new Book()`, `BookCollection`, `ref.Resolve()` at any call depth and across `await`. There is no context to pass, no `Add`, no `SaveChanges`.
4. **Every change is visible at once** in its transaction, even to a query being enumerated; `Commit()` makes it durable and visible to the transactions opened after it.
5. **MVCC**: a transaction reads the Store as it was when it opened. Readers never wait; the first committer wins; the second gets a conflict and writes nothing. GBO never retries by itself.
6. **The model decides the lifetime** of each entity: a Root lives by itself, a part lives in its Owner, a shared object lives while a strong reference retains it. An entity dies at the instruction that releases its last retention, with its parts: no orphan, no dangling reference, no null.
7. **No null**: an absence is an `Option<T>`; a required link is a `T`, given at the birth.
8. **Search with Views, change with Bodies**: Views allocate nothing; a Body is kept by the transaction (one entity, one object).
9. **Lambdas capture nothing**: values go in as a *state*, before the lambda (error GBO013); `System.Linq` is refused on GBO sequences (GBO014).
10. **The Log keeps the facts**: Records appended, never changed; a model chained with its Log is written with it, and can be rebuilt from it by a Replay.
11. **One responsibility per unit, units that do not know each other**: an entity carries its own responsibility only; its other concerns are parts, components (the entity knows nothing of them), systems (the rules over several aspects) and edges (relations with data that neither end knows). Each group of fields is judged by its use (its concept, its fill rate, its detachment, its responsibility); a Root stays compact and delegates its aspects to aggregations and components, which may own lists of entities with components of their own.
12. **The values are value objects**: a price is a `Money`, a quantity a `Quantity`, a code a `Sku`, a state an enum, never a bare `decimal`, `string` or `bool`; a value object generated by `[ValueObject<T>]` on an interface `VMoney`, its rule and its display yours in a `partial struct Money`, its machine code that of `T`, is the first choice, `[ValueObject<GhostString>]` for a text; a property of the model names `VMoney`; a stored struct of explicit layout for a value of several fields. A partial Body holds no C# field: everything stored is in the model (the warning GBO047).

## The API in one screen

```csharp
using GhostBodyObject.Runtime.Logical.Body;      // Option<T>, GhostString, GhostSpan<T>
using GhostBodyObject.Runtime.Logical.Model;     // the attributes
using GhostBodyObject.Runtime.Physical;          // TransactionalMemoryStore
using GhostBodyObject.Runtime.Physical.Constants;// StoreAccessMode, TransactionConflictLevel

[Entity(1, 1, 3)]            // type identifier, model version, highest property identifier
[Root]                       // lives by itself
[Removable]                  // the code may Remove() it
[Enumerable]                 // ArtistCollection, ArtistViewCollection
internal interface MArtist
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] IEntityCollection<MAlbum> Albums { get; }      // composition: the albums are parts of the artist
    [Property(3), Weak] Option<MArtist> Mentor { get; set; }    // weak reference: set to none when the mentor is removed
}

[Entity(2, 1, 2), Enumerable]
internal interface MAlbum                                        // a part: no public constructor, born in its Owner
{
    [Property(1)] GhostString Title { get; set; }
    [Property(2)] int Year { get; set; }
    [OwnedFrom] MArtist Artist { get; }                          // the Owner, read only, no [Property]
}

[GhostRepository(12), IncludeEntityModel(nameof(MArtist)), IncludeEntityModel(nameof(MAlbum))]
internal interface MCatalogRepository { }

// The Store (in memory here; NewFileRepositoryStore(directory, name, mode) on disk) and the Repository.
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("catalog", StoreAccessMode.ReadWrite);
using var catalog = new CatalogRepository(store);

using (var tx = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
{
    var bowie = new Artist { Name = "David Bowie" };             // created in the ambient transaction
    var low = bowie.Albums.New();                                // a part is born in its composition
    low.Title = "Low";                                           // a GhostString: never in a tuple assignment (a ref struct)
    low.Year = 1977;
    tx.Commit();                                                 // synchronous; throws TransactionConflictException on a conflict
}

using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
{
    int before = 1980;
    int old = AlbumViewCollection.Count(before, static (album, year) => album.Year < year);   // state, then lambda
    Option<ArtistView> bowie = ArtistViewCollection.FirstOrNone(static a => a.Name == "David Bowie");
}
```

## The rules that are never broken

| Never | Always | Why |
|---|---|---|
| A `class` for the data, `DbContext`, `SaveChanges`, `repository.Add(entity)`, `Update` | `new Entity { ... }` in an open ReadWrite transaction, then `tx.Commit()` | The transaction is ambient; GBO is not a Unit of Work |
| `await tx.Commit()`, `GhostStore.WriteContext`, `[ObjectBody]`, `[BodyProperty]`, `[ValueObject]`, `IBodyEntityCollection`, `TransactionConflictLevel.Override` | The API of this knowledge base only | These names belong to an old prototype or were removed: they do not compile |
| A read or a write out of a transaction | `using var tx = repository.OpenTransaction(...)` | Out of one, collections and births throw `InvalidOperationException` |
| Two transactions of one Repository open in one execution context | Close the first; other threads (`Task.Run` from outside) have their own | No nesting |
| A lambda that reads a local, a parameter or `this` | `Count(value, static (item, v) => ...)`, up to 4 states | GBO013: a closure allocates at each call |
| `using System.Linq` operators on a GBO collection | The GBO operators (same names), or `.AsEnumerable()` to leave GBO on purpose | GBO014 |
| `null`, `FirstOrDefault`, `int?` | `Option<T>`, `FirstOrNone`, `GetOr(fallback)`, `TryGet` | There is no null in the API |
| `foreach (var x in BodyCollection) ...` to search many entities | Search the `ViewCollection`, then `ToBody()` on the few to change | Each Body is kept by the transaction |
| Rely on the order of a collection | A property (`Position`, `Date`) and `OrderBy` | A collection is a set |
| A rule that may refuse in `On...Changed` | Check, then write, in a method of the partial class | An exception after a write condemns the transaction |
| A business value as a `bool`, a `string`, a `decimal`, a `double` that says nothing of what it is | A value object: a stored struct with its operators (`Money`, `Quantity`, `Rate`), a converted type (`Sku`, `Email`), an enum in place of a `bool` | The model names its concepts; a wrong argument or assignment does not compile |
| A C# field or auto-property in a partial Body (`public int PriceChanges { get; private set; }`) | A stored property kept by `On...Changed`, a Record, a local variable, or a property with a body | State outside the model: never committed, replicated nor replayed, reset at each transaction; the warning GBO047 |
| Catch an exception to choose a business path | `TryGet`, `ResolveOrNone`, `index.Is(key).Any()`, `TryCommit(out kind)`, `TryGetOrNew` | Exceptions are failures, never control flow (the one exception: the refusal of a Mutation, see [knowledge/mutation-sourcing.md](knowledge/mutation-sourcing.md#step-4-decide-in-the-partial-class-of-the-body)) |
| An identity in an entity: a `*Ref`, a struct of the domain that implements `IGhostIdentity`, a `Guid` key of another entity | A link (`[Strong] MCustomer Customer`); an identity belongs in a Record or a Mutation | GBO012, which sees only a `*Ref` |
| A `[Property(n)]` number given to another property, a type identifier reused | Rename with a new version; never renumber | GBO stores values under their numbers (fixed schema) |
| Change the data of the Store without a Record, in a model rebuilt from its Log | Every change of such a model comes from a Record (or a Mutation) | A Replay rebuilds only what the Log says |
| Translate a relational schema table by table; groups of fields mostly empty; a flag and its satellite fields; a code that designates another entity | Classify each concept and each group of fields with [knowledge/modeling/checklist.md](knowledge/modeling/checklist.md) | A table mixes concerns that GBO separates: parts, components, systems, edges |

## What exists, and what does not (yet)

**Built:** entities, Roots, parts (three composition forms), strong and weak references and collections, shared objects, orphans, reverse properties; components and systems (ECS); edges; identities (`*Ref`, and `EntityRef` of any type); `Option<T>` of values, enums, `DateOnly`, `TimeOnly`, `DateTimeOffset`, `Half`, `Int128`, value objects generated by `[ValueObject<T>]` (numbers and texts in disguise), Records as properties of entities and Records and `RecordBag` (REQUIREMENT-052), explicit-layout structs, converters (found without `[Converter]` when unique), `Option<T>` of a converted type, value objects in `Sum`, `[Delta]`, `[Max]`, `[Min]` by their operators; `GhostString`, `GhostSpan<T>`; text, value and hash indexes, unique indexes; `[Mergeable]` with `[Delta]`, `[Max]`, `[Min]`, the Merger, `RemovalWins`; `[ConcurrencyPoint]`; `Claim()`, leases (`TryLock`, `GhostLease`); Exclusive transactions; `[Private]`, `[Internal]`; Records, Logs, subscriptions, chains, Replay; StoreSystems (`[StoreSystem]`, its Stores and their roles, the replay at its start, several Repositories on one Store, the evolution of its model: its numbers, the history of the versions, the migrations of its States); the typed change feed of a Repository (`repository.Feed()`); Mutations and `GhostMutationPump`; the Fixed and Deduced schemas; distribution (P2P endpoints, replicas, write token).

**Call points built:** `On{Property}Changed()`, `On{Collection}Added(item)`, `On{Collection}Removed(item)`, `OnDeleting()`, `bool Merge(TView epoch, TView head)` (Mergeable types), `Decide()` and `Apply()` (Mutations).

**Specified but not built, do not use** (the build fails): `[Required]`, `[Utf8]`, `[ZipCompressed]`, `[SingleBit]`, `[Separated]`, `[Immutable]`, `[Protected]`, `[CheckTransaction]`, `GhostQueue<T>`, `GhostBitmap`, `IIndexKey<T>`, `OnCreated`, `OnValidate`, `OnCommitting`, `OnCommitted`, `OnRolledBack`, the Trim of a Log, automatic fail-over, SQL.

When a document of the repository and the code disagree, the code wins: `GhostBodyObject.Runtime/Logical/Model/ModelAttributes.cs` (the attributes), `GhostBodyObject.Generator/Diagnostics.cs` (the diagnostics), and the tutorial test projects `GhostBodyObject.Tutorial*` (the API in use).

## In the GBO repository itself

The sources of these files, for a detail they do not give: the tutorials `Documentation/Tutorials/One-TheBasics.md` to `Eight-ChainsAndDistribution.md` (each lesson is a test of `GhostBodyObject.TutorialOne` to `Eight`), the specifications listed in `Documentation/Specifications/SPEC-Registry.md` (SPEC-EntityModel, SPEC-MutationSourcing, SPEC-Edges, SPEC-IndexCollections, SPEC-Storage), the reference of the quote port `GhostBodyObject.Quote.Domain/Documentation/Design/GBO-Reference.md`, and the Mutation Sourcing shop of `GhostBodyObject.EventSourcing.Benchmarks/Shop/ShopMutationModel.cs`. A tutorial page may lag behind its test project: the test project is the truth.
