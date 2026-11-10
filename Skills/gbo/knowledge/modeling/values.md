# Values, texts, arrays, structs and value objects

Part of [the modeling knowledge](README.md). The values of an entity and of a Record are stored in one contiguous block, read in place by the Views without copy. The generator accepts the types below and refuses the rest (GBO002, GBO009, GBO033, GBO034).

## The types of a property

| C# type | Entity | Record | Body / View | Default at birth | Notes |
|---|---|---|---|---|---|
| `byte` ... `ulong`, `float`, `double`, `Half`, `Int128`, `UInt128` | yes | yes | same | 0 | |
| `decimal` | yes | yes | same | 0 | The Setter compares the bytes: `1.00m` written over `1.0m` is a change, calls `On...Changed` |
| `bool`, `char` | yes | yes | same | `false`, `'\0'` | |
| `DateTime`, `DateOnly`, `TimeOnly`, `TimeSpan` | yes | yes | same | `default` | A `DateTime` keeps its `Kind` |
| `DateTimeOffset` | yes | yes | same | `0001-01-01 +00:00` | Stored as UTC ticks and offset; not an element of a `GhostSpan`, not a field of a struct |
| `Guid` | yes | yes | same | `Guid.Empty` | |
| An `enum`, `[Flags]` included, any underlying type | yes | yes | the enum | 0, even if no member is 0 | Adding, renaming or reordering members changes nothing stored; changing the underlying type is a new property |
| `string` | yes | yes | `string` | `""` | Allocates at each read; never null: a text never set is empty |
| `GhostString` | yes | yes | `GhostString` / `ReadOnlyGhostString` | empty | A `ref struct`, read without copy; implicit from `string` and `ReadOnlySpan<char>`; compares to a `string` with `==` |
| `GhostSpan<T>` | yes | yes | `GhostSpan<T>` / `ReadOnlyGhostSpan<T>` | empty | An array of values, set from a `T[]` or a span, changed in place by `Set(i, v)` |
| `Option<T>`, `T` a value, an enum, `Guid`, `DateTimeOffset`, a stored struct | yes | yes | `Option<T>` | none | A presence bit; a Ghost of an older version without the property reads none |
| A struct of explicit layout (rules below) | yes | yes | the struct | zeros | Its bytes, as they are |
| A type and its converter `C`, found or named by `[Converter(typeof(C))]` | yes | yes | the type | `C.FromStored(default)` | See value objects |
| A generated `*Ref` | **no** (GBO012) | yes | the `*Ref` | `default`, `IsNull` | 16 bytes; see [identities.md](identities.md) |
| A link to an entity | yes | **no** (GBO020) | Body, `Option`, collection | | See [relationships.md](relationships.md) |
| A Record, `RAddress` | yes | yes | its View, `AddressView` | the empty Record, every field at its default | A value of several fields, read in place, replaced whole: see [Records](#records-a-value-of-several-fields-and-the-bags) |
| `Option<RAddress>` | yes | yes | `Option<AddressView>` | none | |
| `RecordBag`, `RecordBag<R1, …, R8>` | yes | yes | `RecordBag` / `RecordBagView`, with their types | empty | Records in their order, of any type or of these types |

**Refused:** `GhostSpan<RAddress>` and a collection of Records (a bag), an `Option` of a bag (its absence is the empty bag), `[Strong]`, `[Weak]`, `[Delta]` or an index on a Record or a bag (GBO049); `int?` and every `Nullable<T>` (use `Option<T>`), `Option<string>` (a text is empty, never absent), `Option<XRef>` (use `XRef` and `IsNull`), `List<T>`, `T[]`, `object`, any class but `string`, a struct of another assembly (use a converter), a struct without explicit layout, `GhostSpan<string>`, `GhostSpan<DateTimeOffset>`, a property without setter or with `init` (GBO003), `Option<T>` or `GhostSpan<T>` of a converted type.

## Absence: `Option<T>`

```csharp
[Property(5)] Option<DateOnly> AcceptedOn { get; set; }
[Property(7)] Option<decimal> DiscountRate { get; set; }

quote.DiscountRate = 0.05m;                 // implicit T -> Option<T>
quote.DiscountRate = Option.None;           // absent
decimal rate = quote.DiscountRate.GetOr(0m);
if (quote.AcceptedOn.TryGet(out var day)) { /* ... */ }
```

`Option<T>`: `Any()`, `Get()` (throws when absent: use it when the absence is a bug), `TryGet(out x)`, `GetOr(fallback)`, `GetOr(state, static s => ...)`, `Match(some, none)`, `Select`, `Where`, `SelectMany` (each with 1 to 4 states), `foreach` (0 or 1 turn), `==`. A struct: no allocation. No `FirstOrDefault`, no `GetOrDefault`: a fallback is explicit. In a unique index, an absent value is never refused.

## Texts: `string` or `GhostString`

- `GhostString` reads in place: no allocation, the methods of `ReadOnlySpan<char>` and `MemoryExtensions` whose result holds no span (`Length`, `Equals`, `StartsWith`, `Contains`, `IndexOf`, `CompareTo`, `Trim`, `Slice`...), and `Read(Func<ReadOnlySpan<char>, TResult>)` for any other API. `ToString()` copies when needed. Prefer it for the texts of entities and Records.
- A `ref struct` cannot be captured, stored in a field, used as a type argument, held across an `await`, put in an `Option` or in a tuple (`(a.Title, a.Year) = (...)` fails, CS9244): project it (`.ToString()`), compare it in place, assign it in its own statement.
- `string` where `GhostString` is refused: a component of a unique value index, a member of a C# record that leaves GBO.

## Arrays: `GhostSpan<T>`

Elements: the value types above but `DateTimeOffset`, the enums, the stored structs, and in a Record the `*Ref` types. Written from an array, `route.Stops = new[] { 69, 21, 75 };`, changed in place, `route.Stops.Set(1, 71);`, read by the View as `ReadOnlyGhostSpan<T>`, `ToArray()` to copy. An array of an added Record is frozen as its other values.

## Enums and flags

An enum is the right type for a closed set of states (`OrderStatus`), a `[Flags]` enum for options several at once (`Gift | Express`), indexed with `Has`, `One`, `All`, `None` ([../indexes.md](../indexes.md)). An enum whose members start at 1 reads 0 at a birth until set: set it in the object initializer.

**A life cycle is declared on the enum**, by the transitions of each member ([REQUIREMENT-056](../../../../GhostBodyObject.Runtime/Logical/REQUIREMENT-056.md)), not in a Setter or an `OnXChanged`:

```csharp
public enum OrderStatus
{
    [TransitionTo(OrderStatus.Paid)]
    [TransitionTo(OrderStatus.Cancelled)]
    Pending,
    [TransitionTo(OrderStatus.Shipped)]
    Paid,
    Shipped,                                     // final: no transition
    Cancelled
}
```

The check is in the property of every Body, an entity, a part, a Record, a Mutation, and nothing happens at the commit: `order.Status = OrderStatus.Paid; order.Status = OrderStatus.Shipped;` is allowed, in one transaction; `order.Status = OrderStatus.Shipped;` from `Pending` is refused.

| Written | Result |
|---|---|
| A value that the current one designates | Written |
| The current value | Nothing, on a final member too |
| Another value | `GhostTransitionException`, before any write: the transaction goes on |
| On a new Body | The same rule: it is at the value 0, the initial state, and writes its chain: `new Order { Status = OrderStatus.Paid }`, not `Shipped` |
| `Option<T>`: a value over an absent one; `None` over a present one | Written; refused |
| A value over a value of no member (the 0 of an enum that starts at 1) | Written: no state yet |
| In a `GhostSpan<T>`, a field of a struct, a Record assigned whole | Any value |
| By a Merger, in a Replay of an entity | Any value: they write what is decided |

**Keep the transitions on a true life cycle.** A Record or an Intention that names a status follows them too: an enum of a status that a Record names as a fact writes it by the chain, or is a second enum of plain values.

`status.CanTransitionTo(target)`, generated once per enum in `{Enum}Transitions`, asks first: a `Decide` calls it and refuses by its exception of the domain, so that its `Apply` never fails. The rule holds between transactions with nothing added to the commit: two transactions that change one value to two values conflict.

## Stored structs

A struct is stored as its bytes when it is declared **in the compilation of the model** and follows every rule (GBO033):

- `[StructLayout(LayoutKind.Explicit, Size = n)]`, a `[FieldOffset]` on each field;
- each field a value of fixed size and stable layout: an integer, a float, `Half`, `bool`, `char`, an enum, `decimal`, `Guid`, `DateTime`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Int128`, `UInt128`, another stored struct; in the struct of a Record only, a `*Ref` (16 bytes, aligned on 8); never `DateTimeOffset` nor a `string`;
- the fields cover every byte, without gap or overlap, each aligned (free bytes are declared as fields);
- the size is a multiple of the alignment, 32 bytes at most.

```csharp
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct CurrencyAmount(long cents, Currency currency)
{
    [FieldOffset(0)] public readonly long Cents = cents;
    [FieldOffset(8)] public readonly Currency Currency = currency;   // an enum on int
    [FieldOffset(12)] public readonly int Reserved;                  // the free bytes, declared
}

[Property(6)] CurrencyAmount Deposit { get; set; }
```

Its equality is its bytes; its order is none, unless indexed field by field. A changed layout is another type: a new property identifier.

## Value objects: the general rule

**Most fields of a business model are value objects.** A `decimal`, a `string`, a `bool`, a `double` says nothing of what it represents: is this `decimal` a price, a rate, a weight? Is this `string` a code, an e-mail, a name? A value object names the concept, holds its validation and its operators, and makes a wrong assignment or a wrong argument a compile error: a `Money` is not a `Rate`, a `Quantity` is not a line number. A primitive stays for what is truly a raw number without business meaning (a technical counter, a position).

A value object is your own type. **Declare it with `[ValueObject<T>]` first** ([REQUIREMENT-053](../../../../GhostBodyObject.Runtime/Logical/REQUIREMENT-053.md)): you write its rules, GBO writes the rest, and its machine code is that of `T` (verified by the disassembly of the JIT: a read, a comparison, a sum of a `Cents` are those of a `long`). The other forms remain for what one value cannot be.

| Form | For | Stored as | What works (verified by a build and a run) | Limits today |
|---|---|---|---|---|
| **A generated value object**, the first choice: `[ValueObject<T>]` on an interface `VCents` (REQUIREMENT-052), its struct `Cents` generated, its rules in your `partial struct Cents`; a property of the model names `VCents`, a `GhostSpan` the struct | Every concept of one value: `Cents` over a `long` (an amount in cents, exact), `Rate` over a `decimal`, `Isbn` over a `long`, `DueDay` over a `DateOnly`, an identifier, a counter | As `T`, through the converter it holds, `GhostConverter` | `Value`, `From` and `TryFrom` checked by your `static bool IsValid(T value)`, the equality, the order, `Parse` and the formats of `T`, the operators of `Operators` (`Add`, `Subtract`, `Multiply`, `Divide`) and their interfaces, so `Sum` and `[Delta]`; `Option<T>`, `GhostSpan<T>`, the value indexes and their ranges, the merges, the bounds of `[Delta]` in the unit of `T` | One value; an implicit conversion from `T` only with `ImplicitConversion = true` (by default `Cents c = 5;` does not compile: say its unit, `Cents.From(5)`), checked as `From`; never to `T` (`.Value`, `(long)c`) |
| **A generated text**: `[ValueObject<GhostString>]` on an interface `VFirstName` | A text of the domain: a name, an e-mail, a code of any length | As a text, read in place | Two types, exactly as `GhostString` and `ReadOnlyGhostString`: `FirstName`, read and written in place by a Body (`Set`, the indexer of a variable), each text a write would make checked by `IsValid(ReadOnlySpan<char> text)` first; `ReadOnlyFirstName`, read by the Views, converted to `FirstName` without copy; `FirstName name = "Ted";`, checked; `==` with a `string`, `StartsWith`, `Contains`...; a field of an entity, a Record, a Mutation; a parameter; the state of an operator that ends a query (`Count(name, static (u, n) => u.Name == n)`) | A `ref struct`: never a field of a class, an element of a `List`, a parameter of an `async` method, the state of `Where` or `Select`; a Record carries it. It converts implicitly to a `string`, which copies (`$"{name}"`, an assertion), and to a `ReadOnlyGhostString`; `Select` and `OrderBy` take it as their result and their key, `Select(...).Order()` sorts the texts in place |
| **A stored struct**, for a value of several fields: explicit layout, 32 bytes at most, declared in the project of the model | Numbers and values of fixed size: `Money`, `Quantity`, `Rate`, `Weight`, `Level`, a period, a position | Its bytes, without converter | Its operators and methods; `Option<Money>`; `GhostSpan<Money>`; a value index on it (`Is`, `From`, the ranges); `Sum` with `IAdditionOperators` and `IAdditiveIdentity`; `Min`, `Max`, `MaxBy` with `IComparable<T>`; `[Delta]` with `IAdditionOperators` and `ISubtractionOperators`, `[Max]`, `[Min]` with `IComparable<T>`; a `record struct` with `[field: FieldOffset(n)]` | `Average` needs an `INumber<T>`: divide the `Sum`; no bounds on its `[Delta]` |
| **A converter** | A value object over a text (`Sku`, `Email`, `CountryCode`), a type of another assembly or without explicit layout (`Uri`, an existing `record struct`) | Its standard stored type | A typed Body and View; found without `[Converter]` when it is the only one of its type in the project; `Option<T>` of it; a value index by the stored value, ranges with `PreservesOrder`; `Sum`, `[Delta]`, `[Max]`, `[Min]` by its operators, as a stored struct | Never in a `GhostSpan`; converted to a `string`: never in an `Option`, never merged, and it allocates its string at each read |
| **An enum** | A value of a closed set, and **in place of a `bool`** that names a state: `Availability.Available`, not `bool IsAvailable` | Its underlying type | Everything, indexes and flags included | |
| **A Record property**, an interface `R…` (REQUIREMENT-052) | A value of several fields with texts, read and replaced whole, reused in several places: an address | In its host, read in place | Read without allocation, at any depth; an `Option` of it; a bag of them | Changed by its copy, `ToBody()`, then assigned; never indexed; few per entity |
| **A part** | A composite value whose fields change one by one, or with links: the snapshot of a party | An entity of its own | Its fields changed in place; an optional slot | An entity, not a value compared by equality |

**Operators** belong to the value object: arithmetic only where it means something (`Money + Money`, `Money * Quantity`, never `Money * Money`), comparisons (`IComparable<T>`, `<`, `>`), equality (`IEquatable<T>`), and a factory that validates. GBO uses the interfaces of .NET: `IAdditionOperators<T, T, T>` and `IAdditiveIdentity<T, T>` in `Sum`; `IComparable<T>` in `Min`, `Max`, `MaxBy`, `[Max]` and `[Min]`; `IAdditionOperators` and `ISubtractionOperators` in `[Delta]`; the bytes for equality and for the indexes. Write the operators **and** declare these interfaces: an operator alone does not satisfy a generic constraint.

**In the code of a model of value objects** (found by the migration of the seven tutorials, REQUIREMENT-053):
- Declare the value objects **at the head of the model**, before its entities; those several models share, once, in their common namespace.
- Name a value object by its concept, apart from the generated classes: an edge `ERating` gives the class `Rating`, so the stars of a rating are `Stars`.
- A **tuple holds no text value object** (a `ref struct`): assert one value at a time, `Assert.Equal("Low", album.Title)`; assign one property at a time, never `(a.Title, a.Year) = (...)`; a tuple projected by LINQ over a .NET collection takes `ToString()`.
- An **`Option` of a value object takes a value of its type**: `Card = CardNumber.From(1001)`, C# chaining no two conversions.
- The **generated operators never check the rule**, so that they run as `T`: a sum of values bounded by `IsValid` sums their `.Value` (`Sum(static r => r.Stars.Value)`); `Average` takes `.Value`, a value object being no `INumber<T>`.
- Your own operators join the generated ones: `public static TimeSpan operator -(Instant later, Instant earlier) => later.Value - earlier.Value;`.
- Type the parameters of your methods too: `Befriend(Name first, Name second, Instant since)`; a text value object is a parameter of a method that is not `async`.

```csharp
// The first choice: a generated value object, its rule and its display; GBO writes the rest.
[ValueObject<long>(Operators = ValueOperators.Add | ValueOperators.Subtract)]
public interface VCents { }

public partial struct Cents
{
    private static bool IsValid(long value) => value >= 0;
    public override string ToString() => $"{Value / 100}.{Value % 100:D2}";
}

[ValueObject<GhostString>]
public interface VFirstName { }

public partial struct FirstName
{
    private static bool IsValid(ReadOnlySpan<char> text) => text.Length is > 0 and <= 64;
}

// Check, then write: TryFrom says false, From throws, default is the default value, never checked.
if (!Cents.TryFrom(input, out var balance)) return false;
account.Balance = balance;
Cents total = AccountViewCollection.Sum(static a => a.Balance);
```

```csharp
// Written by hand: a stored struct, its operators in the type, no converter; the form of a value of several fields.
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct Money(decimal amount) : IAdditionOperators<Money, Money, Money>, ISubtractionOperators<Money, Money, Money>, IAdditiveIdentity<Money, Money>,
    IComparable<Money>, IEquatable<Money>
{
    [FieldOffset(0)] public readonly decimal Amount = amount;
    public static Money Zero => default;
    public static Money AdditiveIdentity => default;
    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount);
    public static Money operator *(Money price, Quantity quantity) => new(price.Amount * quantity.Units);
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);
    public bool Equals(Money other) => Amount == other.Amount;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => Amount.GetHashCode();
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct Quantity(int units)
{
    [FieldOffset(0)] public readonly int Units = units;
}

