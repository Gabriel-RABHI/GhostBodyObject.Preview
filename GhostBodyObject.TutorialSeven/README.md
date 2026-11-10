# Tutorial Seven: the Log and the Records

The seventh level of the tutorials of GBO, for a developer who did [Tutorial Six](../GhostBodyObject.TutorialSix/README.md). It shows the Log: the Records, the facts that it keeps in the order they happened and never changes; its subscriptions, which read it from any point and follow its new commits; and its replay, which rebuilds a model from it, in memory at each opening or on disk on request.

The domain is a parcel carrier: the parcels registered, scanned at the depots, delivered, first as a journal of Records alone, then as a model of the parcels, chained with the Log of what happened to them. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. A model is declared at the top of the first lesson that uses it, in a namespace of its own. Every value of a model is a value object, of a Record as of an entity: those that the models share are declared at the head of the first one, in the namespace of the tutorial, the others at the head of their model.

Run them with:

```bash
dotnet test GhostBodyObject.TutorialSeven
```

## Records and the Log

| Lesson | Model | What it shows |
|---|---|---|
| [1. Declare a Record and a Log](Lesson01DeclareARecordAndALog.cs) | `Journal` | `[Record(n, version, max)]`, `[GhostLog]`, `[IncludeRecordModel]`; `NewInMemoryLogStore`; `log.OpenTransaction()`, `Add()`, `Commit()`; nothing appended without a commit; `ParcelCollection`, the Records of the transaction; one transaction at a time |
| [2. Read the Log](Lesson02ReadTheLog.cs) | `Journal` | `SubscribeFrom(0, token)`, `Handle<TView, TState>`, `Run()`; the order of the Log, the number of a transaction; a type without handler skipped; `Cancel`, `Completion`; a View kept after its delivery |
| [3. The life of a Record](Lesson03TheLifeOfARecord.cs) | `Journal` | Created anywhere, on any thread, detached; the order of the `Add`; frozen once added; added once; `Add().ToView()` |
| [4. The values of a Record](Lesson04TheValuesOfARecord.cs) | `Journal` | An enum, a decimal, a `GhostSpan<int>`, a struct of an explicit layout, an `Option<T>`, a `GhostString`; read in place by the View; the defaults; the arrays frozen |
| [5. A Log on disk](Lesson05ALogOnDisk.cs) | `Journal` | `NewFileLogStore`; the Records over a restart; the new commits after them |

## Subscriptions

| Lesson | Model | What it shows |
|---|---|---|
| [6. Follow the Log](Lesson06FollowTheLog.cs) | `Journal` | The replay, then each new commit on the thread of the subscription; `OnReplayEnd`; the end by the token, by the Log disposed; the replay of an empty Log |
| [7. Frame the transactions](Lesson07FrameTheTransactions.cs) | `Journal` | `OnTransactionStart`, `OnTransactionEnd`, a transaction applied as a whole; `SubscribeFrom(n)`, after the last transaction handled; a start that cancels, the state at a known point |
| [8. When a handler fails](Lesson08WhenAHandlerFails.cs) | `Journal` | An exception given to `Run`, or to `Completion`; `Cancel` to stop, never an exception; starting again after the last transaction completed |

## The replay

| Lesson | Model | What it shows |
|---|---|---|
| [9. A model and its Log](Lesson09AModelAndItsLog.cs) | `Carrier` | `AddChildStore`, `repository.Chain(log)`; the ambient transaction of the Log; the Records and the entities in one commit, or neither; a `*Ref` in a Record; `Apply(e.Add().ToView())`; the Log opened through its Repository only |
| [10. Replay the Log](Lesson10ReplayTheLog.cs) | `Carrier` | A model in memory, its Log on disk; `IsBehindLog`, `LogTarget`; `OpenTransaction(..., replay: true)`, `Commit(through)`; the transactions of the Log grouped; `ParcelRef.New()` refused in a Replay |
| [11. Rebuild a model](Lesson11RebuildAModel.cs) | `Carrier` | A model on disk, ready at its opening; `RebuildFromLog()`; a change that no Record tells, lost at the rebuild |

## Several Logs

| Lesson | Model | What it shows |
|---|---|---|
| [12. Several Logs on one Store](Lesson12SeveralLogsOnOneStore.cs) | `Journal` | One reader of a Log Store, `journal.Replay(from)`, `log.On(replay)`, `replay.Run()`; the order of the commits, whatever the Log; `repository.Chain(log)` of two Logs, one commit in the order of the `Add` |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
