# Reference: the diagnostics of the generator, and their fix

Part of [gbo](../../SKILL.md). From `GhostBodyObject.Generator/Diagnostics.cs`. A Repository or a Log with an error gets **no generated code**: the errors that follow (unknown `Book`, `BookCollection`) are consequences; fix the GBO errors first. GBO013 and GBO014 are reported once the model compiles. GBO015, GBO016 and GBO026 no longer exist.

| Id | Title | Cause | Fix |
|---|---|---|---|
| GBO001 | Entity model not found | `[IncludeEntityModel("X")]` designates no, or several, `[Entity]`/`[SystemFor]` interfaces | `nameof(MX)` of an interface of the same project |
| GBO002 | Unsupported property type | `int?`, `Nullable<T>`, `Option<string>`, `Option<XRef>`, `List<T>`, `T[]`, `object`, a class, a struct of another assembly, a struct without explicit layout, a `*Ref` of a type without `[WithIdentity]`, none of them with a converter in the project | `Option<T>`; an empty `GhostString`; `XRef` + `IsNull`; `GhostSpan<T>`; a converter in the project, found without `[Converter]`; `[WithIdentity]` on the target |
| GBO003 | Invalid property | No `[Property]` (Fixed), no setter, an `init` setter, `[Strong]`/`[Weak]` on a value, a collection of references with a setter | Add the number; `{ get; set; }` for a value or a reference, `{ get; }` for a composition or a collection |
| GBO004 | Invalid property identifier | Out of 1..`maxPropertyIdentifier`, or used twice | A free number; raise `maxPropertyIdentifier` (and the version) |
| GBO005 | Invalid entity identity | A type identifier 0 or above 64999, a version 0 or above 32767, an identifier twice in a Repository | Unique identifiers; ranges per model |
| GBO006 | Entity without birthplace | Neither Root, nor composed, nor designated by a strong reference; or no chain of Owners reaches a Root | Make it a Root, a part of an Owner, or a shared object designated by `[Strong]` |
| GBO007 | Invalid model name | The interface does not start with `M` (entity, Repository, Log, index), `R` (Record, Mutation), `C` (component), `S` (system), `E` (edge) and an uppercase letter | Rename the interface |
| GBO008 | Unsafe code required | `AllowUnsafeBlocks` missing | `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` |
| GBO009 | Unsupported element type | A `GhostSpan` of `string`, `DateTimeOffset`, a converted type, or an unknown type | A span of values, enums, stored structs (a `*Ref` in a Record); texts as parts or separate Records |
| GBO010 | Record model not found | `[IncludeRecordModel("X")]` designates no, or several, `[Record]` interfaces | `nameof(MX)` of a Record of the same project |
| GBO011 | Invalid Record | An identifier 0 or above 64999, a version 0 or above 16383, `[Root]`/`[Removable]`/`[Enumerable]` on a Record, an `[Entity]` in a Log, an identifier twice | Fix the numbers; a Record has none of these attributes |
| GBO012 | Reference by identity in an entity | A `*Ref` property, a `GhostSpan<XRef>`, in an entity; an `EntityRef`, of any type, is allowed | A link (`[Strong] MOrder Order`); in a Read model, derive with `XRef.Of(source)` |
| GBO013 | Lambda that captures | A lambda given to a GBO sequence, an `Option` or a subscription reads a local, a parameter or `this` | Pass the value as a state, before the lambda; mark it `static` |
| GBO014 | LINQ over a sequence of GBO | A `System.Linq` operator on a GBO sequence | The GBO operator of the same name; `.AsEnumerable()` to leave GBO on purpose |
| GBO017 | Weak required reference | `[Weak] T` | `[Weak] Option<T>`, or `[Strong] T` |
| GBO018 | Strong and weak reference | Both attributes on one link | One of them |
| GBO019 | Reference outside the Repository | A link to a type the Repository does not include | Include the type, or name the entity by a `*Ref` in a Record |
| GBO020 | Link in a Record | A Record property whose type is an entity | Its `*Ref` |
| GBO021 | Composition of a Root | A link without `[Strong]`/`[Weak]` to a `[Root]` | `[Strong]` (or `[Weak]`) |
| GBO022 | Setter on a composition | A composition with a setter | `{ get; }`: a part is born in its composition |
| GBO023 | Invalid part type | A part `[Removable]`; a shared object `[Removable]` or `[Orphanable]`; a Root `[Orphanable]` | Remove the attribute: a part leaves through its Owner, a shared object with its last reference |
| GBO024 | Cycle of required compositions | Required parts (`T`) forming a cycle | Make one link an `Option<T>` or a collection |
| GBO025 | Invalid `[OwnedFrom]` | Not a type that composes the part; a collection; a `T` with several Owner types or `[Orphanable]`; an `Option` of the only Owner type of a required part; a setter; a `[Property]`; on a Root, a shared object, a component or an edge | `T` for one Owner type; `Option<T>` per type; `{ get; }`, no number |
| GBO027 | Cycle of strong references (warning) | Types without Owner that may retain each other: such entities are never removed | Break the cycle with a `[Weak]` link or a composition |
| GBO028 | Invalid reverse property | The path is no reference of the other type to this one, of the nature of the attribute; a setter; a `[Property]` | `nameof(MSource.Prop)` of a `[Strong]` (or `[Weak]`) link to this type; `{ get; }`, no number |
| GBO029 | Invalid component type | A component `[Root]`, `[Removable]`, `[WithIdentity]`, `[Orphanable]`; a bad `[ComponentFor]`; composed; a component of a component; a member of a partial class with the name of a slot | Remove the attribute; rename the member (`Power`, not `Battery`) |
| GBO030 | Invalid system | A bad `[SystemFor]`; not exactly one member of the entity type; a member that is not a component of that type; a component both present and excluded | Fix the members |
| GBO031 | Invalid edge type | A bad `[Edge]`; an edge `[Root]`, `[Orphanable]`, `[WithIdentity]`, a component or a system; with parts; composed; a name clash | An edge carries values and links only |
| GBO032 | Invalid text index | `[TextIndexed]` on a non-string or on a Record; an index that is not one `[TextIndex]` interface; number 0; `MinLength` out of 2..4; a number twice | Fix the declaration |
| GBO033 | Invalid stored struct | A struct without explicit layout, a field without offset, a gap or an overlap, a misaligned field, above 32 bytes, an unsupported field (a `string`, a `DateTimeOffset`, a `*Ref` in an entity) | Follow the rules of [../modeling/values.md](../modeling/values.md); declare free bytes as fields |
| GBO034 | Invalid converter | Not an `IGhostConverter<T, TStored>` of the type of the property, or of the T of its `Option<T>`; a stored type unknown to the model; an `Option` of a type converted to a `string`; a span of a converted type | The right converter; a stored struct; an empty value in place of the absent text |
| GBO035 | Needless converter (warning) | A converter on a standard type | Remove it |
| GBO036 | Invalid value index | No component, more than 8, positions not 1, 2, 3..., an unknown property, an option its type does not take, a `decimal` without `Decimals`, a unique index with a `[Flags]` enum or a `GhostString`, a key above 64 bytes, a number 0 or taken | Fix the declaration; a unique text key is a `string` |
| GBO037 | Invalid hash index | A rule of the hash index broken | Fix the declaration |
| GBO038 | Invalid merge | `[Delta]`/`[Max]`/`[Min]` outside a `[Mergeable]` type, on an unsupported type, a bad Merger, an edge or a Record `[Mergeable]`; on a value object without its interfaces (`IAdditionOperators` and `ISubtractionOperators` for `[Delta]`, `IComparable<T>` for `[Max]`/`[Min]`), converted to a `string`, or with bounds | Fix the type or the attribute; declare the interfaces of the value object |
| GBO039 | Invalid concurrency point | `[ConcurrencyPoint]` on a Record, a component, an edge, a `[Mergeable]` type | Put it on the Owner entity of the domain |
| GBO040 | Invalid visibility | `[Private]` or `[Internal]` on a link; both together | On values only, one of them |
| GBO041 | Number refused by the schema | A number (`[Entity(3 numbers)]`, `[Record(3 numbers)]`, `[Property]`, an index number) in a Deduced schema; `[Entity(typeId)]` and `[Record(typeId)]` alone are accepted | Remove the numbers, or switch to Fixed |
| GBO042 | Identity without a fixed type identifier | A `[WithIdentity]` type of a Deduced Repository without `[Entity(typeId)]` | `[Entity(typeId)]` |
| GBO043 | Invalid deduced type identifier | Two fixed identifiers the same, or an identifier of the runtime | Distinct identifiers |
| GBO044 | Type not fixed | A type of a Fixed Repository or Log without its three numbers, or a stored property without its `[Property]` | Write the numbers (copy them from the comment of a deduced generation) |
| GBO045 | Namespace without model | `[IncludeNamespace("X")]` designates no model interface | The right namespace, `SubNamespaces = true` if they are below |
| GBO046 | Several converters | A property without `[Converter]` of a type that several converters of the project convert | `[Converter(typeof(X))]` on the property names the one to use |
| GBO047 | State outside the model (warning) | An instance field or an auto-property in the partial class of a Body: never stored, committed, replicated nor replayed, lost with its Body | A stored property kept by `On...Changed`, a Record, a local variable, or a computed property; `#pragma warning disable GBO047` where nothing else is possible |
| GBO048 | Invalid value object | A `[ValueObject<T>]` interface nested, generic, with members, over an unsupported type (a `string`: use `GhostString`); its `partial struct` not partial, a ref struct over a number, with a field or a constructor of its own; operators its type has not, or an `IsValid` of another signature | Fix the declaration as the message says |
| GBO049 | Invalid Record property | A Record or a bag in a refused form: an `Option` of a bag, a `GhostSpan` or a collection of Records, `[Strong]`/`[Weak]`/`[OwnedFrom]`, `[Delta]`/`[Max]`/`[Min]`, an index on it or on a field of a Record, a bag of more than 8 types, of a type twice, of a type that is not a Record | A Record property, an `Option<R>`, or a bag of 1 to 8 Record types; no index on a Record |
| GBO050 | Type not in the bag | `Enumerate<T>`, `Count<T>`, `TryGetAt<T>`, `IndexOf<T>`, `RemoveAll<T>` or a handler of `Enumerate(h1, …)` of a constrained bag with a View of a type it does not hold: it would read nothing | A type of the bag, or add the type to the bag |
| GBO052 | Generated name taken | Two model interfaces of one namespace give one generated type: `MAddress` and `RAddress`, `MBattery` and `CBattery` | Rename one of them |
| GBO053 | Invalid transition | `[TransitionTo]` on a `[Flags]` enum, with a target that is no member of the enum, or in an enum with two members of one value | A transition goes from one member to another of the same enum, each of its own value |
| GBO055 | Record not declared | A Fixed Repository or Log uses a Record type, by a property or inside another Record, that its definition does not declare | Add `[IncludeRecordModel(nameof(RX))]` to its definition |
| GBO056 | Record of another model | `Add` or `ReplaceAt` of a bag of any type, a property of a Body, with a Record of another model; what the compiler cannot see, the commit refuses | Declare a Record of the model, and write it from the other one |
| GBO054 | Type in two models | An entity, a component, an edge or a Record type that two models, Repositories or Logs, include or reach | Give it to one model; another model takes a type of its own number, identical if needed, in its own namespace |
| GBO057 | Invalid model number | A Fixed Repository or Log without a number, or two models of two names with one number in a compilation | Write a number of its own, `[GhostRepository(1)]`, and keep it |
| GBO063 | Invalid StoreSystem | A system numbered 0 or without a Store; a member that is no `ISystemStore` property with `[SystemStore]`; a Store numbered 0, of an unknown role, of a number or a name (ignoring case) of another; a Repository role with a Log or without a Repository, a Log role with a Repository or without a Log; no Repository Store; two replayable Logs; a Projection without a replayable Log; a Log Store after the first with other than one Log; a model in two Stores or twice in one; a property named after the system, a member of every system, or another property | Follow the rule the message names: the runtime refuses the same declaration |
| GBO065 | Invalid version of the history | An interface of a namespace `.VersionHistory` without `[Entity(typeId, version, maxPropertyId)]`, not named `M{Type}_v{n}` after its version, of a type that no model holds, or of a version not below the current one | Write the history from the Store, `ModelWriter.WriteHistory`, never by hand; remove a version that no Store holds anymore |
| GBO064 | Model of a Store not found | `[IncludeRepository]` or `[IncludeLog]` names no `[GhostRepository]` or `[GhostLog]` interface, or several | Name the model interface, `nameof(MSalesRepository)`; declare the system in the namespace of a model whose name is not unique |

## Runtime refusals that look like build errors

| Symptom | Cause |
|---|---|
| `new Album()` does not compile | `Album` is a part (no public constructor: `artist.Albums.New()`), or it has a required link (`new Album(artist)`) |
| `bowie.Remove` does not exist | The type is not `[Removable]`, or it is a part (removed by its Owner) |
| `new Order(customer)` does not compile | `[WithIdentity]`: `new Order(OrderRef.New(), customer)` |
| `screening.Remove()` does not exist | The edge type is not `[Removable]` |
| Two `AlbumCollection` in one project | Two Repositories: `using static XRepositoryCollections;` inside the namespace of each file |
