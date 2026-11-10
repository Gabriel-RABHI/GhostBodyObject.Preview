# The classification checklist

Part of [the modeling knowledge](README.md). It is the checklist of [gbo-data-modeler](../../../gbo-data-modeler/SKILL.md), which passes it before its gate, and of [gbo-model-reviewer](../../../gbo-model-reviewer/SKILL.md), which audits by it. Pass it on **every concept** of a domain before writing an interface, and on **every entity** of an existing model to audit it. Its output is the classification table (section H), validated before the code. In sections A and C to F, answer the questions in their order: the first yes decides. In section B, ask every question of each group: the yes answers say why the group leaves the entity and what it looks like; section C then decides its form.

## A. Each concept: what is it?

| # | Question | Yes → | Stop? |
|---|---|---|---|
| A1 | Did it **happen**, and never changes (a payment received, a parcel scanned)? | A Record of a Log; a use case requested and decided: a Mutation | Yes |
| A2 | Is it only a **value** of another concept, without identity nor life of its own (an amount, a period, a position)? | A property, an `Option<T>`, a value object, a stored struct, a converted type; several fields with texts, read and replaced whole, reused (an address): a Record property, few per entity; changed field by field: a part | Yes |
| A3 | Is it an **aspect** of another concept: some instances have it, for a time, in some states; or a separate concern of it; or shared by several types; or a role? | A component: go to D | Yes |
| A4 | Is it a **relation** between two concepts that carries values? | Go to E | Yes |
| A5 | Is it **created, changed and removed with** another concept, under its rules? | A part of that Owner: `T`, `Option<T>` slot, or collection; survives its Owner while used: `[Orphanable]` | Yes |
| A6 | Is it **shared** by several, owned by none, kept while used (an attachment, a tag)? | A shared object, designated by `[Strong]` | Yes |
| A7 | Otherwise | A Root; `[Removable]` if the business forgets it; reference data: `[Root]` alone | Yes |
| A8 | Then: is it **named outside** the Repository (an API, a Record, a Mutation, another system)? | `[WithIdentity]`, except a component or an edge, which never have one | |
| A9 | Then: is it **found by a question** of the screens or the rules? | Go to F | |

## B. Each entity: is it one responsibility?

