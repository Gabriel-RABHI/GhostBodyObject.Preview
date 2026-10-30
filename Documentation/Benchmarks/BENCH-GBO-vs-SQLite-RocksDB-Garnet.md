# The ingestion benchmark, GBO against SQLite, RocksDB and Garnet

**What this document holds:** only what the code of this project reproduces (REQUIREMENT-039), the figures of the current code, the throughputs as operations per second. A change of the code that changes a result reruns the campaign and replaces its tables.

**Measured:** 2026-10-30, one run.
- **The code:** `d78803d`.
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
| 1, one at a time | 13,796 |
| 8, one at a time | 91,392 |
| 1, pipelined by 1,000 | 751,347 |

## 5. The loads and the reopen

1,000,000 records by transactions of 10,000 on one thread; the bytes written on the volume C: per record; the reopen of the data of A, C and F, its records counted after.

| Engine | Data | Load, records/s | B written per record | Reopen |
|---|---|---|---|---|
| GBO | A, C, F | 83,975 | 2,012 | 0.38 s |
| GBO | M | 177,927 | 929 | — |
| GBO | Q | 86,476 | 1,620 | — |
| SQLite | A, C, F | 19,020 | 5,899 | 0.00 s, its pages read later |
| SQLite | M | 427,163 | 83 | — |
| SQLite | Q | 14,229 | 6,776 | — |
| RocksDB | A, C, F | 116,918 | 2,979 | 1.05 s |
| RocksDB | M | 2,574,011 | 53 | — |
| RocksDB | Q | 234,822 | 183 | — |
| Garnet | A, C, F | 108,049 | 1,209 | 6.45 s, its AOF replayed |
| Garnet | M | 256,302 | 142 | — |
| Garnet | Q | 89,255 | 310 | — |

## 6. How to read the tables

- **operations/s:** the operations of the transactions committed during the 15 s of the measure; **conflicts/s** the transactions retried.
- **Latencies,** in µs: of an operation for a batch of 1, of a transaction of 100 operations for a batch of 100; a read and a write apart, a transaction that writes counted as a write.
- **B/operation:** the bytes allocated by the GC of the process per operation, Garnet's server included; the native allocations of SQLite and RocksDB are not counted.
- **disk B/write:** the bytes written on the volume C: during the measure per write, the caches of the OS included.
- **working set:** the memory of the process at the end of the measure.

## 7. Workload A, update heavy: 50 % reads, 50 % updates of one field

| Engine | Threads | Batch | operations/s | reads/s | writes/s | conflicts/s | read p50 / p99 / p99.9 | write p50 / p99 / p99.9 | B/operation | disk B/write | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 86,610 | 43,234 | 43,376 | 0 | 2 / 5 / 13 | 5 / 307 / 1,997 | 7,759 | 2,602 | 2,405 |
| GBO | 1 | 100 | 190,016 | 94,968 | 95,047 | 0 | — | 192 / 2,714 / 5,939 | 999 | 2,341 | 2,093 |
| GBO | 8 | 1 | 112,232 | 56,050 | 56,182 | 581 | 3 / 7 / 339 | 8 / 2,662 / 5,530 | 7,816 | 2,682 | 2,187 |
| GBO | 8 | 100 | 125,558 | 62,811 | 62,746 | 5,658 | — | 3,891 / 33,587 / 81,920 | 5,149 | 2,342 | 2,161 |
| SQLite | 1 | 1 | 10,499 | 5,231 | 5,268 | 0 | 23 / 43 / 69 | 36 / 64 / 2,458 | 1,652 | 7,623 | 230 |
| SQLite | 1 | 100 | 22,915 | 11,457 | 11,459 | 0 | — | 1,997 / 52,429 / 58,982 | 1,064 | 7,392 | 226 |
| SQLite | 8 | 1 | 7,754 | 3,880 | 3,874 | 0 | 32 / 94 / 141 | 58 / 5,427 / 262,144 | 1,652 | 7,765 | 223 |
| SQLite | 8 | 100 | 22,445 | 11,210 | 11,235 | 0 | — | 2,048 / 550,502 / 1,232,077 | 1,063 | 7,383 | 223 |
| RocksDB | 1 | 1 | 26,884 | 13,427 | 13,457 | 0 | 16 / 62 / 90 | 32 / 602 / 768 | 0 | 2,796 | 521 |
| RocksDB | 1 | 100 | 33,624 | 16,783 | 16,842 | 0 | — | 2,509 / 11,674 / 37,683 | 0 | 4,250 | 490 |
| RocksDB | 8 | 1 | 80,564 | 40,270 | 40,294 | 223 | 30 / 182 / 474 | 78 / 640 / 9,011 | 0 | 4,050 | 521 |
| RocksDB | 8 | 100 | 78,433 | 39,230 | 39,203 | 2,791 | — | 6,758 / 54,067 / 511,181 | 2 | 4,327 | 546 |
| Garnet | 1 | 1 | 14,165 | 7,071 | 7,094 | 0 | 70 / 102 / 157 | 67 / 102 / 150 | 1,342 | 216 | 3,299 |
| Garnet | 1 | 100 | 191,195 | 95,559 | 95,636 | 0 | — | 486 / 1,203 / 1,382 | 1,902 | 189 | 3,268 |
| Garnet | 8 | 1 | 85,739 | 42,863 | 42,876 | 0 | 90 / 163 / 640 | 86 / 160 / 627 | 1,334 | 185 | 3,269 |
| Garnet | 8 | 100 | 690,999 | 345,546 | 345,453 | 0 | — | 845 / 3,277 / 4,710 | 1,904 | 182 | 3,270 |

