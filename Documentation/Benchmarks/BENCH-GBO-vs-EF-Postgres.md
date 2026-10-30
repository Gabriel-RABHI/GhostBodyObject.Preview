# A shop: GBO against EF Core and PostgreSQL

**Date:** 2026-10-30. **Machine:** Intel i9-9900K, 16 logical processors, Windows 10 22H2 (19045), .NET 10.0.12, workstation GC. **GBO:** a persistent Repository Store on C:, a WDC PC SN730 NVMe SSD. **EF Core:** Microsoft.EntityFrameworkCore 10.0.9, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1, Npgsql 10.0.2. **PostgreSQL:** 18.0, x86_64-windows, on the same machine, its default settings: `fsync` on, `wal_sync_method` open_datasync, `shared_buffers` 128 MB, `wal_writer_delay` 200 ms.

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
| Insert 100 orders per transaction, durable at each commit                                                | 21,241 orders/s     | 900 orders/s         | ×23.6    |
| Insert 10,000 orders per transaction, durable at each commit                                             | 48,444 orders/s     | 912 orders/s         | ×53.1    |
| Insert 100 orders per transaction, durable within a delay                                                | 32,514 orders/s     | 940 orders/s         | ×34.6    |
| Insert 10,000 orders per transaction, durable within a delay                                             | 43,465 orders/s     | 914 orders/s         | ×47.5    |
| Change 1 order per transaction (a line removed, a line added, the order changed), durable at each commit | 1,729 orders/s      | 1,080 orders/s       | ×1.6     |
| Change 10 orders per transaction, durable at each commit                                                 | 10,851 orders/s     | 3,894 orders/s       | ×2.8     |
| Change 1 order per transaction, durable within a delay                                                   | 16,301 orders/s     | 1,214 orders/s       | ×13.4    |
| Change 10 orders per transaction, durable within a delay                                                 | 16,212 orders/s     | 4,085 orders/s       | ×4.0     |
| Count the orders of a status (`Order.Where`)                                                             | 57,013,136 orders/s | 18,315,018 orders/s  | ×3.1     |
| The total of each order: its lines and the price of their products                                       | 14,992,838 lines/s  | 2,449,743 lines/s    | ×6.1     |
| The same, EF: a `GROUP BY` of the lines                                                                  | 14,992,838 lines/s  | 2,138,080 lines/s    | ×7.0     |
| The orders and the sales of each product, by the reverse                                                 | 5,374,622 lines/s   | 1,616,327 lines/s    | ×3.3     |
| The same, EF: a `GROUP BY` of the lines                                                                  | 5,374,622 lines/s   | 1,265,444 lines/s    | ×4.2     |
| Read 100 random orders per transaction, one by one                                                       | 3,397,945 orders/s  | 10,152 orders/s      | ×334.7   |
| The same, EF: one query for the 100                                                                      | 3,397,945 orders/s  | 303,460 orders/s     | ×11.2    |
| Load and read 1,000 random orders, their lines and products, per transaction                             | 280,773 orders/s    | 12,435 orders/s      | ×22.6    |
| Lazy loading: 1,000 objects per transaction, Order → OrderLine → Product                                 | 1,387,854 objects/s | 5,238 objects/s      | ×265.0   |

### Concurrent: one writer, 1 to 8 readers

One thread changes 1 order per transaction, durable within a delay, while 1 to 8 threads compute the totals of every order, each in a loop, 30 s per number of readers. A total of every order reads the 2,000,000 lines.

| Readers | GBO: changes, orders/s | EF: changes, orders/s | GBO / EF | GBO: totals of every order /s | EF: totals of every order /s | GBO / EF |
|---|---|---|---|---|---|---|
| 1 | 11,576 | 1,190 | ×9.7 | 3.4 | 0.9 | ×3.8 |
| 2 | 13,564 | 1,155 | ×11.7 | 6.3 | 1.6 | ×3.9 |
| 4 | 13,474 | 1,077 | ×12.5 | 11.6 | 3.0 | ×3.9 |
| 8 | 10,765 | 951 | ×11.3 | 20.5 | 5.0 | ×4.1 |

GBO reads 41.0 M lines/s with 8 readers, and EF Core with PostgreSQL 10.0 M lines/s.

## What the figures say

- **A round trip per object is where EF Core and PostgreSQL lose the most.** Reading orders one by one, and the lazy loading, about 870 queries per transaction of 1,000 objects, give GBO its largest leads: ×334.7 and ×265.0. One query for the 100 orders brings the lead to ×11.2.
- **The aggregations run in PostgreSQL, and GBO is 3.3 to 7 times faster.** The `GROUP BY` of the lines is slower than the subqueries that EF Core writes from the navigations.
- **EF Core inserts about 19,000 rows/s, whatever the number of orders per transaction.** It sends one `INSERT ... RETURNING` per row, and allocates 2.2 GB for one transaction of 10,000 orders; about 80 % of `SaveChanges` is the execution of these statements by PostgreSQL. `AddRange`, and the change tracking turned off, change nothing. A set-based insertion by Npgsql alone, one `INSERT ... SELECT unnest(...)` per table, is 2.3 times as fast, but EF Core does not write it.
- **Durable at each commit, a single change waits for the disk on both sides:** GBO leads by ×1.6 for one order per transaction. Within a delay, GBO no longer waits, and leads by ×13.4.
- **With concurrent readers, GBO keeps its writer at 10,765 to 13,564 changes/s, and each reader at 2.6 to 3.4 totals/s.** EF Core keeps its writer at 951 to 1,190 changes/s, and each reader at 0.6 to 0.9 totals/s.

