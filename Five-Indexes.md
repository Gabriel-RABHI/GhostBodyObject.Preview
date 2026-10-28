# Tutorial Five: Indexes

## Indexes and custom collections 

The tutorials before found their entities by their links and by queries over the Views: `BookViewCollection.Where(static book => book.Year < 1960)`. A query reads every entity of its type: about ten milliseconds for a million of them, which is fast, and which grows with the Store. Some questions must not grow with it: the order of a number, among ten million; the twenty most recent orders; the products whose description says "espresso"; the customer of an email, which no other customer may take.

These questions are the work of the **indexes**. An index knows the entities of a value, or of a word, and finds them without reading the others: its cost follows what it finds, not what the Store holds. This tutorial shows how it works:

- **The text indexes.** The words of the texts of your entities, whatever their case and their accents: `ProductTextIndex.Search("coffee -tea")`, with wildcards, typing errors forgiven, the most relevant first.
- **The value indexes.** The values of your entities, alone or together: `OrderNumberIndex.Is(1042)`, `OrderPlacedIndex.Between(monday, tuesday)`, `OrderStatusDayIndex.Is(OrderStatus.Paid).From(day)`; dates at the day, decimals at the euro, flags, absent values.
- **The unique values.** An email given to one customer only, as a constraint `UNIQUE` of SQL, checked at each write and at each commit.
- **The custom collections.** The results of the indexes combined, `paid.And(gifts)`, and the collections that you write yourself over them: `customer.Orders`, always up to date, without anything stored.

None of this takes code of yours: you declare the indexes in the model, and GBO keeps each one up to date with its entities, at each instruction, in the transaction and at the commit.

Each lesson is a test class of the project `GhostBodyObject.TutorialFive`. Read it here, run it, then change it and see what breaks. The tutorial follows Tutorials One to Four: the transactions, the Views and the Bodies, the static lambdas, the references and the partial declarations are used here without being explained again.

## What you will build

A coffee shop. Its products, found by the words of their names and of their descriptions; its catalog, the products and their brands, with the tags of the products. Then its orders and its customers, found by their numbers, their dates, their statuses, their totals, their options, their days of shipping, their emails and their birthdays.

Three models, each in a namespace of its own with its own Repository, as in the tutorials before: `Products` for Lessons 1 and 2, `Catalog` for Lessons 3 and 4, `Shop` for Lessons 5 to 12. In the `Shop`, each index is declared in the lesson that shows it: an index is an interface of the model, wherever it is.

Every declaration of the tutorial, and the lesson that shows it:

| Declaration in the model | What GBO guarantees | Lessons |
| ------------------------ | ------------------- | ------- |
| `[TextIndex(n)]` on `MProductTextIndex` | A text index, of a number stable in its Repository: the class `ProductTextIndex`, its searches and its words | 1, 3 |
| `[TextIndexed(nameof(MProductTextIndex))]` on a property | The words of the property in the index, up to date at the next instruction | 1, 3 |
| `TextIndexing.Commit` | The words of a long text cut once, at the first search or at the commit | 4 |
| `KeepAccents = true`, `Separators = ","` on `TextIndex` | The rules of an index: the accents kept, the words cut at the separators | 4 |
| `[ValueIndex(nameof(MOrder), n)]` and `[ValueIndexComponent(1, nameof(MOrder.Number))]` | A value index: the entities of a value, of a range, in the order of their values | 5 |
| Several `[ValueIndexComponent]` | A composite index, read in the order of its components | 6 |
| `DatePrecision.Day`, `Decimals = 0`, `Step = 1000` | A rounded value: a smaller index, an exact result | 6, 7 |
| `DatePart.MonthOfYear`, `DatePart.DayOfMonth` | The parts of a date: the birthdays, whatever the year | 7 |
| A `[Flags]` enum, an `Option<T>` | Each flag found alone; the absent values found too | 8 |
| `Unique = true`, `IgnoreCase = true` | A value of one entity only, whatever its case, refused at the write and at the commit | 9 |
| A member of your partial declaration over an index | A collection of your own: computed at each read, never stored | 11 |

## Part 1: Text indexes

### Lesson 1: Find by the words of a text

A query by LINQ reads every entity, and every character of their texts: `Description.Contains("coffee")` on a million products reads a million descriptions, and knows neither the words, nor the case, nor the accents. A **text index** knows the words of the properties that it indexes: a search reads the products that have them, whatever the count of the others.

The index is an interface with `[TextIndex(n)]`, its number in the Repository, stable; its name starts with `M`, as a model: `MProductTextIndex` gives `ProductTextIndex`, the class of its searches. Each property of a text names it by `[TextIndexed]`:

```csharp
[TextIndex(1)]
internal interface MProductTextIndex
{
}

[Entity(1, 1, 3)]
[Root]
[Removable]
[Enumerable]
internal interface MProduct
{
    // [TextIndexed]: the words of the property are in the index that it names.
    [Property(1)]
    [TextIndexed(nameof(MProductTextIndex))]
    string Name { get; set; }

    [Property(2)]
    [TextIndexed(nameof(MProductTextIndex))]
    string Description { get; set; }

    [Property(3)]
    decimal Price { get; set; }
}

[GhostRepository]
[IncludeEntityModel(nameof(MProduct))]
internal interface MProductRepository
{
}
```

`ProductTextIndex.Search("coffee")` gives the Views of the products that have the word, in the transaction open in this context; `Count("coffee")` counts them without reading them. A word is found whatever its case and its accents: `cafe` finds `Café`, `medellin` finds `Medellín`. Each word of a query is required: `coffee mug` finds the products that have both.

The shop of the lesson, and the names of the products found:

```csharp
private static ProductRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ProductRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    new Product { Name = "Coffee Mug", Description = "A white mug for your morning coffee.", Price = 9.90m };
    new Product { Name = "Espresso Machine", Description = "Brews a strong coffee in 30 seconds.", Price = 249m };
    new Product { Name = "Tea Pot", Description = "A glass pot for green tea.", Price = 24.50m };
    new Product { Name = "Café de Colombia", Description = "Beans roasted in Medellín.", Price = 12m };
    transaction.Commit();
    return shop;
}

private static List<string> Names(TextSearchResult<ProductView, ProductRepositoryTag> found)
    => found.Select(static product => product.Name).ToList();
```

```csharp
[Fact]
public void FindTheProductsOfAWord()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-word", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

    // The Views of the products that have the word: in the name of the mug, in the description of the machine.
    Assert.Equal(["Coffee Mug", "Espresso Machine"], Names(ProductTextIndex.Search("coffee")));
    Assert.Equal(2, ProductTextIndex.Count("coffee"));

    // A word whatever its case and its accents.
    Assert.Equal(["Café de Colombia"], Names(ProductTextIndex.Search("CAFE")));
    Assert.Equal(["Café de Colombia"], Names(ProductTextIndex.Search("medellin")));

    // Every word of the query is required.
    Assert.Equal(["Coffee Mug"], Names(ProductTextIndex.Search("coffee mug")));
    Assert.Empty(Names(ProductTextIndex.Search("coffee tea")));
}
```

A search gives Views, without creating any Body, as the queries of Tutorial One. To change the products found, `Search<Product>` gives their Bodies. The index is up to date at the next instruction, before the commit: a product created, or changed, is found by its new words at once.

