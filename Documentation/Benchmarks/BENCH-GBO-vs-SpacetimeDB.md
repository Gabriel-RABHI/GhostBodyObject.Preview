# A shop: the engine of GBO against the engine of SpacetimeDB

**Date:** 2026-11-01. **Machine:** Intel i9-9900K, 16 logical processors, Windows 10 22H2 (19045), .NET 10.0.12, workstation GC, a WDC PC SN730 NVMe SSD. **GBO:** a persistent Repository Store on C:. **SpacetimeDB:** 2.14.0 (7c3ee55), its standalone server for Windows, on the same machine and the same disk, its default settings; the module in Rust 1.99.1, built by `cargo` for `wasm32-unknown-unknown`, `opt-level = 3`, fat LTO, without `wasm-opt`.

## What is measured

The engines, without a network. GBO runs inside the process of the application. SpacetimeDB is a server: the code closest to its engine is a module, which runs in it, in WebAssembly. Each scenario is therefore written in the module, in the loops of the scenario of GBO, and opens its transactions from there. The client only starts a procedure and reads what it did. A network, a client SDK and subscriptions, which SpacetimeDB was built for, are not measured here.

The shop is that of the benchmark against EF Core and PostgreSQL: 200 products, 100,000 orders of 20 lines each, 2,000,000 lines. An order has at most one line per product, so the number of orders of a product is the number of its lines.

- **GBO:** `Product` and `Order` are Roots `[WithIdentity]`; an `OrderLine` is a part of its order, and designates its product by a `[Strong]` reference, whose reverse property `Product.Lines` gives the lines of a product. The Store is alone, the only writer of the process (its lock policy `Retain`); each other setting is at its default, the compactor included.
- **SpacetimeDB:** three tables, a key per row, the foreign keys indexed by a B-tree. A price is an integer number of cents, as SpacetimeDB has no decimal. The module is in Rust, its fastest language: a module in C# also runs in WebAssembly, with its runtime. The scenarios are procedures, the only functions of a module that commit several transactions; each iteration is one `with_tx`.

**The same data and the same work.** The module holds a port of `System.Random`: from the same seeds, both engines build the same shop, read the same orders, and change them in the same way. Each read step computes a check from its first transaction: the checks of the two engines are equal, step by step: 33,398 orders of a status, a total of 562,150,423.07 for every order, 2,000,000 orders and the same revenue for the products, 4,949,184 for the random orders, 5,838,903.67 for the graph. The reads run on the last load, before its changes; the changes and the concurrent step follow.

**The durability of the writes.**

| Mode | GBO | SpacetimeDB |
|---|---|---|
| Durable at each commit | `FlushPolicy.DurableOnDisk`: the commit returns once the transaction survives a power loss | A procedure called with `confirmed=true`: `with_tx` returns once the commit log reached the disk |
| Durable within a delay | `FlushPolicy.FastOnDisk`: flushed within 1 ms or 8 MB, each flush followed by `FlushFileBuffers` | A procedure called with `confirmed=false`: `with_tx` returns at the commit; the commit log, which syncs in a loop, follows it |

One thread, 30 s per timed step; each load inserts the 100,000 orders into an empty Store, or a database cleared. A call of a procedure has a budget of fuel, which a loop of 30 s in WebAssembly exhausts: a timed step of SpacetimeDB is a series of calls of 1 s, whose round trip costs 0.1 %. The GBO and SpacetimeDB rows come from one run; no other process averaged more than half a logical processor over any minute of it.

Each operation of a module on a table crosses the boundary of WebAssembly and decodes its row: this cost is part of the only way to reach the engine, and the rows of SpacetimeDB include it. The count has a variant in the host, a SQL query, timed by the server, the only read that crosses no WebAssembly. A SQL query of SpacetimeDB has no `SUM` and no `GROUP BY`, so the aggregations stay in the module, where they also have a variant: one pass on the lines, with the prices in an array.

## The results

