---
name: gbo-implementer
description: Write the operations of a GBO application on its model - its use cases with their tests (CRUD transactions, queries and rules in partial classes; Event Sourcing commands, Records and handlers; Mutation Sourcing Decide, Apply, sub-Mutations, Jobs and the pump), the wiring of the Repositories, Logs, Replay and pump in the host, and the fix of a symptom (a GBO diagnostic, an exception, a slow query, conflicts, a Replay that differs, lost changes). Use it when adding or changing a feature, a command, a Mutation, a query, an index or a subscription in code that uses a generated GBO Repository or Log.
---

# Implementer: the use cases

**Read first:** [the entry point](../gbo/SKILL.md): the mental model, the rules, the paths, the deliverables.

| Contract | |
|---|---|
| **Inputs** | The model of the module, built, and its `Design/<Module>/classification.md`; the use case, from the request or from `Design/modules.md`; the style, from `Design/technical-base.md` or read from the code. |
| **Output** | The use case written with its tests, which pass; no GBO diagnostic; an ADR for each decision that no deliverable holds (an index, a concurrency tool, a policy, a rule set aside, a limit of GBO met). |
| **Gate** | The tests pass, and [the checklist](checklist.md) holds, both sections, with the reports of gbo-code-reviewer (the big picture) and gbo-model-reviewer (the detail). |
| **In parallel** | Use cases of different modules. In one module, use cases that change no model interface; a change of the model goes through gbo-data-modeler, one agent at a time. New Mutations and Records take their type identifiers from the range of the module, reserved before. |
| **Independence** | Its reviewers read the diff, the deliverables and [the checklist](checklist.md), never the reasoning that produced them. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | Where you are | Signals | Branch |
|---|---|---|---|
| I1 | A model was just built; no use case is written on it | Interfaces, no Mutation handled, no method opens its transactions | [First use cases](#first-use-cases) |
| I2 | A use case to add | No Mutation, command or method does it yet | [Add a use case](#add-a-use-case) |
| I3 | A use case to change | It exists | [Change a use case](#change-a-use-case) |
| I4 | A defect or a symptom | An exception, a diagnostic, slowness, conflicts, a Replay that differs | [Symptom](#symptom) |

## The style decides the form of a use case

| Style | A use case is | Its rules live in | Knowledge |
|---|---|---|---|
| CRUD | A method that opens a ReadWrite transaction, checks then writes, and commits with `TryCommit` in a retry of the whole unit | The partial class of the Body for the rules of one entity; the partial struct of a system for the rules of several aspects | [transactions-queries.md](../gbo/knowledge/transactions-queries.md) |
| Event Sourcing | A command: it checks, draws the identities, creates each Record complete, adds it, and applies its View by the handler that the Replay uses | The command checks; the handlers apply, deterministic | [logs-chains.md](../gbo/knowledge/logs-chains.md#event-sourcing-on-gbo) |
| Mutation Sourcing | A Mutation posted to the pump: `Decide` in the partial class of its Body, `Apply` in the partial struct of its View | `Decide` decides and refuses; `Apply` writes the Write and Read models, deterministic | [mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md), steps 4 to 11 |

In a module in transition between two styles, a use case changed keeps its own form, and a new one takes the style of `Design/technical-base.md`.

## First use cases

1. **Wire the host** (left ready by gbo-boilerplate): the Repositories and Logs, `Chain` in the order of the Stores, every policy still set before the first Repository; the Replay when the chain is behind its Log ([logs-chains.md](../gbo/knowledge/logs-chains.md#replay)); the pump in Mutation Sourcing ([mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md#the-architecture)).
2. **Then each use case** by [Add a use case](#add-a-use-case), the ones the others need first (the creations before the changes).
3. **A Replay test** in Mutation or Event Sourcing: run a scenario, snapshot the models, close, reopen, replay, compare ([mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md#step-11-testing)).

## Add a use case

1. **Read** the classification table of the module and the use case. When the use case needs a concept, a group of fields, a relation or a question that the model lacks: [go up](#go-up-when).
2. **Write the test first**, by the style: CRUD, call the method, then read in a new transaction; Event Sourcing, run the command, then read; Mutation Sourcing, post the Mutation, wait, read in a ReadOnly transaction, never write the models in a test. A refusal is tested too: the models unchanged.
3. **Write the use case** in its form ([the table above](#the-style-decides-the-form-of-a-use-case)):
   - search with the Views, change with the Bodies of the few to change ([transactions-queries.md](../gbo/knowledge/transactions-queries.md#views-and-bodies));
   - lambdas `static`, their values passed as states; the GBO operators, never `System.Linq` ([transactions-queries.md](../gbo/knowledge/transactions-queries.md#the-operators));
   - absence by `Option<T>`; a business refusal is a result (CRUD, Event Sourcing) or an exception of the domain (in `Decide` only);
   - check, then write; nothing that may refuse in `On...Changed` ([transactions-queries.md](../gbo/knowledge/transactions-queries.md#business-code-partial-classes-and-call-points));
   - in Mutation Sourcing, everything decided goes in the Decision; `Apply` navigates by identities and links, queries nothing ([mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md#step-5-apply-in-the-partial-struct-of-the-view)).
4. **Side effects**: a Record of the output Log (a Job in Mutation Sourcing), never a call inside the transaction ([logs-chains.md](../gbo/knowledge/logs-chains.md#outbox-and-side-effects)).
5. **Concurrency**: CRUD and Event Sourcing retry the whole unit in a new transaction on a conflict; when conflicts are frequent, the cure is in the model (aggregates, components, `[Mergeable]`: [go up](#go-up-when)) or in the routing of the commands by aggregate to fixed threads; Mutation Sourcing has none with an exclusive pump.
6. **An index** only for a question measured too slow once the paths without index are tried ([indexes.md](../gbo/knowledge/indexes.md#decide-first-no-index)); declared beside the model; its measure in an ADR.
7. **Build, run the tests**, pass [the checklist](checklist.md).

## Change a use case

1. **Find it**: the Mutation type, the command and its handlers, or the method.
2. **What the change touches**:

| Style | Changing | Means |
|---|---|---|
| Mutation Sourcing | `Decide` | Only the future decisions change |
| Mutation Sourcing | `Apply` | The rebuilt models change: `Apply` must stay faithful to the decisions recorded in the Log |
| Mutation Sourcing | The meaning of the Intention or the Decision | A new Mutation type; the old one stays, applied forever ([step 12](../gbo/knowledge/mutation-sourcing.md#step-12-evolution)) |
| Event Sourcing | A handler | The rebuilt models change: it stays faithful to the Records of the Log |
| Event Sourcing | The meaning of a Record | A new Record type; the old handlers stay |
| CRUD | A rule on stored values | The data written by the old rule stay: a migration when their meaning changes, in an Exclusive transaction |

3. **The tests**: the existing ones show what the change breaks; add the new behavior; in Mutation and Event Sourcing, a Replay of a Log written before the change.
4. **The model changes**: [go up](#go-up-when). A change that reopens a validated decision is an ADR.

## Symptom

| Symptom | Look first | Knowledge |
|---|---|---|
| A GBO diagnostic | Its cause and its fix; the errors after the first GBO error are its consequences | [diagnostics.md](../gbo/knowledge/reference/diagnostics.md) |
| A compile error in generated code: CS0118, CS1061 on `New`, CS9244 | The pitfalls 25, 26, 29 | [pitfalls.md](../gbo/knowledge/reference/pitfalls.md) |
| `InvalidOperationException`: no transaction, nested, ReadOnly, condemned, behind its Log | The rules of a transaction; a condemned transaction comes from an exception after a write | [transactions-queries.md](../gbo/knowledge/transactions-queries.md#the-exceptions-of-gbo) |
| A slow query | Bodies where Views would do; then navigation, a reverse property, a system; then an index, measured | [indexes.md](../gbo/knowledge/indexes.md#decide-first-no-index) |
| Frequent conflicts | The aggregates of the model, the routing by aggregate, the pump; `Kind` says what conflicted | [concurrency.md](../gbo/knowledge/modeling/concurrency.md) |
| A Replay that differs from the live models, or changes lost at a restart | A handler or `Apply` that reads the clock, a random value or a configuration; something decided outside the Record; a change written without a Record | [logs-chains.md](../gbo/knowledge/logs-chains.md#replay) |
| A subscription that stopped | A handler threw; `subscription.Completion` | [logs-chains.md](../gbo/knowledge/logs-chains.md#read-subscriptions) |
| Writes stopped on a cluster: `WriteTokenUnavailable` | The owner of the write token is lost: no automatic fail-over | [infrastructure.md](../gbo/knowledge/infrastructure.md#distribution) |

Fix the cause, add the test that shows it, pass the checklist. **A behavior of GBO that contradicts its documentation is reported with a reproduction, never worked around in the application code.**

## Go up when

| You find | Go up to |
|---|---|
| The use case needs a concept, a field, a relation or a question the model lacks; a concurrency tool of the model (`[Mergeable]`, `[ConcurrencyPoint]`) | gbo-data-modeler, *extend* |
| The use case belongs to no module, or to another one | gbo-module-architect, *change the map* |
| A need of durability, writers, servers | gbo-boilerplate, *complete* |

## Before the gate

Pass [checklist.md](checklist.md): the big picture and the detail.
