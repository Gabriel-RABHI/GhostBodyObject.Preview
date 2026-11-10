# Modeling a domain in GBO: separate the concerns, then link them

The model is the most important decision of a GBO application. The method has one goal: **each unit of the model carries one responsibility, and the units know as little of each other as possible.** An entity carries its own responsibility only; each other aspect is a part, a component, a system or an edge; the core concepts make sense with no aspect around them, and an aspect is added or removed without touching them.

**Prerequisite:** the rules of [../../SKILL.md](../../SKILL.md). Declarations are interfaces: `M` for an entity, a Repository, a Log or an index; `R` for a Record, a Mutation included; `C` for a component; `S` for a system; `E` for an edge; `V` for a value object (GBO007).

**Never translate a relational schema table by table**: a table that mixes concepts, a flag and its satellite columns, a code that designates another table, a link table with two foreign keys are the symptoms that this method resolves. Classify first ([checklist.md](checklist.md)), write the interfaces after.

## The topic files

| File | Holds |
|---|---|
| [checklist.md](checklist.md) | **The classification procedure**: the questions per concept, per group of properties, per relation, per question of the screens; the audit of an existing model; the classification table to fill and validate |
| [example-quote.md](example-quote.md) | A worked audit: the quote model reclassified |
| [components-ecs.md](components-ecs.md) | Components, sub-components, systems, roles, polymorphism by C# interfaces |
| [edges.md](edges.md) | Edges: relations that carry values, graphs, walks |
| [relationships.md](relationships.md) | Compositions (three forms), strong and weak references, shared objects, orphans, trees, reverse properties, births, what dies and when |
| [values.md](values.md) | The types of a property, `Option<T>`, texts and arrays, enums, structs, converters: the value objects |
| [identities.md](identities.md) | `[WithIdentity]`, `*Ref`, projections `XRef.Of` |
| [concurrency.md](concurrency.md) | Aggregates as units of conflict, `[ConcurrencyPoint]`, `[Mergeable]`, `Claim()`, leases, Exclusive transactions |
| [schema.md](schema.md) | Fixed and Deduced schemas, evolution of a model |
| [patterns.md](patterns.md) | Complete models of common domains |

## The knowledge rule: who declares whom is who depends on whom

| Tool | Declared by | Knows | Stays ignorant |
|---|---|---|---|
| A property | The entity | | |
| A part (composition) | The Owner: `IEntityCollection<MLine> Lines { get; }`, `MAddress Billing { get; }` | The Owner knows its parts; a part reads its Owner (`[OwnedFrom]`) | |
| A reference `[Strong]`/`[Weak]` | The referrer | The referrer knows its target | The target, unless a reverse property |
| A reverse property | The target | The target knows its referrers | Coupling added: declare it only when read |
| A component | The component: `[ComponentFor(nameof(MQuote))]` | The component knows its entity | **The entity**: it declares nothing; a new aspect changes nothing of it, not even its version |
| An edge | The edge: `[Edge(nameof(MProduct), nameof(MFamily))]` | The edge knows its two ends | **Both ends**: they declare nothing |
| A system | The system: `[SystemFor]`, its members | The system knows the entity and the components it joins | **The entity and the components**: no component knows another; the code that needs two aspects lives in the system |

Prefer the declaration that adds the least knowledge. Composition and reference make the declaring entity depend on the other: use them for what is the entity's own responsibility. Components, edges and systems keep the anchors ignorant: use them for the other concerns.

**Modules are namespaces.** One namespace (folder) per concern: `Quotes.Model` (the anchors), `Quotes.Model.Templates` (`CTemplate`, `STemplateQuote`, their rules in partial classes), `Quotes.Model.Validations`, `Quotes.Model.Prices`. The Repository takes them all: `[IncludeNamespace("Quotes.Model", SubNamespaces = true)]`. The business code of an aspect lives in the partial class of its component or of its system, never in the entity. A Repository includes the model interfaces of its own project only: the modules of one model are namespaces of one project. **Never name a namespace of the model like a generated type**: a namespace `Quotes.Model.Pricing` beside a component `CPricing` (its Body `Pricing`) breaks the generated code (CS0118); name the modules in the plural or by another word.

