# Tutorial One: The Basics

## GBO in a few words

This series of tutorials explains, step by step, how to use GBO.

Each lesson is a test class of the project `GhostBodyObject.TutorialOne`. Read it here, run it, then change it and see what breaks.

### The Tutorial Series

GBO is a sophisticated, production-oriented software technology. Each tutorial introduces a set of features and use case.

- **Tutorial One**: the basics, an overview.
- **Tutorial Two**: object relationships, constraints and deterministic garbage collection.
- **Tutorial Three**: Entity Component System and object composition model.
- **Tutorial Four**: Edges and graph oriented model.
- **Tutorial Five**: indexes and custom collections.
- **Tutorial Six**: the Log and Record types, subscription and replay mode.
- **Tutorial Seven**: the Store chains, the cross-Store transactions, and distribution.
- ----- Next :
- **Tutorial X**: managing the evolution of the model, and the migration policies.
- **Tutorial X**: the distribution models, the ACID configurations, and the large-scale distribution of data over the Internet with the Hub.
- **Tutorial X**: architectural concepts with CRUD, Commands, Mutation Sourcing, Event Sourcing, CQRS.
- **Tutorial X**: the Replay migration concept, and rolling server updates with failover.
- **Tutorial X**: managing external, asynchronous services with the outbox pattern.
- **Tutorial X**: reaching hundreds of millions of operations per second on a single server.
- **Tutorial X**: the tooling, to explore and watch the data, and to manage the infrastructure.

## What you will build

A small library of books. One entity type, `Book`, and one Repository, `LibraryRepository`, are enough to discover the basic API principles : declare a model, open a store, changing data, querying, run code and manage transactions.

## Part 1: Your first data

### Lesson 1: Declare a model

A model is a set of C# interfaces. You never implement them: the generator reads them and writes the classes. An interface whose name starts with `M` gives a class without the `M`: `MBook` gives `Book`.

```csharp
[Entity(1, 1, 5)]
[Root]
[Removable]
[Enumerable]
internal interface MBook
{
    // [Property(n)]: the identifier of the property. GBO stores the value under its identifier, not under its name: you may
    // rename a property, but never give its identifier to another one.
    [Property(1)]
    string Title { get; set; }

    [Property(2)]
    string Author { get; set; }

    [Property(3)]
    int Year { get; set; }

    [Property(4)]
    decimal Price { get; set; }

    [Property(5)]
    bool Borrowed { get; set; }
}
```

| Attribute           | Meaning                                                                                                                                                      |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `[Entity(1, 1, 5)]` | The type identifier of `Book` (1, unique among the types of the Repository), the version of its model (1), and the highest identifier of its properties (5). |
| `[Root]`            | A book lives by itself: GBO never removes it on its own.                                                                                                     |
| `[Removable]`       | Your code may remove a book, with `book.Remove()`.                                                                                                           |
| `[Enumerable]`      | The generator writes `BookCollection`, which enumerates the books.                                                                                           |
| `[Property(n)]`     | The identifier of the property.                                                                                                                              |

> **Warning: Identifiers, not names**
>
> GBO stores a value under the identifier of its property, not under its name. You may rename a property; never give its identifier to another one. Version and Properties number permit to transparently read and write old objects models from the current one, on new unknown one too. It permit to check next versions definition mistakes too.

The Repository gathers the entity types:

```csharp
[GhostRepository]
[IncludeEntityModel(nameof(MBook))]
internal interface MLibraryRepository
{
}
```

From these two interfaces, the generator writes `LibraryRepository`, its transaction `LibraryRepositoryTransaction`, and the collections of its types. The first test only looks at what it wrote:

```csharp
[Fact]
public void TheGeneratorWritesTheTypesOfTheModel()
{
    // Book is a class: the object you create, read and change. GBO calls it a Body.
    Assert.True(typeof(Book).IsClass);

    // BookView is a struct: a read-only view of a book, to read many books without creating an object for each one.
    // Lesson 10 shows it.
    Assert.True(typeof(BookView).IsValueType);

    // The type identifier of the model is on the class.
    Assert.Equal(1, Book.TypeId);

    // LibraryRepository is the class of the Repository; Lesson 2 opens one.
    Assert.Equal("LibraryRepository", LibraryRepository.Model.Name);
}
```

> **Tip: The project**
>
> A project with a model references the Runtime of GBO and its generator, and allows unsafe code: the generated code reads the objects through pointers. Without `AllowUnsafeBlocks`, the build stops on the error GBO008.

### Lesson 2: Open a Store

Two objects work together. The **Store** keeps the data. The **Repository** binds your model to a Store: it knows `Book` and `BookView`, and opens the transactions that read and write them.

```csharp
[Fact]
public void OpenAStoreInMemoryAndItsRepository()
{
    // A Store in memory keeps its data in the memory of the process: nothing remains when the process stops. It is
    // known by its name in the process: each lesson gives its own. StoreAccessMode.ReadWrite allows transactions that
    // write.
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02", StoreAccessMode.ReadWrite);

    // The Repository of the model, generated from MLibraryRepository, bound to the Store.
    using var library = new LibraryRepository(store);

    // Nothing is read or written out of a transaction: BookCollection, the books of the Repository, refuses to count
    // them. Lesson 3 opens the first transaction.
    Assert.Throws<InvalidOperationException>(() => BookCollection.Count());
}
```

