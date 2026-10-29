# The ingestion benchmark, GBO against SQLite, RocksDB and Garnet

**What this document holds:** only what the code of this project reproduces (REQUIREMENT-039), the figures of the current code, the throughputs as operations per second. A change of the code that changes a result reruns the campaign and replaces its tables.

**Measured:** 2026-10-27, one run.
- **The code:** `fcf01c4`.
- **The command:** `dotnet run -c Release --project GhostBodyObject.Ycsb.Benchmarks`, its defaults: the four engines, workloads A, C, F, M and Q, 1 and 8 threads, 1 and 100 operations per transaction, 1,000,000 records, 5 s of warm-up then 15 s of measure per configuration; 96 configurations, 36 minutes with the loads.
- **Machine:** Intel i9-9900K, 8 cores, 16 logical processors, 64 GB, Windows 10 22H2 (19045), the SSD C:.
- **Runtime:** .NET 10.0.12, Release build, workstation GC. The engines one at a time, in this process.

## 1. The engines and their guarantees

| Engine | Kind | Unit of atomicity | Isolation | Durability |
|---|---|---|---|---|
| GBO | Embedded, MVCC | A transaction over any entities | Snapshot, optimistic conflicts, merges of the Mergeable types | `FastOnDisk`: flushed within 1 ms or 8 MB, then `FlushFileBuffers` |
| SQLite 3.50.4, `Microsoft.Data.Sqlite` 10.0.9 | Embedded, B-tree, one writer at a time | A transaction | Serializable | WAL, `synchronous = NORMAL`: synced at its checkpoints |
| RocksDB 11.8.1 (`RocksDbSharp`) | Embedded, LSM | A write batch, or an optimistic transaction | Snapshot, conflicts at the commit of an optimistic transaction | WAL written at each commit, synced every 1 ms |
| Garnet 2.2.0, `StackExchange.Redis` 2.9.32 | Server, in memory, hosted in the process, through the loopback | A command; a `MULTI`; a Lua script | Commands one at a time per key; a `MULTI` and a script lock their keys | AOF committed every 1 ms |

## 2. The structures of each engine

| What a query needs | GBO | SQLite | RocksDB | Garnet |
|---|---|---|---|---|
| A record by its key, A, C, F | A unique hash index of the key | The index of the primary key of a table of rowid | The key, in its LSM | The key, a hash of its fields |
| A device by its number, M | A unique hash index of the number | The `INTEGER PRIMARY KEY` | The key | The key |
| A sensor by its key, a site by its number, Q | A unique hash index of each | The primary key of a table `WITHOUT ROWID`; the `INTEGER PRIMARY KEY` of the sites | The keys, the sites in a column family of their own | The keys |
| The level in its order, Q | A value index of the level | An index of the column | A column family of the level and the key | A sorted set by the level |
| The sensors of a site, Q | A value index of the site | An index of the column | A column family of the site and the key | A set per site |

**The records:** the one of YCSB for A, C and F, a key of 23 characters and 10 fields of 100 printable bytes, `GhostSpan<byte>` in GBO; a device for M, its count, its sum, its minimum, its maximum, the time and the status of its latest message, `[Mergeable]` in GBO; for Q, a sensor of its key, its site, the count and the sum of its messages, the level and the time of the latest one, and a site of the count and the sum of the messages of its 1,000 sensors, both `[Mergeable]` in GBO.

## 3. The settings beyond the defaults, and why

| Engine | Setting | Why |
|---|---|---|
| GBO | `FlushPolicy.FastOnDisk` | The persistent mode of the benchmark |
| GBO | `StoreLockPolicy.Retain` | The only writer of the process: the default releases the write token, and flushes, after each commit |
| SQLite | `journal_mode = WAL`, `synchronous = NORMAL` | The persistent mode nearest to `FastOnDisk`; SQLite has no group commit |
| SQLite | A lock of the application around each write transaction | One writer at a time: the retries of a busy database wait |
| SQLite | A table of rowid for the record of YCSB, `WITHOUT ROWID` for the sensors | The rule of its documentation: a row of 1,035 bytes overflows a page of a table `WITHOUT ROWID` |
| RocksDB | `OptimisticTransactionDB`, `set_snapshot` | Transactions on a snapshot, as GBO's |
| RocksDB | `FlushWAL(true)` every 1 ms, on a thread of its own | The durability within 1 ms |
| RocksDB | An operator of merge for the devices of M; inserts and events in a write batch | Events added without conflict; writes that read nothing |
| Garnet | `--aof --aof-commit-freq 1` | The durability within 1 ms |
| Garnet | `--lua --lua-transaction-mode` | The script of the events of M locks its devices |
| Garnet | A connection per thread | As YCSB gives a client to each of its threads |

