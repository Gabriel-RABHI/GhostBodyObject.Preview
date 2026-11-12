# Tutorial T005: Indexes and custom collections

The fifth level of the tutorials of GBO, for a developer who did [Tutorial T004](../GhostBodyObject.Tutorials.T004/README.md). It shows the indexes: the text indexes, which find the entities by the words of their texts, and the value indexes, which find them by their values, a number, a date, a status, a reference, alone or together; the unique values; the results of the indexes combined; and the collections that you write yourself over the indexes, always up to date, without anything stored.

The domain is a coffee shop: its products and their brands, found by their names and their descriptions; then its orders and its customers, found by their numbers, their dates, their statuses, their totals, their options, their emails. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. A model is declared at the top of the first lesson that uses it, in a namespace of its own; each index is declared in the lesson that shows it. Every value of a model is a value object, found by its indexes as the value inside it: those that the models share are declared at the head of the first one, in the namespace of the tutorial, the others at the head of their model.

Run them with:

```bash
dotnet test GhostBodyObject.Tutorials.T005
```

## Text indexes

| Lesson | Model | What it shows |
|---|---|---|
| [1. Find by the words of a text](Lesson01FindByTheWordsOfAText.cs) | `Products` | `[TextIndex(n)]`, an interface starting with `M`; `[TextIndexed]` on a property; `Search`, `Count`; a word whatever its case and its accents; every word required; `Search<Product>`, the Bodies to change; up to date at the next instruction |
| [2. The language of a query](Lesson02TheLanguageOfAQuery.cs) | `Products` | `+word`, an alternative; `-word`, an exclusion; exclusions alone; `Name:word`; the wildcards `*` and `?`; `TextSearch.Fuzzy`; `Terms(prefix)`, the words of a search box |
| [3. One index for several types](Lesson03OneIndexForSeveralTypes.cs) | `Catalog` | Products and brands in one index; `Search<ProductView>`, `Search<BrandView>`, `Search<Brand>`; the words of both types |
| [4. The most relevant first, and the rules of an index](Lesson04RankAndRules.cs) | `Catalog` | `TextSearch.Ranked`; `KeepAccents`, `Separators`; `TextIndexing.Commit`, a long text cut once |

## Value indexes

| Lesson | Model | What it shows |
|---|---|---|
| [5. Find by a value](Lesson05FindByAValue.cs) | `Shop` | `[ValueIndex]`, `[ValueIndexComponent]`; `Is`, `From`, `Before`, `Between`, `Range`; `Every`; in the order of the values, and `Descending()`; `Count()`, `Bodies()` |
| [6. A composite index](Lesson06ACompositeIndex.cs) | `Shop` | The status then the day, `DatePrecision.Day`; `Is` fixes a component, the struct of the next one; a day, an instant inside a day; the components in their order |
| [7. Round the values](Lesson07RoundTheValues.cs) | `Shop` | `Decimals`, `Step`: a smaller index, an exact result; `DatePart`, the birthdays of a day whatever the year |
| [8. Flags and absent values](Lesson08FlagsAndAbsentValues.cs) | `Shop` | A `[Flags]` enum: `Is`, `Has`, `One`, `All`, `None`; an `Option<T>`: `IsNone()`, never in a range |
| [9. A unique value](Lesson09AUniqueValue.cs) | `Shop` | `Unique = true`; `IgnoreCase`; `GhostUniquenessException`, the previous value kept; an absent value never refused; a value taken meanwhile, `TransactionConflictKind.Uniqueness` |

## Custom collections

| Lesson | Model | What it shows |
|---|---|---|
| [10. Combine the results](Lesson10CombineTheResults.cs) | `Shop` | `And`, `Or`, `AndNot`, of value indexes and of a text search; a combination is a set; its Bodies |
| [11. Your own collections](Lesson11YourOwnCollections.cs) | `Shop` | `customer.Orders`, a member of your partial class over an index: up to date, ordered, counted, nothing stored; on the View, in a query; `Backlog.Late(day)`, a search of the shop named once; a collection of the model, or of an index |
| [12. The indexes and the transactions](Lesson12IndexesAndTransactions.cs) | `Shop` | A rollback; a commit seen by the others; writers in parallel on the same values; a restart |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