A Store in memory keeps its data in the memory of the process: nothing remains when the process stops. It is known by its name in the process. `StoreAccessMode.ReadWrite` allows the transactions that write.

Now the same on disk. Look closely: **only the line that creates the Store changes.**

```csharp
[Fact]
public void OpenAStoreOnDiskAndItsRepository()
{
    // The directory of the Store, next to the tutorial. It stays after the lesson: look at the files that GBO writes
    // there. The next run deletes it first.
    string directory = Path.Combine(AppContext.BaseDirectory, "lesson-02-library");
    if (Directory.Exists(directory))
        Directory.Delete(directory, true);

    // The only difference with a Store in memory: a directory, and the name of the Store in it.
    using var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadWrite);

    // The same Repository, on a Store on disk.
    using var library = new LibraryRepository(store);

    Assert.True(Directory.Exists(directory));
}
```

> **Tip: Persistence is a declaration**
>
> In memory, the data lasts as long as the process; on disk, it is there again when the Store opens again, in this process or in another one. The Repository, the transactions and every line that reads or writes the books stay the same.
>
> A Store can also be shared, and distributed over a farm of servers: hundreds of servers can share the same storage space, transparently. There again, your code does not change.

### Lesson 3: Create entities

Every read and every write happens in a transaction. The Repository opens it, and it becomes **the transaction of this Repository in the current execution context**, until it is closed: you never pass it to the objects. `new Book()` creates a book in it; `Commit()` writes the books in the Store.

```csharp
[Fact]
public void CreateBooksAndCommitThem()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-create", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);

    // A transaction that writes: StoreAccessMode.ReadWrite.
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // A new Book belongs to the transaction open in this context: there is nothing else to call.
        new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
        new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };

        // The first enumeration of the collection: the new books are already there. In Entity Framework, a query would not
        // find them before SaveChanges(); in GBO, there is nothing to save first: every change is visible at once.
        Assert.Equal(2, BookCollection.Count());

        // Commit writes the changes of the transaction in the Store, and closes the transaction.
        transaction.Commit();
    }

    // A transaction that reads: StoreAccessMode.ReadOnly. It sees what was committed before it opened.
    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(2, BookCollection.Count());
    }
}
```

> **Note: Coming from Entity Framework?**
>
> GBO is not a Unit of Work. In Entity Framework, a `DbContext` collects your changes and sends them to the database at `SaveChanges()`: until then, a query on a `DbSet` asks the database, does not find the entities you added, and filters on the values stored before your changes.
>
> In GBO, the transaction is the memory you work in: each creation, change or removal is visible at once to every read and every query of the transaction, as in a collection of .NET objects. `Commit()` makes the changes durable, and visible to the transactions that open after it.

On disk, exactly the same code makes the books persistent. The Store is closed, then opened again:

```csharp
[Fact]
public void FindTheBooksAfterTheStoreReopens()
{
    string directory = Path.Combine(AppContext.BaseDirectory, "lesson-03-library");
    if (Directory.Exists(directory))
        Directory.Delete(directory, true);

    // The Store on disk of Lesson 2, and exactly the code of the test above.
    using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadWrite))
    using (var library = new LibraryRepository(store))
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
        new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
        transaction.Commit();
    }

    // The Store is closed: the books are in the files only. Opened again, here to read only, it gives them back.
    using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "library", StoreAccessMode.ReadOnly))
    using (var library = new LibraryRepository(store))
    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(2, BookCollection.Count());
    }
}
```

And a book needs a transaction: out of one, `new Book()` throws.

```csharp
[Fact]
public void CreateNothingOutOfATransaction()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-no-transaction", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);

    var error = Assert.Throws<InvalidOperationException>(() => new Book { Title = "Dune" });
    Assert.Equal("No LibraryRepositoryTransaction is open in this context.", error.Message);
}
```

### Lesson 4: Read entities

`BookCollection` enumerates the books of the transaction open in this context. It is a static property, written by the generator for each `[Enumerable]` type and usable anywhere in the project, with `foreach` as with any collection.

```csharp
[Fact]
public void ReadEachBookWithForeach()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-foreach", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
        new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
        new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984 };
        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var lines = new List<string>();
        foreach (var book in BookCollection)
            lines.Add($"{book.Title}, {book.Author}, {book.Year}");

        Assert.Equal(["Dune, Frank Herbert, 1965", "Foundation, Isaac Asimov, 1951", "Neuromancer, William Gibson, 1984"], lines);
    }
}
```

A property never set has its default value. And GBO has no `null` strings: a string never set is empty.

```csharp
[Fact]
public void ReadTheDefaultValues()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-defaults", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);

    var book = new Book();

    // GBO has no null strings: a string never set is empty.
    Assert.Equal("", book.Title);
    Assert.Equal(0, book.Year);
    Assert.Equal(0m, book.Price);
    Assert.False(book.Borrowed);
}
```

## Part 2: Changing data

### Lesson 5: Commit, or not

A transaction writes nothing until `Commit()`. Closed without it, by `Rollback()` or at the end of its `using`, it forgets every change: the Store stays as it was, and on disk, nothing reaches the files.

