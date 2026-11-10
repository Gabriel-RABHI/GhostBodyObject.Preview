# Tutorial One: the first steps with GBO

The first level of the tutorials of GBO, for a developer who knows the goal of GBO and nothing of its API. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. The model of every lesson, a library of books, is declared in Lesson 1.

Run them with:

```bash
dotnet test GhostBodyObject.TutorialOne
```

| Lesson | What it shows |
|---|---|
| [1. Declare a model](Lesson01DeclareAModel.cs) | The values of the model first, each a value object (`Title`, `Year`, `Money`...), then the model as interfaces; the classes that the generator writes: `Book`, `BookView`, `LibraryRepository` |
| [2. Open a Store](Lesson02OpenAStore.cs) | A Store in memory or on disk, and the Repository bound to it: persistence is a declaration; a Store shared by a farm of servers |
| [3. Create entities](Lesson03CreateEntities.cs) | A transaction that writes, `new Book()`, `Commit()`; the same code on disk, the Store closed and opened again |
| [4. Read entities](Lesson04ReadEntities.cs) | `BookCollection` and `foreach`; the default values, never null |
| [5. Commit, or not](Lesson05CommitOrNot.cs) | `Rollback()`, a transaction closed without commit, a transaction used once |
| [6. Change entities](Lesson06ChangeEntities.cs) | Set a property; one entity is one object in a transaction |
| [7. Remove entities](Lesson07RemoveEntities.cs) | `Remove()` |
| [8. Query](Lesson08Query.cs) | The operators of LINQ, their `static` lambdas and their states; the errors GBO013 and GBO014 |
| [9. An absence is an Option](Lesson09AbsenceIsAnOption.cs) | `FirstOrNone`, `Option<T>`; the operators that throw |
| [10. Read with Views](Lesson10ReadWithViews.cs) | `BookViewCollection`; search with Views, change with Bodies |
| [11. Add your own code](Lesson11AddYourOwnCode.cs) | The partial classes: computed properties, `On...Changed` that keeps a property of the model; never a field of the Body |
| [12. Call your methods in the queries](Lesson12CallYourMethodsInQueries.cs) | Any method, computed property or inner query in a lambda: what Entity Framework cannot translate |
| [13. Work in deep methods](Lesson13WorkInDeepMethods.cs) | Query, change and create from methods at any depth, even inside a query, with no context to pass |
| [14. The rules of a transaction](Lesson14TheRulesOfATransaction.cs) | One transaction per Repository and context; read-only; across `await` |
| [15. Concurrent transactions](Lesson15ConcurrentTransactions.cs) | Writers on several threads at the same time; MVCC: each transaction reads the Store as it was when it opened |
| [16. Transaction conflicts](Lesson16TransactionConflicts.cs) | The same book changed by two transactions: the second commit throws; different books never conflict |
| [17. Pessimistic transactions](Lesson17PessimisticTransactions.cs) | A decision on what a transaction read, which the commit of Lesson 16 does not see; `TransactionConflictLevel.Pessimistic`, the conflict `Collection`; a type watched, not a shelf |
| [18. Exclusive transactions](Lesson18ExclusiveTransactions.cs) | `TransactionConflictLevel.Exclusive`: the right to write taken at the opening, never a conflict; its wait bounded |
| [19. Give each value its type](Lesson19GiveEachValueItsType.cs) | What GBO writes for the value objects of Lesson 1: their rules, `TryFrom` and `From`, their operators in `Sum` and `Max`; a text in disguise read in place, in a state, a `Select`, an `OrderBy`; an amount in cents over a `long`, the fastest |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
