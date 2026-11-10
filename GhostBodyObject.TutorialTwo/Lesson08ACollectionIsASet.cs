using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Sets
{
    using static RadioRepositoryCollections;

    // The fourth model: a radio station and its broadcasts, as many as the years give.

    /// <summary>The day of a broadcast, from the first day of the station: a value of this model alone.</summary>
    [ValueObject<int>(ImplicitConversion = true)]
    internal interface VDay { }

    [Entity(1, 1, 2), Root, Removable, Enumerable]
    internal interface MStation
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MBroadcast> Broadcasts { get; }
    }

    [Entity(2, 1, 2)]
    internal interface MBroadcast
    {
        // The business order of the broadcasts is a property: the collection has none.
        [Property(1)]
        VDay Day { get; set; }

        [Property(2)]
        VTitle Title { get; set; }

        [OwnedFrom]
        MStation Station { get; }
    }

    [GhostRepository(3), IncludeEntityModel(nameof(MStation)), IncludeEntityModel(nameof(MBroadcast))]
    internal interface MRadioRepository
    {
    }

    /// <summary>
    /// Lesson 8: a collection is a set, of any size.
    /// <para>
    /// A collection is a set, not a list: it has no position, and holds a part once. It enumerates in the order that is the
    /// fastest to read, which GBO chooses: not the order of creation, nor any order the code may rely on. The business order
    /// is a property, read by <c>OrderBy</c>.
    /// </para>
    /// <para>
    /// A collection has no limit of size, and the same code holds three parts or a hundred thousand: <c>Count()</c> stays
    /// exact and immediate, and <c>Contains</c> finds a part without reading the others.
    /// </para>
    /// </summary>
    public sealed class Lesson08ACollectionIsASet
    {
        /// <summary>The business order is a property: <c>OrderBy</c> gives it, whatever the order of the collection.</summary>
        [Fact]
        public void OrderACollectionByAProperty()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-order", StoreAccessMode.ReadWrite);
            using var radio = new RadioRepository(store);
            using var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite);
            var station = new Station { Name = "Jazz FM" };
            foreach (int day in new[] { 3, 1, 2 })
            {
                var broadcast = station.Broadcasts.New();
                broadcast.Day = day;
                broadcast.Title = $"Day {day}";
            }

            Assert.Equal(["Day 1", "Day 2", "Day 3"], station.Broadcasts.OrderBy(static b => b.Day).Select(static b => b.Title).ToList());
            transaction.Commit();
        }

        /// <summary>100,000 broadcasts in one station: the same code as for three, in the transaction and after the commit.</summary>
        [Fact]
        public void HoldAsManyPartsAsNeeded()
        {
            const int count = 100_000;
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-08-large", StoreAccessMode.ReadWrite);
            using var radio = new RadioRepository(store);
            using (var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var station = new Station { Name = "Jazz FM" };
                for (int day = 0; day < count; day++)
                    station.Broadcasts.New().Day = day;
                var middle = station.Broadcasts.Single(static b => b.Day == count / 2);
                Assert.Equal(count, station.Broadcasts.Count());
                Assert.True(station.Broadcasts.Contains(middle));
                transaction.Commit();
            }
            using (var transaction = radio.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var station = StationCollection.Single();
                Assert.Equal(count, station.Broadcasts.Count());
                Assert.Equal((long)count * (count - 1) / 2, station.Broadcasts.Sum(static b => (long)b.Day));

                // A removal in the middle: the count follows at once.
                station.Broadcasts.Remove(station.Broadcasts.Single(static b => b.Day == count / 2));
                Assert.Equal(count - 1, station.Broadcasts.Count());
                transaction.Commit();
            }
        }
    }
}