## 8. Workload C, read only

| Engine | Threads | Batch | reads/s | read p50 / p99 / p99.9 | B/operation | working set, MB |
|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 480,276 | 2 / 3 / 8 | 5,312 | 1,985 |
| GBO | 1 | 100 | 881,148 | 102 / 163 / 314 | 97 | 2,066 |
| GBO | 8 | 1 | 1,505,578 | 3 / 9 / 256 | 5,312 | 2,130 |
| GBO | 8 | 100 | 6,185,526 | 109 / 390 / 512 | 96 | 2,177 |
| SQLite | 1 | 1 | 44,181 | 22 / 41 / 64 | 1,760 | 224 |
| SQLite | 1 | 100 | 57,759 | 1,690 / 1,997 / 2,765 | 1,772 | 224 |
| SQLite | 8 | 1 | 113,449 | 67 / 173 / 282 | 1,760 | 210 |
| SQLite | 8 | 100 | 119,891 | 6,554 / 8,806 / 11,264 | 1,772 | 213 |
| RocksDB | 1 | 1 | 39,306 | 24 / 78 / 106 | 0 | 538 |
| RocksDB | 1 | 100 | 39,727 | 2,458 / 3,379 / 4,403 | 0 | 538 |
| RocksDB | 8 | 1 | 155,231 | 42 / 189 / 256 | 0 | 539 |
| RocksDB | 8 | 100 | 156,727 | 5,018 / 6,554 / 7,578 | 0 | 543 |
| Garnet | 1 | 1 | 13,833 | 70 / 109 / 170 | 2,433 | 3,270 |
| Garnet | 1 | 100 | 145,541 | 653 / 1,306 / 1,459 | 3,008 | 3,266 |
| Garnet | 8 | 1 | 85,120 | 88 / 163 / 627 | 2,432 | 3,259 |
| Garnet | 8 | 100 | 673,480 | 845 / 2,560 / 3,482 | 3,012 | 3,261 |

## 9. Workload F, read-modify-write: 50 % reads, 50 % read-modify-writes

