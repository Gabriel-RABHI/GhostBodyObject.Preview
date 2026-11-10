---
name: gbo-code-reviewer
description: Review GBO code as a whole, the big picture - the boundaries of the transactions, the strategy against conflicts, where the business rules live (partial classes, systems, Decide), the side effects (output Logs, Jobs), the Log and the Replay (everything decided in the Record, deterministic handlers and Apply), the justification of the indexes, the ADRs, and the strategy of the tests. Use it for a pull request or a codebase that uses GBO, or when a symptom (frequent conflicts, a Replay that differs, changes lost at a restart, slowness) points to a flaw of design rather than of a line. The line-by-line review is gbo-model-reviewer, branch code detail.
---

# Code reviewer: the big picture

**Read first:** [the entry point](../gbo/SKILL.md): the mental model, the rules, the deliverables.

| Contract | |
|---|---|
| **Inputs** | The diff or the codebase; the deliverables it touches (`Design/technical-base.md`, `Design/modules.md`, `classification.md`, the ADRs). Never the reasoning of their author. |
| **Output** | A review report by [the template](../gbo/SKILL.md#the-deliverables), in `Design/reviews/`. |
| **Gate** | None of its own: the report goes with the diff to its gate. |
| **In parallel** | With gbo-model-reviewer (the detail) and gbo-architecture-reviewer; a codebase may be split by module. |
| **Independence** | It did not write what it reviews; it starts from the code and the deliverables. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | What there is to review | Signals | Branch |
|---|---|---|---|
| R1 | A codebase | The request names an application or a module, no diff | [Codebase](#codebase) |
| R2 | A pull request, a diff | A branch, commits, a diff | [Pull request](#pull-request) |
| R3 | A symptom | Frequent conflicts, a Replay that differs, changes lost at a restart, slowness, side effects lost or doubled | [Symptom](#symptom) |

## Codebase

1. Read the style from the code ([the entry point](../gbo/SKILL.md#3-the-style-read-from-the-code-never-chosen-here)), and the deliverables when they exist.
2. Module by module, list the use cases (the Mutations, the commands, the methods that open the write transactions), and the places that write the models.
3. Pass **the big picture** of [the checklist of the code](../gbo-implementer/checklist.md#the-big-picture) on the codebase: each item is a question asked of every use case.
4. Look first for what breaks the whole: a model rebuilt from its Log and written without a Record; a second writer of a chain; a side effect inside a transaction; a decision taken again in `Apply` or in a handler.

## Pull request

1. Read the diff with the deliverables it touches; a change of the model in the diff goes to gbo-model-reviewer as well.
2. Pass **the big picture** of the checklist on the changed use cases.
3. A decision of the diff that no deliverable holds and no ADR records (an index, a concurrency tool, a policy, a rule set aside) is a finding.
4. A changed `Apply` or handler: the Log written before the change must replay to the same models, or the change of the rebuilt models is decided (an ADR); a test of it.

## Symptom

| Symptom | The flaw of design to look for |
|---|---|
| Frequent conflicts | Aggregates too large (an order as a part of its customer), a counter written by every transaction, commands spread over threads at random |
| A Replay that differs | A handler or `Apply` that reads the clock, a random value, a configuration, or decides again; something decided outside the Record |
| Changes lost at a restart | A model rebuilt from its Log written by a transaction without a Record; a C# field in a partial Body |
| Slowness | Searches over Bodies, queries in `Apply`, an index missing for a measured question, or one too many on a hot write |
| Side effects lost or doubled | A call inside a transaction (lost on a rollback, doubled on a retry); a service not idempotent |

Trace the symptom to its mechanism in the code, and report the mechanism, not the symptom.

## The report

By [the template and the severities](../gbo/SKILL.md#the-deliverables) of the entry point, in `Design/reviews/`.