```csharp
[Fact]
public void ForgetTheChangesOfATransactionNotCommitted()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-forget", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);

    // The end of the using closes the transaction: without Commit, the book is forgotten.
    using (library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune" };
    }

    // Rollback closes it explicitly, with the same effect.
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Foundation" };
        transaction.Rollback();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(0, BookCollection.Count());
    }
}
```

A transaction is used once: committed or rolled back, it is closed. To write again, open a new one.

```csharp
[Fact]
public void CommitATransactionOnce()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-once", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Dune" };

    // An open transaction is Alive.
    Assert.Equal(GhostTransactionStatus.Alive, transaction.Status);

    transaction.Commit();

    // Committed, it is Closed: a second Commit throws. To write again, open a new transaction.
    Assert.Equal(GhostTransactionStatus.Closed, transaction.Status);
    Assert.Throws<InvalidOperationException>(transaction.Commit);
}
```

### Lesson 6: Change entities

In a transaction that writes, a book changes as any object: set its properties. The transaction sees the change at once, **even in a query that filters on the new value**; the Store sees it after the commit.

```csharp
[Fact]
public void ChangeABookAndCommit()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-change", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        new Book { Title = "Foundation", Price = 8.50m };
        transaction.Commit();
    }

    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // First finds the first book that meets the condition. Lesson 8 explains the word "static".
        var dune = BookCollection.First(static book => book.Title == "Dune");
        dune.Price = 12.50m;

        // The transaction sees the change at once, even in a query that filters on the new value, with nothing to save
        // first (Lesson 3); and it finds the same object again.
        Assert.Equal(1, BookCollection.Count(static book => book.Price > 10m));
        Assert.Equal(12.50m, BookCollection.First(static book => book.Title == "Dune").Price);
        Assert.Same(dune, BookCollection.First(static book => book.Title == "Dune"));

        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(12.50m, BookCollection.First(static book => book.Title == "Dune").Price);
        Assert.Equal(8.50m, BookCollection.First(static book => book.Title == "Foundation").Price);
    }
}
```

> **Tip: One entity, one object**
>
> During a transaction, one entity is always one and the same object. The word `static` in the lambdas is explained in Lesson 8.

### Lesson 7: Remove entities

A type marked `[Removable]` has a method `Remove()`. A removed book leaves the collection of its transaction at once, and the Store at the commit.

```csharp
[Fact]
public void RemoveABook()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-remove", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune" };
        new Book { Title = "Foundation" };
        transaction.Commit();
    }

    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        BookCollection.First(static book => book.Title == "Dune").Remove();

        // Gone from the transaction at once: its queries no longer find it, before any commit (Lesson 3).
        Assert.Equal(1, BookCollection.Count());

        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal("Foundation", BookCollection.Single().Title);
    }
}
```

## Part 3: Querying

The next lessons use six books:

```csharp
new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };
```

### Lesson 8: Query with static lambdas

The collections of GBO have the operators of LINQ, written by GBO for them: `Count`, `Where`, `Select`, `OrderBy`, `GroupBy`, `Sum`... Their lambdas follow **one rule: a lambda captures nothing.**

A lambda that reads a variable from outside, a local, a parameter or `this`, makes .NET allocate an object at each call. GBO refuses it at compile time, with the error **GBO013**. The value a lambda needs is passed before it, as its **state**, and reaches it as a parameter, after the item: up to 4 values.

```csharp
[Fact]
public void CountAndTestBooks()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-count", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    // The six books, as listed above.

    Assert.Equal(6, BookCollection.Count());

    // "static" makes sure the lambda captures nothing: the compiler refuses a static lambda that would.
    Assert.Equal(2, BookCollection.Count(static book => book.Author == "Isaac Asimov"));

    // A value from outside is a state: passed before the lambda, received after the book. This line would not compile:
    //     BookCollection.Count(book => book.Year < before);   // error GBO013: the lambda captures "before"
    int before = 1950;
    Assert.Equal(2, BookCollection.Count(before, static (book, year) => book.Year < year));

    // Two values of state, in their order.
    Assert.Equal(3, BookCollection.Count(1950, 1970, static (book, from, to) => book.Year >= from && book.Year < to));

    Assert.False(BookCollection.Any(static book => book.Year > 2000));
    Assert.True(BookCollection.All(static book => book.Price > 5m));
}
```

Filter, project, order and slice. Each operator gives a new collection, read only when it is enumerated:

```csharp
[Fact]
public void FilterOrderAndProjectBooks()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-filter", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    // The six books, as listed above.

    // Where keeps the books that meet the condition; Select computes a value from each one; ToList reads them.
    var recent = BookCollection.Where(1960, static (book, year) => book.Year >= year).Select(static book => book.Title).ToList();
    Assert.Equal(["Dune", "Neuromancer"], recent);

    // OrderBy orders by a key, ThenBy by a second key among equal ones; Take keeps the first ones.
    var byAuthor = BookCollection.OrderBy(static book => book.Author).ThenBy(static book => book.Year).Select(static book => book.Title).ToList();
    Assert.Equal(["Brave New World", "Dune", "1984", "I, Robot", "Foundation", "Neuromancer"], byAuthor);
    var dearest = BookCollection.OrderByDescending(static book => book.Price).Take(2).Select(static book => book.Title).ToList();
    Assert.Equal(["Neuromancer", "Dune"], dearest);
}
```

Aggregate: sums, averages, extremes, groups and dictionaries.

