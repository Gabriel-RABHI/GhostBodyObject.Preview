using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T010
{
    using static Lesson03ARuleOverTheParts;

    /// <summary>
    /// Lesson 4: the version of a domain.
    /// <para>
    /// A point carries <c>ConcurrencyVersion</c>, which every change of its domain raises, read on its View and on its Body, never written. It says
    /// whether a domain changed since a moment, whatever changed in it, without reading its parts.
    /// </para>
    /// <para>
    /// It serves what no transaction can hold: a client that reads an order now, and writes it minutes later. A transaction does not stay open
    /// between two requests. The page keeps the version of the order, as an ETag, and the request that writes compares it, as an <c>If-Match</c>
    /// of HTTP: the order is no longer the one that the client saw, and the application refuses, or shows it again.
    /// </para>
    /// </summary>
    public sealed class Lesson04TheVersionOfADomain
    {
        /// <summary>A client reads an order, another request changes a line, and the version that the client kept no longer matches.</summary>
        [Fact]
        public void CheckTheVersionThatAClientRead()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-version", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);

            // The page of the client shows the order, and keeps its version.
            ulong version;
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                version = OrderOf("B-1").ConcurrencyVersion;

            // Another request changes a line: the version of the order moves, though no value of the order changed.
            AnotherTill.Commits(boxOffice, static () => AddATicket(OrderOf("B-1").Lines, "Tosca"));

            // The request of the client compares: this order is not the one that the client saw.
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.NotEqual(version, OrderOf("B-1").ConcurrencyVersion);
        }

        /// <summary>The version compared at the start of the transaction that writes: a change that comes after the comparison still conflicts at the commit.</summary>
        [Fact]
        public void WriteOnTheVersionThatAClientRead()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-04-write", StoreAccessMode.ReadWrite);
            using var boxOffice = new BoxOfficeRepository(store);
            Open(boxOffice);
            ulong version;
            using (boxOffice.OpenTransaction(StoreAccessMode.ReadOnly))
                version = OrderOf("B-1").ConcurrencyVersion;
            using (var transaction = boxOffice.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The version matches: the client saw this order. The change marks the point, so a change made from here on conflicts.
                Assert.Equal(version, OrderOf("B-1").ConcurrencyVersion);
                Assert.True(AddATicket(OrderOf("B-1").Lines, "Carmen"));
                AnotherTill.Commits(boxOffice, static () => AddATicket(OrderOf("B-1").Lines, "Tosca"));
                Assert.False(transaction.TryCommit(out var kind));
                Assert.Equal(TransactionConflictKind.Domain, kind);
            }
        }
    }
}