| Engine | Threads | Batch | operations/s | reads/s | writes/s | conflicts/s | read p50 / p99 / p99.9 | write p50 / p99 / p99.9 | B/operation | disk B/write | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 83,307 | 41,710 | 41,597 | 0 | 2 / 4 / 11 | 5 / 563 / 1,946 | 7,745 | 2,565 | 2,281 |
| GBO | 1 | 100 | 174,712 | 87,343 | 87,369 | 0 | — | 218 / 2,867 / 5,734 | 999 | 2,346 | 2,163 |
| GBO | 8 | 1 | 110,837 | 55,374 | 55,462 | 585 | 3 / 7 / 339 | 9 / 2,611 / 5,530 | 7,818 | 2,675 | 2,181 |
| GBO | 8 | 100 | 157,774 | 78,828 | 78,945 | 7,839 | — | 3,584 / 27,034 / 55,706 | 5,617 | 2,337 | 2,114 |
| SQLite | 1 | 1 | 11,828 | 5,891 | 5,937 | 0 | 24 / 44 / 74 | 42 / 72 / 18,022 | 2,535 | 7,703 | 214 |
| SQLite | 1 | 100 | 21,599 | 10,794 | 10,805 | 0 | — | 2,253 / 54,067 / 62,259 | 1,944 | 7,379 | 213 |
| SQLite | 8 | 1 | 10,230 | 5,120 | 5,110 | 0 | 31 / 91 / 134 | 61 / 6,246 / 180,224 | 2,531 | 7,577 | 213 |
| SQLite | 8 | 100 | 21,229 | 10,606 | 10,623 | 0 | — | 2,304 / 668,467 / 1,258,291 | 1,944 | 7,462 | 218 |
| RocksDB | 1 | 1 | 23,233 | 11,578 | 11,655 | 0 | 20 / 70 / 109 | 37 / 640 / 806 | 0 | 2,205 | 526 |
| RocksDB | 1 | 100 | 38,049 | 19,024 | 19,025 | 0 | — | 2,304 / 7,168 / 15,565 | 0 | 3,956 | 522 |
| RocksDB | 8 | 1 | 84,852 | 42,386 | 42,466 | 244 | 28 / 176 / 435 | 77 / 627 / 7,373 | 0 | 4,170 | 542 |
| RocksDB | 8 | 100 | 95,356 | 47,612 | 47,744 | 3,347 | — | 6,758 / 31,949 / 160,563 | 2 | 3,907 | 544 |
| Garnet | 1 | 1 | 12,510 | 6,228 | 6,282 | 0 | 72 / 112 / 166 | 82 / 125 / 205 | 3,544 | 289 | 3,257 |
| Garnet | 1 | 100 | 124,367 | 62,156 | 62,211 | 0 | — | 755 / 1,587 / 1,792 | 3,429 | 187 | 3,257 |
| Garnet | 8 | 1 | 75,823 | 37,855 | 37,968 | 0 | 91 / 163 / 870 | 102 / 186 / 922 | 3,545 | 232 | 3,261 |
| Garnet | 8 | 100 | 466,601 | 233,294 | 233,307 | 0 | — | 1,280 / 3,891 / 5,325 | 3,430 | 186 | 3,265 |

## 10. Workload M, the meters: 1,000,000 devices, events by a law of Zipf of exponent 1

Each event adds 1 to the count of its device and its value to its sum, keeps the minimum and the maximum, and the time and the status of the latest message: GBO by its Mergeable device, SQLite by one `UPDATE`, RocksDB by its operator of merge in a write batch, Garnet by a Lua script.

| Engine | Threads | Batch | events/s | conflicts/s | p50 / p99 / p99.9 | B/event | disk B/event | working set, MB |
|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 1 | 143,740 | 0 | 3 / 11 / 653 | 8,987 | 467 | 519 |
| GBO | 1 | 100 | 422,749 | 0 | 99 / 2,355 / 6,554 | 579 | 200 | 518 |
| GBO | 8 | 1 | 143,461 | 0 | 9 / 794 / 1,741 | 9,030 | 459 | 550 |
| GBO | 8 | 100 | 582,158 | 0 | 189 / 7,782 / 18,432 | 612 | 196 | 578 |
| SQLite | 1 | 1 | 19,878 | 0 | 20 / 38 / 20,890 | 1,832 | 6,356 | 229 |
| SQLite | 1 | 100 | 31,570 | 0 | 768 / 36,864 / 43,418 | 644 | 5,083 | 225 |
| SQLite | 8 | 1 | 17,600 | 0 | 26 / 1,613 / 124,518 | 1,832 | 6,355 | 227 |
| SQLite | 8 | 100 | 29,138 | 0 | 896 / 268,698 / 707,789 | 644 | 5,089 | 223 |
| RocksDB | 1 | 1 | 116,317 | 0 | 6 / 12 / 576 | 5 | 120 | 503 |
| RocksDB | 1 | 100 | 470,783 | 0 | 147 / 742 / 922 | 15 | 75 | 491 |
| RocksDB | 8 | 1 | 144,722 | 0 | 42 / 422 / 486 | 20 | 113 | 506 |
| RocksDB | 8 | 100 | 567,180 | 0 | 1,178 / 5,734 / 15,155 | 14 | 76 | 520 |
| Garnet | 1 | 1 | 11,252 | 0 | 83 / 141 / 205 | 505 | 307 | 2,108 |
| Garnet | 1 | 100 | 127,391 | 0 | 678 / 896 / 35,226 | 167 | 247 | 1,482 |
| Garnet | 8 | 1 | 67,552 | 0 | 102 / 205 / 691 | 463 | 254 | 1,499 |
| Garnet | 8 | 100 | 162,499 | 0 | 2,304 / 38,502 / 73,728 | 157 | 224 | 1,527 |