## 4. The loopback of Garnet

`PING` through the loopback by `StackExchange.Redis`, 2 s each:

| Clients | round trips/s |
|---|---|
| 1, one at a time | 12,416 |
| 8, one at a time | 83,238 |
| 1, pipelined by 1,000 | 737,230 |

## 5. The loads and the reopen

1,000,000 records by transactions of 10,000 on one thread; the bytes written on the volume C: per record; the reopen of the data of A, C and F, its records counted after.

| Engine | Data | Load, records/s | B written per record | Reopen |
|---|---|---|---|---|
| GBO | A, C, F | 104,246 | 1,979 | 0.39 s |
| GBO | M | 135,726 | 929 | — |
| GBO | Q | 75,154 | 1,619 | — |
| SQLite | A, C, F | 16,783 | 5,949 | 0.00 s, its pages read later |
| SQLite | M | 629,140 | 85 | — |
| SQLite | Q | 15,067 | 6,774 | — |
| RocksDB | A, C, F | 195,119 | 2,949 | 0.60 s |
| RocksDB | M | 2,069,817 | 53 | — |
| RocksDB | Q | 216,312 | 183 | — |
| Garnet | A, C, F | 102,339 | 1,204 | 7.12 s, its AOF replayed |
| Garnet | M | 236,276 | 142 | — |
| Garnet | Q | 85,624 | 311 | — |

## 6. How to read the tables

- **operations/s:** the operations of the transactions committed during the 15 s of the measure; **conflicts/s** the transactions retried.
- **Latencies,** in µs: of an operation for a batch of 1, of a transaction of 100 operations for a batch of 100; a read and a write apart, a transaction that writes counted as a write.
- **B/operation:** the bytes allocated by the GC of the process per operation, Garnet's server included; the native allocations of SQLite and RocksDB are not counted.
- **disk B/write:** the bytes written on the volume C: during the measure per write, the caches of the OS included.
- **working set:** the memory of the process at the end of the measure.

## 7. Workload A, update heavy: 50 % reads, 50 % updates of one field

| Engine | Threads | Batch | operations/s | reads/s | writes/s | conflicts/s | read p50 / p99 / p99.9 | write p50 / p99 / p99.9 | B/operation | disk B/write | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 35,330 | 17,675 | 17,654 | 0 | 3 / 10 / 30 | 9 / 1,382 / 6,042 | 17,409 | 1,635 | 2,639 |
| GBO | 1 | 100 | 140,462 | 70,261 | 70,202 | 0 | — | 243 / 4,301 / 7,782 | 1,173 | 2,348 | 2,117 |
| GBO | 8 | 1 | 52,631 | 26,302 | 26,329 | 432 | 7 / 34 / 448 | 53 / 3,379 / 12,288 | 17,719 | 2,759 | 2,115 |
| GBO | 8 | 100 | 99,142 | 49,583 | 49,558 | 5,195 | — | 3,277 / 42,598 / 1,153,434 | 7,036 | 2,342 | 2,167 |
| SQLite | 1 | 1 | 11,036 | 5,502 | 5,534 | 0 | 26 / 51 / 83 | 38 / 77 / 1,741 | 1,652 | 7,616 | 277 |
| SQLite | 1 | 100 | 22,899 | 11,448 | 11,452 | 0 | — | 2,099 / 49,971 / 52,429 | 1,064 | 7,396 | 243 |
| SQLite | 8 | 1 | 10,441 | 5,238 | 5,202 | 0 | 34 / 101 / 182 | 66 / 4,915 / 199,885 | 1,652 | 7,594 | 246 |
| SQLite | 8 | 100 | 21,091 | 10,558 | 10,534 | 0 | — | 2,406 / 386,662 / 720,896 | 1,065 | 7,463 | 243 |
| RocksDB | 1 | 1 | 23,303 | 11,655 | 11,648 | 0 | 20 / 75 / 173 | 37 / 627 / 922 | 0 | 2,511 | 557 |
| RocksDB | 1 | 100 | 30,274 | 15,135 | 15,140 | 0 | — | 3,072 / 7,168 / 14,336 | 0 | 7,586 | 532 |
| RocksDB | 8 | 1 | 54,690 | 27,344 | 27,346 | 153 | 41 / 275 / 691 | 106 / 845 / 6,451 | 0 | 8,330 | 554 |
| RocksDB | 8 | 100 | 73,514 | 36,745 | 36,769 | 2,694 | — | 9,011 / 38,502 / 124,518 | 2 | 5,250 | 537 |
| Garnet | 1 | 1 | 14,279 | 7,129 | 7,150 | 0 | 67 / 125 / 275 | 66 / 122 / 256 | 1,342 | 206 | 3,633 |
| Garnet | 1 | 100 | 191,943 | 95,939 | 96,004 | 0 | — | 486 / 1,203 / 1,434 | 1,902 | 183 | 3,668 |
| Garnet | 8 | 1 | 86,079 | 43,021 | 43,058 | 0 | 88 / 179 / 653 | 85 / 173 / 640 | 1,334 | 185 | 3,669 |
| Garnet | 8 | 100 | 666,726 | 333,349 | 333,377 | 0 | — | 870 / 3,482 / 5,120 | 1,904 | 181 | 3,670 |

