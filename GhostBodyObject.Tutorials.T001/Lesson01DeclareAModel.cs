using System.Globalization;
using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;

namespace Tutorials.T001
{
    // The model of the whole tutorial: a library of books.
    //
    // A model is a set of C# interfaces. You never implement them: the generator of GBO reads them at each build and writes
    // the classes you will use. An interface whose name starts with "M" gives a class without the "M": MBook gives Book.

    // ----- The values of the model, first -----
    //
    // Each value of a book has a type that says what it is: a value object. A decimal does not say whether it is a price or a rate, nor a
    // string whether it is a title or an author; a Money is not a Percent, and a title given for an author no longer compiles. You declare
    // a value object and its rule; the generator writes the rest, and the code that runs is the code of the value inside it. Part 6,
    // Lesson 19, explains the concept in depth and shows what GBO writes for them.

    /// <summary>The title of a book: a text, read in place, without copy.</summary>
    // [ValueObject<GhostString>] on an interface: a text value object, made from a string ("Dune"), compared to one, stored
    // as a text. GBO writes two types: Title, that a book reads and writes in place, and ReadOnlyTitle, that a BookView reads.
    [ValueObject<GhostString>]
    internal interface VTitle { }

    /// <summary>The name of an author.</summary>
    [ValueObject<GhostString>]
    internal interface VAuthorName { }

    /// <summary>The year of publication of a book.</summary>
    // [ValueObject<int>]: a number value object, stored as an int. ImplicitConversion = true: an int converts to it, Year = 1965, checked as
    // Year.From(1965) is; to an int, Value.
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VYear { }

    /// <summary>An amount of money, over a decimal: never negative, added and subtracted, never multiplied by another.</summary>
    // Operators: the operators between two amounts, + and -, by which Sum adds them. IsValid: the rule of a value, checked at each
    // conversion; an amount below zero does not exist.
    [ValueObject<decimal>(Operators = ValueOperators.Add | ValueOperators.Subtract, ImplicitConversion = true)]
    internal interface VMoney { }

    internal partial struct Money
    {
        private static bool IsValid(decimal amount) => amount >= 0;

        public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>The ISBN of a book: its 13 digits in a long, compared and stored as a long, its text built only when it is shown.</summary>
    [ValueObject<long>]
    internal interface VIsbn { }

    internal partial struct Isbn
    {
        private static bool IsValid(long digits) => digits is >= 0 and < 10_000_000_000_000;

        /// <summary>The ISBN of a text of 13 digits, hyphens apart; false for any other text: check the text, then write the ISBN.</summary>
        public static bool TryRead(string text, out Isbn isbn)
        {
            long digits = 0;
            int count = 0;
            foreach (char c in text)
                if (char.IsAsciiDigit(c) && ++count <= 13)
                    digits = digits * 10 + (c - '0');
                else if (c != '-')
                    count = int.MaxValue;
            isbn = default;
            return count == 13 && TryFrom(digits, out isbn);
        }

        public override string ToString() => Value.ToString("D13");
    }

    /// <summary>A rate of discount, in percent, over a decimal.</summary>
    [ValueObject<decimal>(ImplicitConversion = true)]
    internal interface VPercent { }

    internal partial struct Percent
    {
        private static bool IsValid(decimal value) => value is >= 0 and <= 100;

        public Money Of(Money amount) => amount.Value * Value / 100;
    }

    /// <summary>The mark of the shelf of a book, "SF-12": a text that checks its form.</summary>
    [ValueObject<GhostString>]
    internal interface VShelfMark { }

    internal partial struct ShelfMark
    {
        private static bool IsValid(ReadOnlySpan<char> text) => text.Length is > 2 and <= 8 && text.IndexOf('-') > 0;
    }

    /// <summary>Whether a book is on its shelf or borrowed: a state, an enum, not a bool.</summary>
    internal enum Availability
    {
        Available,
        Borrowed
    }

    // ----- The entities -----

    /// <summary>A book of the library: the model of the entity type <c>Book</c>.</summary>
    // [Entity(1, 1, 10)]: the type identifier of Book (1, unique among the types of the Repository), the version of its model
    // (1), and the highest identifier of its properties (10).
    // [Root]: a book lives by itself: GBO never removes it on its own.
    // [Removable]: the code may remove a book, with book.Remove().
    // [Enumerable]: the generator writes BookCollection, which enumerates the books.
    [Entity(1, 1, 10), Root, Removable, Enumerable]
    internal interface MBook
    {
        // [Property(n)]: the identifier of the property. GBO stores the value under its identifier, not under its name: you may
        // rename a property, but never give its identifier to another one.
        [Property(1)]
        VTitle Title { get; set; }

        [Property(2)]
        VAuthorName Author { get; set; }

        [Property(3)]
        VYear Year { get; set; }

        [Property(4)]
        VMoney Price { get; set; }

        [Property(5)]
        Availability Availability { get; set; }

        // The count of the real changes of the price, a raw counter that names no value: stored as any property, and kept by a rule of
        // Lesson 11.
        [Property(6)]
        int PriceChanges { get; set; }

        [Property(7)]
        VIsbn Isbn { get; set; }

        // A deposit and a discount, both optional: absent until given (Lesson 9).
        [Property(8)]
        Option<VMoney> Deposit { get; set; }

        [Property(9)]
        Option<VPercent> MemberDiscount { get; set; }

        [Property(10)]
        VShelfMark Shelf { get; set; }
    }

    /// <summary>The Repository of the library: the entity types it includes, here only <c>Book</c>.</summary>
    // [GhostRepository]: the generator writes LibraryRepository, its transaction, LibraryRepositoryTransaction, and the
    // collections of its entity types.
    // [IncludeEntityModel]: an entity type of the Repository, by the name of its model interface.
    [GhostRepository(1), IncludeEntityModel(nameof(MBook))]
    internal interface MLibraryRepository
    {
    }

    /// <summary>
    /// Lesson 1: declare a model.
    /// <para>
    /// GBO keeps your objects in a memory of its own, compact and transactional, that can be saved to disk. You describe your
    /// objects once, as interfaces; GBO writes the classes. This lesson shows what the generator wrote for the model above.
    /// </para>
    /// </summary>
    public sealed class Lesson01DeclareAModel
    {
        /// <summary>For <c>MBook</c>, the generator wrote a class <c>Book</c>, a struct <c>BookView</c>, and their collections.</summary>
        [Fact]
        public void TheGeneratorWritesTheTypesOfTheModel()
        {
            // Book is a class: the object you create, read and change. GBO calls it a Body.
            Assert.True(typeof(Book).IsClass);

            // BookView is a struct: a read-only view of a book, to read many books without creating an object for each one.
            // Lesson 10 shows it.
            Assert.True(typeof(BookView).IsValueType);

            // The type identifier of the model is on the class.
            Assert.Equal(1, Book.TypeId);

            // LibraryRepository is the class of the Repository; Lesson 2 opens one.
            Assert.Equal("LibraryRepository", LibraryRepository.Model.Name);
        }
    }
}
