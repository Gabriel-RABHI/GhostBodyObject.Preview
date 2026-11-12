# Tutorial T011: Composition

## Several assemblies, one model

[Tutorial T003](T003-Composition.md) gave an entity capabilities through components, and [Tutorial T004](T004-Edges.md) joined entities through edges. The entity did not need to know either of them. That independence also lets the code live in different assemblies: a Catalog declares a Product, Comments gives it a discussion, and Billing keeps the products of an invoice.

The links must still belong to one model. If each module opened an independent Repository, an invoice could not retain a Product in another one, and removing a Product could not cascade through its comments. [Tutorial T008](T008-ChainsAndDistribution.md) made several models commit together; here, the modules contribute to **the same model**, with the same rules of retention and deletion.

### Modularization

**Composition lets modules own their code separately while their data obeys one coherent set of link rules.** Catalog owns products, Comments owns discussions, and Billing owns invoices. Each module defines its part of the model and the operations that maintain it. The application combines those contributions into one Repository, so a relationship between modules has the same transactional meaning as a relationship within a module.

This separates two architectural decisions. A **module boundary** determines which code owns a responsibility and what it exposes to other code. A **Repository boundary** determines which entities participate in the same model and its link rules. Several modules can share that second boundary while keeping distinct implementations and public contracts.

As an application grows, unrestricted access between features makes changes harder to contain. If Catalog reads Billing's internal state, Billing edits Comments' data directly, and Comments calls Catalog's implementation details, each change can affect all three. A monolith in which every feature can query and modify every table has the same problem: the shared persistence layer becomes a route around the intended module boundaries. The difficulty comes from uncontrolled dependencies, rather than the mere fact that data is stored together.

Separate C# projects make the intended dependencies explicit. Each produces an assembly, references the contracts it needs, and controls the visibility of its types and members. A business module may span several assemblies: one can publish its shared entities and Commands, while others contain its implementation. The module can apply the same design internally. Billing might contain Invoicing, Accounting, Collections and Reporting, each with its own implementation and deliberately chosen contracts.

The aim is to keep each dependency small and explicit. A module must know enough to collaborate with another module; it should not need to know how that module represents every state or performs every operation.

### Reverse the dependency: the capability knows its anchor

Suppose an `Order` declares a collection of `OrderLine`. That declaration makes the Order type depend on the OrderLine type. It is a natural design when the two belong to the same responsibility. If another module should own the line-management capability, however, the declaration also makes the shared Order model depend on that module's detail.

GBO allows the module that adds a capability to declare the relationship. The shared entity becomes its **anchor**. Depending on the intended lifetime, the contributing module can:

- Declare an entity with a **reference to Order**. Order needs no property that names the referring type. A strong reference retains the Order; a weak reference follows its removal according to the usual weak-link rules. The referring entity's own ownership and lifetime are declared separately.
- Declare an **OrderLineList component** for Order, with a collection of line parts. The component adds the capability to an Order instance and owns its lines. Its lifetime follows its host, subject to the normal retention rules.
- Declare an **edge** between Order and another anchor. The edge carries the relationship and its values, while both endpoint types can remain unaware of its declaration. Its lifetime follows the edge rules introduced in Tutorial T004.

These choices encode different ownership and deletion rules. Choose the relationship that expresses the business meaning, then place its declaration in the module responsible for it.

In all three cases, the dependency points from the contributing module to the assembly that publishes Order. The Order assembly has no reference back to that implementation. It can compile before the capability exists, and its source does not change when another module adds one. The same pattern appears in this tutorial: Comments references Catalog and declares CommentThread; Catalog's Product declaration never mentions comments.

**The relationship type is declared at compilation; its instances are created during transactions.** A declared component can be attached to a particular Order when that Order needs the capability. References and edges can connect instances as a workflow progresses. Adding a new kind of component or edge requires compiling its module and the composition that selects it. Runtime flexibility concerns the instances and their links, within the compiled model.

The generated navigation API follows the same dependency direction. `product.CommentThread` is declared in Comments, even though it reads like a member of Product at the call site. Catalog code, compiled without Comments, cannot name CommentThread or use that accessor through its typed API. Code that references Comments and has the required access can use it. This gives the capability an explicit owner without adding that owner's types to the anchor's declaration.

### Design the contracts around shared anchors

Start by identifying the identities that several responsibilities need to recognize: a user account, a customer, a product, an order or an invoice. The module that owns an anchor's definition publishes the identity and the model surface required by its consumers. Other modules attach their own meaning to that same entity through their own references, components and edges.

For example, Sales may attach commercial information to a Product, while Accounting attaches a tax classification and Comments attaches a discussion. They agree on **which Product** they are describing. Each module owns the additional state and rules that give that Product meaning within its responsibility. Product does not need fields for every capability that another module might add later.

Anchors give a workflow durable points of reference. A customer may exist before an order, and an order before an invoice. The valid links at each stage depend on the business rules: a draft may allow an optional customer, while confirmation requires one. An anchor is an entity with an identity; an **invariant** is a rule about valid states or transitions. The developer expresses those rules through the model declarations and business code. The resulting entity graph need not be a single hierarchy, and sharing an anchor does not require sharing every module's internal representation.

The public contract should also distinguish **what another module may address** from **what it may request**. A published entity or Ref lets the caller identify an anchor and use the API that is exposed. A public Command describes an intention for the owning module to execute. The caller constructs and submits that Command; the composed Log routes it to the owning Mutation, whose `Decide` checks the intention and whose `Apply` performs the recorded decision. The caller can request the operation without referencing the private handler or reproducing its workflow.

Publishing an entity type also publishes its accessible members. If callers must request a change through a Command, design the visibility accordingly: a public writable property remains callable by code that can access it. The compiler enforces the contract that the assemblies actually expose.

This leads to a practical design sequence:

1. Give each business responsibility an owner, including the definitions of its shared anchors.
2. Publish the anchors, values and Commands that other responsibilities need.
3. Keep the remaining state and handlers in the implementation assemblies.
4. Make consuming modules reference those contracts, with an acyclic project dependency graph.
5. Let the application reference the implementations and compose their fragments.

Apply the same sequence inside a large module. A separate contract assembly is useful when it keeps consumers independent of implementation assemblies; the smaller example in this tutorial places the public anchors and Command contracts together in Catalog.

**Access follows the actual assembly configuration.** GBO's generated infrastructure requires explicit `InternalsVisibleTo` grants to the composition and to dependent modules that need it. A friend assembly can access all of the granting assembly's internal members. In this tutorial, Catalog grants that access to Comments, Billing and the application; Comments and Billing grant it to the application. These grants are part of the dependency design. The composition is trusted to see the contributions it assembles, and each module controls which other assemblies receive that broader access.

### Compose the entity graph, preserve the code boundaries

The source dependency graph and the entity graph serve different purposes:

| Graph | What a link means | Who uses it |
| ----- | ----------------- | ----------- |
| Assembly dependencies | This code can use the types and members made accessible by that assembly | The compiler, to check code access |
| Entity relationships | This instance references, owns, extends or connects to another instance | GBO, to maintain the declared relationships and lifetimes |