public enum Availability { Unavailable, Available }

[Entity(1, 1, 5), Root, Removable, Enumerable]
internal interface MArticle
{
    [Property(1)] GhostString Name { get; set; }
    [Property(2)] VMoney Price { get; set; }
    [Property(3)] Option<VMoney> Discount { get; set; }
    [Property(4)] GhostSpan<Money> PriceHistory { get; set; }
    [Property(5)] Availability Availability { get; set; }
}

Money total = ArticleViewCollection.Sum(static a => a.Price);
Money highest = ArticleViewCollection.Max(static a => a.Price);
```

**Converters**, `IGhostConverter<T, TStored>` in `GhostBodyObject.Runtime.Logical.Model`, for the value objects that a struct cannot be: their static abstract members are inlined by the generator, without virtual call nor boxing.

```csharp
public readonly record struct Sku(string Value);

public readonly struct SkuConverter : IGhostConverter<Sku, string>
{
    public static string ToStored(in Sku value) => value.Value ?? "";
    public static Sku FromStored(in string stored) => new(stored);
    public static bool PreservesOrder => false;
}

public readonly record struct Percent(decimal Value) : IComparable<Percent>
{
    public int CompareTo(Percent other) => Value.CompareTo(other.Value);
}

public readonly struct PercentConverter : IGhostConverter<Percent, decimal>
{
    public static decimal ToStored(in Percent value) => value.Value;
    public static Percent FromStored(in decimal stored) => new(stored);
    public static bool PreservesOrder => true;     // the order of the stored value is the order of the type
}

