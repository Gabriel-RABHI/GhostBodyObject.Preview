# A worked audit: the quote model

Part of [the modeling knowledge](README.md). The port of the quote domain (`GhostBodyObject.Quote.Domain/Model`) translated its source table by table: one GBO type per source type, the source links made compositions or references. It has no component, no system, no edge. This file passes the [checklist](checklist.md) on it. **It is a proposal that shows the method, not the decided design of the port**: each row is to be confirmed against the analysis of the source (`Documentation/Source`). A reviewer of this model, or an agent that ports this domain, reads it after its own reading, for the method only: its rows are not findings. Five rows were corrected on 2026-10-29 against the code (the template, the generation, the cover page, `WithOptions`, the families), and [what it does not cover](#what-this-audit-does-not-cover) is listed at its end.

The forms used below were checked by a build against the generator, and run: a component of a part type, removed with its part; a system over a part type; one part type composed twice by one Owner (`T` and `Option<T>`); a component shared by two entity types; in a deduced Repository, components and a system without `[Entity]`, in sub-namespaces taken by `[IncludeNamespace(..., SubNamespaces = true)]`.

## `MQuote`: about nine concerns in one entity

What condemns it is the use of its fields: most groups below are empty for most quotes (the final customer, the template, the generation, the decision, the cover page), or describe other concepts (the parties, the totals, the document).

| Group of properties | Smell (section G) | Unit proposed | Questions |
|---|---|---|---|
| `Reference`, `Version`, `DraftNumber`, `Status`, `Step`, `Description`, `TextNote`, `ExpirationDate`, `Company`, `BaseQuote`, `BusinessCase`, `Agency`, `Lines` | | **The anchor**: the quote's own responsibility | A7, A8 |
| `PrimaryCustomerCode`, `PrimaryCustomerName`, the address lines, `PrimaryZipCode`, `PrimaryCity`, `CustomerCountryCode`, `CustomerCodeTiers`, `TradeName` | A prefix; a copy of another concept | A part, `MQuoteParty Primary { get; }`: a snapshot that must not follow the customer | B4, B8 |
| `FinalCustomerCode`, `FinalCustomerName`, its address lines, `FinalZipCode`, `FinalCity` | The same prefix, empty for most quotes | The same part type, optional: `Option<MQuoteParty> Final { get; }` | B2, B4, B5 |
| `ContactCode`, `ContactFirstName`, `ContactLastName`, `ContactPhone`, `ContactEmail`, `ContactTitle` | A prefix | An optional part, `Option<MQuoteContact> Contact { get; }` | B4 |
| `PaymentMethodCode`, `IncotermsCode`, `CommercialCode`, `CommercialName` | Codes of other concepts | Commercial terms in the party snapshot; the commercial a `[Weak]` link (or a snapshot) | B8 |
| `TotalPrice`, `TotalTax`, `EcoTaxTotal`, `TotalMargin`, `MarginPercentage`, `TotalDiscount`, `TotalDiscountTaxAmount`, `TaxCountryCode`, `TaxType`, `TNTaxRate` | The pricing concern | A required part `MQuoteTotals Totals { get; }` | B3, B4 |
| `IsTemplate`, `IsPersonalTemplate`, `TemplateGroup`, `TemplateEtab`, `TemplateCategory`, `TemplateFamily`, `TemplateTitle` | A flag and its satellites | A component `CTemplate`; `IsTemplate` is its presence | B2, B5, B6 |
| `BaseTemplate` | Set on the quote from which a template is made ("the quote takes the template as base, its own template block cleared"), never on the template | Not a datum of the template: a `[Weak] Option<MQuote>` of the quote, or a component of its own (`CTemplateOrigin`) | B3, B5 |
| `FinalNumber` | Assigned when the generation starts (`Generating`), released by a cancellation (back to `Draft`) | A value of the numbering of the anchor, beside `DraftNumber`: `Option<int>` | B1, B2 |
| `GenerationDate`, `GeneratedBy`, `FinalDocument`, `FinalDocumentVersion` | Meaningful once generated only; kept after a cancellation, cleared by the next draft number | A component `CGeneration`, removed when the next draft number is assigned, not at the cancellation | B2, B5, B7 |
| `AcceptationOrRejectionDate`, `AcceptedOrRejectedBy`, `AcceptedWithOptions` | Meaningful once decided only | A component `CDecision` | B2, B5, B7 |
| `NeedTechnicalValidation`, `NeedDirectionValidation`, `NeedCommercialValidation` | Flags | A component `CValidationRequest` (its three kinds), removed when validated | B6 |
| `WithCoverPage`, `CoverPageText` | A flag and its satellite; the text is kept while the cover page is off (`CoverPageChanged` carries both) | A component `CCoverPage` holding the text, and whether it is printed as its state | B2, B6 |
| `PdfTemplate`, `Cgvs`, `Reservations` | The document concern | Parts and a link kept on the anchor, or a required part `MQuoteDocument` that groups them | B3 |
| `CreationDate`, `CreatedBy`, `ModificationDate`, `ModifiedBy` | A concern shared by many types | Kept on the anchor, or a component `CAuditStamp` shared with the customer, the product... | B13 |
| `WithOptions` | Set by the user (`ChangeWithOptions`), copied by a duplication and a template: not computable | A value of the anchor, an enum by the value rule | B1, B14 |

The systems that the screens and the rules read (section F): `STemplateQuote` (the quote and its `CTemplate`: the library of templates), `SQuoteToValidate` (the quote and its `CValidationRequest`), `SGeneratedQuote` (the quote, its `CGeneration`, an optional `CDecision`), the drafts (excluding `CGeneration` and `CTemplate`). A rule that needs two aspects (a template cannot be generated) is a method of a system, or a check in the `Decide` of the Mutation.

```csharp
// The sketch, in the deduced form (the models behind the Mutation Log are rebuilt by its Replay); a Fixed model adds the numbers.
[Entity(40), Root, Removable, WithIdentity, Enumerable]
internal interface MQuote
{
    [Strong] Option<MCompany> Company { get; set; }
    GhostString Reference { get; set; }
    int Version { get; set; }
    Option<int> DraftNumber { get; set; }
    Option<int> FinalNumber { get; set; }               // assigned when the generation starts, released by a cancellation
    QuoteStatus Status { get; set; }
    QuoteStep Step { get; set; }
    GhostString Description { get; set; }
    DateTime ExpirationDate { get; set; }
    [Weak] Option<MQuote> BaseQuote { get; set; }
    [Weak] Option<MBusinessCase> BusinessCase { get; set; }
    MQuoteParty Primary { get; }                        // a snapshot of the customer at the date of the quote
    Option<MQuoteParty> Final { get; }
    Option<MQuoteContact> Contact { get; }
    MQuoteTotals Totals { get; }
    IEntityCollection<MQuoteLine> Lines { get; }
}

[ComponentFor(nameof(MQuote))]
internal interface CTemplate                            // namespace Quotes.Model.Templates
{
    bool IsPersonal { get; set; }
    GhostString Group { get; set; }
    GhostString Title { get; set; }
}

[ComponentFor(nameof(MQuote))]
internal interface CGeneration                          // namespace Quotes.Model.Generations (never Generation: the Body of CGeneration)
{
    DateTime At { get; set; }
    Guid By { get; set; }
    Guid FinalDocument { get; set; }
    short FinalDocumentVersion { get; set; }
}

[SystemFor(nameof(MQuote)), Enumerable]
internal interface STemplateQuote { MQuote Quote { get; } CTemplate Template { get; } }
```

## `MQuoteLine`: three kinds of line in one entity

A line is a product line, a group header or a comment (`IsGroup`, `IsComment`): a comment leaves every product and pricing field empty. The kinds share one ordered sequence (`LineNo`), so separate part types in separate collections would lose it: the line stays one anchor, and its kinds become components of the part type.

| Group of properties | Unit proposed | Questions |
|---|---|---|
| `LineNo`, `IsOptional`, `IsInGroup`, `ColorWheelIndex`, `[OwnedFrom] Quote` | The anchor (a part of the quote) | A5 |
| `ProductCode`, `ProductDescription`, `ProductType`, `ProductWebUrl`, `IsShippingCost`, `IsFreeProduct`, `IsService`, `EcoTaxCode` | A component `CProduct`: the snapshot of the product sold | B7, D8 |
| `Quantity`, the public, buying and selling prices, the margins, the discounts, `TaxRate`, `LineTotal`, `LineTaxes`, `EcoTaxAmt`, `PricingDescription`, `HasFooterDiscount`, `HasLineDiscount` | A component `CPricing`; its numbered discounts a `GhostSpan<decimal>` or a sub-component `CDiscounts` | B7, B9, D7 |
| `IsGroup` and the totals of a group | A component `CGroupHeader` | B6, B7 |
| `IsComment` | A component `CComment`, or the absence of `CProduct` | B6 |

`SPricedLine` (the line and its `CPricing`, an optional `CDiscounts`) is the sequence that the computations of totals read. Whether a group should own its lines (a tree: a group header composing its lines) instead of their contiguity by `LineNo` is an open question.

## The delegation tree of the quote

The compact quote delegates each concern, and each delegate does the same in its turn:

```
MQuote                         Root: reference, version, status, step, description, expiration, company, business case
 ├─ Primary, Final, Contact    aggregations: the snapshots of the parties (MQuoteParty, MQuoteContact)
 ├─ Totals                     aggregation: the totals and the taxation of the quote
 ├─ Lines                      aggregation: IEntityCollection<MQuoteLine>
 │   ├─ CProduct               component of a line: the product sold (not on a comment nor a group header)
 │   ├─ CPricing               component of a line: quantity, prices, margins, taxes, amounts
 │   │   └─ CDiscounts         a second component of the line, joined to CPricing by SDiscountedLine
 │   ├─ CGroupHeader           component of a line: the totals of its group
 │   └─ CComment               component of a line: its text
 ├─ CTemplate                  component: a quote that is a template (module Templates)
 ├─ CValidationRequest         component: the validations pending (module Validations)
 ├─ CGeneration                component: the final document, once generated (module Generations)
 │   └─ CDecision              a second component of the quote, present once accepted or rejected
 └─ CCoverPage                 component: the cover page, when asked
```

Each node is the size of its concern; the quote knows its aggregations and none of its components; the screens read systems (`STemplateQuote`, `SQuoteToValidate`, `SGeneratedQuote`, `SPricedLine`), and the rules over two aspects (a template is never generated, a priced line in a group updates its header) are methods of those systems or checks of the `Decide` of the Mutations.

## The catalog: three link tables are edges

| Today | Smell | Proposed |
|---|---|---|
| `MFamilyLink`: a Root with `FromFamily`, `ToFamily` (codes), `FromLevel`, `LinkNumber`, `IsY2` | Two codes of other concepts and few values | Between two families: `[Edge(nameof(MProductFamily), nameof(MProductFamily))] EFamilyLink { LinkNumber, IsY2 }`, the level is the family's. The links from the virtual root (`FromFamily = "Root"`) have no family at their parent end: not edges; their values belong to the family they lead to, to confirm |
| `MLinkedProduct`: `ProductCode`, `LinkedProductCode` (codes), `Rank`, `Description` | The same | `[Edge(nameof(MProduct), nameof(MProduct))] ELinkedProduct { Rank, Description }` |
| `MProductLinkToFamily`: `ProductCode`, `FamilyCode` (codes), `Level`, `ItemIndex` | The same | `[Edge(nameof(MProduct), nameof(MProductFamily))] EProductInFamily { ItemIndex }` |
| `MProduct.ProductFamily1..3` (codes) | Numbered fields; the codes of the families of the ERP, not of the families of the users | A value object of the ERP hierarchy (a stored struct or a part of three codes), never a link: the ERP families are not entities of the model |
| `MProduct.DefaultCategory`, `DefaultFamily`, `DefaultSubFamily` (weak links), `CategoryOrderIndex`, `FamilyOrderIndex`, `SubFamilyOrderIndex` | The first family of each level where the users placed the product, forgotten when it is removed; ranks of the product in other concepts | The ranks are values of `EProductInFamily`; the first placement of each level stays a `[Weak]` link, or a value of the edge, to confirm |

Removing a product then removes its links and placements at that instruction, with no code. These link types have `[WithIdentity]` today because their events designate them: as edges, a Mutation designates one by its pair of ends (`product.LinkedProductEdges.To(other)`), the codes of the ERP resolved in `Decide`, the pair written in the Decision. To confirm: the source removes a link with either end, and holds one link per pair.

## `MProduct` and `MCustomer`

| Entity | Group | Proposed |
|---|---|---|
| Product | `IsEndOfLife`, `EndOfLifeWarningDate`, `EndOfLifeEffectiveDate` | A component `CEndOfLife` |
| Product | `WebUrl` | A component `CWebPage` if few products are sold online |
| Product | `PricingCode`, `TaxFamily1`, `EcoTaxCode`, `HasFooterDiscount`, `HasLineDiscount` | A part or a component of the sales terms |
| Product | The families, their codes and ranks | The edges of the catalog |
| Customer | `Street1..3`, `ZipCode`, `City`, `CountryCode` | A part `MAddress` |
| Customer | `AgencyCode`, `CommercialCode` | Links to `MAgency`, `MCommercial` |
| Customer | `IncotermsCode`, `PricingCode`, `TaxType`, `PaymentMethodCode` | A part of commercial terms |
| Customer | `IsCompetitor` | A role, a component; with the customers, contacts and commercials, the parties-and-roles pattern ([components-ecs.md](components-ecs.md)) is to study |

## The values of the quote

The sketches keep .NET types to show the structure; the value rule types them ([values.md](values.md#value-objects-the-general-rule)): the prices, totals, margins and taxes are `Money` and `Rate` (stored structs, their operators in the type), the quantities `Quantity`, the codes (`ProductCode`, `TaxType`, `PaymentMethodCode`, `IncotermsCode`, `CountryCode`) converted types or enums when their set is closed, the reference and the numbers of a quote `QuoteReference` and `QuoteNumber`, and the `bool` of a line (`IsOptional`, `IsShippingCost`, `IsFreeProduct`) a role of the line, an enum, or a component when data depend on it. The totals of the quote are summed by `Aggregate` and the operators of `Money`.

## What the reclassification costs in Mutation Sourcing

- **The Mutations keep their Intentions**: they name quotes, lines and products by their `*Ref`, which do not change.
- **`Decide` reads aspects** (`quote.Template.TryGet(out var template)`), and the systems replace the filters on flags (`TemplateQuoteSystemCollection`).
- **`Apply` writes aspects**: `quote.Generation.New()`, `line.Pricing.Get()`; it remains a navigation, without query nor index.
- **No data migration**: the Write and Read models are projections of the Log, rebuilt by its Replay with the new `Apply`; they may take the Deduced schema.

## What this audit does not cover

Found by a review of the code on 2026-10-29, to classify with the same method:

- `MAuditEntry`: facts kept as a Root that only grows, and an identity of any type (`EntityRef` of the runtime, the identity of no type, which GBO012 lets into an entity: no relation replaces it).
- Values whose unit follows a flag: the second discounts of a line in percent or in points (`discount2InPoints`), a tax rate or an amount (`MTax.IsRate`): a value object that carries its unit.
- The tax model, and the snapshots held by `MBusinessCase`.

## Open questions

1. The lines of a group: contiguity by `LineNo` (today), or a group header that composes its lines.
2. The audit stamps: on each anchor, or one component `CAuditStamp` shared by the types.
3. The parties: the customer, the contact, the commercial, the company as one `MParty` and its roles.
4. The catalog links: one per pair in the source, and removed with either end, as an edge requires.
