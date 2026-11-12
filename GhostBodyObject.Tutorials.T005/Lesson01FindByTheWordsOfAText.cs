using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T005
{
    // The values of every model of the tutorial, first: each value says what it is by its type, a value object (Tutorial T001,
    // Lesson 18). Declared in the namespace of the tutorial, they serve each model of its lessons; a model declares at its head the
    // values that it alone has. An index finds a value object by the value inside it, a text by its words.

    /// <summary>The name of a product, a brand, a customer: a text, read in place.</summary>
    [ValueObject<GhostString>]
    internal interface VName { }

    /// <summary>An amount of money: never negative, added and subtracted.</summary>
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;
    }
}

namespace Tutorials.T005.Products
{
    using static ProductRepositoryCollections;

    // The first model of the tutorial: the products of a shop, found by the words of their names and of their descriptions.
    //
    // Each group of lessons declares its own model, in its own namespace: the using static at the top of the namespace chooses the
    // collections of its own Repository, as in the tutorials before.

    /// <summary>The description of a product: a value of this model alone, its words in the text index.</summary>
    [ValueObject<GhostString>]
    internal interface VDescription { }

    // A text index: an interface with [TextIndex(n)], its number, stable, and unique among the Repositories of its Store; its name starts with M, as a model.
    // MProductTextIndex gives ProductTextIndex, the class of its searches.
    [TextIndex(1)]
    internal interface MProductTextIndex
    {
    }

    [Entity(1, 1, 3), Root, Removable, Enumerable]
    internal interface MProduct
    {
        // [TextIndexed]: the words of the property are in the index that it names.
        [Property(1), TextIndexed(nameof(MProductTextIndex))]
        VName Name { get; set; }

        [Property(2), TextIndexed(nameof(MProductTextIndex))]
        VDescription Description { get; set; }

        [Property(3)]
        VMoney Price { get; set; }
    }

    [GhostRepository(1), IncludeEntityModel(nameof(MProduct))]
    internal interface MProductRepository
    {
    }

    /// <summary>
    /// Lesson 1: find entities by the words of their texts.
    /// <para>
    /// A query by LINQ reads every entity, and every character of their texts: <c>Description.Contains("coffee")</c> on a million
    /// products reads a million descriptions, and knows neither the words, nor the case, nor the accents. A text index knows the words
    /// of the properties that it indexes: a search reads the entities that have them, whatever the count of the others.
    /// </para>
    /// <para>
    /// Declare the index by an interface with <c>[TextIndex(n)]</c>, then mark each property of a text by
    /// <c>[TextIndexed(nameof(MProductTextIndex))]</c>. The generator writes <c>ProductTextIndex</c>: <c>Search("coffee")</c> gives
    /// the Views of the products that have the word, in the transaction open in this context, and <c>Count("coffee")</c> counts them
    /// without reading them. A word is found whatever its case and its accents: <c>cafe</c> finds <c>Café</c>. Each word of a query
    /// is required: <c>coffee mug</c> finds the products that have both.
    /// </para>
    /// </summary>
    public sealed class Lesson01FindByTheWordsOfAText
    {
        private static ProductRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ProductRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            new Product { Name = "Coffee Mug", Description = "A white mug for your morning coffee.", Price = 9.90m };
            new Product { Name = "Espresso Machine", Description = "Brews a strong coffee in 30 seconds.", Price = 249m };
            new Product { Name = "Tea Pot", Description = "A glass pot for green tea.", Price = 24.50m };
            new Product { Name = "Café de Colombia", Description = "Beans roasted in Medellín.", Price = 12m };
            transaction.Commit();
            return shop;
        }

        private static List<string> Names(TextSearchResult<ProductView, ProductRepositoryTag> found)
            => found.Select(static product => product.Name).ToList();

        /// <summary>The products that have a word, in their name or their description; their count.</summary>
        [Fact]
        public void FindTheProductsOfAWord()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-word", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

            // The Views of the products that have the word: in the name and the description of the mug, in the description of the machine.
            Assert.Equal(["Coffee Mug", "Espresso Machine"], Names(ProductTextIndex.Search("coffee")));
            Assert.Equal(2, ProductTextIndex.Count("coffee"));

            // A word whatever its case and its accents.
            Assert.Equal(["Café de Colombia"], Names(ProductTextIndex.Search("CAFE")));
            Assert.Equal(["Café de Colombia"], Names(ProductTextIndex.Search("medellin")));

            // Every word of the query is required.
            Assert.Equal(["Coffee Mug"], Names(ProductTextIndex.Search("coffee mug")));
            Assert.Empty(Names(ProductTextIndex.Search("coffee tea")));
        }

        /// <summary>The Bodies of the products found, for the few to change: their new words are found at the next instruction.</summary>
        [Fact]
        public void ChangeTheProductsFound()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-change", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using (var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // Search<Product>: the Bodies, to change them.
                foreach (var product in ProductTextIndex.Search<Product>("tea"))
                    product.Description += " Keeps the tea warm.";

                // The index is up to date at the next instruction, before the commit: a new product, a new word.
                new Product { Name = "Milk Frother", Description = "Foams the milk of your coffee.", Price = 19m };
                Assert.Equal(3, ProductTextIndex.Count("coffee"));
                Assert.Equal(["Tea Pot"], Names(ProductTextIndex.Search("warm")));
                transaction.Commit();
            }
            using (shop.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(["Tea Pot"], Names(ProductTextIndex.Search("warm")));
                Assert.Equal(["Coffee Mug", "Espresso Machine", "Milk Frother"], Names(ProductTextIndex.Search("coffee")));
            }
        }

        /// <summary>A product removed is no longer found; a word no product has finds nothing.</summary>
        [Fact]
        public void ForgetTheProductsRemoved()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-01-remove", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            ProductTextIndex.Search<Product>("espresso").Single().Remove();
            Assert.Equal(["Coffee Mug"], Names(ProductTextIndex.Search("coffee")));
            Assert.Equal(0, ProductTextIndex.Count("espresso"));
            Assert.Equal(0, ProductTextIndex.Count("chocolate"));
            transaction.Commit();
        }
    }
}
