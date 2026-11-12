using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T005.Products
{
    /// <summary>
    /// Lesson 2: the language of a query.
    /// <para>
    /// A query is a list of words. A word alone is required; <c>+word</c> is an alternative, which adds the entities that have it;
    /// <c>-word</c> excludes the entities that have it; a query of exclusions only starts from every entity. <c>Name:word</c> looks in
    /// one property. <c>*</c> stands for any letters, <c>?</c> for one: <c>espress*</c>, <c>t?a</c>. The order of the words never
    /// changes the result.
    /// </para>
    /// <para>
    /// <c>TextSearch.Fuzzy</c> forgives a typing error: a word within one letter of the word searched, two for the long words, a
    /// letter added, removed, changed or two swapped. <c>Terms(prefix)</c> gives the words of the index that start with a prefix, in
    /// their order: what a search box proposes while the user types.
    /// </para>
    /// </summary>
    public sealed class Lesson02TheLanguageOfAQuery
    {
        private static ProductRepository NewShop(TransactionalMemoryStore store)
        {
            var shop = new ProductRepository(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadWrite);
            new Product { Name = "Coffee Mug", Description = "A white mug for your morning coffee." };
            new Product { Name = "Espresso Machine", Description = "Brews a strong coffee in 30 seconds." };
            new Product { Name = "Tea Pot", Description = "A glass pot for green tea." };
            new Product { Name = "Tea Mug", Description = "A large mug, for tea or coffee." };
            new Product { Name = "Espresso Cups", Description = "Six small cups." };
            transaction.Commit();
            return shop;
        }

        private static List<string> Names(TextSearchResult<ProductView, ProductRepositoryTag> found)
            => found.Select(static product => product.Name).ToList();

        /// <summary>Required words, alternatives, exclusions, and a word in one property.</summary>
        [Fact]
        public void CombineTheWords()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-words", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

            // An alternative adds; an exclusion takes off.
            Assert.Equal(["Tea Pot", "Tea Mug", "Espresso Cups"], Names(ProductTextIndex.Search("tea +cups")));
            Assert.Equal(["Coffee Mug", "Espresso Machine"], Names(ProductTextIndex.Search("coffee -tea")));

            // Exclusions alone: every product, but those.
            Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("-mug -pot")));

            // The word in one property: "coffee" in the name only, not in a description.
            Assert.Equal(["Coffee Mug"], Names(ProductTextIndex.Search("name:coffee")));

            // The order of the words never changes the result.
            Assert.Equal(Names(ProductTextIndex.Search("coffee -tea")), Names(ProductTextIndex.Search("-tea coffee")));
        }

        /// <summary>Wildcards, a typing error forgiven, and the words that start with a prefix.</summary>
        [Fact]
        public void FindTheWordsThatLookAlike()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-02-alike", StoreAccessMode.ReadWrite);
            using var shop = NewShop(store);
            using var transaction = shop.OpenTransaction(StoreAccessMode.ReadOnly);

            // Any letters, and one letter.
            Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("espr*")));
            Assert.Equal(["Tea Pot", "Tea Mug"], Names(ProductTextIndex.Search("t?a")));

            // A typing error: "expresso" for "espresso", "cofee" for "coffee".
            Assert.Equal(["Espresso Machine", "Espresso Cups"], Names(ProductTextIndex.Search("expresso", TextSearch.Fuzzy)));
            Assert.Equal(3, ProductTextIndex.Count("cofee", TextSearch.Fuzzy));
            Assert.Equal(0, ProductTextIndex.Count("cofee"));

            // The words of the index that start with a prefix, each once, in their order.
            Assert.Equal(["coffee", "cups"], ProductTextIndex.Terms("c"));
            Assert.Equal(["machine", "morning", "mug"], ProductTextIndex.Terms("m"));
        }
    }
}
