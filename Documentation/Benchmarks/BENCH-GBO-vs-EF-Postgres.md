# A shop: GBO against EF Core and PostgreSQL

**Date:** 2026-10-27. **Machine:** Intel i9-9900K, 16 logical processors, Windows 10 22H2 (19045), .NET 10.0.12, workstation GC. **GBO:** a persistent Repository Store on C:, a WDC PC SN730 NVMe SSD. **EF Core:** Microsoft.EntityFrameworkCore 10.0.9, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1, Npgsql 10.0.2. **PostgreSQL:** 18.0, x86_64-windows, on the same machine, its default settings: `fsync` on, `wal_sync_method` open_datasync, `shared_buffers` 128 MB, `wal_writer_delay` 200 ms.

## The shop

200 products, 100,000 orders of 20 lines each: 2,000,000 lines. An order has at most one line per product, so the number of orders of a product is the number of its lines.

- **GBO:** `Product` and `Order` are Roots `[WithIdentity]`; an `OrderLine` is a part of its order, and designates its product by a `[Strong]` reference, whose reverse property `Product.Lines` gives the lines of a product. The Store is alone, the only writer of the process (its lock policy `Retain`); each other setting is at its default, the compactor included.
- **EF Core:** three tables, the keys of the orders and the lines a bigint identity, the foreign keys indexed. A pooled DbContext per transaction, `AsNoTracking` for the reads, `Include` for the graph, the lazy loading by `ILazyLoader`, without proxies, in a model of its own. Npgsql prepares the statements it executes often (`Max Auto Prepare` 64), and the read by key is compiled (`EF.CompileQuery`). The reads open no transaction of their own: each query sees its own snapshot. Each load ends with a `VACUUM ANALYZE`.

**The same data and the same work.** From the same seeds, both engines build the same shop, read the same orders, and change them in the same way. Each read step computes a check from its first transaction: the checks of the two engines are equal, step by step, such as 33,398 orders of a status, a total of 562,150,423.07 for every order, 2,000,000 orders and the same revenue for the products. The reads run on the last load, before its changes; the changes and the concurrent step follow.

**The durability of the writes.**

| Mode | GBO | PostgreSQL |
|---|---|---|
| Durable at each commit | `FlushPolicy.DurableOnDisk`: the commit returns once the transaction survives a power loss | `synchronous_commit = on`: `COMMIT` returns once the WAL is flushed |
| Durable within a delay | `FlushPolicy.FastOnDisk`: flushed within 1 ms or 8 MB, each flush followed by `FlushFileBuffers` | `synchronous_commit = off`: the WAL writer flushes within 3 × `wal_writer_delay`, 600 ms |

One thread, 30 s per timed step; each load inserts the 100,000 orders into an empty Store, or a database created again.

## The results

| Step                                                                                                     | GBO                 | EF Core + PostgreSQL | GBO / EF |
| -------------------------------------------------------------------------------------------------------- | ------------------- | -------------------- | -------- |
| Insert 100 orders per transaction, durable at each commit                                                | 21,496 orders/s     | 878 orders/s         | ×24.5    |
| Insert 10,000 orders per transaction, durable at each commit                                             | 52,784 orders/s     | 882 orders/s         | ×59.8    |
| Insert 100 orders per transaction, durable within a delay                                                | 32,550 orders/s     | 933 orders/s         | ×34.9    |
| Insert 10,000 orders per transaction, durable within a delay                                             | 49,273 orders/s     | 896 orders/s         | ×55.0    |
| Change 1 order per transaction (a line removed, a line added, the order changed), durable at each commit | 1,753 orders/s      | 1,112 orders/s       | ×1.6     |
| Change 10 orders per transaction, durable at each commit                                                 | 10,642 orders/s     | 3,871 orders/s       | ×2.7     |
| Change 1 order per transaction, durable within a delay                                                   | 16,400 orders/s     | 1,250 orders/s       | ×13.1    |
| Change 10 orders per transaction, durable within a delay                                                 | 15,548 orders/s     | 4,045 orders/s       | ×3.8     |
| Count the orders of a status (`Order.Where`)                                                             | 55,467,198 orders/s | 18,235,132 orders/s  | ×3.0     |
| The total of each order: its lines and the price of their products                                       | 12,574,367 lines/s  | 2,353,104 lines/s    | ×5.3     |
| The same, EF: a `GROUP BY` of the lines                                                                  | 12,574,367 lines/s  | 2,083,536 lines/s    | ×6.0     |
| The orders and the sales of each product, by the reverse                                                 | 5,298,306 lines/s   | 1,508,254 lines/s    | ×3.5     |
| The same, EF: a `GROUP BY` of the lines                                                                  | 5,298,306 lines/s   | 1,197,182 lines/s    | ×4.4     |
| Read 100 random orders per transaction, one by one                                                       | 3,205,427 orders/s  | 10,299 orders/s      | ×311.2   |
| The same, EF: one query for the 100                                                                      | 3,205,427 orders/s  | 304,814 orders/s     | ×10.5    |
| Load and read 1,000 random orders, their lines and products, per transaction                             | 271,859 orders/s    | 12,421 orders/s      | ×21.9    |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product                                 | 1,364,387 objects/s | 5,271 objects/s      | ×258.8   |