| Step | GBO | SpacetimeDB | GBO / SpacetimeDB |
|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 19,956 orders/s | 13,720 orders/s | ×1.5 |
| Insert 10,000 orders per transaction, durable at each commit | 53,819 orders/s | 18,250 orders/s | ×2.9 |
| Insert 100 orders per transaction, durable within a delay | 29,556 orders/s | 18,405 orders/s | ×1.6 |
| Insert 10,000 orders per transaction, durable within a delay | 51,865 orders/s | 18,223 orders/s | ×2.8 |
| Change 1 order per transaction, durable at each commit | 1,710 orders/s | 1,719 orders/s | ×1.0 |
| Change 10 orders per transaction, durable at each commit | 10,924 orders/s | 13,471 orders/s | ×0.8 |
| Change 1 order per transaction, durable within a delay | 14,151 orders/s | 45,134 orders/s | ×0.3 |
| Change 10 orders per transaction, durable within a delay | 9,911 orders/s | 79,866 orders/s | ×0.1 |
| Count the orders of a status (`Order.Where`) | 54,056,116 orders/s | 40,927,985 orders/s | ×1.3 |
| The same, SpacetimeDB: a SQL query, in the host | 54,056,116 orders/s | 22,768,669 orders/s | ×2.4 |
| The total of each order: its lines and the price of their products | 15,001,631 lines/s | 2,479,275 lines/s | ×6.1 |
| The same, SpacetimeDB: one pass on the lines | 15,001,631 lines/s | 27,541,691 lines/s | ×0.5 |
| The orders and the sales of each product: its lines, by the reverse | 4,937,320 lines/s | 9,120,580 lines/s | ×0.5 |
| The same, SpacetimeDB: one pass on the lines | 4,937,320 lines/s | 27,692,171 lines/s | ×0.2 |
| Read 100 random orders per transaction | 2,972,259 orders/s | 1,764,483 orders/s | ×1.7 |
| Load and read 1,000 random orders, their lines and products, per transaction | 268,003 orders/s | 103,926 orders/s | ×2.6 |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product | 1,339,748 objects/s | 1,119,899 objects/s | ×1.2 |

### Concurrent: one writer, 1 to 8 readers

One thread changes 1 order per transaction, durable within a delay, while 1 to 8 threads compute the totals of every order, each in a loop, 30 s per number of readers. A total of every order reads the 2,000,000 lines. SpacetimeDB computes them in one pass on the lines, its fastest way. Each of its threads works by calls of 100 ms: a module has a bounded pool of instances, and calls of a whole step would run in turn.

| Readers | GBO: changes, orders/s | SpacetimeDB: changes, orders/s | GBO / SpacetimeDB | GBO: totals of every order /s | SpacetimeDB: totals of every order /s | GBO / SpacetimeDB |
|---|---|---|---|---|---|---|
| 1 | 14,918 | 12,676 | ×1.2 | 3.5 | 8.8 | ×0.4 |
| 2 | 14,677 | 3,291 | ×4.5 | 6.1 | 11.3 | ×0.5 |
| 4 | 12,856 | 545 | ×23.6 | 11.8 | 12.1 | ×1.0 |
| 8 | 11,802 | 752 | ×15.7 | 20.9 | 12.1 | ×1.7 |

GBO reads 41.8 M lines/s with 8 readers, and SpacetimeDB 24.3 M lines/s.

## What the figures say

