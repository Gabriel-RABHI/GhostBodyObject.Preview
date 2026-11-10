using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialFive.Catalog
{
    /// <summary>
    /// Lesson 4: the most relevant first, and the rules of an index.
    /// <para>
    /// By default, a search gives its entities in the order of the Store. <c>TextSearch.Ranked</c> gives the most relevant first: an
    /// entity whose texts repeat the words searched, in a short text, before one that names them once in a long one; and a rare word
    /// weighs more than a frequent one. It is the rank of the search engines, BM25.
    /// </para>
    /// <para>
    /// An index has its rules. The shortest word, two letters by default, <c>MinLength</c>; <c>KeepAccents = true</c>, the accents
    /// kept: <c>été</c> and <c>ete</c> are two words; <c>Separators = ","</c>, a word cut at the commas and the spaces only, so that
    /// <c>fair-trade</c> stays one word. A property <c>TextIndexing.Commit</c> cuts its words once, at the first search of the
    /// transaction or at its commit: a long description written many times costs one cut.
    /// </para>
    /// </summary>
    public sealed class Lesson04RankAndRules
    {
        /// <summary>The products of a word, the most relevant first; a rare word weighs more.</summary>
        [Fact]
        public void RankTheMostRelevantFirst()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rank", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Product { Name = "Coffee Mug", Description = "A mug." };
                new Product { Name = "Espresso Machine", Description = "Coffee, coffee, coffee: for coffee lovers." };
                new Product { Name = "Coffee Grinder", Description = "Grinds coffee beans." };
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // In the order of the Store, then by relevance: the word repeated in a short text first.
                Assert.Equal(["Coffee Mug", "Espresso Machine", "Coffee Grinder"],
                    CatalogTextIndex.Search<ProductView>("coffee").Select(static p => p.Name).ToList());
                Assert.Equal(["Espresso Machine", "Coffee Grinder", "Coffee Mug"],
                    CatalogTextIndex.Search<ProductView>("coffee", TextSearch.Ranked).Select(static p => p.Name).ToList());

                // "grinder", rare, weighs more than "coffee", which every product has.
                Assert.Equal("Coffee Grinder", CatalogTextIndex.Search<ProductView>("coffee +grinder", TextSearch.Ranked).First().Name);
            }
        }

        /// <summary>The tags: the accents kept, a word cut at the commas only.</summary>
        [Fact]
        public void KeepTheRulesOfAnIndex()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-rules", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite);
            new Product { Name = "Iced Coffee", Tags = "été,fair-trade" };
            new Product { Name = "Hot Chocolate", Tags = "hiver,gift" };

            // The accents kept: "ete" is not "été".
            Assert.Equal(1, TagTextIndex.Count("été"));
            Assert.Equal(0, TagTextIndex.Count("ete"));

            // "fair-trade" is one word: neither "fair" nor "trade" alone.
            Assert.Equal(["Iced Coffee"], TagTextIndex.Search("fair-trade").Select(static p => p.Name).ToList());
            Assert.Equal(0, TagTextIndex.Count("trade"));

            // The catalog index, of the default rules, folds the accents of the names.
            new Product { Name = "Café Glacé" };
            Assert.Equal(1, CatalogTextIndex.Search<ProductView>("glace").Count());
            transaction.Commit();
        }

        /// <summary>A long text written many times: its words cut once, and found by the search of the transaction.</summary>
        [Fact]
        public void CutALongTextAtTheCommit()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-commit", StoreAccessMode.ReadWrite);
            using var catalog = new CatalogRepository(store);
            using (var transaction = catalog.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var product = new Product { Name = "Tasting Box" };
                // The description is built in steps: its words are cut once, not at each step.
                for (int day = 1; day <= 5; day++)
                    product.Description = $"{product.Description} Day {day}: a new origin.";
                product.Description = $"{product.Description} Day 6: Ethiopia.";

                // The search of the transaction sees the last text.
                Assert.Equal(1, CatalogTextIndex.Search<ProductView>("ethiopia").Count());
                transaction.Commit();
            }
            using (catalog.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(1, CatalogTextIndex.Search<ProductView>("origin ethiopia").Count());
        }
    }
}