```csharp
[Fact]
public void ChangeTheProductsFound()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-change", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // Search<Product>: the Bodies, to change them.
        foreach (var product in ProductTextIndex.Search<Product>("tea"))
            product.Description += " Keeps the tea warm.";

        // The index is up to date at the next instruction, before the commit: a new product, a new word.
        new Product { Name = "Milk Frother", Description = "Foams the milk of your coffee.", Price = 19m };
        Assert.Equal(3, ProductTextIndex.Count("coffee"));
        Assert.Equal(["Tea Pot"], Names(ProductTextIndex.Search("warm")));
        transaction.Commit();
    }
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(["Tea Pot"], Names(ProductTextIndex.Search("warm")));
        Assert.Equal(["Coffee Mug", "Espresso Machine", "Milk Frother"], Names(ProductTextIndex.Search("coffee")));
    }
}
```

A product removed is no longer found, at the next instruction too:

```csharp
[Fact]
public void ForgetTheProductsRemoved()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-remove", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    ProductTextIndex.Search<Product>("espresso").Single().Remove();
    Assert.Equal(["Coffee Mug"], Names(ProductTextIndex.Search("coffee")));
    Assert.Equal(0, ProductTextIndex.Count("espresso"));
    Assert.Equal(0, ProductTextIndex.Count("chocolate"));
    transaction.Commit();
}
```

### Lesson 2: The language of a query

A query is a list of words. A word alone is **required**; `+word` is an **alternative**, which adds the entities that have it; `-word` **excludes** the entities that have it; a query of exclusions only starts from every entity. `Name:word` looks in one property. The order of the words never changes the result.

The shop of the lesson:

```csharp
private static ProductRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ProductRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    new Product { Name = "Coffee Mug", Description = "A white mug for your morning coffee." };
    new Product { Name = "Espresso Machine", Description = "Brews a strong coffee in 30 seconds." };
    new Product { Name = "Tea Pot", Description = "A glass pot for green tea." };
    new Product { Name = "Tea Mug", Description = "A large mug, for tea or coffee." };
    new Product { Name = "Espresso Cups", Description = "Six small cups." };
    transaction.Commit();
    return shop;
}

private static List<string> Names(TextSearchResult<ProductView, ProductRepositoryTag> found)
    => found.Select(static product => product.Name).ToList();
```

```csharp
[Fact]
public void CombineTheWords()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-words", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

    // An alternative adds; an exclusion takes off.
    Assert.Equal(["Tea Pot", "Tea Mug", "Espresso Cups"], Names(ProductTextIndex.Search("tea +cups")));
    Assert.Equal(["Coffee Mug", "Espresso Machine"], Names(ProductTextIndex.Search("coffee -tea")));

    // Exclusions alone: every product, but those.
    Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("-mug -pot")));

    // The word in one property: "mug" in the name only, not in a description.
    Assert.Equal(["Coffee Mug", "Tea Mug"], Names(ProductTextIndex.Search("name:mug")));

    // The order of the words never changes the result.
    Assert.Equal(Names(ProductTextIndex.Search("coffee -tea")), Names(ProductTextIndex.Search("-tea coffee")));
}
```

`*` stands for any letters, `?` for one: `espr*`, `t?a`. `TextSearch.Fuzzy` forgives a typing error: a word within one letter of the word searched, two for the long words, a letter added, removed, changed, or two letters swapped. `Terms(prefix)` gives the words of the index that start with a prefix, each once, in their order: what a search box proposes while the user types.

```csharp
[Fact]
public void FindTheWordsThatLookAlike()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-alike", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

    // Any letters, and one letter.
    Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("espr*")));
    Assert.Equal(["Tea Pot", "Tea Mug"], Names(ProductTextIndex.Search("t?a")));

    // A typing error: "expresso" for "espresso", "cofee" for "coffee".
    Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("expresso", TextSearch.Fuzzy)));
    Assert.Equal(3, ProductTextIndex.Count("cofee", TextSearch.Fuzzy));
    Assert.Equal(0, ProductTextIndex.Count("cofee"));

    // The words of the index that start with a prefix, each once, in their order.
    Assert.Equal(["coffee", "cups"], ProductTextIndex.Terms("c"));
    Assert.Equal(["machine", "morning", "mug"], ProductTextIndex.Terms("m"));
}
```

| Query | Finds |
| ----- | ----- |
| `coffee mug` | The entities that have both words |
| `tea +cups` | Those of `tea`, and those of `cups` |
| `coffee -tea` | Those of `coffee` that do not have `tea` |
| `-mug -pot` | Every entity but those of `mug` or of `pot` |
| `name:mug` | `mug` in the property `Name` only |
| `espr*`, `t?a` | Any letters, one letter |
| `expresso`, with `TextSearch.Fuzzy` | The words within one or two letters: `espresso` |

### Lesson 3: One index for several types

The properties of several entity types may name the same index: the products and their brands share the catalog index, one search box for both. The second model, the catalog, declares it, with a second index for the tags of the products (Lesson 4):

```csharp
[TextIndex(1)]
internal interface MCatalogTextIndex
{
}

// The rules of an index (Lesson 4): the accents kept, and a term cut at the commas, a tag of several words kept whole.
[TextIndex(2, KeepAccents = true, Separators = ",")]
internal interface MTagTextIndex
{
}

[Entity(1, 1, 4)]
[Root]
[Removable]
[Enumerable]
internal interface MProduct
{
    [Property(1)]
    [TextIndexed(nameof(MCatalogTextIndex))]
    string Name { get; set; }

    // TextIndexing.Commit (Lesson 4): the words of a long text, written many times, are cut once, at the first search or at the commit.
    [Property(2)]
    [TextIndexed(nameof(MCatalogTextIndex), TextIndexing.Commit)]
    GhostString Description { get; set; }

    [Property(3)]
    [TextIndexed(nameof(MTagTextIndex))]
    string Tags { get; set; }

    [Property(4)]
    decimal Price { get; set; }
}

[Entity(2, 1, 2)]
[Root]
[Removable]
[Enumerable]
internal interface MBrand
{
    [Property(1)]
    [TextIndexed(nameof(MCatalogTextIndex))]
    string Name { get; set; }

    [Property(2)]
    [TextIndexed(nameof(MCatalogTextIndex))]
    string Story { get; set; }
}

[GhostRepository]
[IncludeEntityModel(nameof(MProduct))]
[IncludeEntityModel(nameof(MBrand))]
internal interface MCatalogRepository
{
}
```

A search of an index of several types says the type it looks for: `CatalogTextIndex.Search<ProductView>("italian")` gives the products, `Search<BrandView>` the brands, `Search<Brand>` their Bodies. `Terms(prefix)` gives the words of every type of the index.

```csharp
[Fact]
public void SearchEachTypeOfTheIndex()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-types", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Brand { Name = "Lavazza", Story = "An Italian roaster in Turin since 1895." };
        new Brand { Name = "Juan Valdez", Story = "The coffee of the growers of Colombia." };
        new Product { Name = "Lavazza Oro", Description = "Arabica beans, a sweet Italian blend.", Price = 11.90m };
        new Product { Name = "Moka Pot", Description = "The Italian way to brew at home.", Price = 29m };
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        // One word, the entities of each type.
        Assert.Equal(["Lavazza Oro", "Moka Pot"], CatalogTextIndex.Search<ProductView>("italian").Select(static p => p.Name).ToList());
        Assert.Equal(["Lavazza"], CatalogTextIndex.Search<BrandView>("italian").Select(static b => b.Name).ToList());
        Assert.Equal(1, CatalogTextIndex.Search<ProductView>("lavazza").Count());
        Assert.Equal(1, CatalogTextIndex.Search<BrandView>("lavazza").Count());

        // The words of both types.
        Assert.Equal(["coffee", "colombia"], CatalogTextIndex.Terms("co"));
    }
}
```
```csharp
[Fact]
public void ChangeTheEntitiesOfOneType()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-change", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    new Brand { Name = "Lavazza", Story = "An Italian roaster in Turin." };
    new Product { Name = "Lavazza Oro", Description = "An Italian blend." };
    foreach (var brand in CatalogTextIndex.Search<Brand>("turin"))
        brand.Story = "A roaster of Turin, now in Milan too.";
    Assert.Equal(1, CatalogTextIndex.Search<BrandView>("milan").Count());
    Assert.Equal(0, CatalogTextIndex.Search<BrandView>("italian").Count());
    Assert.Equal(1, CatalogTextIndex.Search<ProductView>("italian").Count());
    transaction.Commit();
}
```