## 11. Workload Q, the sensors and their sites

1,000,000 sensors, 1,000 per site. A writer sends messages: each one updates its sensor and its site in one transaction, its level becoming the level of the sensor. A reader runs, by thirds, a range (up to 100 sensors from a level), an aggregate (the sensors of 1,000 levels, counted with their messages) and the snapshot of a site (the site and its 1,000 sensors), one query per read transaction. Each configuration of the writers alone, then with as many readers.

| Engine | Writers | Readers | Batch | messages/s | conflicts/s | queries/s | ranges/s | aggregates/s | snapshots/s | sensors read/s | message p50 / p99 / p99.9 | query p50 / p99 / p99.9 | B/operation | disk B/message | working set, MB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| GBO | 1 | 0 | 1 | 2,522 | 0 | 0 | 0 | 0 | 0 | 0 | 195 / 1,997 / 15,155 | — | 505,764 | 14,480 | 570 |
| GBO | 1 | 1 | 1 | 2,462 | 0 | 8,410 | 2,779 | 2,820 | 2,810 | 5,908,650 | 195 / 2,458 / 15,974 | 46 / 365 / 1,178 | 117,792 | 15,930 | 568 |
| GBO | 1 | 0 | 100 | 17,082 | 0 | 0 | 0 | 0 | 0 | 0 | 5,325 / 28,672 / 36,864 | — | 52,039 | 8,867 | 786 |
| GBO | 1 | 1 | 100 | 14,955 | 0 | 7,635 | 2,540 | 2,571 | 2,524 | 5,350,014 | 5,427 / 29,491 / 144,179 | 51 / 794 / 2,714 | 35,389 | 8,734 | 782 |
| GBO | 8 | 0 | 1 | 2,462 | 0 | 0 | 0 | 0 | 0 | 0 | 211 / 57,344 / 154,010 | — | 501,436 | 15,524 | 569 |
| GBO | 8 | 8 | 1 | 2,124 | 0 | 60,958 | 20,299 | 20,344 | 20,315 | 42,689,199 | 2,765 / 20,070 / 30,310 | 53 / 410 / 1,510 | 20,513 | 16,024 | 575 |
| GBO | 8 | 0 | 100 | 16,634 | 0 | 0 | 0 | 0 | 0 | 0 | 40,141 / 203,162 / 747,110 | — | 51,215 | 8,631 | 820 |
| GBO | 8 | 8 | 100 | 13,617 | 0 | 55,686 | 18,580 | 18,517 | 18,590 | 38,966,106 | 49,152 / 150,733 / 262,144 | 59 / 1,075 / 3,021 | 13,020 | 8,542 | 816 |
| SQLite | 1 | 0 | 1 | 4,665 | 0 | 0 | 0 | 0 | 0 | 0 | 54 / 128 / 40,141 | — | 2,080 | 28,891 | 215 |
| SQLite | 1 | 1 | 1 | 3,824 | 0 | 236 | 77 | 79 | 80 | 167,008 | 75 / 2,662 / 36,045 | 5,222 / 7,373 / 8,602 | 2,117 | 28,974 | 216 |
| SQLite | 1 | 0 | 100 | 5,918 | 0 | 0 | 0 | 0 | 0 | 0 | 4,096 / 62,259 / 70,451 | — | 892 | 23,607 | 220 |
| SQLite | 1 | 1 | 100 | 8,615 | 0 | 211 | 72 | 71 | 69 | 146,763 | 8,602 / 199,885 / 301,466 | 5,530 / 8,602 / 9,830 | 936 | 17,533 | 216 |
| SQLite | 8 | 0 | 1 | 4,592 | 0 | 0 | 0 | 0 | 0 | 0 | 59 / 512 / 498,074 | — | 2,080 | 28,479 | 220 |
| SQLite | 8 | 8 | 1 | 3,013 | 0 | 365 | 120 | 123 | 123 | 257,359 | 192 / 6,451 / 786,432 | 28,672 / 34,406 / 40,141 | 2,150 | 19,648 | 220 |
| SQLite | 8 | 0 | 100 | 5,356 | 0 | 0 | 0 | 0 | 0 | 0 | 55,706 / 799,539 / 1,232,077 | — | 892 | 23,478 | 218 |
| SQLite | 8 | 8 | 100 | 3,605 | 0 | 377 | 127 | 122 | 128 | 262,674 | 216,269 / 419,430 / 458,752 | 27,853 / 34,406 / 38,502 | 1,071 | 14,459 | 218 |
| RocksDB | 1 | 0 | 1 | 23,838 | 0 | 0 | 0 | 0 | 0 | 0 | 27 / 563 / 717 | — | 0 | 546 | 638 |
| RocksDB | 1 | 1 | 1 | 18,765 | 0 | 140 | 46 | 47 | 48 | 98,994 | 33 / 576 / 653 | 6,349 / 13,517 / 15,155 | 0 | 610 | 752 |
| RocksDB | 1 | 0 | 100 | 36,423 | 0 | 0 | 0 | 0 | 0 | 0 | 2,662 / 3,686 / 5,120 | — | 0 | 339 | 794 |
| RocksDB | 1 | 1 | 100 | 32,286 | 0 | 103 | 34 | 35 | 34 | 72,380 | 3,072 / 3,891 / 5,734 | 12,493 / 16,794 / 25,395 | 0 | 399 | 827 |
| RocksDB | 8 | 0 | 1 | 84,530 | 1,564 | 0 | 0 | 0 | 0 | 0 | 69 / 486 / 576 | — | 1 | 294 | 878 |
| RocksDB | 8 | 8 | 1 | 51,848 | 880 | 255 | 81 | 86 | 88 | 182,252 | 102 / 614 / 794 | 36,864 / 68,813 / 77,005 | 1 | 358 | 935 |
| RocksDB | 8 | 0 | 100 | 43,501 | 3,044 | 0 | 0 | 0 | 0 | 0 | 13,107 / 88,474 / 176,947 | — | 4 | 386 | 943 |
| RocksDB | 8 | 8 | 100 | 34,448 | 2,353 | 290 | 97 | 95 | 98 | 202,798 | 17,613 / 93,389 / 127,795 | 32,768 / 54,067 / 55,706 | 4 | 344 | 976 |
| Garnet | 1 | 0 | 1 | 9,204 | 0 | 0 | 0 | 0 | 0 | 0 | 93 / 179 / 410 | — | 5,813 | 573 | 1,862 |
| Garnet | 1 | 1 | 1 | 887 | 0 | 38 | 12 | 13 | 13 | 26,828 | 102 / 40,960 / 114,688 | 2,611 / 121,242 / 498,074 | 312,755 | 789 | 2,774 |
| Garnet | 1 | 0 | 100 | 40,524 | 0 | 0 | 0 | 0 | 0 | 0 | 1,638 / 18,022 / 20,890 | — | 4,941 | 514 | 2,766 |
| Garnet | 1 | 1 | 100 | 4,632 | 0 | 33 | 11 | 11 | 11 | 23,204 | 1,792 / 127,795 / 498,074 | 7,578 / 137,626 / 498,074 | 63,755 | 575 | 2,844 |
| Garnet | 8 | 0 | 1 | 39,188 | 0 | 0 | 0 | 0 | 0 | 0 | 122 / 288 / 15,155 | — | 5,804 | 554 | 2,828 |
| Garnet | 8 | 8 | 1 | 0 | 0 | 162 | 51 | 55 | 55 | 115,433 | — | 18,432 / 242,483 / 786,432 | 7,553,963 | — | 5,511 |
| Garnet | 8 | 0 | 100 | 70,469 | 0 | 0 | 0 | 0 | 0 | 0 | 6,349 / 54,067 / 124,518 | — | 4,920 | 512 | 5,613 |
| Garnet | 8 | 8 | 100 | 0 | 0 | 177 | 58 | 59 | 60 | 124,531 | — | 17,203 / 216,269 / 2,516,582 | 8,067,790 | — | 4,769 |

**Garnet, 8 writers and 8 readers:** its writers committed no message during the 15 s of the measure, whatever the batch: its readers held them. Not explained here.

## 12. The checks

Every check passed, out of the measure: each read found its record; each reopen counted its 1,000,000 records, Garnet's too, which logged errors of the scan of its AOF at its reopen and recovered from the metadata of its last commit; the devices of M counted the events committed, the warm-ups included, in every engine; each snapshot of a site of Q found its totals equal to the sums of its 1,000 sensors; in GBO, the update of a device concurrent with its removal failed.
