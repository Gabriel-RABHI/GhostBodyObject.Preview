---
name: gbo-architecture-reviewer
description: Review the architecture of an application that keeps its data with GBO - its architecture style, its Stores, chains, durability, writers and servers, its projects and test setup, and its division into modules (concern modules, system modules, dependencies, chains, type identifier ranges). Use it to review Design/technical-base.md or Design/modules.md before their gate, to audit an existing GBO application as a whole (rebuilding its technical base and module map from the code when they are missing), or when the behavior of a running application (writers, conflicts, start time, memory) departs from its design.
---

# Architecture reviewer

**Read first:** [the entry point](../gbo/SKILL.md): the mental model, the rules, the deliverables.

| Contract | |
|---|---|
| **Inputs** | `Design/technical-base.md`, `Design/modules.md`, the ADRs of `Design/decisions/`; the projects and the host. Never the reasoning of their author. |
| **Output** | A review report by [the template](#the-report), in `Design/reviews/`; for an audit, the technical base and the module map rebuilt from the code. |
| **Gate** | None of its own: the report goes with the deliverable to its gate. |
| **In parallel** | With the other reviewers; an audit may be split by module. |
| **Independence** | It did not write what it reviews; it starts from the deliverables and the code. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | What there is to review | Signals | Branch |
|---|---|---|---|
| R1 | A running application, and what it does | Measures, logs, incidents: conflicts, start time, memory, writes stopped | [Running](#running) |
| R2 | A module map | `Design/modules.md`, or modules to review in the code | [Map](#map) |
| R3 | A technical base | `Design/technical-base.md`, or the projects and the host | [Base](#base) |

Each branch includes the ones below it: a review of the map reviews the base first.

## Base

1. When `Design/technical-base.md` is missing, rebuild it from the code: the projects and their references, the factories of the Stores, the chain, the policies set, the endpoint, the disposal. It is part of the report.
2. Pass [the checklist of the technical base](../gbo-boilerplate/checklist.md) on the deliverable and on the code; each item that fails is a finding.
3. Check that the code does what the deliverable says: the policies, the order of the chain, the schemas.

## Map

1. [Base](#base) first.
2. When `Design/modules.md` is missing, rebuild it from the code: the Repositories, and the modules of each by their namespaces, their dependencies read from the components, edges, systems and identities; the use cases from the Mutations, the commands or the methods.
3. Pass [the checklist of the module map](../gbo-module-architect/checklist.md); each item that fails is a finding.
4. Look for the two costly mistakes: a module alone in its Repository while its rules need links to another (identities everywhere, rules spread over two transactions), and a module in a shared model while it needs another writer, lifetime or server (one chain written by two writers in turn).

## Running

1. [Map](#map) first.
2. Compare what the application does with what its deliverables decided:

| Observed | Compare with |
|---|---|
| Conflicts by `TransactionConflictKind`, and where | The aggregates, the writers of each chain, the routing of the commands |
| The start time | The place of the models: a Replay grows with its Log |
| The memory | The Stores and their volumes |
| Writes stopped, nodes frozen | The servers, the owner of the write token, the accepted limits |
| Side effects lost or doubled | The output Logs, the idempotence of their services |

3. Each gap is a finding: either the code departs from the decision, or the decision no longer fits and must be reopened (an ADR).

## The report

By [the template and the severities](../gbo/SKILL.md#the-deliverables) of the entry point, in `Design/reviews/`, followed by the decisions missing from the deliverables and, for an audit, the deliverables rebuilt from the code. A typical finding: *Blocking, `Host.cs:42`, every policy before the first Repository: `FlushPolicy` set after it throws at the start; move it before `new WriteModelRepository(store)`.*
