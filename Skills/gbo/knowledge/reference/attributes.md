# Reference: every attribute of a GBO model

Part of [gbo](../../SKILL.md). Verified against `GhostBodyObject.Runtime/Logical/Model/ModelAttributes.cs`, namespace `GhostBodyObject.Runtime.Logical.Model`. The generator reads them by their names; the runtime never does.

Write the attributes of a declaration in one list, on one line: `[Entity(11, 1, 4), Root, Removable, WithIdentity, Enumerable]`, `[Property(7), Private]` (REQUIREMENT-052).

## Types

| Attribute | On | Meaning | Refused with |
|---|---|---|---|
| `[Entity(ushort typeId, ushort version, short maxPropertyIdentifier)]` | interface `MName`, `CName`, `EName` | An entity type of a Fixed schema: identifier 1..64999, version 1..32767, the highest property identifier | A Deduced schema (GBO041) |
| `[Entity(ushort typeId)]` | interface | Deduced schema: the type identifier alone, the rest deduced; required on a `[WithIdentity]` type | A Fixed schema (GBO044) |
| `[Root]` | entity | Lives by itself; nothing removes it unless `[Removable]` | A component (GBO029), an edge (GBO031), a Record (GBO011) |
| `[Removable]` | Root, edge | Generates `Remove()` | A part, a shared object (GBO023), a component |
| `[Enumerable]` | entity, component, edge, system | Generates `XCollection` and `XViewCollection` (a system: `XSystemCollection`) | A Record (GBO011) |
| `[Orphanable]` | part | Survives its Owner while a strong reference retains it; its `[OwnedFrom]` is an `Option<T>` | A Root, a shared object (GBO023), a component |
| `[WithIdentity]` | entity | A business identity: `XRef`, `x.Ref`, the identity first in every birth; irreversible once published | A component (GBO029), an edge (GBO031) |
| `[Mergeable]`, `RemovalWins = bool` | entity, component | Concurrent changes of values merge: `[Delta]`, `[Max]`, `[Min]`, the three-point rule, `bool Merge(TView epoch, TView head)`; relations keep their conflicts; `RemovalWins` (off by default): a removal wins over a write of values | An edge, a Record, a `[ConcurrencyPoint]` (GBO038, GBO039) |
| `[ConcurrencyPoint]` | entity | Every change of its domain marks it: one domain, one unit of conflict; `ConcurrencyVersion` read only | A Record, a component, an edge, a `[Mergeable]` type (GBO039) |
| `[ComponentFor(string entityModel)]` | interface `CName`, repeatable | A component of the entities of that type, at most one per entity | |
| `[SystemFor(string entityModel)]` | interface `SName` | A system: the entity and its components as Views | |
| `[ExcludeComponent(string componentModel)]` | system, repeatable | An entity with that component is out of the system | |
| `[Edge(string from, string to)]`, `Multiple = bool`, `Symmetric = bool` | interface `EName` | A relation with values between two entities that neither owns, removed with either; one per pair unless `Multiple`; `Symmetric`: no direction, one type | |
| `[Record(ushort typeId, ushort version, short maxPropertyIdentifier)]` | interface `RName` | A Record type: of a Log, or the type of a property of an entity or a Record, alone, in an `Option`, in a bag; version 1..16383; in a deduced schema, the prefix `R` alone marks it | |
| `[Record(ushort typeId)]` | interface `RName` | In a deduced schema, fixes the type identifier alone | Three numbers in a deduced schema (GBO041) |
| `[Mutation]` | Record | A Mutation: public properties the Intention, `[Private]` ones the Decision; `Decide()` in the Body, `Apply()` in the View, `Submit()` generated | |
| `[Internal]` | entity, Record, Repository, Log | Its Body, View and `*Ref` (or the classes of the Repository or Log) generated `internal` | |

Every `nameof` argument names the **model interface**: `[ComponentFor(nameof(MDoorLock))]`, `[Edge(nameof(MCritic), nameof(MMovie))]`.

## Properties