### Lesson 4: The most relevant first, and the rules of an index

By default, a search gives its entities in the order of the Store. **`TextSearch.Ranked`** gives the most relevant first: an entity whose texts repeat the words searched, in a short text, before one that names them once in a long one; and a rare word weighs more than a frequent one. It is the rank of the search engines, BM25.

```csharp
[Fact]
public void RankTheMostRelevantFirst()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rank", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Product { Name = "Coffee Mug", Description = "A mug." };
        new Product { Name = "Espresso Machine", Description = "Coffee, coffee, coffee: for coffee lovers." };
        new Product { Name = "Coffee Grinder", Description = "Grinds coffee beans." };
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        // In the order of the Store, then by relevance: the word repeated in a short text first.
        Assert.Equal(["Coffee Mug", "Espresso Machine", "Coffee Grinder"],
            CatalogTextIndex.Search<ProductView>("coffee").Select(static p => p.Name).ToList());
        Assert.Equal(["Espresso Machine", "Coffee Grinder", "Coffee Mug"],
            CatalogTextIndex.Search<ProductView>("coffee", TextSearch.Ranked).Select(static p => p.Name).ToList());

        // "grinder", rare, weighs more than "coffee", which every product has.
        Assert.Equal("Coffee Grinder", CatalogTextIndex.Search<ProductView>("coffee +grinder", TextSearch.Ranked).First().Name);
    }
}
```

An index has its **rules**, given to `[TextIndex]`. The shortest word, two letters by default, `MinLength`, from 2 to 4. `KeepAccents = true`: the accents kept, `été` and `ete` are two words. `Separators = ","`: a word is cut at the commas and the spaces only, so that `fair-trade` stays one word, where the default rules cut it in two. The catalog index keeps the default rules: it folds the accents.

```csharp
[Fact]
public void KeepTheRulesOfAnIndex()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rules", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
    new Product { Name = "Iced Coffee", Tags = "été,fair-trade" };
    new Product { Name = "Hot Chocolate", Tags = "hiver,gift" };

    // The accents kept: "ete" is not "été".
    Assert.Equal(1, TagTextIndex.Count("été"));
    Assert.Equal(0, TagTextIndex.Count("ete"));

    // "fair-trade" is one word: neither "fair" nor "trade" alone.
    Assert.Equal(["Iced Coffee"], TagTextIndex.Search("fair-trade").Select(static p => p.Name).ToList());
    Assert.Equal(0, TagTextIndex.Count("trade"));

    // The catalog index, of the default rules, folds the accents of the names.
    new Product { Name = "Café Glacé" };
    Assert.Equal(1, CatalogTextIndex.Search<ProductView>("glace").Count());
    transaction.Commit();
}
```

A property `TextIndexing.Commit` cuts its words once, at the first search of its index in the transaction, or at its commit: a long description written in many steps costs one cut, and the search of the transaction still sees its last text. By default, `TextIndexing.Immediate`, the words of a property are cut by its Setter.

```csharp
[Fact]
public void CutALongTextAtTheCommit()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-commit", StoreAccessMode.ReadWrite);
    using var catalog = new CatalogRepository(store);
    using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var product = new Product { Name = "Tasting Box" };
        // The description is built in steps: its words are cut once, not at each step.
        for (int day = 1; day <= 5; day++)
            product.Description = $"{product.Description} Day {day}: a new origin.";
        product.Description = $"{product.Description} Day 6: Ethiopia.";

        // The search of the transaction sees the last text.
        Assert.Equal(1, CatalogTextIndex.Search<ProductView>("ethiopia").Count());
        transaction.Commit();
    }
    using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(1, CatalogTextIndex.Search<ProductView>("origin ethiopia").Count());
}
```

> **Tip: Immediate or Commit**
>
> `Immediate`, the default, for the short texts that change once: a name, a title. `Commit` for the long texts that a transaction builds in steps, or rewrites many times: a description, a body, a log. Both modes give the same results; only the moment of the cut changes.

## Part 2: Value indexes

### Lesson 5: Find by a value

The third model, the shop, is the one of the rest of the tutorial: its customers and its orders.

```csharp
internal enum OrderStatus
{
    Pending,
    Paid,
    Shipped,
    Delivered
}

/// <summary>The options of an order, several at once (Lesson 8).</summary>
[Flags]
internal enum OrderOptions
{
    None = 0,
    Gift = 1,
    Express = 2,
    Fragile = 4
}

[Entity(1, 1, 5)]
[Root]
[Removable]
[Enumerable]
internal interface MCustomer
{
    [Property(1)]
    string Name { get; set; }

    [Property(2)]
    string Email { get; set; }

    [Property(3)]
    DateOnly Birthday { get; set; }

    [Property(4)]
    long Points { get; set; }

    // A loyalty card, absent until given (Lesson 9).
    [Property(5)]
    Option<int> Card { get; set; }
}

[Entity(2, 1, 8)]
[Root]
[Removable]
[Enumerable]
internal interface MOrder
{
    [Property(1)]
    int Number { get; set; }

    [Property(2)]
    OrderStatus Status { get; set; }

    [Property(3)]
    DateTime Placed { get; set; }

    [Property(4)]
    decimal Total { get; set; }

    [Property(5)]
    OrderOptions Options { get; set; }

    // The day of shipping, absent until the order ships (Lesson 8).
    [Property(6)]
    Option<DateOnly> Shipped { get; set; }

    // The customer of the order; a guest order has none.
    [Property(7)]
    [Weak]
    Option<MCustomer> Customer { get; set; }

    // The note of the customer, in a text index (Lesson 10).
    [Property(8)]
    [TextIndexed(nameof(MOrderTextIndex))]
    string Note { get; set; }
}

[GhostRepository]
[IncludeEntityModel(nameof(MCustomer))]
[IncludeEntityModel(nameof(MOrder))]
internal interface MShopRepository
{
}
```

A **value index** orders the entities of a type by the values of its components. It is an interface with `[ValueIndex(nameof(MOrder), n)]`, the entity type it indexes and its number among the indexes of the Repository, and one `[ValueIndexComponent]` per component, each at its position, from 1. Its name starts with `M`: `MOrderNumberIndex` gives `OrderNumberIndex`. A Repository has the value indexes of the entity types it includes.

```csharp
[ValueIndex(nameof(MOrder), 1)]
[ValueIndexComponent(1, nameof(MOrder.Number))]
internal interface MOrderNumberIndex
{
}

[ValueIndex(nameof(MOrder), 2)]
[ValueIndexComponent(1, nameof(MOrder.Placed))]
internal interface MOrderPlacedIndex
{
}
```

`OrderNumberIndex.Is(1042)` finds the order of a number, without reading the others. A search gives Views, as a text search; `Bodies()` gives the Bodies of the few to change. The index is up to date at the next instruction: the new number of an order is found at once.

The shop of the lesson, ten orders, and the numbers of the orders found:

```csharp
private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

// Ten orders, one every four hours from Monday morning.
private static ShopRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    for (int i = 0; i < 10; i++)
        new Order { Number = 1040 + i, Placed = Monday.AddHours(4 * i), Total = 10m * (i + 1) };
    transaction.Commit();
    return shop;
}

private static List<int> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();
```

```csharp
[Fact]
public void FindAnOrderByItsNumber()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-number", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Assert.Equal(30m, OrderNumberIndex.Is(1042).Single().Total);
        Assert.Equal(0, OrderNumberIndex.Is(9999).Count());

        // The Body of the order to change; its new number is found at the next instruction.
        var order = OrderNumberIndex.Is(1042).Bodies().Single();
        order.Status = OrderStatus.Paid;
        order.Number = 2042;
        Assert.Equal(0, OrderNumberIndex.Is(1042).Count());
        Assert.Equal(OrderStatus.Paid, OrderNumberIndex.Is(2042).Single().Status);
        transaction.Commit();
    }
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(1, OrderNumberIndex.Is(2042).Count());
}
```

On a date, `OrderPlacedIndex` finds a period:

| Condition | The values |
| --------- | ---------- |
| `Is(value)` | Equal to the value |
| `From(start)` | From the value on, the value included |
| `Before(end)` | Below the value |
| `Between(start, end)` | From the first, below the second: as the ranges of C#, `Between(a, b)` then `Between(b, c)` take each value once |
| `Range(first, last)` | From the first to the last, both included |
| `Every` | Every entity |

A search gives its entities **in the order of their values**: the orders by their dates. `Count()` counts them; the operators of GBO, `Select`, `Sum`, `Take`, work on them as on any collection.

```csharp
[Fact]
public void ReadTheOrdersOfAPeriod()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-period", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
    var tuesday = Monday.Date.AddDays(1);

    // Tuesday and after; before Tuesday.
    Assert.Equal([1044, 1045, 1046, 1047, 1048, 1049], Numbers(OrderPlacedIndex.From(tuesday)));
    Assert.Equal([1040, 1041, 1042, 1043], Numbers(OrderPlacedIndex.Before(tuesday)));

    // Tuesday only: from its start, below the start of Wednesday.
    Assert.Equal([1044, 1045, 1046, 1047, 1048, 1049], Numbers(OrderPlacedIndex.Between(tuesday, tuesday.AddDays(1))));

    // Between excludes its end, Range includes it.
    Assert.Equal([1040, 1041], Numbers(OrderPlacedIndex.Between(Monday, Monday.AddHours(8))));
    Assert.Equal([1040, 1041, 1042], Numbers(OrderPlacedIndex.Range(Monday, Monday.AddHours(8))));

    // Every order, and their count.
    Assert.Equal(10, OrderPlacedIndex.Every.Count());
    Assert.Equal(550m, OrderPlacedIndex.Every.Sum(static order => order.Total));
}
```

`Descending()` reads the index backward: the most recent orders first, the first one found at once, wherever it is. The same search by a query of the Views reads every order.

```csharp
[Fact]
public void ReadTheMostRecentFirst()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-05-recent", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    Assert.Equal([1049, 1048, 1047], OrderPlacedIndex.Every.Descending().Select(static order => order.Number).Take(3).ToList());

    // A new order, at the head at once.
    new Order { Number = 1050, Placed = Monday.AddDays(3) };
    Assert.Equal(1050, OrderPlacedIndex.Every.Descending().First().Number);

    // The same search by a query of the Views reads every order.
    Assert.Equal(1050, OrderViewCollection.MaxBy(static order => order.Placed).Number);
    transaction.Commit();
}
```

### Lesson 6: A composite index

An index of several components orders its entities by the first, then by the second for equal firsts, as the words of a dictionary: the status, then the day. `DatePrecision.Day` keeps the day of the date: the orders of a day share it.

```csharp
[ValueIndex(nameof(MOrder), 3)]
[ValueIndexComponent(1, nameof(MOrder.Status))]
[ValueIndexComponent(2, nameof(MOrder.Placed), DatePrecision.Day)]
internal interface MOrderStatusDayIndex
{
}
```

`Is` fixes a component and gives the conditions of the next one: `OrderStatusDayIndex.Is(OrderStatus.Paid)` gives the paid orders, by day; `.Is(day)`, those of a day; `.From(day)`, those from a day on. The struct that `Is` gives is itself the result of its prefix: it counts, it is read backward. A range ends the search: there is no condition after it.

The shop of the lesson, three days of orders of every status:

```csharp
private static readonly DateOnly Monday = new(2026, 10, 19);

// Three days of orders, every status, one order every three hours.
private static ShopRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    for (int i = 0; i < 24; i++)
        new Order { Number = 100 + i, Status = (OrderStatus)(i % 4), Placed = Monday.ToDateTime(new TimeOnly(0, 30), DateTimeKind.Utc).AddHours(3 * i) };
    transaction.Commit();
    return shop;
}

private static List<int> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();
```

```csharp
[Fact]
public void FixTheComponentsInTheirOrder()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-order", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

    // The paid orders, day by day: the struct of the next component is the result of its prefix as well.
    var paid = OrderStatusDayIndex.Is(OrderStatus.Paid);
    Assert.Equal([101, 105, 109, 113, 117, 121], Numbers(paid));
    Assert.Equal(6, paid.Count());

    // A status and a day; a status from a day on.
    Assert.Equal([109, 113], Numbers(paid.Is(Monday.AddDays(1))));
    Assert.Equal([109, 113, 117, 121], Numbers(paid.From(Monday.AddDays(1))));
    Assert.Equal([121, 117, 113, 109, 105, 101], Numbers(paid.Descending()));

    // Two statuses: by status, then by day.
    Assert.Equal([100, 104, 108, 112, 116, 120, 101, 105, 109, 113, 117, 121], Numbers(OrderStatusDayIndex.Range(OrderStatus.Pending, OrderStatus.Paid)));
}
```

A date at the day takes a `DateOnly`, the whole day. It takes a `DateTime` too: an instant inside a day. The index keeps the day; the orders of the day of the instant are compared one by one to it, and the result is exact.

```csharp
[Fact]
public void CompareAnInstantInsideADay()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-06-instant", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
    var shipped = OrderStatusDayIndex.Is(OrderStatus.Shipped);

    // The shipped orders of Tuesday: 06:30 and 18:30. From Tuesday noon: only 18:30, then those of Wednesday.
    Assert.Equal([110, 114], Numbers(shipped.Is(Monday.AddDays(1))));
    var tuesdayNoon = Monday.AddDays(1).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
    Assert.Equal([114, 118, 122], Numbers(shipped.From(tuesdayNoon)));

    // The same search by a query of the Views reads every order, and finds the same ones.
    Assert.Equal([114, 118, 122], OrderViewCollection.Where(tuesdayNoon, static (order, noon) => order.Status == OrderStatus.Shipped && order.Placed >= noon)
        .Select(static order => order.Number).ToList());
}
```

The components are read **in their order, never skipped**: `OrderStatusDayIndex` has no search of a period of any status. That search is the one of another index, `OrderPlacedIndex` of Lesson 5; and two indexes combine (Lesson 10). Declare the components in the order of the questions: the status first, when every question gives a status.

### Lesson 7: Round the values

An index may keep less than a value: a date at the day (Lesson 6), a decimal at its `Decimals`, an integer at its `Step`. The index is smaller, and the entities of a rounded value are found together. **The result stays exact**: the searches take the true values, and the entities of the rounded value of a bound are compared one by one to it.