## 8. Workload C, read only

| Engine | Threads | Batch | reads/s | read p50 / p99 / p99.9 | B/operation | working set, MB |
|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 284,229 | 3 / 6 / 49 | 7,440 | 2,005 |
| GBO | 1 | 100 | 731,719 | 118 / 256 / 486 | 118 | 2,153 |
| GBO | 8 | 1 | 588,541 | 6 / 40 / 435 | 7,441 | 2,223 |
| GBO | 8 | 100 | 4,688,931 | 154 / 499 / 781 | 118 | 2,287 |
| SQLite | 1 | 1 | 40,266 | 24 / 50 / 122 | 1,760 | 244 |
| SQLite | 1 | 100 | 51,977 | 1,843 / 3,021 / 4,198 | 1,772 | 247 |
| SQLite | 8 | 1 | 100,693 | 75 / 205 / 352 | 1,760 | 244 |
| SQLite | 8 | 100 | 103,179 | 7,578 / 11,059 / 13,107 | 1,772 | 244 |
| RocksDB | 1 | 1 | 68,152 | 13 / 54 / 112 | 0 | 522 |
| RocksDB | 1 | 100 | 67,433 | 1,357 / 2,816 / 3,686 | 0 | 522 |
| RocksDB | 8 | 1 | 335,521 | 18 / 106 / 218 | 0 | 522 |
| RocksDB | 8 | 100 | 305,530 | 2,509 / 4,608 / 6,042 | 0 | 523 |
| Garnet | 1 | 1 | 14,045 | 67 / 122 / 256 | 2,433 | 3,666 |
| Garnet | 1 | 100 | 149,929 | 627 / 1,357 / 1,536 | 3,008 | 3,666 |
| Garnet | 8 | 1 | 82,613 | 91 / 173 / 666 | 2,432 | 3,667 |
| Garnet | 8 | 100 | 648,783 | 870 / 2,714 / 3,277 | 3,012 | 3,671 |

## 9. Workload F, read-modify-write: 50 % reads, 50 % read-modify-writes

