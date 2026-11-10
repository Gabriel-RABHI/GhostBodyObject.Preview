// The diagrams of Tutorial Six. Each one draws the very objects of a test of its lesson: run `node six-records.mjs` after a change
// of the lesson, or of the kit. Their words stay those of the developer: entities, Records, Views, bags, copies.
import { C, PAD, RIGHT, bar, block, card, check, code, cross, dot, line, link, node, owner, rect, steps, text } from "./kit.mjs";

// A Record where it is: a flat value, square next to the rounded box of an entity. Its key, when it has one, reads on its left.
const PAINT = { alive: [C.raised, C.base, C.text], held: [C.blueSoft, C.blue, C.text], gone: ["none", C.base, C.muted] };
const cell = (x, y, w, h, value, { state = "alive", key, sub, size = 12, mono = false } = {}) => {
    const [fill, stroke, ink] = PAINT[state], cy = y + h / 2;
    return [rect(x, y, w, h, { fill, stroke, sw: state === "held" ? 1.5 : 1, rx: 4, dash: state === "gone" ? "4 4" : undefined }),
        key ? [code(x + 10, cy + 4.3, key, { fill: ink === C.muted ? C.muted : C.text2 }), text(x + w - 10, cy + 4.3, value, { size, fill: ink, anchor: "end", mono })]
            : sub !== undefined ? [text(x + w / 2, cy - 3, value, { size, weight: 600, fill: ink, anchor: "middle", mono }), sub && text(x + w / 2, cy + 13, sub, { size: 11, fill: C.text2, anchor: "middle" })]
                : text(x + w / 2, cy + 4.3, value, { size, fill: ink, anchor: "middle", mono })];
};
// An entity that holds a Record: its name on its left, centered on its height, its Record beside it.
const host = (x, y, w, h, name) => [rect(x, y, w, h, { stroke: C.base, rx: 10 }), text(x + 14, y + h / 2 + 4.5, name, { weight: 600, fill: C.text })];

// Lesson 2, ShareAnEntityByALink and CopyARecordByAnAssignment: one second holder, two answers.
{
    const COL = 428, GAP = 40, col = i => PAD + i * (COL + GAP), mid = i => col(i) + COL / 2, TALL = 40, PITCH = 64, NAME = 96;
    const heading = (i, name, decl) => [text(col(i), 108, name, { weight: 600, fill: C.text }), code(col(i), 127, decl), line(col(i), 140, col(i) + COL, 140, { stroke: C.grid })];
    const change = (i, s) => [code(mid(i), 282, s, { anchor: "middle", fill: C.text }), link([[mid(i), 290], [mid(i), 312]], { stroke: C.base, sw: 1.2 })];
    const outcome = (i, s, proof) => [text(mid(i), 452, s, { weight: 600, fill: C.text, anchor: "middle" }), code(mid(i), 472, proof, { fill: C.muted, anchor: "middle" })];
    const hotel = (y, name, state) => {
        const x = col(0), hx = x + COL - 150;
        return [node(x, y, NAME, TALL, "Ada"), node(x, y + PITCH, NAME, TALL, "Bob"), node(hx, y, 150, PITCH + TALL, name, null, { state }),
            [y + TALL / 2, y + PITCH + TALL / 2].map(cy => [link([[x + NAME, cy], [hx, cy]], { weak: true }), code((x + NAME + hx) / 2, cy - 8, "Favorite", { anchor: "middle" })])];
    };
    const guest = (y, name, value, state = "alive") => [host(col(1), y, COL, TALL, name), cell(col(1) + NAME, y + 6, COL - NAME - 6, TALL - 12, value, { key: "Home", state })];
    card("six-link-or-copy.svg", {
        h: 494,
        title: "A link shares one entity, an assignment copies a Record",
        sub: "A second guest: the hotel stays one hotel, the address becomes two values",
        alt: "Two columns. On the left, Ada and Bob each hold a weak link, Favorite, to the one hotel Ritz: after ritz.Name = \"The Ritz\", both see The Ritz. On the right, Ada holds an address in Home, 12 rue de Rivoli, Paris, and Bob receives a copy of it by Home = ada.Home: after ada.Home takes a new address, 3 quai Saint-Antoine, Lyon, Bob still holds 12 rue de Rivoli, Paris: each one has its own.",
    }, [
        heading(0, "An entity, by a link", "[Weak] Option<MHotel> Favorite"),
        hotel(152, "Ritz", "alive"),
        change(0, 'ritz.Name = "The Ritz"'),
        hotel(320, "The Ritz", "held"),
        outcome(0, "Both see one entity", 'ada.Favorite.Get().Name == "The Ritz"'),

        heading(1, "A Record, by a copy", "RAddress Home"),
        guest(152, "Ada", "12 rue de Rivoli, Paris"),
        guest(216, "Bob", "12 rue de Rivoli, Paris"),
        link([[col(1) + 176, 186], [col(1) + 176, 222]]),
        code(col(1) + 188, 208, "Home = ada.Home"),
        change(1, 'ada.Home = new Address { ..., City = "Lyon" }'),
        guest(320, "Ada", "3 quai Saint-Antoine, Lyon", "held"),
        guest(384, "Bob", "12 rue de Rivoli, Paris"),
        outcome(1, "Each one has its own", 'bob.Home.City == "Paris"'),
    ]);
}

