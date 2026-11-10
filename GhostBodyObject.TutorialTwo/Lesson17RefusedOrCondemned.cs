using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Rules
{
    using static LabelRepositoryCollections;

    /// <summary>
    /// Lesson 17: refused, or condemned.
    /// <para>
    /// An operation can fail in two ways, and only one of them leaves the transaction usable.
    /// </para>
    /// <para>
    /// <b>Refused.</b> Every operation of GBO, a Setter, a birth, <c>New</c>, <c>Add</c>, <c>Remove</c>, <c>MoveTo</c>, a
    /// cascade, checks all its refusals before its first write: a removal that a strong reference forbids, a slot already full,
    /// a removed target. The exception changes nothing; catch it, and the transaction goes on, and commits. Write your own
    /// rules the same way: check, then write, as <c>Album.AddTrack</c> does.
    /// </para>
    /// <para>
    /// <b>Condemned.</b> An exception thrown after a write, by a rule of yours that runs after its change, leaves the changes
    /// that the write began, and that nothing undoes. Catching it would let the code commit a state that the rule forbids: so
    /// the exception condemns the transaction. It reads still, but refuses its writes and its commit; <c>CondemnedBy</c> gives
    /// the exception. Only a rollback is left.
    /// </para>
    /// </summary>
    public sealed class Lesson17RefusedOrCondemned
    {
        /// <summary>A refusal, by GBO or by a rule that checks before it writes: caught, the transaction goes on and commits.</summary>
        [Fact]
        public void GoOnAfterARefusal()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-refused", StoreAccessMode.ReadWrite);
            using var label = new LabelRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var low = new Artist { Name = "David Bowie" }.Albums.New();
                low.Title = "Low";
                for (int i = 1; i <= Album.MaxTracks; i++)
                    low.AddTrack($"Track {i}", 180);

                // The rule of the label, checked before the write.
                Assert.Throws<InvalidOperationException>(() => low.AddTrack("One more", 180));

                // A refusal of GBO: a removed track refuses to be written.
                var removed = low.Tracks.First();
                low.Tracks.Remove(removed);
                Assert.Throws<InvalidOperationException>(() => removed.Seconds = 1);

                Assert.Null(transaction.CondemnedBy);
                transaction.Commit();
            }
            using (label.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(Album.MaxTracks - 1, TrackCollection.Count());
        }

        /// <summary>A rule that throws after its change: the transaction is condemned, even if the code catches the exception.</summary>
        [Fact]
        public void CondemnTheTransactionByAnExceptionAfterAWrite()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-17-condemned", StoreAccessMode.ReadWrite);
            using var label = new LabelRepository(store);
            using (var transaction = label.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var track = new Artist { Name = "David Bowie" }.Albums.New().Tracks.New();
                track.Title = "Speed of Life";

                // OnTitleChanged runs after the title is written, and throws: the empty title is written already.
                var thrown = Assert.Throws<InvalidOperationException>(() => track.Title = "");
                Assert.Same(thrown, transaction.CondemnedBy);

                // The transaction still reads; it refuses any write, and its commit.
                Assert.Equal("", track.Title);
                Assert.Throws<InvalidOperationException>(() => track.Seconds = 180);
                Assert.Throws<InvalidOperationException>(transaction.Commit);
            }

            // Nothing was committed.
            using (label.OpenTransaction(StoreAccessMode.ReadOnly))
                Assert.Equal(0, TrackCollection.Count());
        }
    }
}
