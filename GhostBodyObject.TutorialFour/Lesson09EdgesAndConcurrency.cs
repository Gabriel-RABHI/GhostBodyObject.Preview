using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;
using GhostBodyObject.Runtime.Physical.Transactions;

namespace TutorialFour.Social
{
    using static SocialRepositoryCollections;

    /// <summary>
    /// Lesson 9: edges and concurrency.
    /// <para>
    /// An edge is an aggregate of its own: its values conflict only with themselves. One transaction changes the date of a friendship
    /// while another renames one of the friends: two aggregates, both commit.
    /// </para>
    /// <para>
    /// Two transactions that make the same pair friends at the same time conflict at the second commit, which retries: a pair never
    /// ends up with two friendships. Written with <c>GetOrNew</c>, the retry runs the same code, and finds the friendship that the other
    /// transaction gave.
    /// </para>
    /// </summary>
    public sealed class Lesson09EdgesAndConcurrency
    {
        /// <summary>The date of the friendship and the name of Ada, changed by two transactions at once: both commit.</summary>
        [Fact]
        public async Task ChangeAnEdgeAndItsEndsConcurrently()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-parallel", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Person(PersonRef.New()) { Name = "Ada" }.NewFriendship(new Person(PersonRef.New()) { Name = "Bob" });
                transaction.Commit();
            }
            var firstChanged = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var dated = Task.Run(async () =>
            {
                using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
                FriendshipCollection.Single().Since = new DateTime(2019, 6, 1);
                firstChanged.SetResult();
                await secondCommitted.Task;
                transaction.Commit();
            });
            await firstChanged.Task;
            await Task.Run(() =>
            {
                using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
                PersonCollection.Single(static p => p.Name == "Ada").Name = "Ada Lovelace";
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await dated;
            using (social.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var friendship = FriendshipCollection.Single();
                Assert.Equal(new DateTime(2019, 6, 1), friendship.Since);
                Assert.Contains("Ada Lovelace", new[] { friendship.From.Name.ToString(), friendship.To.Name.ToString() });
            }
        }

        /// <summary>Two transactions make Ada and Bob friends: the second commit throws; retried, the same code finds the friendship.</summary>
        [Fact]
        public async Task ConflictOnTheSamePair()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-09-conflict", StoreAccessMode.ReadWrite);
            using var social = new SocialRepository(store);
            using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Person(PersonRef.New()) { Name = "Ada" };
                new Person(PersonRef.New()) { Name = "Bob" };
                transaction.Commit();
            }
            var firstGiven = new TaskCompletionSource();
            var secondCommitted = new TaskCompletionSource();
            var first = Task.Run(async () =>
            {
                using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
                Befriend("Ada", "Bob", new DateTime(2020, 1, 1));
                firstGiven.SetResult();
                await secondCommitted.Task;
                Assert.Throws<TransactionConflictException>(transaction.Commit);
            });
            await firstGiven.Task;
            await Task.Run(() =>
            {
                using var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite);
                Befriend("Bob", "Ada", new DateTime(2021, 1, 1));
                transaction.Commit();
            });
            secondCommitted.SetResult();
            await first;

            // The retry runs the same code: GetOrNew finds the friendship that the other transaction gave, and dates it.
            using (var transaction = social.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                Befriend("Ada", "Bob", new DateTime(2020, 1, 1));
                transaction.Commit();
            }
            using (social.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(new DateTime(2020, 1, 1), FriendshipCollection.Single().Since);
        }

        // Makes two people friends since a date: their friendship, or a new one. The same code for the first try and for the retry.
        private static void Befriend(Name first, Name second, Instant since)
        {
            var person = PersonCollection.Single(first, static (p, name) => p.Name == name);
            var other = PersonCollection.Single(second, static (p, name) => p.Name == name);
            person.FriendshipEdges.GetOrNew(other).Since = since;
        }
    }
}