// Lesson 3, KeepTheValueThatAViewRead: two Views, one change of the guest.
{
    const COL = 428, GAP = 40, col = i => PAD + i * (COL + GAP), mid = i => col(i) + COL / 2, VIEW = 204, Y = 256;
    const view = (x, name, say, key, value, state) => [
        rect(x, Y, VIEW, 78, { stroke: C.base, rx: 8 }),
        code(x + 12, Y + 23, name, { fill: C.text, weight: 600 }),
        text(x + VIEW - 12, Y + 23, say, { size: 11, fill: C.muted, anchor: "end" }),
        cell(x + 12, Y + 36, VIEW - 24, 30, value, { key, state }),
    ];
    const stage = (i, lines, city, follow, keep) => {
        const x = col(i), second = x + COL - VIEW, changed = city !== "Paris" ? "held" : "alive";
        return [
            lines.map((s, k) => code(mid(i), 118 + k * 16, s, { anchor: "middle", fill: C.text })),
            host(x, 152, COL, 44, "Ada"),
            cell(x + 96, 158, COL - 102, 32, city, { key: "Home", state: changed }),
            view(x, "guest", "the View of the guest", "Home.City", city, changed),
            view(second, "before", "the View of the Record", "City", "Paris", "alive"),
            link([[x + 40, Y], [x + 40, 196]], { stroke: C.blue, sw: 2 }),
            text(x + 52, 230, follow, { size: 12 }),
            i === 0 && link([[second + 40, Y], [second + 40, 196]]),
            text(second + 52, 230, keep, { size: 12 }),
        ];
    };
    card("six-view-keeps.svg", {
        h: 356,
        title: "The View of an entity follows it, the View of a Record keeps what it read",
        sub: "Two Views taken of Ada, and what each one reads after a change of the guest",
        alt: "Two stages. First, guest is the View of the guest Ada and before is the View of the Record in its Home: both read Paris. Then ada.Home takes a new address in Lyon, and ada.Name a much longer name. The View of the entity reads the guest as it is now: guest.Home.City is Lyon. The View of the Record is a value and keeps what it read: before.City is still Paris.",
    }, [
        steps(94, [mid(0), mid(1)]),
        stage(0, ["var guest = ada.ToView();", "var before = ada.Home;"], "Paris", "reads the guest", "reads the Record"),
        stage(1, ['ada.Home = new Address { City = "Lyon" };', 'ada.Name = "A name long enough ...";'], "Lyon", "reads it as it is now", "keeps what it read"),
    ]);
}