The runtime must know every relationship in the composed model, including relationships that an anchor's own code cannot name. Suppose Billing's private Invoice strongly references Catalog's Product. Catalog code can request the Product's removal, but GBO sees the invoice's retention and refuses it. If the Product has a Comments component and no retention prevents deletion, its removal cascades through that component and its parts. Catalog needs no dependency on either implementation for those rules to hold.

**Encapsulation is enforced through code access; consistency is enforced over the complete composed model.** The link engine checks the contributions together. The transaction publishes their changes together or rolls them back together. A business callback remains in its owning module, even when another module initiates the cascade that invokes it. Application-specific rules still belong in the declarations and business code that define them.

Fragments connect these two graphs. A Repository fragment declares a contribution to the entity model and generates its local APIs in its owning assembly. A Log fragment does the corresponding work for Commands and Mutations. Neither opens an independent Store or supplies an independent commit. The application selects compiled Repository fragments into a complete Repository, and compiled Log fragments into a complete Log. It then connects those models through the transaction chain used in the following lessons.

The modules retain their responsibilities, their local code and their chosen contracts. Their Repository contributions form **one entity model with one set of link rules**. This is what makes modular composition fundamental: the application can divide its implementation into independently compiled contributions while preserving the relationships and transactional consistency that its business data requires.

## What's in this tutorial

This tutorial shows the three parts of that composition:

- **Compile locally.** Each module declares a fragment and generates its own Bodies, Views, Refs, components, edges and business code. It compiles before the application that will use it.
- **Compose explicitly.** The application selects the fragments. GBO gathers their types and declared dependencies into one Repository, which owns the Store and opens the transaction.
- **Call through contracts.** Public Commands compile before their private handlers. The application composes their Log, and a sub-Mutation participates in the transaction already open.

The examples are tests in `GhostBodyObject.Tutorials.T011`, backed by three module projects. The seven lesson files share a partial test class and contain seventeen test cases. Read them here, run them, then change them and see what breaks. The tutorial follows Tutorials T001 to T010: the Stores, the transactions, the links, the indexes, the Records, the Log, the Views and the Bodies are used here without being explained again.

## What you will build

A small commerce application. Catalog owns the Product and UserAccount anchors. Comments attaches discussions, ratings, friendships and recommendations to them. Billing owns invoices, their parts and their archives. The application composes these contributions into `CommerceRepository`, their Commands and Mutations into `CommerceLog`, and a separate Read model into `CommerceReadRepository`.

Four projects, each producing an assembly of its own:

| Project | What it owns | The application projects it references |
| ------- | ------------ | -------------------------------------- |
| `GhostBodyObject.Tutorials.T011.Catalog` | Public anchors, public Command contracts, an Annotation Record and a Read anchor | None |
| `GhostBodyObject.Tutorials.T011.Comments` | Private components, parts and edges; indexes and a system; the comment handler and its Read component | Catalog |
| `GhostBodyObject.Tutorials.T011.Billing` | Private invoices, parts and shared objects; the purchase handler | Catalog |
| `GhostBodyObject.Tutorials.T011` | The composed Repositories and Log, and the tests | Comments and Billing |

Every project also references the Runtime and uses the Generator as an analyzer. Catalog knows neither Comments nor Billing. Billing knows the public intention to publish a comment, declared in Catalog, but has no reference to Comments. Only the application knows all the modules.

Run the tests from the root of the repository:

```powershell
dotnet test GhostBodyObject.Tutorials.T011/GhostBodyObject.Tutorials.T011.csproj
```

The declarations and calls that connect the assemblies, and the lesson that shows each one:

| Declaration or call | What it does | Lessons |
| ------------------- | ------------ | ------- |
| `[GhostRepository(Fragment = true)]` | Generates a module's local APIs, without an independent Store or transaction | 1 |
| `[IncludeModule(typeof(MCatalogModule))]` | Selects a compiled fragment and its declared dependencies, each once | 1 |
| `InternalsVisibleTo` | Gives the composition access to the generated infrastructure and internal contributions | 1 |
| `new CommerceRepository(store)` | Binds the complete composed model to a Store | 1 |
| `product.CommentThread`, `user.NewRating(product)` | Uses components and edges supplied by another assembly | 2 |
| `[GhostLog(Fragment = true)]` | Compiles contracts or handlers before the final Log | 3 |
| `pump.Post(command)` | Receives a public Command through this pump's composed Log | 3 |
| `command.Submit()` | Decides and applies a sub-Mutation in the transaction already open | 3 |
| `HandleMutations()` | Replays the recorded decisions through their `Apply` methods | 3 |
| `ModuleTransaction.Current` | Observes the transaction bound to a fragment in the current execution context | 1, 4, 7 |
| `repository.OpenTransaction(lease)` | Opens a lease transaction with the same fragment bindings | 5 |
| Local indexes, collections, Records and `repository.Feed()` | Work against the composed model | 6 |
| `write.Chain(read)`, `read.Chain(log)` | Connects the separately composed roles into one transactional chain | 7 |

## Part 1: A model across assemblies

### Lesson 1: Compile the modules, then compose the Repository

A Product must exist before Comments can extend it. Catalog declares the anchors and a fragment, in the namespace `Tutorials.T011.Catalog`:

```csharp
namespace Tutorials.T011.Catalog;

[GhostRepository(Fragment = true)]
public interface MCatalogModule;

[Entity(1101, 1, 1), Root, Removable, Enumerable, WithIdentity]
public interface MProduct
{
    [Property(1)] string Name { get; set; }
}

[Entity(1102, 1, 1), Root, Removable, Enumerable, WithIdentity]
public interface MUserAccount
{
    [Property(1)] string Name { get; set; }
}
```

The generator discovers the declarations in the fragment's own namespace. It writes `Product`, `ProductView`, `ProductRef`, the corresponding UserAccount types, and their local collections into Catalog's assembly. The fragment has no model number and opens no Store. Its types will be bound by the Repository that includes it.

Use `[IncludeNamespace("...", SubNamespaces = true)]` or the existing include attributes to select additional namespaces or declarations. Keep Repository and Log contributions in separate namespaces, so each fragment discovers the declarations of its own role.

**The identifiers are fixed.** A fragment and its composition use explicit type, field and index numbers. Adding a module or changing the order of selection must not renumber data already stored. Here, Catalog uses type identifiers 1101 onwards, Comments 1201 onwards and Billing 1301 onwards. These ranges are a convention of this application; GBO checks that the complete model has no collision.

Comments references Catalog and declares its dependency on the Catalog fragment. A discussion belongs to either a Product or a UserAccount:

```csharp
namespace Tutorials.T011.Comments;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogModule))]
public interface MCommentModule;

[Entity(1201, 1, 2), ComponentFor(nameof(MProduct)), ComponentFor(nameof(MUserAccount)), Enumerable]
internal interface CCommentThread
{
    [Property(1)] IEntityCollection<MComment> Comments { get; }
    [Property(2)] RecordBag<RAnnotation> Notes { get; set; }
}

[Entity(1202, 1, 3), Enumerable]
internal interface MComment
{
    [Property(1), TextIndexed(nameof(MCommentWords))] string Text { get; set; }
    [Property(2), Strong] MUserAccount Author { get; set; }
    [Property(3), Weak] Option<MProduct> Mention { get; set; }
    [OwnedFrom] CCommentThread Thread { get; }
}
```

`RAnnotation` is a Catalog Record, and `MCommentWords` is the local text index; Lesson 6 uses them. The component and its parts stay internal to Comments. The public operation that adds a comment is ordinary business code compiled in that assembly:

```csharp
public static class Discussion
{
    public static void Add(Product product, UserAccount author, string text)
        => product.CommentThread.GetOrNew().Comments.New(author).Text = text;

    public static int Count(Product product)
        => product.CommentThread.TryGet(out var thread) ? thread.Comments.Count() : 0;
}
```

`product.CommentThread` is an extension property generated in Comments. Catalog's Product is already compiled: no partial class crosses the assembly boundary, and Catalog has no dependency on the capability added to it.

Billing also selects Catalog. Its invoice keeps a customer and a collection of products by strong references; its suggestions and favorite are weak:

```csharp
namespace Tutorials.T011.Billing;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogModule))]
public interface MBillingModule;

[Entity(1301, 1, 7), Root, Removable, Enumerable]
internal interface MInvoice
{
    [Property(1), Strong] MUserAccount Customer { get; set; }
    [Property(2), Strong] IEntityCollection<MProduct> Products { get; }
    [Property(3), Weak] IEntityCollection<MProduct> Suggestions { get; }
    [Property(4), Weak] Option<MProduct> Favorite { get; set; }
    [Property(5)] IEntityCollection<MInvoiceLine> Lines { get; }
    [Property(6)] MInvoiceHeader Heading { get; }
    [Property(7)] Option<MInvoiceFooter> Footer { get; }
}
```

Lesson 2 uses the line, heading and footer types. For now, the important relation is `Invoice.Products`: a private type in Billing retains public entities in Catalog.

**The module grants access to the assemblies that compose or depend on it.** Catalog's project contains:

```xml
<InternalsVisibleTo Include="GhostBodyObject.Tutorials.T011" />
<InternalsVisibleTo Include="GhostBodyObject.Tutorials.T011.Comments" />
<InternalsVisibleTo Include="GhostBodyObject.Tutorials.T011.Billing" />
```

Comments and Billing each grant friend access to `GhostBodyObject.Tutorials.T011`. Even a module whose model interfaces are all public has generated internal infrastructure that the composition needs. Friend access is an assembly-level C# permission, not a restriction to the model types, and it is not transitive.

The application now selects the two modules:

```csharp
namespace Tutorials.T011;

[GhostRepository(110), IncludeModule(typeof(MCommentModule)), IncludeModule(typeof(MBillingModule))]
internal interface MCommerceRepository;
```

Both modules depend on Catalog. It participates once. The application does not list the private entities one by one: adding a declaration to a fragment's namespace updates the generated contribution. A project reference alone does not install a fragment; `IncludeModule` is the selection.

The first test opens the composed Repository and uses all three modules:

```csharp
[Fact]
public void CompileModulesSeparatelyAndShareOneTransaction()
{
    Assert.NotEqual(typeof(Product).Assembly, typeof(Discussion).Assembly);
    Assert.NotEqual(typeof(Product).Assembly, typeof(Invoice).Assembly);
    Assert.NotEqual(typeof(Invoice).Assembly, typeof(CommerceRepository).Assembly);

    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed", StoreAccessMode.ReadWrite);
    using var repository = new CommerceRepository(store);
    using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var product = new Product(ProductRef.New()) { Name = "Book" };
        var user = new UserAccount(UserAccountRef.New()) { Name = "Ada" };
        Discussion.Add(product, user, "Useful");
        var invoice = new Invoice(user);
        invoice.Products.Add(product);

        Assert.Same(tx, CatalogModuleTransaction.Current);
        Assert.Same(tx, CommentModuleTransaction.Current);
        Assert.Same(tx, BillingModuleTransaction.Current);
        Assert.Throws<InvalidOperationException>(product.Remove);
        Assert.Throws<InvalidOperationException>(user.Remove);
        tx.Commit();
    }
    using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        var product = ProductCollection.Single();
        Assert.Equal(1, Discussion.Count(product));
        Assert.Equal("Useful", product.ToView().CommentThread.Get().Comments.Single().Text);
        Assert.Equal("Ada", InvoiceCollection.Single().Customer.Name);
    }
    Assert.Null(CatalogModuleTransaction.Current);
}
```

The invoice retains the Product and the UserAccount, and the comment also retains its author. Their `Remove()` calls are refused by the same link engine, although the declarations and calls came from different assemblies. The `Current` assertions observe the binding for the test; business code does not install or clear it.

The full declarations are in [Catalog](../../GhostBodyObject.Tutorials.T011.Catalog/Model.cs), [Comments](../../GhostBodyObject.Tutorials.T011.Comments/Model.cs) and [Billing](../../GhostBodyObject.Tutorials.T011.Billing/Model.cs). The executable test is [Lesson01ComposeAssemblies.cs](../../GhostBodyObject.Tutorials.T011/Lesson01ComposeAssemblies.cs).

### Lesson 2: Keep the rules of every link

A boundary between assemblies changes where the code lives. It does not change what retains an entity, what owns a part or what disappears with an endpoint. The tests of this lesson exercise those rules on the complete model.

**A component can belong to several types of anchors.** The same CommentThread attaches to a UserAccount. Its comment refers strongly to that user, but the reference is inside the set being removed: it leaves with that set and does not retain it from outside.

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("component-hosts", StoreAccessMode.ReadWrite);
using var repository = new CommerceRepository(store);
using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);

var user = new UserAccount(UserAccountRef.New());
var thread = user.CommentThread.New();
thread.Comments.New(user).Text = "Own profile";
Assert.Same(user, thread.UserAccount.Get());
Assert.False(thread.Product.Any());
Assert.Empty(DiscussedProductSystemCollection);

user.Remove();
Assert.Empty(UserAccountCollection);
Assert.Empty(CommentThreadCollection);
Assert.Empty(CommentCollection);
Assert.Equal("Own profile", DeletionNoticeCollection.Single().Text);
tx.Commit();
```

The Product system is empty because this component belongs to a UserAccount. The DeletionNotice comes from a callback in Comments, which we will examine below.

**An edge belongs to its endpoints.** Comments declares three forms:

```csharp
[Entity(1203, 1, 1), Edge(nameof(MUserAccount), nameof(MProduct)), Removable, Enumerable]
internal interface ERating
{
    [Property(1)] int Stars { get; set; }
}

[Entity(1204, 1, 0), Edge(nameof(MUserAccount), nameof(MUserAccount), Symmetric = true), Removable, Enumerable]
internal interface EFriendship;

