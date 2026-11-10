using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    // The generated classes are partial: your code goes in a partial declaration of your own, in your files. GBO stores the
    // properties of the model only: what you add is computed, or kept in a property of the model by your rules.

    internal partial class Book
    {
        /// <summary>A computed property: GBO does not store it, it is computed at each read.</summary>
        public bool IsClassic => Year < 1960;

        // For each property of the model, the generator declares a partial method, called after a real change of its value. This
        // rule keeps PriceChanges, a property of the model: the count is stored, committed and seen by every transaction.
        //
        // Never a field nor an auto-property of your own here: a Body lives in one transaction, and such a value would be
        // stored nowhere, seen by no other transaction, and start again at each one.
        partial void OnPriceChanged() => PriceChanges++;
    }

    internal readonly partial struct BookView
    {
        /// <summary>The same computed property on the View, to use it in the queries over the Views.</summary>
        public bool IsClassic => Year < 1960;
    }

    /// <summary>
    /// Lesson 11: add your own code to an entity.
    /// <para>
    /// <c>Book</c> and <c>BookView</c> are partial: add computed properties and business rules to them in your own partial
    /// declarations, above. The partial method <c>On...Changed</c> of a property is called after each real change of its
    /// value, not when the same value is written again. What a rule must keep is a property of the model, never a field of
    /// the Body.
    /// </para>
    /// </summary>
    public sealed class Lesson11AddYourOwnCode
    {
        /// <summary>A computed property, used in a query; a rule that keeps a stored property on each real change of a price.</summary>
        [Fact]
        public void ComputeAndReactInTheEntity()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-code", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            int changes;
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var dune = new Book { Title = "Dune", Year = 1965, Price = 9.90m };
                new Book { Title = "Foundation", Year = 1951, Price = 8.50m };

                // The computed property, on the Body and on the View, as any property.
                Assert.False(dune.IsClassic);
                Assert.Equal(1, BookCollection.Count(static book => book.IsClassic));
                Assert.Equal("Foundation", BookViewCollection.Single(static book => book.IsClassic).Title);

                // The rule counts the real changes: the same price written again is not one.
                int before = dune.PriceChanges;
                dune.Price = 12.50m;
                dune.Price = 12.50m;
                dune.Price = 13.00m;
                Assert.Equal(2, dune.PriceChanges - before);
                changes = dune.PriceChanges;
                transaction.Commit();
            }

            // The count is a property of the model: committed with the book, a new transaction reads it.
            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.Equal(changes, BookViewCollection.Single(static book => book.Title == "Dune").PriceChanges);
            }
        }
    }
}
