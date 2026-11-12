// The diagrams of Tutorial T010. Each one draws the very objects of a test of its lesson: run `node T010-claims-points-and-leases.mjs` after
// a change of the lesson, or of the kit. Their words stay those of the developer: transactions, commits, claims, points, leases.
import { C, PAD, RIGHT, bar, card, check, code, cross, dot, line, link, node, owner, text } from "./kit.mjs";

// A timeline of a transaction, as in Tutorial T001: its name, and its line from its opening to its end.
const lane = (y, from, to, name) => [text(from - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }), line(from, y, to, y)];
const at = (x, y, s) => [dot(x, y, 4, C.text2), code(x, y - 10, s, { anchor: "middle", fill: C.text })];
const opens = (x, y) => [dot(x, y, 4, C.text2), text(x, y - 10, "opens", { size: 11, fill: C.muted, anchor: "middle" })];
const committed = (x, y, call = "Commit()") => [check(x, y, { r: 6 }), code(x, y - 12, call, { anchor: "middle", fill: C.text })];
const conflict = (x, y, kind, call = "Commit()") => [cross(x, y, { r: 6 }), code(x, y - 12, call, { anchor: "middle", fill: C.text }),
    text(x + 14, y + 4, kind, { size: 12, weight: 600, fill: C.red }), text(x + 14, y + 20, "writes nothing", { size: 11, fill: C.muted })];
const say = (y, name, lines) => [text(PAD, y, name, { weight: 600, fill: C.text }), lines.map((s, k) => text(PAD, y + 20 + k * 16, s, { size: 12 }))];
const note = (y, s) => text(PAD, y, s, { size: 11, fill: C.muted });

// Lesson 1, LeaveAHallEmpty and KeepAnUsherOnDutyWithAClaim: two decisions, two entities, one rule.
{
    const FROM = 280, END = 590, HALL = 722, ONE = 134, TWO = 288;
    const hall = (y, ada, bob, word, stroke) => [
        owner(HALL, y - 22, RIGHT - HALL, 96, "Main hall", word, { state: stroke }),
        node(HALL + 12, y + 16, 84, 44, "Ada", ada[0], { size: 12, state: ada[1] }),
        node(HALL + 108, y + 16, 84, 44, "Bob", bob[0], { size: 12, state: bob[1] }),
    ];
    const row = (y, name, lines, ada, bob, done, result) => [
        say(y - 4, name, lines),
        lane(y, FROM, END, "first"),
        opens(FROM, y),
        at(404, y, ada),
        done(END, y),
        lane(y + 52, 320, 510, "second"),
        opens(320, y + 52),
        at(410, y + 52, bob),
        committed(510, y + 52),
        result,
    ];
    card("T010-write-skew.svg", {
        h: 408,
        title: "Each decision is right on what it read, and the rule breaks",
        sub: "A hall keeps one usher on duty. Each transaction reads two, and writes its own usher",
        alt: "Two transactions read Ada and Bob on duty, and each one takes its own usher off duty. Without a claim both commit, because they wrote two different entities, and the hall has nobody on duty. When both claim the hall, the later commit conflicts, of the kind Claim: Ada stays on duty, and its next attempt refuses to leave.",
    }, [
        row(ONE, "Without a claim", ["Reads are not checked:", "two entities, no conflict"], "Ada leaves", "Bob leaves", committed,
            hall(ONE, ["off", "refused"], ["off", "refused"], "nobody on duty", "refused")),
        line(PAD, ONE + 100, RIGHT, ONE + 100, { stroke: C.grid }),
        row(TWO, "With a claim", ["Both claim the hall:", "the later commit conflicts"], "Claim(), Ada leaves", "Claim(), Bob leaves", (x, y) => conflict(x, y, "Claim"),
            hall(TWO, ["on duty", "held"], ["off", "alive"], "one on duty", "held")),
        note(392, "Run again, the first transaction reads one usher on duty, and refuses."),
    ]);
}

