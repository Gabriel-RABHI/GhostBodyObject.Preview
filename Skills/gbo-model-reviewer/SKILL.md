---
name: gbo-model-reviewer
description: Review a GBO model, and the detail of the code that uses it - review Design/<Module>/classification.md at its gate, before the interfaces exist; check the table against the model interfaces once written; audit a model that has no classification table (a ported or grown model, rebuilt as a table from the code, with the smells of the modeling checklist); and review code line by line against the rules of GBO (transactions, Views then Bodies, static lambdas and states, no System.Linq, Option, check then write, no exception as control flow, no field in a partial Body, the pitfalls). Use it at the gate of a model, to audit an existing or ported model, or for the detailed review of a diff that uses GBO.
---

# Model reviewer: the model, and the code in detail

**Read first:** [the entry point](../gbo/SKILL.md), then [the modeling rules](../gbo/knowledge/modeling/README.md).

| Contract | |
|---|---|
| **Inputs** | `Design/<Module>/classification.md` when it exists; the model interfaces; the analysis of the domain (its requirements, the documents of a ported source); for the code detail, the diff or the files. Never the reasoning of their author. |
| **Output** | A review report by [the template](../gbo/SKILL.md#the-deliverables), in `Design/reviews/`; for an audit, the classification table rebuilt, with the rows to change. |
| **Gate** | None of its own: the report goes with the deliverable or the diff to its gate. |
| **In parallel** | With the other reviewers; an audit may be split by module. |
| **Independence** | It did not write what it reviews; it starts from the deliverables, the analysis of the domain and the code. Without a second agent, [a separate pass](../gbo/SKILL.md#6-one-agent-or-several). |

## Choose your branch: the first yes decides

| # | What there is to review | Signals | Branch |
|---|---|---|---|
| R1 | Code, line by line | A diff or files that use a generated Repository, Log or Mutation | [Code detail](#code-detail) |
| R2 | A model without its classification table | Interfaces; no `classification.md`; a port, a model grown over time | [Audit](#audit) |
| R3 | A classification table and its interfaces | `classification.md` and the interfaces of the module | [Check](#check) |
| R4 | A classification table, no interface yet | The gate of gbo-data-modeler | [Table](#table) |

## Table

The review at the gate of the model, before any interface.

1. **Each concept of the use cases of the module has its row**, and each row its unit, its declaration, the questions that decided and the alternatives rejected.
2. **Re-ask the questions that decided** each row: a row whose answers do not hold is a finding.
3. **Pass sections B to F** of [the checklist](../gbo/knowledge/modeling/checklist.md) on the rows: the groups of fields, the extracted groups, the components, the relations, the questions; then **G**, the smells, on the entities as the table draws them.
4. **The Mutations and Records** of a module in Mutation or Event Sourcing: one per use case, named as its action; the Intention minimal, the identities of what it creates drawn by the application; everything decided in the `[Private]` Decision ([mutation-sourcing.md](../gbo/knowledge/mutation-sourcing.md), steps 1 to 3).

## Check

1. [Table](#table) first.
2. **Each row of the table is in the interfaces**, and each interface has its row: a difference is a finding.
3. **Pass section J**, the model as written.

## Audit

1. **The module**: without `Design/modules.md`, each Repository is a module, named by it.
2. **Rebuild the table** from the interfaces, as it is: one row per type, per group of fields of each entity, per relation, per Record and Mutation, with the column *Today* (section H of [the checklist](../gbo/knowledge/modeling/checklist.md)).
3. **Pass the smells** of section G on each entity; each smell found is a row to redo.
4. **Reclassify** each row to redo by sections A to F, and write the proposed row beside the current one, with the questions that decided and why the current form is rejected.
5. **Pass section J** on the interfaces as they are.
6. **The cost of each change**: a model rebuilt from its Log changes freely (the Replay migrates it); a persistent Fixed model needs a migration of its data ([schema.md](../gbo/knowledge/modeling/schema.md#evolving-a-fixed-model)).
7. Only then, compare with the worked example of an audit when it covers this domain ([the quote model](../gbo/knowledge/modeling/example-quote.md)): it shows the method; its rows are not findings.

The report holds the findings, then the rebuilt table with its rows to change; the findings point to the rows. Once validated, the table becomes `Design/<Module>/classification.md`.

## Code detail

1. Pass **the detail** of [the checklist of the code](../gbo-implementer/checklist.md#the-detail) on each line of the diff.
2. Pass the traps of [pitfalls.md](../gbo/knowledge/reference/pitfalls.md): each one is a pattern to look for.
3. A line that changes the model (an interface, an attribute) is checked by [Check](#check) as well.
4. What the detail cannot judge (the boundaries of the transactions, where the rules live, the Log) goes to gbo-code-reviewer.

## The report

By [the template and the severities](../gbo/SKILL.md#the-deliverables) of the entry point.