| Attribute | On | Meaning |
|---|---|---|
| `[Property(short identifier)]` | stored property (Fixed) | Its number, 1..`maxPropertyIdentifier`, unique in its type, never reused; refused in a Deduced schema |
| (none) | link | A composition: `T` (required part), `Option<T>` (slot), `IEntityCollection<T>` (parts); no setter (GBO022); never toward a Root (GBO021) |
| `[Strong]` | link `T`, `Option<T>`, `IEntityCollection<T>` | A strong reference: refuses the removal of a Removable Root, retains a non-Root |
| `[Weak]` | link `Option<T>`, `IEntityCollection<T>` | A weak reference: set to none when the target goes; never on a `T` (GBO017); never with `[Strong]` (GBO018) |
| `[OwnedFrom]` | `T` or `Option<T>`, get only, no `[Property]` | The Owner of a part: `T` for a single Owner type that it requires; `Option<T>` per type for several Owner types, or for `[Orphanable]` (GBO025) |
| `[ReverseStrongReference(string path)]` | `IEntityCollection<MSource>`, get only, no `[Property]` | The entities that designate this one by the strong reference `path` (kept because declared) |
| `[ReverseWeakReference(string path)]` | the same | The entities that designate this one by the weak reference or collection `path` (always kept) |
| `[Private]` | value | Its members `private` in the Body and the View: only the partial classes access it; never on a link, never with `[Internal]` (GBO040) |
| `[Internal]` | value | Its members `internal` |
| `[Converter(Type converter)]` | property of a type unknown to the model, or an `Option` of one | Stored by an `IGhostConverter<T, TStored>` (GBO034, GBO035); optional when the project declares one converter of the type, which the generator finds; required to choose among several (GBO046) and for a converter of another assembly. A struct of explicit layout needs none |
| `[ValueObject<T>]`, `Operators = ValueOperators.Add | Subtract | Multiply | Divide`, `ImplicitConversion = bool` || Subtract | Multiply | Divide` | interface `VName` over a standard value type or over `GhostString` | A value object generated by GBO (REQUIREMENT-053, REQUIREMENT-052): its struct `Name` written, its rules `static bool IsValid(T value)` and `ToString()` yours, in a `partial struct Name`; a property of the model names `VName`, stored as `T`; the operators on request; over `GhostString`, two types, `X` for the Bodies and `ReadOnlyX` for the Views; GBO048 when it breaks a rule |
| `[Delta]`, `Min = n`, `Max = n` | numeric or `TimeSpan` value of a `[Mergeable]` type, or its `Option` | Merged as `head + (new − epoch)`; bounds (`long` or `double` constants) on the merged value: Setter refusal, `Bound` conflict |
| `[Max]`, `[Min]` | number, date, time, `TimeSpan` of a `[Mergeable]` type | Merged by the max (min) of the head and the new value, the head when untouched |
| `[TransitionTo(object target)]`, repeatable | **member of an enum**, not a property | A value that this member may become: the enum has its transitions, a member without any is final, the property of every Body refuses another change by a `GhostTransitionException`, at the birth too; `value.CanTransitionTo(target)` asks first; GBO053 on a `[Flags]` enum, a target that is no member, two members of one value ([../modeling/values.md](../modeling/values.md)) |
| `[TextIndexed(string index, TextIndexing mode = Immediate)]` | `string` or `GhostString` of an entity | Its terms in the text index of the interface `index`; `TextIndexing.Commit` cuts them once |

## Indexes

| Attribute | On | Meaning |
|---|---|---|
| `[TextIndex(ushort number = 0)]`, `MinLength = 2..4`, `KeepAccents = bool`, `Separators = string` | interface `MName` | A text index; its number stable and unique in the Store (deduced when absent in a Deduced schema) |
| `[ValueIndex(string entity, ushort number = 0)]`, `Unique = bool` | interface `MName` | A value index of the type `entity` (a model interface name) |
| `[ValueIndexComponent(int position, string property)]` | value index, repeatable | A component, positions 1..8 |
| `[ValueIndexComponent(position, property, DatePrecision precision)]` | | `Tick` (default), `Second`, `Minute`, `Hour`, `Day`, `Month`, `Year` |
| `[ValueIndexComponent(position, property, DatePart part)]` | | `MonthOfYear`, `DayOfMonth`, `DayOfWeek`, `HourOfDay`, `MinuteOfHour` |
| named: `Step = long`, `Decimals = int` (required for `decimal`), `IgnoreCase`, `IgnoreAccents`, `RawTicks` | `ValueIndexComponent` | Rounding, folding, raw `DateTime` ticks |
| `[HashIndex(string entity, ushort number = 0)]`, `Unique = bool` | interface `MName` | A hash index: an exact key |
| `[HashIndexComponent(int position, string property)]`, `IgnoreCase`, `IgnoreAccents` | hash index, repeatable | A component, positions 1..8 |

Text, value and hash indexes share one numbering in a Repository (GBO036 on a duplicate). Searches: `TextSearch.Default`, `Fuzzy`, `Ranked` (flags).

## Repositories and Logs

| Attribute | On | Meaning |
|---|---|---|
| `[GhostRepository(number)]`, `Schema = GhostSchema.Fixed / Deduced`, `FirstTypeId = 1` | interface `MName` | A Repository: its class, transaction, tag, collections. Its number, required in a Fixed schema, is the model: its name stays free |
| `[GhostLog(number)]`, `Schema`, `FirstTypeId` | interface `MName` | A Log: its class, transaction, subscription, Records |
| `[IncludeEntityModel(string modelName)]` | Repository, repeatable | Includes an entity, a component, a system or an edge type (GBO001 when none or several) |
| `[IncludeRecordModel(string modelName)]` | Repository or Log, repeatable | Declares a Record type (GBO010): a Fixed model declares each Record it uses, those that a property reaches and the messages of a bag (GBO055); a Deduced model deduces them |
| `[IncludeNamespace(string name)]`, `SubNamespaces = bool` | Repository or Log, repeatable | Every model interface of the namespace, the types they designate included (GBO045 when empty) |
| `[Internal]` | Repository, Log | Its classes `internal` |

A Repository also has the types its included types designate by their properties; components, systems and edges are designated by none: include them.

## StoreSystems

| Attribute | On | Meaning |
|---|---|---|
| `[StoreSystem(number)]`, `SchemaVersion`, `MigrationNumber`, `ReplayNumber` | interface `MName` | A system of Stores (REQUIREMENT-060): its class, `partial`, derived from `StoreSystemBase`. Its number is the system; its name stays free. Its numbers of evolution, from 0, upward only: `SchemaVersion` a mark; `MigrationNumber` raised for a migration of its States; `ReplayNumber` raised for a replay of its Projections |
| `[SystemStore(number, StoreSystemKind kind)]` | an `ISystemStore` property of the system | A Store: its number in the system, its role, `State`, `Projection`, `ReplayableLog` or `Log`; the name of the property names its files |
| `[IncludeRepository(string modelName)]` | a Store `State` or `Projection`, repeatable | A Repository of the Store; several share one transaction of the Store, the first declared first (GBO064 when none or several) |
| `[IncludeLog(string modelName)]` | a Store `ReplayableLog` or `Log`, repeatable | A Log of the Store; a Log Store after the first one includes one (GBO063) |

## Documentation

| Attribute or text | On | Meaning |
|---|---|---|
| A `///` or `/** */` XML documentation | Any declaration of the model | Written as is on what the generator writes for it, and stored in the schema, `Documentation`; no project setting needed |
| `[System.ComponentModel.Description(string)]` | Any declaration of the model | Stored in the schema, `Description`; the summary of the generated code when the XML documentation has none |

