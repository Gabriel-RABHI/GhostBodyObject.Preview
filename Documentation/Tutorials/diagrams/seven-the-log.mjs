// The diagrams of Tutorial Seven. Each one draws the very objects of a test of its lesson: run `node seven-the-log.mjs` after a
// change of the lesson, or of the kit. Their words stay those of the developer: Records, Logs, subscriptions, handlers, transactions.
import { C, PAD, RIGHT, block, card, code, count, cross, dot, line, link, node, owner, rect, ring, steps, text } from "./kit.mjs";

const B = (s, o) => ({ s, size: 13, weight: 600, fill: C.text, ...o }), M = (s, o) => ({ s, mono: true, ...o }), S = (s, o) => ({ s, size: 11, ...o });

// Lesson 1, AppendTheRecordsOfACommit and AppendNothingWithoutACommit: what reaches the Log, and what does not.
{
    const TX = 186, BOX = 278, LOG = 650, GAP = LOG - TX - BOX, INNER = 121, TALL = 48, HEIGHT = 92;
    const records = (x, y, list, state) => list.map(([name, sub], i) => node(x + 12 + i * (INNER + 12), y + 32, INNER, TALL, name, sub, { size: 12, state }));
    const row = (y, name, say, list, call, kept) => [
        text(PAD, y + 40, name, { weight: 600, fill: C.text }),
        say.map((s, i) => text(PAD, y + 60 + i * 16, s, { size: 12 })),
        owner(TX, y, BOX, HEIGHT, "The transaction", "ParcelCollection"),
        records(TX, y, list),
        code(TX + BOX + GAP / 2, y + 46, call, { anchor: "middle", fill: C.text }),
        kept ? link([[TX + BOX, y + 56], [LOG, y + 56]]) : link([[TX + BOX, y + 56], [LOG - 44, y + 56]], { weak: true, head: false, stroke: C.muted }),
        owner(LOG, y, BOX, HEIGHT, "The Log", null, { state: kept ? "alive" : "gone" }),
        kept ? records(LOG, y, list, "held") : text(LOG + BOX / 2, y + 60, "nothing", { fill: C.text, anchor: "middle" }),
    ];
    card("seven-commit-or-nothing.svg", {
        h: 356,
        title: "A commit appends its Records together, or nothing is appended",
        sub: "The Records of a transaction, and what reaches the Log",
        alt: "Two transactions, each with a ParcelRegistered and a ParcelScanned, held by ParcelCollection in the order of their Add. transaction.Commit() appends both to the Log. transaction.Rollback() appends nothing, and the Log stays empty. Until the commit, the Log has none of them, and a transaction disposed without a commit is rolled back.",
    }, [
        row(100, "Committed", ["Both Records, in", "the order of their Add"], [["ParcelRegistered", "PX-1001, Ada"], ["ParcelScanned", "PX-1001, Lyon"]], "transaction.Commit()", true),
        line(PAD, 212, RIGHT, 212, { stroke: C.grid }),
        row(100 + 124, "Rolled back", ["The Log has", "none of them"], [["ParcelRegistered", "PX-1002, Bob"], ["ParcelScanned", "PX-1002, Lyon"]], "transaction.Rollback()", false),
        text(PAD, 338, "Until the commit, the Log has none of them. A transaction disposed without a commit is rolled back.", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 2, the three subscriptions of the lesson over Journey: every type, one type, a Cancel.
{
    const R = 140, X = [262, 410, 594, 778], mid = X.map(x => x + R / 2), TOP = 100, FROM = 252, LANE = 232, HEIGHT = 76;
    const lane = (y, name, decl, marks, end) => [
        text(PAD, y - 6, name, { weight: 600, fill: C.text }),
        code(PAD, y + 13, decl),
        line(FROM, y, end ?? RIGHT, y),
        marks.map(([s, k]) => s === null ? [dot(mid[k], y, 4.5, C.surface), ring(mid[k], y, 4.5, C.muted)] : dot(mid[k], y, 4.5, C.blue)),
        marks.map(([s, k]) => s === null ? text(mid[k], y + 20, "skipped", { size: 11, fill: C.muted, anchor: "middle" }) : code(mid[k], y - 12, s, { anchor: "middle", fill: C.text })),
    ];
    card("seven-subscriptions.svg", {
        h: 426,
        title: "A subscription walks the Log, each Record to the handler of its type",
        sub: "The journey of a parcel, in three commits, and the three subscriptions of the lesson",
        alt: "The Log holds four Records in three commits: ParcelRegistered and ParcelScanned at Lyon in the first, ParcelScanned at Paris in the second, ParcelDelivered in the third. A subscription with a handler for each type receives all four, in the order of the Log. A subscription with a handler for ParcelScannedView alone receives Lyon and Paris, and skips the two others. A subscription whose handler answers Cancel skips the first Record, receives Lyon, and stops there: the Records after it are not delivered, and Completion is IsCompletedSuccessfully.",
    }, [
        text(PAD, 138, "The Log", { weight: 600, fill: C.text }),
        code(PAD, 157, "Journey(log)"),
        owner(FROM, TOP, 308, 82, "First commit"),
        owner(584, TOP, 160, 82, "Second commit"),
        owner(768, TOP, 160, 82, "Third commit"),
        node(X[0], TOP + 28, R, 44, "ParcelRegistered", "PX-2001, Ada", { size: 12 }),
        node(X[1], TOP + 28, R, 44, "ParcelScanned", "PX-2001, Lyon", { size: 12 }),
        node(X[2], TOP + 28, R, 44, "ParcelScanned", "PX-2001, Paris", { size: 12 }),
        node(X[3], TOP + 28, R, 44, "ParcelDelivered", "PX-2001", { size: 12 }),
        lane(LANE, "A handler for each type", "three Handle<TView>", [["registered for Ada", 0], ["scanned at Lyon", 1], ["scanned at Paris", 2], ["delivered", 3]]),
        line(PAD, LANE + 40, RIGHT, LANE + 40, { stroke: C.grid }),
        lane(LANE + HEIGHT, "A handler for one type", "Handle<ParcelScannedView>", [[null, 0], ["Lyon", 1], ["Paris", 2], [null, 3]]),
        line(PAD, LANE + HEIGHT + 40, RIGHT, LANE + HEIGHT + 40, { stroke: C.grid }),
        lane(LANE + 2 * HEIGHT, "A handler that answers Cancel", "LogStoreSubscription.Cancel", [[null, 0], ["Lyon", 1]], mid[1]),
        line(mid[1] + 8, LANE + 2 * HEIGHT - 7, mid[1] + 8, LANE + 2 * HEIGHT + 7, { stroke: C.text2, sw: 2 }),
        text(mid[2] - R / 2, LANE + 2 * HEIGHT - 4, "Cancel: the Records after it are not delivered", { size: 12, fill: C.text }),
        code(mid[2] - R / 2, LANE + 2 * HEIGHT + 15, "Completion.IsCompletedSuccessfully"),
    ]);
}

// Lesson 3, CreateARecordAnywhereAndAddItLater: two Records created in one order, added in the other.
{
    const TOP = 100, HEIGHT = 170, A = 32, AW = 292, X2 = 408, W2 = 194, X3 = 718, W3 = 198, ROW = [134, 200], TALL = 54;
    const c = i => ROW[i] + TALL / 2;
    card("seven-order-of-the-add.svg", {
        h: 306,
        title: "The order of the Log is the order of the Add",
        sub: "Two Records created on a thread of their own, then added in the other order",
        alt: "A scanner creates two Records on a thread of its own, without any transaction: lyon first, then paris, whose depot it corrects from Pari to Paris. The transaction takes paris first, by paris.Add(), then lyon, by lyon.Add(): ParcelCollection gives paris, then lyon. The commit appends them in that order: the Log holds Paris, then Lyon.",
    }, [
        owner(A, TOP, AW, HEIGHT, "A thread of its own", "no transaction: detached"),
        owner(X2 - 12, TOP, W2 + 24, HEIGHT, "The transaction", "ParcelCollection"),
        owner(X3 - 12, TOP, W3 + 24, HEIGHT, "The Log"),
        block(A + 12, ROW[0], AW - 24, TALL, [M(`new ParcelScanned { Depot = "Lyon" }`, { fill: C.text })]),
        block(A + 12, ROW[1], AW - 24, TALL, [M(`new ParcelScanned { Depot = "Pari" }`, { fill: C.text }), M(`paris.Depot = "Paris"`, { fill: C.text })]),
        link([[A + AW - 12, c(0)], [X2, c(1)]]),
        link([[A + AW - 12, c(1)], [X2, c(0)]]),
        node(X2, ROW[0], W2, TALL, "paris.Add()", "Paris, first", { mono: true, size: 12 }),
        node(X2, ROW[1], W2, TALL, "lyon.Add()", "Lyon, second", { mono: true, size: 12 }),
        link([[X2 + W2, c(0)], [X3, c(0)]]),
        link([[X2 + W2, c(1)], [X3, c(1)]]),
        code((X2 + W2 + X3) / 2, (c(0) + c(1)) / 2 + 4, "Commit()", { anchor: "middle", fill: C.text }),
        node(X3, ROW[0], W3, TALL, "Paris", null, { state: "held" }),
        node(X3, ROW[1], W3, TALL, "Lyon", null, { state: "held" }),
    ]);
}

// Lesson 3, FreezeARecordOnceAdded: the value of Depot at each instruction.
{
    const STEPS = [
        { code: ["new ParcelScanned", `{ Depot = "Lyon" }`], depot: "Lyon" },
        { code: ["scanned.Depot", `= "Lille"`], depot: "Lille" },
        { code: ["scanned.Add()"], depot: "Lille" },
        { code: ["scanned.Depot", `= "Paris"`], depot: "Lille", refused: true },
        { code: ["transaction", ".Commit()"], depot: "Lille", log: true },
        { code: ["scanned.Depot", `= "Paris"`], depot: "Lille", refused: true, log: true },
    ];
    const COL = 138, GAP = 13.6, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, BOX = 164, TALL = 44, BAND = 240, LOGY = 280;
    card("seven-frozen.svg", {
        h: 352,
        title: "A Record is written until its Add, and frozen after it",
        sub: "The value of its Depot at each instruction, and what the Log holds",
        alt: "Six instructions. A ParcelScanned is created with the depot Lyon: it is detached. scanned.Depot = \"Lille\" changes it. scanned.Add() makes it a fact, frozen. scanned.Depot = \"Paris\" is refused, and the depot stays Lille. transaction.Commit() appends it: the Log holds Lille. scanned.Depot = \"Paris\" is refused again, after the commit: the Record keeps Lille.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => [
            s.code.map((c, k) => code(mid(i), 118 + k * 16, c, { anchor: "middle", fill: C.text })),
            node(x0(i), BOX, COL, TALL, s.depot, null, { state: s.refused ? "refused" : "alive" }),
            s.refused && text(mid(i), BOX + TALL + 20, "refused", { size: 12, weight: 600, fill: C.red, anchor: "middle" }),
            s.log && node(x0(i), LOGY, COL, TALL, "Lille", null, { state: "held" }),
        ]),
        rect(x0(0), BAND, x0(1) + COL - x0(0), 26, { fill: C.raised, stroke: C.base, rx: 6 }),
        text((x0(0) + x0(1) + COL) / 2, BAND + 17, "Detached: written as many times as needed", { size: 12, anchor: "middle" }),
        rect(x0(2), BAND, x0(5) + COL - x0(2), 26, { fill: C.blueSoft, stroke: C.blue, rx: 6 }),
        text((x0(2) + x0(5) + COL) / 2, BAND + 17, "A fact: frozen, in its transaction and after it", { size: 12, anchor: "middle", fill: C.text }),
        text(PAD, 334, "The Log holds the value written before the Add", { size: 11, fill: C.muted }),
        text(x0(4) - 12, LOGY + 27, "The Log", { weight: 600, fill: C.text, anchor: "end" }),
    ]);
}

// Lesson 6, DeliverTheNewCommitsAfterTheReplay: the replay, then each new commit, then the token cancelled.
{
    const COL = 110, GAP = 8, INNER = 12, TOP = 100, ROW = 136, TALL = 46, BOARD = 206, X = [32, 295, 676];
    const cell = (phase, k) => X[phase] + INNER + k * (COL + GAP), cx = (phase, k) => cell(phase, k) + COL / 2;
    const record = (p, k, sub, o) => node(cell(p, k), ROW, COL, TALL, "ParcelScanned", sub, { size: 12, ...o });
    const delivered = (p, k) => link([[cx(p, k), ROW + TALL], [cx(p, k), BOARD]]);
    const board = (p, k, lines) => block(cell(p, k), BOARD, COL, TALL, lines.map(l => S(l, { fill: C.text })), { state: "held" });
    const width = n => 2 * INNER + n * COL + (n - 1) * GAP;
    card("seven-follow.svg", {
        h: 302,
        title: "Run delivers what is there, then a thread follows the new commits",
        sub: "Each commit of the Log, and the board after its delivery",
        alt: "The Log holds a scan of PX-6001 at Lyon. Run delivers it on the calling thread, the board shows PX-6001 at Lyon, OnReplayEnd gives the number of the last transaction, and Run returns. The subscription then delivers each new commit on a thread of its own: PX-6001 at Paris, PX-6002 at Lille, then PX-6002 delivered, which leaves PX-6001 at Paris. end.Cancel() ends the subscription, its Completion cancelled: the scan at Nice reaches it no more, and the board stays at Paris.",
    }, [
        owner(X[0], TOP, width(2), 164, "The replay, in Run()", "the calling thread"),
        owner(X[1], TOP, width(3), 164, "Each new commit", "on a thread of its own, at once"),
        owner(X[2], TOP, width(2), 164, "The token cancelled", "the subscription ends", { state: "gone" }),
        record(0, 0, "PX-6001, Lyon"), delivered(0, 0), board(0, 0, ["PX-6001 at Lyon"]),
        block(cell(0, 1), ROW, COL, TALL, [M("OnReplayEnd", { fill: C.text, size: 11 }), S("the end of the replay")]),
        link([[cx(0, 1), ROW + TALL], [cx(0, 1), BOARD]]),
        block(cell(0, 1), BOARD, COL, TALL, [M("ReadyAt > 0", { fill: C.text, size: 11 })], { state: "held" }),
        record(1, 0, "PX-6001, Paris"), delivered(1, 0), board(1, 0, ["PX-6001 at Paris"]),
        record(1, 1, "PX-6002, Lille"), delivered(1, 1), board(1, 1, ["PX-6001 at Paris", "PX-6002 at Lille"]),
        node(cell(1, 2), ROW, COL, TALL, "ParcelDelivered", "PX-6002", { size: 12 }), delivered(1, 2), board(1, 2, ["PX-6001 at Paris"]),
        block(cell(2, 0), ROW, COL, TALL, [M("end.Cancel()", { fill: C.text, size: 11 })]),
        block(cell(2, 0), BOARD, COL, TALL, [M("Completion", { size: 11 }), M(".IsCanceled", { size: 11 })]),
        link([[cx(2, 0), ROW + TALL], [cx(2, 0), BOARD]]),
        record(2, 1, "PX-6001, Nice", { state: "gone" }),
        board(2, 1, ["PX-6001 at Paris"]),
        text(cx(2, 1), ROW + TALL + 16, "not delivered", { size: 11, fill: C.muted, anchor: "middle" }),
        text(PAD, 284, "The handlers are never called together: the replay and each delivery after it, one at a time", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 7, FrameEachTransactionThenTheReplay: every call of the subscription, in its order.
{
    const COL = 96, GAP = 6, INNER = 12, TOP = 106, X = [32, 470], FOOT = 304, ROW = TOP + 32, TALL = 50, HEIGHT = 176;
    const cell = (frame, k) => X[frame] + INNER + k * (COL + GAP);
    const frame = (i, cells, name, sub) => {
        const w = 2 * INNER + cells.length * COL + (cells.length - 1) * GAP, last = cell(i, cells.length - 1);
        return [
            owner(X[i], TOP, w, HEIGHT, name, sub),
            cells.map((c, k) => [
                c.edge ? block(cell(i, k), ROW, COL, TALL, [M("OnTransaction", { size: 11, fill: C.text }), M(c.edge, { size: 11, fill: C.text })], { state: "held" })
                    : block(cell(i, k), ROW, COL, TALL, [M("Handle", { size: 11, fill: C.text }), S(c.number)]),
                code(cell(i, k) + COL / 2, ROW + TALL + 20, c.state, { size: 11, anchor: "middle" }),
            ]),
            link([[last + COL / 2, ROW + TALL], [last + COL / 2, TOP + HEIGHT - 52]]),
            block(last + COL - 182, TOP + HEIGHT - 52, 182, 40, [M(cells.at(-1).closed, { fill: C.text })], { state: "held" }),
            text(last + COL - 190, TOP + HEIGHT - 27, "Closed", { size: 12, anchor: "end" }),
        ];
    };
    const LONE = 806;
    card("seven-frames.svg", {
        h: 322,
        title: "Each transaction comes framed: its start, its Records, its end",
        sub: "The calls of one subscription, in their order, and the rounds that they build",
        alt: "Two commits: a round of Lyon with the scans PX-7001 and PX-7002, and a round of Paris with the scan PX-7003. The subscription calls OnTransactionStart, which empties the round; the handler of each scan, which counts it, to Lyon and 1, then Lyon and 2; OnTransactionEnd, which closes the round as first, Lyon, 2. Then the same for the next transaction: first + 1, Paris, 1. OnReplayEnd comes last, and rounds.Last is first + 1.",
    }, [
        steps(94, [...[0, 1, 2, 3].map(k => cell(0, k) + COL / 2), ...[0, 1, 2].map(k => cell(1, k) + COL / 2), LONE + 61]),
        frame(0, [
            { edge: "Start", state: `("", 0)` },
            { number: "PX-7001", state: `("Lyon", 1)` },
            { number: "PX-7002", state: `("Lyon", 2)` },
            { edge: "End", state: "", closed: `(first, "Lyon", 2)` },
        ], "The round of Lyon", "transaction first"),
        frame(1, [
            { edge: "Start", state: `("", 0)` },
            { number: "PX-7003", state: `("Paris", 1)` },
            { edge: "End", state: "", closed: `(first + 1, "Paris", 1)` },
        ], "The round of Paris", "transaction first + 1"),
        block(LONE, ROW, 122, TALL, [M("OnReplayEnd", { fill: C.text, size: 11 })], { state: "held" }),
        link([[LONE + 61, ROW + TALL], [LONE + 61, TOP + HEIGHT - 52]]),
        block(LONE, TOP + HEIGHT - 52, 122, 40, [M("Last = first + 1", { fill: C.text, size: 11 })], { state: "held" }),
        text(PAD, FOOT, "In parentheses: the depot and the scans of the round. first: the number of the first transaction closed", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 8, StartAgainAfterTheLastTransactionCompleted: a failure at Atlantis, and the position that the second run starts from.
{
    const W = 208, X = [252, 486, 720], TOP = 100, mid = X.map(x => x + W / 2), end = X.map(x => x + W - 18), RUN1 = 214, RUN2 = 314;
    const scan = (i, depot) => [owner(X[i], TOP, W, 76, ["First", "Second", "Third"][i] + " commit"), node(X[i] + 10, TOP + 28, W - 20, 38, "ParcelScanned", depot, { size: 12 })];
    const done = (y, i, name) => [dot(mid[i], y, 4.5, C.blue), code(mid[i], y - 12, name, { anchor: "middle", fill: C.text }), rect(end[i] - 2, y - 11, 4, 22, { fill: C.blue, rx: 2 })];
    card("seven-start-again.svg", {
        h: 410,
        title: "After a failure, start again after the last transaction completed",
        sub: "The position moves at the end of each transaction, never in the middle of one",
        alt: "The Log holds three commits, a scan at Lyon, at Atlantis and at Paris. The tracker knows Lyon and Paris. The first run, Follow(log, 0, tracker), completes the transaction of Lyon: the depots hold Lyon, and the position moves to it. At Atlantis the handler throws a FormatException: the run ends there, and the position stays. The depot added to the known ones, the second run, Follow(log, tracker.Position + 1, tracker), starts at the transaction that failed and completes Atlantis, then Paris. The depots hold Lyon, Atlantis and Paris.",
    }, [
        text(PAD, 134, "The Log", { weight: 600, fill: C.text }),
        code(PAD, 153, "Scan, three times"),
        scan(0, "Lyon"), scan(1, "Atlantis"), scan(2, "Paris"),

        text(PAD, RUN1 - 6, "The first run", { weight: 600, fill: C.text }),
        text(PAD, RUN1 + 13, "The tracker knows Lyon and Paris", { size: 12 }),
        code(PAD, RUN1 + 30, "Follow(log, 0, tracker)"),
        line(X[0], RUN1, mid[1], RUN1),
        done(RUN1, 0, "Lyon"),
        code(mid[1], RUN1 - 16, "Atlantis", { anchor: "middle", fill: C.red }),
        dot(mid[1], RUN1, 10, C.surface), cross(mid[1], RUN1, { r: 6 }),
        code(mid[1], RUN1 + 28, "FormatException", { anchor: "middle", fill: C.text }),
        text(X[2], RUN1 + 4, "Paris is not delivered", { size: 12, fill: C.muted }),
        text(X[2], RUN1 + 28, "tracker.Depots", { size: 12, fill: C.muted }),
        code(RIGHT, RUN1 + 28, `["Lyon"]`, { anchor: "end", fill: C.text }),

        link([[end[0], RUN1 + 11], [end[0], RUN2], [X[1], RUN2]], { stroke: C.blue, sw: 2 }),
        code(end[0] - 10, RUN1 + 62, "Position + 1", { anchor: "end", fill: C.text }),

        text(PAD, RUN2 + 8, "The second run", { weight: 600, fill: C.text }),
        code(PAD, RUN2 + 27, `tracker.Known.Add("Atlantis")`),
        code(PAD, RUN2 + 44, "Follow(log, tracker.Position + 1, tracker)"),
        line(X[1], RUN2, end[2], RUN2),
        done(RUN2, 1, "Atlantis"), done(RUN2, 2, "Paris"),
        text(X[1], RUN2 + 44, "tracker.Depots", { size: 12, fill: C.muted }),
        code(RIGHT, RUN2 + 44, `["Lyon", "Atlantis", "Paris"]`, { anchor: "end", fill: C.text }),
        text(PAD, 392, "A bar: the end of a transaction, where the position moves", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 9, CommitTheRecordsAndTheParcelsTogether and WriteNeitherWithoutACommit: one transaction, two Stores.
{
    const TX = 136, BOX = 404, GOAL = 708, GW = 220, INNER = (BOX - 36) / 2, HEIGHT = 112, CALL = (TX + BOX + GOAL) / 2;
    const part = (x, y, name, lines) => [owner(x, y + 32, INNER, 68, name), lines.map((s, i) => text(x + INNER / 2, y + (lines.length === 1 ? 82 : 74) + i * 16, s, { size: 12, anchor: "middle", fill: C.text }))];
    const store = (y, name, s, done) => block(GOAL, y, GW, 52, [B(name, { size: 12, mono: true, fill: done ? C.text : C.muted }), S(s, { fill: done ? C.text : C.muted })], { state: done ? "held" : "gone" });
    const row = (y, name, parcel, records, call, done) => {
        const a = y + 26, b = y + 86, stub = { weak: true, head: false, stroke: C.muted };
        return [
            text(PAD, y + 60, name, { weight: 600, fill: C.text }),
            owner(TX, y, BOX, HEIGHT, "The transaction of the Repository"),
            part(TX + 12, y, "The parcel", parcel),
            part(TX + 24 + INNER, y, "The transaction of the Log", records),
            done ? [link([[TX + BOX, a], [GOAL, a]]), link([[TX + BOX, b], [GOAL, b]])] : [link([[TX + BOX, a], [GOAL - 44, a]], stub), link([[TX + BOX, b], [GOAL - 44, b]], stub)],
            code(CALL, y + 60, call, { anchor: "middle", fill: C.text }),
            store(y, "parcels", done ? parcel[0] : "nothing", done),
            store(y + 60, "journal", done ? records.join(", ") : "nothing", done),
        ];
    };
    card("seven-one-transaction.svg", {
        h: 392,
        title: "One transaction writes the parcels and their Records, or neither",
        sub: "repository.Chain(log): each ReadWrite transaction of the Repository opens a transaction of the Log",
        alt: "A transaction of the Repository holds a transaction of the Log. Parcels.Register and Parcels.Scan change the parcel PX-9001 and add two Records, registered PX-9001 and scanned at Lyon. transaction.Commit() writes both, in the Store of the parcels and in the Store of the Log. In the second test, Parcels.Register adds PX-9002, and transaction.Rollback() writes neither: the snapshot and the Log stay empty.",
    }, [
        row(100, "Committed", ["PX-9001 InTransit Lyon 1"], ["registered PX-9001", "scanned at Lyon"], "transaction.Commit()", true),
        line(PAD, 224, RIGHT, 224, { stroke: C.grid }),
        row(236, "Rolled back", ["PX-9002, for Bob"], ["registered PX-9002"], "transaction.Rollback()", false),
        text(PAD, 374, "Chained, the Log has no transaction of its own: it opens and commits with the one of its Repository", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 10, the commands of RebuildAModelInMemoryAtEachOpening and Replay.Run: the same handler, two paths to it.
{
    const ROW = [134, 262], TALL = 72, A = 200, LOG = 356, LW = 156, H = 632, HW = 134, GOAL = 796, GW = 132, c = i => ROW[i] + TALL / 2, TOPY = ROW[0], BOT = ROW[1] + TALL;
    card("seven-same-handlers.svg", {
        h: 396,
        title: "A command and a replay run the same handler",
        sub: "A command adds its Record, a replay finds it in the Log; both give its View to Apply",
        alt: "A command, Parcels.Scan(ada, \"Lyon\"), runs in a transaction of the Repository and of the Log: it adds the Record ParcelScanned to the Log by Add(), and gives its View to Apply by ToView(). A replay, Replay.Run, runs in a Replay transaction, which opens no transaction of the Log: a subscription from the start of the Log gives the same Record, as a View, to the same Apply. Apply reads the values of its Record only, and changes the parcel. In a Replay transaction, ParcelRef.New() is refused: the identities come from the Records.",
    }, [
        block(PAD, ROW[0], A, TALL, [B("A command"), M(`Parcels.Scan(ada, "Lyon")`, { fill: C.text }), S("with a transaction of the Log")]),
        block(PAD, ROW[1], A, TALL, [B("A replay"), M("Replay.Run(...)", { fill: C.text }), S("a Replay transaction: none of the Log")]),
        text(PAD, 104, "Who starts it", { size: 12, fill: C.muted }),
        text(LOG, 104, "The Log", { size: 12, fill: C.muted }),
        text(H, 104, "The handler", { size: 12, fill: C.muted }),
        text(GOAL, 104, "The model", { size: 12, fill: C.muted }),
        block(LOG, TOPY, LW, BOT - TOPY, [B("A Record"), M("ParcelScanned", { fill: C.text }), M("Parcel = ada", {}), M(`Depot = "Lyon"`, {})], { state: "held" }),
        link([[PAD + A, c(0)], [LOG, c(0)]]),
        code((PAD + A + LOG) / 2, c(0) - 8, "Add()", { anchor: "middle" }),
        link([[PAD + A, c(1)], [LOG, c(1)]]),
        code((PAD + A + LOG) / 2, c(1) - 8, "SubscribeFrom(0)", { anchor: "middle" }),
        link([[LOG + LW, c(0)], [H, c(0)]]),
        code((LOG + LW + H) / 2, c(0) - 8, "ToView()", { anchor: "middle" }),
        link([[LOG + LW, c(1)], [H, c(1)]]),
        code((LOG + LW + H) / 2, c(1) - 8, "Handle<TView>", { anchor: "middle" }),
        block(H, TOPY, HW, BOT - TOPY, [M("Apply(e)", { fill: C.text, weight: 600 }), S("the same code"), S("the values of"), S("its Record only")]),
        link([[H + HW, (TOPY + BOT) / 2], [GOAL, (TOPY + BOT) / 2]]),
        block(GOAL, TOPY, GW, BOT - TOPY, [B("The parcel"), S("InTransit", { fill: C.text }), S("Lyon", { fill: C.text }), S("Scans + 1", { fill: C.text })]),
        text(PAD, 378, "In a Replay transaction, ParcelRef.New() is refused: the identities come from the Records", { size: 11, fill: C.muted }),
    ]);
}

// The opening strip of Lessons 10 and 11: what the model and its Log hold at each step.
const opening = (file, { h, title, sub, alt, modelName, logName }, STEPS, logRow) => {
    const COL = 168, GAP = 14, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, MODEL = 170, TALL = 64, LOG = 280;
    const STATE = { ready: "held", behind: "orphan", changed: "refused" };
    return card(file, { h, title, sub, alt }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        text(PAD, 160, modelName, { size: 11, fill: C.muted }),
        text(PAD, 270, logName, { size: 11, fill: C.muted }),
        STEPS.map((s, i) => [
            s.head.map((c, k) => code(mid(i), 118 + k * 16, c, { anchor: "middle", fill: C.text })),
            block(x0(i), MODEL, COL, TALL, s.model.map(l => typeof l === "string" ? S(l, { fill: C.text }) : l), { state: STATE[s.state] }),
            s.say && text(mid(i), MODEL + TALL + 20, s.say, { size: 12, weight: s.refused ? 600 : 400, fill: s.refused ? C.red : C.muted, anchor: "middle" }),
            logRow(s, x0(i), LOG, COL, mid(i)),
        ]),
    ]);
};

// Lesson 10, RebuildAModelInMemoryAtEachOpening: two openings, a model in memory and its Log on disk.
{
    const COMMITS = [1, 1, 2, 2], ribbon = (s, x, y, w, cx) => {
        const list = s.extra ? [...COMMITS, 1] : COMMITS, widths = list.map(n => 8 + n * 10), total = widths.reduce((a, b) => a + b, 0) + (list.length - 1) * 4;
        let at = cx - total / 2;
        const out = list.map((n, k) => {
            const box = [rect(at, y, widths[k], 26, { stroke: C.base, rx: 4 }), Array.from({ length: n }, (_, r) => rect(at + 5 + r * 10, y + 7, 7, 12, { fill: C.text2, rx: 2 }))];
            at += widths[k] + 4;
            return box;
        });
        return [out, count(cx, y + 54, s.extra ? 7 : 6), text(cx, y + 71, "Records", { size: 12, fill: C.muted, anchor: "middle" })];
    };
    const READY = ["PX-10001 InTransit Paris 2", "PX-10002 Delivered Lyon 1"];
    opening("seven-behind-its-log.svg", {
        h: 380,
        title: "At each opening the model is behind its Log, until a replay",
        sub: "A model in memory, its Log on disk: what each one holds at each step",
        modelName: "The parcels, in memory", logName: "The Log, on disk: its transactions and their Records",
        alt: "Five steps. The first opening: four commits leave two parcels, PX-10001 InTransit Paris 2 and PX-10002 Delivered Lyon 1, and six Records in the Log. The second opening: the model is empty and behind its Log, IsBehindLog, and the Log holds its six Records. A transaction of the commands is refused meanwhile. Replay.Run brings the model to the same two parcels: it is ready. Parcels.Register goes on, in the model and in the Log: three parcels, seven Records.",
    }, [
        { head: ["the first opening", "four commits"], state: "ready", model: READY },
        { head: ["the second opening", "new InMemoryChain"], state: "behind", model: [B("Empty", { size: 12 }), M("IsBehindLog", { fill: C.text })] },
        { head: ["OpenTransaction", "(ReadWrite)"], state: "behind", model: [B("Empty", { size: 12 }), M("IsBehindLog", { fill: C.text })], say: "refused", refused: true },
        { head: ["Replay.Run(...)", ""], state: "ready", model: READY, say: "ready" },
        { head: ["Parcels.Register(", `"PX-10003", "Cy")`], state: "ready", model: [...READY, "PX-10003 Registered  0"], extra: true },
    ], ribbon);
}

// Lesson 10, the two groups of RebuildAModelInMemoryAtEachOpening: four Replay commits, or one.
{
    const LEFT = 176, W = 142, GAP = 12, x = i => LEFT + i * (W + GAP), TOP = 100, ROW = [178, 262], TALL = 58, RES = 860;
    const COMMITS = [["PX-10001", "registered"], ["PX-10002", "registered"], ["PX-10001, PX-10002", "scanned at Lyon"], ["PX-10001 at Paris", "PX-10002 delivered"]];
    const replay = (x0, y, w, say) => block(x0, y, w, TALL, [B("Replay transaction", { size: 12 }), M("Commit(through)", { size: 11, fill: C.text }), say && S(say, { fill: C.text })].filter(Boolean), { state: "held" });
    card("seven-replay-groups.svg", {
        h: 356,
        title: "A Replay transaction may hold many transactions of the Log",
        sub: "The same Log replayed by transactions, then by groups of 100: each commit says where the model is",
        alt: "The Log holds the four transactions of the commands. With group 1, each is replayed in a Replay transaction of its own, four Replay commits, each Commit(through) naming its transaction. With group 100, one Replay transaction holds all four, one commit, through the last transaction of the Log. Both leave the same model, no longer behind its Log.",
    }, [
        text(PAD, 134, "The Log", { weight: 600, fill: C.text }),
        text(PAD, 152, "four transactions", { size: 12 }),
        COMMITS.map((c, i) => block(x(i), TOP, W, 62, c.map(s => S(s, { fill: C.text })))),
        text(RES, 122, "Replay", { size: 12, fill: C.muted, anchor: "middle" }),
        text(RES, 138, "commits", { size: 12, fill: C.muted, anchor: "middle" }),
        text(PAD, ROW[0] + 26, "One by one", { weight: 600, fill: C.text }),
        code(PAD, ROW[0] + 45, "group: 1"),
        COMMITS.map((_, i) => [line(x(i) + W / 2, 162, x(i) + W / 2, ROW[0], { stroke: C.base }), replay(x(i), ROW[0], W)]),
        count(RES, ROW[0] + 35, 4),
        line(PAD, ROW[0] + TALL + 13, RIGHT, ROW[0] + TALL + 13, { stroke: C.grid }),
        text(PAD, ROW[1] + 26, "All together", { weight: 600, fill: C.text }),
        code(PAD, ROW[1] + 45, "group: 100"),
        replay(x(0), ROW[1], x(3) + W - x(0), "through the last transaction of the Log"),
        count(RES, ROW[1] + 35, 1),
        text(PAD, 338, "Both leave the same model, and IsBehindLog false", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 11, RebuildAModelOnDiskOnRequest: four openings, and a change without a Record.
{
    const THERE = [B("PX-11001", { size: 12 }), "InTransit Lyon 1"];
    const log = (s, x, y, w) => block(x, y, w, 58, ["registered PX-11001", "scanned at Lyon"].map(l => S(l, { fill: C.text })));
    opening("seven-rebuild.svg", {
        h: 366,
        title: "A change that no Record tells is lost at a rebuild",
        sub: "A model on disk and its Log on disk: what each one holds, over four openings",
        modelName: "The parcel, on disk", logName: "The Log, on disk",
        alt: "Five steps. The first opening: one commit, by Parcels.Register and Parcels.Scan, leaves PX-11001 InTransit Lyon 1, and the Log holds registered PX-11001 and scanned at Lyon. The second opening finds the model ready; a transaction moves the parcel to Nowhere without a Record: the model says InTransit Nowhere 1, and the Log has not changed. The third opening calls RebuildFromLog(): the model is empty, behind its Log, which is kept. Replay.Run brings it back: PX-11001 InTransit Lyon 1. The fourth opening finds it ready, without a replay.",
    }, [
        { head: ["the first opening", "Register, Scan"], state: "ready", model: THERE },
        { head: ["the second opening", `.Depot = "Nowhere"`], state: "changed", model: [B("PX-11001", { size: 12 }), "InTransit Nowhere 1"], say: "no Record tells it", refused: true },
        { head: ["the third opening", "RebuildFromLog()"], state: "behind", model: [B("Empty", { size: 12 }), M("IsBehindLog", { fill: C.text })] },
        { head: ["Replay.Run(...)", ""], state: "ready", model: THERE, say: "where its Records say" },
        { head: ["the fourth opening", ""], state: "ready", model: THERE, say: "ready, without a replay" },
    ], log);
}

// Lesson 12, ReadSeveralLogsInTheOrderOfTheirCommits: two Logs by turns, one reader.
{
    const LABEL = 216, W = 220, GAP = 20, x = i => LABEL + i * (W + GAP), mid = i => x(i) + W / 2, Y = [100, 196, 300], TALL = [44, 44, 56];
    const FLOW = [
        { log: "parcels", record: "ParcelRegistered", on: "parcels.On(replay)", handle: "Handle<ParcelRegisteredView>", seen: "registered PX-1201" },
        { log: "routes", record: "RoutePlanned", on: "routes.On(replay)", handle: "Handle<RoutePlannedView>", seen: "planned PX-1201" },
        { log: "parcels", record: "ParcelScanned", on: "parcels.On(replay)", handle: "Handle<ParcelScannedView>", seen: "scanned PX-1201" },
    ];
    card("seven-one-reader.svg", {
        h: 410,
        title: "One reader, each Record to its handler, in the order of the Store",
        sub: "Two Logs on one Log Store: their commits by turns, read as one journal",
        alt: "Two Logs, parcels and routes, on one Log Store, journal. parcels commits ParcelRegistered, routes commits RoutePlanned, parcels commits ParcelScanned. The journal holds the three Records in that order. One reader, journal.Replay(from: 0), holds the handlers that each Log registers by On(replay), and gives each Record to its handler in the order of the Store: seen is registered PX-1201, planned PX-1201, scanned PX-1201.",
    }, [
        text(PAD, 118, "Two Logs, by turns", { weight: 600, fill: C.text }),
        code(PAD, 137, "transaction.Commit()"),
        owner(LABEL - 12, 172, RIGHT - LABEL + 12, 92, ""),
        text(PAD, 216, "One Log Store", { weight: 600, fill: C.text }),
        code(PAD, 235, "journal"),
        owner(LABEL - 12, 280, RIGHT - LABEL + 12, 92, ""),
        text(PAD, 322, "One reader", { weight: 600, fill: C.text }),
        code(PAD, 341, "journal.Replay(from: 0)"),
        code(PAD, 394, "seen"),
        FLOW.map((f, i) => [
            node(x(i), Y[0], W, TALL[0], f.log, "a transaction of its own", { mono: true, size: 12 }),
            link([[mid(i), Y[0] + TALL[0]], [mid(i), Y[1]]]),
            node(x(i), Y[1], W, TALL[1] + 12, f.record, "PX-1201", { size: 12, state: "held" }),
            link([[mid(i), Y[1] + TALL[1] + 12], [mid(i), Y[2]]]),
            block(x(i), Y[2], W, TALL[2], [M(f.on, { fill: C.text }), M(f.handle, { fill: C.text })]),
            code(mid(i), 394, f.seen, { anchor: "middle", fill: C.text }),
        ]),
    ]);
}

// Lesson 12, the two tests of the order: three commits by turns, or one commit in the order of the Add.
{
    const COL = 434, X = [32, 494], PITCH = 52, TALL = 40, rowY = k => 190 + k * PITCH, NODE = 104, NW = 222, BRACKET = X[1] + NODE + NW + 18;
    const entry = (x, k, tag, name, sub, state) => [code(x + 14, rowY(k) + 25, tag), node(x + NODE, rowY(k), NW, TALL, name, sub, { size: 12, state })];
    const top = rowY(0) + 4, bottom = rowY(3) + TALL - 4;
    card("seven-order-of-two-logs.svg", {
        h: 450,
        title: "Chained to one Repository, two Logs write one commit, in the order of the Add",
        sub: "The same order for the reader: by the commits on the left, by the Add on the right",
        alt: "Two columns. Without a Repository, parcels and routes commit by turns, Records alone, three commits: ParcelRegistered, RoutePlanned, ParcelScanned; the order is the one of their commits. Chained to one Repository, one transaction holds a Counter, then ParcelRegistered, RoutePlanned and ParcelScanned, added in that order across the Logs, in one commit. A reader sees registered, planned, scanned in both, over three transactions on the left, one on the right.",
    }, [
        text(X[0], 108, "Without a Repository", { weight: 600, fill: C.text }),
        code(X[0], 127, "two Logs commit by turns"),
        line(X[0], 140, X[0] + COL, 140, { stroke: C.grid }),
        text(X[1], 108, "Chained to one Repository", { weight: 600, fill: C.text }),
        code(X[1], 127, "repository.Chain(parcels), repository.Chain(routes)"),
        line(X[1], 140, X[1] + COL, 140, { stroke: C.grid }),

        text(X[0] + NODE, rowY(0) + 25, "No entity: the Records alone", { size: 12, fill: C.muted }),
        [["parcels", "ParcelRegistered", "PX-1201", 1], ["routes", "RoutePlanned", "PX-1201", 2], ["parcels", "ParcelScanned", "PX-1201", 3]].map(([tag, name, sub, k]) => [
            rect(X[0], rowY(k) - 3, COL, TALL + 6, { stroke: C.base, rx: 8 }),
            entry(X[0], k, tag, name, sub, "held"),
            code(X[0] + COL - 12, rowY(k) + 25, "Commit()", { anchor: "end", fill: C.text }),
        ]),

        owner(X[1], rowY(0) - 36, COL, rowY(3) + TALL + 6 - (rowY(0) - 36), "One transaction", "the entities and the Records of both"),
        entry(X[1], 0, "repository", "Counter", "Lyon", "alive"),
        entry(X[1], 1, "parcels", "ParcelRegistered", "PX-1203", "held"),
        entry(X[1], 2, "routes", "RoutePlanned", "PX-1203", "held"),
        entry(X[1], 3, "parcels", "ParcelScanned", "PX-1203", "held"),
        link([[BRACKET - 8, top], [BRACKET, top], [BRACKET, bottom], [BRACKET - 8, bottom]], { head: false, stroke: C.base, sw: 1.2 }),
        code(X[1] + COL - 12, (top + bottom) / 2 + 4, "Commit()", { anchor: "end", fill: C.text }),

        code(X[0], 424, "seen: registered, planned, scanned"),
        text(X[0] + COL, 424, "three commits", { size: 12, anchor: "end" }),
        code(X[1], 424, "seen: registered, planned, scanned"),
        text(X[1] + COL, 424, "one commit", { size: 12, anchor: "end" }),
    ]);
}