```csharp
[Fact]
public void AggregateBooks()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-aggregate", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    // The six books, as listed above.

    Assert.Equal(51.60m, BookCollection.Sum(static book => book.Price));
    Assert.Equal(8.60m, BookCollection.Average(static book => book.Price));
    Assert.Equal((1932, 1984), (BookCollection.Min(static book => book.Year), BookCollection.Max(static book => book.Year)));

    // MaxBy gives the book of the greatest key, where Max gives the key.
    Assert.Equal("Neuromancer", BookCollection.MaxBy(static book => book.Price).Title);

    // GroupBy gives one group per author, with its key and its books, in the order of their first book.
    var authors = BookCollection.GroupBy(static book => book.Author).Select(static group => $"{group.Key}: {group.Count()}").ToList();
    Assert.Equal(["Frank Herbert: 1", "Isaac Asimov: 2", "William Gibson: 1", "George Orwell: 1", "Aldous Huxley: 1"], authors);

    // ToDictionary finds a book by a key: two books of the same key would throw.
    var byTitle = BookCollection.ToDictionary(static book => book.Title);
    Assert.Equal(1965, byTitle["Dune"].Year);
}
```

> **Warning: No LINQ on a collection of GBO**
>
> `System.Linq` itself is refused on a collection of GBO, with the error **GBO014**: it would allocate too. For an operator that GBO does not have, `AsEnumerable()` leaves GBO explicitly.

### Lesson 9: An absence is an Option

GBO gives no `null`. An operator that may find nothing, `FirstOrNone`, `SingleOrNone`, `MinOrNone`..., gives an `Option<T>`: zero or one item, which you cannot read without handling its absence. An `Option` is a struct: it allocates nothing.

```csharp
[Fact]
public void FindABookOrNone()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-option", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
    new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
    new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };

    Option<Book> dune = BookCollection.FirstOrNone(static book => book.Title == "Dune");
    Option<Book> hamlet = BookCollection.FirstOrNone(static book => book.Title == "Hamlet");

    // Any tells whether the item is there.
    Assert.True(dune.Any());
    Assert.False(hamlet.Any());

    // TryGet gives the item when it is there, as Dictionary.TryGetValue.
    if (dune.TryGet(out var book))
        Assert.Equal(1965, book.Year);

    // GetOr gives the item, or a fallback that you choose: never an implicit default value.
    Assert.Equal(0, hamlet.Select(static found => found.Year).GetOr(0));

    // Match handles both cases.
    Assert.Equal("not in the library", hamlet.Match(static found => found.Title, static () => "not in the library"));

    // Get gives the item, and throws if it is absent: use it when the absence is a bug.
    Assert.Equal("Dune", dune.Get().Title);
    Assert.Throws<InvalidOperationException>(() => hamlet.Get());
}
```

The operators that must find a value throw when there is none, or when there is more than one:

```csharp
[Fact]
public void ThrowWhenAValueIsDueAndMissing()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-throw", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
    new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };

    // First: no book of Shakespeare.
    Assert.Throws<InvalidOperationException>(() => BookCollection.First(static book => book.Author == "William Shakespeare"));

    // Single: two books of Asimov. SingleOrNone throws too: more than one is never "none".
    Assert.Throws<InvalidOperationException>(() => BookCollection.Single(static book => book.Author == "Isaac Asimov"));
    Assert.Throws<InvalidOperationException>(() => BookCollection.SingleOrNone(static book => book.Author == "Isaac Asimov"));

    // Max over no book throws; MaxOrNone gives none.
    var none = BookCollection.Where(static book => book.Year > 2000);
    Assert.Throws<InvalidOperationException>(() => none.Max(static book => book.Year));
    Assert.False(none.MaxOrNone(static book => book.Year).Any());
}
```

### Lesson 10: Read with Views

A `Book`, a Body, is an object: the transaction keeps each one it gives, so that one entity is always the same object. A `BookView` is a **read-only struct**: the transaction keeps nothing of it, and it allocates nothing.

A general rule for performance, memory usage, expressivness and security is this one : **Search with Views, change with Bodies**

To read or search many books, use the Views, `BookViewCollection`. To change the few you found, take their Bodies, with `ToBody()`.

```csharp
[Fact]
public void SearchWithViews()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-search", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
        new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
        new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        // A View has the properties of the book, to read only: it has no setter.
        BookView dune = BookViewCollection.First(static book => book.Title == "Dune");
        Assert.Equal(("Frank Herbert", 1965), (dune.Author, dune.Year));

        Assert.Equal(2, BookViewCollection.Count(10m, static (book, price) => book.Price < price));
        Assert.Equal(["Foundation", "Dune", "Neuromancer"], BookViewCollection.OrderBy(static book => book.Year).Select(static book => book.Title).ToList());
    }
}
```

The Views find the books; only those become Bodies, to be changed. A View reads the current version of its book: it sees the change made through the Body.