```csharp
// The total of an order, by the euro: Decimals = 0 keeps no decimal of a decimal. A decimal in an index always says its decimals.
[ValueIndex(nameof(MOrder), 4)]
[ValueIndexComponent(1, nameof(MOrder.Total), Decimals = 0)]
internal interface MOrderTotalIndex
{
}

// The points of a customer, by the thousand: Step = 1000.
[ValueIndex(nameof(MCustomer), 5)]
[ValueIndexComponent(1, nameof(MCustomer.Points), Step = 1000)]
internal interface MCustomerPointsIndex
{
}

// The birthday of a customer: two parts of a date, the month then the day of the month, whatever the year.
[ValueIndex(nameof(MCustomer), 6)]
[ValueIndexComponent(1, nameof(MCustomer.Birthday), DatePart.MonthOfYear)]
[ValueIndexComponent(2, nameof(MCustomer.Birthday), DatePart.DayOfMonth)]
internal interface MCustomerBirthdayIndex
{
}
```

`OrderTotalIndex.From(19.99m)`, on an index by the euro, keeps 19.99 and leaves 19.50: both are in the euro 19, which the search compares to the bound. A bound at the start of its euro, `Between(10.50m, 20m)` for its end, takes or leaves the euro whole, without comparing anything.

```csharp
[Fact]
public void KeepTheResultExactWithARoundedIndex()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-total", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    decimal[] totals = [9.99m, 10.00m, 10.50m, 19.50m, 19.99m, 20.00m, 25.50m, 49.99m, 50.00m];
    for (int i = 0; i < totals.Length; i++)
        new Order { Number = i + 1, Total = totals[i] };

    static List<decimal> Totals(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found)
        => found.Select(static order => order.Total).ToList();
    Assert.Equal([19.99m, 20.00m, 25.50m, 49.99m, 50.00m], Totals(OrderTotalIndex.From(19.99m)));
    Assert.Equal([10.50m, 19.50m, 19.99m], Totals(OrderTotalIndex.Between(10.50m, 20m)));
    Assert.Equal([25.50m], Totals(OrderTotalIndex.Is(25.50m)));
    Assert.Equal(4, OrderTotalIndex.Before(19.99m).Count());
    transaction.Commit();
}
```
```csharp
[Fact]
public void RoundAnIntegerByItsStep()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-points", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    foreach (var (name, points) in new[] { ("Ada", 500L), ("Bob", 1_200L), ("Cid", 1_800L), ("Dan", 2_500L), ("Eve", 3_100L) })
        new Customer { Name = name, Email = $"{name.ToLowerInvariant()}@example.com", Points = points };
    Assert.Equal(["Cid", "Dan", "Eve"], CustomerPointsIndex.From(1_500).Select(static c => c.Name).ToList());
    Assert.Equal(["Bob", "Cid"], CustomerPointsIndex.Between(1_000, 2_000).Select(static c => c.Name).ToList());
    transaction.Commit();
}
```

A **part** of a date is a cycle: `DatePart.MonthOfYear`, `DayOfMonth`, `DayOfWeek`, `HourOfDay`, `MinuteOfHour`. The month then the day of the month give the birthdays, whatever the year, which no precision gives: `CustomerBirthdayIndex.Is(10).Is(22)`, the customers born on an October 22. A part takes an `int`, a `DayOfWeek` for the day of the week, and its ranges are those of its cycle.

```csharp
[Fact]
public void FindByThePartsOfADate()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-birthday", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    // Each customer has an email of its own: the index of the emails is unique (Lesson 9).
    new Customer { Name = "Ada", Email = "ada@example.com", Birthday = new DateOnly(1990, 10, 22) };
    new Customer { Name = "Bob", Email = "bob@example.com", Birthday = new DateOnly(2000, 3, 14) };
    new Customer { Name = "Cid", Email = "cid@example.com", Birthday = new DateOnly(1985, 10, 22) };
    new Customer { Name = "Dan", Email = "dan@example.com", Birthday = new DateOnly(1999, 10, 1) };

    // Born on an October 22; in October, by day; in the second half of October.
    Assert.Equal(["Ada", "Cid"], CustomerBirthdayIndex.Is(10).Is(22).Select(static c => c.Name).ToList());
    Assert.Equal(["Dan", "Ada", "Cid"], CustomerBirthdayIndex.Is(10).Result.Select(static c => c.Name).ToList());
    Assert.Equal(["Ada", "Cid"], CustomerBirthdayIndex.Is(10).Range(15, 31).Select(static c => c.Name).ToList());

    // The same birthdays by a query of the Views, which reads every customer.
    Assert.Equal(2, CustomerViewCollection.Count(static c => c.Birthday.Month == 10 && c.Birthday.Day == 22));
    transaction.Commit();
}
```

| Option | Of | What the index keeps |
| ------ | -- | -------------------- |
| `DatePrecision.Year` to `Tick` | `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | The value rounded down to its unit; `Tick`, the default, the value itself |
| `DatePart.MonthOfYear`, `DayOfMonth`, `DayOfWeek`, `HourOfDay`, `MinuteOfHour` | The dates and the times | One part, a cycle |
| `Decimals = n` | `float`, `double`, `decimal` (required) | The value rounded down to `n` decimals |
| `Step = n` | The integers | The value rounded down to a multiple of `n` |
| `IgnoreCase`, `IgnoreAccents` | `string` | The string without its case, its accents (Lesson 9) |

### Lesson 8: Flags and absent values

The options of an order are a `[Flags]` enum, several at once; its day of shipping is an `Option<DateOnly>`, absent until it ships.

```csharp
[ValueIndex(nameof(MOrder), 7)]
[ValueIndexComponent(1, nameof(MOrder.Options))]
internal interface MOrderOptionsIndex
{
}

