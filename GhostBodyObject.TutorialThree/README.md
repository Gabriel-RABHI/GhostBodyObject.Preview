# Tutorial Three: Entity Component System and object composition model

The third level of the tutorials of GBO, for a developer who did [Tutorial Two](../GhostBodyObject.TutorialTwo/README.md). It shows how the entities of a model gain capabilities that they do not know, the components; how a capability is shared by several types of entities without any base class; how a system joins an entity and its capabilities; how roles replace the subclasses of an object model; and how values that go together, an address, a history, an inbox, travel in an entity as Records.

The domain is the technical management of a building, then the parties of a business: door locks, cameras and thermostats, their batteries, places, firmwares and maintenances; companies and people, the customers, suppliers and employees that they are. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. A model is declared at the top of the first lesson that uses it, in a namespace of its own. Every value of a model is a value object: those that the models share are declared at the head of the first one, in the namespace of the tutorial, the others at the head of their model.

Run them with:

```bash
dotnet test GhostBodyObject.TutorialThree
```

## Capabilities

| Lesson | Model | What it shows |
|---|---|---|
| [1. Give an entity a capability](Lesson01GiveAnEntityACapability.cs) | `Capabilities` | `[ComponentFor]`, an interface starting with `C`; the slot `door.Battery`, `New`, `GetOrNew`, `Any`, `Get`, `TryGet`, `Remove`; one component of a type per entity; the component reads its entity; the slot of a View |
| [2. A capability lives with its entity](Lesson02ACapabilityLivesWithItsEntity.cs) | `Capabilities` | The parts of a component, removed with it; the components removed with their entity, never the reverse; no `MoveTo` |
| [3. The links of a capability](Lesson03TheLinksOfACapability.cs) | `Capabilities` | A required link given to `New`; `GetOrNew` and `TryGetOrNew`, which compare it; a strong reference to a component retains it and its entity; a weak one forgets it |

## Shared capabilities

| Lesson | Model | What it shows |
|---|---|---|
| [4. One capability, several types](Lesson04OneCapabilitySeveralTypes.cs) | `Devices` | `[ComponentFor]` repeated: a battery on cameras and locks, an `Option` of each entity type; the collection of the component, every device that has it |
| [5. Your interfaces over the capabilities](Lesson05YourInterfacesOverTheCapabilities.cs) | `Devices` | C# interfaces on the partial classes, `ILocated` and `IPowered`: one method for every device, without inheritance |

## Systems

| Lesson | Model | What it shows |
|---|---|---|
| [6. A system joins an entity and its capabilities](Lesson06ASystemJoinsAnEntityAndItsCapabilities.cs) | `Devices` | `[SystemFor]`, an interface starting with `S`; required, optional and excluded components; a collection of Views; `ToBody()` for the few to change |
| [7. The system of one entity, and your code in it](Lesson07TheSystemOfOneEntity.cs) | `Devices` | `Of(entity)`; a capability given or taken seen at once; a rule written in the partial struct of the system |

## The composition model

| Lesson | Model | What it shows |
|---|---|---|
| [8. Roles, not subclasses](Lesson08RolesNotSubclasses.cs) | `Parties` | A party that is a customer, a supplier and an employee at once, gains and loses its roles, and stays one party; systems over the roles |
| [9. Capabilities and concurrency](Lesson09CapabilitiesAndConcurrency.cs) | `Parties` | A component is an aggregate of its own: a role and its party change in parallel; two transactions that give one party a role conflict, and `GetOrNew` makes the retry the same code |
| [10. What composes what](Lesson10WhatComposesWhat.cs) | `Parties` | Components, parts, strong references and exclusions in one model: what each removal refuses, removes or keeps |

## Values that travel together

| Lesson | Model | What it shows |
|---|---|---|
| [11. Values that travel together](Lesson11ValuesThatTravelTogether.cs) | `Contacts` | A Record, an interface starting with `R`, as a property: an address, never absent, and an `Option` of one; changed by its copy, `ToBody()`, then assigned, the value read before unchanged; a bag of one type, the history of the moves, in their order; a bag of any Record, an inbox for two services, each one reading its own messages, taking one, dequeuing |

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