```csharp
[Fact]
public void ChangeTheBooksFoundWithTheirBodies()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-change", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        new Book { Title = "Foundation", Price = 8.50m };
        new Book { Title = "Neuromancer", Price = 10.90m };
        transaction.Commit();
    }

    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The Views find the cheap books; only those become Bodies, to be changed.
        foreach (var cheap in BookViewCollection.Where(10m, static (book, price) => book.Price < price).ToList())
            cheap.ToBody().Price += 1m;

        // A View reads the current version of its book: it sees the change made through the Body.
        BookView dune = BookViewCollection.First(static book => book.Title == "Dune");
        Assert.Equal(10.90m, dune.Price);

        // ToView goes the other way, from a Body to its View.
        Book foundation = BookCollection.First(static book => book.Title == "Foundation");
        Assert.Equal(9.50m, foundation.ToView().Price);

        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal([10.90m, 9.50m, 10.90m], BookViewCollection.Select(static book => book.Price).ToList());
    }
}
```

## Part 4: Your code, everywhere

### Lesson 11: Add your own code

`Book` and `BookView` are partial: add computed properties and business rules to them in your own files. GBO stores the properties of the model only; what you add is computed, or lives as long as the object.

```csharp
internal partial class Book
{
    /// <summary>A computed property: GBO does not store it, it is computed at each read.</summary>
    public bool IsClassic => Year < 1960;

    /// <summary>A plain property of the object: GBO does not store it, it lives as long as this Body.</summary>
    public int PriceChanges { get; private set; }

    // For each property of the model, the generator declares a partial method, called after a real change of its value.
    partial void OnPriceChanged() => PriceChanges++;
}

internal readonly partial struct BookView
{
    /// <summary>The same computed property on the View, to use it in the queries over the Views.</summary>
    public bool IsClassic => Year < 1960;
}
```

A computed property is used in a query as any property, and the rule counts the real changes only: the same price written again is not one.

```csharp
[Fact]
public void ComputeAndReactInTheEntity()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-code", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    var dune = new Book { Title = "Dune", Year = 1965, Price = 9.90m };
    new Book { Title = "Foundation", Year = 1951, Price = 8.50m };

    // The computed property, on the Body and on the View, as any property.
    Assert.False(dune.IsClassic);
    Assert.Equal(1, BookCollection.Count(static book => book.IsClassic));
    Assert.Equal("Foundation", BookViewCollection.Single(static book => book.IsClassic).Title);

    // The rule counts the real changes: the same price written again is not one.
    int before = dune.PriceChanges;
    dune.Price = 12.50m;
    dune.Price = 12.50m;
    dune.Price = 13.00m;
    Assert.Equal(2, dune.PriceChanges - before);
}
```

### Lesson 12: Call your methods in the queries

> **Note: A major shift from Entity Framework**
>
> In Entity Framework, a LINQ query on a `DbSet` is not run: it is translated to SQL, and run by the database. A method of your own in a `Where` has no translation, and the query throws when it runs.
>
> In GBO, a query runs in your process, over the objects themselves: its lambda is plain C#. It may call any method, read any computed property, and even run another query.

The collections are ambient: `BookCollection` finds the transaction of the context by itself. A lambda has no context to capture to query the books, so it stays static, and allocates nothing.

```csharp
// A rule of the library, in a plain static method.
private static bool IsBargain(Book book) => book.Price < 8m;

// A method that runs a query itself: it receives no context, no transaction, no collection.
private static int CountBooksOf(string author) => BookCollection.Count(author, static (book, name) => book.Author == name);
```

```csharp
[Fact]
public void CallAMethodInAQuery()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-method", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
    new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
    new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950, Price = 7.90m };
    new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984, Price = 10.90m };
    new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };
    new Book { Title = "Brave New World", Author = "Aldous Huxley", Year = 1932, Price = 7.50m };

    // The method, called in the lambda: Entity Framework could not translate it.
    Assert.Equal(3, BookCollection.Count(static book => IsBargain(book)));

    // A static method can be given to the operator directly: it captures nothing either.
    Assert.Equal(3, BookCollection.Count(IsBargain));

    // The computed property of Lesson 11, a method of the object, in a query too.
    Assert.Equal(["Foundation", "I, Robot", "1984", "Brave New World"], BookCollection.Where(static book => book.IsClassic).Select(static book => book.Title).ToList());
}
```

A query inside a query: through a method that runs it, or written in the lambda, the author passed to the inner lambda as its state.

```csharp
[Fact]
public void RunAQueryInAQuery()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-inner", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
    new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
    new Book { Title = "I, Robot", Author = "Isaac Asimov", Year = 1950 };
    new Book { Title = "Neuromancer", Author = "William Gibson", Year = 1984 };

    // The books of the authors who wrote more than one: for each book, the method counts the books of its author.
    Assert.Equal(["Foundation", "I, Robot"], BookCollection.Where(static book => CountBooksOf(book.Author) > 1).Select(static book => book.Title).ToList());

    // The same inner query, written in the lambda: BookCollection is ambient, and the author goes to the inner lambda
    // as its state.
    var others = BookCollection.Select(static book => $"{book.Title}: {BookCollection.Count(book.Author, static (other, author) => other.Author == author) - 1} other")
        .ToList();
    Assert.Equal(["Dune: 0 other", "Foundation: 1 other", "I, Robot: 1 other", "Neuromancer: 0 other"], others);

    // Each inner query reads the books again: for N books, N inner queries. GBO reads them in memory, fast; for a
    // large collection, GroupBy (Lesson 8) groups them once.
}
```

> **Tip: Mind the cost**
>
> Each inner query reads the books again: for N books, N inner queries. GBO reads them in memory, fast; for a large collection, `GroupBy` (Lesson 8) groups them once.

