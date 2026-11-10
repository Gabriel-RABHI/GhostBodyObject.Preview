// The diagrams of Tutorial Five. Each one draws the very objects of a test of its lesson: run `node five-indexes.mjs` after a change
// of the lesson, or of the kit. Their words stay those of the developer: words, values, conditions, searches, commits.
import { C, PAD, RIGHT, block, card, check, code, count, cross, dot, legend, line, link, node, rect, ring, steps, text } from "./kit.mjs";

const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o }), S = s => ({ s, size: 11 });

// A value in a row: found, in view, or left aside.
const chip = (x, y, w, h, s, kind = "alive") => kind === "left"
    ? [rect(x, y, w, h, { stroke: C.grid }), code(x + w / 2, y + h / 2 + 4.3, s, { anchor: "middle", fill: C.muted })]
    : node(x, y, w, h, s, null, { state: kind, size: 12, mono: true });
const found = (x, y, yes) => yes ? check(x, y, { r: 6 }) : line(x - 5, y, x + 5, y, { stroke: C.muted, sw: 2 });

// Lesson 1, FindTheProductsOfAWord: the same four products, what a query reads and what a search reads.
{
    const COL = 432, GAP = 32, x0 = i => PAD + i * (COL + GAP), SOURCE = 112, RAIL = 132, AT = 152, BOX = 280, TALL = 44, TOP = 152, PITCH = 52;
    const PRODUCTS = [["Coffee Mug", "A white mug for your morning coffee."], ["Espresso Machine", "Brews a strong coffee in 30 seconds."], ["Tea Pot", "A glass pot for green tea."], ["Café de Colombia", "Beans roasted in Medellín."]];
    const cy = k => TOP + k * PITCH + TALL / 2, mid = (cy(0) + cy(3)) / 2;
    const panel = (i, name, call, source, read) => {
        const x = x0(i);
        return [
            text(x, 108, name, { weight: 600, fill: C.text }), code(x, 127, call), line(x, 140, x + COL, 140, { stroke: C.grid }),
            block(x, mid - 24, SOURCE, 48, source),
            PRODUCTS.map(([product, description], k) => [
                node(x + AT, TOP + k * PITCH, BOX, TALL, product, description, { state: read.includes(k) ? "held" : "alive" }),
                read.includes(k) && link([[x + SOURCE, mid], [x + RAIL, mid], [x + RAIL, cy(k)], [x + AT, cy(k)]], { stroke: C.blue }),
            ]),
            count(x + 8, 386, read.length), text(x + 24, 385, "products read, to find 2", { fill: C.text }),
        ];
    };
    card("five-query-or-index.svg", {
        h: 434,
        title: "A query reads every product, a text index those of its word",
        sub: "The four products of the lesson, and what each one reads to find coffee",
        alt: "Two panels over the same four products: Coffee Mug, Espresso Machine, Tea Pot and Café de Colombia. A query by LINQ, Description.Contains(\"coffee\"), reads the text of the four products to find 2. A text index, ProductTextIndex.Search(\"coffee\"), goes from the word to the two products that have it, Coffee Mug and Espresso Machine, and reads neither Tea Pot nor Café de Colombia. On a million products, the query reads a million descriptions, and the search still reads the products that have the word.",
    }, [
        panel(0, "A query by LINQ", "Description.Contains(\"coffee\")", [B("Each text"), S("every character")], [0, 1, 2, 3]),
        panel(1, "A text index", "ProductTextIndex.Search(\"coffee\")", [M("coffee", { size: 13, weight: 600, fill: C.text }), S("a word")], [0, 1]),
        text(PAD, 416, "On a million products, the query reads a million descriptions; the search still reads the products that have the word", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 2, CombineTheWords: the words of five products, and what three queries find.
{
    const NAME = 196, GROUP = 244, ROW = 40, FIRST = 174, COLS = [40, 112, 184];
    const PRODUCTS = ["Coffee Mug", "Espresso Machine", "Tea Pot", "Tea Mug", "Espresso Cups"];
    const HAS = { coffee: ["Coffee Mug", "Espresso Machine", "Tea Mug"], tea: ["Tea Pot", "Tea Mug"], mug: ["Coffee Mug", "Tea Mug"], pot: ["Tea Pot"], cups: ["Espresso Cups"] };
    const QUERIES = [
        { query: "tea +cups", words: [["tea", "required"], ["+cups", "adds"]], finds: ["Tea Pot", "Tea Mug", "Espresso Cups"] },
        { query: "coffee -tea", words: [["coffee", "required"], ["-tea", "excludes"]], finds: ["Coffee Mug", "Espresso Machine"] },
        { query: "-mug -pot", words: [["-mug", "excludes"], ["-pot", "excludes"]], finds: ["Espresso Machine", "Espresso Cups"] },
    ];
    const has = (x, y, on, excluded) => on ? dot(x, y, 6, excluded ? C.red : C.blue) : ring(x, y, 6, C.base);
    card("five-query-language.svg", {
        h: 396,
        title: "A word requires, + adds, - takes off",
        sub: "The five products of the lesson, the words they have, and what three queries find",
        alt: "Five products and five words. Coffee Mug has coffee and mug. Espresso Machine has coffee. Tea Pot has tea and pot. Tea Mug has coffee, tea and mug. Espresso Cups has cups. The query tea +cups finds Tea Pot, Tea Mug and Espresso Cups. The query coffee -tea finds Coffee Mug and Espresso Machine: Tea Mug has coffee, and tea excludes it. The query -mug -pot starts from every product and finds Espresso Machine and Espresso Cups.",
    }, [
        line(PAD, 152, NAME - 8, 152, { stroke: C.base }),
        QUERIES.map((q, i) => {
            const x0 = NAME + i * GROUP;
            return [
                code(x0 + GROUP / 2, 106, q.query, { anchor: "middle", fill: C.text, weight: 600 }),
                line(x0 + 8, 152, x0 + GROUP - 8, 152, { stroke: C.base }),
                q.words.map(([word, role], c) => [text(x0 + COLS[c], 128, word, { size: 12, weight: 600, fill: C.text, anchor: "middle", mono: true }), text(x0 + COLS[c], 143, role, { size: 11, fill: C.muted, anchor: "middle" })]),
                text(x0 + COLS[2], 128, "Found", { size: 12, weight: 600, fill: C.text, anchor: "middle" }),
                PRODUCTS.map((product, k) => {
                    const y = FIRST + k * ROW;
                    return [
                        q.words.map(([word], c) => has(x0 + COLS[c], y, HAS[word.replace(/^[+-]/, "")].includes(product), word[0] === "-")),
                        found(x0 + COLS[2], y, q.finds.includes(product)),
                    ];
                }),
            ];
        }),
        PRODUCTS.map((product, k) => {
            const y = FIRST + k * ROW;
            return [text(PAD, y + 4, product, { fill: C.text }), k < PRODUCTS.length - 1 && line(PAD, y + 20, RIGHT, y + 20, { stroke: C.grid })];
        }),
        text(PAD, 378, "A filled dot: the product has the word, in its name or its description. Red: a word that excludes it", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 4, CutALongTextAtTheCommit: the six writes of the description, and when its words are cut, in the two modes.
{
    const WRITES = [240, 298, 356, 414, 472, 530], SEARCH = 650, COMMIT = 772, CUTS = 888, ROWS = [170, 234];
    const columns = [...WRITES, SEARCH, COMMIT];
    const lane = (y, name, say, cuts) => [
        code(PAD, y - 2, name, { fill: C.text }),
        text(PAD, y + 15, say, { size: 11, fill: C.muted }),
        [WRITES[0] - 28, ...cuts.flatMap(x => [x - 23, x + 23]), COMMIT + 28].map((x, i, ends) => i % 2 === 0 && line(x, y, ends[i + 1], y)),
        columns.map(x => cuts.includes(x) ? [rect(x - 23, y - 14, 46, 28, { fill: C.blueSoft, stroke: C.blue, sw: 1.5 }), text(x, y + 4.5, "cut", { size: 12, weight: 600, fill: C.text, anchor: "middle" })] : dot(x, y, 3.5, C.text2)),
        count(CUTS, y + 6, cuts.length),
    ];
    card("five-cut-moment.svg", {
        h: 300,
        title: "Immediate cuts the words at each write, Commit once",
        sub: "The six writes of the description of the Tasting Box, then a search and the commit",
        alt: "The same instructions in two rows: six writes of the description, the search of ethiopia, then the commit. With TextIndexing.Immediate, the default, each of the six writes cuts the words: 6 cuts. With TextIndexing.Commit, no write cuts them; the first search cuts them once, and sees the last text: 1 cut. Without a search, the cut falls at the commit. Both modes give the same results.",
    }, [
        steps(94, columns),
        WRITES.map((x, i) => code(x, 118, `Day ${i + 1}`, { anchor: "middle", fill: C.text })),
        text((WRITES[0] + WRITES[5]) / 2, 136, "product.Description = ...", { size: 11, fill: C.muted, anchor: "middle", mono: true }),
        code(SEARCH, 118, "Search(\"ethiopia\")", { anchor: "middle", fill: C.text }),
        text(SEARCH, 136, "the first search", { size: 11, fill: C.muted, anchor: "middle" }),
        code(COMMIT, 118, "Commit()", { anchor: "middle", fill: C.text }),
        text(CUTS, 118, "Cuts", { size: 12, weight: 600, fill: C.text, anchor: "middle" }),
        lane(ROWS[0], "TextIndexing.Immediate", "the default, for a short text", columns.slice(0, 6)),
        lane(ROWS[1], "TextIndexing.Commit", "for a long text, written in steps", [SEARCH]),
        text(PAD, 282, "Both modes give the same results; only the moment of the cut changes. Without a search, the cut falls at the commit", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 5, ReadTheOrdersOfAPeriod: the ten orders in the order of their dates, and what each condition takes.
{
    const ORDERS = [[1040, "09:00"], [1041, "13:00"], [1042, "17:00"], [1043, "21:00"], [1044, "01:00"], [1045, "05:00"], [1046, "09:00"], [1047, "13:00"], [1048, "17:00"], [1049, "21:00"]];
    const PITCH = 63, BOX = 56, X0 = RIGHT - 10 * PITCH + (PITCH - BOX), at = i => X0 + i * PITCH, TUESDAY = at(4) - (PITCH - BOX) / 2, HEAD = 112, TOP = 176, ROW = 38, TALL = 28;
    const CONDITIONS = [
        { call: "From(tuesday)", take: [4, 5, 6, 7, 8, 9] },
        { call: "Before(tuesday)", take: [0, 1, 2, 3] },
        { call: "Between(Monday, Monday.AddHours(8))", take: [0, 1], end: "left out" },
        { call: "Range(Monday, Monday.AddHours(8))", take: [0, 1, 2], end: "included" },
        { call: "Every", take: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9] },
    ];
    card("five-conditions.svg", {
        h: 390,
        title: "Each condition takes a stretch of the index",
        sub: "OrderPlacedIndex and the ten orders of the lesson, always in the order of their dates",
        alt: "The ten orders 1040 to 1049, one every four hours from Monday 09:00 to Tuesday 21:00, in the order of their dates. From(tuesday) takes 1044 to 1049; Before(tuesday) takes 1040 to 1043. Between(Monday, Monday.AddHours(8)) takes 1040 and 1041, and leaves out its end, 1042. Range with the same bounds takes 1040, 1041 and 1042, its end included. Every takes the ten orders.",
    }, [
        code(PAD, HEAD + 20, "OrderPlacedIndex", { fill: C.text }),
        text(PAD, HEAD + 36, "the order of their dates", { size: 11, fill: C.muted }),
        text(at(0), 104, "Monday", { size: 12, weight: 600, fill: C.text }),
        text(at(4), 104, "Tuesday", { size: 12, weight: 600, fill: C.text }),
        line(TUESDAY, 94, TUESDAY, TOP + 2 * ROW - 10, { stroke: C.text2, sw: 1.2 }),
        ORDERS.map(([number, time], i) => node(at(i), HEAD, BOX, 44, number, time, { size: 12, mono: true })),
        line(PAD, 166, RIGHT, 166, { stroke: C.grid }),
        CONDITIONS.map(({ call, take, end }, k) => {
            const y = TOP + k * ROW, last = take.at(-1);
            return [
                code(PAD, y + 18, call, { fill: C.text }),
                take.map(i => chip(at(i), y, BOX, TALL, ORDERS[i][0], "held")),
                end === "left out" && chip(at(2), y, BOX, TALL, ORDERS[2][0], "left"),
                end && text(at(end === "left out" ? 3 : last + 1), y + 18, `its end, ${end}`, { size: 12 }),
            ];
        }),
    ]);
}

// Lesson 6, FixTheComponentsInTheirOrder: the 24 orders as the index keeps them, and the searches of its prefixes.
{
    const STATUSES = ["Pending", "Paid", "Shipped", "Delivered"], DAYS = ["Mon", "Tue", "Wed"], BOX = 25, TALL = 24, X0 = 202, TOP = 178, ROW = 34;
    // Every order at its place: the status, then the day, as the shop of the lesson creates them.
    const orders = [], days = [];
    let x = X0;
    const spans = STATUSES.map((status, s) => {
        const from = x;
        DAYS.forEach((day, d) => {
            days.push({ day, x: x + BOX + 1.5 });
            for (const k of [0, 1]) {
                orders.push({ number: 100 + s + 8 * d + 4 * k, status, day, x });
                x += BOX + (k === 0 ? 3 : d < 2 ? 6 : 14);
            }
        });
        return { status, mid: (from + x - 14) / 2 };
    });
    const STYLE = { alive: [C.raised, C.base, 1], held: [C.blueSoft, C.blue, 1.5], refused: [C.redSoft, C.red, 1.5] };
    const small = (y, numbers, state) => numbers.map(n => {
        const { x } = orders.find(o => o.number === n), [fill, stroke, sw] = STYLE[state];
        return [rect(x, y, BOX, TALL, { fill, stroke, sw, rx: 5 }), text(x + BOX / 2, y + 16, n, { size: 11, weight: 600, fill: C.text, anchor: "middle", mono: true })];
    });
    const PAID = [101, 105, 109, 113, 117, 121];
    const SEARCHES = [
        ["paid = Is(Paid)", PAID],
        ["paid.Is(tuesday)", [109, 113]],
        ["paid.From(tuesday)", [109, 113, 117, 121]],
        ["Range(Pending, Paid)", [100, 104, 108, 112, 116, 120, ...PAID]],
    ];
    card("five-composite.svg", {
        h: 402,
        title: "A composite index reads as the words of a dictionary: the status, then the day",
        sub: "OrderStatusDayIndex and the 24 orders of the lesson; a search fixes the components in their order",
        alt: "The 24 orders in the order of the index: by status, Pending, Paid, Shipped, Delivered, then by day, Monday, Tuesday, Wednesday, two orders per day. Is(Paid) takes the six paid orders, in one stretch. paid.Is(tuesday) takes 109 and 113; paid.From(tuesday) takes 109 to 121. Range(Pending, Paid) takes the pending orders, then the paid ones. The orders of Tuesday, of any status, sit in four places: the components are never skipped, and this index has no such search.",
    }, [
        spans.map(({ status, mid }) => text(mid, 106, status, { size: 12, weight: 600, fill: C.text, anchor: "middle" })),
        days.map(({ day, x }) => text(x, 124, day, { size: 11, fill: C.muted, anchor: "middle" })),
        code(PAD, 149, "OrderStatusDayIndex", { fill: C.text }),
        small(132, orders.map(o => o.number), "alive"),
        line(PAD, 166, RIGHT, 166, { stroke: C.grid }),
        SEARCHES.map(([call, numbers], k) => [code(PAD, TOP + k * ROW + 16, call, { fill: C.text }), small(TOP + k * ROW, numbers, "held")]),
        line(PAD, TOP + 4 * ROW, RIGHT, TOP + 4 * ROW, { stroke: C.grid }),
        text(PAD, TOP + 4 * ROW + 24, "Tuesday, of any status", { size: 12, weight: 600, fill: C.red }),
        text(PAD, TOP + 4 * ROW + 40, "in four places: no such search", { size: 11, fill: C.muted }),
        small(TOP + 4 * ROW + 16, [108, 112, 109, 113, 110, 114, 111, 115], "refused"),
        text(PAD, 384, "The components are read in their order, never skipped: a period of any status is the search of another index, OrderPlacedIndex", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 7, KeepTheResultExactWithARoundedIndex: the nine totals in their euros, and the two searches of the text.
{
    const EUROS = [["9", ["9.99"]], ["10", ["10.00", "10.50"]], ["19", ["19.50", "19.99"]], ["20", ["20.00"]], ["25", ["25.50"]], ["49", ["49.99"]], ["50", ["50.00"]]];
    const CHIP = 50, INNER = 8, TALL = 26, HEAD = 26, HIGH = HEAD + TALL + INNER, GAP = 15, widthOf = n => n * CHIP + (n + 1) * INNER;
    const widths = EUROS.map(([, totals]) => widthOf(totals.length)), X0 = RIGHT - widths.reduce((a, b) => a + b, 0) - GAP * (EUROS.length - 1);
    const lefts = widths.map((_, i) => X0 + widths.slice(0, i).reduce((a, b) => a + b, 0) + i * GAP);
    const STYLE = { compared: [C.amberSoft, C.amber, 1.5, C.text], whole: [C.blueSoft, C.blue, 1.5, C.text], left: ["none", C.grid, 1, C.muted] };
    const euro = (x, y, w, name, kind, kinds, totals) => {
        const [fill, stroke, sw, ink] = STYLE[kind];
        return [
            rect(x, y, w, HIGH, { fill, stroke, sw }),
            text(x + INNER + 2, y + 18, name, { size: 12, weight: 600, fill: ink }),
            totals.map((total, t) => chip(x + INNER + t * (CHIP + INNER), y + HEAD, CHIP, TALL, total, kinds[t])),
        ];
    };
    const row = (y, call, say, cases, bounds) => [
        code(PAD, y + 20, call, { fill: C.text }),
        say.map((s, i) => text(PAD, y + 38 + i * 15, s, { size: 11, fill: C.muted })),
        EUROS.map(([name, totals], i) => {
            const c = cases[i], kind = Array.isArray(c) ? "compared" : c;
            return euro(lefts[i], y, widths[i], name, kind, Array.isArray(c) ? c : totals.map(() => c === "whole" ? "held" : "left"), totals);
        }),
        bounds.map(([x, label]) => [line(x, y - 6, x, y + HIGH + 4, { stroke: C.text, sw: 1.5 }), code(x, y - 11, label, { anchor: "middle", fill: C.text })]),
    ];
    const between = i => lefts[i] + INNER + CHIP + INNER / 2;
    const key = (x, kind, s) => { const [fill, stroke, sw] = STYLE[kind]; return [rect(x, 303, 20, 14, { fill, stroke, sw, rx: 3 }), text(x + 28, 314, s, { size: 11, fill: C.muted })]; };
    card("five-rounded.svg", {
        h: 332,
        title: "A rounded index stays exact: only a euro split by a bound is compared",
        sub: "OrderTotalIndex, by the euro, and the nine totals of the lesson",
        alt: "Nine totals in seven euros: 9.99 in the euro 9; 10.00 and 10.50 in the euro 10; 19.50 and 19.99 in the euro 19; 20.00, 25.50, 49.99 and 50.00 each in its own. From(19.99m) leaves the euros 9 and 10 whole, compares the euro 19, keeping 19.99 and leaving 19.50, and takes the following euros whole. Between(10.50m, 20m) leaves the euro 9, compares the euro 10, keeping 10.50 and leaving 10.00, takes the euro 19 whole, and leaves the euro 20 whole, without comparing anything: 20 is at the start of its euro.",
    }, [
        row(124, "From(19.99m)", ["19.99 falls inside its euro"], ["left", "left", ["left", "held"], "whole", "whole", "whole", "whole"], [[between(2), "19.99m"]]),
        row(226, "Between(10.50m, 20m)", ["10.50 falls inside its euro,", "20 at the start of its euro"], ["left", ["left", "held"], "whole", "left", "left", "left", "left"], [[between(1), "10.50m"], [lefts[3] - GAP / 2, "20m"]]),
        key(PAD, "whole", "taken whole, nothing compared"),
        key(238, "compared", "split by a bound: compared one by one to it"),
        key(508, "left", "left whole, nothing compared"),
    ]);
}

// Lesson 8, FindTheFlags: the flags of five orders, and what each condition finds.
{
    const ORDERS = [["Order 1", []], ["Order 2", ["Gift"]], ["Order 3", ["Gift", "Express"]], ["Order 4", ["Express", "Fragile"]], ["Order 5", ["Gift", "Express", "Fragile"]]];
    const FLAGS = ["Gift", "Express", "Fragile"], FLAG = [156, 222, 288], FIRST = 174, ROW = 40;
    const CONDITIONS = [
        ["Is", "Gift | Express", [3]],
        ["Has", "Gift", [2, 3, 5]],
        ["One", "Gift | Express", [2, 3, 4, 5]],
        ["All", "Gift | Express", [3, 5]],
        ["None", "Fragile", [1, 2, 3]],
    ];
    const at = i => 412 + i * 112;
    card("five-flags.svg", {
        h: 392,
        title: "Is wants exactly its flags; Has, One, All and None look at each flag",
        sub: "OrderOptionsIndex and the five orders of the lesson",
        alt: "Five orders and their flags. Order 1 has no flag; order 2 has Gift; order 3 has Gift and Express; order 4 has Express and Fragile; order 5 has all three. Is(Gift | Express) finds order 3 only, of exactly these flags. Has(Gift) finds 2, 3 and 5. One(Gift | Express) finds 2, 3, 4 and 5. All(Gift | Express) finds 3 and 5. None(Fragile) finds 1, 2 and 3.",
    }, [
        text(FLAG[1], 104, "Its flags", { size: 12, fill: C.muted, anchor: "middle" }),
        text((at(0) + at(4)) / 2, 104, "What each condition finds", { size: 12, fill: C.muted, anchor: "middle" }),
        FLAGS.map((flag, i) => text(FLAG[i], 128, flag, { size: 12, weight: 600, fill: C.text, anchor: "middle" })),
        CONDITIONS.map(([name, flags], i) => [text(at(i), 128, name, { size: 12, weight: 600, fill: C.text, anchor: "middle", mono: true }), text(at(i), 143, flags, { size: 11, fill: C.muted, anchor: "middle", mono: true })]),
        line(PAD, 152, RIGHT, 152, { stroke: C.base }),
        ORDERS.map(([name, flags], k) => {
            const y = FIRST + k * ROW;
            return [
                text(PAD, y + 4, name, { fill: C.text }),
                FLAGS.map((flag, i) => flags.includes(flag) ? dot(FLAG[i], y, 6, C.blue) : ring(FLAG[i], y, 6, C.base)),
                CONDITIONS.map(([, , numbers], i) => found(at(i), y, numbers.includes(k + 1))),
                k < ORDERS.length - 1 && line(PAD, y + 20, RIGHT, y + 20, { stroke: C.grid }),
            ];
        }),
        text(PAD, 374, "A filled dot: the order has the flag", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 9, RefuseAValueTakenMeanwhile: two transactions, one email, and the retry.
{
    const LABEL = 226, FIRST = 140, SECOND = 204, RETRY = 276, W1 = 340, W2 = 436, C2 = 600, C1 = 730, R1 = 830;
    const who = (y, name, say) => [text(PAD, y - 2, name, { weight: 600, fill: C.text }), say.map((s, i) => text(PAD, y + 16 + i * 16, s, { size: 12 }))];
    card("five-unique-race.svg", {
        h: 352,
        title: "Two transactions take one email: the first commit wins",
        sub: "Neither sees the other, so both Setters pass; the second commit is refused, and its retry meets the email at once",
        alt: "Two concurrent transactions. The first creates Ada with hello@example.com; the second creates Bob with Hello@Example.com, the same email whatever its case. Neither sees the other: both Setters pass. The second commits. The commit of the first is refused by a TransactionConflictException of the kind Uniqueness, and nothing of it is written. Its retry creates Ada with the same email: the Setter refuses it at once, by a GhostUniquenessException. CustomerEmailIndex.Is(\"hello@example.com\") finds Bob.",
    }, [
        who(FIRST, "The first transaction", ["Ada"]),
        line(LABEL, FIRST, C1, FIRST),
        dot(W1, FIRST, 4, C.text2), code(W1, FIRST - 12, "Email = \"hello@example.com\"", { anchor: "middle", fill: C.text }),
        code(C1, FIRST - 12, "Commit()", { anchor: "middle", fill: C.text }),
        cross(C1, FIRST, { r: 6 }),
        text(C1 + 14, FIRST + 4, "conflict", { size: 12, weight: 600, fill: C.red }),
        code(C1 + 72, FIRST + 4, "Uniqueness"),
        text(C1 + 14, FIRST + 20, "writes nothing", { size: 11, fill: C.muted }),

        who(SECOND, "The second", ["Bob, the same email,", "whatever its case"]),
        line(LABEL, SECOND, C2, SECOND),
        dot(W2, SECOND, 4, C.text2), code(W2, SECOND - 12, "Email = \"Hello@Example.com\"", { anchor: "middle", fill: C.text }),
        code(C2 + 6, SECOND - 12, "Commit()", { anchor: "end", fill: C.text }),
        check(C2, SECOND, { r: 6 }),
        text(C2, SECOND + 22, "committed", { size: 12, anchor: "middle" }),
        link([[C2 + 12, SECOND - 4], [C1 - 10, FIRST + 7]], { weak: true, stroke: C.red }),

        who(RETRY, "The retry of the first", ["Ada: it sees the email taken"]),
        link([[C1, FIRST + 10], [C1, RETRY], [R1 - 10, RETRY]], { stroke: C.base, sw: 1.2 }),
        code(RIGHT, RETRY - 12, "Email = \"hello@example.com\"", { anchor: "end", fill: C.text }),
        cross(R1, RETRY, { r: 6 }),
        text(R1 + 14, RETRY + 4, "refused", { size: 12, weight: 600, fill: C.red }),
        code(RIGHT, RETRY + 22, "GhostUniquenessException", { anchor: "end" }),

        code(PAD, 334, "CustomerEmailIndex.Is(\"hello@example.com\"): Bob", { fill: C.text }),
    ]);
}

// Lesson 10, CombineATextSearch: the sets of three searches, and a combination that combines again.
{
    const WIDTH = 244, HIGH = 78, COL = [32, 358, 684], ROWS = [106, 206, 306], SLOT = 36, STEP = 44;
    const set = (x, y, title, numbers, state = "alive") => [
        rect(x, y, WIDTH, HIGH, state === "held" ? { fill: C.blueSoft, stroke: C.blue, sw: 1.5 } : { fill: C.raised, stroke: C.base }),
        code(x + 16, y + 24, title, { fill: C.text }),
        [1, 2, 3, 4, 5].map((n, i) => chip(x + 16 + i * STEP, y + 38, SLOT, 28, n, numbers.includes(n) ? "held" : "left")),
    ];
    const middle = ROWS[0] / 2 + ROWS[1] / 2, last = (middle + ROWS[2]) / 2;
    const fork = COL[0] + WIDTH + 41, join = COL[1] + WIDTH + 41;
    card("five-combine.svg", {
        h: 424,
        title: "A text search and a value index combine, and the result combines again",
        sub: "The five orders of the lesson: the paid ones of a note that says door, but not those to call first",
        alt: "Three searches over the orders 1 to 5. OrderStatusDayIndex.Is(Paid) gives 1, 2 and 4. OrderTextIndex.Search(\"door\") gives 1, 4 and 5. OrderTextIndex.Search(\"call\") gives 2 and 4. The paid orders .And(door) give 1 and 4, those of both. That result, .AndNot(Search(\"call\")), gives 1: those of the first, not of the second. A combination is a set: its orders come in no particular order.",
    }, [
        set(COL[0], ROWS[0], "OrderStatusDayIndex.Is(Paid)", [1, 2, 4]),
        set(COL[0], ROWS[1], "OrderTextIndex.Search(\"door\")", [1, 4, 5]),
        set(COL[0], ROWS[2], "OrderTextIndex.Search(\"call\")", [2, 4]),
        set(COL[1], middle, ".And(door)", [1, 4]),
        text(COL[1] + 16, middle + HIGH + 18, "Of both", { size: 12 }),
        set(COL[2], last, ".AndNot(Search(\"call\"))", [1], "held"),
        text(COL[2] + 16, last + HIGH + 18, "Of the first, not of the second", { size: 12 }),
        link([[COL[0] + WIDTH, ROWS[0] + HIGH / 2], [fork, ROWS[0] + HIGH / 2], [fork, middle + HIGH / 2 - 8], [COL[1], middle + HIGH / 2 - 8]]),
        link([[COL[0] + WIDTH, ROWS[1] + HIGH / 2], [fork, ROWS[1] + HIGH / 2], [fork, middle + HIGH / 2 + 8], [COL[1], middle + HIGH / 2 + 8]]),
        link([[COL[1] + WIDTH, middle + HIGH / 2], [join, middle + HIGH / 2], [join, last + HIGH / 2 - 8], [COL[2], last + HIGH / 2 - 8]]),
        link([[COL[0] + WIDTH, ROWS[2] + HIGH / 2], [join, ROWS[2] + HIGH / 2], [join, last + HIGH / 2 + 8], [COL[2], last + HIGH / 2 + 8]]),
        text(PAD, 406, "A combination is a set: its orders come in no particular order", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 11, ReadTheOrdersOfACustomer: a new order of Ada is in ada.Orders, without any Add.
{
    const COL = 432, GAP = 32, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, BOX = 92, TALL = 30, PITCH = 38, TOP = 136, RIGHTS = 176, READ = 284;
    const y = k => TOP + k * PITCH, cy = k => y(k) + TALL / 2;
    const stage = (i, added, orders, last) => {
        const x = x0(i), target = x + RIGHTS, order = (k, n, to) => [
            node(x, y(k), BOX, TALL, `Order ${n}`, null, { size: 12, state: n === 5 ? "held" : "alive" }),
            link([[x + BOX, cy(k)], [target, cy(k)]], { weak: true, stroke: n === 5 ? C.blue : C.text2 }),
        ];
        return [
            node(target, y(0), BOX, 3 * PITCH + TALL, "Ada", null, { state: "alive" }),
            node(target, y(4), BOX, TALL, "Bob", null, { size: 12 }),
            node(target, y(5), BOX, TALL, "Cid", null, { size: 12 }),
            order(0, 1), order(1, 3), order(2, 4), added && order(3, 5), order(4, 2),
            code(x + (BOX + RIGHTS) / 2, cy(0) - 7, "Customer", { anchor: "middle" }),
            code(x + READ, cy(0) + 4, "ada.Orders.Count()"), count(x + COL - 8, cy(0) + 6, orders, { fill: added ? C.blue : C.text }),
            code(x + READ, cy(2) + 4, "ada.LastOrder"), count(x + COL - 8, cy(2) + 6, last, { fill: added ? C.blue : C.text }),
            text(x + READ, cy(5) + 4, "no order", { size: 12 }),
        ];
    };
    card("five-own-collection.svg", {
        h: 408,
        title: "The values fill the collection: no Add, nothing stored",
        sub: "customer.Orders is computed by an index at each read, from the Customer of each order",
        alt: "Two stages of the shop of the lesson. Orders 1, 3 and 4 have Ada for Customer, order 2 has Bob, and no order has Cid: ada.Orders.Count() is 3, and ada.LastOrder is order 4. After new Order { Number = 5, Customer = ada }, with no Add, ada.Orders.Count() is 4 and ada.LastOrder is order 5. The Customer of each order is a weak reference from the order to the customer, and the collection, a member over OrderCustomerIndex, is computed at each read.",
    }, [
        steps(94, [mid(0), mid(1)]),
        code(mid(0), 118, "the shop of the lesson", { anchor: "middle", fill: C.text }),
        code(mid(1), 118, "new Order { Number = 5, Customer = ada }", { anchor: "middle", fill: C.text }),
        stage(0, false, 3, 4),
        stage(1, true, 4, 5),
        legend(390, [{ mark: "weak", name: "weak reference" }]),
        code(RIGHT, 390, "Orders => OrderCustomerIndex.Is(this)", { anchor: "end" }),
    ]);
}

// Lesson 12, SeeTheChangesOfACommitOnly: the count of the paid orders, read around a rollback and around a commit.
{
    const HEIGHT = 136, FROM = 322, AT = [384, 530, 684, 800];
    const lane = (y, name, from, to) => [name && text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }), line(from, y, to, y)];
    const read = (x, y, n, say) => [dot(x, y, 4, C.text2), code(x, y - 11, "Count()", { anchor: "middle", fill: C.text }), count(x, y + 25, n), text(x + 14, y + 24, say, { size: 11, fill: C.muted })];
    const row = (i, name, say, body) => {
        const y0 = 96 + i * HEIGHT, first = y0 + 34, second = y0 + 92;
        return [
            i > 0 && line(PAD, y0 - 6, RIGHT, y0 - 6, { stroke: C.grid }),
            text(PAD, y0 + 34, name, { weight: 600, fill: C.text }),
            say.map((s, k) => text(PAD, y0 + 54 + k * 16, s, { size: 12 })),
            line(AT[2], first + 10, AT[2], second + 30, { stroke: C.grid }),
            lane(first, "writer", FROM, AT[2]),
            dot(AT[0], first, 4, C.text2), code(AT[0], first - 11, "new Order", { anchor: "middle", fill: C.text }),
            body(first, second),
        ];
    };
    card("five-transactions.svg", {
        h: 372,
        title: "An index changes with its transaction",
        sub: "OrderStatusDayIndex.Is(Paid).Count(), read before and after the end of the writer",
        alt: "Two scenarios. A rollback: a transaction creates a paid order, and counts 1; after its rollback, a new transaction counts 0. A commit: a writer creates a paid order and waits; a reader, on its own thread, counts 0 meanwhile, as the order is not committed; the writer commits, and a transaction opened then counts 1.",
    }, [
        row(0, "A rollback", ["Its changes are rolled", "back with the transaction"], (first, second) => [
            read(AT[1], first, 1, "in the transaction"),
            code(AT[2], first - 11, "Rollback()", { anchor: "middle", fill: C.text }), dot(AT[2], first, 4, C.text2),
            lane(second, "reader", AT[3] - 60, AT[3] + 60),
            read(AT[3], second, 0, "a new transaction"),
        ]),
        row(1, "A commit", ["The others see a change", "at its commit only"], (first, second) => [
            code(AT[2], first - 11, "Commit()", { anchor: "middle", fill: C.text }), check(AT[2], first, { r: 6 }),
            lane(second, "reader", AT[1] - 60, AT[1] + 60),
            read(AT[1], second, 0, "not committed yet"),
            lane(second, "", AT[3] - 60, AT[3] + 60),
            read(AT[3], second, 1, "a new transaction"),
        ]),
    ]);
}

// The chapter From the need to the index: the tool first, then what each yes adds to it.
{
    const WIDTH = 192, ANSWER = 212, TALL = 60, PITCH = 72, TOP = 304;
    const FLOW = [PAD, 344, 656], WIDE = 248, HIGH = 56, ROW1 = 116, ROW2 = 200;
    const yes = (s, x, y) => text(x, y, s, { size: 11, anchor: "middle" });
    const rung = (x, i, { q, a }) => {
        const y = TOP + i * PITCH, cy = y + TALL / 2;
        return [
            block(x, y, WIDTH, TALL, q.map(s => ({ s, size: 12, fill: C.text }))),
            link([[x + WIDTH, cy], [x + WIDTH + 28, cy]]),
            text(x + WIDTH + 14, cy - 7, "yes", { size: 11, anchor: "middle" }),
            block(x + WIDTH + 28, y, ANSWER, TALL, a, { state: "held" }),
        ];
    };
    const D = s => M(s, { size: 11 });
    card("five-choose-an-index.svg", {
        h: 536,
        title: "From the need to the index",
        sub: "Ask the questions in their order: the tool first, then what each yes adds to it",
        alt: "Does the question read a few entities of many? No: a query of the Views. Yes: is it about words in a text, or about values? Words: a text index, [TextIndex(n)] and [TextIndexed]. Values: a value index, [ValueIndex] and [ValueIndexComponent]. Then each yes adds its declaration. A long text written in steps: TextIndexing.Commit. Several values, in the order of a question: a composite index, several components. Conditions on any of them: indexes combined, And, Or, AndNot. A precision, a cycle: a rounded value or a part, DatePrecision, Decimals, Step, DatePart. One entity per value: a unique index, Unique = true. A collection of what the values decide: your own collection, a member over an index.",
    }, [
        text(PAD, 104, "Which tool?", { weight: 600, fill: C.text }),
        block(FLOW[0], ROW1, WIDE, HIGH, [{ s: "Does the question read", size: 12, fill: C.text }, { s: "a few entities of many?", size: 12, fill: C.text }]),
        block(FLOW[1], ROW1, WIDE, HIGH, [{ s: "Is it about words in a text,", size: 12, fill: C.text }, { s: "or about values?", size: 12, fill: C.text }]),
        block(FLOW[2], ROW1, 272, HIGH, [B("A value index"), D("[ValueIndex], [ValueIndexComponent]")], { state: "held" }),
        link([[FLOW[0] + WIDE, ROW1 + HIGH / 2], [FLOW[1], ROW1 + HIGH / 2]]),
        yes("yes", FLOW[0] + WIDE + 32, ROW1 + HIGH / 2 - 7),
        link([[FLOW[1] + WIDE, ROW1 + HIGH / 2], [FLOW[2], ROW1 + HIGH / 2]]),
        yes("values", FLOW[1] + WIDE + 32, ROW1 + HIGH / 2 - 7),
        block(FLOW[0], ROW2, WIDE, HIGH, [B("A query of the Views"), S("a report, a sum of every order")], { state: "held" }),
        block(FLOW[1], ROW2, WIDE, HIGH, [B("A text index"), D("[TextIndex(n)], [TextIndexed]")], { state: "held" }),
        link([[FLOW[0] + WIDE / 2, ROW1 + HIGH], [FLOW[0] + WIDE / 2, ROW2]]),
        text(FLOW[0] + WIDE / 2 + 8, ROW1 + HIGH + 18, "no", { size: 11 }),
        link([[FLOW[1] + WIDE / 2, ROW1 + HIGH], [FLOW[1] + WIDE / 2, ROW2]]),
        text(FLOW[1] + WIDE / 2 + 8, ROW1 + HIGH + 18, "words", { size: 11 }),
        text(PAD, TOP - 12, "What does each yes add?", { weight: 600, fill: C.text }),
        [
            { q: ["A long text written", "in steps?"], a: [B("Its words cut at the commit"), D("TextIndexing.Commit")] },
            { q: ["Several values, in the", "order of a question?"], a: [B("A composite index"), S("several components")] },
            { q: ["Conditions on any", "of them?"], a: [B("Indexes combined"), D("And, Or, AndNot")] },
        ].map((r, i) => rung(PAD, i, r)),
        [
            { q: ["A precision, a cycle?"], a: [B("A rounded value, a part"), D("DatePrecision, Decimals,"), D("Step, DatePart")] },
            { q: ["One entity per value?"], a: [B("A unique index"), D("Unique = true")] },
            { q: ["A collection of what", "the values decide?"], a: [B("Your own collection"), S("a member over an index")] },
        ].map((r, i) => rung(496, i, r)),
    ]);
}
