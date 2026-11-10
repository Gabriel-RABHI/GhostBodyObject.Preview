# Schemas: Fixed or Deduced numbers, and the evolution of a model

Part of [the modeling knowledge](README.md). GBO stores each value under the **number** of its property, and each entity under the **number** of its type, never under their names. The numbers are what makes a model evolve: a new version of the application reads the data of the old one, and the old one the data of the new one. A Repository or a Log numbers its model in one of two ways that differ **by their numbers only**: every other attribute (`[Root]`, `[Removable]`, `[Enumerable]`, the links, the indexes, `[WithIdentity]`) is written the same in both, with the same meaning.

## Fixed, the default

```csharp
[Entity(1, 1, 5)]                 // type identifier (1..64999), model version (1..32767), highest property identifier
[Root, Removable, Enumerable]
internal interface MBook
{
    [Property(1)] GhostString Title { get; set; }
    [Property(2)] GhostString Author { get; set; }
    [Property(3)] int Year { get; set; }
    [Property(4)] decimal Price { get; set; }
    [Property(5)] bool Borrowed { get; set; }
}

[GhostRepository(1)]              // its number; Schema = GhostSchema.Fixed by default
[IncludeEntityModel(nameof(MBook))]
internal interface MLibraryRepository { }

[Record(1, 1, 4)]                 // a Record: type identifier, version (1..16383), highest property identifier
internal interface RParcelRegistered { /* [Property(n)] on each property */ }
```

- Each type writes its three numbers; each stored property its `[Property(n)]`, 1 to `maxPropertyIdentifier`, unique in its type (GBO004); each index its number, unique among the indexes of its Repository (text, value and hash share one numbering).
- Properties without storage take no `[Property]`: `[OwnedFrom]`, the reverse properties, the members of a system, and a property with a body (code).
- The versions follow the rules of compatibility: a binary reads the entities of older and newer versions of its model.
- A Fixed type without its numbers, or a stored property without its number, is refused (GBO044).

## Deduced

```csharp
[GhostRepository(Schema = GhostSchema.Deduced, FirstTypeId = 1)]
[IncludeEntityModel(nameof(MCustomer))]           // the others are found by their relations
internal interface MShopRepository { }

[Entity(1)]                                       // required: a [WithIdentity] type fixes its type identifier
[Root, Removable, WithIdentity, Enumerable]
internal interface MCustomer
{
    GhostString Name { get; set; }                // no [Property]: numbered by the order of the names
    [ReverseStrongReference(nameof(MOrder.Customer))]
    IEntityCollection<MOrder> Orders { get; }
}

[Entity(3), Root, Removable, WithIdentity, Enumerable]
internal interface MOrder
{
    [Strong] MCustomer Customer { get; set; }
    decimal Total { get; set; }
    IEntityCollection<MOrderLine> Lines { get; }
}

internal interface MOrderLine                     // no [Entity] at all: a part without identity
{
    [Strong] MProduct Product { get; set; }
    int Quantity { get; set; }
    [OwnedFrom] MOrder Order { get; }
}
```

**What is deduced:**
- **The types of a Repository**: those it includes by name (`[IncludeEntityModel]`) or by namespace (`[IncludeNamespace("Shop.Model", SubNamespaces = true)]`), then, until none is added, every type a property of one of them designates. **Components, systems and edges** are designated by no property: include them by name or namespace. This discovery serves both schemas.
- **The type identifiers**: `[Entity(typeId)]` when written; otherwise the free identifiers from `FirstTypeId`, in the ordinal order of the full names of the types.
- **The version**: 1, always. **The property identifiers**: 1 plus the rank among the stored properties of the type, in the ordinal order of their names (moving a property in its interface changes nothing; renaming one changes the numbers). **The index numbers**: the rank of each index interface, by name.
- The generated code starts with a comment that lists the deduced numbers: copy them to freeze the model into a Fixed one, without changing anything else.

**Its Records** (REQUIREMENT-052): the prefix `R` marks a Record type; a deduced Repository numbers its Record types with its entity types, in the order of their full names; `[Record(typeId)]` fixes one.

