# The checklist of the code

Shared by [gbo-implementer](SKILL.md), which passes it before its gate, and by the reviewers of the code: [gbo-code-reviewer](../gbo-code-reviewer/SKILL.md) audits by **the big picture**, [gbo-model-reviewer](../gbo-model-reviewer/SKILL.md) by **the detail**. Each item holds, or its exception is an ADR.

## The big picture

**Transactions**
- [ ] One transaction per unit of work, opened by the code that decides to commit; the methods it calls use the ambient transaction; no Repository nor transaction passed around or injected.
- [ ] Conflicts: the whole unit of work runs again in a new transaction (`TryCommit`); commands in parallel are routed by aggregate to fixed threads, or go through the single-writer pump.
- [ ] Mutation Sourcing: the application never opens a write transaction; only Mutations write the models, the tests included.

**Rules**
- [ ] A rule of one entity is in the partial class of its Body; a rule of several aspects in the partial struct of a system; in Mutation Sourcing, the decisions in `Decide`; never in the application layer.
- [ ] Rules check, then write; nothing that may refuse in `On...Changed`.
- [ ] A business refusal is a result (CRUD, Event Sourcing) or an exception of the domain thrown in `Decide`; every other exception is a bug.

**Side effects**
- [ ] No mail, file, call or payment inside a transaction, a call point, a handler, `Decide` or `Apply`: a Record of an output Log or a Job, consumed after the commit, idempotent by an identity it carries.

**The Log**
- [ ] Everything decided is in the Record or the Decision: identities, prices, numbers, dates.
- [ ] Handlers and `Apply` are deterministic: no clock, random value, configuration, `XRef.New()`; `Apply` queries no collection nor index.
- [ ] The Replay commits through the last transaction and returns `Cancel` from `OnReplayEnd`; a Replay test compares the rebuilt models with the live ones.
- [ ] Every Log is Fixed; a changed meaning is a new Record or Mutation type.

**Indexes and decisions**
- [ ] Every index answers a question measured too slow without it, recorded in an ADR.
- [ ] Every decision that no deliverable holds has its ADR; every limit of GBO met is reported.

**Tests**
- [ ] In-memory Stores with unique names; the tests that open Stores not parallelized; concurrency ordered by signals, never by delays.
- [ ] Each use case has its test, its refusals included; Mutation Sourcing tests post Mutations and wait.

## The detail

**Transactions**
- [ ] Every read and write inside a transaction; `using` on every transaction; one transaction per Repository per execution context; none shared by threads.
- [ ] A Body is never used out of its transaction to resolve links; a removed Body is not used.

**Queries**
- [ ] Searches on the Views; `ToBody()` on the few to change; no `foreach` over a Body collection to search.
- [ ] Lambdas `static`, their values passed as states before them (GBO013); no `System.Linq` on a GBO sequence (GBO014); `AsEnumerable()` only to leave GBO on purpose.
- [ ] No order assumed from a collection: a property and `OrderBy`; no collection changed while it is enumerated.

**Absence and errors**
- [ ] No null: `Option<T>`, `FirstOrNone`, `TryGet`, `GetOr`; `Get()` only where the absence is a bug.
- [ ] No exception as control flow: `TryCommit`, `ResolveOrNone`, `index.Is(key).Any()`, `TryGetOrNew`; the exceptions of GBO never caught to choose a path.

**Values and Bodies**
- [ ] No C# field or auto-property in a partial Body (GBO047).
- [ ] `ref struct` values (`GhostString`, `GhostSpan<T>`) never in a tuple assignment, a capture, an `Option`, a field, across an `await`.
- [ ] A required link and a unique value are given at the birth.
- [ ] The order of instructions: a part removed before its Owner when it must be; the new strong reference taken before the old one is released.

**Namespaces**
- [ ] `using static XRepositoryCollections;` where two Repositories share a type name or in another project; `using ModelNamespace;` where parts are born in another namespace.

**The pitfalls**
- [ ] None of the traps and pitfalls of [pitfalls.md](../gbo/knowledge/reference/pitfalls.md).