Both are outside the definition of a version: changing them never asks for a new version ([schema.md](../modeling/schema.md#the-whole-model-in-its-store)).

## Runtime types used in a model

| Type | Namespace | Use |
|---|---|---|
| `IEntityCollection<T>` | `GhostBodyObject.Runtime.Logical.Model` | The declaration of a collection (parts, references, reverse) |
| `Option<T>`, `Option.None` | `GhostBodyObject.Runtime.Logical.Body` | Optional links and values |
| `GhostString`, `GhostSpan<T>` (Body), `ReadOnlyGhostString`, `ReadOnlyGhostSpan<T>` (View) | `GhostBodyObject.Runtime.Logical.Body` | Texts and arrays read in place |
| `IGhostConverter<T, TStored>`, `GhostConverters.CheckOrder` | `GhostBodyObject.Runtime.Logical.Model` | Converters |
| `TextIndexing`, `TextSearch`, `DatePrecision`, `DatePart`, `GhostSchema` | `GhostBodyObject.Runtime.Logical.Model` | Enums of the attributes |

## Not built: never use

`[Required]`, `[Utf8]`, `[ZipCompressed]`, `[SingleBit]`, `[Separated]`, `[Immutable]`, `[Protected]`, `[CheckTransaction]`, `[IncludeNamespaceEntityModels]`, `[IncludeNamespaceRecordModels]`, `GhostQueue<T>`, `GhostBitmap`, `IIndexKey<T>`. Removed: `[ValueObject]` (use a converter), the conflict level `Override`. Never existed in this API: `[ObjectBody]`, `[BodyProperty]`, `[EntityBody]`, `[ComponentBody]`, `[System]`, `[SystemMemberType]`, `[TypeIdentifier]`, `[ReverseRelationAttribute]`, `[ForbidTargetDeletion]`, `IBodyEntityCollection<T>`.
