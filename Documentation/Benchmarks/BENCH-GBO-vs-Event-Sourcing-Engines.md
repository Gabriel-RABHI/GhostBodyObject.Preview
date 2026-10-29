# The shop in Event Sourcing: GBO against KurrentDB and Marten

The same shop on four engines, the same commands in the same order: GBO in Mutation Sourcing, GBO in Event Sourcing, KurrentDB, and Marten on PostgreSQL.

**Date:** 2026-10-28. **Machine:** Intel Core i9-9900K at 3.6 GHz, 8 cores, 64 GB, an NVMe WD SN730 of 1 TB (C:), Windows 10 Pro 19045; every engine and its server on it, natively, on the SSD C:, over the loopback. **Runtime:** .NET 10 (SDK 10.0.401).

## Mutation Sourcing

GBO implements a new pattern of Event Sourcing, **Mutation Sourcing**. A Mutation is a single work record: the command (its intention) and its event (its decision), stored together in the Log. A pump decides and applies the Mutations one at a time, in the order received, so that it is the only writer: no conflict, and the effects of a Mutation are written in the models in its transaction. GBO can also implement classic Event Sourcing, with persistent projections, the one that decides the commands included.

## The stream

- The catalog, 10,000 customers and 2,000 products, then the mix.
- 2,012,000 commands in a file, played by every engine from its start.
- The mix: PlaceOrder 50 %, ShipOrder 20 %, ArchiveOrder 12 %, CancelOrder 8 %, Restock 6 %, RegisterCustomer 1.5 %, ListProduct 1 %, DelistProduct 1 %, CloseAccount 0.5 %, refused while the customer has orders.
- An order has 1 to 8 lines for 80 % of the orders, 9 to 40 for the others, its products drawn by a law of Zipf.

**Each run, on an empty storage:**

1. the catalog, then the mix: 10 s of warm-up, then 60 s measured;
2. the threads go on, out of the measure, to the next checkpoint;
3. the Read model of the engine is compared with the expected one after the catalog, after the mix and after the restart: every count and every sum of the accounts, the summaries, the sales and the buyers;
4. every run passed its three checks.

**The threads:** 1 or 8, each one the commands of its aggregates, in their order. A thread posts a command and waits for its outcome; the latency of a command is the time from its call to its outcome.

## The engines

| Engine | Durability | The aggregates | The Read model | A conflict |
|---|---|---|---|---|
| **GBO, Mutation Sourcing** | `FastOnDisk`, a flush to the disk within 1 ms; or `DurableOnDisk`, a flush at each commit | The Write model in memory; a command is a Mutation, decided and applied by the pump of the base, in batches of up to 256 Mutations or 1 ms | In memory, in the transaction of the Mutation | None: the pump is the only writer |
| **GBO, Event Sourcing** | `FastOnDisk` | The Write model in memory; a transaction of the chain per command, its Events in the Event Log | In memory, in the transaction of the command | `TryCommit` gives it back; the command runs again |
| **KurrentDB 26.2.3** | Its default, a flush at each write | A stream per aggregate, cached in the process; a command appended by `AppendRecords`, atomic over its streams, the revision of each stream it read checked | In memory, by a catch-up subscription to `$all` | An exception of the client; its streams caught up, the command runs again |
| **Marten 9.54.2, PostgreSQL 18** | `synchronous_commit = off`, `wal_writer_delay = 1ms`: durable within about 3 ms | A stream per aggregate, its snapshot projected inline; locked by `FetchForExclusiveWriting`, the customer then the products in the order of their keys; Quick appends | Documents projected inline, in the transaction of the command | None: the locks wait |

The settings of the installation for the rest: PostgreSQL with its 128 MB of shared buffers; KurrentDB and GBO with their defaults.

What each engine writes for a command:
- **KurrentDB and Marten** append an event to each stream whose aggregate the command changes. A `PlaceOrder` writes its order, its customer and each of its products.
- **GBO** writes one Mutation in Mutation Sourcing, and the Events of the command in Event Sourcing.

## The results

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

