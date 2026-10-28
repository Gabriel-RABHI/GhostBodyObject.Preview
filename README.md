![Ghost Body Object](GBO-Logo.png)

# What is GBO?

**GBO** (Ghost Body Object) is a high-performance, transactional, persistent and distributed memory manager. It manages millions of C# objects in collections that can be several times larger than the physical RAM. These large collections can be shared and spread over dozens of servers in real time, transparently. GBO can make this memory persistent on disk, and supports the best standards of snapshot isolation (MVCC) and durability (ACID, hash-based recovery).

Its API is ambient: managing these objects takes no load and no save operation. You open a transaction, create or find the objects, change them, and commit the transaction. These collections of objects can hold the data of a business application, make independent systems communicate without a message broker, build a cache infrastructure, aggregate data in edge computing or IoT, or build complex distributed systems that combine all of these.

Transactions at the memory level bring a massive simplification and a smaller code base, from the smallest applications to the largest, modular and distributed ones. The data model combines object graphs, entity component systems and edge-based graphs, and supports various indexes, text included.

GBO introduces a new paradigm in error and memory management, made for both human developers and the next generation of AI Dark Software Factories. It combines strong safety (no more `NullReferenceException`), deterministic memory management and a sophisticated, constrained data model: the combination that makes Rust so efficient in AI-assisted software development.

### A minimal sample

An agenda of appointments, kept on disk: the model in one file, the operations in another, then the same program on two nodes.

#### 1. The model: `Model.cs`

A model is a set of interfaces: the generator of GBO reads them at each build, and writes the classes.

```csharp
// An appointment: from this interface, the generator writes the class Appointment and AppointmentCollection.
[Root]       // an appointment lives by itself
[Enumerable] // AppointmentCollection enumerates the appointments
internal interface MAppointment
{
    GhostString Subject { get; set; }

    DateTime At { get; set; }
}

// Your own code in the generated class: computed at each read, never stored.
internal partial class Appointment
{
    public bool IsToday => At.Date == DateTime.Today;
}

// The Repository of the model.
[GhostRepository(Schema = GhostSchema.Deduced)]
[IncludeEntityModel(nameof(MAppointment))]
internal interface MAgendaRepository
{
}
```

This model is deliberately minimal: a model can declare many entity types, with complex relations, constraints and indexes ([Tutorials Two to Five](Two-Relationships.md)). A model that must evolve, read by both the old and the new versions of an application, gives its numbers, as [Tutorial One](One-TheBasics.md) shows.

#### 2. The operations: `Program.cs`

Each operation is a transaction. The new objects and the collections find the open transaction by themselves: there is no context to pass.

```csharp
// A Store on disk, in "c:/gbo-sample/", and the Repository that binds the model to it.
using var store = TransactionalMemoryStore.NewFileRepositoryStore("c:/gbo-sample/", "agenda", StoreAccessMode.ReadWrite);
using var agenda = new AgendaRepository(store);

// A Write transaction inserts an appointment, today at 9:00: it is created in the open transaction, and written by Commit.
using (var tx = agenda.OpenTransaction(StoreAccessMode.ReadWrite))
{
    new Appointment { Subject = "Dentist", At = DateTime.Today.AddHours(9) };
    tx.Commit();
}

// A second one finds it, and moves it to 10:30.
using (var tx = agenda.OpenTransaction(StoreAccessMode.ReadWrite))
{
    AppointmentCollection.First(static a => a.Subject == "Dentist").At = DateTime.Today.AddHours(10.5);
    tx.Commit();
}

// A Read transaction finds the appointments of today, as committed.
using (agenda.OpenTransaction(StoreAccessMode.ReadOnly))
{
	// Notice that you can call any added entity member in LINQ queries.
	// IsToday is a calculated property, evaluated on the fly.
	// Collections are not IQueryable, but true IEnumerable.
    foreach (var appointment in AppointmentCollection.Where(static a => a.IsToday))
        Console.WriteLine($"{appointment.Subject} at {appointment.At:t}"); // Dentist at 10:30
}
```

The same Store can be opened by several processes on the same computer, each with its own Repository, without duplicating the data in memory, and all of them can write. This lets the GBO tooling view and query the data while the applications run.

#### 3. Distributed: a second node