[Entity(1205, 1, 1), Edge(nameof(MProduct), nameof(MProduct), Multiple = true), Removable, Enumerable]
internal interface ERecommendation
{
    [Property(1)] string Reason { get; set; }
}
```

A user has one Rating per Product. A Friendship is the same pair in either direction. Several Recommendations can connect the same two Products. Their generated accessors belong to Comments; their endpoints belong to Catalog.

Inside a fresh transaction, the test checks each form:

```csharp
var ada = new UserAccount(UserAccountRef.New());
var bob = new UserAccount(UserAccountRef.New());
var product = new Product(ProductRef.New());
var other = new Product(ProductRef.New());

var rating = ada.NewRating(product);
Assert.Throws<InvalidOperationException>(() => ada.NewRating(product));
Assert.Same(rating, ada.RatingEdges.GetOrNew(product));

var friendship = ada.NewFriendship(bob);
Assert.Same(friendship, bob.FriendshipEdges.Edge(ada).Get());
Assert.Throws<InvalidOperationException>(() => bob.NewFriendship(ada));

product.NewRecommendation(other).Reason = "First";
product.NewRecommendation(other).Reason = "Second";
Assert.Equal(2, RecommendationCollection.Count());

Assert.True(ada.RatingEdges.Remove(product));
other.Remove();
Assert.Empty(RecommendationCollection);
bob.Remove();
Assert.Empty(FriendshipCollection);
Assert.Single(UserAccountCollection);
```

Removing an endpoint removes its edges, not the other endpoint. Unique pairs remain unique, and symmetric navigation finds the same edge.

**Weak references follow the cascade.** Comments supplies the callback that records which comments were deleted:

```csharp
[Entity(1207, 1, 1), Root, Enumerable]
internal interface MDeletionNotice
{
    [Property(1)] string Text { get; set; }
}

internal partial class Comment
{
    partial void OnDeleting()
    {
        if (Text == "protected")
            throw new InvalidOperationException("Moderation refused deletion.");
        new DeletionNotice { Text = Text };
    }
}
```

The callback remains in its owning assembly. A deletion initiated through Catalog still calls it. This transaction creates a discussion and a rating, and gives an invoice two weak references to the Product:

```csharp
var product = new Product(ProductRef.New());
var user = new UserAccount(UserAccountRef.New());
Discussion.Add(product, user, "Review");
var comment = product.CommentThread.Get().Comments.Single();
comment.Mention = product;

var invoice = new Invoice(user) { Favorite = product };
invoice.Suggestions.Add(product);
user.NewRating(product).Stars = 5;
Assert.Equal(5, user.ToView().RatingEdges.Single().Stars);

product.Remove();
Assert.False(invoice.Favorite.Any());
Assert.Empty(invoice.Suggestions);
Assert.Empty(CommentCollection);
Assert.Empty(CommentThreadCollection);
Assert.Empty(RatingCollection);
Assert.Equal("Review", DeletionNoticeCollection.Single().Text);
Assert.Single(UserAccountCollection);
```

The invoice has no strong Product reference in this example. Its weak favorite and suggestions are cleared. The Product's component, its parts and its rating disappear. The author remains, and the deletion notice is written in that same transaction.

**A reference to a component or an edge retains its host.** A moderation entry can keep either of them:

```csharp
[Entity(1206, 1, 2), Root, Removable, Enumerable]
internal interface MModeration
{
    [Property(1), Strong] Option<CCommentThread> Thread { get; set; }
    [Property(2), Strong] Option<ERating> Rating { get; set; }
}
```

The test removes the retentions one at a time:

```csharp
var product = new Product(ProductRef.New());
var user = new UserAccount(UserAccountRef.New());
var thread = product.CommentThread.New();
var rating = user.NewRating(product);
var moderation = new Moderation { Thread = thread, Rating = rating };

Assert.Throws<InvalidOperationException>(product.Remove);
Assert.Throws<InvalidOperationException>(() => product.CommentThread.Remove());
Assert.Throws<InvalidOperationException>(rating.Remove);

moderation.Thread = default;
Assert.Throws<InvalidOperationException>(product.Remove);
moderation.Rating = default;
product.Remove();
Assert.Empty(RatingCollection);
Assert.Empty(CommentThreadCollection);
```

Keeping a component keeps the entity that carries it. Keeping an edge keeps its endpoints. Catalog cannot decide that its Product is removable by inspecting only its own declarations: the complete model decides.

**Parts and shared objects keep different rules.** Billing declares an orphanable line, an archive that can keep it, a shared label and the invoice's required and optional parts:

```csharp
[Entity(1302, 1, 1), Enumerable, Orphanable]
internal interface MInvoiceLine
{
    [Property(1)] int Quantity { get; set; }
    [OwnedFrom] Option<MInvoice> Invoice { get; }
    [ReverseStrongReference(nameof(MArchive.Line))] IEntityCollection<MArchive> Archives { get; }
}

[Entity(1303, 1, 2), Root, Removable, Enumerable]
internal interface MArchive
{
    [Property(1), Strong] Option<MInvoiceLine> Line { get; set; }
    [Property(2), Strong] Option<MLabel> Label { get; set; }
}

[Entity(1304, 1, 1), Enumerable]
internal interface MLabel
{
    [Property(1)] string Text { get; set; }
}

[Entity(1305, 1, 1)]
internal interface MInvoiceHeader
{
    [Property(1)] string Reference { get; set; }
    [OwnedFrom] MInvoice Invoice { get; }
}

[Entity(1306, 1, 1), Enumerable]
internal interface MInvoiceFooter
{
    [Property(1)] string Text { get; set; }
}
```

The required heading is born with its invoice. The optional footer is born on demand. A line moves between invoices, and its inverse reports the archive that keeps it:

```csharp
var user = new UserAccount(UserAccountRef.New());
var first = new Invoice(user);
var second = new Invoice(user);
Assert.Same(first, first.Heading.Invoice);
first.Heading.Reference = "A";
first.Footer.New().Text = "Terms";

var line = first.Lines.New();
line.Quantity = 2;
line.MoveTo(second.Lines);
Assert.Empty(first.Lines);
Assert.Same(second, line.Invoice.Get());

var archive = new Archive { Line = line };
Assert.Same(archive, line.Archives.Single());
```

Continue in the same transaction. A shared label lives while either archive keeps it. An orphanable line survives the removal of its invoice while the archive keeps it:

```csharp
var label = archive.Label.New();
label.Text = "Keep";
var another = new Archive();
another.Label.Set(label);
archive.Label.Clear();
Assert.Single(LabelCollection);
another.Label.Clear();
Assert.Empty(LabelCollection);