[Property(3)] Percent Margin { get; set; }                                 // the only converter of Percent, found
[Property(4)] [Converter(typeof(SkuConverter))] Sku Code { get; set; }     // written: required only when Sku has several converters
```

- `TStored` is a standard type: a number, a date, a string, an enum, a stored struct (GBO034 otherwise). A converter on a standard type is the warning GBO035.
- **Found, or written on the property**, never on a type ([REQUIREMENT-051](../../../../GhostBodyObject.Runtime/Logical/REQUIREMENT-051.md)): the only converter of a type in the project is used without `[Converter]`; with several, a property without `[Converter]` is the error GBO046, and `[Converter]` chooses, so that two properties of one type may take two converters. The search covers only the types the model does not store by itself: a stored struct, an enum and a standard type keep their form, and adding a converter never changes how an existing property is stored. A converter of a referenced assembly is always written.
- Equality and order come from `IEquatable<T>` and `IComparable<T>` of the type, otherwise from the stored value.
- In a value index, a converted property is indexed by its stored value, with the options of the stored type (a converter to `decimal` requires `Decimals = n` on its component); a range (`From`, `Before`, `Between`, `Range`) requires `PreservesOrder`, checked in `DEBUG`; a test can call `GhostConverters.CheckOrder<C, T, TStored>(values)`.
- The cost is the conversion: a converter to `string` allocates at each read, one to a number costs nothing measurable.
- A converter that changes but keeps its stored type changes nothing stored.

**Validation of a value object** lives in its own type: its constructor or its factory refuses an invalid value, before it reaches the Setter (check, then write: `if (!Isbn.TryParse(text, out var isbn)) return false; book.Isbn = isbn;`). An optional converted value object is an `Option` of it, stored as the `Option` of its stored type: `[Property(7)] Option<Percent> Discount { get; set; }`.

## Records: a value of several fields, and the bags

A Record, `[Record(typeId, version, maxPropertyId)]` on an interface `R…`, is a value of several fields, without identity nor link. Besides a Log, it is the type of a property of an entity or of another Record, at any depth; a bag holds Records in their order ([REQUIREMENT-052](../../../../GhostBodyObject.Runtime/Logical/REQUIREMENT-052.md), verified by Tutorial Three, Lesson 11).

```csharp
[Record(2, 1, 3)]
internal interface RAddress
{
    [Property(1)] VStreet Street { get; set; }
    [Property(2)] VCity City { get; set; }
    [Property(3)] VPostalCode PostalCode { get; set; }
}

