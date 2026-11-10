---
name: gbo-boilerplate
description: Set up the technical base of an application that keeps its data with GBO - choose the architecture style (CRUD, Event Sourcing, Mutation Sourcing), where the models live between two runs, the durability, the writers, one server or several, the side effects, then write the solution, the host that opens and closes the Stores, the test project, and Design/technical-base.md. Use it when an application or its data side starts from nothing, when GBO comes into an application that keeps its data elsewhere (EF Core, SQL, another ORM), or when a GBO project lacks its tests, its durability or its servers. Not for dividing the data into modules (gbo-module-architect) nor for writing models (gbo-data-modeler).
---

# Boilerplate: the technical base

**Read first:** [the entry point](../gbo/SKILL.md): the mental model, the rules, the paths, the deliverables.

| Contract | |
|---|---|
| **Inputs** | The mission. For an adoption: the code of the application and its persistence. |
| **Output** | `Design/technical-base.md`; the solution, its host project and its test project, which build; the code that opens and closes the Stores. No model interface. |
| **Gate** | The user validates `Design/technical-base.md`, with the report of gbo-architecture-reviewer, before gbo-module-architect starts. |
| **In parallel** | Nothing: every other role depends on it. |
| **Independence** | Its reviewer reads `Design/technical-base.md`, the projects and [the checklist](checklist.md), never the reasoning that produced them. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | Where you are | Signals | Branch |
|---|---|---|---|
| B1 | An application keeps its data elsewhere | A `DbContext`, EF Core packages, SQL scripts or migrations, another ORM; no GBO | [Adoption](#adoption) |
| B2 | GBO is set up, and a piece is missing | The Generator referenced as an analyzer; no test project, no durability decided, a second server or process wanted | [Complete](#complete) |
| B3 | Nothing yet | No project, or none that keeps data | [From scratch](#from-scratch) |

## From scratch

### 1. The questions

Each answer is a decision of the business, written in `Design/technical-base.md` with its reason and its rejected alternatives. Take the answers from the request; ask the user only those it does not give, and propose for each the default below, marked *proposed* until validated.

| Question | Decides | Default | Knowledge |
|---|---|---|---|
| Must the business keep what happened: a history, an audit, a past state explained, models rebuilt or migrated from their facts? | Yes: **Mutation Sourcing**, also for a system that is event-sourced today; **Event Sourcing** only when the user asks for it. No: **CRUD** | Mutation Sourcing for a business domain | [mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md), [logs-chains.md](../gbo/knowledge/logs-chains.md#event-sourcing-on-gbo) |
| Where do the models live between two runs? | Mutation or Event Sourcing: in memory, rebuilt from a persistent Log at each start (Deduced schema), or on disk, ready at the opening. CRUD: a file Repository Store (Fixed schema). In Mutation Sourcing the application reads the Read model, the domain writes both | In memory behind a file Log | [logs-chains.md](../gbo/knowledge/logs-chains.md#replay), [schema.md](../gbo/knowledge/modeling/schema.md#which-schema) |
| How long may a start take? | A model in memory replays its whole Log at each start, a time that grows with the Log; a model on disk opens ready and is rebuilt on request (`RebuildFromLog()`). Measure it: a Replay test on a Log of the expected volume, never an estimate | In memory, until the measured Replay exceeds what the user accepts | [logs-chains.md](../gbo/knowledge/logs-chains.md#replay) |
| What may a crash lose? | `FlushPolicy`: `Durable` (each commit survives a crash of the process), `DurableOnDisk` (a power loss), `Fast`, `FastOnDisk` (a window of about a millisecond, for throughput). The examples of the knowledge use `FastOnDisk`: it is their choice, not a default | `Durable` | [infrastructure.md](../gbo/knowledge/infrastructure.md#durability) |
| Must the data survive the loss of the machine? | A replica on another server holds a whole copy (a `ReadOnly` replica is enough). These skills document no other backup: decide it with the user | A replica when the business cannot lose its history | [infrastructure.md](../gbo/knowledge/infrastructure.md#distribution) |
| How many processes write? | One: `StoreLockPolicy.Retain`, and in Mutation Sourcing the pump `Exclusive = true`. Several on one machine: the default lock policy | One | [infrastructure.md](../gbo/knowledge/infrastructure.md#durability) |
| One server or several? | Several: a `StoreDistributionEndPoint`; `ReadOnly` replicas for readers, `ReadWrite` replicas for writers, which share one write token per chain; no automatic fail-over yet | One | [infrastructure.md](../gbo/knowledge/infrastructure.md#distribution) |
| What acts outside the system: mails, documents, payments, calls? | An output Log (an outbox; Jobs in Mutation Sourcing) chained after the Log, consumed after the commit by a service that keeps its position ([subscriptions](../gbo/knowledge/logs-chains.md#read-subscriptions)), is idempotent by the identity of the Job, and returns its result as a new Mutation or command | An output Log as soon as there is one | [logs-chains.md](../gbo/knowledge/logs-chains.md#outbox-and-side-effects), [mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md#step-7-jobs-the-side-effects) |
| What happens by time: a due date, a reminder, an expiry? | A scheduler of the host posts the Mutations (or runs the commands) at their time, the time in their Intention (`At`); `Decide` takes the time from it; `Apply` and the handlers never read the clock | A scheduler in the host | [mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md#step-2-the-intention-public) |

### 2. The chain pattern

From the answers, write the chain that each module of the application will instantiate: its Stores in order, the kind and persistence of each, its schema. Mutation Sourcing: Write model (memory, Deduced) → Read model (memory, Deduced) → Mutation Log (file, Fixed) → Jobs Log (file, Fixed), as in [the architecture of Mutation Sourcing](../gbo/knowledge/mutation-sourcing.md#the-architecture). CRUD: a file Repository Store (Fixed), and an output Log when there are side effects. The modules, their number and their chains are decided by gbo-module-architect.

### 3. The projects

- The solution, a host project (the application, the service), and a test project ([infrastructure.md](../gbo/knowledge/infrastructure.md#tests)): tests that open Stores are not parallelized, each Store has a unique name, the Generator is referenced as an analyzer so that GBO013 and GBO014 apply to the queries of the tests too.
- The shape of a model project, written in `Design/technical-base.md`: `AllowUnsafeBlocks`, the Runtime, the Generator as an analyzer ([infrastructure.md](../gbo/knowledge/infrastructure.md#the-project)). The model projects themselves come with the modules: a Repository includes the model interfaces of its own project only.
- GBO is not published as a package: the solution references the projects of the GBO repository (`GhostBodyObject.Runtime`, and `GhostBodyObject.Generator` as an analyzer), from a git submodule or a checkout beside it; write which in the deliverable.

### 4. The host

The code that opens and closes the Stores, which needs no model: the factories, `AddChildStore` in the order of the chain, every policy on the master Store, the distribution endpoint (`endPoint.Add(store)` before the first transaction), and the disposal in the reverse order ([infrastructure.md](../gbo/knowledge/infrastructure.md#stores)). It leaves a place for what comes with the models, written by gbo-implementer: the Repositories and Logs, `Chain`, the Replay when the chain is behind its Log, the pump; every policy stays set before the first Repository.

In a service host (ASP.NET Core, a worker), the opening, the Replay and the pump belong to the start of the host, and the host serves no request before the Replay ends (a write is refused while the chain is behind its Log, and a read would see a model half rebuilt); the disposal belongs to its stop. The pump and the Repositories are single instances of the process.

### 5. The deliverable

Write `Design/technical-base.md` by [the template](#the-deliverable-designtechnical-basemd), pass [the checklist](checklist.md), build, and submit it to the gate.

## Adoption

1. **Inventory the existing persistence**, in a section *Existing system* of the deliverable: its databases and schemas, or its event log; who writes them (applications, jobs, imports); its transactions and their boundaries; the side effects inside them; the queries and reports that matter; the volumes, or *unknown, to measure* when no source gives them. The request and the sources may disagree on what the persistence is: the code and its analysis win, and the difference is reported.
2. **Answer the questions** of [From scratch](#1-the-questions) for the GBO side.
3. **Decide the transition**: which areas move, and in which order, is decided module by module by gbo-module-architect; write here the rules of the transition: a use case writes one side only; what crosses between the two sides goes by an output Log consumed after the commit; the existing data enters GBO by a bulk migration in an Exclusive transaction in CRUD, or by Mutations (Records in Event Sourcing) of an import use case, since only they write the models.
4. **Then** steps 2 to 5 of From scratch. The schemas to port go to gbo-data-modeler, branch *port*, once the module map exists.

## Complete

1. Read `Design/technical-base.md`; when it is missing, rebuild it from the code first (the projects, the host, the policies set), as the facts.
2. Add the missing piece by its section of the knowledge: the tests ([infrastructure.md](../gbo/knowledge/infrastructure.md#tests)), the durability ([infrastructure.md](../gbo/knowledge/infrastructure.md#durability)), the servers ([infrastructure.md](../gbo/knowledge/infrastructure.md#distribution)), the side effects ([logs-chains.md](../gbo/knowledge/logs-chains.md#outbox-and-side-effects)).
3. Update the deliverable; when the change reopens a validated decision (a durability, a writer, a server), write the ADR ([the deliverables](../gbo/SKILL.md#the-deliverables)).

## Hand over

After the gate: gbo-module-architect, *first map* (or *inherited modules* for an adoption, *change the map* when the change of the base touches the modules).

## The deliverable: `Design/technical-base.md`

```markdown
# Technical base
Validated by <who> on <yyyy-mm-dd>, review: Design/reviews/<the report>    (absent until the gate)

## Decisions
| Decision | Choice | Reason | Rejected alternatives | Status |
|---|---|---|---|---|
| Style | Mutation Sourcing | ... | CRUD: ...; Event Sourcing: ... | Validated |
| Models between runs | In memory, rebuilt from the Mutation Log | ... | On disk: ... | Validated |
| Durability | `FlushPolicy.Durable` | ... | ... | Proposed |
| Copy elsewhere | ... | ... | ... | Proposed |
| Writers | One process; `StoreLockPolicy.Retain`; pump `Exclusive` | ... | ... | Proposed |
| Servers | One | ... | ... | Proposed |
| Side effects | A Jobs Log after the Mutation Log | ... | ... | Proposed |
| Time | A scheduler in the host posts the Mutations due | ... | ... | Proposed |
| GBO reference | A git submodule of the GBO repository | ... | ... | Proposed |

## The chain pattern
| Position | Store | Kind | Persistence | Schema |
|---|---|---|---|---|
| 1 (master) | Write model | Repository | Memory | Deduced |
| ... | | | | |

## Projects
| Project | Role | References |
|---|---|---|

## Opening and closing
The order of the host: the Stores, the chain, the policies, the endpoint; then the Repositories and Logs, the Replay, the pump; disposal in reverse.

## Existing system (adoption only)
```

## Before the gate

Pass [checklist.md](checklist.md); every item holds, or its exception is a decision of the deliverable.