first.Remove();
Assert.Empty(InvoiceFooterCollection);
second.Remove();
Assert.False(line.Invoice.Any());
Assert.Single(InvoiceLineCollection);
archive.Line = default;
Assert.Empty(InvoiceLineCollection);
```

Nothing in these calls names the composition mechanism. They are the local APIs of [Tutorial T002](T002-Relationships.md), operating on the types installed by the composed Repository.

All five tests are in [Lesson02LinksAndLifetime.cs](../../GhostBodyObject.Tutorials.T011/Lesson02LinksAndLifetime.cs). Each one opens its own Store and transaction; the short examples above are separate test scenarios, not operations to append to a single shared database.

## Part 2: Transactions across modules

### Lesson 3: Publish intentions, keep handlers private

Billing wants to publish a comment when a purchase is accepted. It should know the intention it submits, without knowing the CommentPublication handler or its private decision. Catalog declares the public contracts in a separate namespace, `Tutorials.T011.Catalog.Contracts`:

```csharp
namespace Tutorials.T011.Catalog.Contracts;

[GhostLog(Fragment = true)]
public interface MCommerceContracts;

[Record(1401, 1, 3), Command]
public interface RPublishComment
{
    [Property(1)] ProductRef Product { get; set; }
    [Property(2)] UserAccountRef Author { get; set; }
    [Property(3)] string Text { get; set; }
}

[Record(1402, 1, 3), Command]
public interface RCheckout
{
    [Property(1)] ProductRef Product { get; set; }
    [Property(2)] UserAccountRef Customer { get; set; }
    [Property(3)] bool FailAfterComment { get; set; }
}
```

These are detached Records: the caller composes their values before the transaction that will execute them. The entity references travel in the intention, as in [Tutorial T007](T007-TheLog.md). `FailAfterComment` is a switch used by the test to refuse the parent after its child has run.

The contracts compile without their handlers. Comments declares a Log fragment that selects them and supplies the private Mutation:

```csharp
namespace Tutorials.T011.Comments.Mutations;

[GhostLog(Fragment = true), IncludeModule(typeof(MCommerceContracts))]
public interface MCommentMutations;

[Record(1411, 1, 2), Mutation]
internal interface RCommentPublication
{
    [Property(1)] RPublishComment Command { get; set; }
    [Property(2), Private] string AcceptedText { get; set; }
}

internal partial class CommentPublication
{
    private partial void Decide(PublishComment command)
    {
        if (string.IsNullOrWhiteSpace(command.Text))
            throw new ArgumentException("A comment needs text.");
        AcceptedText = command.Text.Trim();
    }
}

internal readonly partial struct CommentPublicationView
{
    internal partial void Apply()
        => Discussion.Add(Command.Product.Resolve(), Command.Author.Resolve(), AcceptedText);
}
```

The decision validates the intention and keeps its accepted text. `Apply` uses that decision to change the model. The same `Apply` will run during replay; it does not need to decide again whether the original text was acceptable.

Billing supplies the purchase handler in its own assembly. Its `Decide` submits the public comment intention:

```csharp
namespace Tutorials.T011.Billing.Mutations;

[GhostLog(Fragment = true), IncludeModule(typeof(MCommerceContracts))]
public interface MBillingMutations;

[Record(1421, 1, 1), Mutation]
internal interface RPurchase
{
    [Property(1)] RCheckout Command { get; set; }
}

internal partial class Purchase
{
    private partial void Decide(Checkout command)
    {
        new PublishComment
        {
            Product = command.Product,
            Author = command.Customer,
            Text = "Purchased"
        }.Submit();

        if (command.FailAfterComment)
            throw new InvalidOperationException("Purchase refused after the sub-Mutation.");
    }
}

internal readonly partial struct PurchaseView
{
    internal partial void Apply()
    {
        var invoice = new Invoice(Command.Customer.Resolve());
        invoice.Products.Add(Command.Product.Resolve());
    }
}
```

Billing references Catalog, where `PublishComment` is public. It has no reference to Comments, where the corresponding handler is internal. The application supplies the missing composition:

```csharp
[GhostLog(111), IncludeModule(typeof(MBillingMutations)), IncludeModule(typeof(MCommentMutations))]
internal interface MCommerceLog;
```

The composed Log checks that every Command has exactly one Mutation and generates the dispatch between them. The contract fragment is a dependency of both handler fragments and participates once.

**Post through the instance that will execute the Command.** The test chains the Repository and the Log, creates the anchors, then posts a Checkout to the pump:

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("mutation-state", StoreAccessMode.ReadWrite);
using var journal = store.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("mutation-log", StoreAccessMode.ReadWrite));
using var repository = new CommerceRepository(store);
using var log = new CommerceLog(journal);
repository.Chain(log);

var product = ProductRef.New();
var customer = UserAccountRef.New();
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    new Product(product);
    new UserAccount(customer);
    tx.Commit();
}

using (var pump = new GhostMutationPump(repository))
{
    var command = new Checkout { Product = product, Customer = customer };
    var mutation = pump.Post(command);
    Assert.Equal(MutationStatus.Accepted, pump.Wait(mutation));
    Assert.IsType<Purchase>(mutation);
    Assert.Throws<InvalidOperationException>(() => command.FailAfterComment = true);

    var rejected = pump.Post(new Checkout
    {
        Product = product,
        Customer = customer,
        FailAfterComment = true
    });
    Assert.Equal(MutationStatus.Failed, pump.Wait(rejected));

    var empty = pump.Post(new PublishComment { Product = product, Author = customer, Text = " " });
    Assert.Equal(MutationStatus.Failed, pump.Wait(empty));
}

using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
{
    Assert.Single(InvoiceCollection);
    Assert.Single(CommentCollection);
}
```

The first purchase succeeds. The second creates its provisional comment, then fails: neither its comment nor its invoice remains. The empty comment fails its own decision. Only the successful purchase and its child survive.

The pump resolves the handler through its Repository chain. There is no process-wide registration that sends every Checkout to one Store. Inside `Decide`, `Submit()` uses the Log bound to the transaction already open; the child cannot commit independently.

**The child's decision precedes the parent's decision in the journal.** The child decides, is appended, applies, then returns to the parent. The parent can see those provisional effects. The journal records their successful application order:

```csharp
var order = new List<string>();
log.SubscribeFrom(0)
    .Handle<CommentPublicationView, List<string>>(order, static (_, _, seen) =>
    {
        seen.Add("comment");
        return LogStoreSubscription.Continue;
    })
    .Handle<PurchaseView, List<string>>(order, static (_, _, seen) =>
    {
        seen.Add("purchase");
        return LogStoreSubscription.Continue;
    })
    .OnReplayEnd(static _ => LogStoreSubscription.Cancel).Run();
Assert.Equal(["comment", "purchase"], order);
```

The failed parent left no child entry behind. Transactional failure covers the model changes and the Log entries together.

**Replay only the recorded decisions.** Continue the test with a fresh Repository and the same anchor identities. The anchors were created before the logged operations, so this test seeds them before replay:

```csharp
using var rebuilt = TransactionalMemoryStore.NewInMemoryRepositoryStore("mutation-rebuilt", StoreAccessMode.ReadWrite);
using var target = new CommerceRepository(rebuilt);
using (var tx = target.OpenTransaction(StoreAccessMode.ReadWrite))
{
    new Product(product);
    new UserAccount(customer);
    tx.Commit();
}
using (var tx = target.OpenTransaction(StoreAccessMode.ReadWrite, replay: true))
{
    log.SubscribeFrom(0)
        .HandleMutations()
        .OnReplayEnd(static _ => LogStoreSubscription.Cancel).Run();
    tx.Commit();
}
using (target.OpenTransaction(StoreAccessMode.ReadOnly))
{
    Assert.Single(InvoiceCollection);
    Assert.Equal("Purchased", CommentCollection.Single().Text);
}
```