Each node runs the same program. An endpoint connects the nodes, and the Store is added to it before its first transaction: only the opening of the Store and the end of the program change.

```csharp
// An endpoint on the port 7000, which joins the cluster through the other node, its seed. The program receives its address:
// 10.0.0.2 on the node 10.0.0.1, and 10.0.0.1 on the node 10.0.0.2.
using var endPoint = new StoreDistributionEndPoint(DistributionModel.P2P, new() { ListenPort = 7000, Others = [new(args[0], 7000)] });

// The same Store on each node, in its own directory, added to the endpoint before its first transaction.
using var store = TransactionalMemoryStore.NewFileRepositoryStore("c:/gbo-sample/", "agenda", StoreAccessMode.ReadWrite);
endPoint.Add(store);
using var agenda = new AgendaRepository(store);

// The three transactions of Program.cs, unchanged.

// The node stays in the cluster until Enter. At its stop, the other node holds each of its commits.
Console.ReadLine();
```

Each commit on one node is copied to the other, in the order of the commits: both nodes read and write the same appointments. Start the second node while the first one runs. Two nodes that both wrote while apart hold two different histories: when they meet, both stop rather than merge them ([Tutorial Seven](Seven-ChainsAndDistribution.md)).

You can read the first seven tutorials to discover what GBO can do :

1. [Tutorial One: The Basics](One-TheBasics.md): GBO in a few words
2. [Tutorial Two: Relationships](Two-Relationships.md): links, constraints and deterministic garbage collection
3. [Tutorial Three: Composition](Three-Composition.md): Entity Component System and object composition model
4. [Tutorial Four: Edges](Four-Edges.md): edges and graph oriented model
5. [Tutorial Five: Indexes](Five-Indexes.md): indexes and custom collections
6. [Tutorial Six: The Log](Six-TheLog.md): the Log and the Records
7. [Tutorial Seven: Chains and Distribution](Seven-ChainsAndDistribution.md): several Stores, several servers

## Why GBO?

### **Extreme simplicity**

The database, the message broker and the application are one: the data is C# objects in the memory of the process, found and changed in ambient transactions, with no query to load them, no save, no mapping and no cache to keep in sync accross an entire cluster. The state is always coherent: a transaction reads a snapshot, and commits all or nothing, over every model it touches and on every server. The code shrinks with it: no SQL, no DTOs, no mappers, no cache invalidation, no integration tests.

### **None of the pitfalls of an ORM**

No N+1 queries: a navigation is a read of memory, not a round trip, and lazy loading costs nothing: 1.36 M objects/s against 5,271 for EF Core with PostgreSQL. No change tracker, no context to keep alive or dispose at the right moment, no detached entity, no stale copy: an object changed is changed in its transaction, and the others see it at its commit. No impedance mismatch: the model is the C# model, its relations included.

### **Deterministic memory, by the model**

An object lives while the model says so: a root, a part of its owner, or designated by a strong reference. It is removed at the instruction that ends this, its parts with it, its weak references cleared: no orphan, no dangling reference, no null reference (`Option<T>`). The data stays out of the .NET garbage collector, which never scans it, whatever its volume.

### **Infrastructure collapse**

One library in the process and one directory on the disk replace the database server, the ORM, the distributed cache and, for the events of the application, the message broker: replicas copy the data where it is read, Logs and their subscriptions carry the events, a replay rebuilds a model. Fewer moving parts: less to deploy, to monitor, to secure and to pay for.

### **High performance**