// Lesson 2, ConflictOnAnyUsherAtThePessimisticLevel and LeaveTheOtherHallAlone: what each tool watches.
{
    const W2 = 206, TOP = 134, LEFT = PAD, RIGHT2 = 502;
    const hall = (x, y, name, watched, ushers, state) => [
        owner(x, y, W2, 112, name, null, { state: watched ? "held" : "alive" }),
        ushers.map((u, i) => node(x + 12 + i * 94, y + 42, 88, 52, u, "usher", { size: 12, state })),
    ];
    card("T010-what-it-watches.svg", {
        h: 324,
        title: "Pessimistic watches a type, a claim watches an entity",
        sub: "In blue, what refuses the commit if it changes. The decision reads the main hall",
        alt: "At the Pessimistic level, the transaction consulted the type of the ushers: a change of any usher, of the main hall or of the small hall, refuses its commit. With a claim, the transaction watches one entity, the main hall: the ushers of the small hall never concern it, and a change of an usher that does not claim the hall is not seen either, because it writes the usher and not the hall.",
    }, [
        text(LEFT, 112, "Pessimistic", { size: 13, weight: 600, fill: C.text }),
        text(LEFT + 90, 112, "every usher of every hall", { size: 12 }),
        hall(LEFT, TOP, "Main hall", false, ["Ada", "Bob"], "held"),
        hall(LEFT + W2 + 14, TOP, "Small hall", false, ["Cleo", "Dan"], "held"),
        text(RIGHT2, 112, "hall.Claim()", { size: 13, weight: 600, fill: C.text, mono: true }),
        text(RIGHT2 + 104, 112, "the main hall, and nothing else", { size: 12 }),
        hall(RIGHT2, TOP, "Main hall", true, ["Ada", "Bob"], "alive"),
        hall(RIGHT2 + W2 + 14, TOP, "Small hall", false, ["Cleo", "Dan"], "alive"),
        note(278, "A claim sees what claims the hall too: two decisions that both claim it conflict, of the kind Claim."),
        note(296, "It does not see an usher changed without a claim: that change writes the usher, not the hall."),
    ]);
}

// Lesson 3, BreakARuleOfThePartsWithoutAPoint and KeepARuleOfThePartsWithAPoint: an aggregate, with and without a point.
{
    const W2 = 420, TOP = 110;
    const lines = (x, y, state) => [
        node(x + 16, y, 186, 48, "Carmen: 3 + 1", "first transaction", { size: 12, state }),
        node(x + 218, y, 186, 48, "Tosca: 2 + 1", "second transaction", { size: 12, state }),
    ];
    card("T010-concurrency-point.svg", {
        h: 320,
        title: "A concurrency point makes an aggregate one domain of conflict",
        sub: "Six tickets at most, all lines together. Each transaction reads five, and raises one line",
        alt: "A basket without a point: the first transaction writes the Carmen line, the second writes the Tosca line, nothing is written in common, both commit, and the basket holds seven tickets, breaking the rule. An order with ConcurrencyPoint: each change of a line marks the order, so both transactions write the order, and the second one conflicts, of the kind Domain. The order holds six tickets.",
    }, [
        owner(PAD, TOP, W2, 134, "Basket", "nothing in common", { state: "refused" }),
        lines(PAD, TOP + 56, "alive"),
        text(PAD + W2 / 2, TOP + 158, "both commit: 7 tickets", { size: 13, weight: 600, fill: C.red, anchor: "middle" }),
        owner(RIGHT - W2, TOP, W2, 134, "[ConcurrencyPoint] Order", "both mark it", { state: "held" }),
        lines(RIGHT - W2, TOP + 56, "held"),
        text(RIGHT - W2 / 2, TOP + 158, "the second conflicts, Domain: 6 tickets", { size: 13, weight: 600, fill: C.text, anchor: "middle" }),
        note(TOP + 192, "A line added or removed changes its Owner, in both. The point adds the values of the lines, at any depth, without anyone asking."),
    ]);
}

// Lesson 5, MarkTheNearestPoint and KeepTheLinesOfTwoOrdersApartAndTheirValuesTogether: points that nest.
{
    const X = PAD, Y = 100, W = 572, H = 204, INNER = 262, TALL = 148, LIST = 640;
    const order = (x, name) => [
        owner(x, Y + 40, INNER, TALL, `[ConcurrencyPoint] ${name}`, null, { state: "held" }),
        node(x + 12, Y + 94, 112, 44, "Line", "Carmen", { size: 12 }),
        node(x + 138, Y + 94, 112, 44, "Line", "Tosca", { size: 12 }),
        text(x + INNER / 2, Y + 166, "its lines mark it, and nothing higher", { size: 11, anchor: "middle" }),
    ];
    const rows = [["A value of a line", "its order"], ["A birth, a removal of a line", "its order"], ["A move to another order", "both orders"], ["A value of an order", "the customer"], ["A value of the customer", "the customer"]];
    card("T010-nested-points.svg", {
        h: 348,
        title: "Each change marks the nearest point, and stops there",
        sub: "A customer and its orders, each a concurrency point: the lines of two orders never meet",
        alt: "A customer holds two orders, and each order holds its lines; the customer and the orders are concurrency points. A value of a line, a birth or a removal marks its order, and nothing higher, so the lines of two orders never conflict. A move to another order marks both orders. A value of an order marks the customer, so the values of two orders of one customer conflict. A value of the customer marks the customer.",
    }, [
        owner(X, Y, W, H, "[ConcurrencyPoint] Customer", null, { state: "held" }),
        order(X + 16, "Order B-1"),
        order(X + 16 + INNER + 16, "Order B-2"),
        text(LIST, Y + 12, "The change", { size: 12, fill: C.muted }),
        text(RIGHT, Y + 12, "marks", { size: 12, fill: C.muted, anchor: "end" }),
        rows.map(([change, point], i) => [
            line(LIST, Y + 24 + i * 36, RIGHT, Y + 24 + i * 36, { stroke: C.grid }),
            text(LIST, Y + 46 + i * 36, change, { size: 12, fill: C.text }),
            text(RIGHT, Y + 46 + i * 36, point, { size: 12, fill: C.text2, anchor: "end" }),
        ]),
        note(332, "A write of mergeable values only marks nothing, and neither does a claim or a lease."),
    ]);
}

