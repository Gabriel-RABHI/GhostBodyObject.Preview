# Reference: what the generator writes

Part of [gbo](../../SKILL.md). The generated types are in the namespace of their model interface and take its accessibility (`[Internal]` forces `internal`). The model interface name without its prefix (`M` an entity, a Repository, a Log or an index, `R` a Record, `C` a component, `S` a system, `E` an edge) is the base name.

## Per declaration

| Declaration | Generated |
|---|---|
| `[GhostRepository] MShopRepository` | `ShopRepository` (constructor `new ShopRepository(store)`, `OpenTransaction(...)`, `Chain(...)`, `Store`, `Model`), `ShopRepositoryTransaction` (`Current`, `Require()`), `ShopRepositoryTag`, `ShopRepositoryCollections` (static, with `global using static` in the project) when a type is `[Enumerable]` |
| `[Entity] MBook` | `Book`: `sealed partial class`, the Body; `BookView`: `readonly partial struct`; `Book.TypeId`; `BookVTable` (internal machinery) |
| `[Enumerable]` | `BookCollection` (Bodies) and `BookViewCollection` (Views) in `ShopRepositoryCollections` |
| `[Removable]` Root | `book.Remove()` |
| `[WithIdentity]` | `BookRef` (`New()`, `Of(otherRef)`, `Resolve()`, `ResolveOrNone()`, `TryResolve(out)`, `IsNull`, `Parse`, `TryParse`, `ToString`, equality, order), `book.Ref`, `bookView.Ref`; every birth takes the `BookRef` first |
| A required link `[Strong] MArtist Artist` | A constructor parameter (or `New` parameter for a part), a Setter |
| A required strong link to a shared object | Its parameter accepts an existing object or `new Recording.Init()`; `track.NewRecording(...)` |
| A required part with links or identity | Its `Init` descriptor: `new Release.Init(releaseRef)`, `new OrderDetails.Init(carrier)` |
| `IEntityCollection<MTrack> Tracks` (composition) | A part collection: `New(...)`, `Add`, `Remove`, `Contains`, `Count()`, the operators; `track.MoveTo(other.Tracks)` |
| `Option<MBooklet> Booklet { get; }` (composition) | A slot: `Any`, `Get`, `TryGet`, `GetOr`, `Match`, `New`, `GetOrNew`, `TryGetOrNew`, `Remove()`; `booklet.MoveTo(other.Booklet)` |
| `[Strong] Option<MLyrics>` to a shared object | A strong slot: `New`, `Set(x)`, `Clear()`, the `Option` API, `GetOrNew`, `TryGetOrNew` |
| `[Strong]` / `[Weak] IEntityCollection<T>` | `Add`, `Remove` (and `New` toward a shared object), the operators |
| `[ReverseStrongReference]`, `[ReverseWeakReference]` | A read-only collection, on the Body and the View |
| A property `X` | `partial void OnXChanged()`; for a collection `X`: `partial void OnXAdded(T item)`, `OnXRemoved(T item)`; `partial void OnDeleting()` |
| `[ComponentFor(nameof(MDoorLock))] CBattery` | `Battery` (Body), `BatteryView`; the slot `doorLock.Battery` (`Any`, `Get`, `TryGet`, `GetOr`, `New`, `GetOrNew`, `TryGetOrNew`, `Remove()`); `battery.DoorLock` (`T`, or an `Option` per entity type) |
| `[SystemFor(nameof(MDoorLock))] SPoweredLock` | `PoweredLockSystem` (`readonly partial struct`, its members Views), `PoweredLockSystemCollection` with `[Enumerable]`, `PoweredLockSystem.Of(body or view)` → `Option` |
| `[Edge(nameof(MCritic), nameof(MMovie))] ERating` | `Rating` (Body), `RatingView`; `critic.NewRating(movie, values...)`; `critic.RatingEdges` (`Targets`, `To(movie)`, `Contains`, `GetOrNew`, `TryGetOrNew`, `Remove(movie)`); `movie.RatingEdges` (`Sources`, `From(critic)`); `rating.Critic`, `rating.Movie`, `rating.Remove()` |
| Edge of one type, directed | `city.RoadEdges` (`Targets`, `To`), `city.IncomingRoadEdges` (`Sources`, `From`); `road.From`, `road.To` |
| Edge `Symmetric = true` | `person.FriendshipEdges` (`Others`, `With(other)`); `friendship.Other(person)` |
| `[Mergeable]` | `private bool Merge(BookView epoch, BookView head)` in the partial Body, optional |
| `[ConcurrencyPoint]` | `ConcurrencyVersion` (read only) |
| `[TransitionTo]` in `enum Status` | `StatusTransitions`, in the namespace of the enum: `status.CanTransitionTo(target)` |
| A Body, a View | `body.ToView()`, `view.ToBody()`, `Claim()`, `view.TryLock(duration, out lease)`, `view.IsLocked` |
| `[TextIndex] MProductTextIndex` | `ProductTextIndex`: `Number`, `Search(query, options)`, `Search<TView>`, `Search<TBody>`, `Count(query, options)`, `Terms(prefix)` |
| `[ValueIndex] MOrderStatusDayIndex` | `OrderStatusDayIndex`: `Number`, `Every`, `Is`, `From`, `Before`, `Between`, `Range`, `IsNone`, `Has`, `One`, `All`, `None` per component; stages `OrderStatusDayIndex.WithStatus` |
| `[HashIndex] MDeviceSerialIndex` | `DeviceSerialIndex`: `Find(key)` (unique), `Exists(key)`, `Count(key)`, `Is(key)` |
| `[GhostLog] MCarrierLog` | `CarrierLog` (`OpenTransaction()`, `SubscribeFrom(from, token)`, `On(replay)`, `Chain(output)`, `Store`), `CarrierLogTransaction` (`Current`, `Require()`), `CarrierLogSubscription`, `CarrierLogTag`, `ICarrierRecord`, `ICarrierRecordView`, `CarrierLogCollections.CarrierCollection` |
| `[Record] RParcelScanned` | `ParcelScanned` (Body: detached creation, `Add()` and `ToView()` for a Record of a Log, `On{Property}Changed()`, the conversions to its View and to its `Option`, but for a Mutation), `ParcelScannedView` (`ToBody()`, a detached copy, but for a Mutation) |
| A Record property `RAddress Home`, `Option<RAddress> Billing` | `AddressView Home { get; set; }`, `Option<AddressView> Billing { get; set; }` on the Body, `{ get; }` on the View; the Setter copies, `On{Property}Changed()` |
| A bag `RecordBag Inbox`, `RecordBag<RMove> Moves` | `RecordBag` / `RecordBag<MoveView>` on the Body, written through it; `RecordBagView` / `RecordBagView<MoveView>` on the View; see [values.md](../modeling/values.md#records-a-value-of-several-fields-and-the-bags) |
| Every `[GhostRepository]` and `[GhostLog]` | `{Name}.Schema.g.cs`: the schema of the model in a comment, the JSON of its catalog and of its definitions between `/* <gbo-schema>` and `</gbo-schema> */` |
| A `///` documentation or a `[Description]` on a declaration | The same documentation on its generated types and members; `[Description]` as their summary when the XML has none; the documentation of the values of an edge on the parameters of its birth |
| `[StoreSystem(1)] MShopSystem` | `ShopSystem : StoreSystemBase`, `sealed partial` (`new ShopSystem(options)`, `Start()`, `State`, `OpenTransaction(mode, conflictLevel)`, `Number`, `Dispose()`); a `TransactionalMemoryStore` property per Store, named as its property, a property per Repository and per Log, named as its class; `OnReplay(LogStoreReplay)` for the developer to write in the other part of the class; with `MigrationNumber` above 0, `private partial void Migrate(ShopSystemMigration migration)`, required, and `ShopSystemMigration : StoreSystemMigration` (`From`, `To`, `Changes`, `Versions<TView>()`) |
| `[Entity(21, 1, 3)] MParcel_v1` in `Carrier.VersionHistory` | `ParcelView_v1 : IHistoryView<ParcelView_v1>`, read-only, over a Ghost of the Store in the version 1 (REQUIREMENT-060, step 6): its fields as the version declares them, a link the View of its target, a collection `HistoryCollection<TView, TRepo>`; `ParcelVTable_v1`; `migration.Upgrade(old)`, the Body of the current version born from it. Written by `ModelWriter.WriteHistory`, never by hand |
| `[Record] [Mutation] ROrderPlacement` | `OrderPlacement : MutationBodyBase` (`Status`, `IsProcessed`, `Failure`, `internal Submit()`, `private partial void Decide()` to write), `OrderPlacementView` (`internal partial void Apply()` to write); on the subscription of its Log, `HandleMutations()` |

## Result types of the indexes

| Type | Members |
|---|---|
| `TextSearchResult<TView, TTag>`, `TextSearchBodies<TBody, TTag>` | A GBO sequence; `And`, `Or`, `AndNot` with value results |
| `ValueIndexResult<TView, TBody, TTag>` | A GBO sequence in key order; `Count()`, `Descending()`, `Bodies()`, `And`, `Or`, `AndNot` |
| A stage `WithX` | `Result`, `Count()`, `Descending()`, `Bodies()`, `And`, `Or`, `AndNot`, enumeration, the next conditions; other operators on `.Result` |
| `IndexSetResult<TView, TBody, TTag>` | A set without order; `Count()`, `Bodies()`, `And`, `Or`, `AndNot`, the operators |

## Runtime types

| Type | Namespace |
|---|---|
| `TransactionalMemoryStore` | `GhostBodyObject.Runtime.Physical` |
| `StoreAccessMode`, `TransactionConflictLevel`, `TransactionConflictKind`, `StoreLockPolicy` | `GhostBodyObject.Runtime.Physical.Constants` |
| `FlushPolicy` | `GhostBodyObject.Runtime.Physical.Segments` |
| `TransactionConflictException` | `GhostBodyObject.Runtime.Physical.Transactions` |
| `StoreDistributionEndPoint`, `StoreDistributionEndPointConfiguration`, `DistributionEndPoint`, `DistributionModel` | `GhostBodyObject.Runtime.Physical.Distribution` |
| `Option<T>`, `Option`, `GhostString`, `GhostSpan<T>`, `ReadOnlyGhostString`, `ReadOnlyGhostSpan<T>`, `GhostLockedException` | `GhostBodyObject.Runtime.Logical.Body` |
| `GhostUniquenessException` | `GhostBodyObject.Runtime.Logical.Index` |
| `GhostLease` | `GhostBodyObject.Runtime.Logical.Repository` |
| `LogStoreSubscription`, `GhostMutationPump`, `MutationBodyBase`, `MutationStatus`, `ReplayContext` | `GhostBodyObject.Runtime.Logical.Log` |
| The attributes, `IEntityCollection<T>`, `IGhostConverter<T, TStored>`, `RecordBag`, `RecordBagView`, `RecordBag<T1, …, T8>`, `RecordBagView<T1, …, T8>` | `GhostBodyObject.Runtime.Logical.Model` |

## The handlers of a subscription

| Delegate | Signature |
|---|---|
| `Handle<TView>(handler)` | `LogStoreSubscription (ulong transactionId, TView record)` |
| `Handle<TView, T1..T4>(state1.., handler)` | `LogStoreSubscription (ulong transactionId, TView record, T1 state1, ...)` |
| `OnTransactionStart`, `OnTransactionEnd`, `OnReplayEnd` (with 0 to 4 states) | `LogStoreSubscription (ulong transactionId, T1 state1, ...)` |
| Return | `LogStoreSubscription.Continue` or `Cancel` |
| `subscription.Run()`, `subscription.Completion` | Deliver, then follow; how it ended |