- **Within a delay, SpacetimeDB changes orders 3.2 to 8.1 times faster:** 45,134 orders/s and 79,866 orders/s against 14,151 and 9,911. This is its largest lead over GBO.
- **Durable at each commit, a change waits for the disk on both sides:** 1,710 orders/s for GBO, 1,719 for SpacetimeDB, the limit of this disk, about 1,900 synchronous writes/s. For 10 orders per transaction, SpacetimeDB leads by ×1.2.
- **GBO inserts 1.5 to 2.9 times faster.** SpacetimeDB inserts about 18,000 orders/s, 378,000 rows/s, nearly whatever the size of the transaction.
- **By the keys and the references, GBO leads:** ×1.7 for random orders, ×2.6 for the graph, ×1.2 for the walks. For the totals of the orders, where a walk makes one lookup per line, the lead is ×6.1.
- **In one pass on its lines, SpacetimeDB aggregates faster:** 27.5 M lines/s against the 15.0 M lines/s of GBO, which walks the lines of each order, and 27.7 M lines/s for the sales, against 4.9 M lines/s by the reverse. By its index on the products, it reaches 9.1 M lines/s.
- **A count is faster in the module than in the host of SpacetimeDB,** 40.9 M orders/s against 22.8 M; GBO counts 54.1 M orders/s.
- **Concurrent reads are where the engines differ the most.** A module has no read transaction: each `with_tx` is an exclusive write transaction. With 4 and 8 readers, SpacetimeDB computes at most 12.1 totals/s, and its writer falls from 12,676 to 545 and 752 changes/s. GBO keeps its writer at 11,802 to 14,918 changes/s, and its readers add up, from 3.5 to 20.9 totals/s. With 1 and 2 readers, SpacetimeDB computes more totals: 8.8 and 11.3 against 3.5 and 6.1.

**On Linux,** the server of SpacetimeDB under WSL 2 on the same machine, measured on 20,000 orders, gives memory rows within about 30 % of the native server, both ways, and durable changes 2.7 to 2.9 times slower: a synchronous write of the virtual disk runs at 700/s, against 1,881/s on C:. **A client over WebSocket** does not reach these rates: on the Windows server, a call by WebSocket takes about 15.6 ms, against 0.8 ms by HTTP, and 1.4 ms by WebSocket on Linux.

## The details

### GBO

| Step | transactions | items | duration | transactions/s | items/s | allocated per transaction |
|---|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 5.0 s | 200 | 19,956 | 1,387,016 B |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 1.9 s | 5 | 53,819 | 56,901,212 B |
| Change 1 order per transaction, durable at each commit | 51,301 | 51,301 orders | 30.0 s | 1,710 | 1,710 | 20,516 B |
| Change 10 orders per transaction, durable at each commit | 32,772 | 327,720 orders | 30.0 s | 1,092 | 10,924 | 227,728 B |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 3.4 s | 296 | 29,556 | 1,388,437 B |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 1.9 s | 5 | 51,865 | 56,901,171 B |
| Count the orders of a status (`Order.Where`) | 16,217 | 1,621,700,000 orders | 30.0 s | 541 | 54,056,116 | 1,040 B |
| The total of each order: its lines and the price of their products | 226 | 452,000,000 lines | 30.1 s | 8 | 15,001,631 | 8,389,953 B |
| The orders and the sales of each product: its lines, by the reverse | 75 | 150,000,000 lines | 30.4 s | 2 | 4,937,320 | 17,510 B |
| Read 100 random orders per transaction | 891,678 | 89,167,800 orders | 30.0 s | 29,723 | 2,972,259 | 14,564 B |
| Load and read 1,000 random orders, their lines and products, per transaction | 8,041 | 8,041,000 orders | 30.0 s | 268 | 268,003 | 2,304,510 B |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product | 40,113 | 40,193,226 objects | 30.0 s | 1,337 | 1,339,748 | 623,322 B |
| Change 1 order per transaction, durable within a delay | 424,544 | 424,544 orders | 30.0 s | 14,151 | 14,151 | 24,621 B |
| Change 10 orders per transaction, durable within a delay | 29,733 | 297,330 orders | 30.0 s | 991 | 9,911 | 220,933 B |
| Readers | changes | changes, orders/s | totals of every order | totals /s | totals /s per reader | lines aggregated /s |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 449,429 | 14,918 | 104 | 3.5 | 3.5 | 6,904,143 |
| 2 | 444,035 | 14,677 | 186 | 6.1 | 3.1 | 12,296,167 |
| 4 | 388,419 | 12,856 | 355 | 11.8 | 2.9 | 23,500,591 |
| 8 | 356,844 | 11,802 | 632 | 20.9 | 2.6 | 41,804,375 |