**What it refuses:** `[Entity]` with three numbers, `[Record]` with three numbers, `[Property]`, an index number (GBO041); a `[WithIdentity]` type without `[Entity(typeId)]` (GBO042, its `*Ref` holds the type identifier outside the model).

**Its compatibility is strict**: each stored definition carries the hash of the whole model; the bind of a persistent or replicated Store refuses another deduced model, naming its first difference. Any change of the model refuses the bind of existing data.

## Which schema

| The model | Schema | Why |
|---|---|---|
| The Write and Read models **in memory**, rebuilt at each start by a Replay of a persistent Log | **Deduced** | The Replay rebuilds them whatever their change: "the Replay migrates" |
| The Log of Records, the Mutation Log | **Fixed** | Its Records are kept forever, read by every future version |
| A persistent CRUD model, rolling upgrades, replicas of several versions | **Fixed** | Old and new binaries read and write each other's data |
| Application state (MVU, Blazor), tests, prototypes | Deduced | No numbers to maintain |
| A persistent deduced model | Possible | Accept that any model change refuses the existing data |
| A model kept in memory or on disk by a choice of deployment | As its persistent case | A deployment on disk keeps the data: Fixed, or accept the refusal above |

## Several Repositories on one Store

The rules of the user (2026-10-31, REQUIREMENT-057): model with them now. What is not built yet is marked.

- **A type belongs to one model**, a Repository or a Log: an entity, a Record, an index is never shared, even with the same definition.
- **A type is its identifier and its nature**, entity, Record or index; never its name.
- **A Repository is a whole unit**: its schema, stored in its Store, regenerates its model alone, with no type of another model, so that one Store, compiled from its schema at run time, reads all its content. Five Repositories on one Store have five schemas (not built: one catalog per Store today).
- **Declare every Record type of a model** in its definition, `[IncludeRecordModel(nameof(RX))]`, those that a property reaches, at any depth, and the messages of a bag (GBO055); a Deduced model deduces its list.
- **A Log Store takes several Logs**, for messages, Jobs and Commands, each with its types and its schema; one reader, each Log registering its handlers, called in the order of the Store: see [logs-chains](../logs-chains.md). A Mutation Log keeps its Store.
- **The bind of a Log does nothing**: a Log writes its model, its own types only, when its hash differs from the last one of its number in the Store, which it reads in the last Segment.
- **Two numbers make two types**: identical types of two numbers may live in two namespaces, under one name; never in one namespace.
- **A Repository type binds to as many Stores as the process opens**, each instance on its Store: its Refs resolve, and its Bodies write, in the transaction of the type open in their execution context. An execution context holds one transaction of a Repository at a time; two execution contexts open transactions of one Repository and one Store, or two, in parallel (REQUIREMENT-064).
- **A model has a number**, `[GhostRepository(1)]`, `[GhostLog(1)]`, required in a Fixed schema and kept for ever: it says "the same model" from one binary to the next, and keys its schema in the Store (GBO057). A Deduced model takes 1, or the number it writes.

The bind of a Repository compares it with the schema of the Store, every Repository already bound and every Log of its chain, and throws `InvalidOperationException` at the first conflict, with nothing written:

| Declared by a second Repository of the Store | The bind |
|---|---|
| The same definition of a type (same identifier, same version, same content) | Refused: a type is in one model, never shared; GBO054 when both are in one compilation |
| The identifier of a type of the first, under any name, in any version, with another definition | Refused |
| An entity on the identifier of a Record, or a Record on the identifier of an entity | Refused, in any Repository, in any version |
| An index of the number of an index of the first | Refused: index numbers are unique in a Store, type identifiers are another numbering. Number the indexes of the Repositories of a Store apart |
| A Record of a Log on an identifier of a Repository of its Store, chained or not | Refused at `Chain`; a Repository bound after the chain is refused at its bind |