### Lesson 13: Work in deep methods

In Entity Framework, a method that reads or writes needs the `DbContext`: it receives it as a parameter, or by dependency injection, and its changes wait for `SaveChanges()`. In GBO, the transaction is ambient: **a method at any depth of the calls queries, changes and creates books directly.** It receives nothing, and everything it does is visible at once. The code that opened the transaction keeps the decision to commit.

Here is the end of the year of the library, in methods that call each other, as your services would:

```csharp
private static void CloseTheYear(int year)
{
    DiscountTheBooksBefore(1960, 0.10m);
    ReprintTheBooksBefore(1950, year);
}

// Queries and changes.
private static void DiscountTheBooksBefore(int year, decimal rate)
{
    foreach (var book in BookCollection.Where(year, static (book, before) => book.Year < before))
        book.Price = Math.Round(book.Price * (1 - rate), 2);
}

// Queries and creates.
private static void ReprintTheBooksBefore(int before, int year)
{
    foreach (var book in BookCollection.Where(before, static (book, limit) => book.Year < limit))
        Reprint(book, year);
}

// Creates, deeper again.
private static Book Reprint(Book book, int year) => new() { Title = book.Title, Author = book.Author, Year = year, Price = book.Price };
```

Three levels of calls below the transaction, the methods find it by themselves:

```csharp
[Fact]
public void ChangeTheBooksFromDeepMethods()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-deep", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965, Price = 9.90m };
        new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951, Price = 8.50m };
        new Book { Title = "1984", Author = "George Orwell", Year = 1949, Price = 6.90m };

        // Three levels of calls below, the methods find this transaction by themselves.
        CloseTheYear(2026);

        // Their changes are visible here at once: two books discounted, one reprinted.
        Assert.Equal([9.90m, 7.65m, 6.21m, 6.21m], BookCollection.Select(static book => book.Price).ToList());
        Assert.Equal(2, BookCollection.Count(static book => book.Title == "1984"));

        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(4, BookCollection.Count());
    }
}
```

The lambda of a query is such a method too: it may change the books it reads, or create others.

```csharp
// Change or create, and say so: to be called in the lambda of a query.
private static bool Lend(Book book)
{
    book.Borrowed = true;
    return true;
}

private static bool Reprinted(Book book, int year)
{
    Reprint(book, year);
    return true;
}
```

```csharp
[Fact]
public void ChangeAndCreateBooksInsideAQuery()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-13-query", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
    new Book { Title = "Dune", Author = "Frank Herbert", Year = 1965 };
    new Book { Title = "Foundation", Author = "Isaac Asimov", Year = 1951 };
    new Book { Title = "1984", Author = "George Orwell", Year = 1949 };

    // The lambda lends each old book it meets: the query changes the books while it reads them.
    Assert.Equal(2, BookCollection.Count(static book => book.Year < 1960 && Lend(book)));
    Assert.Equal(["Foundation", "1984"], BookCollection.Where(static book => book.Borrowed).Select(static book => book.Title).ToList());

    // The lambda reprints each book older than 1950, in 2026. The reprint is created during the enumeration, so the
    // same query meets it too, later: it is from 2026, and is not reprinted again.
    Assert.Equal(1, BookCollection.Count(2026, static (book, year) => book.Year < 1950 && Reprinted(book, year)));
    Assert.Equal(4, BookCollection.Count());
}
```

> **Note: Everything is visible at once**
>
> A book created while `BookCollection` is enumerated is enumerated too: there is no Unit of Work, not even inside a query.

## Part 5: Transactions in depth

### Lesson 14: The rules of a transaction

The transaction of a Repository is the one of the current execution context: the code that runs in it, synchronous or after an `await`, finds it without receiving it. Three rules follow.

**1. One transaction of a Repository at a time, in a context.** Transactions do not nest. Other contexts, on other threads, run their own transactions at the same time (Lesson 15).

```csharp
[Fact]
public void OpenOneTransactionAtATime()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-one", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);

    // Out of a transaction, Current is null.
    Assert.Null(LibraryRepositoryTransaction.Current);

    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // Current is the transaction open in this context: the one that new Book() and BookCollection use.
        Assert.Same(transaction, LibraryRepositoryTransaction.Current);

        // A second transaction of the same Repository in the same context throws: close the first one before.
        Assert.Throws<InvalidOperationException>(() => library.OpenTransaction(StoreAccessMode.ReadOnly));
    }

    Assert.Null(LibraryRepositoryTransaction.Current);
}
```

**2. A transaction that reads refuses every change.** Its commit has nothing to write: it closes the transaction, as `Rollback()`.

```csharp
[Fact]
public void ChangeNothingInATransactionThatReads()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-read", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        transaction.Commit();
    }

    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var dune = BookCollection.Single();
        var error = Assert.Throws<InvalidOperationException>(() => dune.Price = 12.50m);
        Assert.StartsWith("The transaction is ReadOnly", error.Message);
        Assert.Throws<InvalidOperationException>(() => new Book { Title = "Foundation" });
        Assert.Throws<InvalidOperationException>(dune.Remove);

        // Commit has nothing to write: it closes the transaction, as Rollback.
        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(9.90m, BookCollection.Single().Price);
    }
}
```

**3. The transaction follows the code across an `await`,** even when it resumes on another thread.

