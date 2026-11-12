// The diagrams of Tutorial T008. Each one draws the very objects of a test of its lesson: run `node T008-chains-and-distribution.mjs`
// after a change of the lesson, or of the kit. Their words stay those of the developer: Stores, Repositories, transactions, servers.
import { C, PAD, RIGHT, bar, block, card, check, code, cross, dot, line, link, node, owner, rect, steps, text } from "./kit.mjs";

const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o });

// An Owner whose name is a variable of the test.
const box = (x, y, w, h, name, sub, { state = "alive", tint = false } = {}) => [owner(x, y, w, h, "", sub, { state, tint }), code(x + 12, y + 20, name, { weight: 600, fill: state === "gone" ? C.muted : C.text })];

// A server along the time: its name on the left, its line from its first moment to its last.
const lane = (y, name, sub, from, to) => [text(PAD, y - 2, name, { weight: 600, fill: C.text }), sub && text(PAD, y + 15, sub, { size: 11, fill: C.muted }), line(from, y, to, y)];

// The write token on a server, from one moment to another.
const token = (x0, x1, y) => bar(x0, x1, y - 3, 6, C.blue, { rounded: false });

// The token and the copies, named on the last line.
const timeLegend = (y, ...more) => [
    bar(PAD, PAD + 26, y - 7, 6, C.blue, { rounded: false }), text(PAD + 34, y, "the write token", { size: 11, fill: C.muted }),
    link([[172, y - 4], [198, y - 4]], { stroke: C.text2 }), text(206, y, "a commit copied", { size: 11, fill: C.muted }),
    link([[318, y - 4], [344, y - 4]], { stroke: C.blue, sw: 2 }), text(352, y, "the token taken by a commit", { size: 11, fill: C.muted }),
    more,
];