Pass these questions on each group of fields of the entity: they apply the usage rule of [the modeling rules](README.md#the-usage-rule-what-each-group-of-fields-is-for) (concept, option, detachment, responsibility, pace, sharing). A Root stays compact: it keeps its identity, the values every instance has for its whole life and its own rules read, and its own structure; it delegates the rest.

| # | Question | Yes → |
|---|---|---|
| B1 | Does the group describe **the entity itself**, and does the entity need it for **its own rules**? | It stays in the entity; otherwise B2 to B14 say where it goes |
| B2 | Is the group **empty for most entities**, or for most of their life? | One value: an `Option<T>`; a group: an optional part or a component |
| B3 | For each group: would the entity still make sense, and keep its rules, **without it**? | It belongs to another concern: B4 to B14 say which |
| B4 | Do some fields share a **prefix or a meaning** (`Billing*`, `Contact*`, `Total*`, `Final*`)? | A part (required, or an optional slot); a Record property when the group is a value read and replaced whole that other places reuse (`Billing*`: an `RAddress`) |
| B5 | Are some fields **set and cleared together**, present on **some entities**, or in **some states** only? | A component |
| B6 | Is there a **flag** `IsX`, `HasX`, `NeedX`? | With satellite fields, or marking an aspect that the entity must not know, or selecting the entities of a system: a component whose presence is the flag; when its data stay while it is off, the component keeps them and the flag becomes its state. Alone, a state of the entity read by its own rules: an enum (B14) |
| B7 | Does a **`Type`, `Kind` or `Status`** decide which fields mean something? | A component per kind or state that has data; keep the enum if nothing depends on it |
| B8 | Does a field hold a **code or a name of another concept**? | In the same Repository: a link to it; a copy for a snapshot only, as a part. Of another system module or of an outside system (an ERP): a value object, the business key, never a link nor an identity |
| B9 | Is a group **repeated** (`Street1..3`, `Family1..3`)? | A collection of parts, a `GhostSpan<T>`, or links; a bag of Records for a list of values that the entity owns and reads whole (a history) |
| B10 | Do fields describe the place of the entity **in another one** (a rank, a level, an index in a family)? | The values of an edge, or of a part of the owning end |
| B11 | Is a field **computable** from others? | A property with a body; but a value frozen at a moment (the totals of a generated document) is stored: it keeps what was true then |
| B12 | Is a field written by **other people, at another pace**, than the rest (stock and description, credit limit and address)? | A component: an aggregate of its own |
| B13 | Is a concern **shared by many types** (audit stamps, comments, synchronization)? | One component attached to each type |
| B14 | Is a field a `bool`, a `string`, a `decimal`, a `double`... that names a **business value** (a price, a rate, a code, a state)? | A value object: a stored struct with its operators, a converted type, an enum in place of a `bool` ([values.md](values.md#value-objects-the-general-rule)) |

## C. Each extracted concept: which link back?

A group taken out of an entity, or a concept met in A, is linked by the link rule of [the modeling rules](README.md#the-link-rule-how-an-extracted-concept-is-linked-back). The first yes decides.

| # | Question | Yes → |
|---|---|---|
| C1 | Does it stand **between two concepts**, carry values, and belong to neither? | An edge (E3); if it must not vanish with an end or is named by its own identity, a Root with two `[Strong]` references (E4) |
| C2 | Is it **the anchor's own structure**: the anchor creates and removes it, the anchor's rules read it? | An aggregation: `T` if every anchor has exactly one, a slot `Option<T>` for one or none, a collection for many |
| C3 | Can an anchor have **several** of it, or does it need **its own identity**, or does it **move** to another anchor? | An aggregation (a part, `[WithIdentity]` if named outside, `MoveTo`): never a component |
| C4 | Is it an **aspect the anchor must not know**: some entities, some states, a block that comes and goes, shared by types, another module, another pace, at most one per entity? | A component: check its limits in D |
| C5 | Does it have **a life of its own**, designated by others, outliving the anchor? | A Root, and a `[Strong]` or `[Weak]` reference from the one that needs it |
| C6 | Is it **shared** by several anchors, owned by none, kept while used? | A shared object, designated by `[Strong]` |
| C7 | Does the extracted concept hold **a list of entities**, or groups of its own? | Pass B and C on it in turn: a component may own parts, which may have components of their own (the delegation tree) |

## D. Each component candidate: are its limits respected?

| # | Question | Answer → |
|---|---|---|
| D1 | Can an entity have **several** of it? | Not a component: parts of a component, or a collection of parts |
| D2 | Must it be **named from outside by its own identity**, independently of its entity? | Not a component: a part with `[WithIdentity]`, or a Root |
| D3 | Does it **move** to another entity? | Not a component: a part (`MoveTo`) or a Root |
| D4 | Does every entity have it, for its whole life, as its own responsibility? | Not a component: a property or a part |
| D5 | Is it shared by several entity types? | `[ComponentFor]` repeated |
| D6 | Does it own sub-structures? | Parts of the component |
| D7 | Does it only make sense with another aspect? | A second component of the same entity, and a system that joins them |
| D8 | Is it an aspect of a part of the entity? | A component of the part type |
| D9 | Do its rules need another aspect? | They go in a system, not in the component |

## E. Each relation between two concepts

| # | Question | Yes → |
|---|---|---|
| E1 | Does one end **create, own and remove** it? | A composition of that end; with values, a part holding a `[Strong]` reference to the other end |
| E2 | Does it carry **no value**? | References: `[Strong]` if the target must stay, `[Weak]` if it may go; a collection for many; a reverse property only if the code reads it |
| E3 | With values, owned by neither end, **vanishing with either**, and both ends always there? | An edge; several per pair: `Multiple`; no direction: `Symmetric` (one type). An end that may be absent or unknown (a virtual root, a code of an outside system) is not an edge: a part of the known end |
| E4 | With values, owned by neither end, but **outliving an end**, or designated by **its own identity**, or by other entities as a fact? | A Root with a `[Strong]` reference to each end |
| E5 | Must something **keep** the relation alive? | A strong reference to the edge: it retains the edge and its two ends |
| E6 | Is the relation **named by a Record or a Mutation**? | An edge is named by its pair of ends (one per pair), or by its pair and one of its values (`Multiple`); otherwise E4 |
| E7 | Is the target in another Repository of the chain? | No link: a derived identity, `XRef.Of(source)` |

## F. Each question of the screens and the rules

| # | Question | Yes → |
|---|---|---|
| F1 | Does it select the entities that **have, may have, or lack** some aspects? | A system: required, `Option`, `[ExcludeComponent]` |
| F2 | Does a rule need **several aspects** of one entity? | A method of the partial struct of the system |
| F3 | Is it a condition on **values** only? | A query over the system or the Views |
| F4 | Does it read **a few entities among very many**, on a hot path, unreachable by navigation? | An index, measured ([../indexes.md](../indexes.md)) |

## G. The smells of an existing model

Pass these on each entity; each one found is a row of the classification table to redo.

| Smell | Probable cause |
|---|---|
| An entity whose groups of fields have different uses: some mostly empty, some about other concepts, some for one state | Several concerns in one entity: B, then C |
| A Root that holds its states, its roles, the concerns of other modules | A Root that does not delegate: aggregations and components |
| A family of properties with a common prefix | A part, or a Record property for a value read and replaced whole |
| Many Record properties in one entity; Records in place of the parts of an aggregate (order lines in a bag) | Few Records per entity, for reusable values; the parts of an aggregate are parts: identity, links, conflicts, indexes |
| A boolean and its satellite fields | A component |
| A group of fields empty for most entities | An `Option<T>`, an optional part, or a component |
| Fields meaningful in one state only (`GenerationDate`, `AcceptedBy`) | A component of that state |
| A `Type` or `Kind` and fields used by one kind only | Components per kind |
| A `string XCode` or `XName` of another concept | A link in the same Repository; a business key, a value object, toward another system module or an outside system |
| Numbered fields (`Street1..3`, `Family1..3`) | A collection, or links |
| A Root with two codes or two links and a few values | An edge, or a Root of a relation that outlives its ends |
| A rank, a level, an index of the entity in another concept | The values of an edge |
| The same information held twice (a code and a link to the same concept) | One link |
| A rule in an entity that reads another aspect | A system |
| A reverse property the code never reads | Remove it |
| `bool`, `string`, `decimal`, `double` fields that say nothing of what they represent | Value objects, enums |
| A C# field or auto-property in a partial Body | State outside the model: a stored property, a Record, a local variable, or a property with a body |

## H. The classification table

Fill one row per concept, then one row per split group of an entity, per relation, and per Mutation or Record. A value object has its row (its type in *Declaration*, no Owner); a field of the source left out has its row too (unit *Dropped*, and why). An audit adds a column *Today*, the current form, before *Unit*. It is the document validated before the code.

| Concept, group or relation | Unit | Declaration | Owner, entity or ends | Questions that decided | Alternatives rejected, and why |
|---|---|---|---|---|---|
| Quote | Removable Root, identity | `[Root] [Removable] [WithIdentity] MQuote` | | A7, A8 | |
| Template data of a quote | Component | `[ComponentFor(nameof(MQuote))] CTemplate` | Quote | B2, B5, B6, D1-D4 | A flag and six fields on the quote: most quotes leave them empty |
| Customer snapshot of a quote | Part | `MQuoteParty Primary { get; }` | Quote | B4, B8 | A link to the customer: the quote keeps the values of its date |
| Product proposed with a product | Edge | `[Edge(nameof(MProduct), nameof(MProduct))] ELinkedProduct` | Product, product | E3, E6 | A Root with two codes: it vanishes with either product, and is named by its pair |
| The quotes to validate | System | `[SystemFor(nameof(MQuote))] SQuoteToValidate` | Quote, `CValidationRequest` | F1 | A query on three booleans |

## I. The decision record

For each row that is not obvious, one line: the decision, the reason, the alternative rejected. A later change of the model starts by reading it. Once the table is validated, a change of one of its decisions is also an ADR of the module (see [the deliverables](../../SKILL.md#the-deliverables)).

## J. The model as written: the checks before the gate

Pass them on the interfaces, after the build, before the model is handed over.

- [ ] Every row of the table is in the interfaces, and every interface has its row.
- [ ] Each type has a kind (Root, part, shared object, component, edge, system, Record, Mutation) and a place of birth.
- [ ] Each link declares its nature: compositions have no setter; weak links are `Option<T>` or collections; a required link `T` is given at the birth.
- [ ] Aggregates are the size of the rule they protect: the orders are not parts of the customer ([concurrency.md](concurrency.md)).
- [ ] Capabilities that come and go are components, never nullable properties nor inheritance.
- [ ] Values are value objects: `[ValueObject<T>]` interfaces `V…`, stored structs, converted types, enums; no C# field or auto-property in a partial Body (GBO047).
- [ ] A Record property for a reusable value of several fields, few per entity, never the parts of an aggregate; a bag for a list owned by its host or for messages; each Record type has one home, a Log or a Repository that includes it, or one model whose types reach it (GBO051).
- [ ] `[WithIdentity]` on what is named outside the Repository only; no identity value in an entity: no `*Ref` (GBO012), and no struct of the domain that implements `IGhostIdentity` nor `Guid` key of another entity, which GBO012 does not see; no link toward another Repository: `XRef.Of`.
- [ ] Fixed: numbers unique, never reused, versions raised; Deduced: `[Entity(typeId)]` on each `[WithIdentity]` type ([schema.md](schema.md)); type identifiers in the range of the module.
- [ ] One namespace per concern, never named like a generated type; the Repository includes them (`[IncludeNamespace(..., SubNamespaces = true)]`, or by name for components, systems and edges).
- [ ] No index without a measured question; none yet in a first model.
- [ ] The build reports no GBO diagnostic.