// Lesson 4, ChangeOneFieldByACopy: the value goes out, changes in your hands, and comes back.
{
    const GUEST = 306, COPY = 672, WIDTH = 256, TOP = 140, PITCH = 52, TALL = 36;
    const y = i => TOP + i * PITCH, OLD = "12 rue de Rivoli, Paris", NEW = "14 rue de Rivoli, Paris";
    const ROWS = [
        { code: "var ada = new Guest { ... }", guest: [OLD] },
        { code: "var home = ada.Home.ToBody()", guest: [OLD], copy: [OLD], out: true },
        { code: 'home.Street = "14 rue de Rivoli"', guest: [OLD], copy: [NEW, "held"] },
        { code: "ada.Home = home", guest: [NEW, "held"], copy: [NEW], back: true },
    ];
    card("six-change-by-copy.svg", {
        h: 366,
        title: "To change one field, change a copy, then assign it",
        sub: "The Home of the guest and your copy, at each instruction",
        alt: "Four instructions. The guest Ada holds the address 12 rue de Rivoli, Paris. ada.Home.ToBody() copies it out into home. home.Street changes the copy to 14 rue de Rivoli, and the guest still reads 12 rue de Rivoli. ada.Home = home copies the value back: the guest reads 14 rue de Rivoli, Paris.",
    }, [
        code(GUEST + WIDTH / 2, 110, "ada.Home", { anchor: "middle", fill: C.text, weight: 600 }),
        text(GUEST + WIDTH / 2, 126, "in the guest", { size: 11, fill: C.muted, anchor: "middle" }),
        code(COPY + WIDTH / 2, 110, "home", { anchor: "middle", fill: C.text, weight: 600 }),
        text(COPY + WIDTH / 2, 126, "your copy", { size: 11, fill: C.muted, anchor: "middle" }),
        ROWS.map((r, i) => {
            const cy = y(i) + TALL / 2, gx = GUEST + WIDTH;
            return [
                i > 0 && line(PAD, y(i) - 8, RIGHT, y(i) - 8, { stroke: C.grid }),
                code(PAD, cy + 4, r.code, { fill: C.text }),
                cell(GUEST, y(i), WIDTH, TALL, r.guest[0], { state: r.guest[1] }),
                r.copy && cell(COPY, y(i), WIDTH, TALL, r.copy[0], { state: r.copy[1] }),
                r.out && [link([[gx, cy], [COPY, cy]]), text((gx + COPY) / 2, cy - 8, "copied out", { size: 11, anchor: "middle" })],
                r.back && [link([[COPY, cy], [gx, cy]], { stroke: C.blue, sw: 2 }), text((gx + COPY) / 2, cy - 8, "copied back", { size: 11, anchor: "middle" })],
            ];
        }),
    ]);
}

// Lesson 5, ChangeARecordInARecord: the city of Ada's contact, in the guest and in the two copies, at each instruction.
{
    const X = [400, 600, 800], WIDTH = 128, TOP = 140, PITCH = 44, TALL = 30, LONDON = "London", CAMBRIDGE = "Cambridge";
    const y = i => TOP + i * PITCH;
    const ROWS = [
        { code: "var ada = new Guest { ... }", cells: [LONDON] },
        { code: "var emergency = ada.Emergency.ToBody()", cells: [LONDON, LONDON], from: 0 },
        { code: "var address = emergency.Address.ToBody()", cells: [LONDON, LONDON, LONDON], from: 1 },
        { code: 'address.City = "Cambridge"', cells: [LONDON, LONDON, CAMBRIDGE] },
        { code: "emergency.Address = address", cells: [LONDON, CAMBRIDGE, CAMBRIDGE], to: 1 },
        { code: "ada.Emergency = emergency", cells: [CAMBRIDGE, CAMBRIDGE, CAMBRIDGE], to: 0 },
    ];
    const head = (i, s, say) => [code(X[i] + WIDTH / 2, 110, s, { anchor: "middle", fill: C.text, weight: 600 }), text(X[i] + WIDTH / 2, 126, say, { size: 11, fill: C.muted, anchor: "middle" })];
    card("six-nested-change.svg", {
        h: 424,
        title: "Each level is copied out, then assigned back to the level above",
        sub: "The city of the emergency contact of Ada, at each instruction",
        alt: "Six instructions, three columns. The guest Ada holds a contact, which holds an address in London. ada.Emergency.ToBody() copies the contact out. emergency.Address.ToBody() copies its address out. address.City becomes Cambridge in the last copy alone. emergency.Address = address assigns it to the copy of the contact. ada.Emergency = emergency assigns that to the guest, which now reads Cambridge.",
    }, [
        head(0, "ada.Emergency.Address.City", "in the guest"),
        head(1, "emergency.Address.City", "your copy"),
        head(2, "address.City", "your copy"),
        ROWS.map((r, i) => {
            const cy = y(i) + TALL / 2;
            return [
                i > 0 && line(PAD, y(i) - 7, RIGHT, y(i) - 7, { stroke: C.grid }),
                code(PAD, cy + 4, r.code, { fill: C.text }),
                r.cells.map((s, k) => cell(X[k], y(i), WIDTH, TALL, s, { state: s === CAMBRIDGE ? "held" : "alive" })),
                r.from !== undefined && [link([[X[r.from] + WIDTH, cy], [X[r.from + 1], cy]]), text(X[r.from] + WIDTH + 36, cy - 6, "copied", { size: 11, anchor: "middle" })],
                r.to !== undefined && [link([[X[r.to + 1], cy], [X[r.to] + WIDTH, cy]], { stroke: C.blue, sw: 2 }), text(X[r.to] + WIDTH + 36, cy - 6, "assigned", { size: 11, anchor: "middle" })],
            ];
        }),
    ]);
}

