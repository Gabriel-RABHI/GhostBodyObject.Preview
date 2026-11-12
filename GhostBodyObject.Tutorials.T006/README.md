# Tutorial T006: Records and RecordBags

The sixth level of the tutorials of GBO, for a developer who did [Tutorial T005](../GhostBodyObject.Tutorials.T005/README.md). It shows the Records: values of several fields, without an identity, that entities hold as properties, in bags of Records, and that travel between threads, services and Logs. It shows above all what they are not: a Record never replaces a separate entity.

The domain is a hotel: the guests and their addresses, the stays, their charges, their history and their requests to the services of the hotel, the reviews and their replies, then one restaurant bill written twice, and a booking kept by a Log. Each lesson is a test class that runs without a server or a file: every Store lives in memory.

Run them with:

```bash
dotnet test GhostBodyObject.Tutorials.T006
```

## A Record is a value

| Lesson | Model | What it shows |
|---|---|---|
| [1. Declare a Record](Lesson01DeclareARecord.cs) | `Guests` | `[Record(n, version, max)]`, its class and its View; created outside any transaction, assigned to an entity; the default Record; one type in two entities |
| [2. An entity or a Record](Lesson02AnEntityOrARecord.cs) | `Guests` | Someone or something: a link shares an entity, an assignment copies a Record; a Record lives and leaves with its host |
| [3. Read a Record in place](Lesson03ReadARecordInPlace.cs) | `Guests` | Its View, without a copy nor an allocation; a query by a field of a Record; the View of a Record is a value, unlike the View of an entity: after its transaction, on another thread |
| [4. Change a Record by its copy](Lesson04ChangeARecordByItsCopy.cs) | `Guests` | Replaced whole; `ToBody()`, changed, assigned; the copy stays yours; the same value is no change |
| [5. An optional Record, and a Record in a Record](Lesson05OptionalAndNestedRecords.cs) | `Guests` | `Option<RAddress>`, absent or empty; read straight through, changed level by level |

## The RecordBag

| Lesson | Model | What it shows |
|---|---|---|
| [6. A bag of one type](Lesson06ABagOfOneType.cs) | `Stays` | `RecordBag<RCharge>`, written directly; its forms in the code, `RecordBagView`; `Add`, `Count()`, `Size`, `Enumerate<T>()` and its operators, `TryGetAt` |
| [7. Find and take a Record](Lesson07FindAndTakeARecord.cs) | `Stays` | `Contains`, `IndexOf`, by value and by type; take a Record of a type; take them in their order |
| [8. Change a bag](Lesson08ChangeABag.cs) | `Stays` | `RemoveAt`, `Remove`, `RemoveAll`, `ReplaceAt`, `Clear`; one Record changed by its copy |
| [9. A bag of several types](Lesson09ABagOfSeveralTypes.cs) | `Stays` | `RecordBag<R1, R2, R3>`, GBO050; `Count<T>()`; `Enumerate(h1, h2, h3)`, a handler per type |
| [10. A bag of any Record, the messenger](Lesson10ABagOfAnyRecord.cs) | `Stays` | `RecordBag`; each service reads its types and leaves the others; a Record that carries a bag; distinct numbers for what travels together |
| [11. A free bag, and the copies of a bag](Lesson11AFreeBag.cs) | `Stays` | `new RecordBag<T>()`, filled then assigned; the View of a bag, a value; its `ToBody()`; the types taken off and put back |
| [12. Records at any depth](Lesson12RepliesOfReplies.cs) | `Reviews` | A bag of its own type; read at any depth; written below the entity by a copy and `ReplaceAt` |

## When not to use a Record

| Lesson | Model | What it shows |
|---|---|---|
| [13. What a Record is not](Lesson13WhatARecordIsNot.cs) | `Bills` | One bill, its lines as Records and as parts: the conflict on the host, the index, the link; what stops the build |
| [14. Fast to read, slow to change](Lesson14FastToReadSlowToChange.cs) | `Bills` | What a change costs, measured; many Records written and changed at once |
| [15. What a Record is for](Lesson15WhatARecordIsFor.cs) | `Bookings` | A message without a Store; one Record, a fact in a Log and a value in a bag |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the document [T006-Records.md](../Documentation/Tutorials/T006-Records.md) takes its code from them.

[Tutorial T007](../GhostBodyObject.Tutorials.T007/README.md) starts from there: the Log, its Records as facts, their subscriptions and their replay.