// Lesson 1, RefuseTheNumberOfATypeOfAnotherRepository: what the Store holds, and four models of a third Repository at its bind.
{
    const ROW = [134, 190, 246, 302], TALL = 40, CHIP = 44, CHIPW = 306, DECL = 436, DECLW = 256, SAY = 708, GUTTER = 398, cy = i => ROW[i] + TALL / 2;
    const held = (i, number, type, repository) => [
        rect(CHIP, ROW[i], CHIPW, TALL, { fill: C.raised, stroke: C.base, rx: 6 }),
        text(CHIP + 14, cy(i) + 5, number, { weight: 600, fill: C.text }),
        code(CHIP + 52, cy(i) + 4, type, { fill: C.text }),
        code(CHIP + CHIPW - 12, cy(i) + 4, repository, { size: 11, anchor: "end", fill: C.muted }),
    ];
    const bind = (i, declaration, state, verdict, why, fill) => [
        node(DECL, ROW[i], DECLW, TALL, declaration, null, { state, size: 12, mono: true }),
        text(SAY, cy(i) - 3, verdict, { weight: 600, fill }),
        text(SAY, cy(i) + 14, why, { size: 12 }),
    ];
    card("T008-bind-numbers.svg", {
        h: 396,
        title: "A number is one type in a Store, whatever its name or its definition",
        sub: "Each bind compares its model with the numbers the Store keeps: a number taken is refused, a free one accepted",
        alt: "The Store keeps the number 1, MParcel, of CarrierRepository, and the numbers 11, MTrackingPage, and 12, MDepotBoard, of TrackingRepository; 13 is free. A third Repository is refused with [Entity(11, 1, 1)] MInvoice, the number of the pages; with [Entity(11, 2, 5)] MTrackingPage, the same name in a higher version; and with [Entity(12, 1, 2)] MDepotBoard, strictly the same definition. With [Entity(13, 1, 1)] MInvoice, a number of its own, it is accepted. A refusal writes nothing, and the Repositories of the Store go on.",
    }, [
        owner(PAD, 100, 330, 256, "The Store", "the numbers of its Repositories"),
        held(0, "1", "MParcel", "CarrierRepository"),
        held(1, "11", "MTrackingPage", "TrackingRepository"),
        held(2, "12", "MDepotBoard", "TrackingRepository"),
        rect(CHIP, ROW[3], CHIPW, TALL, { stroke: C.base, dash: "4 4", rx: 6 }),
        text(CHIP + 14, cy(3) + 5, "13", { weight: 600, fill: C.muted }),
        text(CHIP + 52, cy(3) + 4, "free", { size: 12, fill: C.muted }),

        code(DECL, 120, "new InvoicingRepository(store)", { fill: C.text }),
        text(SAY, 120, "Its bind", { weight: 600, fill: C.text }),
        bind(0, "[Entity(11, 1, 1)] MInvoice", "refused", "Refused", "The number of the pages", C.red),
        bind(1, "[Entity(11, 2, 5)] MTrackingPage", "refused", "Refused", "The same name, in a higher version", C.red),
        bind(2, "[Entity(12, 1, 2)] MDepotBoard", "refused", "Refused", "Strictly the same definition", C.red),
        bind(3, "[Entity(13, 1, 1)] MInvoice", "held", "Accepted", "A number of its own", C.blue),

        link([[DECL, cy(0)], [GUTTER, cy(0)], [GUTTER, cy(1) - 8], [CHIP + CHIPW, cy(1) - 8]], { stroke: C.red }),
        link([[DECL, cy(1) + 6], [CHIP + CHIPW, cy(1) + 6]], { stroke: C.red }),
        link([[DECL, cy(2)], [CHIP + CHIPW, cy(2)]], { stroke: C.red }),
        link([[DECL, cy(3)], [CHIP + CHIPW, cy(3)]], { stroke: C.blue, sw: 2 }),
        text(PAD, 378, "Refused at the bind, with nothing written: the Repositories of the Store go on", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 1, CommitEachRepositoryApart, beside Lesson 3, WriteNothingOfARollback: two models written together, and a rollback.
{
    const PANEL = 430, GAP = 36, COL = 205, px = i => PAD + i * (PANEL + GAP), TX = 152, TXH = 76, STORE = 282, cols = i => [px(i) + COL / 2, px(i) + PANEL - COL / 2];
    const head = (i, name, declaration) => [text(px(i), 108, name, { weight: 600, fill: C.text }), code(px(i), 127, declaration, { size: 11 }), line(px(i), 140, px(i) + PANEL, 140, { stroke: C.grid })];
    const pair = (i, y, left, right, { leftState, rightState } = {}) => [
        node(px(i) + 12, y, COL - 24, 34, left, null, { size: 12, state: leftState }),
        node(px(i) + PANEL - COL + 12, y, COL - 24, 34, right, null, { size: 12, state: rightState }),
    ];
    const write = x => link([[x, TX + TXH], [x, STORE]], { stroke: C.blue, sw: 2 });
    const noWrite = x => [link([[x, TX + TXH], [x, STORE]], { stroke: C.base }), cross(x, (TX + TXH + STORE) / 2 - 2, { r: 4, stroke: C.muted })];
    const outcome = (i, say, result) => [text(px(i) + PANEL / 2, 384, say, { weight: 600, fill: C.text, anchor: "middle" }), code(px(i) + PANEL / 2, 404, result, { fill: C.muted, anchor: "middle" })];
    card("T008-store-or-chain.svg", {
        h: 422,
        title: "One Store, two transactions; a chain, only one",
        sub: "Two models written together, and a rollback: what each one keeps",
        alt: "Two panels. Two Repositories on one Store: parcelTransaction writes the parcel PX-0102 and boardTransaction writes the board of Lyon; parcelTransaction commits and boardTransaction rolls back; the Store holds the parcel and no board. Two Stores in a chain: one transaction writes the parcel PX-2001 and its page; it rolls back, and neither Store holds anything.",
    }, [
        head(0, "Two Repositories on one Store", "new CarrierRepository(store), new TrackingRepository(store)"),
        box(px(0), TX, COL, TXH, "parcelTransaction"),
        box(px(0) + PANEL - COL, TX, COL, TXH, "boardTransaction"),
        pair(0, TX + 30, "Parcel PX-0102", "DepotBoard Lyon"),
        write(cols(0)[0]), code(cols(0)[0] + 12, 258, "Commit()"),
        noWrite(cols(0)[1]), code(cols(0)[1] + 12, 258, "Rollback()"),
        box(px(0), STORE, PANEL, 76, "store"),
        pair(0, STORE + 30, "Parcel PX-0102", "DepotBoard Lyon", { leftState: "held", rightState: "gone" }),
        outcome(0, "The parcel is committed, its board is not", "1 parcel, 0 boards"),

        head(1, "Two Stores in a chain", "parcels.AddChildStore(tracking), carrier.Chain(site)"),
        box(px(1), TX, PANEL, TXH, "transaction"),
        pair(1, TX + 30, "Parcel PX-2001", "TrackingPage PX-2001"),
        noWrite(cols(1)[0]), noWrite(cols(1)[1]), code(px(1) + PANEL / 2, 258, "Rollback()", { anchor: "middle" }),
        box(px(1), STORE, COL, 76, "parcels"),
        box(px(1) + PANEL - COL, STORE, COL, 76, "tracking"),
        pair(1, STORE + 30, "Parcel PX-2001", "TrackingPage PX-2001", { leftState: "gone", rightState: "gone" }),
        outcome(1, "Nothing in any Store", "(0, 0)"),
    ]);
}

// Lesson 2, WriteTwoModelsInOneTransaction: the instructions of the test, one row each, in the two models.
{
    const LEFT = 270, RIGHTW = 290, X2 = RIGHT - RIGHTW, TOP = 116, PITCH = 62, TALL = 46, y = i => TOP + i * PITCH, mid = (PAD + LEFT + X2) / 2;
    const row = (i, left, right, over, under, called = true) => {
        const cy = y(i) + TALL / 2;
        return [
            node(PAD, y(i), LEFT, TALL, left[0], left[1], { size: 12, mono: true }),
            node(X2, y(i), RIGHTW, TALL, right[0], right[1], { size: 12, mono: true }),
            link([[PAD + LEFT, cy], [X2, cy]]),
            called ? code(mid, cy - 8, over, { anchor: "middle", fill: C.text }) : text(mid, cy - 8, over, { size: 12, fill: C.text, anchor: "middle" }),
            text(mid, cy + 17, under, { size: 11, fill: C.muted, anchor: "middle" }),
        ];
    };
    card("T008-chain.svg", {
        h: 432,
        title: "A chain opens both transactions, and commits both Stores",
        sub: "The instructions of the test, from top to bottom, in the model of the parcels and in the model of the tracking",
        alt: "From top to bottom. parcels.AddChildStore(tracking) chains the Store of the tracking after the Store of the parcels, before their first transaction. carrier.Chain(site) chains their Repositories in the same order. The transaction opened by carrier.OpenTransaction opens TrackingRepositoryTransaction.Current, ambient, in the same mode. The code writes the parcel PX-1001 of Ada and its page, Registered, whose identity TrackingPageRef.Of(parcel) derives from the parcel. transaction.Commit() writes both Stores.",
    }, [
        text(PAD + LEFT / 2, 106, "The parcels", { weight: 600, fill: C.text, anchor: "middle" }),
        text(X2 + RIGHTW / 2, 106, "The tracking", { weight: 600, fill: C.text, anchor: "middle" }),
        row(0, ["parcels", "a Store"], ["tracking", "a Store"], "parcels.AddChildStore(tracking)", "before their first transaction"),
        row(1, ["carrier", "new CarrierRepository(parcels)"], ["site", "new TrackingRepository(tracking)"], "carrier.Chain(site)", "in the same order"),
        row(2, ["transaction", "carrier.OpenTransaction(ReadWrite)"], ["TrackingRepositoryTransaction.Current", "ReadWrite, as the first one"], "opens the second one", "ambient, in the same mode", false),
        row(3, ["new Parcel(ParcelRef.New())", "PX-1001, Ada"], ["new TrackingPage(...)", "PX-1001, Ada, Registered"], "TrackingPageRef.Of(parcel)", "the same parcel gives the same page"),
        link([[PAD + LEFT / 2, y(3) + TALL], [PAD + LEFT / 2, y(4)]]),
        link([[X2 + RIGHTW / 2, y(3) + TALL], [X2 + RIGHTW / 2, y(4)]]),
        block(PAD, y(4), RIGHT - PAD, TALL, [M("transaction.Commit()", { fill: C.text, weight: 600 }), { s: "One commit: it writes both Stores", size: 11 }], { state: "held" }),
    ]);
}

// Lesson 3, RefuseTheCommitOfAChainCondemnedInOneStore: the transaction of the tracking is condemned, the commit is called on the other one.
{
    const FIRST = 134, SECOND = 226, FROM = 296, NEW = 396, RULE = 566, END = 764, COMMIT = 796;
    card("T008-chain-condemned.svg", {
        h: 316,
        title: "A Store condemned refuses the commit of the whole chain",
        sub: "The rule of the tracking throws after a write; the commit of the chain gives it as its cause",
        alt: "Two lines of time, one transaction per Store. In the transaction of the parcels, a new parcel PX-2002 of Bob. In the transaction of the tracking, a new board of Lyon, then Parcels--: the rule throws after the write, and CondemnedBy holds it. transaction.Commit(), called on the transaction of the parcels, is refused, the rule given as its inner exception. Neither Store holds anything.",
    }, [
        text(PAD, FIRST - 2, "The parcels", { weight: 600, fill: C.text }), code(PAD, FIRST + 15, "transaction", { size: 11, fill: C.muted }),
        text(PAD, SECOND - 2, "The tracking", { weight: 600, fill: C.text }), code(PAD, SECOND + 15, "TrackingRepositoryTransaction.Current", { size: 11, fill: C.muted }),
        line(FROM, FIRST, COMMIT, FIRST),
        dot(NEW, FIRST, 4, C.text2), code(NEW, FIRST - 12, "new Parcel(...)", { anchor: "middle", fill: C.text }), text(NEW, FIRST + 20, "PX-2002, Bob", { size: 11, fill: C.muted, anchor: "middle" }),
        cross(COMMIT, FIRST, { r: 6 }), code(COMMIT, FIRST - 14, "transaction.Commit()", { anchor: "middle", fill: C.text }),
        text(COMMIT + 14, FIRST + 4, "Refused", { size: 12, weight: 600, fill: C.red }),
        text(COMMIT + 14, FIRST + 20, "InnerException: the rule", { size: 11, fill: C.muted }),

        line(FROM, SECOND, RULE, SECOND),
        line(RULE, SECOND, END, SECOND, { stroke: C.red, sw: 2 }),
        dot(NEW, SECOND, 4, C.text2), code(NEW, SECOND - 12, 'new DepotBoard { Depot = "Lyon" }', { anchor: "middle", fill: C.text }),
        dot(RULE, SECOND, 5, C.red), code(RULE, SECOND - 12, ".Parcels--", { anchor: "middle", fill: C.text }),
        text(RULE, SECOND + 20, "The rule throws after the write", { size: 11, fill: C.muted, anchor: "middle" }),
        code(END, SECOND - 12, "CondemnedBy: the rule", { size: 11, anchor: "end", fill: C.red }),
        link([[END + 4, SECOND - 6], [COMMIT - 7, FIRST + 11]], { weak: true, stroke: C.red }),
        line(PAD, 274, RIGHT, 274, { stroke: C.grid }),
        text(PAD, 298, "Nothing in any Store: no parcel, no board", { size: 12 }),
    ]);
}

// Lesson 3, ConflictOnAnyStoreOfTheChain: two writers, two parcels, one board; the second one commits first.
{
    const FIRST = 134, SECOND = 226, TALL = 64, WRITER = 176, PX = 256, PW = 186, SX = 516, SW = 184, RESULT = 760, c1 = FIRST + TALL / 2, c2 = SECOND + TALL / 2;
    const writer = (y, name, call) => block(PAD, y, WRITER, TALL, [B(name), M(call, { size: 11 })]);
    card("T008-chain-conflict.svg", {
        h: 324,
        title: "A conflict in one Store fails the commit of the whole chain",
        sub: "Two transactions scan two parcels at Lyon: their parcels differ, and both change the same board",
        alt: "Two writers. The first one scans the parcel PX-2003 at Lyon, then the second one scans the parcel PX-2004 at Lyon. The parcels are two entities of the Store of the parcels; the board of Lyon, in the Store of the tracking, is changed by both. The second writer commits first. The commit of the first one conflicts and writes nothing in any Store: PX-2003 stays without a depot, PX-2004 is at Lyon, and the board counts 1 parcel.",
    }, [
        writer(FIRST, "The first writer", 'Arrive(first, "Lyon")'),
        writer(SECOND, "The second writer", 'Arrive(second, "Lyon")'),
        box(PX - 14, 100, PW + 28, 206, "parcels"),
        node(PX, FIRST, PW, TALL, "PX-2003", "unchanged, no depot"),
        node(PX, SECOND, PW, TALL, "PX-2004", "at Lyon", { state: "held" }),
        box(SX - 14, 100, SW + 28, 206, "tracking"),
        node(SX, FIRST, SW, SECOND + TALL - FIRST, "The board of Lyon", "1 parcel", { state: "refused" }),
        link([[PAD + WRITER, c1], [PX, c1]]),
        link([[PAD + WRITER, c2], [PX, c2]]),
        link([[PX + PW, c1], [SX, c1]], { stroke: C.red }),
        link([[PX + PW, c2], [SX, c2]], { stroke: C.red }),
        link([[SX + SW, c1], [RESULT - 12, c1]], { head: false, stroke: C.base }),
        link([[SX + SW, c2], [RESULT - 12, c2]], { head: false, stroke: C.base }),
        cross(RESULT, c1, { r: 6 }), code(RESULT + 16, c1 - 14, "Commit()", { fill: C.text }),
        text(RESULT + 16, c1 + 4, "conflict", { size: 12, weight: 600, fill: C.red }), text(RESULT + 16, c1 + 20, "writes nothing, in any Store", { size: 11, fill: C.muted }),
        check(RESULT, c2, { r: 6 }), code(RESULT + 16, c2 - 14, "Commit()", { fill: C.text }),
        text(RESULT + 16, c2 + 4, "committed first", { size: 12 }), text(RESULT + 16, c2 + 20, "both Stores written", { size: 11, fill: C.muted }),
        link([[RESULT, c2 - 12], [RESULT, c1 + 12]], { weak: true, stroke: C.red }),
    ]);
}

// Lesson 4, CommitACommandInTheThreeStores: three commands, their Records, their handlers, one commit.
{
    const CMD = 262, LOG = 312, LOGW = 140, MODELS = 524, SPLIT = 676, TOP = 136, PITCH = 66, TALL = 54, y = i => TOP + i * PITCH;
    const cell = (x, w, i, first, second, third) => {
        const lines = [first, second, third].filter(Boolean), top = y(i) + TALL / 2 - (lines.length - 1) * 8 + 4;
        return lines.map((s, k) => text(x + w / 2, top + k * 16, s, { size: 11, fill: k === 0 ? C.text : C.text2, anchor: "middle" }));
    };
    const row = (i, command, record, value, parcel, page, boards) => {
        const cy = y(i) + TALL / 2;
        return [
            block(PAD, y(i), CMD, TALL, [M(command, { fill: C.text })]),
            link([[PAD + CMD, cy], [LOG, cy]]),
            block(LOG, y(i), LOGW, TALL, [M(record, { fill: C.text }), { s: value, size: 11 }], { state: "held" }),
            link([[LOG + LOGW, cy], [MODELS, cy]]),
            code((LOG + LOGW + MODELS) / 2, cy - 8, "Apply(e)", { anchor: "middle" }),
            rect(MODELS, y(i), RIGHT - MODELS, TALL, { fill: C.raised, stroke: C.base }),
            line(SPLIT, y(i), SPLIT, y(i) + TALL, { stroke: C.base }),
            cell(MODELS, SPLIT - MODELS, i, ...parcel),
            cell(SPLIT, RIGHT - SPLIT, i, page, ...boards),
        ];
    };
    const head = (x, name, variable) => [text(x, 106, name, { weight: 600, fill: C.text, anchor: "middle" }), code(x, 123, variable, { anchor: "middle" })];
    card("T008-command-three-stores.svg", {
        h: 410,
        title: "The commands write the three Stores, in one commit",
        sub: "Each command adds its Record to the Log, and its handler changes both models; the commit writes the three",
        alt: "Three commands in one transaction of the chain. Parcels.Register adds ParcelRegistered to the Log; its handler creates the parcel and its page, Registered. Parcels.Scan at Lyon adds ParcelScanned; the handler moves the parcel to Lyon, the page reads In transit at Lyon, its Steps 1, and the board of Lyon counts 1. Parcels.Scan at Paris adds ParcelScanned; the parcel is at Paris, the page reads In transit at Paris, its Steps 2, the board of Lyon counts 0 and the board of Paris 1. transaction.Commit() is one commit of the three Stores.",
    }, [
        text(PAD + CMD / 2, 106, "The command", { weight: 600, fill: C.text, anchor: "middle" }),
        head(LOG + LOGW / 2, "The Log", "journal"),
        head((MODELS + SPLIT) / 2, "The parcels", "parcels"),
        head((SPLIT + RIGHT) / 2, "The tracking", "tracking"),
        row(0, 'Parcels.Register("PX-3001", "Ada")', "ParcelRegistered", "PX-3001, Ada", ["new Parcel", "Status: Registered"], "new TrackingPage", ["Status: Registered"]),
        row(1, 'Parcels.Scan(ada, "Lyon")', "ParcelScanned", "Lyon", ["Status: InTransit", "Depot: Lyon"], "In transit at Lyon, Steps: 1", ["Board Lyon: 1"]),
        row(2, 'Parcels.Scan(ada, "Paris")', "ParcelScanned", "Paris", ["Status: InTransit", "Depot: Paris"], "In transit at Paris, Steps: 2", ["Board Lyon: 0, board Paris: 1"]),
        block(PAD, y(3), CMD, TALL, [M("transaction.Commit()", { fill: C.text })]),
        link([[PAD + CMD, y(3) + TALL / 2], [LOG, y(3) + TALL / 2]], { stroke: C.blue, sw: 2 }),
        block(LOG, y(3), RIGHT - LOG, TALL, [B("One commit of the three Stores"), { s: "the Log, the parcels and the tracking, together", size: 11 }], { state: "held" }),
    ]);
}

// Lesson 4, ReplayBothModelsFromTheLog: the models in memory, the Log on disk; closed, opened again, replayed.
{
    const COL = 278, GAP = 31, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, PARCELS = 132, LOG = 224, TRACKING = 334, TALL = 84, FIRST = 66;
    const lines = (x, y, rows) => rows.map((s, k) => code(x + 12, y + 40 + k * 16, s, { size: 11, fill: C.text2 }));
    const stage = (i, { empty = false, replayed = false, behind }) => {
        const x = x0(i), state = empty ? "gone" : "alive";
        return [
            box(x, PARCELS, COL, FIRST, "parcels", "in memory", { state }),
            !empty && [node(x + 12, PARCELS + 30, 121, 26, "PX-3002", null, { size: 12 }), node(x + COL - 133, PARCELS + 30, 121, 26, "PX-3003", null, { size: 12 })],
            empty && text(mid(i), PARCELS + 47, "empty", { size: 12, fill: C.muted, anchor: "middle" }),
            box(x, LOG, COL, TALL, "journal", "on disk", { state: "held", tint: true }),
            lines(x, LOG, ["ParcelRegistered PX-3002, PX-3003", "ParcelScanned PX-3002, PX-3003, Lyon", "ParcelDelivered PX-3003"]),
            box(x, TRACKING, COL, TALL, "tracking", "in memory", { state }),
            !empty && lines(x, TRACKING, ["Lyon: 1", "PX-3002: In transit at Lyon, 1 steps", "PX-3003: Delivered, 2 steps"]),
            empty && text(mid(i), TRACKING + 54, "empty", { size: 12, fill: C.muted, anchor: "middle" }),
            replayed && [link([[mid(i), LOG], [mid(i), PARCELS + FIRST]], { stroke: C.blue, sw: 2 }), code(mid(i) + 10, LOG - 9, "Apply(e)"),
                link([[mid(i), LOG + TALL], [mid(i), TRACKING]], { stroke: C.blue, sw: 2 }), code(mid(i) + 10, LOG + TALL + 17, "Apply(e)")],
            behind && code(mid(i), 444, `IsBehindLog: ${behind}`, { anchor: "middle" }),
        ];
    };
    card("T008-replay.svg", {
        h: 462,
        title: "One replay of the Log gives both models again",
        sub: "The two models in memory, the Log on disk: closed, opened again, replayed",
        alt: "Three stages. After two commits the Store of the parcels holds PX-3002 and PX-3003, the Store of the tracking reads Lyon: 1, PX-3002: In transit at Lyon, 1 steps, and PX-3003: Delivered, 2 steps, and the Log on disk holds their Records. Opened again by new Chain(directory), both models are empty and IsBehindLog is true; the Log is whole. After the replay, committed by replay.Commit(last), the handlers have written both models again, the same lines, and IsBehindLog is false.",
    }, [
        steps(94, [0, 1, 2].map(mid)),
        code(mid(0), 118, "two commits", { anchor: "middle", fill: C.text }),
        code(mid(1), 118, "new Chain(directory)", { anchor: "middle", fill: C.text }),
        code(mid(2), 118, "replay.Commit(last)", { anchor: "middle", fill: C.text }),
        stage(0, {}),
        stage(1, { empty: true, behind: "true" }),
        stage(2, { replayed: true, behind: "false" }),
    ]);
}

// Lesson 5, CopyTheCommitsOfAWriterToAReplica: two servers, the same Store, each commit copied and read.
{
    const SERVER = 360, X1 = PAD, X2 = RIGHT - SERVER, IN = SERVER - 32, GAPX = (X1 + SERVER + X2) / 2, ROWS = [134, 192, 250, 308], TALL = 46, cy = i => ROWS[i] + TALL / 2;
    const row = (i, left, right, o = {}) => [
        node(X1 + 16, ROWS[i], IN, TALL, left[0], left[1], { size: 12, mono: true, ...o }),
        node(X2 + 16, ROWS[i], IN, TALL, right[0], right[1], { size: 12, mono: true, ...o }),
    ];
    card("T008-replica.svg", {
        h: 386,
        title: "A replica: the same Store on another server, each commit copied",
        sub: "An endpoint connects each server; the writer commits, the replica reads the last commit it holds",
        alt: "Two servers. The endpoint of the replica joins the cluster through the endpoint of the writer, its seed. Each holds the Store parcels in a directory of its own: ReadWrite in lesson-05-writer, ReadOnly in lesson-05-replica. Each commit of the writer is copied to the replica, in the order of the commits. The writer commits the parcel PX-4001 of Ada, and the replica reads the recipient Ada; the writer commits the depot Lyon, and the replica reads Lyon.",
    }, [
        owner(X1, 100, SERVER, 266, "The writer", "new CarrierRepository(writerStore)"),
        owner(X2, 100, SERVER, 266, "The replica", "new CarrierRepository(replicaStore)"),
        row(0, ["writerEndPoint", "listens on a port"], ["replicaEndPoint", "Cluster.EndPoint(writerEndPoint)"]),
        row(1, ["writerStore", "\"parcels\", ReadWrite, in lesson-05-writer"], ["replicaStore", "\"parcels\", ReadOnly, in lesson-05-replica"], { state: "held" }),
        row(2, ["new Parcel(...)", "PX-4001, Ada, then Commit()"], ["ada.Resolve().Recipient", "\"Ada\""]),
        row(3, ['ada.Resolve().Depot = "Lyon"', "then Commit()"], ["ada.Resolve().Depot", "\"Lyon\""]),
        link([[X2 + 16, cy(0)], [X1 + 16 + IN, cy(0)]]),
        text(GAPX, cy(0) - 8, "joins through its seed", { size: 12, anchor: "middle" }),
        link([[X1 + 16 + IN, cy(1)], [X2 + 16, cy(1)]], { stroke: C.blue, sw: 2 }),
        text(GAPX, cy(1) - 8, "each commit, in order", { size: 12, weight: 600, fill: C.text, anchor: "middle" }),
        link([[X1 + 16 + IN, cy(2)], [X2 + 16, cy(2)]]),
        code(GAPX, cy(2) - 8, "CaughtUp", { anchor: "middle" }),
        link([[X1 + 16 + IN, cy(3)], [X2 + 16, cy(3)]]),
        code(GAPX, cy(3) - 8, "CaughtUp", { anchor: "middle" }),
    ]);
}

// Lesson 6, MoveTheWriteTokenBetweenTwoWriters: three commits, the token moves twice, one sequence on both replicas.
{
    const OFFICE = 150, DEPOT = 254, FROM = 164, A = 250, B1 = 530, C1 = 770, JOIN = 330;
    const chip = (x, y, w, s) => node(x, y, w, 32, s, null, { size: 12, mono: true, state: "held" });
    card("T008-write-token.svg", {
        h: 398,
        title: "The write token goes to the replica that commits",
        sub: "Two writers of one Store, along the time: a commit takes the token once its replica holds every commit before",
        alt: "Along the time, the office commits the parcel PX-5001 of Ada and holds the write token. The depot, a second writer, receives that commit; its commit of the status and of the depot Lyon takes the token. The office waits for that commit by CaughtUp, and its commit of the recipient Ada Lovelace takes the token back. Both replicas hold the three commits, in the same order, and TokenGeneration is at least generation + 2.",
    }, [
        lane(OFFICE, "The office", "ReadWrite", FROM, RIGHT),
        lane(DEPOT, "The depot", "ReadWrite", JOIN, RIGHT),

        token(A, B1 - 20, OFFICE), token(B1 - 20, C1 - 20, DEPOT), token(C1 - 20, RIGHT, OFFICE),
        link([[B1 - 20, OFFICE], [B1 - 20, DEPOT]], { stroke: C.blue, sw: 2 }),
        link([[C1 - 20, DEPOT], [C1 - 20, OFFICE]], { stroke: C.blue, sw: 2 }),

        link([[A + 6, OFFICE + 5], [416, DEPOT - 6]]),
        link([[B1 + 6, DEPOT - 5], [636, OFFICE + 6]]),
        link([[C1 + 6, OFFICE + 5], [866, DEPOT - 6]]),
        dot(420, DEPOT, 3.5, C.text2), code(420, DEPOT + 22, "CaughtUp", { anchor: "middle" }),
        dot(870, DEPOT, 3.5, C.text2), code(870, DEPOT + 22, "CaughtUp", { anchor: "middle" }),

        dot(A, OFFICE, 5, C.text), code(A, OFFICE - 28, "Commit()", { anchor: "middle", fill: C.text }), text(A, OFFICE - 12, "PX-5001, Ada", { size: 11, fill: C.muted, anchor: "middle" }),
        dot(B1, DEPOT, 5, C.text), code(B1, DEPOT + 22, "Commit()", { anchor: "middle", fill: C.text }), code(B1, DEPOT + 38, 'Depot = "Lyon"', { size: 11, anchor: "middle", fill: C.muted }),
        dot(640, OFFICE, 3.5, C.text2), code(640, OFFICE - 12, "CaughtUp", { anchor: "middle" }),
        dot(C1, OFFICE, 5, C.text), code(C1, OFFICE - 28, "Commit()", { anchor: "middle", fill: C.text }), code(C1, OFFICE - 12, 'Recipient = "Ada Lovelace"', { size: 11, anchor: "middle", fill: C.muted }),

        text(PAD, 330, "Both replicas, one sequence", { size: 12, weight: 600, fill: C.text }),
        chip(224, 312, 150, "PX-5001, Ada"),
        link([[374, 328], [398, 328]]),
        chip(398, 312, 170, 'Depot = "Lyon"'),
        link([[568, 328], [592, 328]]),
        chip(592, 312, 230, 'Recipient = "Ada Lovelace"'),
        timeLegend(380, code(RIGHT, 380, "TokenGeneration >= generation + 2", { size: 11, anchor: "end", fill: C.muted })),
    ]);
}

// Lesson 7, ConflictOnAParcelChangedOnAnotherServer and RefuseAUniqueValueTakenOnAnotherServer: the depot commits first, the office conflicts.
{
    const ROWS = [
        { name: "The same parcel", why: ["The commit of the depot is kept on", "both servers; the change of the", "office is on none"], first: 'Recipient = "Ada Lovelace"', second: 'Depot = "Lyon"', kind: "Change" },
        { name: "The same unique value", why: ["One parcel of that number, the", "one of the depot, on both", "servers"], first: "PX-6002, Ada", second: "PX-6002, Bob", kind: "Uniqueness" },
    ];
    const AT = [420, 586, 700, 830], FROM = 322, HEIGHT = 122;
    const writer = (y, name, write, xWrite, xCommit) => [
        text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }),
        line(FROM, y, xCommit, y),
        dot(xWrite, y, 4, C.text2), code(xWrite, y - 10, write, { anchor: "middle", fill: C.text }),
        code(xCommit, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    ];
    card("T008-conflicts-across-servers.svg", {
        h: 348,
        title: "Two servers are two writers: the first commit wins",
        sub: "The office writes, then the depot writes and commits first: the commit of the office finds what changed",
        alt: "Two scenarios of two servers, each on the last commit of its replica. The office changes the recipient of a parcel while the depot changes its depot and commits first: the commit of the office conflicts, of the kind Change; the commit of the depot is kept on both servers, the change of the office on none. Both give the number PX-6002 to a new parcel, for Ada at the office and for Bob at the depot: the commit of the office conflicts, of the kind Uniqueness, and one parcel of that number, Bob's, is on both servers.",
    }, ROWS.map((r, i) => {
        const y0 = 96 + i * HEIGHT, first = y0 + 40, second = y0 + 88;
        return [
            i > 0 && line(PAD, y0 - 2, RIGHT, y0 - 2, { stroke: C.grid }),
            text(PAD, y0 + 26, r.name, { weight: 600, fill: C.text }),
            r.why.map((s, k) => text(PAD, y0 + 46 + k * 16, s, { size: 12 })),
            writer(first, "The office", r.first, AT[0], AT[3]),
            writer(second, "The depot", r.second, AT[1], AT[2]),
            check(AT[2], second, { r: 6 }),
            link([[AT[2] + 12, second - 4], [AT[3] - 10, first + 6]], { weak: true, stroke: C.red }),
            cross(AT[3], first, { r: 6 }),
            text(AT[3] + 14, first + 4, "conflict", { size: 12, weight: 600, fill: C.red }),
            code(AT[3] + 14, first + 20, r.kind, { size: 11, fill: C.muted }),
        ];
    }));
}

// Lesson 8, CopyEachStoreOfTheChainToTheServersThatHoldIt: the whole chain on the writer, one Store on each of the two others.
{
    const L = 282, G = 24, N = 254, C3 = RIGHT - L / 2, C2 = C3 - L - G, C1 = C2 - L - G, TOP = 136, TALL = 52, LOW = 264, LOWH = 122, SAY = 230;
    const store = (cx, name, sub) => node(cx - N / 2, TOP, N, TALL, name, sub, { size: 12, mono: true, state: "held" });
    const server = (cx, name, sub, variable, mode, lines) => [
        owner(cx - L / 2, LOW, L, LOWH, name, sub),
        node(cx - L / 2 + 12, LOW + 30, L - 24, 42, variable, mode, { size: 12, mono: true, state: "held" }),
        lines.map((s, k) => text(cx - L / 2 + 12, LOW + 90 + k * 16, s, { size: 12 })),
    ];
    const copy = cx => [link([[cx, TOP + TALL], [cx, LOW]], { stroke: C.blue, sw: 2 }), text(cx + 12, SAY, "its transactions", { size: 12 })];
    card("T008-chain-over-servers.svg", {
        h: 408,
        title: "A chain is distributed whole; each server holds the Stores it needs",
        sub: "The cluster copies to each server the transactions of its Stores only",
        alt: "The writer holds the whole chain ReadWrite, the Stores parcels, tracking and journal, distributed by writerEndPoint.Add(parcels), and runs Parcels.Register, Parcels.Scan and Parcels.Deliver. The site holds the Store of the tracking alone, ReadOnly, with its Repository alone, and reads the page Delivered and the board Lyon: 0. The notifications hold the Log alone, ReadOnly, followed from its start by a subscription, and give Your parcel is at Lyon and Your parcel is delivered. No other server holds the parcels.",
    }, [
        owner(PAD, 100, RIGHT - PAD, 100, "The writer", "writerEndPoint.Add(parcels): the whole chain"),
        store(C1, "parcels", '"parcels", ReadWrite'),
        store(C2, "tracking", '"tracking", ReadWrite'),
        store(C3, "journal", '"journal", ReadWrite'),
        link([[C1 + N / 2, TOP + TALL / 2], [C2 - N / 2, TOP + TALL / 2]]),
        link([[C2 + N / 2, TOP + TALL / 2], [C3 - N / 2, TOP + TALL / 2]]),
        text(C1, SAY, "no other server holds it", { size: 12, fill: C.muted, anchor: "middle" }),
        copy(C2), copy(C3),
        text(PAD, LOW + 20, "The writer runs", { weight: 600, fill: C.text }),
        code(PAD, LOW + 48, 'Parcels.Register("PX-7001", "Ada")'),
        code(PAD, LOW + 70, 'Parcels.Scan(ada, "Lyon")'),
        code(PAD, LOW + 92, "Parcels.Deliver(ada)"),
        server(C2, "The site", "new TrackingRepository(siteTracking)", "siteTracking", '"tracking", ReadOnly', ["The page reads Delivered", "The board reads Lyon: 0"]),
        server(C3, "The notifications", "SubscribeFrom(0)", "notifierJournal", '"journal", ReadOnly', ["Your parcel is at Lyon", "Your parcel is delivered"]),
    ]);
}

// Lesson 9, GoOnWhenAWriterLeavesAndCopyTheStoreToAServerThatJoins: a writer leaves, the office goes on, a site joins.
{
    const OFFICE = 134, DEPOT = 226, SITE = 318, FROM = 150, A = 190, JOIN = 244, COPIED = 304, D = 384, BACK = 456, LEAVE = 500, E = 590, SITEIN = 636, WHOLE = 712, HELD = 758;
    card("T008-leave-and-join.svg", {
        h: 386,
        title: "A server leaves, a server joins, and nothing is lost",
        sub: "The replicas of one Store along the time: each commit copied, and the write token where it is",
        alt: "Along the time, the office commits PX-8001. The depot joins through the office, receives that commit, commits PX-8002 and takes the write token. It then leaves, its Repository, its Store, then its endpoint, once the office holds its last commit; the token goes to another writer, or is released. The commit of PX-8003 on the office takes the token. A site joins later through the office, with an empty directory; its replica receives the whole Store, and reads PX-8001, PX-8002 and PX-8003.",
    }, [
        lane(OFFICE, "The office", "ReadWrite", FROM, RIGHT),
        lane(DEPOT, "The depot", "ReadWrite", JOIN, LEAVE),
        lane(SITE, "The site", "ReadOnly", SITEIN, RIGHT),

        token(A, D - 16, OFFICE), token(D - 16, LEAVE, DEPOT), token(E - 16, RIGHT, OFFICE),
        link([[D - 16, OFFICE], [D - 16, DEPOT]], { stroke: C.blue, sw: 2 }),
        link([[LEAVE + 5, DEPOT - 6], [E - 20, OFFICE + 7]], { weak: true, stroke: C.blue }),
        text(BACK + 104, 172, "The token goes to another", { size: 11, fill: C.muted }),
        text(BACK + 104, 187, "writer, or is released", { size: 11, fill: C.muted }),

        link([[A + 6, OFFICE + 5], [COPIED - 4, DEPOT - 6]]),
        dot(COPIED, DEPOT, 3.5, C.text2),
        link([[D + 6, DEPOT - 5], [BACK - 4, OFFICE + 6]]),
        dot(BACK, OFFICE, 3.5, C.text2),
        link([[WHOLE, OFFICE + 5], [HELD - 4, SITE - 6]], { sw: 2 }),
        dot(HELD, SITE, 3.5, C.text2),
        text(HELD - 6, 230, "the whole Store", { size: 12, weight: 600, fill: C.text }),

        dot(A, OFFICE, 5, C.text), code(A, OFFICE - 14, "PX-8001", { anchor: "middle", fill: C.text }),
        dot(D, DEPOT, 5, C.text), code(D, DEPOT + 22, "PX-8002", { anchor: "middle", fill: C.text }),
        dot(LEAVE, DEPOT, 5, C.muted), text(D + 40, DEPOT + 20, "Leaves: its Repository, its Store, its endpoint", { size: 11, fill: C.muted }),
        dot(E, OFFICE, 5, C.text), code(E, OFFICE - 14, "PX-8003", { anchor: "middle", fill: C.text }),
        text(SITEIN, SITE + 20, "Joins, through the office, with an empty directory", { size: 11, fill: C.muted }),
        code(RIGHT, SITE - 12, "PX-8001, PX-8002, PX-8003", { size: 11, anchor: "end", fill: C.text }),
        timeLegend(368),
    ]);
}