// Lesson 6, HoldASeatDuringAPayment, and Lesson 7, RefuseASeatThatAnotherTillHolds: a lease along the time.
{
    const FROM = 158, ONE = 134, THICK = 12, OTHER = 248, TAKEN = 300, BACK = 610, DONE = 830;
    card("T010-lease.svg", {
        h: 322,
        title: "A lease holds a seat between two transactions",
        sub: "A payment takes seconds, outside any transaction: nothing else stops, and only this seat is refused",
        alt: "A first transaction calls TryLock on seat 1, and its commit takes the lease. The payment runs outside any transaction, charged once by the key lease.Id. A transaction of the lease writes the buyer, calls Release, and its commit frees the seat. While the lease is active, another till that writes the seat is refused at its instruction, its transaction intact: it writes seat 2 and commits.",
    }, [
        lane(ONE, FROM, TAKEN, "the holder"),
        opens(FROM, ONE),
        at(228, ONE, "TryLock"),
        committed(TAKEN, ONE),
        bar(TAKEN + 12, BACK - 12, ONE - THICK / 2, THICK, C.blue),
        text((TAKEN + BACK) / 2, ONE - 14, "the payment, outside any transaction", { size: 12, fill: C.text2, anchor: "middle" }),
        text((TAKEN + BACK) / 2, ONE + 24, "charged once, by lease.Id", { size: 12, anchor: "middle" }),
        line(BACK, ONE, DONE, ONE),
        opens(BACK, ONE),
        at(722, ONE, "Buyer, Release()"),
        committed(DONE, ONE),

        lane(OTHER, 400, 664, "another till"),
        opens(400, OTHER),
        cross(500, OTHER, { r: 6 }),
        code(500, OTHER - 12, "Seat(1).Buyer =", { anchor: "middle", fill: C.text }),
        text(500, OTHER + 20, "GhostLockedException", { size: 12, weight: 600, fill: C.red, anchor: "middle" }),
        at(606, OTHER, ""),
        code(606, OTHER + 19, "Seat(2)", { anchor: "middle", fill: C.text }),
        committed(664, OTHER),
        text(682, OTHER + 4, "the rest of its transaction commits", { size: 12 }),
        note(306, "A lease not released ends with its duration, and the commit of its holder then fails: LeaseLost, nothing written."),
    ]);
}

// Lesson 9, LoseALeaseThatExpired: the end of a lease, and the fencing of its holder.
{
    const FROM = 158, ONE = 134, THICK = 12, TWO = 214, TAKEN = 262, ENDS = 490, BACK = 742;
    card("T010-fencing.svg", {
        h: 300,
        title: "A lease that ended never writes",
        sub: "The payment takes too long. Another till takes the seat, and the holder comes back",
        alt: "A commit takes the lease on seat 1 for five minutes. The lease ends while the payment runs. Another till then writes the seat, and commits. The holder comes back and opens a transaction of its lease: its commit is refused, of the kind LeaseLost, and nothing is written. There are never two holders of one seat.",
    }, [
        lane(ONE, FROM, TAKEN, "the holder"),
        opens(FROM, ONE),
        committed(TAKEN, ONE, "TryLock, Commit()"),
        bar(TAKEN + 12, ENDS, ONE - THICK / 2, THICK, C.blue),
        text((TAKEN + ENDS) / 2, ONE + 24, "the lease, five minutes", { size: 12, fill: C.text2, anchor: "middle" }),
        line(ENDS, ONE, BACK, ONE, { dash: "4 4" }),
        text((ENDS + BACK) / 2, ONE + 24, "the payment goes on", { size: 12, anchor: "middle" }),
        conflict(BACK, ONE, "LeaseLost", "OpenTransaction(lease)"),

        lane(TWO, 590, 690, "another till"),
        opens(590, TWO),
        committed(690, TWO),
        code(640, TWO + 22, 'Buyer = "Bob"', { anchor: "middle", fill: C.text }),

        line(ENDS, ONE + 10, ENDS, 252, { stroke: C.amber }),
        text(ENDS, 270, "the lease ends: the seat is free", { size: 12, fill: C.amber, anchor: "middle" }),
    ]);
}
