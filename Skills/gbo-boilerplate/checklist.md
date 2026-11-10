# The checklist of the technical base

Shared by [gbo-boilerplate](SKILL.md), which passes it before its gate, and by [gbo-architecture-reviewer](../gbo-architecture-reviewer/SKILL.md), which audits by it. Each item holds, or its exception is a decision of `Design/technical-base.md`.

## The decisions

- [ ] `Design/technical-base.md` exists; each decision has its reason, its rejected alternatives and its status; nothing proposed is taken as validated.
- [ ] The style answers the need of the business: Mutation Sourcing for a domain that keeps what happened; Event Sourcing only when the user asks for it; CRUD otherwise.
- [ ] Mutation Sourcing: the models are written by Mutations only, through one pump per chain; the application never opens a write transaction.
- [ ] The place of the models between two runs and the start time are decided together: a model in memory replays its Log at each start.

## The projects

- [ ] Each model project: `AllowUnsafeBlocks`, the Runtime, the Generator with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`; `InternalsVisibleTo` the tests when the model is internal.
- [ ] The solution references the GBO projects by a way written in the deliverable (a submodule, a checkout beside it): GBO is not a package.
- [ ] The test project references the Generator as an analyzer; `DisableTestParallelization` where tests open Stores; a unique Store name per test.

## The Stores

- [ ] Every policy (`FlushPolicy`, `LockPolicy`, `RebuildFromLog()`) is set on the master Store before the first Repository.
- [ ] The Stores are chained by `AddChildStore` before their first transaction; the Repositories and Logs will be chained in the same order.
- [ ] Every Log is Fixed; a model rebuilt from its Log may be Deduced; a persistent CRUD model is Fixed.
- [ ] A chain behind its persistent Log keeps the same Stores, order and names from one opening to the next.
- [ ] At the start, a chain behind its Log is replayed before any write and before the pump.
- [ ] The disposal: the pump, then the Logs, the Repositories, the Stores, then the endpoint.
- [ ] A service host opens, replays and starts the pump at its start, and serves no request before the Replay ends.

## Durability, writers, servers

- [ ] The `FlushPolicy` follows the loss the business accepts, written as such.
- [ ] `StoreLockPolicy.Retain` only when this process is the only writer.
- [ ] The copy of the data outside the machine is decided: a replica, or what the user chose.
- [ ] Several servers: `endPoint.Add(store)` on the first Store of each chain, before its first transaction; one writer at a time per chain; the loss of the owner of the write token stops the writes of its chain (no automatic fail-over): accepted in the deliverable.

## Side effects

- [ ] Every action outside the system (mail, document, payment, call) goes through an output Log chained after the Log, consumed after the commit by a service made idempotent by an identity carried in the Record.

## Time

- [ ] What happens by time is posted by a scheduler of the host, its time in the Intention or the command; nothing reads the clock in `Apply` or a handler.

## Adoption

- [ ] The existing system is inventoried: its writers, its transactions, its side effects, its queries, its volumes.
- [ ] The transition rules are written: one side written per use case; crossings by an output Log; the existing data imported by an Exclusive transaction (CRUD) or by Mutations or Records (sourcing).