### SpacetimeDB

The module does not report what it allocates. The count in the host sums the durations reported by the server.

| Step | transactions | items | duration | transactions/s | items/s |
|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 7.3 s | 137 | 13,720 |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 5.5 s | 2 | 18,250 |
| Change 1 order per transaction, durable at each commit | 51,664 | 51,664 orders | 30.1 s | 1,719 | 1,719 |
| Change 10 orders per transaction, durable at each commit | 40,490 | 404,900 orders | 30.1 s | 1,347 | 13,471 |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 5.4 s | 184 | 18,405 |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 5.5 s | 2 | 18,223 |
| Count the orders of a status (`Order.Where`) | 12,336 | 1,233,600,000 orders | 30.1 s | 409 | 40,927,985 |
| Count the orders of a status (`Order.Where`), SpacetimeDB: a SQL query, in the host | 5,930 | 593,000,000 orders | 26.0 s | 228 | 22,768,669 |
| The total of each order: its lines and the price of their products | 39 | 78,000,000 lines | 31.5 s | 1 | 2,479,275 |
| The total of each order: its lines and the price of their products, SpacetimeDB: one pass on the lines | 428 | 856,000,000 lines | 31.1 s | 14 | 27,541,691 |
| The orders and the sales of each product: its lines, by the reverse | 138 | 276,000,000 lines | 30.3 s | 5 | 9,120,580 |
| The orders and the sales of each product: its lines, by the reverse, SpacetimeDB: one pass on the lines | 429 | 858,000,000 lines | 31.0 s | 14 | 27,692,171 |
| Read 100 random orders per transaction | 529,899 | 52,989,900 orders | 30.0 s | 17,645 | 1,764,483 |
| Load and read 1,000 random orders, their lines and products, per transaction | 3,167 | 3,167,000 orders | 30.5 s | 104 | 103,926 |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product | 33,608 | 33,675,216 objects | 30.1 s | 1,118 | 1,119,899 |
| Change 1 order per transaction, durable within a delay | 1,356,886 | 1,356,886 orders | 30.1 s | 45,134 | 45,134 |
| Change 10 orders per transaction, durable within a delay | 239,820 | 2,398,200 orders | 30.0 s | 7,987 | 79,866 |
| Readers | changes | changes, orders/s | totals of every order | totals /s | totals /s per reader |
| --- | --- | --- | --- | --- | --- |
| 1 | 383,474 | 12,676 | 267 | 8.8 | 8.8 |
| 2 | 100,535 | 3,291 | 345 | 11.3 | 5.6 |
| 4 | 16,548 | 545 | 366 | 12.1 | 3.0 |
| 8 | 23,046 | 752 | 372 | 12.1 | 1.5 |

## Run it

The server runs apart, as for PostgreSQL:

```bash
spacetime start --listen-addr 127.0.0.1:3000
```

The module builds once, with Rust and its target `wasm32-unknown-unknown`, then the benchmark publishes it:

```bash
cargo build --release --target wasm32-unknown-unknown --manifest-path GhostBodyObject.Benchmarks.SpacetimeDB/module/Cargo.toml
```

```bash
dotnet run -c Release --project GhostBodyObject.Benchmarks.SpacetimeDB -- all 30 100000
```

The arguments: `gbo`, `stdb` or `all`; the seconds of each timed step; the number of orders. The address of the server comes from the variable `GBO_SPACETIMEDB`, by default `http://127.0.0.1:3000`, and the path of the compiled module from `GBO_SPACETIMEDB_MODULE`, by default the output of `cargo`. The benchmark takes an identity, creates and clears a database `gbo-shop-benchmark-<process>`, and deletes it at the end. The GBO Stores live under `C:\gbo-work\ef-bench`, deleted at the end. The run takes 16 minutes.