| Engine | Threads | Batch | operations/s | reads/s | writes/s | conflicts/s | read p50 / p99 / p99.9 | write p50 / p99 / p99.9 | B/operation | disk B/write | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 40,681 | 20,336 | 20,345 | 0 | 3 / 9 / 30 | 10 / 1,331 / 3,174 | 17,452 | 2,296 | 2,530 |
| GBO | 1 | 100 | 154,000 | 76,971 | 77,030 | 0 | — | 243 / 3,379 / 6,144 | 1,174 | 2,338 | 2,513 |
| GBO | 8 | 1 | 54,293 | 27,091 | 27,203 | 445 | 7 / 32 / 448 | 50 / 3,123 / 11,264 | 17,718 | 2,747 | 2,560 |
| GBO | 8 | 100 | 115,626 | 57,735 | 57,891 | 6,393 | — | 3,226 / 44,237 / 222,822 | 7,316 | 2,378 | 2,270 |
| SQLite | 1 | 1 | 11,962 | 5,958 | 6,003 | 0 | 26 / 54 / 118 | 46 / 98 / 1,792 | 2,535 | 7,656 | 244 |
| SQLite | 1 | 100 | 21,485 | 10,737 | 10,749 | 0 | — | 2,406 / 50,790 / 54,067 | 1,945 | 7,417 | 244 |
| SQLite | 8 | 1 | 10,629 | 5,315 | 5,314 | 0 | 34 / 96 / 163 | 70 / 5,837 / 160,563 | 2,532 | 9,280 | 248 |
| SQLite | 8 | 100 | 18,274 | 9,155 | 9,118 | 0 | — | 2,816 / 347,341 / 589,824 | 1,944 | 7,441 | 245 |
| RocksDB | 1 | 1 | 18,373 | 9,146 | 9,227 | 0 | 29 / 109 / 224 | 49 / 576 / 3,072 | 0 | 15,588 | 541 |
| RocksDB | 1 | 100 | 35,984 | 17,987 | 17,997 | 0 | — | 2,355 / 11,264 / 28,672 | 0 | 4,488 | 524 |
| RocksDB | 8 | 1 | 69,806 | 34,828 | 34,978 | 204 | 32 / 218 / 512 | 88 / 768 / 9,830 | 0 | 4,425 | 580 |
| RocksDB | 8 | 100 | 71,517 | 35,682 | 35,835 | 2,636 | — | 7,987 / 62,259 / 458,752 | 2 | 4,581 | 579 |
| Garnet | 1 | 1 | 12,583 | 6,263 | 6,320 | 0 | 70 / 138 / 282 | 80 / 150 / 314 | 3,544 | 267 | 3,671 |
| Garnet | 1 | 100 | 126,201 | 63,074 | 63,126 | 0 | — | 742 / 1,613 / 1,894 | 3,429 | 184 | 3,667 |
| Garnet | 8 | 1 | 75,679 | 37,790 | 37,888 | 0 | 91 / 182 / 922 | 102 / 205 / 973 | 3,546 | 227 | 3,671 |
| Garnet | 8 | 100 | 447,807 | 223,904 | 223,903 | 0 | — | 1,331 / 4,096 / 5,530 | 3,431 | 182 | 3,668 |

## 10. Workload M, the meters: 1,000,000 devices, events by a law of Zipf of exponent 1

Each event adds 1 to the count of its device and its value to its sum, keeps the minimum and the maximum, and the time and the status of the latest message: GBO by its Mergeable device, SQLite by one `UPDATE`, RocksDB by its operator of merge in a write batch, Garnet by a Lua script.

| Engine | Threads | Batch | events/s | conflicts/s | p50 / p99 / p99.9 | B/event | disk B/event | working set, MB |
|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 52,475 | 0 | 8 / 358 / 768 | 26,227 | 490 | 524 |
| GBO | 1 | 100 | 424,823 | 0 | 106 / 1,485 / 3,277 | 762 | 201 | 542 |
| GBO | 8 | 1 | 51,882 | 0 | 59 / 1,178 / 12,083 | 26,377 | 547 | 549 |
| GBO | 8 | 100 | 675,604 | 0 | 627 / 5,632 / 21,299 | 792 | 196 | 574 |
| SQLite | 1 | 1 | 20,584 | 0 | 21 / 44 / 16,794 | 1,832 | 6,366 | 253 |
| SQLite | 1 | 100 | 32,528 | 0 | 781 / 35,226 / 36,864 | 644 | 5,088 | 249 |
| SQLite | 8 | 1 | 18,180 | 0 | 26 / 1,331 / 108,134 | 1,832 | 6,337 | 252 |
| SQLite | 8 | 100 | 30,562 | 0 | 1,152 / 190,054 / 380,109 | 644 | 5,086 | 253 |
| RocksDB | 1 | 1 | 106,614 | 0 | 6 / 15 / 602 | 6 | 248 | 519 |
| RocksDB | 1 | 100 | 430,162 | 0 | 157 / 768 / 1,894 | 16 | 77 | 511 |
| RocksDB | 8 | 1 | 137,851 | 0 | 42 / 435 / 653 | 13 | 101 | 508 |
| RocksDB | 8 | 100 | 560,315 | 0 | 1,229 / 6,246 / 12,288 | 17 | 73 | 521 |
| Garnet | 1 | 1 | 11,113 | 0 | 83 / 160 / 282 | 505 | 308 | 2,166 |
| Garnet | 1 | 100 | 127,103 | 0 | 678 / 973 / 34,406 | 167 | 243 | 1,496 |
| Garnet | 8 | 1 | 66,299 | 0 | 106 / 230 / 717 | 463 | 252 | 1,513 |
| Garnet | 8 | 100 | 147,160 | 0 | 2,611 / 40,960 / 77,005 | 157 | 224 | 1,541 |