[Record(3, 1, 2)]
internal interface RMove
{
    [Property(1)] VDay On { get; set; }
    [Property(2)] RAddress From { get; set; }        // a Record in a Record
}

[Entity(1, 1, 5), Root, Enumerable]
internal interface MClient
{
    [Property(1)] VName Name { get; set; }
    [Property(2)] RAddress Home { get; set; }        // never absent: the empty Record until written
    [Property(3)] Option<RAddress> Billing { get; set; }
    [Property(4)] RecordBag<RMove> Moves { get; set; }  // these types only, one to eight
    [Property(5)] RecordBag Inbox { get; set; }      // any Record of the Repository
}

// A Record that no property names, a message of the inbox, is included by its name.
[GhostRepository(10), IncludeEntityModel(nameof(MClient)), IncludeRecordModel(nameof(RCallBack)), IncludeRecordModel(nameof(RSendBrochure))]
internal interface MClientRepository { }

var ada = new Client { Name = "Ada" };
ada.Home = new Address { Street = "1 rue de Rivoli", City = "Paris", PostalCode = "75001" };   // copied into the client

var home = ada.Home.ToBody();         // change one field: copy, change, assign; nothing else
home.City = "Lyon";
ada.Home = home;

ada.Moves.Add(new Move { On = new DateOnly(2026, 9, 15), From = ada.Home });   // a bag of a Body is written through it
int inLyon = ClientViewCollection.Count(static c => c.Home.City == "Lyon");   // read in place through the Views

