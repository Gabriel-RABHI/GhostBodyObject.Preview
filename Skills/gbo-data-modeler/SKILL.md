---
name: gbo-data-modeler
description: Design, port or extend the GBO model of a module - list its concepts, classify each concept, group of fields, relation and question as an entity (Root, part, shared object), an Option, a component, a system, an edge or a value object, fill Design/<Module>/classification.md and have it validated, then write the model interfaces, one namespace per concern, and build them without GBO diagnostics; in Mutation or Event Sourcing, design the Mutations or Records of the module too. Use it before writing or changing any GBO model interface, when porting a relational schema, EF Core classes or a model translated table by table, when a use case needs a concept or a field the model lacks, and when an entity grows.
---

# Data modeler: the model of a module

**Read first:** [the entry point](../gbo/SKILL.md), then [the modeling rules](../gbo/knowledge/modeling/README.md): the knowledge rule, the usage rule, the value rule, the compact anchor, the link rule, when to use each tool. This role applies them; it does not repeat them.

| Contract | |
|---|---|
| **Inputs** | `Design/modules.md`: the module, its responsibility, its use cases, its level, its namespace or project, its range of type identifiers. `Design/technical-base.md`: the style and the schema of each Store. For a port: the source schema or model. |
| **Output** | `Design/<Module>/classification.md`: the classification table and its decision record. Then the model interfaces, one namespace per concern, which build without a GBO diagnostic. |
| **Gate** | The user validates `classification.md`, with the report of gbo-model-reviewer, before the interfaces are written; the interfaces then pass section J of [the checklist](../gbo/knowledge/modeling/checklist.md#j-the-model-as-written-the-checks-before-the-gate). |
| **In parallel** | One modeler per module, in [the order of the models](../gbo-module-architect/SKILL.md#the-order-of-the-models), each in its own namespaces and its own sub-range of type identifiers. Inside a module, one agent at a time writes the model. |
| **Independence** | Its reviewer reads `classification.md`, the interfaces and [the checklist](../gbo/knowledge/modeling/checklist.md), never the reasoning that produced them. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | Where you are | Signals | Branch |
|---|---|---|---|
| D1 | A schema or a model comes from elsewhere | SQL tables, EF Core classes, another ORM, a GBO model translated table by table, an adoption | [Port](#port) |
| D2 | The module has a model, and the mission needs what it lacks | `classification.md` or interfaces of the module exist; a use case needs a concept, a group of fields, a relation, a question | [Extend](#extend) |
| D3 | The module has no model | The module is in `Design/modules.md`; no interface, no `classification.md` | [First model](#first-model) |

In every branch, when the style of the module is Mutation or Event Sourcing, its Mutations or Records are classified in the same table, before the gate: [The Mutations and the Records](#the-mutations-and-the-records).

## First model

1. **List the concepts** from the use cases of the module: the nouns, the states, the relations, the questions of the screens and of the rules.
2. **Classify** each of them with [the checklist](../gbo/knowledge/modeling/checklist.md): sections A and C to F in their order, the first yes deciding; section B whole on each group of fields. Fill the table of section H and the decision record of section I in `Design/<Module>/classification.md`, the Mutations or Records included.
3. **Gate**: submit `classification.md`. No interface before it is validated.
4. **Write the interfaces**: the anchors, then the parts, components, edges and systems; one namespace per concern; each value its value object ([values.md](../gbo/knowledge/modeling/values.md#value-objects-the-general-rule)); `[WithIdentity]` on what is named outside the Repository ([identities.md](../gbo/knowledge/modeling/identities.md)); the type identifiers in the range of the module.
5. **The schema** ([schema.md](../gbo/knowledge/modeling/schema.md#which-schema)): Fixed for a persistent CRUD model and for every Log; Deduced for the models rebuilt from their Log, each `[WithIdentity]` type fixing its `[Entity(typeId)]`.
6. **The concurrency** ([concurrency.md](../gbo/knowledge/modeling/concurrency.md)): aggregates the size of the rule they protect; with a single-writer pump, no concurrency tool at all.
7. **Build**, and fix each GBO diagnostic ([diagnostics.md](../gbo/knowledge/reference/diagnostics.md)): a Repository with an error gets no generated code.
8. **Check** with sections G and J of the checklist, and hand over.

No index in a first model: an index answers a measured question ([indexes.md](../gbo/knowledge/indexes.md#decide-first-no-index)).

## Port

**Never translate table by table, nor class by class.** A source table mixes the concerns that GBO separates; the port is a classification of its columns, not a copy. The worked example is [the audit of the quote port](../gbo/knowledge/modeling/example-quote.md): read it for the method, after your own classification when the port is that very domain.

1. **Read the source as concepts**, within the scope of the mission: its headings first, then, for each concept in scope, the sections that define its fields and its rules; the parts not read are listed in the decision record. For each table or class, its columns grouped by prefix, by meaning, by state, by fill rate (measure it on the data when they are at hand).
2. **Pass section B on each group** and section C on each group extracted. The usual answers:

| In the source | In GBO |
|---|---|
| A group of columns with a prefix (`Billing*`, `Total*`) | A part |
| A flag and its satellite columns (`IsTemplate`, `Template*`) | A component whose presence is the flag |
| Columns meaningful in one state, or for one kind | A component per state or kind |
| Nullable groups, mostly empty | An `Option<T>`, an optional part, a component |
| A code or a name of another table | In the same Repository: a link; a copy only for a snapshot, as a part. In another Repository, or mastered by an outside system: a business key, a value object |
| A link table with two foreign keys and values | An edge (E3), or a Root with two `[Strong]` references when it outlives its ends (E4) |
| Numbered columns (`Street1..3`) | A collection of parts, a `GhostSpan<T>`, links |
| A `Guid` or `int` primary key | Nothing, or `[WithIdentity]` when the entity is named outside |
| A discriminator and subtables (inheritance) | Components on one anchor, systems to query them |
| `decimal`, `string`, `bool`, `double` columns | Value objects: stored structs, converted types, enums |
| Soft-delete columns | A Log for the history; `[Removable]` for what really goes |

3. **Then** steps 3 to 8 of [First model](#first-model). In each row of the table, the column *Alternatives rejected* says what the source did and why GBO does otherwise.
4. **The data**: their import is a use case, written by gbo-implementer by the transition rules of `Design/technical-base.md`.

## Extend

1. **Read** `classification.md` and the interfaces of the module; when the table is missing, rebuild it from the interfaces first, as the facts, by [the audit of gbo-model-reviewer](../gbo-model-reviewer/SKILL.md#audit).
2. **Classify the addition**: section A for a new concept; for a new field, section B on the entity it would join: it stays in the entity only when B1 holds; otherwise it is an `Option`, a part, a component, an edge.
3. **Update** `classification.md`. A change of a validated row is an ADR of the module ([the deliverables](../gbo/SKILL.md#the-deliverables)).
4. **Evolve the schema** ([schema.md](../gbo/knowledge/modeling/schema.md#evolving-a-fixed-model)): in a Fixed model a new number, never a reused one, the version raised, a new required link added as an `Option` first; a Deduced model rebuilt by a Replay changes freely. In a Log, a field added reads its default in old Records; a changed meaning is a new Record or Mutation type.
5. **Then** steps 7 and 8 of First model.

## The Mutations and the Records

In Mutation Sourcing, the model of a module includes its Mutations, one per use case of `Design/modules.md`, each a row of the classification table (its Intention and its Decision in *Declaration*), reviewed at the same gate ([mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md), steps 1 to 3):

- each named as its action, a noun (`OrderPlacement`), never an imperative nor a participle;
- its Intention: what the application knows and asks, minimal, the identities of what it creates drawn by the application;
- its Decision, `[Private]`: everything `Decide` will decide, so that `Apply` never decides again;
- in the Mutation Log (Fixed), in the range of type identifiers of the module.

In Event Sourcing, the Records of the Log: each a fact, complete, naming entities by identity ([logs-chains.md](../gbo/knowledge/logs-chains.md#declare-records-and-a-log)). `Decide`, `Apply` and the handlers are written by gbo-implementer.

## Go up when

| You find | Go up to |
|---|---|
| A concept whose home is another module, or no module | gbo-module-architect, *change the map* |
| A rule that needs links across two system modules | gbo-module-architect: the boundary may be wrong |
| A need of persistence, durability or servers | gbo-boilerplate, *complete* |

## Hand over

After the build and section J: gbo-implementer, *first use cases* (or back to the use case that asked for the extension).

## The deliverable: `Design/<Module>/classification.md`

```markdown
# <Module>: classification

Validated by <who> on <yyyy-mm-dd>, review: Design/reviews/<the report>    (absent until the gate)

## Table
| Concept, group or relation | Unit | Declaration | Owner, entity or ends | Questions that decided | Alternatives rejected, and why |
|---|---|---|---|---|---|
| <an entity, a group of fields, a relation> | | | | | |
| <a value object> | Value object | `Money`: `[ValueObject<long>]` in cents | — | A2, B14 | |
| <a field of the source left out> | Dropped | — | — | | <why> |
| <a Mutation> | Mutation | Intention: ...; Decision: ... | — | A1 | |

## Decision record
- <decision>: <reason>; rejected: <alternative>.

## Type identifiers
<the range of the module, and the identifier of each type>
```
