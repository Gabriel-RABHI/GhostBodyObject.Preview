# Tutorial Ten: claims, points and leases

The tenth level of the tutorials of GBO, for a developer who did [Tutorial Nine](../GhostBodyObject.TutorialNine/README.md). It shows what neither a commit nor a merge protects: a decision on what a transaction read, by a claim; a rule over the parts of an aggregate, by a concurrency point; and a work outside GBO, a payment, by a lease that holds entities between two transactions.

The domain is the box office of a concert hall, where several tills work at the same time: the halls and their ushers, the baskets and the orders of the customers, the seats. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. The model, `BoxOffice`, is declared at the top of Lesson 1, its values first, each a value object; the types of the orders and of the seats are declared with the first lesson that uses them.

Run them with:

```bash
dotnet test GhostBodyObject.TutorialTen
```

## Claims

| Lesson | What it shows |
|---|---|
| [1. A decision on what was read](Lesson01ADecisionOnWhatWasRead.cs) | Two decisions right on what they read, and a rule broken; `Claim()`, the conflict `Claim`; the work run again |
| [2. What a claim watches](Lesson02WhatAClaimWatches.cs) | The `Pessimistic` level watches a type, a claim one entity; a claim on a View; a change of the entity claimed; a decision that does not claim; a transaction that reads |

## Concurrency points

| Lesson | What it shows |
|---|---|
| [3. A rule over the parts](Lesson03ARuleOverTheParts.cs) | A rule that a basket does not keep; `[ConcurrencyPoint]`, the conflict `Domain`; two domains apart |
| [4. The version of a domain](Lesson04TheVersionOfADomain.cs) | `ConcurrencyVersion`, an ETag, for a client that reads now and writes later |
| [5. What marks a point](Lesson05WhatMarksAPoint.cs) | The nearest point, once per transaction; a move, a mergeable value; points that nest; `Domain` or `Change`; what GBO039 refuses |

## Leases

| Lesson | What it shows |
|---|---|
| [6. Hold a seat](Lesson06HoldASeat.cs) | `TryLock`, `OpenTransaction(lease)`, `Release`; `lease.Id`, the key of the work outside; nothing held without a commit |
| [7. What a lease refuses](Lesson07WhatALeaseRefuses.cs) | `IsLocked`, `GhostLockedException`; the conflict `Lock`; one transaction of a lease at a time |
| [8. Several seats](Lesson08SeveralSeats.cs) | `NewLease`, `TryAdd`, all or none; `lease.Views<TView>()`; a lease that grows |
| [9. The life of a lease](Lesson09TheLifeOfALease.cs) | `Renew`, `Remaining`; the expiry, `LeaseLost`; the fencing of a lease taken by another, or expired during its transaction |
| [10. Leases, points and exclusive transactions](Lesson10LeasesPointsAndExclusiveTransactions.cs) | A lease that holds the domain of a point; `ignoreLocks`, and what it costs a lease |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