var inbox = ClientViewCollection.Single().Inbox;                             // a bag of a View is a value
foreach (var brochure in inbox.Enumerate<SendBrochureView>()) { /* ... */ }  // the other types are skipped
var work = new List<string>();
inbox.Enumerate(work,                                                       // one walk, a static handler per type
    static (List<string> w, SendBrochureView brochure) => w.Add("mail " + brochure.Subject),
    static (List<string> w, CallBackView call) => w.Add("call " + call.Reason));
if (ada.Inbox.IndexOf<CallBackView>(out int at) && ada.Inbox.TryGetAt(at, out CallBackView call))
    ada.Inbox.RemoveAt(at);                                                   // take one: find, read, remove
```

- **A Record is a value**: its property gives a View, in the Body as in the View, read in place without allocation (`client.Home.City`); a View kept never changes, after a write, the commit, on another thread. The Setter takes a View, a Body (converted) or `Option.None`, and copies its values. `client.Home.ToBody().City = "Lyon"` alone changes only the copy: assign it back.
- **The bag API**, on the Body and the View: `Count()`, `Size`, `Count<T>()`, `Enumerate<T>()` (a GBO sequence and its operators), `Enumerate(h1, …, h8)` with 0 to 4 states first, `TryGetAt<T>(index, out record)` (a position out of the bag throws), `IndexOf<T>(out index)`, `IndexOf(record, out index)`, `Contains(record)`; the View adds `ToBody()`, a free copy; the Body `Add`, `RemoveAt`, `Remove`, `RemoveAll(record)`, `RemoveAll<T>()`, `ReplaceAt`, `Clear()`. `T` is the View of a Record type; equal means the same type, version and values.
- **A bag of a Body** is written through it, without an assignment; a free bag, `new RecordBag<CostView>()`, is assigned (copied). Below the first level, a Record of a bag is changed by its `ToBody()`, then `ReplaceAt`.
- **A constrained bag** refuses at the compilation a write of a type it does not hold, and its reads of one (GBO050). A Record of a type or a version that the process cannot read is counted, skipped, and kept by every copy: a service forwards what it does not know.
- **A Record type belongs to one model**, a Log or a Repository, which declares it (the user, 2026-10-31, REQUIREMENT-057). Never hold a Record of a Log in a Repository, nor a Record of another model in a bag: a Read model is independent of the Mutations, and its `Apply` writes its own values. Two models that hold one Record type: GBO054. Record types that travel in one bag take distinct type identifiers: a bag recognizes its Records by them.
- **A new field** is a new version of the Record type, `[Record(2, 2, 4)]`; its hosts keep theirs; the Records already written read it at its default.
- **A `*Ref` in a Record held by an entity** is a value, never followed: GBO012 does not apply.
- **In an `Apply`**, a Record may be born, to write a model; its `Add()` to a Log stays refused.

**When a Record, when something else** (the rule of REQUIREMENT-052, D19, measured):

| The concept | Its form |
|---|---|
| One value: a price, a code, a day | A value object |
| Several fields with texts, without identity, read and replaced whole, reused in several places: an address | A Record property, **few per entity**: read 1.45 times faster than the same values in a part, changed half as fast |
| The parts of an aggregate, changed one by one, found, referenced: the lines of an order; an optional block with links | Parts, never Records: a part has its identity, its links, its conflicts, its indexes |
| A list of values owned by its host, read whole: a history, the costs of a Mutation | A constrained bag |
| Messages of several types for several services of one Repository: instructions, an inbox | A bag of any Record of the Repository |
| A fact of the domain, in the order of time | A Record of a Log: a bag never replaces a Replay |

Many Records in an entity is a smell: they couple the evolution of all their hosts, none of their fields is indexed, and their host is their unit of conflict.

## Computed and encapsulated values

```csharp
internal partial class Book
{
    public bool IsClassic => Year < 1960;          // a property with a body: computed at each read, never stored
}