### Concurrent: one writer, 1 to 8 readers

One thread changes 1 order per transaction, durable within a delay, while 1 to 8 threads compute the totals of every order, each in a loop, 30 s per number of readers. A total of every order reads the 2,000,000 lines.

| Readers | GBO: changes, orders/s | EF: changes, orders/s | GBO / EF | GBO: totals of every order /s | EF: totals of every order /s | GBO / EF |
|---|---|---|---|---|---|---|
| 1 | 9,777 | 1,205 | ×8.1 | 3.0 | 0.9 | ×3.5 |
| 2 | 13,488 | 1,124 | ×12.0 | 5.8 | 1.6 | ×3.7 |
| 4 | 11,918 | 1,045 | ×11.4 | 11.0 | 2.9 | ×3.9 |
| 8 | 10,686 | 867 | ×12.3 | 19.8 | 4.6 | ×4.3 |

GBO reads 39.6 M lines/s with 8 readers, and EF Core with PostgreSQL 9.2 M lines/s.

## What the figures say

- **A round trip per object is where EF Core and PostgreSQL lose the most.** Reading orders one by one, and the lazy loading, about 870 queries per transaction of 1,000 objects, give GBO its largest leads: ×311.2 and ×258.8. One query for the 100 orders brings the lead to ×10.5.
- **The aggregations run in PostgreSQL, and GBO is 3.5 to 6 times faster.** The `GROUP BY` of the lines is slower than the subqueries that EF Core writes from the navigations.
- **EF Core inserts about 19,000 rows/s, whatever the number of orders per transaction.** It sends one `INSERT ... RETURNING` per row, and allocates 2.2 GB for one transaction of 10,000 orders; about 80 % of `SaveChanges` is the execution of these statements by PostgreSQL. `AddRange`, and the change tracking turned off, change nothing. A set-based insertion by Npgsql alone, one `INSERT ... SELECT unnest(...)` per table, is 2.3 times as fast, but EF Core does not write it.
- **Durable at each commit, a single change waits for the disk on both sides:** GBO leads by ×1.6 for one order per transaction. Within a delay, GBO no longer waits, and leads by ×13.1.
- **With concurrent readers, GBO keeps its writer at 9,777 to 13,488 changes/s, and each reader at 2.5 to 3.0 totals/s.** EF Core keeps its writer at 867 to 1,205 changes/s, and each reader at 0.6 to 0.9 totals/s.

## The details

### GBO