## 11. Workload Q, the sensors and their sites

1,000,000 sensors, 1,000 per site. A writer sends messages: each one updates its sensor and its site in one transaction, its level becoming the level of the sensor. A reader runs, by thirds, a range (up to 100 sensors from a level), an aggregate (the sensors of 1,000 levels, counted with their messages) and the snapshot of a site (the site and its 1,000 sensors), one query per read transaction. Each configuration of the writers alone, then with as many readers.

| Engine | Writers | Readers | Batch | messages/s | conflicts/s | queries/s | ranges/s | aggregates/s | snapshots/s | sensors read/s | message p50 / p99 / p99.9 | query p50 / p99 / p99.9 | B/operation | disk B/message | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 0 | 1 | 1,525 | 0 | 0 | 0 | 0 | 0 | 0 | 275 / 12,698 / 17,203 | — | 1,200,290 | 12,577 | 654 |
| GBO | 1 | 1 | 1 | 1,505 | 0 | 7,058 | 2,335 | 2,368 | 2,356 | 4,956,563 | 262 / 11,878 / 17,203 | 48 / 576 / 2,970 | 216,327 | 16,502 | 581 |
| GBO | 1 | 0 | 100 | 16,390 | 0 | 0 | 0 | 0 | 0 | 0 | 5,632 / 27,853 / 36,045 | — | 60,228 | 8,886 | 803 |
| GBO | 1 | 1 | 100 | 14,857 | 0 | 6,258 | 2,080 | 2,106 | 2,072 | 4,386,528 | 6,042 / 31,949 / 40,960 | 58 / 1,331 / 3,789 | 43,738 | 8,712 | 781 |
| GBO | 8 | 0 | 1 | 1,567 | 0 | 0 | 0 | 0 | 0 | 0 | 2,304 / 33,587 / 49,971 | — | 1,193,920 | 16,439 | 595 |
| GBO | 8 | 8 | 1 | 1,118 | 0 | 44,422 | 14,802 | 14,808 | 14,812 | 31,100,956 | 4,915 / 34,406 / 42,598 | 70 / 1,357 / 10,854 | 35,376 | 16,957 | 610 |
| GBO | 8 | 0 | 100 | 16,048 | 0 | 0 | 0 | 0 | 0 | 0 | 45,056 / 85,197 / 150,733 | — | 59,256 | 8,640 | 825 |
| GBO | 8 | 8 | 100 | 12,501 | 0 | 43,446 | 14,490 | 14,452 | 14,504 | 30,405,708 | 54,067 / 321,126 / 406,323 | 78 / 1,562 / 4,301 | 17,955 | 8,602 | 875 |
| SQLite | 1 | 0 | 1 | 4,736 | 0 | 0 | 0 | 0 | 0 | 0 | 56 / 170 / 37,683 | — | 2,080 | 28,909 | 242 |
| SQLite | 1 | 1 | 1 | 3,952 | 0 | 217 | 71 | 72 | 75 | 153,857 | 74 / 2,867 / 35,226 | 5,632 / 9,216 / 10,650 | 2,114 | 28,901 | 246 |
| SQLite | 1 | 0 | 100 | 5,784 | 0 | 0 | 0 | 0 | 0 | 0 | 4,301 / 62,259 / 67,174 | — | 892 | 23,678 | 250 |
| SQLite | 1 | 1 | 100 | 7,887 | 0 | 202 | 69 | 67 | 66 | 140,569 | 8,806 / 229,376 / 314,573 | 5,837 / 9,626 / 10,650 | 939 | 18,314 | 251 |
| SQLite | 8 | 0 | 1 | 4,547 | 0 | 0 | 0 | 0 | 0 | 0 | 61 / 512 / 537,395 | — | 2,080 | 28,770 | 247 |
| SQLite | 8 | 8 | 1 | 2,600 | 0 | 323 | 105 | 110 | 109 | 228,626 | 224 / 6,963 / 825,754 | 31,949 / 40,141 / 44,237 | 2,150 | 22,175 | 247 |
| SQLite | 8 | 0 | 100 | 5,142 | 0 | 0 | 0 | 0 | 0 | 0 | 137,626 / 786,432 / 943,718 | — | 891 | 26,838 | 246 |
| SQLite | 8 | 8 | 100 | 3,172 | 0 | 316 | 106 | 104 | 106 | 220,216 | 242,483 / 498,074 / 524,288 | 33,587 / 40,960 / 43,418 | 1,061 | 15,670 | 250 |
| RocksDB | 1 | 0 | 1 | 20,826 | 0 | 0 | 0 | 0 | 0 | 0 | 29 / 563 / 717 | — | 0 | 595 | 661 |
| RocksDB | 1 | 1 | 1 | 19,718 | 0 | 155 | 50 | 51 | 53 | 109,322 | 31 / 563 / 666 | 6,144 / 13,517 / 14,336 | 0 | 494 | 774 |
| RocksDB | 1 | 0 | 100 | 36,379 | 0 | 0 | 0 | 0 | 0 | 0 | 2,662 / 3,891 / 6,451 | — | 0 | 338 | 815 |
| RocksDB | 1 | 1 | 100 | 32,048 | 0 | 103 | 34 | 35 | 34 | 72,822 | 3,021 / 4,301 / 9,011 | 12,493 / 16,794 / 27,034 | 0 | 365 | 906 |
| RocksDB | 8 | 0 | 1 | 73,297 | 1,351 | 0 | 0 | 0 | 0 | 0 | 78 / 512 / 691 | — | 1 | 293 | 981 |
| RocksDB | 8 | 8 | 1 | 53,215 | 897 | 255 | 81 | 86 | 88 | 181,779 | 101 / 602 / 768 | 37,683 / 68,813 / 80,282 | 1 | 320 | 912 |
| RocksDB | 8 | 0 | 100 | 39,529 | 2,763 | 0 | 0 | 0 | 0 | 0 | 13,926 / 101,581 / 262,144 | — | 4 | 401 | 910 |
| RocksDB | 8 | 8 | 100 | 34,561 | 2,376 | 308 | 103 | 101 | 104 | 215,378 | 17,613 / 88,474 / 147,456 | 33,587 / 46,694 / 60,621 | 4 | 308 | 945 |
| Garnet | 1 | 0 | 1 | 9,176 | 0 | 0 | 0 | 0 | 0 | 0 | 93 / 195 / 390 | — | 5,813 | 1,203 | 1,792 |
| Garnet | 1 | 1 | 1 | 1,026 | 0 | 37 | 12 | 12 | 13 | 26,236 | 96 / 31,130 / 111,411 | 2,662 / 124,518 / 524,288 | 271,997 | 707 | 2,723 |
| Garnet | 1 | 0 | 100 | 41,140 | 0 | 0 | 0 | 0 | 0 | 0 | 1,638 / 17,613 / 20,070 | — | 4,940 | 514 | 2,686 |
| Garnet | 1 | 1 | 100 | 4,492 | 0 | 33 | 11 | 11 | 11 | 23,128 | 1,792 / 134,349 / 537,395 | 8,602 / 134,349 / 537,395 | 64,896 | 539 | 2,759 |
| Garnet | 8 | 0 | 1 | 37,893 | 0 | 0 | 0 | 0 | 0 | 0 | 125 / 333 / 16,384 | — | 5,803 | 553 | 2,772 |
| Garnet | 8 | 8 | 1 | 0 | 0 | 163 | 51 | 56 | 55 | 116,412 | — | 19,251 / 242,483 / 773,325 | 7,581,727 | — | 5,457 |
| Garnet | 8 | 0 | 100 | 67,392 | 0 | 0 | 0 | 0 | 0 | 0 | 6,451 / 60,621 / 144,179 | — | 4,921 | 513 | 5,672 |
| Garnet | 8 | 8 | 100 | 0 | 0 | 164 | 55 | 55 | 55 | 115,262 | — | 18,842 / 235,930 / 2,516,582 | 8,147,573 | — | 4,625 |

**Garnet, 8 writers and 8 readers:** its writers committed no message during the 15 s of the measure, whatever the batch: its readers held them. Not explained here.

## 12. The checks

Every check passed, out of the measure: each read found its record; each reopen counted its 1,000,000 records, Garnet's too, which logged errors of the scan of its AOF at its reopen and recovered from the metadata of its last commit; the devices of M counted the events committed, the warm-ups included, in every engine; each snapshot of a site of Q found its totals equal to the sums of its 1,000 sensors; in GBO, the update of a device concurrent with its removal failed.