// Lesson 8, RemoveRecordsFromABag: the instructions of the test on one bag, each row what the instruction does to it.
{
    const CELLS = 314, STEP = 83, WIDTH = 78, TOP = 126, PITCH = 38, TALL = 28, BACK = 870, BF = "Breakfast", R = (n, state) => [`CleanRoom ${n}`, state];
    const ROWS = [
        { code: "RemoveAt(1)", cells: [R(12), R(14, "gone"), R(12, "held"), R(16, "held"), R(12, "held"), [BF, "held"]] },
        { code: "Remove(new CleanRoom { Room = 16 })", cells: [R(12), R(12), R(16, "gone"), R(12, "held"), [BF, "held"]], back: "true" },
        { code: "Remove(new CleanRoom { Room = 99 })", cells: [R(12), R(12), R(12), [BF]], back: "false" },
        { code: "RemoveAll(new CleanRoom { Room = 12 })", cells: [R(12, "gone"), R(12, "gone"), R(12, "gone"), [BF, "held"]], back: "3" },
        { code: "Add(new CleanRoom { Room = 14 })", cells: [[BF], R(14, "held")] },
        { code: "RemoveAll<BreakfastView>()", cells: [[BF, "gone"], R(14, "held")], back: "1" },
        { code: "Clear()", cells: [R(14, "gone")] },
    ];
    const y = i => TOP + i * PITCH;
    card("six-bag-writes.svg", {
        h: 412,
        title: "A removal moves the Records after it down",
        sub: "The positions of a bag, instruction by instruction: dashed, what it removes; blue, what it moves down or adds",
        alt: "A bag of six Records, CleanRoom 12, 14, 12, 16, 12 and a Breakfast, at positions 0 to 5. RemoveAt(1) removes CleanRoom 14, and the four Records after it move down by one place. Remove of CleanRoom 16 removes the first equal value and returns true. Remove of CleanRoom 99 finds none and returns false: nothing moves. RemoveAll of CleanRoom 12 removes the three equal values, returns 3, and the Breakfast comes to position 0. Add puts CleanRoom 14 at the end. RemoveAll<BreakfastView>() removes the Breakfast, returns 1, and CleanRoom 14 moves down. Clear() empties the bag.",
    }, [
        code(PAD, 112, "stay.Requests", { fill: C.muted }),
        [0, 1, 2, 3, 4, 5].map(p => text(CELLS + p * STEP + WIDTH / 2, 112, p, { size: 11, fill: C.muted, anchor: "middle" })),
        text(BACK, 112, "returns", { size: 11, fill: C.muted, anchor: "middle" }),
        ROWS.map((r, i) => [
            i > 0 && line(PAD, y(i) - 5, RIGHT, y(i) - 5, { stroke: C.grid }),
            code(PAD, y(i) + TALL / 2 + 4, r.code, { fill: C.text }),
            r.cells.map(([s, state], p) => cell(CELLS + p * STEP, y(i), WIDTH, TALL, s, { state, size: 11 })),
            r.back && code(BACK, y(i) + TALL / 2 + 4, r.back, { anchor: "middle", fill: C.text }),
        ]),
    ]);
}

