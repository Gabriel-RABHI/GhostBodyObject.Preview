using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace Tutorials.T003.Contacts
{
    using static ClientRepositoryCollections;

    // The model of Lesson 11: the clients of a delivery service, their addresses, the history of their moves, and the messages that
    // the services of the business leave for them.

    // The values of this model alone, first; the others are those of the tutorial.

    /// <summary>The street of an address, its number included.</summary>
    [ValueObject<GhostString>]
    internal interface VStreet { }

    /// <summary>The city of an address.</summary>
    [ValueObject<GhostString>]
    internal interface VCity { }

    /// <summary>The postal code of an address.</summary>
    [ValueObject<GhostString>]
    internal interface VPostalCode { }

    /// <summary>A day of the calendar.</summary>
    [ValueObject<DateOnly>(ImplicitConversion = true)]
    internal interface VDay { }

    /// <summary>The subject of a brochure.</summary>
    [ValueObject<GhostString>]
    internal interface VSubject { }

    /// <summary>A postal address: several values that go together, read and replaced whole, without an identity of its own.</summary>
    [Record(2, 1, 3)]
    internal interface RAddress
    {
        [Property(1)]
        VStreet Street { get; set; }

        [Property(2)]
        VCity City { get; set; }

        [Property(3)]
        VPostalCode PostalCode { get; set; }
    }

    /// <summary>A move of a client: its day, and the address that the client left. A Record holds Records too.</summary>
    [Record(3, 1, 2)]
    internal interface RMove
    {
        [Property(1)]
        VDay On { get; set; }

        [Property(2)]
        RAddress From { get; set; }
    }

    /// <summary>A message for the call center: call the client back.</summary>
    [Record(4, 1, 1)]
    internal interface RCallBack
    {
        [Property(1)]
        VReason Reason { get; set; }
    }

    /// <summary>A message for the mailing service: send the client a brochure.</summary>
    [Record(5, 1, 1)]
    internal interface RSendBrochure
    {
        [Property(1)]
        VSubject Subject { get; set; }
    }

    [Entity(1, 1, 5), Root, Enumerable]
    internal interface MClient
    {
        [Property(1)]
        VName Name { get; set; }

        /// <summary>Where the client lives: never absent, an empty address until written.</summary>
        [Property(2)]
        RAddress Home { get; set; }

        /// <summary>Where the invoices go, when it is not home.</summary>
        [Property(3)]
        Option<RAddress> Billing { get; set; }

        /// <summary>The moves of the client, in their order: a bag of one type of Record.</summary>
        [Property(4)]
        RecordBag<RMove> Moves { get; set; }

        /// <summary>The messages that the services leave for the client: a bag of any Record.</summary>
        [Property(5)]
        RecordBag Inbox { get; set; }
    }

    // The messages of the inbox are named by no property of the model: the Repository includes them by their names.
    [GhostRepository(4), IncludeEntityModel(nameof(MClient)), IncludeRecordModel(nameof(RCallBack)), IncludeRecordModel(nameof(RSendBrochure)), IncludeRecordModel(nameof(RAddress)), IncludeRecordModel(nameof(RMove))]
    internal interface MClientRepository
    {
    }

    /// <summary>
    /// Lesson 11: values that travel together, the Records.
    /// <para>
    /// A value object holds one value: a city, a day. An address is several of them that always go together: a street, a city, a postal
    /// code. A Record declares them once, <c>RAddress</c>, and any entity, or any other Record, holds it as one property: the home of a
    /// client, the address that a move left. A Record has no identity and no link: it is a value, read whole, replaced whole. Its
    /// property is never absent, an empty address until written; an <c>Option</c> of it may be absent.
    /// </para>
    /// <para>
    /// A list of such values that the entity owns, the history of its moves, is a bag of Records: <c>RecordBag&lt;RMove&gt;</c>, its
    /// Records in their order. A bag of any Record, <c>RecordBag</c>, carries messages for several services: each one reads the types
    /// it knows, and leaves the others.
    /// </para>
    /// <para>
    /// A Record is not a part (Tutorial T006 goes through the Records). The lines of an order, changed one by one, found, referenced, are parts of their aggregate (Tutorial T002);
    /// a Record is the value that several places reuse. Keep few of them in an entity: an entity of many Records couples the evolution
    /// of all their hosts, and none of their fields is indexed.
    /// </para>
    /// </summary>
    public sealed class Lesson11ValuesThatTravelTogether
    {
        /// <summary>An address, born on its own, assigned to a client, then changed by its copy: the value read before stays the same.</summary>
        [Fact]
        public void AnAddressIsAValue()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-address", StoreAccessMode.ReadWrite);
            using var clients = new ClientRepository(store);
            using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // A Record is born anywhere, then assigned: its values are copied into the client.
                var ada = new Client { Name = "Ada" };
                Assert.Equal("", ada.Home.City.ToString());
                ada.Home = new Address { Street = "1 rue de Rivoli", City = "Paris", PostalCode = "75001" };
                Assert.False(ada.Billing.Any());
                transaction.Commit();
            }
            using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var ada = ClientCollection.Single();
                var before = ada.Home;
                // To change one of its values: copy the address, change the copy, assign it back.
                var home = ada.Home.ToBody();
                home.City = "Lyon";
                home.PostalCode = "69001";
                ada.Home = home;
                Assert.Equal("Lyon", ada.Home.City.ToString());
                // The address read before is a value: it has not changed.
                Assert.Equal("Paris", before.City.ToString());
                // The copy is a Record of its own: the client sees its next change only when it is assigned again.
                home.Street = "2 quai Saint-Antoine";
                Assert.Equal("1 rue de Rivoli", ada.Home.Street.ToString());
                ada.Billing = home;
                transaction.Commit();
            }
            using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // Read through the Views, an address is read in place, as any value of its entity.
                Assert.Equal(1, ClientViewCollection.Count(static c => c.Home.City == "Lyon"));
                var ada = ClientViewCollection.Single();
                Assert.True(ada.Billing.TryGet(out var billing) && billing.Street == "2 quai Saint-Antoine" && billing.City == "Lyon");
            }
        }

        /// <summary>The moves of a client in a bag of Records: added in their order, read by their type, each one holding the address it left.</summary>
        [Fact]
        public void KeepAHistoryInABag()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-history", StoreAccessMode.ReadWrite);
            using var clients = new ClientRepository(store);
            using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var ada = new Client { Name = "Ada", Home = new Address { City = "Paris" } };
                // A move: the address left joins the history, then the new one is assigned. The bag of the client changes the client.
                ada.Moves.Add(new Move { On = new DateOnly(2024, 3, 1), From = ada.Home });
                ada.Home = new Address { City = "Lyon" };
                ada.Moves.Add(new Move { On = new DateOnly(2026, 9, 15), From = ada.Home });
                ada.Home = new Address { City = "Nice" };
                transaction.Commit();
            }
            using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                var ada = ClientViewCollection.Single();
                Assert.Equal(2, ada.Moves.Count());
                // Enumerate gives the Records of a type in their order, with the operators of the sequences.
                Assert.Equal(["Paris", "Lyon"], ada.Moves.Enumerate<MoveView>().Select(static m => m.From.City.ToString()).ToList());
                Assert.True(ada.Moves.Enumerate<MoveView>().LastOrNone().TryGet(out var last) && last.On == new DateOnly(2026, 9, 15));
                Assert.Equal("Nice", ada.Home.City.ToString());
            }
        }

        /// <summary>
        /// An inbox for several services: the call center and the mailing service each read their own messages, take one, and leave the
        /// others in place.
        /// </summary>
        [Fact]
        public void CarryMessagesForSeveralServices()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-11-inbox", StoreAccessMode.ReadWrite);
            using var clients = new ClientRepository(store);
            using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                var ada = new Client { Name = "Ada" };
                ada.Inbox.Add(new SendBrochure { Subject = "Winter offers" });
                ada.Inbox.Add(new CallBack { Reason = "Damaged parcel" });
                ada.Inbox.Add(new SendBrochure { Subject = "New depots" });
                transaction.Commit();
            }
            using (clients.OpenTransaction(StoreAccessMode.ReadOnly))
            {
                // The mailing service knows one type: the other messages are skipped.
                var inbox = ClientViewCollection.Single().Inbox;
                Assert.Equal(3, inbox.Count());
                Assert.Equal(["Winter offers", "New depots"], inbox.Enumerate<SendBrochureView>().Select(static b => b.Subject.ToString()).ToList());
                // A service that knows both walks the inbox once, a handler per type; the handlers capture nothing, their state comes first.
                var work = new List<string>();
                inbox.Enumerate(work,
                    static (List<string> w, SendBrochureView brochure) => w.Add("mail " + brochure.Subject),
                    static (List<string> w, CallBackView call) => w.Add("call " + call.Reason));
                Assert.Equal(["mail Winter offers", "call Damaged parcel", "mail New depots"], work);
            }
            using (var transaction = clients.OpenTransaction(StoreAccessMode.ReadWrite))
            {
                // The call center takes its message: found by its type, read, then removed; the others stay in their order.
                var ada = ClientCollection.Single();
                Assert.True(ada.Inbox.IndexOf<CallBackView>(out int at) && ada.Inbox.TryGetAt(at, out CallBackView call));
                ada.Inbox.RemoveAt(at);
                Assert.Equal("Damaged parcel", call.Reason.ToString());
                // The mailing service takes the first message, as long as there is one.
                var sent = new List<string>();
                while (ada.Inbox.Count() > 0)
                {
                    if (ada.Inbox.TryGetAt(0, out SendBrochureView brochure))
                        sent.Add(brochure.Subject.ToString());
                    ada.Inbox.RemoveAt(0);
                }
                Assert.Equal(["Winter offers", "New depots"], sent);
                transaction.Commit();
            }
        }
    }
}