## The details

### GBO

| Step | transactions | items | duration | transactions/s | items/s | allocated per transaction |
|---|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 4.7 s | 212 | 21,241 | 1,487,519 B |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 2.1 s | 4.8 | 48,444 | 56,900,191 B |
| Change 1 order per transaction, durable at each commit | 51,887 | 51,887 orders | 30.0 s | 1,729 | 1,729 | 22,623 B |
| Change 10 orders per transaction, durable at each commit | 32,553 | 325,530 orders | 30.0 s | 1,085 | 10,851 | 250,748 B |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 3.1 s | 325 | 32,514 | 1,487,666 B |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 2.3 s | 4.3 | 43,465 | 56,898,200 B |
| Count the orders of a status | 17,104 | 1,710,400,000 orders | 30.0 s | 570 | 57,013,136 | 1,040 B |
| The total of each order | 225 | 450,000,000 lines | 30.0 s | 7.5 | 14,992,838 | 8,389,953 B |
| The orders and the sales of each product | 81 | 162,000,000 lines | 30.1 s | 2.7 | 5,374,622 | 17,509 B |
| Read 100 random orders per transaction | 1,019,390 | 101,939,000 orders | 30.0 s | 33,979 | 3,397,945 | 14,564 B |
| Load and read 1,000 random orders, their lines and products | 8,424 | 8,424,000 orders | 30.0 s | 281 | 280,773 | 2,304,449 B |
| Lazy loading, 1,000 objects per transaction | 41,553 | 41,636,106 objects | 30.0 s | 1,385 | 1,387,854 | 623,010 B |
| Change 1 order per transaction, durable within a delay | 489,047 | 489,047 orders | 30.0 s | 16,301 | 16,301 | 26,584 B |
| Change 10 orders per transaction, durable within a delay | 48,638 | 486,380 orders | 30.0 s | 1,621 | 16,212 | 244,887 B |

### EF Core + PostgreSQL

| Step | transactions | items | duration | transactions/s | items/s | allocated per transaction |
|---|---|---|---|---|---|---|
| Insert 100 orders per transaction, durable at each commit | 1,000 | 100,000 orders | 111.1 s | 9.0 | 900 | 22,827,020 B |
| Insert 10,000 orders per transaction, durable at each commit | 10 | 100,000 orders | 109.6 s | 0.09 | 912 | 2,247,540,053 B |
| Change 1 order per transaction, durable at each commit | 32,396 | 32,396 orders | 30.0 s | 1,080 | 1,080 | 69,791 B |
| Change 10 orders per transaction, durable at each commit | 11,682 | 116,820 orders | 30.0 s | 389 | 3,894 | 591,085 B |
| Insert 100 orders per transaction, durable within a delay | 1,000 | 100,000 orders | 106.4 s | 9.4 | 940 | 22,814,866 B |
| Insert 10,000 orders per transaction, durable within a delay | 10 | 100,000 orders | 109.4 s | 0.09 | 914 | 2,247,537,474 B |
| Count the orders of a status | 5,495 | 549,500,000 orders | 30.0 s | 183 | 18,315,018 | 5,013 B |
| The total of each order | 37 | 74,000,000 lines | 30.2 s | 1.2 | 2,449,743 | 28,704,731 B |
| The total of each order, a `GROUP BY` | 33 | 66,000,000 lines | 30.9 s | 1.1 | 2,138,080 | 28,516,072 B |
| The orders and the sales of each product | 25 | 50,000,000 lines | 30.9 s | 0.81 | 1,616,327 | 80,928 B |
| The orders and the sales of each product, a `GROUP BY` | 19 | 38,000,000 lines | 30.0 s | 0.63 | 1,265,444 | 82,745 B |
| Read 100 random orders per transaction, one by one | 3,046 | 304,600 orders | 30.0 s | 102 | 10,152 | 222,458 B |
| Read 100 random orders per transaction, one query | 91,039 | 9,103,900 orders | 30.0 s | 3,035 | 303,460 | 41,810 B |
| Load and read 1,000 random orders, their lines and products | 374 | 374,000 orders | 30.1 s | 12 | 12,435 | 22,616,521 B |
| Lazy loading, 1,000 objects per transaction | 157 | 157,314 objects | 30.0 s | 5.2 | 5,238 | 22,845,367 B |
| Change 1 order per transaction, durable within a delay | 36,431 | 36,431 orders | 30.0 s | 1,214 | 1,214 | 69,761 B |
| Change 10 orders per transaction, durable within a delay | 12,254 | 122,540 orders | 30.0 s | 409 | 4,085 | 591,478 B |

## Run it

```bash
dotnet run -c Release --project GhostBodyObject.Benchmarks.EntityFramework -- all 30 100000
```

The arguments: `gbo`, `ef` or `all`; the seconds of each timed step; the number of orders. The connection to PostgreSQL, its database aside, comes from the variable `GBO_POSTGRES`; without it, `Host=localhost;Port=5432;Username=postgres`, whose password Npgsql reads in `%APPDATA%\postgresql\pgpass.conf`. The role creates and drops the database `gbo_ef_benchmark` at each load. The GBO Stores live under `C:\gbo-work\ef-bench`, deleted at the end.