- A Repository renames its own type by a new version: the same type, whatever its name. A Repository renames itself freely, under its number: its types are still its own.
- A deduced model is left out of this: its identifiers, numbered by the generator, conflict with those of another model. Bind it alone to its Store. It is compatible with persistence and distribution.

## Evolving a Fixed model

| Change | Rule |
|---|---|
| Rename a property, a type, an interface | A new version: the names are part of the definition of a version; the numbers stay |
| Move a type to another namespace, change a documentation or a `[Description]` | Free: outside the definition of a version, rewritten in the Store by the binary of the latest version |
| Add a property | A new number above the others; raise `maxPropertyIdentifier` and the version; old entities read its default (`White`: zeros, `Option` none) |
| Remove a property | Stop declaring it; **never give its number to another property** |
| Change the type of a property | It is a new property: a new number |
| Change the layout of a stored struct, or the stored type of a converter | A new type: a new number |
| Add, rename, reorder enum members | Free; changing its underlying type is a new property |
| Add a required link `T` to an existing type | Refused directly: add it as `Option<T>`, fill it by a migration, then make it `T` in a later version once every binary writes it |
| Add `[WithIdentity]` to a type | Old entities have none: `Ref` throws on them until a write gives them one |
| Remove `[WithIdentity]` | Refused at the bind: old events must stay resolvable |
| Remove a reference or a collection, switch weak and strong, add an index | Structure changes: an explicit migration in CRUD; nothing in Event or Mutation Sourcing, where the Replay rebuilds the model with the new code |
| Add an index | Built at the start of the Repository from the entities already there (a longer start); rebuilt when its declaration changes; dropped when no longer declared |
| Change a Record | Its fields evolve by numbers as an entity's: an older Record reads the fields it has, a new field its default; a changed meaning is a new Record type. A Record held by entities takes its new version alone, its hosts keeping theirs; both versions live side by side in the hosts and the bags |

## The whole model in its Store

A Store holds its whole model, but the code of the partial classes ([REQUIREMENT-054](../../../../GhostBodyObject.Runtime/Logical/REQUIREMENT-054.md)): each version of each type with its namespace, its accessibility, its declared property types, its flags and its texts; and a catalog, by namespace, of the value objects, enums and structs (with the names of their fields), the systems, the Repositories, the Logs and their indexes. A tool reads a Repository or a Log with it, and `ModelWriter.Write(catalog, definitions)` writes the C# of the model back.

- **Document the model where you declare it**: a `///` comment and `[Description]` on an interface, a property, a value object, an enum and its members, a struct and its fields. Both are kept, in two fields; the generated Body, View, Record, index and Repository show them in IntelliSense.
- **The namespace is the separation of the declarations**: two `User` in two namespaces, of two type identifiers, are two types.
- **The binary of the latest version rewrites these texts** at its start; an older binary and a read-only Store never write them.
- **A Store regenerates its models, with no code of theirs**, a Repository Store as a Log Store: `StoreSchema.Read(store)` gives each model, `ModelWriter.Write(schema)` its C#, alone, and `SchemaCompiler.Compile(schema, code: [reader])`, in `GhostBodyObject.Schema.Compiler`, the assembly, each older generation in `V{n}.{Namespace}`, with the C# that reads it: strings, spans and bags are ref structs, which reflection cannot read. Open the Store read-only to read it with an older generation. The rule of a value object and a converter of the developer are code, in no schema. A `*Ref` of another model comes back as its 16 bytes, a value.
- **A type that a binary leaves stays in its model**: its Ghosts are in the Store, and no other model takes its identifier.
- **The generated code carries the schema**: `{Repository}.Schema.g.cs`, a comment between `/* <gbo-schema>` and `</gbo-schema> */`, the JSON of the catalog and of the definitions; read it to see what the generator understood of the model, without a Store.

## Type identifiers across a chain

The types of every Repository and Log of a chain need identifiers unique across the chain: `Chain()` checks it. Give each model a range: Write model 1-99, Read model 200-299, Records 1000-1999, Mutations of one area 2200-2399.