[ValueIndex(nameof(MOrder), 8)]
[ValueIndexComponent(1, nameof(MOrder.Shipped))]
internal interface MOrderShippedIndex
{
}
```

The index of a `[Flags]` enum finds an exact combination, and each flag:

| Condition | The orders |
| --------- | ---------- |
| `Is(Gift \| Express)` | Of exactly these flags |
| `Has(Gift)` | That have the flag, whatever their others |
| `One(Gift \| Express)` | That have one of the flags at least |
| `All(Gift \| Express)` | That have both |
| `None(Fragile)` | That have none of them |

`One`, `All` and `None` give a set of orders, in no particular order, as the combinations of Lesson 10.

The numbers of the orders found, by a search and by a set:

```csharp
private static List<int> Numbers(ValueIndexResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();

private static List<int> Numbers(IndexSetResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).ToList();
```

```csharp
[Fact]
public void FindTheFlags()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-flags", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    new Order { Number = 1, Options = OrderOptions.None };
    new Order { Number = 2, Options = OrderOptions.Gift };
    new Order { Number = 3, Options = OrderOptions.Gift | OrderOptions.Express };
    new Order { Number = 4, Options = OrderOptions.Express | OrderOptions.Fragile };
    new Order { Number = 5, Options = OrderOptions.Gift | OrderOptions.Express | OrderOptions.Fragile };

    Assert.Equal([3], Numbers(OrderOptionsIndex.Is(OrderOptions.Gift | OrderOptions.Express)));
    Assert.Equal([2, 3, 5], Numbers(OrderOptionsIndex.Has(OrderOptions.Gift)));
    Assert.Equal([2, 3, 4, 5], Numbers(OrderOptionsIndex.One(OrderOptions.Gift | OrderOptions.Express)));
    Assert.Equal([3, 5], Numbers(OrderOptionsIndex.All(OrderOptions.Gift | OrderOptions.Express)));
    Assert.Equal([1, 2, 3], Numbers(OrderOptionsIndex.None(OrderOptions.Fragile)));

    // A flag taken off an order: the order leaves the flag at the next instruction.
    OrderNumberIndex.Is(5).Bodies().Single().Options &= ~OrderOptions.Gift;
    Assert.Equal([2, 3], Numbers(OrderOptionsIndex.Has(OrderOptions.Gift)));
    transaction.Commit();
}
```

An `Option<T>` may be absent: its index finds the absent values too, `IsNone()`, the orders not shipped yet. **A range never holds an absent value**: `From(day)` gives the orders shipped from a day on, never those not shipped.

```csharp
[Fact]
public void FindTheAbsentValues()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-absent", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    var monday = new DateOnly(2026, 10, 19);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Order { Number = 1, Shipped = monday };
        new Order { Number = 2 };
        new Order { Number = 3, Shipped = monday.AddDays(2) };
        new Order { Number = 4 };
        transaction.Commit();
    }
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Assert.Equal([2, 4], Numbers(OrderShippedIndex.IsNone()));
        Assert.Equal([1], Numbers(OrderShippedIndex.Is(monday)));
        Assert.Equal([1, 3], Numbers(OrderShippedIndex.From(monday)));
        Assert.Empty(Numbers(OrderShippedIndex.Before(monday)));

        // Shipping an order: it leaves the absent values.
        foreach (var order in OrderShippedIndex.IsNone().Bodies())
            order.Shipped = monday.AddDays(3);
        Assert.Equal(0, OrderShippedIndex.IsNone().Count());
        Assert.Equal([3, 2, 4], Numbers(OrderShippedIndex.From(monday.AddDays(1))));
        transaction.Commit();
    }
}
```

### Lesson 9: A unique value

`Unique = true` gives a value to one entity only, as a constraint `UNIQUE` of SQL. The email of a customer is unique whatever its case, `IgnoreCase = true`; a string is found by its exact value, never by a range. The loyalty card is unique too, and absent for the customers without one.

```csharp
// The email of a customer: unique, whatever its case. A string is found by its exact value, never by a range.
[ValueIndex(nameof(MCustomer), 9, Unique = true)]
[ValueIndexComponent(1, nameof(MCustomer.Email), IgnoreCase = true)]
internal interface MCustomerEmailIndex
{
}

// The loyalty card of a customer: unique, and absent for the customers without one.
[ValueIndex(nameof(MCustomer), 10, Unique = true)]
[ValueIndexComponent(1, nameof(MCustomer.Card))]
internal interface MCustomerCardIndex
{
}
```

The Setter refuses a value that another entity has, in the Store or in the transaction: it throws a `GhostUniquenessException`, and the entity keeps its previous value; the transaction goes on, and commits. A value freed, by a change or a removal, may be taken at the next instruction.

```csharp
[Fact]
public void RefuseAValueTaken()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-unique", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Customer { Name = "Ada", Email = "ada@example.com" };
        new Customer { Name = "Bob", Email = "bob@example.com" };
        transaction.Commit();
    }
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        Assert.Equal("Ada", CustomerEmailIndex.Is("ADA@Example.com").Single().Name);

        // The email of Ada, whatever its case, refused to Bob: he keeps his own.
        var bob = CustomerEmailIndex.Is("bob@example.com").Bodies().Single();
        Assert.Throws<GhostUniquenessException>(() => bob.Email = "Ada@Example.com");
        Assert.Equal("bob@example.com", bob.Email);

        // Ada changes her email: the old one is free, and Bob takes it.
        CustomerEmailIndex.Is("ada@example.com").Bodies().Single().Email = "ada@lovelace.org";
        bob.Email = "ada@example.com";
        transaction.Commit();
    }
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal("Bob", CustomerEmailIndex.Is("ada@example.com").Single().Name);
        Assert.Equal("Ada", CustomerEmailIndex.Is("ada@lovelace.org").Single().Name);
    }
}
```

An **absent value is never refused**, as the `NULL` of SQL: many customers have no card, and a card is given once.

```csharp
[Fact]
public void NeverRefuseAnAbsentValue()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-absent", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Customer { Name = "Ada", Email = "ada@example.com" };
    var bob = new Customer { Name = "Bob", Email = "bob@example.com" };
    Assert.Equal(2, CustomerCardIndex.IsNone().Count());

    ada.Card = 1001;
    Assert.Throws<GhostUniquenessException>(() => bob.Card = 1001);
    Assert.False(bob.Card.Any());
    bob.Card = 1002;
    Assert.Equal("Bob", CustomerCardIndex.Is(1002).Single().Name);
    transaction.Commit();
}
```

Two concurrent transactions may give the same value, each alone in its own: neither sees the other. The first commit wins; the second is refused by a `TransactionConflictException` of the kind `Uniqueness`, and nothing of it is written. Its retry sees the value taken, and its Setter refuses it at once, as for any conflict (Tutorial One, Lesson 16).

```csharp
[Fact]
public async Task RefuseAValueTakenMeanwhile()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-concurrent", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    var firstChanged = new TaskCompletionSource();
    var secondCommitted = new TaskCompletionSource();
    var first = Task.Run(async () =>
    {
        using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
        new Customer { Name = "Ada", Email = "hello@example.com" };
        firstChanged.SetResult();
        await secondCommitted.Task;
        return Assert.Throws<TransactionConflictException>(() => transaction.Commit());
    });
    await firstChanged.Task;
    await Task.Run(() =>
    {
        using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
        new Customer { Name = "Bob", Email = "Hello@Example.com" };
        transaction.Commit();
    });
    secondCommitted.SetResult();
    Assert.Equal(TransactionConflictKind.Uniqueness, (await first).Kind);

    // The retry of the first one: the email is taken, its Setter refuses it at once.
    await Task.Run(() =>
    {
        using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
        Assert.Throws<GhostUniquenessException>(() => new Customer { Name = "Ada", Email = "hello@example.com" });
    });
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal("Bob", CustomerEmailIndex.Is("hello@example.com").Single().Name);
}
```

> **Tip: Every entity has a value**
>
> A unique index holds every entity of its type. A customer created without an email has the empty email: a second one is refused at the commit, as two customers of the same email. Give each entity its value at its birth, or make the property an `Option<T>`, whose absent values are never refused.

## Part 3: Custom collections

### Lesson 10: Combine the results

A result of an index is a set of entities: it combines with another result of the same type, of another value index or of a text search.

| Combination | The entities |
| ----------- | ------------ |
| `a.And(b)` | Of both |
| `a.Or(b)` | Of either |
| `a.AndNot(b)` | Of the first, not of the second |

A combination reads each index once, never an entity that none of them gives. The notes of the orders are in a text index of the shop, numbered with its value indexes:

```csharp
[TextIndex(11)]
internal interface MOrderTextIndex
{
}
```
The shop of the lesson, five orders, and the numbers of a combination, sorted:

```csharp
private static ShopRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    var placed = new DateTime(2026, 10, 19, 10, 0, 0, DateTimeKind.Utc);
    new Order { Number = 1, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.Gift, Note = "Leave at the door." };
    new Order { Number = 2, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.None, Note = "Call before delivery." };
    new Order { Number = 3, Status = OrderStatus.Shipped, Placed = placed, Options = OrderOptions.Gift, Note = "Gift wrap, please." };
    new Order { Number = 4, Status = OrderStatus.Paid, Placed = placed, Options = OrderOptions.Gift | OrderOptions.Express, Note = "Call me, then leave at the door." };
    new Order { Number = 5, Status = OrderStatus.Pending, Placed = placed, Options = OrderOptions.Express, Note = "Back door." };
    transaction.Commit();
    return shop;
}