## The usage rule: what each group of fields is for

Each group of fields of an entity is judged by its use:

| Criterion | Keep the fields in the entity when | Separate them when |
|---|---|---|
| **Concept** | They describe the entity itself | They describe another concept: a party, an address, a document, a relation |
| **Option** (fill rate) | They are filled for almost every entity, all the time | They are empty for most entities: one value is an `Option<T>`; a group is an optional part or a component |
| **Detachment** | They live with the entity, from its birth to its end | They come and go as a block, set and cleared together, in some states or periods only: a component, an optional part |
| **Responsibility** | The entity needs them for its own rules | The entity keeps its sense and its rules without them: another concern |
| **Pace and writers** | The same use cases write them, at the same pace | Other people, another pace: a component, an aggregate of its own |
| **Sharing** | They are specific to this type | Several types share them: one component attached to each |

What a separated group becomes:

| Inside the entity | Becomes |
|---|---|
| A group of fields of a common prefix or meaning: an address, a contact, a snapshot of a party, the totals | A **part** (aggregation): `MAddress Billing { get; }`, an optional slot `Option<MAddress> Delivery { get; }` |
| A group set and cleared together, present on some entities only, or in some states only: template, generation, decision, end of life, cover page | A **component** |
| A flag `IsX`, `HasX`, `NeedX`, with or without satellite fields | A **component whose presence is the flag**: `CEndOfLife`, `CValidationRequest` |
| A `Type`, `Kind` or `Status` that decides which fields mean something | One **component per kind or per state** that has data; the enum alone when no data depends on it |
| A single value that may be absent | `Option<T>` |
| A repeated group: `Street1..3`, `Family1..3`, `Phone1..2` | A collection of parts, a `GhostSpan<T>`, or links |
| A code or a name of another concept: `CommercialCode`, `AgencyCode`, `CustomerName` | A **link** to that concept; a copy only for a snapshot that must not follow it, and then a part |
| Fields about the place of the entity in another one: rank, level, position in a family | A value of an **edge**, or of a part of the owning end |
| A value computable from others | A property with a body in the partial class |
| A cross-cutting concern of many types: audit stamps, synchronization state, comments | **One component attached to each type** (`[ComponentFor]` repeated): the types stay ignorant of it |

The two tests of each group of fields: **is it filled for almost every entity, for its whole life?** and **would the entity still make sense, and keep its rules, without it?** A group mostly empty, or that the entity does not need, belongs to another unit.

## The value rule: the values are value objects

**Most fields of a business model are value objects.** A `bool`, a `string`, a `decimal`, a `double` says nothing of what it represents, and lets a price go where a rate is expected. Each value concept of the domain gets its type, with its validation and its operators:

| The value | Its type |
|---|---|
| A number of the business, a value of one field: a price in cents, a quantity, a rate, a weight, a level, an identifier | **A generated value object**, the first choice: `[ValueObject<long>] interface VCents { }`, your rule `IsValid` and your `ToString()` in a `partial struct Cents`, the rest written by GBO, the machine code of the `long`, stored as it, usable in an `Option`, a `GhostSpan`, an index, `Sum` and the merges |
| A text of the business: a name, an e-mail, a code | **A generated text**: `[ValueObject<GhostString>] interface VFirstName { }`, read in place, carried by a Record; two types as `GhostString` and `ReadOnlyGhostString`, `FirstName` for the Bodies, `ReadOnlyFirstName` for the Views |
| A value of several fields of fixed size: an amount and its currency | **A stored struct** of explicit layout: stored as its bytes, its operators in the type |
| A type of another assembly, `Uri` | **A converted type**: its converter is found without `[Converter]` when it is the only one of its type; `[Converter(typeof(...))]` chooses among several |
| A state, a choice in a closed set; a `bool` that names a state | **An enum**: `Availability.Available`, not `bool IsAvailable`; a `bool` with satellite fields is a component |
| A composite value with texts, read and replaced whole, reused in several places: an address | **A Record property**, few per entity: `RAddress Home { get; set; }`, changed by `ToBody()` and an assignment ([values.md](values.md#records-a-value-of-several-fields-and-the-bags)) |
| A composite value changed field by field, or with links: a party snapshot | **A part** |

The forms, what GBO does with each (operators, `Option`, spans, indexes, aggregates) and their limits today are in [values.md](values.md#value-objects-the-general-rule). A value that only the business code may change is `[Private]`, exposed by the methods that hold its rules.

**Never a C# field or an auto-property in a partial Body** (`public int PriceChanges { get; private set; }`): state outside the model, never committed, replicated, seen by the Views nor rebuilt by a Replay, reset at each transaction. Avoid it at all costs, unless nothing else is possible: a stored property kept by `On...Changed`, a Record, a local variable, or a property with a body.

## The compact anchor: a Root delegates its aspects

A Root keeps only what makes it itself: its identity, the values that every instance has for its whole life and that its own rules read, and the declarations of its own structure. It **delegates** every other aspect:

- to **aggregations** (parts), for what it owns and rules: its lines, its address, its totals;
- to **components**, for what it must not know: its states, its roles, its optional capabilities, the concerns of other modules.

The delegation goes down as far as the concepts need, each level compact in its turn: **a component may own lists of entities, which have components of their own**, which may own parts again.

```
MParty                              Root: the party itself, its name
 ├─ CCustomer                       component: the customer role, its credit limit
 └─ CSupplier                       component: the supplier role, its payment days
     └─ Offers                      parts of the component: IEntityCollection<MOffer>
         └─ CPromotion              component of an offer: the offers on promotion only
             └─ Tiers               parts of the component: IEntityCollection<MPromotionTier>
```

```csharp
[ComponentFor(nameof(MParty))]
internal interface CSupplier
{
    int PaymentDays { get; set; }
    IEntityCollection<MOffer> Offers { get; }       // a component owns a list of entities
}

[Entity(2), WithIdentity, Enumerable]
internal interface MOffer
{
    GhostString Product { get; set; }
    decimal Price { get; set; }
    [OwnedFrom] CSupplier Supplier { get; }
}

[ComponentFor(nameof(MOffer))]                      // which have components of their own
internal interface CPromotion
{
    decimal Rate { get; set; }
    DateOnly Until { get; set; }
    IEntityCollection<MPromotionTier> Tiers { get; }
}

[SystemFor(nameof(MOffer)), Enumerable]
internal interface SPromotedOffer { MOffer Offer { get; } CPromotion Promotion { get; } }
```

The values keep their .NET types to keep the tree short; in a model they are value objects (`Money Price`, a `PaymentTerms`), by [the value rule](#the-value-rule-the-values-are-value-objects).

- Each node is removed with its parent, at the same instruction: removing the supplier role removes its offers, their promotions and their tiers; the party stays.
- A node reads up its tree: `offer.Supplier.Party`.
- A system may be over any node type: `SPromotedOffer`, the offers that have a promotion.
- Each component is an aggregate of its own: adding an offer changes the structure of the supplier role, never the party.

(This tree was built and run against the generator, in a Deduced Repository.)

## The link rule: how an extracted concept is linked back

Once a concept or a group of fields leaves an entity, criteria of the same kind decide its link:

| Criterion | Aggregation (a part) | Component | Edge | Root and a reference |
|---|---|---|---|---|
| **Knowledge** | The anchor declares it: its own structure | The anchor ignores it; the component declares its entity | Neither end knows it; it declares both | The referrer knows the target |
| **Ownership and life** | Born in its Owner, removed with it or by it | Removed with its entity; comes and goes by itself | Removed with either end | Its own life; the reference refuses its removal (`[Strong]`) or forgets it (`[Weak]`) |
| **Rules** | Under the rules of its Owner: a total, a limit | Its own; with other aspects, in a system | Its own values | Its own |
| **Cardinality** | One (`T`), one or none (a slot), many (a collection) | At most one of its type per entity | One per pair, or several (`Multiple`) | Many referrers to one target |
| **What it attaches to** | One Owner, of one or several types (a tree) | One entity, of one or several types (a shared aspect) | Two entities, which it relates | One target per reference |
| **Identity** | May have `[WithIdentity]` | Never: named by its entity | Never: named by its pair | Usually `[WithIdentity]` |
| **Moves** | `MoveTo` another Owner | Never | Never: another pair is another edge | The Setter designates another target |
| **Concurrency** | Its structure conflicts with its Owner, its values do not | An aggregate of its own | An aggregate of its own | An aggregate of its own |
| **Examples** | The lines of an order, an address, totals, a party snapshot | A role, a state (generation, decision), a template, an end of life, audit stamps | A rating, a membership, a product in a family | An order of a customer, a transfer between accounts |

Decide in this order:

1. **Between two concepts, carrying values, owned by neither?** An edge. If it must outlive an end, or is named by its own identity: a Root with two `[Strong]` references.
2. **The anchor's own structure**: the anchor creates and removes it, its rules read it; or there are several per anchor, or it needs an identity, or it moves? An aggregation.
3. **An aspect the anchor must not know**: some entities or some states only, coming and going as a block, shared by several types, owned by another module, written at another pace, at most one per entity? A component.
4. **A concept with a life of its own**, designated by others, outliving the anchor? A Root, and a `[Strong]` or `[Weak]` reference from the one that needs it.
5. **Shared by several, owned by none, kept while used?** A shared object.

## When to use each tool

### An entity, the anchor

**Use** for a concept with a business identity, a lifecycle, designated by others. Choose its kind by who decides its end:

| Kind | Declaration | Lives | Born by |
|---|---|---|---|
| Removable Root | `[Root]` `[Removable]` | Until `Remove()`, refused while a strong reference retains it | `new X { ... }` |
| Root (reference data) | `[Root]` | Forever; a strong reference to it costs nothing | `new X { ... }` |
| Part | Composed by its Owner | With its Owner | `owner.Parts.New()`, a slot, or with its Owner |
| Orphanable part | `[Orphanable]` | With its Owner, or while retained | As a part |
| Shared object | Neither Root nor composed, designated by `[Strong]` | While a strong reference designates it | By its first strong reference |

**Not** when the concept is only a value of another (a property, a struct, a converter), an aspect of another (a component), a relation (an edge), or a fact (a Record). A Removable Root that past facts designate is rather not removed: `bool Discontinued`.

### A part (aggregation)

**Use** when the concept is created, changed and removed with its Owner, inside the Owner's responsibility, under the Owner's rules (a total, a limit of lines): exactly one (`T`, born with the Owner, never replaced), one or none (an `Option<T>` slot), or many (`IEntityCollection<T>`). It is also the form of a value object with texts (an address, a party snapshot).
**Not** when it lives a life of its own or is designated as an independent thing by other aggregates (a Root); when the Owner must not know it (a component); when it would make the Owner a point of contention (the orders of a customer).

### `Option<T>`

**Use** for one value, one part (a slot), or one reference that may be absent.
**Not** for several values always set and cleared together: that is a part or a component. Three `Option` that move together are one concept.

### A reference

**Use** `[Strong]` when the target must stay while designated (the product of an order line), `[Weak]` when it may go and the link forget it (a history, a suggestion). A collection of references for a relation without values (a like, a tag, a favorite); a reverse property only where the code reads it.
**Not** for a relation that carries values (an edge, or a part of the owning end); never a foreign key as a code or a `*Ref` in an entity.

### A component

**Use** for an aspect of an entity when any of these holds:
- only some entities have it, or only for a time, or only in some states;
- its fields are set and cleared together;
- several unrelated types share it (everything that can be audited, commented, synchronized), without a base class;
- it is a role (customer, supplier, employee of one party);
- it is a separate concern that the entity must not know (templates of a quote, pricing of a line);
- it changes at another pace, by other people: as an aggregate of its own, it does not conflict with its entity.

**Not** when (the limits are discriminating):
- the entity has several of it: parts of a component, or a collection of parts;
- it needs an identity of its own, named from outside independently of its entity: a part with `[WithIdentity]`, or a Root (a component has no identity; a Record names it by the `*Ref` of its entity and its type);
- it moves to another entity: a part (`MoveTo`) or a Root;
- every entity has it for its whole life, and it is the entity's own responsibility: a property or a part.

A component may attach to a Root, a part (the kinds of the lines of a quote) or several types; it is removed with its entity; it may own parts; it is never the component of a component, never `[WithIdentity]`, never a concurrency point; include it by name or namespace.

### A sub-component

GBO has no component of a component (GBO029). An aspect of an aspect takes one of three forms:

| The need | Form |
|---|---|
| The aspect owns sub-structures (the offers of a supplier role, the alarms of an alarm log) | **Parts of the component**: they live and die with it |
| A second aspect that only makes sense with the first (a discount on the pricing of a line) | **A second component of the same entity**, and a system that joins them (`SDiscountedLine`) |
| An aspect of a part of the entity (the pricing of each line of a quote) | **A component of the part type**: `[ComponentFor(nameof(MQuoteLine))]` |
| A list of entities inside an aspect, which have aspects of their own (the offers of a supplier role, some on promotion) | **Parts of the component, and components of those parts**: the delegation tree of [the compact anchor](#the-compact-anchor-a-root-delegates-its-aspects) |

### A system

**Use** when a question or a rule concerns a combination of aspects: "the quotes that are templates", "the locks on battery not under maintenance", "the lines priced and discounted". It reads only the entities of its rarest required component, gives Views, sees each change at the next instruction, and its partial struct is **the home of the rules that need several aspects**: the code that would otherwise make one component know another.
**Not** for a condition on values only (a query over the system or the Views; an index only when measured). A system holds no data.

### An edge

**Use** when three answers are yes: the relation carries values; it belongs to neither end; it must vanish with either end. One per pair, `Multiple = true` for several, `Symmetric = true` for no direction.
**Not** when one end owns it (a part of that end), when it carries no value (references), when it must outlive an end or is designated by its own identity (a Root with two `[Strong]` references: a transfer, a contract, a booking). An edge has no identity: a Record or a Mutation designates it by its pair of ends (`a.XEdges.To(b)`), and with `Multiple` by its pair and one of its values.

## X or Y?

| Hesitation | X when | Y when |
|---|---|---|
| Property or component | Every entity has it, for its whole life, as its own responsibility | Some entities, a time, a state, a group of fields, a separate concern |
| Part or component | The Owner's responsibility, under its rules, possibly several | An aspect the entity must not know, at most one, possibly on several types |
| Optional part or component | The Owner decides and knows it (the proof of a delivery) | An aspect added from outside, queried by systems, shared by types |
| Component or Root | Lives with the entity, at most one, no identity | Its own identity, designated from outside, outlives the entity, several per entity |
| Edge or part of an end | Owned by neither end | One end owns it (the lines of an order) |
| Edge or Root with two `[Strong]` | Vanishes with either end, found by its pair | Outlives its ends, or designated by its own identity |
| Edge or collection of references | Carries values | No value |
| System or query | A combination of aspects, read often | A condition on values |
| System or method of the entity | The rule needs several aspects | The rule needs the entity's own values |
| Reverse property or index | The referrers by one reference, a set | An order, a range, a top-N |

## The procedure

1. **List the concepts** of the domain: its nouns, its states, its relations, the questions of its screens and rules.
2. **Classify** each of them with [checklist.md](checklist.md), and fill the classification table: unit, declaration, the questions that decided, the alternatives rejected. Have it validated before any code.
3. **Write the interfaces**, one namespace per concern; anchors first, then parts, components, edges, systems.
4. **Build**: fix the GBO diagnostics ([../reference/diagnostics.md](../reference/diagnostics.md)).
5. **Audit** the result with the smells of [checklist.md](checklist.md): no group of fields mostly empty, no flag with satellites, no code of another concept, no field the entity does not need.

## The other decisions

**Values** ([values.md](values.md#value-objects-the-general-rule)): **most fields of a business model are value objects** (a `decimal`, a `string`, a `bool` says nothing of what it represents): a generated value object first, `[ValueObject<T>]` (`Money`, `Quantity`, `Rate`, a text `Email`), declared at the head of the model, then a stored struct of explicit layout for several fields, a converted type for a foreign type, an enum in place of a `bool` that names a state; then the .NET types; `Option<T>` for an absent value (never `int?`; a text is empty, never absent); `GhostString` for texts (no copy); `GhostSpan<T>` for arrays; enums; a value object that cannot be a stored struct is its own type stored by a **converter**, found by the generator when it is the only one of its type (`[Converter(typeof(MoneyConverter))]` chooses among several); a small composite value of fixed size is a struct of explicit layout (≤ 32 bytes); a composite value with texts is a part; a value only the business code may change is `[Private]`.

**Identity** ([identities.md](identities.md)): `[WithIdentity]` on what is known outside the Repository (APIs, Records, Mutations, other systems): `XRef.New()`, `ref.Resolve()`; projections take `ReadXRef.Of(xRef)`. Components and edges never have one.

**Finding** ([../indexes.md](../indexes.md)): navigation (links, slots, edges), identity, reverse property, system, query over the Views, and an index last, for a measured need.

**Concurrency** ([concurrency.md](concurrency.md)): an aggregate is a unit of conflict, sized by the rule it protects; a value of a part does not conflict with another part (`[ConcurrencyPoint]` for an invariant over the parts); a counter shared by many writers is a `[Delta]` of a `[Mergeable]` type; components and edges are aggregates of their own. With a single-writer Mutation pump, none of this conflicts.

**Schema** ([schema.md](schema.md)): Fixed numbers for persistent models and every Log; Deduced for the models rebuilt by a Replay at each start, which then change freely.

## The anti-patterns

| Anti-pattern | Instead |
|---|---|
| An entity whose groups of fields are mostly empty, or belong to other concepts | The usage rule, then the link rule: parts, components, options, edges, links |
| A Root that holds its states, its roles and the concerns of other modules | A compact Root that delegates them to aggregations and components |
| A table translated as an entity, column by column | Classify the concept and each group of its columns |
| `bool IsTemplate` and six `Template*` fields | A component `CTemplate`, and a system for the templates |
| `bool NeedValidation`, `DateTime ValidatedAt`, `Guid ValidatedBy` | A component per pending request, removed when done |
| `string CommercialCode` designating another entity | `[Strong] MCommercial Commercial` |
| `Street1`, `Street2`, `Street3`, `ZipCode`, `City` on a customer | A part `MAddress` |
| A Root with two string codes and a rank, designating two entities | An edge with its rank |
| A foreign key, `Guid CustomerId` or `CustomerRef Customer`, in an entity | `[Strong] MCustomer Customer` |
| Inheritance, a `Type` discriminator, nullable columns for subtypes | Components on one anchor; systems to query them |
| The rule that needs two aspects written in the entity | A method of the system that joins them |
| An `Id` property and `Where(x => x.Id == id)` | `[WithIdentity]` and `ref.Resolve()` |
| A position kept by a list | A `Position` property and `OrderBy` |
| The orders as parts of the customer | Orders as Roots designating their customer |
| An index per searchable property | Navigation, systems and Views first |
| A counter on a Root updated by every transaction | A `[Delta]` of a `[Mergeable]` type, or the single-writer pump |
| `Option<string>` for a missing text | An empty `GhostString` |
| `decimal Price`, `string Email`, `bool IsActive`, `double Weight` | Value objects: `Money`, `Email`, an enum, `Weight` |
| A C# field or auto-property in a partial Body | Never: a stored property, a Record, a local variable, or a property with a body |
| A soft-delete flag on every type | A Log of Records for the history, `[Removable]` for what really goes |
