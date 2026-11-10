// The diagrams of Tutorial One. Each one draws the very objects of a test of its lesson: run `node one-the-basics.mjs` after a
// change of the lesson, or of the kit. Their words stay those of the developer: Stores, Repositories, transactions, Bodies, Views.
import { C, PAD, RIGHT, bar, block, card, check, code, count, cross, dot, line, link, node, owner, rect, text } from "./kit.mjs";

const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o });

// A timeline of a transaction: its name, and its line from its opening to its end.
const lane = (y, from, to, name) => [text(from - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }), line(from, y, to, y)];
const at = (x, y, s) => [dot(x, y, 4, C.text2), code(x, y - 10, s, { anchor: "middle", fill: C.text })];
const opens = (x, y) => [dot(x, y, 4, C.text2), text(x, y - 10, "opens", { size: 11, fill: C.muted, anchor: "middle" })];
const committed = (x, y) => [check(x, y, { r: 6 }), code(x, y - 12, "Commit()", { anchor: "middle", fill: C.text })];
const conflict = (x, y) => [cross(x, y, { r: 6 }), code(x, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    text(x + 14, y + 4, "conflict", { size: 12, weight: 600, fill: C.red }), text(x + 14, y + 20, "writes nothing", { size: 11, fill: C.muted })];
const say = (y, name, lines) => [text(PAD, y, name, { weight: 600, fill: C.text }), lines.map((s, k) => text(PAD, y + 20 + k * 16, s, { size: 12 }))];

// Lesson 1, TheGeneratorWritesTheTypesOfTheModel: what you declare, and what the generator writes from it.
{
    const LEFT = 350, TO = 440, PITCH = 25, PADDED = 14, GROUPS = [
        { lines: [M("[ValueObject<GhostString>]", { size: 11 }), M("interface VTitle", { size: 13, weight: 600, fill: C.text })],
            types: [["Title", "what a book reads and writes in place"], ["ReadOnlyTitle", "what a BookView reads"]] },
        { lines: [M("[Entity(1, 1, 10), Root, Removable, Enumerable]", { size: 11 }), M("interface MBook", { size: 13, weight: 600, fill: C.text })],
            types: [["Book", "a class, a Body: the object you change"], ["BookView", "a struct: a read-only view of a book"], ["BookCollection", "enumerates the books"], ["BookViewCollection", "enumerates their Views"]] },
        { lines: [M("[GhostRepository(1),", { size: 11 }), M("IncludeEntityModel(nameof(MBook))]", { size: 11 }), M("interface MLibraryRepository", { size: 13, weight: 600, fill: C.text })],
            types: [["LibraryRepository", "opens the transactions"], ["LibraryRepositoryTransaction", "its transaction"]] },
    ];
    let y = 118;
    const rows = GROUPS.map(g => {
        const tall = g.types.length * PITCH + PADDED, mid = y + tall / 2, top = y;
        y += tall + 14;
        return [
            block(PAD, top, LEFT, tall, g.lines),
            link([[PAD + LEFT, mid], [TO, mid]]),
            rect(TO, top, RIGHT - TO, tall, { fill: C.blueSoft, stroke: C.blue, sw: 1.5 }),
            g.types.map(([type, say], k) => {
                const row = top + PADDED / 2 + PITCH / 2 + k * PITCH + 4;
                return [code(TO + 14, row, type, { fill: C.text }), text(TO + 226, row, say, { size: 12, fill: C.text2 })];
            }),
        ];
    });
    card("one-generated-types.svg", {
        h: y + 26,
        title: "You declare interfaces, the generator writes the types",
        sub: "What you write on the left; what your code uses on the right",
        alt: "Three interfaces and what the generator writes from each. VTitle, a value object, gives Title, that a book reads and writes in place, and ReadOnlyTitle, that a BookView reads. MBook gives the class Book, a Body, the object you change; the struct BookView, a read-only view of a book; BookCollection, which enumerates the books; and BookViewCollection, which enumerates their Views. MLibraryRepository gives LibraryRepository, which opens the transactions, and its transaction, LibraryRepositoryTransaction.",
    }, [
        text(PAD, 104, "You declare", { weight: 600, fill: C.text }),
        text(TO, 104, "The generator writes", { weight: 600, fill: C.text }),
        rows,
        text(PAD, y + 8, "An interface gives a type without its first letter: MBook gives Book, VTitle gives Title", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 2, OpenAStoreInMemoryAndItsRepository and OpenAStoreOnDiskAndItsRepository: one line differs, and what it decides.
{
    const COL = 436, SECOND = RIGHT - COL, mid = [PAD + COL / 2, SECOND + COL / 2], FULL = RIGHT - PAD, STORE = 266, TALL = 96;
    const store = (x, name, call) => block(x, STORE, COL, TALL, [B(name), M("TransactionalMemoryStore.", { size: 11 }), M(call, { fill: C.text }), M("StoreAccessMode.ReadWrite)", { size: 11 })], { state: "held" });
    card("one-store-one-line.svg", {
        h: 428,
        title: "Only the line that creates the Store changes",
        sub: "The Repository binds your model to a Store; every line that reads or writes the books stays the same",
        alt: "Two stacks with the same top. The code that reads and writes the books goes through the same Repository, new LibraryRepository(store). Below it, two Stores: NewInMemoryRepositoryStore, whose data disappears when the process stops, and NewFileRepositoryStore, whose data is there again when the Store opens again, in this process or in another one.",
    }, [
        block(PAD, 100, FULL, 52, [B("Every line that reads or writes the books"), M("library.OpenTransaction(...) · new Book { ... } · BookCollection · transaction.Commit()", { size: 11 })]),
        link([[480, 152], [480, 178]]),
        block(PAD, 178, FULL, 52, [M("using var library = new LibraryRepository(store);", { fill: C.text, weight: 600 }), { s: "The Repository: it knows Book and BookView, and opens the transactions", size: 11 }]),
        mid.map(x => link([[x, 230], [x, STORE]])),
        store(PAD, "A Store in memory", 'NewInMemoryRepositoryStore("lesson-02",'),
        store(SECOND, "A Store on disk", 'NewFileRepositoryStore(directory, "library",'),
        text(mid[0], 386, "The data lasts as long as the process", { weight: 600, fill: C.text, anchor: "middle" }),
        text(mid[1], 386, "The data is there again when the Store opens again", { weight: 600, fill: C.text, anchor: "middle" }),
        text(mid[0], 405, "Nothing remains when the process stops", { size: 12, anchor: "middle" }),
        text(mid[1], 405, "in this process or in another one", { size: 12, anchor: "middle" }),
    ]);
}

// Lesson 3, CreateBooksAndCommitThem, and its note on Entity Framework: the same three moments, and where a query looks.
{
    const BOX = 104, HOLE = 20, x0 = i => 200 + i * 250, store = i => x0(i) + BOX + HOLE, TALL = 88;
    const hold = (x, top, name, state) => [owner(x, top, BOX, TALL, name), node(x + 8, top + 30, BOX - 16, 22, "Dune", null, { size: 12, state }), node(x + 8, top + 56, BOX - 16, 22, "Foundation", null, { size: 12, state })];
    const says = (x, top, lines, o) => lines.map((s, k) => text(x + BOX / 2, top + 58 + (k - (lines.length - 1) / 2) * 16, s, { size: 12, fill: C.muted, anchor: "middle", ...o }));
    const row = (y, { name, say, keep, base, add, query, into, save, closed }) => {
        const top = y + 38, stroke = into ? C.red : C.blue, target = into ? store(1) : x0(1), arrow = (x, s) => link([[x + BOX / 2, y + 14], [x + BOX / 2, top]], { stroke: s });
        const label = (x, s, mono) => mono ? code(x, y + 6, s, { fill: C.text }) : text(x, y + 6, s, { size: 12, fill: C.text });
        const empty = i => [owner(store(i), top, BOX, TALL, base), says(store(i), top, ["no book"])];
        return [
            text(PAD, y + 44, name, { weight: 600, fill: C.text }),
            say.map((s, k) => text(PAD, y + 64 + k * 16, s, { size: 12 })),

            label(x0(0), add, !into), arrow(x0(0), C.text2),
            hold(x0(0), top, keep, "held"), empty(0),

            label(target, query, !into), arrow(target, stroke),
            hold(x0(1), top, keep, "held"),
            into ? [owner(store(1), top, BOX, TALL, base, null, { state: "refused" }), says(store(1), top, ["does not", "find them"], { fill: C.red, weight: 600 })] : empty(1),

            label(x0(2), save, true),
            closed ? [owner(x0(2), top, BOX, TALL, keep, null, { state: "gone" }), says(x0(2), top, [closed])] : hold(x0(2), top, keep, "alive"),
            link([[x0(2) + BOX, top + TALL / 2], [store(2), top + TALL / 2]], { stroke: C.blue, sw: 2 }),
            hold(store(2), top, base, "held"),
        ];
    };
    card("one-visible-at-once.svg", {
        h: 412,
        title: "A query of a transaction sees every change at once",
        sub: "The same three moments: nothing to save first, because the query reads the memory you work in",
        alt: "Two rows of three moments. In Entity Framework, a DbContext collects two new books; a query on a DbSet asks the database, and does not find them; SaveChanges() sends them to the database. In GBO, two new books are in the transaction; BookCollection.Count() reads the transaction, and gives 2; transaction.Commit() writes them in the Store and closes the transaction.",
    }, [
        row(94, { name: "Entity Framework", say: ["A DbContext collects", "your changes"], keep: "DbContext", base: "Database", add: "two new books", query: "a query on a DbSet", into: 1, save: "SaveChanges()" }),
        line(PAD, 236, RIGHT, 236, { stroke: C.grid }),
        row(256, { name: "GBO", say: ["The transaction is the", "memory you work in"], keep: "Transaction", base: "Store", add: "new Book { ... } × 2", query: "BookCollection.Count() == 2", into: 0, save: "transaction.Commit()", closed: "closed" }),
    ]);
}

// Lesson 8, CountAndTestBooks: the capture that GBO013 refuses, and the state that replaces it.
{
    const WIDE = 7.2, HIGH = 28;
    const tokens = (x, cy, list) => {
        const out = [], where = {};
        for (const t of list) {
            if (typeof t === "string") {
                const w = t.length * WIDE;
                out.push(code(x + w / 2, cy + 4, t, { anchor: "middle", fill: C.text }));
                x += w;
            } else {
                const w = t.s.length * WIDE + 16;
                out.push(node(x, cy - HIGH / 2, w, HIGH, t.s, null, { size: 12, mono: true, state: t.state ?? "alive" }));
                where[t.key] = { x, w, mid: x + w / 2 };
                x += w;
            }
        }
        return { out, where };
    };
    const refused = tokens(PAD, 170, [{ s: "BookCollection", key: "items" }, ".Count(book => book.Year <", { s: "before", key: "before", state: "refused" }, ");"]);
    const passed = tokens(PAD, 298, [{ s: "BookCollection", key: "items" }, ".Count(", { s: "before", key: "before", state: "held" }, ", static (", { s: "book", key: "book" }, ", ", { s: "year", key: "year", state: "held" }, ") => book.Year < year);"]);
    const declared = PAD + "Year before = 1950;".length * WIDE;
    const { before: b1 } = refused.where, { before: b2, year, book, items } = passed.where;
    card("one-lambda-state.svg", {
        h: 392,
        title: "A lambda captures nothing: a value from outside is its state",
        sub: "The state is passed before the lambda, and reaches it as a parameter, after the book",
        alt: "Two versions of one count of the books before 1950. The first reads the local value before in the lambda: it captures it, makes .NET allocate an object at each call, and GBO refuses it at compile time with the error GBO013. The second passes before to Count, ahead of a static lambda: the lambda receives each book, then the state as its parameter year. It compiles, and counts 2 books. Up to 4 values of state can be passed, in their order.",
    }, [
        text(PAD, 106, "A capture", { weight: 600, fill: C.red }),
        code(PAD, 132, "Year before = 1950;"),
        link([[declared + 8, 128], [b1.mid, 128], [b1.mid, 170 - HIGH / 2]], { stroke: C.red }),
        text((declared + 8 + b1.mid) / 2, 120, "captured", { size: 12, anchor: "middle" }),
        refused.out,
        text(640, 164, "Error GBO013", { weight: 600, fill: C.red }),
        text(640, 182, "An object allocated at each call", { size: 12 }),

        line(PAD, 206, RIGHT, 206, { stroke: C.grid }),

        text(PAD, 230, "The state", { weight: 600, fill: C.blue }),
        code(PAD, 256, "Year before = 1950;"),
        link([[declared + 8, 252], [b2.mid - 10, 252], [b2.mid - 10, 298 - HIGH / 2]], { stroke: C.blue, sw: 2 }),
        link([[b2.mid + 10, 298 - HIGH / 2], [b2.mid + 10, 270], [year.mid, 270], [year.mid, 298 - HIGH / 2]], { stroke: C.blue, sw: 2 }),
        text(b2.mid + 18, 262, "reaches it after the book", { size: 12 }),
        passed.out,
        link([[items.mid, 298 + HIGH / 2], [items.mid, 332], [book.mid, 332], [book.mid, 298 + HIGH / 2]]),
        text((items.mid + book.mid) / 2, 348, "each book", { size: 12, anchor: "middle" }),
        count(646, 304, "2"),
        text(662, 302, "books before 1950", { size: 12 }),
        text(PAD, 374, "Up to 4 values of state, in their order: BookCollection.Count(1950, 1970, static (book, from, to) => ...)", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 9, FindABookOrNone and ThrowWhenAValueIsDueAndMissing, and Lesson 6 for First: each operator, for what it finds.
{
    const CELL = 200, HIGH = 32, COLS = [300, 514, 728], TOP = 150, PITCH = 44;
    const HEAD = [["no book", 'Title == "Hamlet"'], ["one book", 'Title == "Dune"'], ["two books", 'Author == "Isaac Asimov"']];
    const ROWS = [
        ["First", "Book", "throws", "the book", "the first"],
        ["FirstOrNone", "Option<Book>", "none", "the book", "the first"],
        ["Single", "Book", "throws", "the book", "throws"],
        ["SingleOrNone", "Option<Book>", "none", "the book", "throws"],
    ];
    const STATE = { throws: "refused", none: "alive" };
    const y = i => TOP + i * PITCH + (i > 1 ? 14 : 0);
    card("one-option-operators.svg", {
        h: 382,
        title: "An OrNone operator gives none for an absence, never for more than one",
        sub: "What each operator gives, for the books that meet its condition",
        alt: "A grid of four operators against no book, one book and two books. First gives a Book: it throws for no book, gives the book for one, the first one for two. FirstOrNone gives an Option<Book>: none for no book, the book for one, the first one for two. Single gives a Book: it throws for no book and for two, and gives the book for one. SingleOrNone gives an Option<Book>: none for no book, the book for one, and it throws for two: more than one is never none.",
    }, [
        HEAD.map(([name, say], k) => [text(COLS[k] + CELL / 2, 112, name, { weight: 600, fill: C.text, anchor: "middle" }), code(COLS[k] + CELL / 2, 130, say, { size: 11, fill: C.muted, anchor: "middle" })]),
        ROWS.map(([op, gives, ...cells], i) => [
            code(PAD, y(i) + 20, op, { fill: C.text, size: 13, weight: 600 }),
            code(PAD + 136, y(i) + 20, gives, { fill: C.muted }),
            cells.map((c, k) => node(COLS[k], y(i), CELL, HIGH, c, null, { size: 12, state: STATE[c] ?? "held" })),
        ]),
        line(PAD, y(2) - 13, RIGHT, y(2) - 13, { stroke: C.grid }),
        text(PAD, 364, "An Option: zero or one item, which you cannot read without handling its absence. So with Max: over no book it throws, MaxOrNone gives none", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 10, ChangeTheBooksFoundWithTheirBodies: three Views, two Bodies, one commit.
{
    const VIEW = 190, BODY = 214, STORE = 190, XV = PAD, XB = 372, XS = RIGHT - STORE, TALL = 46, TOP = 188, PITCH = 64, y = i => TOP + i * PITCH, mid = i => y(i) + TALL / 2, HOLD = 12;
    const ROWS = [["Dune", "9.90", "10.90"], ["Foundation", "8.50", "9.50"], ["Neuromancer", "10.90", "10.90"]];
    card("one-views-bodies.svg", {
        h: 408,
        title: "Search with Views, change with Bodies",
        sub: "Three books read, two kept: only those found become objects of the transaction",
        alt: "Three books, Dune at 9.90, Foundation at 8.50 and Neuromancer at 10.90. BookViewCollection.Where finds, with one View of each book, those below 10. ToBody() turns the two found into Bodies, which the transaction keeps; Price += 1m brings them to 10.90 and 9.50. Neuromancer gets no Body. transaction.Commit() writes the two changes: the Store holds 10.90, 9.50 and 10.90.",
    }, [
        text(XV, 108, "The Views find those below 10", { weight: 600, fill: C.text }),
        code(XV, 127, "BookViewCollection.Where(10m, ...)"),
        text(XB - HOLD, 108, "Only those become Bodies", { weight: 600, fill: C.text }),
        code(XB - HOLD, 127, "cheap.ToBody().Price += 1m"),
        text(XS, 108, "The Store, after the commit", { weight: 600, fill: C.text }),
        code(XS, 127, "transaction.Commit()"),
        owner(XB - HOLD, 150, BODY + 2 * HOLD, y(1) + TALL + HOLD - 150, "Kept by the transaction", null, { state: "held" }),
        ROWS.map(([name, was, now], i) => [
            node(XV, y(i), VIEW, TALL, name, was),
            i < 2 ? [
                node(XB, y(i), BODY, TALL, name, `Price: ${now}`, { state: "held" }),
                link([[XV + VIEW, mid(i)], [XB, mid(i)]], { stroke: C.blue, sw: 2 }),
                link([[XB + BODY, mid(i)], [XS, mid(i)]]),
                node(XS, y(i), STORE, TALL, name, now, { state: "held" }),
            ] : [
                text(XB + BODY / 2, mid(i) + 4, "Not found: no Body", { size: 12, fill: C.muted, anchor: "middle" }),
                node(XS, y(i), STORE, TALL, name, now),
            ],
        ]),
        text(PAD, 390, "A View is a read-only struct: the transaction keeps nothing of it. A Body is an object: the transaction keeps each one it gives", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 12, CallAMethodInAQuery, and its note on Entity Framework: the same method in a query, and where it runs.
{
    const COL = 434, GAP = 28, x0 = i => PAD + i * (COL + GAP), TOP = 156, TALL = 124, Q = 164, TO = 274, WIDE = 160, cy = TOP + 80;
    const heading = (i, name, decl) => [text(x0(i), 108, name, { weight: 600, fill: C.text }), code(x0(i), 127, decl), line(x0(i), 140, x0(i) + COL, 140, { stroke: C.grid })];
    const inner = (x, lines, state) => block(x, TOP + 34, WIDE - 26, 76, lines, { state });
    card("one-query-in-process.svg", {
        h: 366,
        title: "A query runs in your process: its lambda is plain C#",
        sub: "The same method of yours in a query: translated in Entity Framework, called in GBO",
        alt: "Two columns. In Entity Framework, a query is not run in your process: it is translated to SQL and run by the database. A method of your own, IsBargain, has no translation, and the query throws when it runs. In GBO, the query runs in your process, over the objects themselves: the lambda calls IsBargain, which reads book.Price < 8m, and the count gives 3 books. The lambda may call any method, read any computed property, and even run another query.",
    }, [
        heading(0, "Entity Framework", "Where(book => IsBargain(book))"),
        owner(x0(0), TOP, Q, TALL, "Your process"),
        block(x0(0) + 13, TOP + 34, Q - 26, 76, [B("The query"), M("IsBargain(book)", { size: 11 })]),
        owner(x0(0) + TO, TOP, WIDE, TALL, "The database"),
        inner(x0(0) + TO + 13, [{ s: "SQL", size: 13, weight: 600, fill: C.muted }, { s: "run by the database", size: 11, fill: C.muted }]),
        link([[x0(0) + Q - 13, cy], [x0(0) + TO + 13, cy]], { stroke: C.red }),
        dot(x0(0) + (Q + TO) / 2, cy, 9, C.surface),
        cross(x0(0) + (Q + TO) / 2, cy, { r: 4 }),
        text(x0(0) + (Q + TO) / 2, cy - 18, "no translation", { size: 12, anchor: "middle" }),
        text(x0(0) + COL / 2, 310, "The query throws when it runs", { weight: 600, fill: C.red, anchor: "middle" }),
        text(x0(0) + COL / 2, 330, "A method of yours has no translation to SQL", { size: 12, anchor: "middle" }),

        heading(1, "GBO", "BookCollection.Count(static book => IsBargain(book))"),
        owner(x0(1), TOP, COL, TALL, "Your process", null, { state: "held" }),
        block(x0(1) + 13, TOP + 34, Q - 26, 76, [B("The query"), M("IsBargain(book)", { size: 11 })]),
        inner(x0(1) + TO + 13, [M("IsBargain(book)", { fill: C.text, weight: 600 }), M("book.Price < 8m", { size: 11 })], "held"),
        link([[x0(1) + Q - 13, cy], [x0(1) + TO + 13, cy]], { stroke: C.blue, sw: 2 }),
        text(x0(1) + (Q + TO) / 2, cy - 10, "called", { size: 12, anchor: "middle" }),
        text(x0(1) + COL / 2, 310, "3 books, over the objects themselves", { weight: 600, fill: C.text, anchor: "middle" }),
        text(x0(1) + COL / 2, 330, "Any method, any computed property, even another query", { size: 12, anchor: "middle" }),
    ]);
}

// Lesson 13, ChangeTheBooksFromDeepMethods: the calls, none of which receives the transaction.
{
    const BOX = 262, HIGH = 36, TOP = 104, PITCH = 50, y = i => TOP + i * PITCH, cy = i => y(i) + HIGH / 2, X = d => PAD + d * 32, BAND = 640, BW = RIGHT - BAND;
    const CALLS = [
        { depth: 0, name: "library.OpenTransaction(...)", say: "opens it" },
        { depth: 1, name: "CloseTheYear(2026)" },
        { depth: 2, name: "DiscountTheBooksBefore(1960, 10)", say: "queries and changes" },
        { depth: 2, name: "ReprintTheBooksBefore(1950, 2026)", say: "queries" },
        { depth: 3, name: "Reprint(book, 2026)", say: "creates" },
        { depth: 0, name: "transaction.Commit()", say: "commits it" },
    ];
    const BOOKS = [["Dune", "9.90", false], ["Foundation", "8.50 → 7.65", false], ["1984", "6.90 → 6.21", false], ["1984, of 2026", "6.21", true]];
    const branch = (from, to) => link([[X(CALLS[from].depth) + 16, y(from) + HIGH], [X(CALLS[from].depth) + 16, cy(to)], [X(CALLS[to].depth), cy(to)]], { head: false, stroke: C.base, sw: 1.2 });
    card("one-deep-methods.svg", {
        h: 436,
        title: "Three levels down, a method finds the transaction by itself",
        sub: "No method receives it; the code that opened it keeps the decision to commit",
        alt: "A tree of calls next to the transaction of the context. The test opens the transaction and calls CloseTheYear(2026), which calls DiscountTheBooksBefore(1960, 10), which queries and changes, and ReprintTheBooksBefore(1950, 2026), which queries and calls Reprint(book, 2026), which creates. Each of them reaches the transaction by itself. Dune stays at 9.90; Foundation goes from 8.50 to 7.65; 1984 from 6.90 to 6.21; a reprint of 1984, of 2026, is created at 6.21. The test commits.",
    }, [
        line(X(0) + 16, y(0) + HIGH, X(0) + 16, y(5), { stroke: C.base, sw: 1.2 }),
        branch(0, 1), branch(1, 2), branch(1, 3), branch(3, 4),
        CALLS.map((c, i) => node(X(c.depth), y(i), BOX, HIGH, c.name, null, { size: 12, mono: true })),
        CALLS.map((c, i) => c.say && [
            link([[X(c.depth) + BOX, cy(i)], [BAND, cy(i)]], { stroke: C.blue, sw: i === 0 || i === 5 ? 2 : 1.6 }),
            text(BAND - 12, cy(i) - 8, c.say, { size: 12, anchor: "end" }),
        ]),
        owner(BAND, TOP - 6, BW, y(5) + HIGH + 6 - TOP + 6, "The transaction of the context", null, { state: "held", tint: true }),
        BOOKS.map(([name, price, born], i) => [
            rect(BAND + 12, y(i + 1) - 2, BW - 24, 40, { fill: C.surface, stroke: born ? C.blue : C.base, sw: born ? 1.5 : 1 }),
            text(BAND + 24, y(i + 1) + 22, name, { fill: C.text, weight: 600 }),
            code(BAND + BW - 24, y(i + 1) + 22, price, { anchor: "end", fill: C.text }),
        ]),
        text(PAD, 418, "Their changes are visible at once: two books discounted, one reprinted, four books after the commit", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 13, ChangeAndCreateBooksInsideAQuery: the book that the lambda creates, met later by the same query.
{
    const COL = 200, GAP = 32, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, BOOK = 66, TALL = 50, Y = 168;
    const MET = [
        { name: "Dune", year: "1965", test: "1965 < 1950", say: "false" },
        { name: "Foundation", year: "1951", test: "1951 < 1950", say: "false" },
        { name: "1984", year: "1949", test: "1949 < 1950", say: "true: reprinted", hit: true },
        { name: "1984", year: "2026", test: "2026 < 1950", say: "false", born: true },
    ];
    card("one-enumerated-too.svg", {
        h: 312,
        title: "A book created during a query is enumerated too",
        sub: "The lambda reprints each book older than 1950, in 2026: the same query meets the reprint, later",
        alt: "The query meets four books in their order. Dune, of 1965, and Foundation, of 1951, are not older than 1950. 1984, of 1949, is: the lambda reprints it, and creates a book 1984 of 2026. The same query meets that reprint later: 2026 is not below 1950, and it is not reprinted again. Count gives 1, and the library holds 4 books.",
    }, [
        code(PAD, 104, "BookCollection.Count(2026, static (book, year) => book.Year < 1950 && Reprinted(book, year))", { fill: C.text }),
        line(PAD, 126, RIGHT, 126),
        MET.map((m, i) => dot(mid(i), 126, 3.5, C.text2)),
        MET.map((m, i) => [
            code(mid(i), 151, m.test, { anchor: "middle", fill: C.text }),
            node(mid(i) - BOOK, Y, BOOK * 2, TALL, m.name, m.year, { state: m.born ? "held" : "alive" }),
            text(mid(i), Y + TALL + 22, m.say, { size: 12, weight: m.hit ? 600 : 400, fill: m.hit ? C.blue : C.muted, anchor: "middle" }),
        ]),
        link([[mid(2) + BOOK, Y + TALL / 2], [mid(3) - BOOK, Y + TALL / 2]], { stroke: C.blue, sw: 2 }),
        text((mid(2) + mid(3)) / 2, Y + TALL / 2 - 10, "creates", { size: 12, anchor: "middle" }),
        code(PAD, 272, "Count(...) == 1", { fill: C.text }),
        code(PAD + 138, 272, "BookCollection.Count() == 4", { fill: C.text }),
        text(PAD, 294, "The reprint is from 2026: the query meets it, and does not reprint it again", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 15, ReadTheStoreAsItWasWhenTheTransactionOpened: three transactions, and the Store each one reads.
{
    const FROM = 150, R = 118, WR = 174, N = 230, STORE = 276, HIGH = 30, A = { open: 170, wOpen: 300, book: 430, commit: 580, count: 690, nOpen: 780, nCount: 860 };
    card("one-snapshot.svg", {
        h: 338,
        title: "A transaction reads the Store as it was when it opened",
        sub: "Three transactions in time: a commit reaches only the transactions that open after it",
        alt: "A timeline. A reader opens while the Store holds no book. A writer then opens, creates the book Dune and commits: the Store holds Dune. The reader, still open, counts 0: the book was committed after it opened. A new transaction opens after the commit, and counts 1.",
    }, [
        lane(R, A.open, A.count, "reader"),
        opens(A.open, R),
        at(A.count, R, "Count()"),
        count(A.count + 40, R + 6, "0"),
        text(A.count + 56, R + 4, "It does not see the book", { size: 12 }),

        lane(WR, A.wOpen, A.commit, "writer"),
        opens(A.wOpen, WR),
        at(A.book, WR, 'new Book { Title = "Dune" }'),
        committed(A.commit, WR),

        lane(N, A.nOpen, A.nCount, "a new transaction"),
        opens(A.nOpen, N),
        at(A.nCount, N, "Count()"),
        count(A.nCount + 40, N + 6, "1"),

        text(PAD, STORE + 20, "The Store", { weight: 600, fill: C.text }),
        rect(FROM, STORE, A.commit - FROM - 2, HIGH, { fill: C.raised, stroke: C.base, rx: 6 }),
        text((FROM + A.commit) / 2, STORE + 20, "no book", { size: 12, fill: C.muted, anchor: "middle" }),
        rect(A.commit + 2, STORE, RIGHT - A.commit - 2, HIGH, { fill: C.blueSoft, stroke: C.blue, sw: 1.5, rx: 6 }),
        text((A.commit + RIGHT) / 2, STORE + 20, "Dune", { size: 12, weight: 600, fill: C.text, anchor: "middle" }),

        link([[A.open, R + 8], [A.open, STORE]], { stroke: C.blue, sw: 2 }),
        text(A.open + 10, STORE - 12, "It reads this Store until it closes", { size: 12 }),
        link([[A.commit, WR + 12], [A.commit, STORE]]),
        text(A.commit + 10, STORE - 12, "The commit publishes the book", { size: 12 }),
        link([[A.nOpen, N + 8], [A.nOpen, STORE]], { stroke: C.blue, sw: 2 }),
    ]);
}

// Lesson 16, RefuseTheSecondChangeOfTheSameBook and ChangeTwoBooksAtTheSameTime: the same book, then two books.
{
    const F = { open: 262, write: 360 }, S = { open: 404, write: 478 }, C1 = 572, C2 = 700, NEW = 728, rowOne = 136, rowTwo = 290;
    card("one-first-commit-wins.svg", {
        h: 362,
        title: "The first commit wins, the other one writes nothing",
        sub: "Two concurrent transactions: the later commit conflicts when both changed the same book",
        alt: "Two scenarios. Both transactions change Dune, at 9.90: the first sets 12.50, the second sets 11.00 and commits first; the commit of the first throws a TransactionConflictException and writes nothing. The application decides: a new transaction reads 11.00, sets 12.50 and commits. In the second scenario, the transactions change Dune and Foundation: both commit.",
    }, [
        say(rowOne - 2, "The same book", ["Each changes its own version of", "Dune, at 9.90. GBO never retries:", "the application decides"]),
        lane(rowOne, F.open, C2, "first"),
        opens(F.open, rowOne),
        at(F.write, rowOne, "Price = 12.50"),
        conflict(C2, rowOne),
        lane(rowOne + 44, S.open, C1, "second"),
        opens(S.open, rowOne + 44),
        at(S.write, rowOne + 44, "Price = 11.00"),
        committed(C1, rowOne + 44),
        link([[C1 + 14, rowOne + 43], [C2 - 10, rowOne + 6]], { weak: true, stroke: C.red }),
        lane(rowOne + 88, NEW, 894, "a new transaction reads 11.00"),
        opens(NEW, rowOne + 88),
        at(806, rowOne + 88, "Price = 12.50"),
        committed(894, rowOne + 88),

        line(PAD, rowOne + 112, RIGHT, rowOne + 112, { stroke: C.grid }),

        say(rowTwo - 2, "Two books", ["Changes of different books", "never conflict"]),
        lane(rowTwo, F.open, C1, "first"),
        opens(F.open, rowTwo),
        at(F.write, rowTwo, "dune.Price = 12.50"),
        committed(C1, rowTwo),
        lane(rowTwo + 44, S.open - 92, C2, "second"),
        opens(S.open - 92, rowTwo + 44),
        at(S.write, rowTwo + 44, "foundation.Price = 10.00"),
        committed(C2, rowTwo + 44),
    ]);
}

// Lesson 17, EmptyTheShelvesAtTheOptimisticLevel and KeepABookOnTheShelvesAtThePessimisticLevel: two decisions, two books, one rule.
{
    const FROM = 280, END = 590, SHELF = 722, ONE = 134, TWO = 288;
    const shelves = (y, dune, foundation, word, state) => [
        owner(SHELF, y - 22, RIGHT - SHELF, 96, "The shelves", word, { state }),
        node(SHELF + 12, y + 16, 84, 44, "Dune", dune[0], { size: 12, state: dune[1] }),
        node(SHELF + 108, y + 16, 84, 44, "Foundation", foundation[0], { size: 12, state: foundation[1] }),
    ];
    const row = (y, name, lines, done, result) => [
        say(y - 4, name, lines),
        lane(y, FROM, END, "first"),
        opens(FROM, y),
        at(392, y, 'Borrow("Dune")'),
        done(END, y),
        lane(y + 52, 296, 510, "second"),
        opens(296, y + 52),
        at(402, y + 52, 'Borrow("Foundation")'),
        committed(510, y + 52),
        result,
    ];
    card("one-pessimistic.svg", {
        h: 408,
        title: "Each decision is right on what it read, and the rule breaks",
        sub: "The library keeps one book on its shelves. Each transaction reads two, and writes its own book",
        alt: "Two transactions read Dune and Foundation available, and each one borrows its own book. At the Optimistic level both commit, because they wrote two different books, and the shelves are empty. At the Pessimistic level the later commit conflicts, of the kind Collection, because the books it read changed: Dune stays on the shelves, and its retry refuses to borrow it.",
    }, [
        row(ONE, "Optimistic", ["Reads are not checked:", "two books, no conflict"], committed,
            shelves(ONE, ["borrowed", "refused"], ["borrowed", "refused"], "empty", "refused")),
        line(PAD, ONE + 100, RIGHT, ONE + 100, { stroke: C.grid }),
        row(TWO, "Pessimistic", ["The books it read changed:", "the later commit conflicts"], (x, y) => conflict(x, y),
            shelves(TWO, ["available", "held"], ["borrowed", "alive"], "one book", "held")),
        text(PAD, 392, "Run again, the first transaction reads one book available, and refuses.", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 18, CommitAnExclusiveTransactionFirst and BoundTheWaitOfAnExclusiveTransaction: what waits for the right to write.
{
    const FROM = 262, THICK = 12;
    const right = (x0, x1, y) => [bar(x0, x1, y - THICK / 2, THICK, C.blue), text(x0, y + 24, "holds the right to write", { size: 12, fill: C.text2 })];
    const wait = (x0, x1, y, s) => [bar(x0, x1, y - THICK / 2, THICK, C.amber), text((x0 + x1) / 2, y + 24, s, { size: 12, fill: C.amber, anchor: "middle" })];
    const one = 138, two = 294, CALL = 610, CLOSE = 754, HOLDS = 880, OTHER = 434, SECOND = 354, READER = 566;
    card("one-exclusive.svg", {
        h: 442,
        title: "An exclusive transaction holds the right to write from its opening",
        sub: "The other commits and the other exclusive transactions wait; the transactions that read do not",
        alt: "Two scenarios. An exclusive transaction opens, takes the right to write, and sets the price of Dune to 12.50. Another transaction sets it to 11.00: its commit waits for the exclusive one to close, then finds the book changed since it opened, and conflicts. In the second scenario, an exclusive transaction holds the right to write. A second exclusive transaction waits for it when it opens, 100 milliseconds at most by a CancellationToken, and throws an OperationCanceledException. A transaction that reads then opens at once, and counts 0.",
    }, [
        say(one - 2, "A commit waits", ["It waits for the exclusive one to", "close, then finds the book", "changed since it opened"]),
        lane(one, FROM, CLOSE, "exclusive"),
        right(FROM, CLOSE, one),
        opens(FROM, one),
        at(372, one, "Price = 12.50"),
        check(CLOSE + 10, one, { r: 6 }),
        code(CLOSE, one - 12, "Commit()", { anchor: "middle", fill: C.text }),

        lane(one + 66, OTHER, CALL, "other"),
        opens(OTHER, one + 66),
        at(520, one + 66, "Price = 11.00"),
        at(CALL, one + 66, "Commit()"),
        wait(CALL, CLOSE, one + 66, "waits"),
        cross(CLOSE + 20, one + 66, { r: 6 }),
        text(CLOSE + 34, one + 70, "conflict", { size: 12, weight: 600, fill: C.red }),
        text(CLOSE + 34, one + 86, "writes nothing", { size: 11, fill: C.muted }),

        line(PAD, one + 110, RIGHT, one + 110, { stroke: C.grid }),

        say(two - 2, "An opening waits", ["A CancellationToken bounds the", "wait of an exclusive transaction", "for the right to write"]),
        lane(two, FROM, HOLDS, "first exclusive"),
        right(FROM, HOLDS, two),
        opens(FROM, two),
        text(HOLDS, two - 10, "closes", { size: 11, fill: C.muted, anchor: "middle" }),

        text(SECOND - 12, two + 66, "second exclusive", { size: 12, fill: C.muted, anchor: "end" }),
        wait(SECOND, 500, two + 62, "100 milliseconds at most"),
        opens(SECOND, two + 62),
        cross(516, two + 62, { r: 6 }),
        code(532, two + 66, "OperationCanceledException", { fill: C.red }),

        lane(two + 118, READER, 648, "reader"),
        opens(READER, two + 118),
        at(648, two + 118, "Count() == 0"),
        text(704, two + 122, "A transaction that reads never waits", { size: 12 }),
    ]);
}

// Lesson 19, StoreAndQueryTheValueObjectsOfTheBooks: what passes the rule of a value, and what never exists.
{
    const GATE = 470, BOOK = 640, TOP = 150, PITCH = 30, FROM = 400, DOWN = 288, DOWNP = 28;
    const PASS = [['Isbn.TryRead("978-0-441-17271-9", out var dune)', "Isbn = dune"], ["Money.From(20m)", "Deposit"], ["Percent.From(10)", "MemberDiscount"], ['"SF-12"', "Shelf"]];
    const STOP = [['Isbn.TryRead("not an isbn", out _)', "false"], ["Money.TryFrom(-1m, out _)", "false"], ["Percent.From(150)", "ArgumentException"], ["Money price = -0.01m", "ArgumentException"], ['new ShelfMark("12")', "ArgumentException"]];
    const y = i => TOP + i * PITCH, down = i => DOWN + i * DOWNP, END = down(STOP.length - 1) + 12;
    const stamp = (cy, mark) => [dot(GATE, cy, 10, C.surface), mark];
    card("one-value-rule.svg", {
        h: END + 22,
        title: "A value that breaks its rule never reaches a book",
        sub: "The rule is checked once, where the value is made: any code that receives it knows that it is valid",
        alt: "A line between the code that makes values and a book. Four values pass the rule and reach the book Dune: an Isbn read from 978-0-441-17271-9, Money.From(20m), Percent.From(10) and the shelf mark SF-12. Five do not exist: Isbn.TryRead of a text that is no ISBN and Money.TryFrom(-1m) say false; Percent.From(150), the conversion of -0.01 to a Money and new ShelfMark(\"12\") throw an ArgumentException.",
    }, [
        text(PAD, 112, "Where a value is made", { weight: 600, fill: C.text }),
        text(GATE, 112, "Its rule", { weight: 600, fill: C.text, anchor: "middle" }),
        line(GATE, 122, GATE, END, { stroke: C.text2, sw: 2 }),
        owner(BOOK, 104, RIGHT - BOOK, y(PASS.length - 1) + 16 - 104, "The book Dune", null, { state: "held", tint: true }),
        PASS.map(([made, field], i) => [
            code(PAD, y(i) + 4, made, { fill: C.text }),
            link([[FROM, y(i)], [BOOK, y(i)]], { stroke: C.blue, sw: 2 }),
            stamp(y(i), check(GATE, y(i), { r: 6 })),
            code(BOOK + 14, y(i) + 4, field, { fill: C.text }),
        ]),
        STOP.map(([made, answer], i) => [
            code(PAD, down(i) + 4, made),
            link([[FROM, down(i)], [GATE - 12, down(i)]], { stroke: C.red, head: false }),
            stamp(down(i), cross(GATE, down(i), { r: 6 })),
            code(GATE + 20, down(i) + 4, answer, { fill: C.red }),
        ]),
        text(BOOK, down(1) + 8, "A value that breaks its rule does not exist:", { size: 12 }),
        text(BOOK, down(1) + 26, "TryFrom says false, From and a conversion throw", { size: 12 }),
    ]);
}