How to read the columns:
- **Mix:** the commands done in the 60 s measured; the refused ones count, 0 to 0.5 % of them, more as the mix goes on.
- **Conflicts:** the executions again of a command whose engine met a concurrent one.
- **Latency:** in the pipelined row, a thread posts without waiting, so the latency of a post means nothing.
- **On the disk**, what each engine wrote for its commands of the mix:
  - GBO: its Log;
  - KurrentDB: its log, by the positions of `$all`, its chunks being files of 256 MB allocated at once;
  - Marten: the WAL of PostgreSQL.
- **Commands played:** the catalog and the mix, to the checkpoint after the measure; the Read model of each engine holds them, and its restart rebuilds them.
- **Restart:** the application stopped and started again, until its first command can run.
  - GBO replays its Log into both models, Write and Read.
  - KurrentDB rebuilds its Read model in memory from the first event; its aggregates load again at their first command, out of this time.
  - Marten has nothing to rebuild: its snapshots and its Read model are tables.
- **Replayed:** the commands played divided by the time of the restart.
- **Working set:** the process of the benchmark and its servers, at the end of the mix, for the commands played. For PostgreSQL, the private bytes of its processes and its shared buffers once.

## What the figures say

- **The commands of the mix, the same durability within 1 to 3 ms:** GBO in Mutation Sourcing runs 7,083 commands/s on one thread against 178 for Marten, ×39.8; 8,728 against 273 on 8 threads, ×32.0.
- **Durable at each write, against KurrentDB:** GBO durable runs 1,745 against 754 commands/s on one thread, ×2.3; 5,170 against 1,014 on 8, ×5.1.
- **The pipelined pump:** 17,729 commands/s on one thread, 2.5 times the same pump waited one by one.
- **The latency of one command, on one thread:**
  - GBO: 28 µs at the median in Event Sourcing, 42 µs in Mutation Sourcing, 534 µs durable at each commit;
  - KurrentDB: 1.2 ms;
  - Marten: 4.6 ms.
- **The conflicts make the 8 threads:**
  - Most orders share the most popular products with the orders in flight beside them: about one order in two holds the most popular one.
  - KurrentDB executes a command 3.3 times again on average; its p99 rises to 126 ms.
  - GBO in Event Sourcing executes 0.78 times again per command: 6,508 commands/s on 8 threads, against 8,298 on one.
  - In Mutation Sourcing, the pump is the only writer: no conflict, 8,728 commands/s on 8 threads.
  - Marten waits on its locks instead: 273 commands/s, p99 125 ms.
- **The restart:** GBO replays 47,000 to 66,000 commands per second into its two models; KurrentDB 5,200 to 5,400 per second, into its Read model only.
- **The disk per command:**
  - GBO: 182 to 271 B for a Mutation, its intention and its decision once; 424 to 428 B for the Events of a command;
  - KurrentDB: 2,371 B, an event in each stream of the command, in JSON;
  - Marten: 13.1 to 13.5 KB of WAL, its events, its snapshots and its documents rewritten.
- **The memory:** the working sets hold different numbers of commands, from 25,000 to 1,269,000. GBO holds both models in memory: 1,036 MB for 520,000 commands.

## Limits

- **One machine,** every server on the loopback: the costs of a network between the application and KurrentDB or PostgreSQL are those of the loopback only. GBO runs in the process of the application.
- **Exceptions:** KurrentDB and Marten give a conflict by an exception only, which their runners catch to execute the command again; its cost counts in their figures.
- **One run each,** 60 s each, not repeated: the variation between runs is not measured here.

## Run it

```bash
dotnet run -c Release --project GhostBodyObject.EventSourcing.Benchmarks -- run <engine> <stream> <threads> <seconds> <warm-up seconds>
```

The engines: `gbo-ms`, `gbo-ms-durable`, `gbo-ms-pipelined`, `gbo-es`, `kurrent` and `marten`. The stream is written once by `generate <stream> 2000000`. The Marten engine sets `synchronous_commit` and `wal_writer_delay` by `ALTER SYSTEM`, and resets them at its end.
