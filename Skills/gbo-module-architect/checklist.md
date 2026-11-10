# The checklist of the module map

Shared by [gbo-module-architect](SKILL.md), which passes it before its gate, and by [gbo-architecture-reviewer](../gbo-architecture-reviewer/SKILL.md), which audits by it. Each item holds, or its exception is a decision of `Design/modules.md`.

## Responsibilities

- [ ] Each module has one responsibility, said in one sentence, and its use cases listed, each named as an action.
- [ ] Each use case belongs to one module.
- [ ] Each concept has one home: the module whose use cases create it and change it; reference data only read have the module that imports or maintains them; the others read a concept, designate it or attach aspects to it.
- [ ] No module was drawn from an old schema, service or package alone: each comes from a responsibility.
- [ ] The rules that the mission did not settle are asked of the user, their proposed defaults marked *Proposed*.

## Placement

- [ ] Each module has its Repository; a module alone in its Repository (a system module) has the criterion that chose it: another lifetime, writers, servers, style or persistence, never size alone.
- [ ] No rule needs a link between two Repositories: across Repositories there are identities in Records and Mutations, projections, business keys, never a link nor an identity in an entity.
- [ ] The modules of one model are namespaces of one project (a Repository includes the interfaces of its own project only).
- [ ] Each module has its namespaces for its model, its Read model and its Mutations; none is named like a generated type.

## Dependencies

- [ ] The dependencies go one way, without a cycle.
- [ ] A module that references, extends or composes the anchors of another, or submits its Mutations, depends on it; those anchors know nothing of its components.
- [ ] The identities and business keys that cross Repositories are listed.
- [ ] The order of the models follows the dependencies.

## Repositories and chains

- [ ] Each Repository has its chain, instantiating the pattern of the technical base, or its difference decided.
- [ ] One writer at a time per chain: a module that needs writers of its own has a Repository and a chain of its own.
- [ ] A Read model is a Repository chained after its Write model, or fed asynchronously by a subscription when it lives elsewhere.
- [ ] Mutation Sourcing: the Read model is in the assembly of the Mutations (their Decision is private), or reads a Log of published Records.
- [ ] The ranges of type identifiers are unique across the whole application, a sub-range per module, reserved before the models are written.

## Side effects between modules

- [ ] A module that acts on another system does it by an output Log consumed after the commit, never by a call inside a transaction.