`HandleMutations()` calls each recorded `Apply` in journal order. It never runs `Decide`, so the purchase does not submit the comment again. The caller also cannot submit a Command from an `Apply`: submission is refused under `ReplayContext`.

The second test checks invalid contexts separately: `PublishComment.Submit()` without an open transaction is refused, a Log transaction cannot nest, and an Annotation handler cannot be installed on the Commerce Log because that Record belongs to the Repository model.

The contracts are in [Contracts.cs](../../GhostBodyObject.Tutorials.T011.Catalog/Contracts.cs), the handlers in [Comments](../../GhostBodyObject.Tutorials.T011.Comments/Mutations.cs) and [Billing](../../GhostBodyObject.Tutorials.T011.Billing/Mutations.cs), and the tests in [Lesson03MutationsAndReplay.cs](../../GhostBodyObject.Tutorials.T011/Lesson03MutationsAndReplay.cs).

### Lesson 4: Follow the context, roll back the whole model

A compiled fragment is reusable on several Stores. What selects the Store at an instruction is the transaction of the current execution context, not the assembly and not a single global instance.

The first test opens two instances of the same composed Repository, then writes in two independent asynchronous contexts:

```csharp
[Fact]
public async Task BindEachExecutionContextToItsOwnStoreAcrossAwait()
{
    using var first = TransactionalMemoryStore.NewInMemoryRepositoryStore("first-context", StoreAccessMode.ReadWrite);
    using var second = TransactionalMemoryStore.NewInMemoryRepositoryStore("second-context", StoreAccessMode.ReadWrite);
    using var a = new CommerceRepository(first);
    using var b = new CommerceRepository(second);

    await Task.WhenAll(Write(a, "First"), Write(b, "Second"));
    using (a.OpenTransaction(StoreAccessMode.ReadOnly))
    {
        Assert.Equal("First", ProductCollection.Single().Name);
        Assert.Throws<InvalidOperationException>(() => b.OpenTransaction(StoreAccessMode.ReadOnly));
    }
    using (b.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Equal("Second", ProductCollection.Single().Name);
    Assert.Throws<InvalidOperationException>(() => new Product(ProductRef.New()));

    static async Task Write(CommerceRepository repository, string name)
    {
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        await Task.Yield();
        Assert.Same(tx, CatalogModuleTransaction.Current);
        new Product(ProductRef.New()) { Name = name };
        tx.Commit();
    }
}
```

After `await`, the local APIs still use the correct transaction. After disposal, the binding is gone. Within one execution context, two open transactions cannot share the same fragment, even when their compositions or Store instances differ. Close one before opening the other.

**A callback failure rolls back every contribution.** Lesson 2's `OnDeleting` refuses a comment whose text is `"protected"`. This test also changes a Catalog value before attempting the cascade:

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("failed-hook", StoreAccessMode.ReadWrite);
using var repository = new CommerceRepository(store);
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    var product = new Product(ProductRef.New());
    var user = new UserAccount(UserAccountRef.New());
    Discussion.Add(product, user, "protected");
    tx.Commit();
}
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    ProductCollection.Single().Name = "Uncommitted";
    Assert.Throws<InvalidOperationException>(ProductCollection.Single().Remove);
    Assert.Throws<InvalidOperationException>(tx.Commit);
}
using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
{
    Assert.NotEqual("Uncommitted", ProductCollection.Single().Name);
    Assert.Equal("protected", CommentCollection.Single().Text);
}
```

The callback failure condemns the transaction. Catching its exception does not make the partial changes committable. The Product name and the protected comment both retain their committed values.

**A reopen reads the complete persisted model.** The file-based test writes a Product, its discussion and a rating, then abandons a transaction that removes the Product:

```csharp
// directory is a temporary directory owned by the test.
using (var store = TransactionalMemoryStore.NewFileRepositoryStore(directory, "commerce", StoreAccessMode.ReadWrite))
using (var repository = new CommerceRepository(store))
{
    using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        var product = new Product(ProductRef.New()) { Name = "Saved" };
        var user = new UserAccount(UserAccountRef.New());
        Discussion.Add(product, user, "Persisted");
        user.NewRating(product).Stars = 4;
        tx.Commit();
    }
    using (repository.OpenTransaction(StoreAccessMode.ReadWrite))
        ProductCollection.Single().Remove(); // No commit.
}

using var recovered = TransactionalMemoryStore.NewFileRepositoryStore(directory, "commerce", StoreAccessMode.ReadWrite);
using var rebound = new CommerceRepository(recovered);
using var read = rebound.OpenTransaction(StoreAccessMode.ReadOnly);
Assert.Equal("Saved", ProductCollection.Single().Name);
Assert.Equal("Persisted", CommentCollection.Single().Text);
Assert.Equal(4, RatingCollection.Single().Stars);
Assert.Empty(DeletionNoticeCollection);
```

The committed links survive the reopen. The abandoned cascade and its deletion notice do not. This checks reopening and transaction abandonment; it does not simulate a power cut. The full test owns the temporary directory and releases its memory mappings before deleting it.

The three tests are in [Lesson04IsolationAndRecovery.cs](../../GhostBodyObject.Tutorials.T011/Lesson04IsolationAndRecovery.cs).

### Lesson 5: Check concurrent links on the global model

A deletion can be valid in its snapshot and become invalid before its commit. Another transaction may create an invoice that retains the Product in the meantime. The complete model must check the final state, regardless of the assembly that created each link.

The test coordinates two transactions explicitly. One provisionally removes the Product; the other commits a new strong reference to it:

```csharp
[Fact]
public async Task RefuseADeletionWhenAnotherModuleRetainsItsTargetAfterTheSnapshot()
{
    using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("concurrent-retention", StoreAccessMode.ReadWrite);
    using var repository = new CommerceRepository(store);
    using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
    {
        new Product(ProductRef.New());
        new UserAccount(UserAccountRef.New());
        tx.Commit();
    }

    var removed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var deletion = Task.Run(async () =>
    {
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        ProductCollection.Single().Remove();
        removed.SetResult();
        await held.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Throws<TransactionConflictException>(tx.Commit);
    });
    await removed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    try
    {
        using var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite);
        new Invoice(UserAccountCollection.Single()).Products.Add(ProductCollection.Single());
        tx.Commit();
    }
    finally { held.SetResult(); }
    await deletion;

    using (repository.OpenTransaction(StoreAccessMode.ReadOnly))
        Assert.Same(ProductCollection.Single(), InvoiceCollection.Single().Products.Single());
}
```

The deletion fails at commit. The Product remains, and the invoice points to it. Splitting the declarations across DLLs did not weaken the check.

**Birth slots also conflict.** A second test runs the same race twice, once for a component and once for an edge:

| Both transactions create | The shared constraint | After the first commit |
| ------------------------ | --------------------- | ---------------------- |
| `product.CommentThread.New()` | One CommentThread of this type on this Product | The second commit conflicts; one component remains |
| `user.NewRating(product)` | One Rating for this pair | The second commit conflicts; one edge remains |

Both creations can be valid at their own snapshot. The commit reconciles their structural writes against the head of the Store. The modules do not provide a separate lock or uniqueness check.

**A lease transaction binds the same fragments.** The last test acquires a lease on a Product and opens a transaction through that lease:

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed-lease", StoreAccessMode.ReadWrite);
using var repository = new CommerceRepository(store);
GhostLease lease;
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    new Product(ProductRef.New());
    tx.Commit();
}
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    Assert.True(ProductViewCollection.Single().TryLock(TimeSpan.FromMinutes(1), out lease!));
    tx.Commit();
}
using (var tx = repository.OpenTransaction(lease))
{
    Assert.Same(tx, CatalogModuleTransaction.Current);
    ProductCollection.Single().Name = "Leased";
    lease.Release();
    tx.Commit();
}
Assert.Null(CatalogModuleTransaction.Current);
```

