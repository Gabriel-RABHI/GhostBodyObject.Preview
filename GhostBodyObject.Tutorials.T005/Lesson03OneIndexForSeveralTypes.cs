using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T005.Catalog
{
    // The second model: the catalog of a coffee shop, its products and their brands, in one text index; the tags of the products in
    // an index of their own, of its own rules (Lesson 4).

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The tags of a product, "#espresso #arabica".</summary>
    [ValueObject<GhostString>]
    internal interface VTags { }

    /// <summary>The story of a brand.</summary>
    [ValueObject<GhostString>]
    internal interface VStory { }

    [TextIndex(1)]
    internal interface MCatalogTextIndex
    {
    }

    // The rules of an index (Lesson 4): the accents kept, and a term cut at the commas and the spaces only, so that fair-trade stays one word.
    [TextIndex(2, KeepAccents = true, Separators = ",")]
    internal interface MTagTextIndex
    {
    }

    [Entity(1, 1, 4), Root, Removable, Enumerable]
    internal interface MProduct
    {
        [Property(1), TextIndexed(nameof(MCatalogTextIndex))]
        VName Name { get; set; }

        // TextIndexing.Commit (Lesson 4): the words of a long text, written many times, are cut once, at the first search or at the commit.
        [Property(2), TextIndexed(nameof(MCatalogTextIndex), TextIndexing.Commit)]
        GhostString Description { get; set; }

        [Property(3), TextIndexed(nameof(MTagTextIndex))]
        VTags Tags { get; set; }

        [Property(4)]
        VMoney Price { get; set; }
    }

    [Entity(2, 1, 2), Root, Removable, Enumerable]
    internal interface MBrand
    {
        [Property(1), TextIndexed(nameof(MCatalogTextIndex))]
        VName Name { get; set; }

        [Property(2), TextIndexed(nameof(MCatalogTextIndex))]
        VStory Story { get; set; }
    }

    [GhostRepository(2), IncludeEntityModel(nameof(MProduct)), IncludeEntityModel(nameof(MBrand))]
    internal interface MCatalogRepository
    {
    }

    /// <summary>
    /// Lesson 3: one index for several types.
    /// <para>
    /// The properties of several entity types may name the same index: the products and their brands share the catalog index, one
    /// search box for both. A search then says the type it looks for: <c>CatalogTextIndex.Search&lt;ProductView&gt;("italian")</c> gives
    /// the products, <c>Search&lt;BrandView&gt;</c> the brands, <c>Search&lt;Brand&gt;</c> their Bodies. <c>Terms(prefix)</c> gives the words
    /// of every type of the index.
    /// </para>
    /// </summary>
    public sealed class Lesson03OneIndexForSeveralTypes
    {
        /// <summary>The products and the brands of a word, each by its type.</summary>
        [Fact]
        public void SearchEachTypeOfTheIndex()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-types", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Brand { Name = "Lavazza", Story = "An Italian roaster in Turin since 1895." };
                new Brand { Name = "Juan Valdez", Story = "The coffee of the growers of Colombia." };
                new Product { Name = "Lavazza Oro", Description = "Arabica beans, a sweet Italian blend.", Price = 11.90m };
                new Product { Name = "Moka Pot", Description = "The Italian way to brew at home.", Price = 29m };
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // One word, the entities of each type.
                Assert.Equal(["Lavazza Oro", "Moka Pot"], CatalogTextIndex.Search<ProductView>("italian").Select(static p => p.Name).ToList());
                Assert.Equal(["Lavazza"], CatalogTextIndex.Search<BrandView>("italian").Select(static b => b.Name).ToList());
                Assert.Equal(1, CatalogTextIndex.Search<ProductView>("lavazza").Count());
                Assert.Equal(1, CatalogTextIndex.Search<BrandView>("lavazza").Count());

                // The words of both types.
                Assert.Equal(["coffee", "colombia"], CatalogTextIndex.Terms("co"));
            }
        }

        /// <summary>The Bodies of a type found, changed; the words of the other type stay.</summary>
        [Fact]
        public void ChangeTheEntitiesOfOneType()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-03-change", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            new Brand { Name = "Lavazza", Story = "An Italian roaster in Turin." };
            new Product { Name = "Lavazza Oro", Description = "An Italian blend." };
            foreach (var brand in CatalogTextIndex.Search<Brand>("turin"))
                brand.Story = "A roaster of Turin, now in Milan too.";
            Assert.Equal(1, CatalogTextIndex.Search<BrandView>("milan").Count());
            Assert.Equal(0, CatalogTextIndex.Search<BrandView>("italian").Count());
            Assert.Equal(1, CatalogTextIndex.Search<ProductView>("italian").Count());
            transaction.Commit();
        }
    }
}