```csharp
[Fact]
public async Task FindTheTransactionAfterAnAwait()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-14-await", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune" };

        // The code resumes after the await, maybe on another thread: the transaction is still the one of its context.
        await Task.Delay(10);
        new Book { Title = "Foundation" };
        Assert.Same(transaction, LibraryRepositoryTransaction.Current);

        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(2, BookCollection.Count());
    }
}
```

### Lesson 15: Concurrent transactions

"One transaction at a time" holds in one execution context only. **GBO runs many transactions at the same time, on different threads: readers and writers together.** A reader never waits for a writer, and writers do not wait for one another while they work; only their commits take their turn, one after the other, for a short moment.

> **Tip: MVCC, in four sentences**
>
> A change creates a new version of its entity, private to its transaction. A commit publishes the new versions of its transaction, all together. A transaction reads the versions that were committed when it opened: the Store as it was then, whatever is committed after. To see the commits made since, open a new transaction.

Each thread below runs in a `Task.Run` of its own, so that each one has its own execution context, and its own transaction. A `TaskCompletionSource` is a signal: one thread sets it, another one waits for it.

```csharp
[Fact]
public async Task ReadTheStoreAsItWasWhenTheTransactionOpened()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-snapshot", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    var readerOpened = new TaskCompletionSource();
    var writerCommitted = new TaskCompletionSource();

    // The reader opens its transaction while the library is empty, then waits for the writer.
    var reader = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly);
        readerOpened.SetResult();
        await writerCommitted.Task;

        // The book was committed after this transaction opened: this transaction does not see it.
        return BookCollection.Count();
    });

    // The writer commits a book while the reader's transaction is open.
    await readerOpened.Task;
    await Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        new Book { Title = "Dune" };
        transaction.Commit();
    });
    writerCommitted.SetResult();

    Assert.Equal(0, await reader);

    // A transaction opened now sees the book.
    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(1, BookCollection.Count());
    }
}
```

Three threads now. The one that looks starts first; then two writers add a book each, at the same time:

```csharp
[Fact]
public async Task WriteFromThreeThreadsAtTheSameTime()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-threads", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);

    // The number of commits of the writers, that the looking thread checks its count against.
    int committed = 0;

    // The thread that looks starts first. It opens a transaction, counts the books, and starts again in a new
    // transaction until it sees both: a transaction sees the commits made before it opened, never those made after.
    var looking = Task.Run(() =>
    {
        while (true)
        {
            bool bothCommitted = Volatile.Read(ref committed) == 2;
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            if (BookCollection.Count() == 2)
            {
                new Book { Title = "Neuromancer" };
                transaction.Commit();
                return;
            }

            // Not yet: this transaction closes at the end of the turn, and the next turn opens a new one. Had both
            // writers committed before it opened, it would have seen their books.
            Assert.False(bothCommitted, "A transaction opened after two commits must see them.");
        }
    });

    // Then the two writers, each in its own thread and its own transaction, at the same time.
    var first = Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        new Book { Title = "Dune" };
        transaction.Commit();
        Interlocked.Increment(ref committed);
    });
    var second = Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        new Book { Title = "Foundation" };
        transaction.Commit();
        Interlocked.Increment(ref committed);
    });

    await Task.WhenAll(looking, first, second);

    // A last transaction sees the three books.
    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(["Dune", "Foundation", "Neuromancer"], BookCollection.Select(static book => book.Title).Order().ToList());
    }
}
```

### Lesson 16: Transaction conflicts

Two concurrent transactions may change the same book: each one changes its own version of it. **The first commit wins.** The second one finds that the book changed since its transaction opened: it throws a `TransactionConflictException`, and writes nothing. GBO never retries by itself: the application decides, with a new transaction, which reads the current version.

```csharp
[Fact]
public async Task RefuseTheSecondChangeOfTheSameBook()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-conflict", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        transaction.Commit();
    }
    var firstChanged = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();

    // The first transaction changes the price, then waits before its commit.
    var first = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.Single().Price = 12.50m;
        firstChanged.SetResult();
        await secondCommitted.Task;

        // The book changed since this transaction opened: its commit throws, and writes nothing.
        Assert.Throws<TransactionConflictException>(transaction.Commit);
    });

    // The second one changes the same price, and commits first: it wins.
    await firstChanged.Task;
    await Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.Single().Price = 11.00m;
        transaction.Commit();
    });
    secondCommitted.SetResult();
    await first;

    // The application decides: here, it applies the change again in a new transaction, which reads 11.00.
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var dune = BookCollection.Single();
        Assert.Equal(11.00m, dune.Price);
        dune.Price = 12.50m;
        transaction.Commit();
    }

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(12.50m, BookCollection.Single().Price);
    }
}
```

Changes of **different books never conflict**: both transactions below are open and have changed their book before the first commit, and both commit.

```csharp
[Fact]
public async Task ChangeTwoBooksAtTheSameTime()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-two-books", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        new Book { Title = "Foundation", Price = 8.50m };
        transaction.Commit();
    }
    var bothChanged = new TaskCompletionSource();
    var firstChanged = new TaskCompletionSource();

    // Both transactions are open, and have changed their book, before the first commit.
    var first = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.First(static book => book.Title == "Dune").Price = 12.50m;
        firstChanged.SetResult();
        await bothChanged.Task;
        transaction.Commit();
    });
    var second = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.First(static book => book.Title == "Foundation").Price = 10.00m;
        await firstChanged.Task;
        bothChanged.SetResult();
        await first;
        transaction.Commit();
    });
    await Task.WhenAll(first, second);

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal([12.50m, 10.00m], BookCollection.Select(static book => book.Price).ToList());
    }
}
```