The lease changes who may write the entity; it does not introduce a new way to address the fragment. Claims, concurrency points and leases retain the rules of [Tutorial T010](T010-Claims-Points-And-Leases.md).

The concurrency tests and their synchronization are in [Lesson05ConcurrentLinks.cs](../../GhostBodyObject.Tutorials.T011/Lesson05ConcurrentLinks.cs).

## Part 3: Local APIs and application roles

### Lesson 6: Keep indexes, systems and Records local

A module's contribution includes the indexes and systems declared with its types. Code that was compiled in that module uses its ordinary local APIs after composition.

Catalog supplies the Product name index and the Annotation Record used by CommentThread:

```csharp
// Tutorials.T011.Catalog
[HashIndex(nameof(MProduct), 110), HashIndexComponent(1, nameof(MProduct.Name))]
public interface MProductNames;

[Record(1103, 1, 1)]
public interface RAnnotation
{
    [Property(1)] string Text { get; set; }
}
```

Comments supplies a text index, a value index and a system that selects Products with a CommentThread:

```csharp
// Tutorials.T011.Comments
[TextIndex(120)]
internal interface MCommentWords;

[ValueIndex(nameof(ERating), 121), ValueIndexComponent(1, nameof(ERating.Stars))]
internal interface MRatingStars;

[SystemFor(nameof(MProduct)), Enumerable]
internal interface SDiscussedProduct
{
    MProduct Product { get; }
    CCommentThread Thread { get; }
}
```

The Product comes from Catalog; the capability that makes it part of the system comes from Comments. The test uses all three index kinds and stores a Catalog Record in the component's typed bag:

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("composed-indexes", StoreAccessMode.ReadWrite);
using var repository = new CommerceRepository(store);
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    var product = new Product(ProductRef.New()) { Name = "Book" };
    var user = new UserAccount(UserAccountRef.New());
    Discussion.Add(product, user, "Useful reference");
    product.CommentThread.Get().Notes.Add(new Annotation { Text = "Imported Record" });
    user.NewRating(product).Stars = 5;

    Assert.Equal(product.Ref, ProductNames.Is("Book").Single().Ref);
    Assert.Single(RatingStars.Is(5));
    Assert.Equal("Useful reference", CommentWords.Search<CommentView>("useful").Single().Text);
    Assert.Single(DiscussedProductSystemCollection);
    tx.Commit();
}
```

**A subscription is configured outside a transaction.** Continue with a typed feed on the composed Repository:

```csharp
var seen = new List<string>();
using var feed = repository.Feed();
feed.Handle<ProductView, List<string>>(seen, static (_, view, values) => values.Add(view.Name.ToString()));
feed.Handle<CommentView, List<string>>(seen, static (_, view, values) => values.Add(view.Text.ToString()));
feed.Start();
Assert.Contains("Book", seen);
Assert.Contains("Useful reference", seen);
```

The handlers are registered against this Repository's model. Neither a feed nor a Log subscription needs a fragment transaction to be open just to identify a type. As in the earlier tutorials, handlers take their state explicitly and use static lambdas.

**Changes update the complete model.** The next transaction finds the Product by its local index, reads the imported Record, changes the indexed name and removes the Product:

```csharp
using (var tx = repository.OpenTransaction(StoreAccessMode.ReadWrite))
{
    var product = ProductNames.Is("Book").Single().ToBody();
    Assert.Equal("Imported Record", product.CommentThread.Get().Notes.Enumerate<AnnotationView>().Single().Text);
    product.Name = "Reference";
    Assert.Empty(ProductNames.Is("Book"));
    Assert.Single(ProductNames.Is("Reference"));

    product.Remove();
    Assert.Empty(RatingStars.Is(5));
    Assert.Empty(CommentWords.Search<CommentView>("useful"));
    Assert.Empty(DiscussedProductSystemCollection);
    tx.Commit();
}
```

The deletion removes the component, its comments and the rating. The index entries and system membership follow those changes. The application does not synchronize a separate Catalog index with a Comments database: these contributions are in the same model.

The test is in [Lesson06IndexesAndRecords.cs](../../GhostBodyObject.Tutorials.T011/Lesson06IndexesAndRecords.cs).

### Lesson 7: Compose Write and Read separately, then chain them

The Read model has its own entities and its own lifetime. It can still be assembled from contributions of the same modules. Catalog declares a public Read anchor in `Tutorials.T011.Catalog.Read`:

```csharp
namespace Tutorials.T011.Catalog.Read;

[GhostRepository(Fragment = true)]
public interface MCatalogReadModule;

[Entity(1601, 1, 1), Root, Enumerable]
public interface MProductSummary
{
    [Property(1)] string Name { get; set; }
}
```

Comments gives that anchor a summary component, in `Tutorials.T011.Comments.Read`:

```csharp
namespace Tutorials.T011.Comments.Read;

[GhostRepository(Fragment = true), IncludeModule(typeof(MCatalogReadModule))]
public interface MCommentReadModule;