No serialization, no network round trip, no copy: the data is read where it is stored. Against EF Core with PostgreSQL, on the same machine, GBO inserts 25 to 60 times faster, reads orders by their key 311 times faster, and aggregates 3.5 to 6 times faster ([results](#gbo-vs-ef--postgresql)). On YCSB, it reads up to 4.7 M records/s on 8 threads, 15 times RocksDB and 7 times Garnet.

### **Built for AI-assisted development**

A constrained model, which the generator checks at each build with about forty diagnostics: the relations and their cycles, the lifetimes, the types of the properties, the lambdas that would capture or allocate. An agent meets its mistake as a compile error, not as a bug in production. There is less code to generate, to read and to review, and each test runs the real engine in memory.

### **Concurrency without locks**

Threads, processes and servers share the data through transactions, not critical sections: no lock to forget, no deadlock, no race on a half-written state. An exception, a failed rule or a conflict rolls the whole transaction back: an error never leaves a partial state to repair.

### **Distribution by configuration**

A Store is replicated by adding it to an endpoint: read-only replicas for the front ends, several writers sharing one write token, each server with the part of the data it needs. The code of the application does not change.

### **Events and history built in**

Logs of Records, subscriptions that replay then follow, chains that commit the models and their Log together: Event Sourcing, CQRS and Mutation Sourcing without an event store, a broker or a projection pipeline to run. A model rebuilt by a replay is a migration with no script to write.

### **A model that evolves**

Each property has a number: a new version of the application reads the data of the old one, and the old one the data of the new one. Rolling upgrades, and no migration for a property added.

### **Fast and faithful tests**

In-memory Stores run the same engine as the persistent ones: no database to start, no mock, a test in milliseconds; the thirty lessons of Tutorial Six run in half a second.

### **Data that protects itself**

Stores on disk are read through read-only views, so that a stray write of the application cannot corrupt them; each transaction carries its hash, checked at the opening and at each copy, so that a damaged or diverging replica is stopped, never replicated. A Store of 1,000,000 records reopens in 0.4 s, its index ready.

## GBO vs EF + PostgreSQL

GBO is faster than almost all general purpose database engines and provide a more coherent experience that an ORM like Entity Framework. Here a benchmark between GBO and Entity Framework using PostgreSQL 18.

**Date:** 2026-10-27. **Machine:** Intel i9-9900K, 16 logical processors, Windows 10 22H2 (19045), .NET 10.0.12, workstation GC :

- **GBO:** a persistent Repository Store on "C:", a WDC PC SN730 NVMe SSD.
- **EF Core:** Microsoft.EntityFrameworkCore 10.0.9, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1, Npgsql 10.0.2.
- **PostgreSQL:** 18.0, x86_64-windows, on the same machine, its default settings: `fsync` on, `wal_sync_method` open_datasync, `shared_buffers` 128 MB, `wal_writer_delay` 200 ms.

**The same data model.**

- **GBO:** `Product` and `Order` are Roots `[WithIdentity]`; an `OrderLine` is a part of its order, and designates its product by a `[Strong]` reference, whose reverse property `Product.Lines` gives the lines of a product. The Store is alone, the only writer of the process (its lock policy `Retain`); each other setting is at its default, the compactor included.
- **EF Core:** three tables, the keys of the orders and the lines a bigint identity, the foreign keys indexed. A pooled DbContext per transaction, `AsNoTracking` for the reads, `Include` for the graph, the lazy loading by `ILazyLoader`, without proxies, in a model of its own. Npgsql prepares the statements it executes often (`Max Auto Prepare` 64), and the read by key is compiled (`EF.CompileQuery`). The reads open no transaction of their own: each query sees its own snapshot. Each load ends with a `VACUUM ANALYZE`.

The dataset: 200 products, 100,000 orders of 20 lines each: 2,000,000 lines. An order has at most one line per product, so the number of orders of a product is the number of its lines.

**The same data and the same work.**

From the same seeds, both engines build the same shop, read the same orders, and change them in the same way. Each read step computes a check from its first transaction: the checks of the two engines are equal, step by step, such as 33,398 orders of a status, a total of 562,150,423.07 for every order, 2,000,000 orders and the same revenue for the products. The reads run on the last load, before its changes; the changes and the concurrent step follow.

**The durability of the writes.**

| Mode                   | GBO                                                                                              | PostgreSQL                                                                               |
| ---------------------- | ------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------- |
| Durable at each commit | `FlushPolicy.DurableOnDisk`: the commit returns once the transaction survives a power loss       | `synchronous_commit = on`: `COMMIT` returns once the WAL is flushed                      |
| Durable within a delay | `FlushPolicy.FastOnDisk`: flushed within 1 ms or 8 MB, each flush followed by `FlushFileBuffers` | `synchronous_commit = off`: the WAL writer flushes within 3 × `wal_writer_delay`, 600 ms |

One thread, 30 s per timed step; each load inserts the 100,000 orders into an empty Store, or a database created again.

### The results

| Step                                                         | GBO             | EF Core + PostgreSQL | GBO / EF |
| ------------------------------------------------------------ | --------------- | -------------------- | -------- |
| Insert 100 orders per transaction, durable at each commit    | 21,496 orders/s | 878 orders/s         | ×24.5    |
| Insert 10,000 orders per transaction, durable at each commit | 52,784 orders/s | 882 orders/s         | ×59.8    |
| Insert 100 orders per transaction, durable within a delay    | 32,550 orders/s | 933 orders/s         | ×34.9    |
| Insert 10,000 orders per transaction, durable within a delay | 49,273 orders/s | 896 orders/s         | ×55.0    |

| Step                                                                                                     | GBO                 | EF Core + PostgreSQL | GBO / EF |
| -------------------------------------------------------------------------------------------------------- | ------------------- | -------------------- | -------- |
| Change 1 order per transaction (a line removed, a line added, the order changed), durable at each commit | 1,753 orders/s      | 1,112 orders/s       | ×1.6     |
| Change 10 orders per transaction, durable at each commit                                                 | 10,642 orders/s     | 3,871 orders/s       | ×2.7     |
| Change 1 order per transaction, durable within a delay                                                   | 16,400 orders/s     | 1,250 orders/s       | ×13.1    |
| Change 10 orders per transaction, durable within a delay                                                 | 15,548 orders/s     | 4,045 orders/s       | ×3.8     |

| Step (using LINQ)                                                  | GBO                 | EF Core + PostgreSQL | GBO / EF |
| ------------------------------------------------------------------ | ------------------- | -------------------- | -------- |
| Count the orders of a status (`Order.Where`)                       | 55,467,198 orders/s | 18,235,132 orders/s  | ×3.0     |
| The total of each order: its lines and the price of their products | 12,574,367 lines/s  | 2,353,104 lines/s    | ×5.3     |
| The same, EF: a `GROUP BY` of the lines                            | 12,574,367 lines/s  | 2,083,536 lines/s    | ×6.0     |
| The orders and the sales of each product, by the reverse           | 5,298,306 lines/s   | 1,508,254 lines/s    | ×3.5     |
| The same, EF: a `GROUP BY` of the lines                            | 5,298,306 lines/s   | 1,197,182 lines/s    | ×4.4     |

| Step                                                                                                     | GBO                 | EF Core + PostgreSQL | GBO / EF |
| -------------------------------------------------------------------------------------------------------- | ------------------- | -------------------- | -------- |
| Read 100 random orders per transaction, one by one                                                       | 3,205,427 orders/s  | 10,299 orders/s      | ×311.2   |
| The same, EF: one query for the 100                                                                      | 3,205,427 orders/s  | 304,814 orders/s     | ×10.5    |
| Load and read 1,000 random orders, their lines and products, per transaction                             | 271,859 orders/s    | 12,421 orders/s      | ×21.9    |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product                                 | 1,364,387 objects/s | 5,271 objects/s      | ×258.8   |

**This last comparison is unfair:** GBO has no data to transport, since every entity is instantly accessible. It simply shows that navigating between entities is no longer a problem with GBO. For its part, EF Core re-hydrating 304k entities per second is a strong result.

### Concurrent: one writer, 1 to 8 readers

One thread changes 1 order per transaction, durable within a delay, while 1 to 8 threads compute the totals of every order, each in a loop, 30 s per number of readers. A total of every order reads the 2,000,000 lines.

| Readers | GBO: changes, orders/s | EF: changes, orders/s | GBO / EF | GBO: totals of every order /s | EF: totals of every order /s | GBO / EF |
|---|---|---|---|---|---|---|
| 1 | 9,777 | 1,205 | ×8.1 | 3.0 | 0.9 | ×3.5 |
| 2 | 13,488 | 1,124 | ×12.0 | 5.8 | 1.6 | ×3.7 |
| 4 | 11,918 | 1,045 | ×11.4 | 11.0 | 2.9 | ×3.9 |
| 8 | 10,686 | 867 | ×12.3 | 19.8 | 4.6 | ×4.3 |

GBO reads 39.6 M lines/s with 8 readers, and EF Core with PostgreSQL 9.2 M lines/s.

## GBO vs Event Sourcing solutions

GBO is implementing a new Event Sourcing pattern, called **Mutation Sourcing**. A Mutation is a single work record with Command (Intent) and Event (Decision) stored in the log. GBO can be used to implement Event Sourcing too, with persistent projections, including the one used to "Decide" the Command processing.

| Engine | Durability | The aggregates | The Read model | A conflict |
|---|---|---|---|---|
| **GBO, Mutation Sourcing** | `FastOnDisk`, a flush to the disk within 1 ms; or `DurableOnDisk`, a flush at each commit | The Write model in memory; a command is a Mutation, decided and applied by the pump of the base, in batches of up to 256 Mutations or 1 ms | In memory, in the transaction of the Mutation | None: the pump is the only writer |
| **GBO, Event Sourcing** | `FastOnDisk` | The Write model in memory; a transaction of the chain per command, its Events in the Event Log | In memory, in the transaction of the command | `TryCommit` gives it back; the command runs again |
| **KurrentDB 26.2.3** | Its default, a flush at each write | A stream per aggregate, cached in the process; a command appended by `AppendRecords`, atomic over its streams, the revision of each stream it read checked | In memory, by a catch-up subscription to `$all` | An exception of the client; its streams caught up, the command runs again |
| **Marten 9.54.2, PostgreSQL 18** | `synchronous_commit = off`, `wal_writer_delay = 1ms`: durable within about 3 ms | A stream per aggregate, its snapshot projected inline; locked by `FetchForExclusiveWriting`, the customer then the products in the order of their keys; Quick appends | Documents projected inline, in the transaction of the command | None: the locks wait |
| **Axon Framework 5.4.1, Axon Server 2026.1.7** | Its events forced to the disk every 1 ms; its default, 1,000 ms, in one more run | Dynamic Consistency Boundaries: an event per fact, tagged with each entity it concerns; the entities sourced from their tags, a snapshot every 100 events | In memory, by a subscribing processor, each event applied once the unit of work commits | An exception, the append rejected by its condition; the command runs again |

The settings of the installation for the rest: PostgreSQL with its 128 MB of shared buffers; KurrentDB, Axon Server and GBO with their defaults.

What each engine writes for a command:
- **KurrentDB and Marten** append an event to each stream whose aggregate the command changes. A `PlaceOrder` writes its order, its customer and each of its products.
- **Axon** writes one event, tagged with all of them.
- **GBO** writes one Mutation in Mutation Sourcing, and the Events of the command in Event Sourcing.

### The results

| Engine | Threads | Catalog, commands/s | Mix, commands/s | Conflicts | Latency p50, µs | p99, µs | p99.9, µs | On the disk, B per command | Commands played | Restart, s | Replayed, commands/s | Working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO, Mutation Sourcing | 1 | 19,258 | 7,083 | 0 | 42 | 1,892 | 3,625 | 264 | 520,000 | 10.46 | 49,713 | 1,036 |
| GBO, Mutation Sourcing | 8 | 44,864 | 8,728 | 0 | 630 | 4,116 | 10,387 | 211 | 648,000 | 11.80 | 54,915 | 1,447 |
| GBO, Mutation Sourcing, pipelined | 1 | 103,212 | 17,729 | 0 | — | — | — | 187 | 1,269,000 | 20.94 | 60,602 | 9,961 |
| GBO, Mutation Sourcing, durable | 1 | 2,090 | 1,745 | 0 | 534 | 1,526 | 2,398 | 271 | 136,000 | 2.05 | 66,341 | 495 |
| GBO, Mutation Sourcing, durable | 8 | 7,623 | 5,170 | 0 | 1,358 | 4,194 | 6,369 | 182 | 381,000 | 6.31 | 60,380 | 815 |
| GBO, Event Sourcing | 1 | 31,620 | 8,298 | 0 | 28 | 1,858 | 3,652 | 424 | 606,000 | 12.98 | 46,687 | 1,390 |
| GBO, Event Sourcing | 8 | 33,757 | 6,508 | 303,371 | 60 | 14,328 | 29,852 | 428 | 482,000 | 9.48 | 50,844 | 1,142 |
| KurrentDB | 1 | 742 | 754 | 0 | 1,220 | 2,978 | 13,705 | 2,371 | 65,000 | 11.96 | 5,435 | 1,542 |
| KurrentDB | 8 | 3,424 | 1,014 | 199,477 | 2,122 | 126,353 | 333,971 | 2,374 | 85,000 | 16.28 | 5,221 | 1,570 |
| Marten | 1 | 1,062 | 178 | 0 | 4,600 | 22,479 | 25,051 | 13,458 | 25,000 | 0.16 | — | 476 |
| Marten | 8 | 4,601 | 273 | 0 | 6,492 | 124,912 | 165,937 | 13,142 | 33,000 | 0.25 | — | 535 |
| Axon | 1 | 306 | 209 | 0 | 4,084 | 15,262 | 26,984 | — | 27,000 | 1.73 | 15,607 | 2,296 |
| Axon | 8 | 967 | 134 | 13,906 | 23,331 | 708,313 | 2,336,227 | — | 24,000 | 1.63 | 14,724 | 2,895 |
| Axon, sync 1,000 ms | 1 | 304 | 210 | 0 | 4,098 | 14,713 | 27,886 | — | 27,000 | 5.82 | 4,639 | 2,314 |
| Axon, without snapshots | 1 | 305 | 205 | 0 | 4,094 | 20,660 | 34,931 | — | 27,000 | 1.60 | 16,875 | 2,299 |

How to read the columns:
- **Mix:** the commands done in the 60 s measured; the refused ones count, 0 to 0.5 % of them, more as the mix goes on.
- **Conflicts:** the executions again of a command whose engine met a concurrent one.
- **Latency:** in the pipelined row, a thread posts without waiting, so the latency of a post means nothing.
- **On the disk**, what each engine wrote for its commands of the mix:
  - GBO: its Log;
  - KurrentDB: its log, by the positions of `$all`, its chunks being files of 256 MB allocated at once;
  - Marten: the WAL of PostgreSQL;
  - Axon Server: not measured, its segments allocated at once.
- **Commands played:** the catalog and the mix, to the checkpoint after the measure; the Read model of each engine holds them, and its restart rebuilds them.
- **Restart:** the application stopped and started again, until its first command can run.
  - GBO replays its Log into both models, Write and Read (it rebuild the projections).
  - KurrentDB and Axon rebuild their Read model in memory from the first event; their aggregates load again at their first command, out of this time.
  - Marten has nothing to rebuild: its snapshots and its Read model are tables.
- **Replayed:** the commands played divided by the time of the restart.
- **Working set:** the process of the benchmark and its servers, at the end of the mix, for the commands played. For PostgreSQL, the private bytes of its processes and its shared buffers once.
## Spreadsheet

GBO on one page: what the engine does, how each guarantee is obtained, and the industry principle it follows. The details are in the [tutorials](One-TheBasics.md).

#### At a glance

| | GBO |
|---|---|
| **Kind** | An embedded, in-process transactional object store for .NET 10: the data lives in the memory of the application, as C# objects, with no database server and no mapping layer |
| **Data model** | C# interfaces, compiled by a source generator into structs: entities, their parts (composition), strong and weak references, components (ECS), edges (property graph); Records, immutable facts, in Logs |
| **Storage** | Append-only, log-structured segments, in RAM or in memory-mapped files; Repository Stores (the current state) and Log Stores (every Record, kept) |
| **Transactions** | ACID; snapshot isolation by MVCC; optimistic by default, pessimistic or exclusive on demand; commits serialized in the whole cluster |
| **Durability** | Group commit: from a flush to the device at each commit to a flush within 1 ms, 10 ms or 100 ms |
| **Replication** | Log shipping of the committed bytes, pulled by each replica; read-only replicas and several writers sharing one write token |
| **Queries** | Operators over read-only Views, without allocation; text indexes (BM25), value indexes, hash indexes, all transactional |
| **Memory** | The objects outside the .NET GC; their deletion deterministic, by ownership and reference counts |
| **Architectures** | Chains of Stores, one transaction over several models and their Log; subscriptions and replay (Event Sourcing, CQRS); Mutation Sourcing |

#### Transactions and concurrency

| Mechanism | How GBO does it | Industry principle |
|---|---|---|
| Isolation | A transaction reads every object as of its epoch, the last commit published when it opened. Each version keeps a link to the previous one; readers never block writers, nor writers readers | Multi-version concurrency control and snapshot isolation (Berenson et al., *A Critique of ANSI SQL Isolation Levels*, 1995), as in PostgreSQL and Oracle |
| Writes | A write copies the object into the arena of its transaction, local to its thread (copy-on-write): the transaction reads its own writes; the others see nothing before the commit | Copy-on-write, read-your-writes |
| Validation | At the commit, under the write token, each object written is checked against the versions committed since the epoch: the first committer wins, the second gets a `TransactionConflictException`, or `false` from `TryCommit` | Optimistic concurrency control, backward validation (Kung & Robinson, 1981) |
| Conflict levels | `Optimistic`: write-write conflicts, as snapshot isolation (write skew possible). `Pessimistic`: also the collections read, a conflict if one of their objects changed since the epoch (phantoms included). `Exclusive`: the write token taken at the opening, no other commit in the cluster until the end, nothing to validate | From snapshot isolation to serial execution |
| Mergeable types | Concurrent changes of the values of a `[Mergeable]` object are merged at the commit, from the version of the epoch, the head and the transaction, instead of conflicting; a removal wins | Three-way merge, in the spirit of CRDT counters (Shapiro et al., 2011) |
| Concurrency points | A change anywhere in an aggregate raises the version of its `[ConcurrencyPoint]`: two transactions on the same aggregate conflict, whatever objects they changed | Aggregate versioning, an ETag (Domain-Driven Design, optimistic concurrency tokens) |
| Leased locks | `Claim()` locks objects for a duration, across transactions and servers: committed and replicated with the object, checked at each write, refused at once (no queue, no deadlock); durations on the monotonic clock of each node, never a wall clock compared; a lease lost or expired fails its holder's commit | Leases (Gray & Cheriton, 1989) with fencing tokens (Kleppmann) |
| Unique values | A unique index refuses a duplicate at the write, and again at the commit against the commits of other transactions and other servers (`Uniqueness`) | `UNIQUE` constraints |

#### Storage and durability

| Mechanism | How GBO does it | Industry principle |
|---|---|---|
| Layout | Each commit is appended to the head segment of its Store, and never changed; a segment is sealed when full | Log-structured storage (Rosenblum & Ousterhout, *The Log-Structured File System*, 1991): the log is the database |
| Atomicity | Each transaction ends with its hash; a chain of Stores combines the hashes of its Stores in the commit of its first Store: all of them or none, without two-phase commit | Commit record, checksummed |
| Recovery | At the opening, a transaction whose hash is missing or wrong, interrupted, is discarded; nothing to redo or undo | Torn-write detection by checksum |
| Durability | The flush policy of a Store: `Durable` (each commit flushed before it returns), `Fast` (within 1 ms or 8 MB), `Paged`, `Batched`, `Delayed`, `Manual`; `OnDisk` adds `FlushFileBuffers` on Windows (on Linux, `msync(MS_SYNC)` already reaches the device). Another process or server sees a commit only once it is durable | Group commit (DeWitt et al., 1984), as `synchronous_commit` in PostgreSQL |
| Index of the objects | The current version of each object, in an index in RAM, rebuilt at the opening; on disk, each sealed segment has an index file, which the opening reads instead of the segment | Bitcask and its hint files |
| Old versions | A version is kept while a transaction may still read it: the oldest open transaction sets the horizon | Epoch-based reclamation, as the vacuum horizon of PostgreSQL |
| Space and order | A compactor moves the live versions to the head, oldest segment first, so that its file can be deleted; it keeps each collection in runs of about 64 KB in the order of its objects, which scans read sequentially | Segment cleaning of log-structured stores; clustering |
| Several processes | The memory-mapped files are shared by the processes of a machine; a lock file serializes their commits | Shared memory, single writer at a time |
| Protection | A Store on disk is read through read-only mapped views: a stray write of the application cannot corrupt it | Protected memory |

#### Replication and distribution

| Mechanism | How GBO does it | Industry principle |
|---|---|---|
| Membership | Each `StoreDistributionEndPoint` listens on a TCP port and joins through one seed; members spread by gossip with heartbeat counters; a member silent for `DeadTimeout` is ejected; a restarted process is a new incarnation | Gossip-style failure detection (van Renesse et al., 1998), as in Cassandra |
| Data | A replica copies the committed bytes of each segment, identical, verifies each transaction by its hash, then publishes it; it pulls one block of 32 KB at a time, from any replica that has it, chosen at random | Physical log shipping, as the streaming replication of PostgreSQL; pull-based |
| Roles | A Store opened `ReadWrite` is a Master, `ReadOnly` a Slave; a Slave shows only the transactions that every admitted Master holds, so that any Master that remains holds what a Slave showed | Primary-copy replication, consistent prefix |
| Writes | One write token per chain of Stores. A commit asks the owner, which hands it over in the order of the requests, the queue travelling with the token, its generation increasing at each move; the new owner first catches up with the last commit. The commits form one sequence, the same on every replica. A Master that writes alone keeps the token and commits without a network round trip | Token-based mutual exclusion (Suzuki–Kasami, 1985), generations as fencing epochs |
| Partial replicas | A server may hold a part of a chain: the Read model of a CQRS front end alone, a Log alone and its subscription | Selective replication |
| Leaving and joining | A Master that stops hands its token over and waits until another Master holds its last commit; a new replica copies the whole Store, then follows | Graceful leave, catch-up replication |
| Failures | If the owner of the token is lost, the writes of its chain stop (`WriteTokenUnavailable`) and the reads go on; data that do not verify stop the chain (`Corrupt`). No quorum and no automatic fail-over yet: high availability is planned for a second version | Consistency over availability (CP) |
| Not yet | The Hub model, for WAN, IoT and edge computing over WebSocket; the sharing of transactions over UDP | |

#### Model, memory and queries

| Mechanism | How GBO does it | Industry principle |
|---|---|---|
| Model | Interfaces with `[Entity]`, `[Record]`, `[Property(n)]`; the source generator writes the Bodies (to change), the Views (to read), the Repositories, the Logs | Code generation from a schema |
| Versions of the model | Properties identified by their number, not their name: a binary reads the objects of older and newer versions of its model, in both directions | Field numbers, as in Protocol Buffers |
| Memory | The values of an object in one contiguous block; strings and arrays read in place, without copy (`GhostString`, `GhostSpan<T>`); the .NET GC never scans the data | Off-heap storage, zero-copy |
| Lifetime | An object lives while it is a Root, has an owner, or a strong reference designates it; removed at the instruction that ends this, its parts in cascade, its weak references cleared; no graph is ever traversed | Ownership and reference counting, deterministic (as Rust ownership or `ON DELETE CASCADE`) |
| Queries | `Where`, `Count`, `FirstOrNone`... over the Views, with lambdas that capture nothing, so no allocation; a scan reads collections kept in order by the compactor | Language-integrated queries, sequential scans |
| Text index | Words of the texts, whatever their case and accents; required words, alternatives, exclusions, wildcards, fuzzy terms; the most relevant first | Inverted index, BM25 ranking with the parameters of Lucene (`k1 = 1.2`, `b = 0.75`) |
| Value indexes | Keys of any standard type, alone or composite, in sorted pages of 4 KB; each key's set of objects in the most compact container: array, bitmap, runs, inverted, full | B+tree-like sorted pages; Roaring bitmaps (Chambi, Lemire et al., 2016) |
| Hash index | A key found without order, its buckets in pages | Hash index |
| Up to date | Every index is up to date at the next instruction, in the transaction too, rolled back with it, committed with it, rebuilt from the objects when needed | Transactional, synchronous indexes |
| Logs and subscriptions | A subscription reads a Log from any transaction, then follows its new commits on a thread of its own; its handlers capture nothing | Catch-up subscriptions, as in EventStoreDB, or the offsets of Kafka |
| Replay | A model chained with its Log is rebuilt by replaying it: in memory at each opening, or on disk on request | Event Sourcing, CQRS |
| Mutation Sourcing | A Mutation records the intention and the decision of a command; a pump runs the Decides one at a time on each server, in the order received, the effects in the models applied by `Apply` | Single-writer principle (LMAX Disruptor), the Decider pattern |

#### Limits

- **One writer at a time per chain.** The commits of a chain are serialized in the whole cluster: write throughput is that of one writer, without network round trips while it keeps the token. Writers scale by splitting the data into several chains.
- **No SQL.** The queries are C#, over the Views and the indexes.
- **Little-endian platforms only.**

#### To come

- **Automatic fail-over.** The loss of the owner of the write token stops the writes of its chain, the reads going on, until the chain is restarted. It will perform a recovery with quorum.
- **Internet Scale Distribution**. Will use Web Sockets.
- **Transparent SQL connectors**, to fill an SQL server with every transaction done using GBO. EF entities included.
