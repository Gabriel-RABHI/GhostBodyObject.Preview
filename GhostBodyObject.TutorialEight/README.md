# Tutorial Eight: chains of Stores and distribution

The eighth level of the tutorials of GBO, for a developer who did [Tutorial Seven](../GhostBodyObject.TutorialSeven/README.md). It shows several Repositories on one Store, then the chains of Stores: several models, each in its Store, and their Log, written in one transaction, all or nothing; then the distribution: the same Stores on several servers, read by the replicas, written by several writers, conflicts and unique values guaranteed in the whole cluster, a chain or a part of it on each server, servers that leave and join.

The domain is the parcel carrier of Tutorial Seven: the parcels in one Repository, the tracking that the customers read in another one, the Log of what happened to the parcels after them. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. The model is declared in the first lessons: the two Repositories in Lesson 1, the Log in Lesson 4, the unique index in Lesson 7. Every value of the model is a value object, declared at its head. Every server of the lessons runs in the process of the tests, on the loopback, each in a directory of its own.

Run them with:

```bash
dotnet test GhostBodyObject.TutorialEight
```

## Several models

| Lesson | What it shows |
|---|---|
| [1. Two Repositories on one Store](Lesson01TwoRepositoriesOnOneStore.cs) | Two models bound to one Store, each with its transactions; a number is one type in the Store, of one nature, whatever its name; an index number is one index; a conflict refused at the bind, with nothing written; the Store opened again, the Repositories bound in any order |
| [2. Chain two Repositories](Lesson02ChainTwoRepositories.cs) | `AddChildStore`, `carrier.Chain(site)`; the ambient transaction of the second Repository; two models in one transaction; `TrackingPageRef.Of(parcel)`; the second Repository opened and committed with the first one only |
| [3. All or nothing](Lesson03AllOrNothing.cs) | A rollback; a rule that condemns the transaction of one Store, the commit of the chain refused; a conflict in one Store, the whole commit failed |
| [4. The chain and its Log](Lesson04TheChainAndItsLog.cs) | Three Stores, `AddChildStore(...).AddChildStore(...)`, `site.Chain(log)`; a command and its handlers, both models and the Log in one commit; one replay of both models |

## Distribution

| Lesson | What it shows |
|---|---|
| [5. A replica](Lesson05AReplica.cs) | `StoreDistributionEndPoint`, its seeds; `endPoint.Add(store)`; a replica `ReadOnly`, its Repository opened once it holds the model; `CurrentEpoch`; `GetStatus`, the write token |
| [6. Two writers](Lesson06TwoWriters.cs) | Two replicas `ReadWrite`; the token taken at each commit, `IsWritable`, `TokenGeneration`; a transaction on the last commit its replica holds; writers in parallel, one sequence |
| [7. Conflicts across servers](Lesson07ConflictsAcrossServers.cs) | The same parcel changed on two servers, `TransactionConflictKind.Change`; a unique tracking number, `Uniqueness`, in the whole cluster |
| [8. Distribute a chain, and a part of it](Lesson08DistributeAChain.cs) | The chain distributed by its first Store; a site with the tracking alone; a service of notifications with the Log alone, its subscription fed by the copies |
| [9. Leave and join](Lesson09LeaveAndJoin.cs) | A writer that leaves, its token gone to another writer or released; the others go on; a server that joins later through any member, the whole Store copied; `KnownEndPoints` |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
