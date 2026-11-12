# Tutorial T009: transaction conflicts

The ninth level of the tutorials of GBO, for a developer who did [Tutorial T008](../GhostBodyObject.Tutorials.T008/README.md). The levels of conflict, `Optimistic`, `Pessimistic` and `Exclusive`, are in [Tutorial T001](../GhostBodyObject.Tutorials.T001/README.md); this tutorial shows the concurrent changes that need no conflict: the auto-merge of the values that many writers change, `[Delta]`, `[Max]` and `[Min]`, and the explicit merge, by a Merger of your own.

The domain is the box office of a concert hall, where several tills sell at the same time: the counters of a concert, the notice on its door, its posters, the ticket machines of the foyer. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. The model, `Sales`, is declared at the top of Lesson 1, its values first, each a value object; the types of a lesson of Part 2 are declared with their lesson.

Run them with:

```bash
dotnet test GhostBodyObject.Tutorials.T009
```

## Auto-merge

| Lesson | What it shows |
|---|---|
| [1. Add the sales](Lesson01AddTheSales.cs) | A counter of an ordinary entity, which conflicts; `TryCommit(out kind)`; `[Mergeable]`, `[Delta]`; the epoch, the head and the new value; `head + (new - epoch)`; four tills without a conflict |
| [2. The latest and the lowest](Lesson02TheLatestAndTheLowest.cs) | `[Max]`, `[Min]`; a correction that a transaction did not make, kept; rules of merge, not rules of the value |
| [3. Sell no more than the seats](Lesson03SellNoMoreThanTheSeats.cs) | A merge reads no rule of a value; `[Delta(Min = 0)]`; `GhostBoundException` in the Setter; `Bound` at the commit; 500 seats sold exactly to eight tills |
| [4. The other values, and the links](Lesson04TheOtherValuesAndTheLinks.cs) | The rule of the three points; the value as the unit of conflict; two values for one field; a link never merges |

## Explicit merge

| Lesson | What it shows |
|---|---|
| [5. A Merger decides](Lesson05AMergerDecides.cs) | `private bool Merge(NoticeView epoch, NoticeView head)`; the latest message, the furthest stage; called only on a conflict of values; what comes after it |
| [6. When a Merger refuses](Lesson06WhenAMergerRefuses.cs) | `false`, a conflict `Change`; `GhostMergeException`, a Merger that does what it may not |
| [7. A removal that wins](Lesson07ARemovalThatWins.cs) | `[Mergeable(RemovalWins = true)]`; a write dropped, the rest of its transaction committed; both orders; the conflict of a link |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