// Lesson 9, WalkABagByAHandlerPerType and SkipTheTypesWithoutAHandler: the four Records of one history, two walks.
{
    const COL = 428, GAP = 40, col = i => PAD + i * (COL + GAP), SLOT = 100, PAD4 = 9.3, RECORD = 170, HANDLER = 256, OUT = 340;
    const slot = (i, k) => col(i) + k * (SLOT + PAD4), center = (i, k) => slot(i, k) + SLOT / 2;
    const RECORDS = [["CheckedIn", "Room 12"], ["RoomChanged", "12 to 14"], ["RoomChanged", "14 to 16"], ["CheckedOut", ""]];
    const heading = (i, name, decl) => [text(col(i), 108, name, { weight: 600, fill: C.text }), code(col(i), 127, decl), line(col(i), 140, col(i) + COL, 140, { stroke: C.grid })];
    const walk = i => [text(col(i), 160, "One walk, in their order", { size: 11, fill: C.muted }), link([[col(i) + 136, 156], [col(i) + COL, 156]], { stroke: C.base, sw: 1.2 })];
    const records = i => RECORDS.map(([name, sub], k) => cell(slot(i, k), RECORD, SLOT, 46, name, { sub }));
    const handler = (i, from, to, name) => node(slot(i, from), HANDLER, slot(i, to) + SLOT - slot(i, from), 40, name, null, { state: "held", size: 11, mono: true });
    const down = (i, k, y1, y2) => link([[center(i, k), y1], [center(i, k), y2]]);
    const out = (i, k, s) => [down(i, k, HANDLER + 40, OUT), cell(slot(i, k), OUT, SLOT, 30, s, { mono: true })];
    card("six-walk-handlers.svg", {
        h: 414,
        title: "One walk gives each Record to the handler of its type, in their order",
        sub: "The history of a stay: four Records of three types, and the two RoomChanged go to one handler",
        alt: "Two columns on the same four Records: CheckedIn room 12, RoomChanged 12 to 14, RoomChanged 14 to 16, CheckedOut. With three handlers, one walk gives each Record to the handler of its type, in their order, and the story reads in 12, to 14, to 16, out. With one handler, for RoomChanged, the Records of the other types are skipped, and rooms reads 14, 16.",
    }, [
        heading(0, "A handler per type", "Enumerate(story, h1, h2, h3)"),
        walk(0),
        records(0),
        [0, 1, 2, 3].map(k => down(0, k, RECORD + 46, HANDLER)),
        handler(0, 0, 0, "CheckedInView"),
        handler(0, 1, 2, "RoomChangedView"),
        handler(0, 3, 3, "CheckedOutView"),
        out(0, 0, '"in 12"'), out(0, 1, '"to 14"'), out(0, 2, '"to 16"'), out(0, 3, '"out"'),
        text(col(0), 392, "The list story, in the order of the Records", { size: 11, fill: C.muted }),

        heading(1, "A handler for one type", "Enumerate(rooms, h)"),
        walk(1),
        records(1),
        [1, 2].map(k => down(1, k, RECORD + 46, HANDLER)),
        handler(1, 1, 2, "RoomChangedView"),
        text(center(1, 0), HANDLER + 24, "skipped", { size: 12, fill: C.muted, anchor: "middle" }),
        text(center(1, 3), HANDLER + 24, "skipped", { size: 12, fill: C.muted, anchor: "middle" }),
        out(1, 1, "14"), out(1, 2, "16"),
        text(col(1), 392, "The list rooms", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 11, CopyABag: bound to a stay, free in your hands, bound to another stay.
{
    const X = [350, 558, 766], WIDTH = 162, TOP = 144, PITCH = 48, TALL = 32, CHIP = 78, ROOM = ["Room"], PARKING = ["Parking"];
    const ROWS = [
        { code: "ada.Charges.Add(new Charge { ... })", bags: [[["Room", "held"]], [], []] },
        { code: "var copy = ada.ToView().Charges.ToBody()", bags: [[ROOM], [ROOM], []], arrow: 0 },
        { code: "copy.Add(new Charge { ... })", bags: [[ROOM], [ROOM, ["Parking", "held"]], []] },
        { code: "var bob = new Stay { ..., Charges = copy }", bags: [[ROOM], [ROOM, PARKING], [ROOM, PARKING]], arrow: 1 },
        { code: "ada.Charges.Clear()", bags: [[["Room", "gone"]], [ROOM, PARKING], [ROOM, PARKING]] },
    ];
    const y = i => TOP + i * PITCH, start = r => X[r.arrow] + r.bags[r.arrow].length * (CHIP + 6) - 6;
    const head = (i, s, say) => [code(X[i], 110, s, { fill: C.text, weight: 600 }), text(X[i], 126, say, { size: 11, fill: C.muted })];
    card("six-bag-copies.svg", {
        h: 406,
        title: "The copy of a bag is free: to change, and to assign to another stay",
        sub: "The Records of each bag, at each instruction",
        alt: "Five instructions, three bags. ada.Charges receives a charge, Room. ada.ToView().Charges.ToBody() gives copy, a free bag that holds Room. copy.Add adds Parking to the copy alone: ada.Charges still counts 1. The new stay of Bob receives the copy by Charges = copy, and holds Room and Parking. ada.Charges.Clear() empties the bag of Ada: the stay of Bob keeps its two charges.",
    }, [
        head(0, "ada.Charges", "bound to the stay of Ada"),
        head(1, "copy", "free: it belongs to nothing"),
        head(2, "bob.Charges", "bound to the stay of Bob"),
        ROWS.map((r, i) => {
            const cy = y(i) + TALL / 2;
            return [
                i > 0 && line(PAD, y(i) - 8, RIGHT, y(i) - 8, { stroke: C.grid }),
                code(PAD, cy + 4, r.code, { fill: C.text }),
                r.bags.map((bag, k) => bag.map(([s, state], c) => cell(X[k] + c * (CHIP + 6), y(i), CHIP, TALL, s, { state }))),
                r.arrow !== undefined && [link([[start(r), cy], [X[r.arrow + 1], cy]]), text((start(r) + X[r.arrow + 1]) / 2, cy - 7, "copied", { size: 11, anchor: "middle" })],
            ];
        }),
    ]);
}

// Lesson 12, ReadARecordAtAnyDepth: built from the inside, read as a whole.
{
    const ORDER = 536;
    const level = (x, y, w, h, bag, author, say, n, instruction) => [
        code(x, y - 8, bag),
        rect(x, y, w, h, { stroke: C.base, rx: 4 }),
        text(x + 12, y + 26, author, { weight: 600, fill: C.text }),
        text(x + 48, y + 26, say, { size: 13 }),
        text(ORDER, y + 26, n, { weight: 600, fill: C.text, anchor: "middle" }),
        code(ORDER + 16, y + 26, instruction, { fill: C.text }),
    ];
    card("six-replies.svg", {
        h: 390,
        title: "Replies of replies stay in one entity",
        sub: "Built from the inside: each Add copies a comment into the bag above it, the last one in a transaction",
        alt: "A review of the Ritz, one entity, holds in its bag Comments a comment of Ada, A wonderful view. That comment holds in its bag Replies a comment of Bob, Which room? It holds in its own bag Replies a comment of Ada, Number 12. The test builds them from the inside: first reply.Replies.Add, then comment.Replies.Add(reply), then the Add on the bag of the review, in a transaction. CountAll follows the replies, and reads the 3 comments in place.",
    }, [
        owner(PAD, 100, 896, 248, "A review of the Ritz", "one entity", { state: "held" }),
        level(48, 152, 864, 184, "Comments", "Ada", "A wonderful view.", "3", "new Review { ... }.Comments.Add(comment)"),
        level(64, 212, 832, 112, "Replies", "Bob", "Which room?", "2", "comment.Replies.Add(reply)"),
        level(80, 272, 800, 40, "Replies", "Ada", "Number 12.", "1", "reply.Replies.Add(new Comment { ... })"),
        text(PAD, 372, "CountAll follows the replies, in place, and reads the 3 comments", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 13, ConflictOnTheHostOfTwoRecords and LeaveTwoPartsToTwoTransactions: the same change, in the lanes of Tutorial Two.
{
    const AT = [440, 560, 672, 780], FROM = 372, HEIGHT = 150;
    const ROWS = [
        { name: "Its lines as Records", decl: "RecordBag<RBillLine> Lines", first: "Change(0, 2_500)", second: "Change(1, 500)", conflict: true },
        { name: "Its lines as parts", decl: "IEntityCollection<MLine> Lines", first: 'Change("Wine", 2_500)', second: 'Change("Coffee", 500)', conflict: false },
    ];
    const lane = (y, name, write, xWrite, xCommit) => [
        text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }),
        line(FROM, y, xCommit, y),
        dot(xWrite, y, 4, C.text2), code(xWrite, y - 10, write, { anchor: "middle", fill: C.text }),
        code(xCommit, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    ];
    card("six-conflict-host.svg", {
        h: 408,
        title: "The host is the unit of conflict of its Records",
        sub: "Two concurrent transactions change two different lines: the later commit conflicts when they changed one entity",
        alt: "One bill of Ada, two lines, Wine at 3,000 and Coffee at 400, written twice. As Records, the two lines live in one entity: a first transaction changes line 0, a second one changes line 1 and commits, and the commit of the first one conflicts. As parts, each line is an entity: the same two changes both commit.",
    }, ROWS.map((r, i) => {
        const y0 = 96 + i * HEIGHT, first = y0 + 54, second = y0 + 112, box = y0 + 52, inner = box + 36;
        return [
            i > 0 && line(PAD, y0 - 2, RIGHT, y0 - 2, { stroke: C.grid }),
            text(PAD, y0 + 22, r.name, { weight: 600, fill: C.text }),
            code(PAD, y0 + 41, r.decl),
            owner(PAD, box, 300, 80, "The bill of Ada", r.conflict ? "one entity" : "each line, an entity", { state: r.conflict ? "refused" : "alive" }),
            r.conflict ? [cell(44, inner, 132, 32, "3_000", { key: "Wine", mono: true }), cell(188, inner, 132, 32, "400", { key: "Coffee", mono: true })]
                : [cell(44, inner, 132, 32, "3_000", { key: "Wine", mono: true, state: "held" }), cell(188, inner, 132, 32, "400", { key: "Coffee", mono: true, state: "held" })],
            lane(first, "T1", r.first, AT[0], AT[3]),
            lane(second, "T2", r.second, AT[1], AT[2]),
            check(AT[2], second, { r: 6 }),
            r.conflict ? [link([[AT[2] + 12, second], [AT[3], second], [AT[3], first + 10]], { weak: true, stroke: C.red }), cross(AT[3], first, { r: 6 }),
                text(AT[3] + 14, first + 4, "conflict", { size: 12, weight: 600, fill: C.red })]
                : [check(AT[3], first, { r: 6 }), text(AT[3] + 14, first + 4, "committed", { size: 12 })],
        ];
    }));
}

// Lesson 14: the table of the lesson, in the bars of the charts of the README.
{
    const AXIS = [124, 398, 672], LENGTH = 186, TOP = 142, PITCH = 56, BAR = 14, GAP = 4, KEY = 806;
    const LINES = ["10 lines", "100 lines", "1,000 lines"];
    const COLUMNS = [
        { name: "Read a line", say: "through the Views", max: 196, rows: [[173, 40], [193, 42], [196, 43]], show: v => `${v} M/s`, end: "4 to 5 times faster, at any size" },
        { name: "Change one line", say: "in a transaction of its own", max: 479_000, rows: [[434_000, 479_000], [136_000, 399_000], [15_800, 105_900]], show: v => `${v.toLocaleString("en-US")}/s`, end: "7 times slower at 1,000 lines" },
        { name: "Allocated per change", say: "the same at any size for a part", max: 150, rows: [[4.0, 3.4], [17.4, 3.5], [150, 3.3]], show: v => `${v === 150 ? "150" : v.toFixed(1)} KB`, end: "One change rewrites the bag" },
    ];
    const bottom = TOP + 3 * PITCH - 16;
    card("six-read-change.svg", {
        h: 372,
        title: "Records read faster at any size, and change slower as the bag grows",
        sub: "The two bills of Lesson 13 · each column on its own linear scale",
        alt: "The measures of the lesson as bars, Records against parts, for 10, 100 and 1,000 lines in the bill. Reading a line through the Views: 173, 193, 196 M/s for Records, 40, 42, 43 M/s for parts. Changing one line: 434,000, 136,000, 15,800 changes/s for Records, 479,000, 399,000, 105,900 for parts. Allocated per change: 4.0, 17.4, 150 KB for Records, 3.4, 3.5, 3.3 KB for parts. Records read 4 to 5 times faster at any size, and change 7 times slower at 1,000 lines: one change rewrites the bag.",
    }, [
        rect(KEY, 58, 12, 12, { fill: C.blue, rx: 3 }), text(KEY + 18, 68, "Records", { size: 12 }),
        rect(KEY + 76, 58, 12, 12, { fill: C.grey, rx: 3 }), text(KEY + 94, 68, "parts", { size: 12 }),
        COLUMNS.map((c, k) => {
            const x = AXIS[k], scale = v => x + Math.max(v / c.max * LENGTH, 2);
            return [
                text(x, 106, c.name, { weight: 600, fill: C.text }),
                text(x, 122, c.say, { size: 11, fill: C.muted }),
                line(x, 132, x, bottom),
                c.rows.map(([record, part], r) => {
                    const y = TOP + r * PITCH, second = y + BAR + GAP;
                    return [
                        bar(x, scale(record), y, BAR, C.blue), text(scale(record) + 8, y + 11.5, c.show(record), { fill: C.text, weight: 600 }),
                        bar(x, scale(part), second, BAR, C.grey), text(scale(part) + 8, second + 11.5, c.show(part)),
                    ];
                }),
                text(x, bottom + 26, c.end, { size: 12, fill: C.text }),
            ];
        }),
        LINES.map((s, r) => text(112, TOP + r * PITCH + 21, s, { fill: C.text, anchor: "end" })),
        text(PAD, 354, "A thousand bills of each kind, one thread, a Store in memory, each line changed in a transaction of its own · Intel i9-9900K, .NET 10", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 15, KeepARecordInALogAndInABag: from the fact of one model to the value of another.
{
    const LEFT = PAD, WIDTH = 272, MID = 340, MIDDLE = 280, RIGHT0 = RIGHT - WIDTH, Y = 134, H = 190, F = 34, ROW = Y + 54;
    const fields = (x, y, w, rows) => rows.map(([key, value], i) => cell(x, y + i * F, w, 28, value, { key }));
    card("six-log-to-bag.svg", {
        h: 352,
        title: "The Log keeps the fact, the room writes a value of its own from it",
        sub: "A Record type belongs to one model: what goes from one to the other is a value",
        alt: "Three steps. new RoomBooked with the guest Ada, room 12, on Monday, is added to BookingLog, whose model includes RRoomBooked: a fact. log.SubscribeFrom delivers a RoomBookedView, a value kept after its subscription. From it, the code writes a Stay, its Guest from the Guest of the fact, its From from On, into the Bookings of the room 12 of RoomRepository, whose model includes RStay. Neither model depends on the other.",
    }, [
        steps(94, [LEFT + WIDTH / 2, MID + MIDDLE / 2, RIGHT0 + WIDTH / 2]),
        code(LEFT + WIDTH / 2, 118, "new RoomBooked { ... }.Add()", { anchor: "middle", fill: C.text }),
        code(MID + MIDDLE / 2, 118, "log.SubscribeFrom(0, ...)", { anchor: "middle", fill: C.text }),
        code(RIGHT0 + WIDTH / 2, 118, "Bookings.Add(new Stay { ... })", { anchor: "middle", fill: C.text }),

        owner(LEFT, Y, WIDTH, H, "BookingLog", "RoomBooked, a fact"),
        fields(LEFT + 12, Y + 36, WIDTH - 24, [["Guest", "Ada"], ["Room", "12"], ["On", "Monday"]]),
        code(LEFT + 12, Y + H - 14, "IncludeRecordModel(nameof(RRoomBooked))", { size: 11, fill: C.muted }),

        block(MID + 34, ROW - 32, MIDDLE - 68, 64, [{ s: "RoomBookedView", mono: true, fill: C.text }, { s: "a value, kept after its subscription", size: 11 }], { state: "held" }),
        link([[LEFT + WIDTH, ROW], [MID + 34, ROW]], { stroke: C.blue, sw: 2 }),
        link([[MID + MIDDLE - 34, ROW], [RIGHT0, ROW]], { stroke: C.blue, sw: 2 }),
        code(MID + MIDDLE / 2, Y + 118, "Guest = delivered.Single().Guest", { anchor: "middle", fill: C.text }),
        code(MID + MIDDLE / 2, Y + 136, "From = delivered.Single().On", { anchor: "middle", fill: C.text }),
        text(MID + MIDDLE / 2, Y + H - 14, "Neither model depends on the other", { size: 11, fill: C.muted, anchor: "middle" }),

        owner(RIGHT0, Y, WIDTH, H, "RoomRepository", "a Stay, its own value"),
        owner(RIGHT0 + 12, Y + 32, WIDTH - 24, 112, "Room 12", "Bookings"),
        fields(RIGHT0 + 24, Y + 64, WIDTH - 48, [["Guest", "Ada"], ["From", "Monday"]]),
        code(RIGHT0 + 12, Y + H - 14, "IncludeRecordModel(nameof(RStay))", { size: 11, fill: C.muted }),
    ]);
}