private static List<int> Numbers(IndexSetResult<OrderView, Order, ShopRepositoryTag> found) => found.Select(static order => order.Number).OrderBy(static n => n).ToList();
```

```csharp
[Fact]
public void CombineTwoIndexes()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-two", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
    var paid = OrderStatusDayIndex.Is(OrderStatus.Paid);
    var gifts = OrderOptionsIndex.Has(OrderOptions.Gift);
    Assert.Equal([1, 4], Numbers(paid.And(gifts)));
    Assert.Equal([1, 2, 3, 4], Numbers(paid.Or(gifts)));
    Assert.Equal([2], Numbers(paid.AndNot(gifts)));
    Assert.Equal(2, paid.And(gifts).Count());
}
```

A text search combines as well: the orders of a note that says "door", paid, and not to call first. A combination combines again.

```csharp
[Fact]
public void CombineATextSearch()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-text", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
    var door = OrderTextIndex.Search("door");
    Assert.Equal([1, 4, 5], door.Select(static order => order.Number).ToList());
    Assert.Equal([1, 4], Numbers(OrderStatusDayIndex.Is(OrderStatus.Paid).And(door)));
    Assert.Equal([5], Numbers(door.AndNot(OrderOptionsIndex.Has(OrderOptions.Gift))));

    // A combination combines again: the paid orders of a door, but not those to call first.
    Assert.Equal([1], Numbers(OrderStatusDayIndex.Is(OrderStatus.Paid).And(door).AndNot(OrderTextIndex.Search("call"))));
}
```

A combination is a **set**: its entities come in no particular order, the order of none of its indexes; the lesson sorts them to compare them. It counts, and gives its Bodies for the few to change.

```csharp
[Fact]
public void ChangeTheEntitiesOfACombination()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-10-change", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        // The paid gifts ship.
        foreach (var order in OrderStatusDayIndex.Is(OrderStatus.Paid).And(OrderOptionsIndex.Has(OrderOptions.Gift)).Bodies())
            order.Status = OrderStatus.Shipped;
        transaction.Commit();
    }
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal([1, 3, 4], OrderStatusDayIndex.Is(OrderStatus.Shipped).Result.Select(static order => order.Number).ToList());
        Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
    }
}
```

> **Tip: A composite index, or two indexes combined**
>
> A composite index answers the questions of its prefixes, in the order of its components, and gives its entities in the order of their values: the paid orders, by day. Two separate indexes, combined, answer the conditions on any of them, in no order: the paid orders that are gifts. Declare a composite for a frequent question that needs its order; combine for the others.

### Lesson 11: Your own collections

A collection of the model (Tutorial Two) is a set that your code fills, by `Add` and `Remove`. An index gives another kind of collection: the entities of a value, computed at each read, that your code never fills. Write it as a member of your partial declarations (Tutorial One, Lesson 11): `customer.Orders`, the orders whose customer is this one, by an index of the customer and the instant.

```csharp
[ValueIndex(nameof(MOrder), 12)]
[ValueIndexComponent(1, nameof(MOrder.Customer))]
[ValueIndexComponent(2, nameof(MOrder.Placed))]
internal interface MOrderCustomerIndex
{
}

// Your collections, in your partial declarations (Tutorial One, Lesson 11): members of the customer, computed by the index at each
// read. GBO stores nothing for them.
internal partial class Customer
{
    /// <summary>The orders of the customer, from the oldest.</summary>
    public OrderCustomerIndex.WithCustomer Orders => OrderCustomerIndex.Is(this);

    /// <summary>The orders of the customer placed from <paramref name="since"/> on.</summary>
    public ValueIndexResult<OrderView, Order, ShopRepositoryTag> OrdersSince(DateTime since) => OrderCustomerIndex.Is(this).From(since);

    /// <summary>The last order of the customer, if it has one.</summary>
    public Option<OrderView> LastOrder => Orders.Descending().FirstOrNone();
}

internal readonly partial struct CustomerView
{
    /// <summary>The same collection on the View, for the queries over the Views.</summary>
    public OrderCustomerIndex.WithCustomer Orders => OrderCustomerIndex.Is(this);
}

// A collection of your own, of the whole shop: a search named once, used everywhere.
internal static class Backlog
{
    /// <summary>The paid orders placed before <paramref name="day"/>, still not shipped: the oldest first.</summary>
    public static ValueIndexResult<OrderView, Order, ShopRepositoryTag> Late(DateOnly day) => OrderStatusDayIndex.Is(OrderStatus.Paid).Before(day);
}
```

The collection is always up to date, at the next instruction: a new order of the customer is in it without any `Add`. It is in the order of its index, read backward, counted, combined; and GBO stores nothing for it.

The shop of the lesson: three customers, four orders.

```csharp
private static readonly DateTime Monday = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