The level of conflict is chosen when a transaction opens:

| Level                      | What the commit checks                                                                             |
| -------------------------- | -------------------------------------------------------------------------------------------------- |
| `Optimistic` (the default) | A book changed or removed since the transaction opened is a conflict.                              |
| `Pessimistic`              | As `Optimistic`, and a change of any entity of a type that the transaction read is a conflict too. |
| `Override`                 | Nothing: the last commit wins.                                                                     |
| `Exclusive`                | Nothing to check: no conflict can happen (Lesson 17).                                              |

With `Override`, the first transaction below commits after the second one, and replaces its price:

```csharp
[Fact]
public async Task LetTheLastCommitWinWithOverride()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-16-override", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        transaction.Commit();
    }
    var firstChanged = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();

    var first = Task.Run(async () =>
    {
        // The level of conflict, given when the transaction opens.
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Override);
        BookCollection.Single().Price = 12.50m;
        firstChanged.SetResult();
        await secondCommitted.Task;

        // No check: this commit replaces the price that the other transaction committed.
        transaction.Commit();
    });
    await firstChanged.Task;
    await Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.Single().Price = 11.00m;
        transaction.Commit();
    });
    secondCommitted.SetResult();
    await first;

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(12.50m, BookCollection.Single().Price);
    }
}
```

### Lesson 17: Exclusive transactions

A long transaction that changes many books risks a conflict at its commit, and then all its work is to do again. **An exclusive transaction never conflicts:** it takes the right to write when it opens, not when it commits, and keeps it until it closes. It reads the last version of each book, and no other commit can come before its own: the other commits wait for it. The transactions that read do not wait.

> **Warning: Exclusive on the whole farm**
>
> An exclusive transaction is exclusive on the whole farm of servers that share the Store, not on this server only: the right to write that it takes is the one of the farm, and no server commits in the shared storage space while it is open.

It suits the work that must succeed without being replayed: a bulk load, a migration, a maintenance. Its price is its guarantee: while it is open, no server of the farm commits. Keep it short.

```csharp
[Fact]
public async Task CommitAnExclusiveTransactionFirst()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-exclusive", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Book { Title = "Dune", Price = 9.90m };
        transaction.Commit();
    }
    var exclusiveOpened = new TaskCompletionSource();
    var otherChanged = new TaskCompletionSource();

    // The exclusive transaction: TransactionConflictLevel.Exclusive, when it opens.
    var exclusive = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive);
        BookCollection.Single().Price = 12.50m;
        exclusiveOpened.SetResult();

        // It waits here for the other one to change the book too: nothing can be committed before it meanwhile.
        await otherChanged.Task;
        transaction.Commit();
    });
    await exclusiveOpened.Task;

    // Another transaction changes the same book. Its commit waits for the exclusive one to close, then finds the book
    // changed since it opened.
    var other = Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
        BookCollection.Single().Price = 11.00m;
        otherChanged.SetResult();
        Assert.Throws<TransactionConflictException>(transaction.Commit);
    });
    await Task.WhenAll(exclusive, other);

    using (library.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(12.50m, BookCollection.Single().Price);
    }
}
```

An exclusive transaction waits for the right to write while another one holds it. Bound that wait with a `CancellationToken`:

```csharp
[Fact]
public async Task BoundTheWaitOfAnExclusiveTransaction()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-wait", StoreAccessMode.ReadWrite);
    using var library = new LibraryRepository(store);
    var firstOpened = new TaskCompletionSource();
    var firstMayClose = new TaskCompletionSource();
    var first = Task.Run(async () =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive);
        firstOpened.SetResult();
        await firstMayClose.Task;
    });
    await firstOpened.Task;

    // A second exclusive transaction waits for the first one to close; here, 100 milliseconds at most.
    await Task.Run(() =>
    {
        using var patience = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        Assert.ThrowsAny<OperationCanceledException>(() => library.OpenTransaction(StoreAccessMode.ReadWrite, TransactionConflictLevel.Exclusive, cancellationToken: patience.Token));
    });

    // A transaction that reads never waits.
    await Task.Run(() =>
    {
        using var transaction = library.OpenTransaction(StoreAccessMode.ReadOnly);
        Assert.Equal(0, BookCollection.Count());
    });

    firstMayClose.SetResult();
    await first;
}
```

## What you learned

- A **model** is a set of interfaces; the generator writes `Book`, `BookView` and `LibraryRepository`.
- A **Store** keeps the data, in memory, on disk or on a farm of servers: the choice is one line.
- Every read and write happens in an **ambient transaction**, with no Unit of Work: every change is visible at once, and `Commit()` makes it durable.
- **Queries** are the operators of LINQ, with static lambdas and their states: no allocation, and any method of yours inside.
- An absence is an **`Option<T>`**, never `null`.
- **Views** read many entities for free; **Bodies** change the few you need.
- Transactions run **concurrently** under MVCC; a conflict is reported at the commit, and an **exclusive** transaction never meets one.

The next tutorials go further, from where this one stops.
