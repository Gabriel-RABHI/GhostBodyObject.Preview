---
name: gbo-module-architect
description: Divide the data of a GBO application into modules - list its use cases, group them by responsibility, give each concept one home, place each module in a shared model as a namespace (a concern module) or in a Repository of its own (a system module), decide the Repositories with their chains, styles, writers, servers and type identifier ranges, and the one-way dependencies between modules. Use it after the technical base exists and before any model is written, when a business area belongs to no module, when the schemas or services of an existing system must be regrouped, or when a module grew too large. Produces Design/modules.md, the module map.
---

# Module architect: the module map

**Read first:** [the entry point](../gbo/SKILL.md): the mental model, the rules, the paths, the deliverables.

| Contract | |
|---|---|
| **Inputs** | `Design/technical-base.md` (the style, the chain pattern); the mission: the business areas, the use cases. For inherited modules: the existing schemas, services or packages. |
| **Output** | `Design/modules.md`: the Repositories, each with its chain, style, writers, servers and range of type identifiers; the modules, each with its responsibility, its Repository, its namespaces, what it knows, its sub-range and its use cases. |
| **Gate** | The user validates `Design/modules.md`, with the report of gbo-architecture-reviewer, before any model interface is written. |
| **In parallel** | After the gate, one gbo-data-modeler per module, [in the order of their dependencies](#the-order-of-the-models), each in its own namespaces and its own sub-range of type identifiers. |
| **Independence** | Its reviewer reads `Design/modules.md` and [the checklist](checklist.md), never the reasoning that produced them. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Modules and Repositories

A **module** is a responsibility: the use cases that share their rules and their concepts. A **Repository** is where its model lives. The work of this role is to give each module its Repository:

| | A concern module | A system module |
|---|---|---|
| **Is** | A module that **shares a model** with others: its namespace (`Library.Model.Loans`) in the project of that model, one Repository for all of them | A module **alone in its Repository**: its project, its Store, its Log and its chain |
| **Holds** | Its own anchors, their parts, and the components, edges and systems it attaches to the anchors of the modules it knows | A whole model |
| **Links to the other modules** | Links of one model: a reference to their anchors, a component on them, an edge between them; their anchors know nothing of its components | **No link and no identity in an entity**: identities in Records and Mutations, `XRef.Of` for a projection in its chain, a business key (a value object) for what another system masters, or a subscription to a Log |
| **Transactions** | One with the other modules of its model | Its own; one with others only inside a chain |
| **Choose it when** | The default: one model keeps every link and every rule exact at each instruction | Another lifetime of the data, other writers, other servers, another style, another persistence, or a model that must be replaced or rebuilt alone |

A rule that needs links between two modules puts them in one model. A system module pays its independence with identities instead of links: choose it for a criterion of the table, and write the criterion. A Read model is a Repository of its own, chained after its Write model, which serves the modules of that model ([logs-chains.md](../gbo/knowledge/logs-chains.md#cqrs-the-read-model)).

**Who declares whom is who depends on whom** ([the knowledge rule](../gbo/knowledge/modeling/README.md#the-knowledge-rule-who-declares-whom-is-who-depends-on-whom)), between modules too: a module whose types reference, extend or compose the anchors of another depends on it; so does a module whose Mutations submit a sub-Mutation of another.

**A module may hold several namespaces of concern** (its validations, its generations): they are decided by gbo-data-modeler inside the module and are not listed in the map. A namespace that has use cases of its own is a module.

## Choose your branch: the first yes decides

| # | Where you are | Signals | Branch |
|---|---|---|---|
| M1 | The modules come from another system | An adoption: SQL schemas, services, packages, a previous division | [Inherited modules](#inherited-modules) |
| M2 | A map exists | `Design/modules.md` | [Change the map](#change-the-map) |
| M3 | A technical base, no map | `Design/technical-base.md`, no `Design/modules.md` | [First map](#first-map) |

## First map

1. **List the use cases** of the application, one line each, named as the action they perform, a noun: `QuoteCreation`, `LineAddition`, `OrderPlacement` ([the naming of Mutation Sourcing](../gbo/knowledge/mutation-sourcing.md#step-1-one-mutation-per-use-case), whatever the style). Note the concepts each one touches, at a coarse grain. A rule that the mission does not settle is asked of the user with a proposed default ([the deliverables](../gbo/SKILL.md#the-deliverables)).
2. **Group them by responsibility**: a module is the set of use cases that share their rules and their concepts. Say the responsibility of each in one sentence.
3. **Give each concept one home**: the module whose use cases create it and change it under their rules. Reference data only imported and read (taxes, payment methods, countries) have their home in the module whose use case imports or maintains them; when no use case does, add it (an import, an administration). The other modules read a concept, designate it or attach aspects to it; they never write its own values.
4. **Give each module its Repository** by [the table](#modules-and-repositories): a shared model unless a criterion of the system module holds.
5. **Draw the dependencies**: one way, no cycle. A cycle means that two modules share their rules: one module, or a concept moved to its true home.
6. **For each Repository**: its chain, instantiating the pattern of `Design/technical-base.md`; its style when it differs; its writers (one writer at a time per chain: a module that needs writers of its own needs a Repository and a chain of its own); its servers; its **range of type identifiers**, and a sub-range per module. Keep the ranges unique across the whole application, not only across a chain: an identity carries the type identifier of its entity ([schema.md](../gbo/knowledge/modeling/schema.md#type-identifiers-across-a-chain)). A Deduced Repository starts its `FirstTypeId` at its range; its `[WithIdentity]` types take their identifiers in the sub-range of their module.
7. **For each module**: its namespaces, for its model, its Read model and its Mutations (`Library.Model.Loans`, `Library.Read.Loans`, `Library.Mutations.Loans`). A namespace is never named like a generated type: a namespace `Pricing` beside a component `CPricing` (whose Body is `Pricing`) breaks the generated code; the plural avoids it.
8. **Write** `Design/modules.md` by [the template](#the-deliverable-designmodulesmd), pass [the checklist](checklist.md), submit it to the gate.

A vertical slice is one use case through its layers; it belongs to one module. In Mutation Sourcing, it is one Mutation, with its `Decide` and its `Apply`.

## Inherited modules

1. **Inventory** the existing division: its schemas, services or packages, the tables or types of each, the use cases they serve.
2. **Regroup by responsibility**, by steps 1 to 3 of [First map](#first-map): never one module per old schema. An old schema often mixes concerns; the tables of one concept are often spread over several.
3. **Order the move**: the modules move to GBO one by one, the least dependent first, by the transition rules of `Design/technical-base.md`; write the order in the map. A module that stays outside GBO for now is designated by its business keys.
4. **Then** steps 4 to 8 of First map. Each module moved goes to gbo-data-modeler, branch *port*.

## Change the map

| The change | Do |
|---|---|
| Add a module | Steps 1 to 7 of [First map](#first-map) for its use cases; its dependencies toward the existing modules only; its sub-range |
| Split a module that grew | Its use cases fall in groups that share few concepts; each group a module, in the same model unless a criterion of the system module holds |
| Merge two modules | They always change together, or their rules need links that identities make heavy |
| Move a concept | Its home becomes the module whose rules now decide it |

Each change of a validated map is an ADR in `Design/decisions/` ([the deliverables](../gbo/SKILL.md#the-deliverables)); the models of the modules touched go to gbo-data-modeler, branch *extend*.

## The order of the models

A module is modeled after the modules whose anchors it references, extends or composes: it needs their types. A module that knows another by identities only (another Repository) needs only the names of the identities it designates, written in the map: the two can be modeled in parallel. When the mission targets one module, model first, in the modules it depends on, the concepts it designates.

## Go up when

A decision of the base changes: a module needs another persistence, other servers, another durability. Go up to gbo-boilerplate, branch *complete*, then come back.

## Hand over

After the gate: gbo-data-modeler, *first model* (or *port*), module by module, in [the order of the models](#the-order-of-the-models).

## The deliverable: `Design/modules.md`

```markdown
# Modules
Validated by <who> on <yyyy-mm-dd>, review: Design/reviews/<the report>    (absent until the gate)

## Repositories
| Repository | Project | Chain | Style | Writers | Servers | Type ids |
|---|---|---|---|---|---|---|
| Library | Library.Domain | Write → Read → Mutations → Jobs | Mutation Sourcing | The pump | One | 1-999 |

## Modules
| Module | Responsibility | Repository | Namespaces | Knows | Type ids | Status |
|---|---|---|---|---|---|---|
| Catalog | The books, their copies and authors | Library | Library.Model.Catalog, .Read.Catalog, .Mutations.Catalog | | 1-99 | Proposed |
| Loans | Lend, return, fine | Library | Library.Model.Loans, ... | Catalog, Members | 200-299 | Proposed |

## Dependencies
Loans → Catalog (a loan references a copy), Loans → Members.

## Use cases
### Loans
- Loan: a member borrows a copy.
- ...

## Identities across Repositories
<the *Ref or business keys that a module designates in another Repository>

## Decisions
| Decision | Reason | Rejected alternatives | Status |
|---|---|---|---|

## Order of the move (adoption only)
```

## Before the gate

Pass [checklist.md](checklist.md).