private static ShopRepository NewShop(TransactionalMemoryStore store)
{
    var shop = new ShopRepository(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = new Customer { Name = "Ada", Email = "ada@example.com" };
    var bob = new Customer { Name = "Bob", Email = "bob@example.com" };
    new Customer { Name = "Cid", Email = "cid@example.com" };
    new Order { Number = 1, Customer = ada, Placed = Monday, Status = OrderStatus.Delivered };
    new Order { Number = 2, Customer = bob, Placed = Monday.AddHours(1), Status = OrderStatus.Paid };
    new Order { Number = 3, Customer = ada, Placed = Monday.AddDays(1), Status = OrderStatus.Paid };
    new Order { Number = 4, Customer = ada, Placed = Monday.AddDays(2), Status = OrderStatus.Pending };
    transaction.Commit();
    return shop;
}
```

```csharp
[Fact]
public void ReadTheOrdersOfACustomer()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-orders", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    var ada = CustomerEmailIndex.Is("ada@example.com").Bodies().Single();
    Assert.Equal([1, 3, 4], ada.Orders.Result.Select(static order => order.Number).ToList());
    Assert.Equal(3, ada.Orders.Count());
    Assert.Equal([4, 3, 1], ada.Orders.Descending().Select(static order => order.Number).ToList());
    Assert.Equal([3, 4], ada.OrdersSince(Monday.AddDays(1)).Select(static order => order.Number).ToList());
    Assert.Equal(4, ada.LastOrder.Get().Number);

    // A new order of Ada: in her orders at the next instruction, without any Add.
    new Order { Number = 5, Customer = ada, Placed = Monday.AddDays(3) };
    Assert.Equal(4, ada.Orders.Count());
    Assert.Equal(5, ada.LastOrder.Get().Number);
    transaction.Commit();
}
```

The View has it too, `CustomerView.Orders`, for the queries over the Views: the customers of two orders at least, and those without any, without a Body.

```csharp
[Fact]
public void QueryTheCollectionsOfTheViews()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-views", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
    Assert.Equal(["Ada"], CustomerViewCollection.Where(static customer => customer.Orders.Count() >= 2).Select(static customer => customer.Name).ToList());
    Assert.Equal(["Cid"], CustomerViewCollection.Where(static customer => customer.Orders.Count() == 0).Select(static customer => customer.Name).ToList());
}
```

A search of the whole shop is a collection as well, named once in a static class, and used everywhere: `Backlog.Late(day)`, the paid orders placed before a day, still not shipped.

```csharp
[Fact]
public void NameASearchOfTheShop()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-backlog", StoreAccessMode.ReadWrite);
    using var shop = NewShop(store);
    using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
    var thursday = DateOnly.FromDateTime(Monday).AddDays(3);
    Assert.Equal([2, 3], Backlog.Late(thursday).Select(static order => order.Number).ToList());

    // Shipped, an order leaves the backlog.
    foreach (var order in Backlog.Late(thursday).Bodies())
        order.Status = OrderStatus.Shipped;
    Assert.Equal(0, Backlog.Late(thursday).Count());
    transaction.Commit();
}
```

| | A collection of the model | A collection of an index |
| --- | --- | --- |
| Declared by | `IEntityCollection<T>`, `[Strong]` or `[Weak]` | A member of your partial declaration over an index |
| Filled by | Your code, `Add` and `Remove` | The values of the entities |
| Its order | None | The order of the index, and backward |
| Stored | Yes | No: computed at each read |
| For | What the code decides: the favorites of a listener, the parts of an aggregate | What the values decide: the orders of a customer, of a status, of a period |

### Lesson 12: The indexes and the transactions

An index is a part of the data, as the entities. It is up to date at the next instruction in the transaction that changes them (Lessons 1 and 5); its changes are rolled back with the transaction; the other transactions see them at its commit only.

```csharp
[Fact]
public async Task SeeTheChangesOfACommitOnly()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-commit", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Order { Number = 1, Status = OrderStatus.Paid, Placed = Monday };
        Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
        transaction.Rollback();
    }
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(0, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());

    // A writer changes the index, then waits; a reader, on its own thread, looks meanwhile (Tutorial One, Lesson 15).
    var written = new TaskCompletionSource();
    var looked = new TaskCompletionSource();
    var writer = Task.Run(async () =>
    {
        using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
        new Order { Number = 2, Status = OrderStatus.Paid, Placed = Monday };
        written.SetResult();
        await looked.Task;
        transaction.Commit();
    });
    await written.Task;
    int seen = await Task.Run(() =>
    {
        using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);
        return OrderStatusDayIndex.Is(OrderStatus.Paid).Count();
    });
    looked.SetResult();
    await writer;

    // The order was not committed when the reader looked; a transaction opened now sees it.
    Assert.Equal(0, seen);
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal(1, OrderStatusDayIndex.Is(OrderStatus.Paid).Count());
}
```

Concurrent transactions that give the same value to their entities, the same status or the same day, never conflict by the index: every commit keeps its entities. Only two changes of the same entity conflict, as in Tutorial One, and two entities of one value of a unique index (Lesson 9).

```csharp
[Fact]
public void CommitInParallelOnTheSameValues()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-parallel", StoreAccessMode.ReadWrite);
    using var shop = new ShopRepository(store);
    const int Writers = 4, Orders = 100;
    Parallel.For(0, Writers, writer =>
    {
        for (int i = 0; i < Orders; i++)
        {
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            new Order { Number = writer * Orders + i, Status = OrderStatus.Paid, Placed = Monday };
            transaction.Commit();
        }
    });
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal(Writers * Orders, OrderStatusDayIndex.Is(OrderStatus.Paid).Is(DateOnly.FromDateTime(Monday)).Count());
        Assert.Equal(Writers * Orders, OrderNumberIndex.Between(0, Writers * Orders).Count());
    }
}
```

The indexes are kept with the data, over a restart of the Repository:

```csharp
[Fact]
public void KeepTheIndexesOverARestart()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-12-restart", StoreAccessMode.ReadWrite);
    using (var shop = new ShopRepository(store))
    using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Customer { Name = "Ada", Email = "ada@example.com" };
        new Order { Number = 7, Status = OrderStatus.Shipped, Placed = Monday };
        transaction.Commit();
    }
    using (var shop = new ShopRepository(store))
    using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal("Ada", CustomerEmailIndex.Is("ADA@example.com").Single().Name);
        Assert.Equal(7, OrderStatusDayIndex.Is(OrderStatus.Shipped).Result.Single().Number);
    }
}
```

An index is a **projection** of the entities: it can always be made again from them. A Repository builds the indexes that its Store does not have yet at its start, from the entities already there, before its first transaction; it builds an index again when its declaration changes, and removes the indexes that it no longer declares. A new index on a large Store makes its start longer by its build.

## From the need to the index

Every question has its tool. The path, from the most common to the rarest:

### 1. Does the question read a few entities of many?

**The order of a number, the orders of a day, the products of a word.** An index: its cost follows what it finds. A query of the Views reads every entity: right for a question over most of them, a report, a sum of every order, or for a type of a few thousands of entities.

### 2. Is it about words, or about values?

**Words in a text, whatever their case, their accents, their order:** a text index (Lessons 1 to 4). **A value, a range, an order:** a value index (Lessons 5 to 9).

### 3. Several values together?

**A frequent question that fixes the first values and wants the order of the next one:** a composite index, its components in the order of the questions (Lesson 6). **Conditions on any of them, in any order:** separate indexes, combined (Lesson 10).

### 4. How precise?

**The day of a date, the euro of a total:** a precision, a smaller index, an exact result (Lesson 7). **A cycle, the birthdays, the Mondays at 9:** the parts of a date.

### 5. One entity per value?

**An email, a number of a card:** `Unique = true` (Lesson 9); an `Option<T>` for the entities without one.

### 6. A collection that the values decide?

**The orders of a customer, the late orders:** a member of your partial declaration over an index (Lesson 11). **A set that the code decides:** a collection of the model (Tutorial Two).

### The whole path

| The question | When the answer is yes | The declaration | Lessons |
| ------------ | ---------------------- | --------------- | ------- |
| Does it read a few entities of many? | An index | `[TextIndex]` or `[ValueIndex]` | 1, 5 |
| Words of a text? | A text index | `[TextIndex(n)]`, `[TextIndexed]` | 1 to 4 |
| A long text written in steps? | Its words cut at the commit | `TextIndexing.Commit` | 4 |
| A value, a range, an order? | A value index | `[ValueIndex]`, `[ValueIndexComponent]` | 5 |
| Several values, in the order of a question? | A composite index | Several components | 6 |
| Conditions on any of them? | Indexes combined | `And`, `Or`, `AndNot` | 10 |
| A precision, a cycle? | A rounded value, a part | `DatePrecision`, `Decimals`, `Step`, `DatePart` | 6, 7 |
| One entity per value? | A unique index | `Unique = true` | 9 |
| A collection of what the values decide? | Your own collection | A member over an index | 11 |

## What you learned

- A **text index** finds the entities by the words of their texts, whatever their case and their accents: `[TextIndex(n)]`, `[TextIndexed]` on the properties; `Search`, `Count`, `Terms`.
- A query is **required words, alternatives and exclusions**, in any order, with wildcards and typing errors forgiven; **`TextSearch.Ranked`** gives the most relevant first.
- A **value index** finds the entities by their values, in the order of their values: `Is`, `From`, `Before`, `Between`, `Range`, `Every`, `Descending()`.
- A **composite index** is read in the order of its components: `Is` fixes one, and gives the conditions of the next.
- A **rounded value** makes a smaller index, and the result stays exact; the **parts of a date** are cycles.
- The **flags** are found alone, together or none; the **absent values** are found, never in a range.
- A **unique value** is refused at the Setter, the previous value kept, and at the commit, by a conflict of the kind `Uniqueness`; an absent value never is.
- The results of the indexes **combine**, `And`, `Or`, `AndNot`, a text search included, in a set without order.
- **Your own collections** are members over the indexes: always up to date, ordered, counted, never stored.
- An index is **a part of the data**: up to date at the next instruction, rolled back, committed, kept over a restart, and made again from the entities when needed.

The next tutorial shows the Log and the Record types: their subscription, and their replay.
