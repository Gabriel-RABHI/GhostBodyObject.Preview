# Infrastructure: projects, Stores, durability, distribution, tests

**Prerequisite:** [../SKILL.md](../SKILL.md).

## The project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- The generated code reads the objects through pointers: without it, the error GBO008. -->
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\GhostBodyObject.Runtime\GhostBodyObject.Runtime.csproj" />
    <!-- The generator: an analyzer only; it writes the code of the model at each build and reports GBO001-GBO048. -->
    <ProjectReference Include="..\GhostBodyObject.Generator\GhostBodyObject.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
  <ItemGroup>
    <!-- When the model interfaces are internal: the tests see the generated types. -->
    <InternalsVisibleTo Include="MyDomain.Tests" />
  </ItemGroup>
</Project>
```

- A Repository includes only the model interfaces of **its own project**; a Log only the Records of its project; the indexes of a type live in the project of that type. A Record may hold a `*Ref`, an enum or a converter of another assembly; a stored struct must be declared in the project of the model that stores it.
- A generated type takes the accessibility of its interface (`[Internal]` forces `internal`); a Repository is public only when its interface and all its types are.
- A Repository or a Log with a model error gets no generated code: fix the GBO errors first; GBO013 and GBO014 appear once the model compiles.
- Little-endian platforms only.

## Stores

| Factory (`TransactionalMemoryStore`, `GhostBodyObject.Runtime.Physical`) | Kind |
|---|---|
| `NewInMemoryRepositoryStore(string name, StoreAccessMode mode)` | Repository Store, volatile |
| `NewFileRepositoryStore(string directory, string name, StoreAccessMode mode)` | Repository Store, persistent |
| `NewInMemoryLogStore(string name, StoreAccessMode mode)` | Log Store, volatile |
| `NewFileLogStore(string directory, string name, StoreAccessMode mode)` | Log Store, persistent |

- **An in-memory Store lives by its name in the process**: every instance opened with one name shares its data while one of them is open; when the last is disposed, the data is gone. Tests give each Store a unique name.
- **A Store on disk** is append-only Segments in its directory, mapped read-only (a stray write of the application cannot corrupt it); each transaction carries its hash, checked at the opening: an interrupted transaction is discarded, nothing to redo. The same Store can be opened by several processes of one machine, each with its own Repository, without duplicating the data in memory, all of them writing.
- `StoreAccessMode.ReadWrite` allows transactions that write; `ReadOnly` a Store (or a replica) that is only read.
- The constructor of a Repository opens its first transaction to bind its model: **every policy is set before it**, on the master Store; a later set throws.
- `store.CurrentEpoch`: the number of the last transaction the Store holds.

## Durability

`store.FlushPolicy` (`GhostBodyObject.Runtime.Physical.Segments.FlushPolicy`), on the master Store before the first Repository:

| Policy | A commit is flushed | For |
|---|---|---|
| `Durable` (default) | At each commit, before it returns | Each commit must survive a crash of the process |
| `DurableOnDisk` | At each commit, and `FlushFileBuffers` on Windows (`msync(MS_SYNC)` on Linux already reaches the device) | Each commit must survive a power loss |
| `Fast`, `FastOnDisk` | Within 1 ms or 8 MB (group commit) | High throughput, a loss window of about a millisecond |
| `Paged`, `PagedOnDisk` | Within 10 ms or 64 KB | |
| `Batched`, `BatchedOnDisk` | Every 256 transactions, 1 MB or 10 ms | |
| `Delayed`, `DelayedOnDisk` | Within 100 ms or 1 MB | |
| `Manual` | When the code asks | |

Another process, a replica or a subscription sees a commit once it is flushed. `policy with { OnDisk = true }` adds the device flush to any policy.

`store.LockPolicy` (`StoreLockPolicy`): `ImmediateRelease` (default: the lock file serializing the commits of the processes is released after each commit), `DelayedRelease` (after `LockReleaseDelay`, 10 ms by default), `Retain` (kept: the fastest when this process is the only writer).

## Chains

Several Stores that commit as one: `master.AddChildStore(child)` (returns the child) before the first transaction, then `masterRepository.Chain(childRepository)` and `childRepository.Chain(log)` in the same order. Details in [logs-chains.md](logs-chains.md). `store.RebuildFromLog()`, `store.IsBehindLog`, `store.LogTarget` for the models rebuilt from their Log.

## Distribution

Each server holds a whole replica of a Store, the same name in its own directory; an endpoint connects the servers.

```csharp
using var endPoint = new StoreDistributionEndPoint(DistributionModel.P2P, new StoreDistributionEndPointConfiguration
{
    ListenPort = 7000,                                   // 0 (the default) lets the system choose; ListenAddress "0.0.0.0" by default
    Others = [new DistributionEndPoint("10.0.0.1", 7000)] // the seeds: it joins through them, discovers the others by gossip
});                                                      // also: AdvertisedAddress, AdvertisedPort, NodeName, HeartbeatInterval (250 ms), DeadTimeout (5 s)
using var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "agenda", StoreAccessMode.ReadWrite);
endPoint.Add(store);                                     // before its first transaction; on the first Store of a chain: the whole chain
using var agenda = new AgendaRepository(store);
// The code of the transactions does not change.
```

| Fact | Consequence |
|---|---|
| Each commit is copied to every replica, in the order of the commits, verified by its hash | A damaged or diverging replica stops; it is never replicated |
| A replica `ReadOnly` reads the last commit it holds; it never waits for the copy | Read-only front ends, reports, caches, as many as needed; a ReadWrite transaction on it throws |
| Replicas `ReadWrite` all write; their commits form **one sequence**, the same everywhere | One **write token** per chain: a commit takes it, the owner hands it over in the order of the requests; a writer alone keeps it and commits without a round trip |
| A transaction opens on the last commit its replica holds | A server that changes what another just changed waits for that commit first, or conflicts |
| Conflicts and unique values are guaranteed cluster-wide | `TransactionConflictKind.Change`, `Uniqueness` across servers |
| An Exclusive transaction is exclusive on the whole cluster | No server commits while it is open |
| A chain is distributed as a whole; each server holds the Stores it needs | A writer the whole chain; a site the Read Store alone (`ReadOnly`); a notifier the Log alone, followed by a subscription |
| `endPoint.GetStatus(store)`: `IsWritable`, `OwnerEndPointId`, `TokenGeneration`; `endPoint.EndPointId`, `KnownEndPoints`, `ListenPort` | Where the token is |
| A server leaves by disposing its Repositories, its Stores, then its endpoint | Its token goes to another writer, or is released; it leaves once another writer holds its last commit |
| A server joins at any time with an empty directory | Its replica receives the whole Store, then follows |

**Limits**: one writer at a time per chain (writers scale by splitting the data into several chains); no automatic fail-over yet: if the owner of the token is lost, the writes of its chain stop (`WriteTokenUnavailable`) and the reads go on; two nodes that both wrote while apart hold two histories, and both stop when they meet, rather than merge. No WAN/Hub model yet. A test that waits for a replica polls `replica.CurrentEpoch < source.CurrentEpoch` with a short delay, never a fixed sleep.

## Tests

```csharp
// TestOrder.cs: the tests that open Stores run one after the other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

- In-memory Stores run the same engine as the persistent ones: no database, no mock; a unique name per test (`$"orders-{Guid.NewGuid():N}"`).
- A test project without its own model needs no `AllowUnsafeBlocks`; reference the Generator as an analyzer to get GBO013 and GBO014 on the test queries too; `using static MyDomain.XRepositoryCollections;` to see the collections.
- Concurrency: one `Task.Run` per transaction (each its own execution context), the steps ordered by `TaskCompletionSource` signals, never by delays.
- Subscriptions: `log.Store.DeliverTransactions(subscription)` delivers on the test thread what the subscription has not received yet.
- Persistence: a directory per test, deleted before the run; after disposing the Stores, a folder is deleted after `GC.Collect()` (the Segments are unmapped by the GC), with retries.
- Restart: close everything, reopen with the same directory and the same names (a volatile Store before a persistent Log keeps its name), check `IsBehindLog`, replay, compare snapshots.
- Mutation Sourcing: post and wait; never write the models directly ([mutation-sourcing.md](mutation-sourcing.md)).
- Run a test project: `dotnet test MyDomain.Tests`.
