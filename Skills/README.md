# The GBO skills for AI agents

A knowledge base on GBO, written for coding agents. It follows the Agent Skills format: each `SKILL.md` starts with a `name` and a `description` that tell an agent when to load it; the other files are loaded on demand.

It has two layers:

- **The role skills** say what to do for a mission, step by step. An agent enters by the entry point, `gbo`, which finds the path of its mission in a decision tree (build an application, adopt GBO, add a module, add or change a use case, fix a symptom, review) and gives it a role and a branch. Each role chooses its branch by what it finds in the repository, states its **contract** (inputs, output, gate, what may run in parallel, the independence of its review), and says when to go up to another role.
- **The knowledge** says how GBO works. The roles link to its sections and never copy them.

## The hierarchy

```
Skills/
├── gbo/
│   ├── SKILL.md                     gbo: the entry point; the decision tree, the deliverables and the ADR rule,
│   │                                the mental model, the rules never broken, what is built
│   └── knowledge/
│       ├── modeling/
│       │   ├── README.md            separate the concerns; when to use an entity, a part, an Option, a component,
│       │   │                        a sub-component, a system, an edge; the procedure; the anti-patterns
│       │   ├── checklist.md         the classification of each concept, group of fields, relation and question;
│       │   │                        the smells; the classification table; the checks of the model as written
│       │   ├── example-quote.md     a worked audit: the quote model reclassified
│       │   ├── relationships.md     compositions, strong and weak references, shared objects, orphans, trees, reverse properties
│       │   ├── components-ecs.md    components, systems, roles, polymorphism by C# interfaces
│       │   ├── edges.md             edges, graphs, walks
│       │   ├── values.md            property types, Option<T>, texts, arrays, structs, converters: value objects
│       │   ├── identities.md        [WithIdentity], *Ref, projections
│       │   ├── concurrency.md       aggregates, [ConcurrencyPoint], [Mergeable], [Delta], Claim, leases, Exclusive
│       │   ├── schema.md            Fixed and Deduced schemas, evolution of a model
│       │   └── patterns.md          complete models of common domains
│       ├── transactions-queries.md  transactions, conflicts, Views and Bodies, operators, business code
│       ├── indexes.md               when (rarely) and how: text, value, hash, unique
│       ├── logs-chains.md           Records, Logs, subscriptions, chains, Event Sourcing, CQRS, Replay
│       ├── mutation-sourcing.md     a system in Mutation Sourcing
│       ├── infrastructure.md        projects, Stores, durability, distribution, tests
│       └── reference/
│           ├── attributes.md        every attribute, its arguments, its status
│           ├── generated-api.md     what the generator writes for each declaration
│           ├── diagnostics.md       GBO001 to GBO048, their causes and fixes
│           └── pitfalls.md          traps for agents, pitfalls of the API
├── gbo-boilerplate/                 the technical base: style, Stores, durability, servers, projects, host
│   ├── SKILL.md                     branches: adoption, complete, from scratch → Design/technical-base.md
│   └── checklist.md                 shared with gbo-architecture-reviewer
├── gbo-module-architect/            the module map: concern modules and system modules, chains, dependencies
│   ├── SKILL.md                     branches: inherited modules, change the map, first map → Design/modules.md
│   └── checklist.md                 shared with gbo-architecture-reviewer
├── gbo-data-modeler/SKILL.md        the model of a module; branches: port, extend, first model
│                                    → Design/<Module>/classification.md, then the interfaces
├── gbo-implementer/                 the use cases; branches: first use cases, add, change, symptom
│   ├── SKILL.md
│   └── checklist.md                 the big picture (gbo-code-reviewer) and the detail (gbo-model-reviewer)
├── gbo-architecture-reviewer/SKILL.md   branches: running, map, base
├── gbo-model-reviewer/SKILL.md      branches: code detail, audit, check, table
└── gbo-code-reviewer/SKILL.md       the big picture; branches: codebase, pull request, symptom
```

A creator and its reviewer share one checklist: the exit criteria of the one, the audit grid of the other. The model's checklist is `gbo/knowledge/modeling/checklist.md`.

## The deliverables

The roles write their decisions in the repository of the application, in `Design/`: `technical-base.md`, `modules.md`, `<Module>/classification.md`, the ADRs in `decisions/`, and the reports of the reviewers in `reviews/`. They are the hand-over between the roles, so that an agent that starts cold needs nothing else; the state of the repository decides the branch of each role. The rules are in [gbo/SKILL.md](gbo/SKILL.md#the-deliverables).

## Install

- **Claude Code, in a project that uses GBO**: copy (or link) every folder of `Skills/` to `.claude/skills/` of the project, or to `~/.claude/skills/` for every project, side by side: the roles link to `../gbo/` by relative paths. Each folder is a skill discovered by its description: `gbo`, `gbo-boilerplate`, `gbo-module-architect`, `gbo-data-modeler`, `gbo-implementer`, `gbo-architecture-reviewer`, `gbo-model-reviewer`, `gbo-code-reviewer`.
- **Other agents**: point the agent instructions (`AGENTS.md`, a system prompt) to `Skills/gbo/SKILL.md`.

## Sources and maintenance

Written from the tutorials (`Documentation/Tutorials`), the specifications (`Documentation/Specifications`), the reference of the quote port (`GhostBodyObject.Quote.Domain/Documentation/Design/GBO-Reference.md`), and checked against the code: the attributes (`GhostBodyObject.Runtime/Logical/Model/ModelAttributes.cs`), the diagnostics (`GhostBodyObject.Generator/Diagnostics.cs`), the generator and the tutorial test projects. The knowledge describes the state of 2026-10-28; the roles were added on 2026-10-29.

When GBO changes, update the file of the knowledge and, for an attribute, a diagnostic or a call point, the list of what is built in `gbo/SKILL.md` and `knowledge/reference/attributes.md`; a role changes only when a step, a branch or a deliverable changes. A new layer (the components of a user interface, an application state) comes as new roles and new knowledge files, without rewriting the others. The previous single-file skill, `Documentation/Agentic/01-SKILL-DataSchema.md`, describes an earlier prototype API and is superseded by this folder.
