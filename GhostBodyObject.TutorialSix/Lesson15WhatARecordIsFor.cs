using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Log;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialSix.Bookings
{
    using static RoomRepositoryCollections;

    // The last model, of Lesson 15: a booking, as a message; the Log that keeps it, and the rooms that keep a copy of it.

    /// <summary>A room was booked: a fact, that a Log keeps.</summary>
    [Record(2, 1, 3)]
    internal interface RRoomBooked
    {
        [Property(1)]
        VName Guest { get; set; }

        [Property(2)]
        VRoomNumber Room { get; set; }

        [Property(3)]
        VInstant On { get; set; }
    }

    // A Log holds Records, and nothing else: Tutorial Seven shows it in detail.
    [GhostLog(6), IncludeRecordModel(nameof(RRoomBooked))]
    internal interface MBookingLog
    {
    }

    /// <summary>A stay of a room: a value of the Repository. A Record type belongs to one model: the Log keeps its fact, the room its own value.</summary>
    [Record(3, 1, 2)]
    internal interface RStay
    {
        [Property(1)]
        VName Guest { get; set; }

        [Property(2)]
        VInstant From { get; set; }
    }

    [Entity(1, 1, 2), Root, Enumerable]
    internal interface MRoom
    {
        [Property(1)]
        VRoomNumber Number { get; set; }

        [Property(2)]
        RecordBag<RStay> Bookings { get; set; }
    }

    [GhostRepository(5), IncludeEntityModel(nameof(MRoom)), IncludeRecordModel(nameof(RStay))]
    internal interface MRoomRepository
    {
    }

    /// <summary>
    /// Lesson 15: what a Record is for.
    /// <para>
    /// A Record is a value that moves: closer to a DTO or to a message than to an object of your domain. It needs no Store and no
    /// transaction to exist, it belongs to no one, it is copied whole from where it is to where it goes, on any thread.
    /// </para>
    /// <para>
    /// This is why a Log needs it. A Log keeps what happened, in order: a fact is a value, it has no identity, and it never changes. An
    /// event, a command, a Mutation, every entry of a Log is a Record: a <c>[GhostLog]</c> holds nothing else. A Record type belongs to
    /// one model: a Repository that keeps something of a fact declares its own Record, and writes it from the fact. Tutorial Seven starts from there.
    /// </para>
    /// </summary>
    public sealed class Lesson15WhatARecordIsFor
    {
        private static readonly DateTime Monday = new(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);

        /// <summary>A message, created on a thread, without a Store: a value, ready to go wherever it is sent.</summary>
        [Fact]
        public async Task CreateAMessageWithoutAStore()
        {
            var booked = await Task.Run(static () => new RoomBooked { Guest = "Ada", Room = 12, On = Monday });
            Assert.True(booked.Guest == "Ada" && booked.Room == 12);
        }

        /// <summary>A Record kept by a Log as a fact; the room that it is about writes a value of its own from it.</summary>
        [Fact]
        public void KeepARecordInALogAndInABag()
        {
            using var logStore = TransactionalMemoryStore.NewInMemoryLogStore("lesson-15-log", StoreAccessMode.ReadWrite);
            using var log = new BookingLog(logStore);
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-15-rooms", StoreAccessMode.ReadWrite);
            using var rooms = new RoomRepository(store);

            // Added to its Log, the Record is a fact: frozen, and kept in order.
            using (var transaction = log.OpenTransaction())
            {
                new RoomBooked { Guest = "Ada", Room = 12, On = Monday }.Add();
                transaction.Commit();
            }

            // The View of a Record is a value: the one that the Log delivers is kept after its subscription.
            var delivered = new List<RoomBookedView>();
            using var end = new CancellationTokenSource();
            log.SubscribeFrom(0, end.Token).Handle<RoomBookedView, List<RoomBookedView>>(delivered, static (_, booked, delivered) =>
            {
                delivered.Add(booked);
                return LogStoreSubscription.Continue;
            }).Run();
            end.Cancel();

            using (var transaction = rooms.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                new Room { Number = 12 }.Bookings.Add(new Stay { Guest = delivered.Single().Guest, From = delivered.Single().On });
                transaction.Commit();
            }

            using (rooms.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                Assert.True(RoomViewCollection.Single().Bookings.TryGetAt(0, out StayView kept));
                Assert.True(kept.Guest == "Ada" && kept.From == Monday);
            }
        }
    }
}