| Step | transactions | items | duration | transactions/s | items/s | allocated per transaction |
|---|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 4.7 s | 215 | 21,496 | 1,489,459 B |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 1.9 s | 5.3 | 52,784 | 56,891,671 B |
| Change 1 order per transaction, durable at each commit | 52,598 | 52,598 orders | 30.0 s | 1,753 | 1,753 | 22,432 B |
| Change 10 orders per transaction, durable at each commit | 31,926 | 319,260 orders | 30.0 s | 1,064 | 10,642 | 250,586 B |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 3.1 s | 326 | 32,550 | 1,484,955 B |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 2.0 s | 5.0 | 49,273 | 56,895,002 B |
| Count the orders of a status | 16,641 | 1,664,100,000 orders | 30.0 s | 555 | 55,467,198 | 1,057 B |
| The total of each order | 189 | 378,000,000 lines | 30.1 s | 6.3 | 12,574,367 | 8,389,972 B |
| The orders and the sales of each product | 80 | 160,000,000 lines | 30.2 s | 2.6 | 5,298,306 | 17,525 B |
| Read 100 random orders per transaction | 961,629 | 96,162,900 orders | 30.0 s | 32,054 | 3,205,427 | 14,580 B |
| Load and read 1,000 random orders, their lines and products | 8,156 | 8,156,000 orders | 30.0 s | 272 | 271,859 | 2,304,501 B |
| Lazy loading, 1,000 objects per transaction | 40,850 | 40,931,700 objects | 30.0 s | 1,362 | 1,364,387 | 623,010 B |
| Change 1 order per transaction, durable within a delay | 491,998 | 491,998 orders | 30.0 s | 16,400 | 16,400 | 26,423 B |
| Change 10 orders per transaction, durable within a delay | 46,645 | 466,450 orders | 30.0 s | 1,555 | 15,548 | 244,799 B |

### EF Core + PostgreSQL

| Step | transactions | items | duration | transactions/s | items/s | allocated per transaction |
|---|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 113.9 s | 8.8 | 878 | 22,826,462 B |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 113.3 s | 0.09 | 882 | 2,247,537,631 B |
| Change 1 order per transaction, durable at each commit | 33,361 | 33,361 orders | 30.0 s | 1,112 | 1,112 | 69,768 B |
| Change 10 orders per transaction, durable at each commit | 11,613 | 116,130 orders | 30.0 s | 387 | 3,871 | 591,647 B |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 107.1 s | 9.3 | 933 | 22,814,940 B |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 111.6 s | 0.09 | 896 | 2,247,537,727 B |
| Count the orders of a status | 5,471 | 547,100,000 orders | 30.0 s | 182 | 18,235,132 | 5,053 B |
| The total of each order | 36 | 72,000,000 lines | 30.6 s | 1.2 | 2,353,104 | 28,705,302 B |
| The total of each order, a `GROUP BY` | 32 | 64,000,000 lines | 30.7 s | 1.0 | 2,083,536 | 28,514,634 B |
| The orders and the sales of each product | 23 | 46,000,000 lines | 30.5 s | 0.75 | 1,508,254 | 81,741 B |
| The orders and the sales of each product, a `GROUP BY` | 18 | 36,000,000 lines | 30.1 s | 0.60 | 1,197,182 | 85,780 B |
| Read 100 random orders per transaction, one by one | 3,090 | 309,000 orders | 30.0 s | 103 | 10,299 | 222,498 B |
| Read 100 random orders per transaction, one query | 91,445 | 9,144,500 orders | 30.0 s | 3,048 | 304,814 | 41,730 B |
| Load and read 1,000 random orders, their lines and products | 373 | 373,000 orders | 30.0 s | 12 | 12,421 | 22,616,572 B |
| Lazy loading, 1,000 objects per transaction | 158 | 158,316 objects | 30.0 s | 5.3 | 5,271 | 22,821,434 B |
| Change 1 order per transaction, durable within a delay | 37,497 | 37,497 orders | 30.0 s | 1,250 | 1,250 | 69,745 B |
| Change 10 orders per transaction, durable within a delay | 12,136 | 121,360 orders | 30.0 s | 405 | 4,045 | 592,039 B |

## Run it

```bash
dotnet run -c Release --project GhostBodyObject.Benchmarks.EntityFramework -- all 30 100000
```

The arguments: `gbo`, `ef` or `all`; the seconds of each timed step; the number of orders. The connection to PostgreSQL, its database aside, comes from the variable `GBO_POSTGRES`; without it, `Host=localhost;Port=5432;Username=postgres`, whose password Npgsql reads in `%APPDATA%\postgresql\pgpass.conf`. The role creates and drops the database `gbo_ef_benchmark` at each load. The GBO Stores live under `C:\gbo-work\ef-bench`, deleted at the end.