internal readonly partial struct BookView
{
    public bool IsClassic => Year < 1960;          // the same on the View, for the queries over the Views
}
```

> **Anti-pattern: a C# field or auto-property in a partial Body.** `public int PriceChanges { get; private set; }` is state outside the model: never stored, never committed, never seen by the Views, by the other transactions, by the replicas, nor rebuilt by a Replay; it starts again at each transaction, which gives a new Body, and a rollback does not undo it. Avoid it at all costs, unless nothing else is possible. Instead: a stored property maintained by an `On...Changed`, when the value belongs to the entity; a Record of a Log, when it is the history of the changes; a local variable of the code that makes the changes, when only that code needs it; a property with a body, when it is computed.

`[Private]` on a value generates its member `private` in the Body and the View: only the partial classes read and write it, and expose it by methods that hold the rules. `[Internal]` makes it `internal`. Both on values only, never on a link, never together (GBO040); they change nothing stored.

```csharp
[Property(2)] [Private] Money Balance { get; set; }

public partial class Account
{
    public Money Available => Balance;

    // Check, then write: a refusal writes nothing, and is a result, not an exception.
    public bool TryDebit(Money amount)
    {
        if (amount <= Money.Zero || amount > Balance)
            return false;
        Balance -= amount;
        return true;
    }
}
```