[Entity(1602, 1, 1), ComponentFor(nameof(MProductSummary))]
internal interface CDiscussionSummary
{
    [Property(1)] int Count { get; set; }
}
```

These are different types from the Write model. Their separate namespaces also keep each fragment's discovery within its role. The application selects the Read contribution:

```csharp
[GhostRepository(112), IncludeModule(typeof(Tutorials.T011.Comments.Read.MCommentReadModule))]
internal interface MCommerceReadRepository;
```

Now connect the three Stores and their models as in Tutorial T008:

```csharp
using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("chained-write", StoreAccessMode.ReadWrite);
using var readStore = store.AddChildStore(TransactionalMemoryStore.NewInMemoryRepositoryStore("chained-read", StoreAccessMode.ReadWrite));
using var logStore = readStore.AddChildStore(TransactionalMemoryStore.NewInMemoryLogStore("chained-log", StoreAccessMode.ReadWrite));
using var write = new CommerceRepository(store);
using var read = new CommerceReadRepository(readStore);
using var log = new CommerceLog(logStore);
write.Chain(read);
read.Chain(log);
```

Opening the Write transaction opens the transactions of the chain. Each role binds its own fragments. The test submits a comment and writes the Read summary explicitly:

```csharp
using (var tx = write.OpenTransaction(StoreAccessMode.ReadWrite))
{
    Assert.Same(tx, CatalogModuleTransaction.Current);
    Assert.Same(CommerceReadRepositoryTransaction.Current, CatalogReadModuleTransaction.Current);
    Assert.Same(CatalogReadModuleTransaction.Current, CommentReadModuleTransaction.Current);
    Assert.Same(CommerceLogTransaction.Current, CommerceContractsTransaction.Current);

    var product = new Product(ProductRef.New()) { Name = "Book" };
    var author = new UserAccount(UserAccountRef.New());
    new PublishComment { Product = product.Ref, Author = author.Ref, Text = "Useful" }.Submit();
    new ProductSummary { Name = product.Name }.DiscussionSummary.GetOrNew().Count = Discussion.Count(product);
    tx.Commit();
}
```

The comment handler writes through the Write fragments. The ProductSummary and DiscussionSummary use the Read fragments. Their transactions belong to one chain, but their entities belong to separate models. The projection update above is business code in the test; composing a Read fragment does not make GBO infer a projection automatically.

An abandoned transaction changes neither model:

```csharp
using (write.OpenTransaction(StoreAccessMode.ReadWrite))
{
    ProductCollection.Single().Name = "Abandoned";
    ProductSummaryCollection.Single().DiscussionSummary.Get().Count = 99;
}
using (write.OpenTransaction(StoreAccessMode.ReadOnly))
{
    Assert.Equal("Book", ProductCollection.Single().Name);
    Assert.Equal("Book", ProductSummaryCollection.Single().Name);
    Assert.Equal(1, ProductSummaryCollection.Single().DiscussionSummary.Get().Count);
}
Assert.Null(CatalogModuleTransaction.Current);
Assert.Null(CommentReadModuleTransaction.Current);
Assert.Null(CommerceContractsTransaction.Current);
```

Use the same pattern for Front State when it is split between modules: compose that role's fragments into its own Repository, then use the existing chain or StoreSystem facilities where several models must participate in one operation.

The Read declarations are in [Catalog](../../GhostBodyObject.Tutorials.T011.Catalog/ReadModel.cs) and [Comments](../../GhostBodyObject.Tutorials.T011.Comments/ReadModel.cs). The executable chain is [Lesson07ChainCompositions.cs](../../GhostBodyObject.Tutorials.T011/Lesson07ChainCompositions.cs).

## From the boundary to the composition

An assembly, a model and a Store solve different problems. Choose each boundary for the rule it must preserve:

| What you need | What to declare or open | What remains together |
| ------------- | ----------------------- | --------------------- |
| Private module code, with links to another module's entities | Repository fragments selected by one composed Repository | One entity graph and its link rules |
| A public intention with a private implementation | A contract Log fragment and handler Log fragments selected by one Log | One dispatch and one journal of decisions |
| Several models that must commit atomically | A chain of Stores and their Repositories or Logs | One transaction of the chain |
| Independent Store instances using the same modules | Several instances of the composed Repository, used in separate execution contexts | Each instance's own data and transaction |
| Several servers holding the same data | The distribution of the Store or chain, as in Tutorial T008 | The replicated history and its conflict rules |
| Independently deployed modules with independent data | An explicit integration protocol and its consistency rules | Only the guarantees of that protocol |

Moving a module's assembly is a code organization choice. Moving its entities into an independent Domain changes the consistency boundary. Strong references, cascades and synchronous sub-Mutations that relied on the composed model must then be replaced by an explicit integration design.

**Composition is checked at compilation.** The module DLLs must exist before the application compiles. The generator reports an invalid selection through `GBO066`; existing diagnostics still check the complete model:

| Declaration that cannot compose | What to correct |
| ------------------------------ | --------------- |
| A module without `Fragment = true` | Select a fragment, rather than an independent Repository or Log |
| A Repository fragment selected by a Log, or the reverse | Keep each contribution in its own role |
| Inaccessible internal contributions or generated infrastructure | Grant `InternalsVisibleTo` from the owning module |
| A missing or unsupported generated manifest | Compile the module with a compatible generator before composing it |
| A cycle of module dependencies | Make the dependency graph acyclic |
| A fragment type used without selecting its contribution | Add the appropriate `IncludeModule` dependency |
| Deduced numbering on a fragment or composition | Assign stable identifiers and use the Fixed schema |
| A persistent identifier used twice | Give the types or indexes distinct identifiers in the composed model |
| No handler, or several handlers, for a Command in the final Log | Select exactly one corresponding Mutation |

Adding a module can add a retention to an existing anchor. Its identifiers stay stable, but an existing deletion workflow must respect the new complete model. The module order can change transient positions in a binary; it cannot be used as a persistent identity rule.

**The Store keeps the complete schema.** The composed catalog describes the flattened model, including its types, indexes and value declarations. Its description does not require the original module DLLs. Explicit struct layouts are exported by the generator, and historical declarations in a module's `VersionHistory` namespace keep their locally compiled Views and contribute to the composed Repository's history.

The [round-trip tests](../../GhostBodyObject.Generator.Tests/RoundTripShould.cs) persist all three models of this tutorial, regenerate their declarations, compile them and compare their definitions and catalogs. The schema does not contain the developer's business method bodies; the handlers remain code in their owning assemblies.

## What you learned

- A **fragment** compiles a module's local APIs before the application. It owns declarations and code, without opening an independent Store.
- **`IncludeModule`** selects compiled contributions and their dependencies, each once. A project reference alone does not select them.
- A **composed Repository** gives the contributions one entity graph: strong and weak references, parts, shared objects, components, edges and their cascades obey the same link rules across assemblies.
- **Friend access** lets the composition use internal contributions and generated infrastructure. The module that grants it chooses the assemblies that may see its internals.
- A **public Command** can compile before its private handler. The composed Log resolves that handler through the instance executing the work.
- A **sub-Mutation** runs in the current transaction. Its effects are provisional until the chain commits, and a failure of the parent rolls them back with its Log entries.
- **Replay** applies the recorded decisions in their recorded order. It never runs the decisions again or resubmits their children.
- **The execution context** selects the active Store. Bindings follow `await`, close with the transaction and cannot overlap for the same fragment within one context.
- **Write, Read and Front State** can each have their own composition. A chain connects their transactions; it does not turn their separate models into one graph.

Continue with the [architecture and API contract](../Specifications/GhostBodyObject.Runtime/Model/SPEC-RepositoryComposition.md), or run the tests and change one dependency, retention or handler. The [validation report](../Implementation/RepositoryComposition-Validation.md) records the implementation's test results and measured coverage.
