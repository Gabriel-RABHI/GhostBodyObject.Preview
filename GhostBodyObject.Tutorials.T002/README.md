# Tutorial T002: object relationships, constraints and deterministic garbage collection

The second level of the tutorials of GBO, for a developer who did [Tutorial T001](../GhostBodyObject.Tutorials.T001/README.md). It shows how the entities of a model hold each other: the references, strong and weak; the aggregates, their parts and their cascades; the shared objects; the reverse properties; and how GBO keeps every link right, refuses what would break one, and removes each entity at the very instruction that releases its last retention, a garbage collection that is deterministic.

The domain is a music label and a streaming service: artists, their albums and their tracks, the recordings and the lyrics they share, the playlists and the favorites of the listeners, the festivals that book artists. Each lesson is a test class: read it from top to bottom, run it, change it. The lessons go in order; each one uses only what the lessons before it showed. A model enriches the previous one with the concept of its lesson: it is declared at the top of the first lesson that uses it, in a namespace of its own. Every value of a model is a value object: those that the models share are declared at the head of the first one, in the namespace of the tutorial, the others at the head of their model.

Run them with:

```bash
dotnet test GhostBodyObject.Tutorials.T002
```

## The references between Roots

| Lesson | Model | What it shows |
|---|---|---|
| [1. Link two entities](Lesson01LinkTwoEntities.cs) | `References` | A link is a property of the type of another entity; `T` required, a parameter of the constructor; `Option<T>` optional; `[Strong]` and `[Weak]`; the links of the Views |
| [2. A strong reference protects](Lesson02StrongReferencesProtect.cs) | `References` | `Remove()` refused while a strong reference designates the entity, `ON DELETE RESTRICT`; the order of the instructions; a Root that is not Removable |
| [3. A weak reference forgets](Lesson03WeakReferencesForget.cs) | `References` | The removal of the target sets every weak reference to it to none, at once, `ON DELETE SET NULL` |

## The aggregates

| Lesson | Model | What it shows |
|---|---|---|
| [4. Parts are born in their Owner](Lesson04PartsAreBornInTheirOwner.cs) | `Compositions` | A composition without `[Strong]` nor `[Weak]`; `New` of a collection and of a slot; a required part born with its Owner; `[OwnedFrom]` |
| [5. Removing an Owner removes its parts](Lesson05RemovingAnOwnerRemovesItsParts.cs) | `Compositions` | A part taken out of its Owner is removed; the cascade, `ON DELETE CASCADE`, done when `Remove()` returns |
| [6. Move a part](Lesson06MoveAPart.cs) | `Compositions` | A part in several compositions of its Owner, `Add`; `MoveTo`, in one operation |
| [7. Build a tree](Lesson07BuildATree.cs) | `Trees` | A part of several types of Owner, a folder in a library or in a folder; no cycle; a subtree removed |
| [8. A collection is a set](Lesson08ACollectionIsASet.cs) | `Sets` | No position, the business order by `OrderBy`; 100,000 parts with the same code as three |

## The retention across aggregates

| Lesson | Model | What it shows |
|---|---|---|
| [9. A reference to a part retains its chain](Lesson09AReferenceToAPartRetainsItsChain.cs) | `Retention` | A playlist retains a track, its album and its artist; the exception names the chain; a reference from inside a removed aggregate retains nothing |
| [10. An orphan survives its Owner](Lesson10AnOrphanSurvivesItsOwner.cs) | `Orphans` | `[Orphanable]`: a retained album survives its artist, whole; removed with its last retention; placed under another artist by `Add` |

## The shared objects

| Lesson | Model | What it shows |
|---|---|---|
| [11. A shared object lives with its references](Lesson11ASharedObjectLivesWithItsReferences.cs) | `Sharing` | A recording, held by no composition, born by its first strong reference, removed with its last; its descriptor, `Init`; `NewRecording` |
| [12. Take the new retention first](Lesson12TakeTheNewRetentionFirst.cs) | `Sharing` | A strong slot, `New`, `Set`, `Clear`; the order of the retentions |
| [13. A collection of strong references](Lesson13ACollectionOfStrongReferences.cs) | `Lineups` | A lineup retains every artist; posters shared between editions |

## The weak links and the reverse properties

| Lesson | Model | What it shows |
|---|---|---|
| [14. A weak collection forgets its targets](Lesson14AWeakCollectionForgetsItsTargets.cs) | `Audience` | Favorites that retain nothing: a removed album leaves them all, the counts exact |
| [15. Who points to me?](Lesson15WhoPointsToMe.cs) | `Audience` | `[ReverseWeakReference]` and `[ReverseStrongReference]`: the fans of an album, the albums of a genre, kept at each instruction |

## Your rules, and the concurrency

| Lesson | Model | What it shows |
|---|---|---|
| [16. Your rules on the links](Lesson16YourRulesOnTheLinks.cs) | `Rules` | `On...Added`, `On...Removed`, `On...Changed` and `OnDeleting`: a duration kept by the code, an archive of the removals |
| [17. Refused, or condemned](Lesson17RefusedOrCondemned.cs) | `Rules` | A refusal changes nothing, the transaction goes on; an exception after a write condemns it: it can only roll back |
| [18. Links and concurrency](Lesson18LinksAndConcurrency.cs) | `Retention` | An aggregate is a unit of conflict; a reference to a target removed concurrently is never committed |

## The identities, and the whole picture

| Lesson | Model | What it shows |
|---|---|---|
| [19. The identities of a graph](Lesson19TheIdentitiesOfAGraph.cs) | `Identities` | Every birth of a type `[WithIdentity]` takes its identity; a command and the Replay of its event create the same graph; no identity drawn in a Replay |
| [20. What dies, and when](Lesson20WhatDiesAndWhen.cs) | `Catalog` | Every link of the tutorial in one catalog: four instructions, and what each one refuses, removes or keeps |

## The capabilities

| Lesson | Model | What it shows |
|---|---|---|
| [21. Capabilities, the components and the systems](Lesson21CapabilitiesAndSystems.cs) | `Capabilities` | `[ComponentFor]`: a tour, a badge, a retirement that an artist gains and loses without knowing them; a component of two entity types; `[SystemFor]`, the artists on tour and not retired, `Of` |

Each model lives in a namespace of its own. The generator imports the collections of a Repository in the whole project, so thirteen models in one project give thirteen `AlbumCollection`: each lesson begins its namespace with `using static CatalogRepositoryCollections;`, the collections of its own Repository, which C# finds before those of the project. An application with one Repository needs no such line, as in Tutorial T001.

The comments of the lessons are written to become the pages of the documentation of the web site: the summary of each class introduces its lesson, the summary of each test says what it shows, and the comments in the code explain each step.
