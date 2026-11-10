using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialOne
{
    /// <summary>An amount in cents, over a long: the fastest form of an amount, exact, added and compared as a long.</summary>
    // A decimal adds and compares by a call; a long, in one instruction. The amounts of the library are decimals, to read as they are
    // written (9.90); a model whose amounts are many and summed often stores them in cents.
    [ValueObject<long>(Operators = ValueOperators.Add | ValueOperators.Subtract)]
    internal interface VCents { }

    internal partial struct Cents
    {
        private static bool IsValid(long cents) => cents >= 0;

        public override string ToString() => $"{Value / 100}.{Value % 100:D2}";
    }

    /// <summary>
    /// Lesson 19: give each value its type.
    /// <para>
    /// Every value of <c>MBook</c> is a value object, declared at the head of the model (Lesson 1): a <c>Title</c>, an <c>AuthorName</c>,
    /// a <c>Year</c>, a <c>Money</c>, an <c>Isbn</c>, an <c>Option&lt;Money&gt;</c>, an <c>Option&lt;Percent&gt;</c>, a <c>ShelfMark</c>.
    /// For each one, the generator wrote its value, <c>Value</c>; <c>From</c> and <c>TryFrom</c>, which check its rule, <c>IsValid</c>;
    /// its equality, its order, its parsing and its display; the operators that <c>Operators</c> asks. A value that breaks its rule does
    /// not exist: <c>TryFrom</c> says false, <c>From</c> and a conversion throw. A value read from the Store is never checked again.
    /// </para>
    /// <para>
    /// A text value object is a text in disguise: <c>Title</c>, that a book reads and writes in place, and <c>ReadOnlyTitle</c>, that a
    /// View reads, as a <c>GhostString</c> and a <c>ReadOnlyGhostString</c>. It is a <c>ref struct</c>: a field of the model, a parameter,
    /// a local value, the state of a query that ends; never a field of a class nor an element of a <c>List</c>, a Record carries it
    /// (Tutorial Six). A <c>Select</c> and an <c>OrderBy</c> read it in place; <c>ToString()</c>, or a conversion to a <c>string</c>,
    /// copies it.
    /// </para>
    /// </summary>
    public sealed class Lesson19GiveEachValueItsType
    {
        /// <summary>The value objects of the books: checked, written, then read in the queries by their own operators.</summary>
        [Fact]
        public void StoreAndQueryTheValueObjectsOfTheBooks()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-values", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // Check, then write: a text that is no ISBN gives none.
                Assert.False(Isbn.TryRead("not an isbn", out _));
                Assert.True(Isbn.TryRead("978-0-441-17271-9", out var dune));
                Assert.True(Isbn.TryRead("978-0-553-29335-7", out var foundation));
                new Book { Title = "Dune", Isbn = dune, Deposit = Money.From(20m), MemberDiscount = Percent.From(10), Shelf = "SF-12" };
                new Book { Title = "Foundation", Isbn = foundation, Deposit = Money.From(15m), Shelf = "SF-07" };
                // Without a deposit nor a discount: the Options are absent.
                new Book { Title = "Emma", Shelf = "RO-01" };

                // A value that breaks its rule does not exist: TryFrom says false, From and a conversion throw.
                Assert.False(Money.TryFrom(-1m, out _));
                Assert.Throws<ArgumentException>(static () => Percent.From(150));
                Assert.Throws<ArgumentException>(static () => { Money price = -0.01m; });
                Assert.Throws<ArgumentException>(static () => new ShelfMark("12"));
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // The sum and the greatest of the deposits, by the operators of Money, and its display.
                var total = BookViewCollection.Sum(static book => book.Deposit.GetOr(0m));
                Assert.Equal((Money.From(35m), "35.00"), (total, total.ToString()));
                Assert.Equal(Money.From(20m), BookViewCollection.Max(static book => book.Deposit.GetOr(0m)));

                // A book found by its ISBN, compared as a long; the ISBN given to the lambda as its state.
                Assert.True(Isbn.TryRead("9780553293357", out var wanted));
                var found = BookViewCollection.Single(wanted, static (book, isbn) => book.Isbn == isbn);
                Assert.Equal("Foundation", found.Title);
                Assert.Equal("9780553293357", found.Isbn.ToString());

                // The discount of a member, applied by its own method.
                var discounted = BookViewCollection.Single(static book => book.MemberDiscount.Any());
                Assert.Equal(Money.From(2m), discounted.MemberDiscount.Get().Of(discounted.Deposit.Get()));
                Assert.False(BookViewCollection.Single(static book => book.Title == "Emma").Deposit.Any());
            }
        }

        /// <summary>A text in disguise: compared to a string, given as the state of a query, selected and ordered in place.</summary>
        [Fact]
        public void ReadTheTextsInPlace()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-19-texts", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using (var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Book { Title = "Foundation", Author = "Isaac Asimov", Shelf = "SF-07" };
                new Book { Title = "Dune", Author = "Frank Herbert", Shelf = "SF-12" };
                new Book { Title = "Emma", Author = "Jane Austen", Shelf = "RO-01" };
                transaction.Commit();
            }

            using (library.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // A local value made from a string, checked, then given as the state of a query.
                ShelfMark shelf = "SF-12";
                Assert.Equal("Dune", BookViewCollection.Single(shelf, static (book, mark) => book.Shelf == mark).Title);
                Assert.True(BookViewCollection.First().Shelf.StartsWith("SF-"));

                // Selected and ordered in place: ToList copies each title into a string, once.
                Assert.Equal(["Dune", "Emma", "Foundation"], BookViewCollection.OrderBy(static book => book.Title).Select(static book => book.Title).ToList());
                Assert.Equal(["Emma", "Foundation"], BookViewCollection.Where(static book => book.Shelf != "SF-12").OrderByDescending(static book => book.Author).Select(static book => book.Title).ToList());

                // A string of it, when the code needs one: an interpolation, a key of a dictionary.
                var dune = BookViewCollection.Single(static book => book.Title == "Dune");
                Assert.Equal("Dune, by Frank Herbert", $"{dune.Title}, by {dune.Author}");
                Assert.Equal(3, BookViewCollection.ToDictionary(static book => book.Title.ToString()).Count);
            }
        }

        /// <summary>An amount in cents: the same operators, the code of a long.</summary>
        [Fact]
        public void CountInCents()
        {
            var price = Cents.From(990);
            Assert.Equal((Cents.From(1980), "19.80"), (price + price, (price + price).ToString()));
            Assert.True(Cents.From(850) < price);
            Assert.False(Cents.TryFrom(-1, out _));
        }
    }
}
