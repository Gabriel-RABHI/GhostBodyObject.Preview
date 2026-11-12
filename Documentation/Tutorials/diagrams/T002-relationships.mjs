// The diagrams of Tutorial T002. Each one draws the very objects of a test of its lesson: run `node T002-relationships.mjs` after
// a change of the lesson, or of the kit. Their words stay those of the developer: links, Owners, parts, strong references.
import { C, PAD, RIGHT, bar, block, card, check, code, count, cross, dot, legend, line, link, node, owner, rect, ring, steps, text } from "./kit.mjs";

const STRONG = { mark: "strong", name: "strong reference" }, WEAK = { mark: "weak", name: "weak reference" }, PART = { mark: "part", name: "a part, in its Owner" };
const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o });

// Lessons 2, 3 and 5 side by side: one removal, three answers.
{
    const COL = 278, GAP = 31, col = i => PAD + i * (COL + GAP), mid = i => col(i) + COL / 2, NODE = 92, TALL = 40;
    const heading = (i, name, decl) => [text(col(i), 108, name, { weight: 600, fill: C.text }), code(col(i), 127, decl), line(col(i), 140, col(i) + COL, 140, { stroke: C.grid })];
    const removal = (i, s) => [code(mid(i), 252, s, { anchor: "middle", fill: C.text }), link([[mid(i), 260], [mid(i), 282]], { stroke: C.base, sw: 1.2 })];
    const outcome = (i, s, sql, fill) => [text(mid(i), 392, s, { weight: 600, fill, anchor: "middle" }), code(mid(i), 412, sql, { fill: C.muted, anchor: "middle" })];
    const pair = (i, y, left, right, name, { weak = false, target = "alive", stroke = C.text2, none = false } = {}) => {
        const x = col(i), x2 = x + COL - NODE, cy = y + TALL / 2;
        return [node(x, y, NODE, TALL, left), node(x2, y, NODE, TALL, right, null, { state: target }),
            none ? [link([[x + NODE, cy], [x + NODE + 24, cy]], { weak: true, head: false, stroke: C.muted }), text(x + NODE + 32, cy + 4.5, "none", { fill: C.text })]
                : [link([[x + NODE, cy], [x2, cy]], { weak, stroke }), code(mid(i), cy - 8, name, { anchor: "middle" })]];
    };
    const family = (i, y, state) => {
        const x = col(i);
        return [owner(x, y, COL, 78, "Bowie", null, { state }), node(x + 12, y + 34, 120, 32, "Low", null, { state }), node(x + COL - 132, y + 34, 120, 32, "Heroes", null, { state })];
    };
    card("T002-link-natures.svg", {
        h: 434,
        title: "Three links, three answers when the target goes",
        sub: "One removal, and what the nature of the link makes of it",
        alt: "Three columns. A strong reference from the album Low to its artist Bowie: the removal of Bowie is refused and nothing changes, as ON DELETE RESTRICT. A weak reference from Heroes to its previous album Low: Low is removed and the link becomes none, as ON DELETE SET NULL. A composition, Bowie holding the albums Low and Heroes: the removal of the Owner removes its parts, as ON DELETE CASCADE.",
    }, [
        heading(0, "A strong reference", "[Strong] MArtist Artist"),
        pair(0, 171, "Low", "Bowie", "Artist"),
        removal(0, "bowie.Remove()"),
        pair(0, 309, "Low", "Bowie", "Artist", { target: "refused", stroke: C.red }),
        outcome(0, "Refused: nothing changes", "ON DELETE RESTRICT", C.red),

        heading(1, "A weak reference", "[Weak] Option<MAlbum> Previous"),
        pair(1, 171, "Heroes", "Low", "Previous", { weak: true }),
        removal(1, "low.Remove()"),
        pair(1, 309, "Heroes", "Low", "", { none: true, target: "gone" }),
        outcome(1, "The link becomes none", "ON DELETE SET NULL", C.text),

        heading(2, "A composition", "IEntityCollection<MAlbum> Albums"),
        family(2, 152, "alive"),
        removal(2, "bowie.Remove()"),
        family(2, 290, "gone"),
        outcome(2, "Its parts go with it", "ON DELETE CASCADE", C.text),
    ]);
}

// Lesson 2, RemoveTheArtistOnceNothingDesignatesIt: two albums, three instructions, and the count that decides.
{
    const STEPS = [
        { code: "two albums", eno: false, heroes: true, n: 2, say: "strong references" },
        { code: "low.Artist = eno", eno: true, heroes: true, n: 1 },
        { code: "bowie.Remove()", eno: true, heroes: true, n: 1, refused: true, say: "refused" },
        { code: "heroes.Remove()", eno: true, heroes: false, n: 0 },
        { code: "bowie.Remove()", eno: true, heroes: false, n: "", removed: true, say: "removed" },
    ];
    const COL = 168, GAP = 14, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, ROW1 = 136, ROW2 = 218, BOX = 76, TALL = 34;
    card("T002-strong-count.svg", {
        h: 360,
        title: "An artist can be removed once nothing designates it",
        sub: "The strong references to Bowie, counted at each instruction",
        alt: "Five steps. Two albums designate Bowie: 2 strong references. Low designates Brian Eno instead: 1. bowie.Remove() is refused, and the count stays 1. heroes.Remove() releases the last reference: 0. bowie.Remove() passes, and Bowie is removed.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => {
            const left = x0(i) + 4, right = x0(i) + COL - BOX - 4, cl = left + BOX / 2, cr = right + BOX / 2, numberY = 292;
            return [
                code(mid(i), 118, s.code, { anchor: "middle", fill: C.text }),
                node(left, ROW1, BOX, TALL, "Low"),
                node(right, ROW1, BOX, TALL, "Heroes", null, { state: s.heroes ? "alive" : "gone" }),
                s.eno && node(left, ROW2, BOX, TALL, "Eno"),
                node(right, ROW2, BOX, TALL, "Bowie", null, { state: s.removed ? "gone" : s.refused ? "refused" : "held" }),
                s.eno ? link([[cl, ROW1 + TALL], [cl, ROW2]]) : link([[cl + 14, ROW1 + TALL], [right + 10, ROW2]]),
                s.heroes && link([[cr, ROW1 + TALL], [cr, ROW2]], { stroke: s.refused ? C.red : C.text2 }),
                count(cr, numberY, s.n, { fill: s.refused ? C.red : C.text }),
                s.say && text(cr, s.removed ? numberY - 5 : numberY + 18, s.say, { size: 12, weight: s.refused ? 600 : 400, fill: s.refused ? C.red : C.muted, anchor: "middle" }),
            ];
        }),
        legend(342, [STRONG]),
    ]);
}

// Lesson 4, CreateAnAggregate: an artist, an album, and the three forms of composition.
{
    const ROWS = [
        { y: 166, code: "MRelease Release { get; }", say: "Exactly one part: born with its album" },
        { y: 224, code: "Option<MBooklet> Booklet { get; }", say: "One part, or none: a slot, filled by low.Booklet.New()" },
        { y: 282, code: "IEntityCollection<MTrack> Tracks { get; }", say: "Any number of parts: each born by low.Tracks.New()" },
    ];
    card("T002-aggregate.svg", {
        h: 396,
        title: "An aggregate: an Owner and the parts it holds",
        sub: "Created, changed, removed and committed together",
        alt: "The artist David Bowie, a Root, holds the album Low, a part. The album holds its release, exactly one; its booklet, one or none; and its tracks, any number. Each part reads its Owner by [OwnedFrom].",
    }, [
        owner(PAD, 100, 896, 254, "David Bowie", "Artist, the Root: new Artist { ... }"),
        owner(48, 134, 864, 208, "Low", "Album, a part of the artist: bowie.Albums.New()"),
        node(64, ROWS[0].y, 150, 46, "Release", "RCA, PL 12030"),
        node(64, ROWS[1].y, 150, 46, "Booklet", "12 pages"),
        node(64, ROWS[2].y, 150, 46, "Speed of Life", "166 s"),
        node(222, ROWS[2].y, 150, 46, "Sound and Vision", "183 s"),
        line(214, ROWS[0].y + 23, 384, ROWS[0].y + 23, { stroke: C.grid }),
        line(214, ROWS[1].y + 23, 384, ROWS[1].y + 23, { stroke: C.grid }),
        line(372, ROWS[2].y + 23, 384, ROWS[2].y + 23, { stroke: C.grid }),
        ROWS.map(r => [code(396, r.y + 19, r.code, { fill: C.text }), text(396, r.y + 37, r.say, { size: 12 })]),
        text(PAD, 378, "Each part reads its Owner by [OwnedFrom]: speed.Album, speed.Album.Artist", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 7, MoveAndRemoveASubtree: the same tree, a move refused and a move done.
{
    const ROW = 38, TOP = 130, STEP = 40, BOX = 118, TALL = 30;
    const tree = (x0, rows) => {
        const at = rows.map((r, i) => ({ ...r, x: x0 + r.depth * STEP, y: TOP + i * ROW }));
        return {
            at, draw: [
                at.map(r => {
                    if (r.parent === undefined) return null;
                    const p = at[r.parent], px = p.x + 16, cy = r.y + TALL / 2;
                    return link([[px, p.y + TALL], [px, cy], [r.x, cy]], { head: false, stroke: C.base, sw: 1.2 });
                }),
                at.map(r => node(r.x, r.y, BOX, TALL, r.name, null, { state: r.state ?? "alive" })),
            ],
        };
    };
    const left = tree(PAD, [
        { name: "Ada", depth: 0 },
        { name: "Music", depth: 1, parent: 0, state: "refused" },
        { name: "Jazz", depth: 2, parent: 1 },
        { name: "Bebop", depth: 3, parent: 2 },
        { name: "Playlist", depth: 4, parent: 3 },
        { name: "Archive", depth: 1, parent: 0 },
    ]);
    const RIGHT0 = 500;
    const right = tree(RIGHT0, [
        { name: "Ada", depth: 0 },
        { name: "Archive", depth: 1, parent: 0 },
        { name: "Music", depth: 2, parent: 1, state: "held" },
        { name: "Jazz", depth: 3, parent: 2, state: "held" },
        { name: "Bebop", depth: 4, parent: 3, state: "held" },
        { name: "Playlist", depth: 5, parent: 4, state: "held" },
    ]);
    const music = left.at[1], bebop = left.at[3], gutter = 372;
    card("T002-tree-move.svg", {
        h: 394,
        title: "A folder moves anywhere, except into its own subtree",
        sub: "MoveTo changes the Owner of a whole subtree, or refuses before anything changes",
        alt: "Two trees of the library of Ada. On the left, music.MoveTo(bebop.Folders) is refused: Bebop is a subfolder of Music, and the move would make a cycle. On the right, music.MoveTo(archive.Folders) moves Music, Jazz, Bebop and the playlist under Archive: the Owner of Music, a listener before, is now a folder.",
    }, [
        code(PAD, 108, "music.MoveTo(bebop.Folders)", { fill: C.text }),
        code(RIGHT0, 108, "music.MoveTo(archive.Folders)", { fill: C.text }),
        left.draw,
        right.draw,
        link([[music.x + BOX, music.y + TALL / 2], [gutter, music.y + TALL / 2], [gutter, bebop.y + TALL / 2], [bebop.x + BOX, bebop.y + TALL / 2]], { stroke: C.red }),
        text(gutter + 12, music.y + 58, "Refused: a cycle", { weight: 600, fill: C.red }),
        text(gutter + 12, music.y + 76, "nothing changes", { size: 12, fill: C.muted }),
        code(RIGHT0 + 208, right.at[2].y + 19, "music.Parent: Archive"),
        code(RIGHT0 + 248, right.at[3].y + 19, "music.Listener: none"),
    ]);
}

// Lesson 9, RefuseTheRemovalOfTheChainOfARetainedTrack: one reference, three refusals.
{
    const refuse = (y, s) => [code(878, y, s, { anchor: "end", fill: C.red }), cross(894, y - 4, { r: 4 })];
    card("T002-retention-chain.svg", {
        h: 316,
        title: "A reference to a part retains its whole chain",
        sub: "A part requires its Owner, and its Owner requires its own, up to the Root",
        alt: "An entry of the playlist Favorites designates the track Sound and Vision by a strong reference. The track, its album Low and its artist David Bowie are all retained: bowie.Remove(), bowie.Albums.Remove(low) and low.Tracks.Remove(sound) are each refused. The exception names the chain: Artist 1, Album 1, Track 1.",
    }, [
        owner(PAD, 140, 214, 92, "Favorites", "a playlist"),
        node(48, 174, 182, 46, "Entry", null),
        owner(370, 100, 558, 154, "David Bowie", "", { state: "held" }),
        owner(386, 134, 526, 106, "Low", "", { state: "held" }),
        node(402, 174, 180, 46, "Sound and Vision", null, { state: "held" }),
        link([[230, 197], [402, 197]], { stroke: C.blue, sw: 2 }),
        code(308, 188, "[Strong] Track", { anchor: "middle" }),
        refuse(124, "bowie.Remove()"),
        refuse(158, "bowie.Albums.Remove(low)"),
        refuse(202, "low.Tracks.Remove(sound)"),
        text(PAD, 282, "The exception names the chain, from the removed entity to the retained one:", { size: 12 }),
        code(480, 282, "Artist 1 → Album 1 → Track 1", { fill: C.text }),
    ]);
}

// Lesson 10: the removal of the artist, then the release of the last retention.
{
    const COL = 278, GAP = 31, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, ROOT = 132, OWNER = 200;
    const stage = (i, { artist, low, lodger, tracks, entry }) => {
        const x = x0(i), inner = x + 12, target = inner + 94 + 37;
        return [
            owner(x, ROOT, 200, 44, "Favorites"),
            node(target - 37, ROOT + 8, 74, 28, "Entry", null, { size: 12, state: entry }),
            owner(x, OWNER, COL, 134, "David Bowie", null, { state: artist }),
            owner(inner, OWNER + 32, 176, 90, low === "orphan" ? "Low, an orphan" : "Low", null, { state: low, tint: low === "orphan" }),
            node(inner + 12, OWNER + 64, 74, 46, "Speed", null, { size: 12, state: tracks }),
            node(inner + 94, OWNER + 64, 74, 46, "Sound", null, { size: 12, state: tracks }),
            node(x + 196, OWNER + 32, 70, 90, "Lodger", null, { size: 12, state: lodger }),
            entry !== "gone" && link([[target, ROOT + 36], [target, OWNER + 64]], { stroke: C.blue, sw: 2 }),
        ];
    };
    card("T002-orphan.svg", {
        h: 376,
        title: "An orphan survives its Owner, until its last retention",
        sub: "Low is [Orphanable], and an entry of the playlist retains one of its tracks",
        alt: "Three stages. First, David Bowie holds the albums Low and Lodger, and an entry of the playlist Favorites designates the track Sound of Low. After bowie.Remove(), Bowie and Lodger are removed; Low survives whole, with its two tracks and no artist: an orphan. After favorites.Entries.Remove(entry), the last retention is released: Low and its tracks are removed by this very instruction.",
    }, [
        steps(94, [0, 1, 2].map(mid)),
        code(mid(0), 118, "the catalog", { anchor: "middle", fill: C.text }),
        code(mid(1), 118, "bowie.Remove()", { anchor: "middle", fill: C.text }),
        code(mid(2), 118, "favorites.Entries.Remove(entry)", { anchor: "middle", fill: C.text }),
        stage(0, { artist: "alive", low: "alive", lodger: "alive", tracks: "alive", entry: "alive" }),
        stage(1, { artist: "gone", low: "orphan", lodger: "gone", tracks: "alive", entry: "alive" }),
        stage(2, { artist: "gone", low: "gone", lodger: "gone", tracks: "gone", entry: "gone" }),
        legend(358, [STRONG, PART]),
    ]);
}

// Lesson 11, ShareARecordingBetweenTwoAlbums: born by its first reference, removed with its last.
{
    const STEPS = [
        { code: "new Recording.Init()", low: true, best: true, first: true, second: false, n: 1 },
        { code: "best.Tracks.New(recording)", low: true, best: true, first: true, second: true, n: 2 },
        { code: "bowie.Albums.Remove(low)", low: false, best: true, first: false, second: true, n: 1 },
        { code: "bowie.Albums.Remove(best)", low: false, best: false, first: false, second: false, n: "" },
    ];
    const COL = 206, GAP = 24, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, ALBUM = 96, TOP = 134, REC = 252;
    const album = (x, name, alive, track) => [owner(x, TOP, ALBUM, 74, name, null, { state: alive ? "alive" : "gone" }), track !== undefined && node(x + 12, TOP + 32, ALBUM - 24, 30, "Track", null, { size: 12, state: track ? "alive" : "gone" })];
    card("T002-shared-recording.svg", {
        h: 368,
        title: "A shared object lives as long as its strong references",
        sub: "No Owner, no Remove(): the tracks that designate a recording keep it alive",
        alt: "Four steps. A track of the album Low is born with a new recording: 1 strong reference. A track of the compilation Best of designates the same recording: 2. The album Low is removed with its track: 1, and the recording lives. The compilation is removed: nothing retains the recording, removed by the same instruction.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => {
            const x = x0(i), recX = x + 28, recW = COL - 56, lx = x + ALBUM / 2, bx = x + COL - ALBUM / 2, removed = s.n === "";
            return [
                code(mid(i), 118, s.code, { anchor: "middle", fill: C.text }),
                album(x, "Low", s.low, s.low),
                album(x + COL - ALBUM, "Best of", s.best, i === 0 ? undefined : s.best),
                node(recX, REC, recW, 38, "Recording", null, { state: removed ? "gone" : "held" }),
                s.first && link([[lx, TOP + 62], [lx, REC]]),
                s.second && link([[bx, TOP + 62], [bx, REC]]),
                count(mid(i), 326, s.n),
                i === 0 && text(mid(i), 344, "strong reference", { size: 12, fill: C.muted, anchor: "middle" }),
                removed && text(mid(i), 321, "removed", { size: 12, fill: C.muted, anchor: "middle" }),
            ];
        }),
    ]);
}

// Lesson 12: the same two instructions, in the two orders.
{
    const LEFT = 196, PANEL = 236, GAP = 12, px = i => LEFT + i * (PANEL + GAP), BOX = 72, TALL = 30;
    const scene = (i, y, { title, demo, final, lyrics, n, refused }) => {
        const x = px(i), lx = x + 116, ly = y + 42, cy = ly + TALL / 2;
        return [
            code(x + PANEL / 2, y + 8, title, { anchor: "middle", fill: refused ? C.red : C.text }),
            node(x, y + 20, BOX, TALL, "demo", null, { size: 12 }),
            node(x, y + 64, BOX, TALL, "final", null, { size: 12 }),
            node(lx, ly, BOX, TALL, "Lyrics", null, { size: 12, state: lyrics }),
            demo && link([[x + BOX, y + 35], [x + 94, y + 35], [x + 94, cy - 6], [lx, cy - 6]]),
            final && !refused && link([[x + BOX, y + 79], [x + 94, y + 79], [x + 94, cy + 6], [lx, cy + 6]]),
            refused && [link([[x + BOX, y + 79], [lx, cy + 6]], { stroke: C.red }), dot(x + 94, (y + 79 + cy + 6) / 2, 9, C.surface), cross(x + 94, (y + 79 + cy + 6) / 2, { r: 4 })],
            count(x + 214, cy + 6, n, { fill: lyrics === "gone" ? C.muted : C.text }),
        ];
    };
    const row = (y, name, say, fill) => [text(PAD, y + 40, name, { weight: 600, fill }), text(PAD, y + 60, say[0], { size: 12 }), text(PAD, y + 76, say[1], { size: 12 })];
    card("T002-retention-order.svg", {
        h: 372,
        title: "Take the new retention before releasing the old one",
        sub: "The same two instructions, and the strong references to the lyrics at each one",
        alt: "Two rows. Retain, then release: final.Lyrics.Set(lyrics) brings the count to 2, demo.Lyrics.Clear() brings it back to 1, and the lyrics live, now held by final. Release, then retain: demo.Lyrics.Clear() brings the count to 0 and removes the lyrics; final.Lyrics.Set(lyrics) is then refused, as nothing is left to designate.",
    }, [
        row(100, "Retain, then release", ["The lyrics pass from", "one holder to the other"], C.text),
        scene(0, 100, { title: "the start", demo: true, final: false, lyrics: "held", n: 1 }),
        scene(1, 100, { title: "final.Lyrics.Set(lyrics)", demo: true, final: true, lyrics: "held", n: 2 }),
        scene(2, 100, { title: "demo.Lyrics.Clear()", demo: false, final: true, lyrics: "held", n: 1 }),
        line(PAD, 216, RIGHT, 216, { stroke: C.grid }),
        row(232, "Release, then retain", ["Removed at the release:", "nothing left to designate"], C.red),
        scene(0, 232, { title: "the start", demo: true, final: false, lyrics: "held", n: 1 }),
        scene(1, 232, { title: "demo.Lyrics.Clear()", demo: false, final: false, lyrics: "gone", n: 0 }),
        scene(2, 232, { title: "final.Lyrics.Set(lyrics)", demo: false, final: true, lyrics: "gone", n: 0, refused: true }),
    ]);
}

// Lesson 17: where an exception falls in one operation.
{
    const Y = 136, TALL = 56;
    const throwing = x => link([[x, Y + TALL], [x, Y + TALL + 24]], { stroke: C.red });
    card("T002-refused-condemned.svg", {
        h: 346,
        title: "The first write decides: refused, or condemned",
        sub: "The steps of one operation, and what an exception leaves at each of them",
        alt: "An operation runs from left to right: your check, then the checks of GBO, then the first write, then your On...Changed. An exception before the first write changes nothing: caught, the transaction goes on and commits. An exception after it condemns the transaction: it still reads, refuses its writes and its Commit, and only a rollback is left.",
    }, [
        owner(PAD, 100, 462, 222, "Before the first write"),
        owner(514, 100, 414, 222, "After it", null, { state: "refused" }),
        block(48, Y, 200, TALL, [B("Your check"), M("Album.AddTrack")]),
        block(280, Y, 198, TALL, [B("Every check of GBO"), { s: "a retention, a full slot", size: 11 }]),
        block(530, Y, 170, TALL, [B("The writes of GBO"), { s: "the operation, done", size: 11 }]),
        block(732, Y, 180, TALL, [B("Your On...Changed"), M("OnTitleChanged")]),
        link([[248, Y + 28], [280, Y + 28]]),
        link([[478, Y + 28], [530, Y + 28]], { stroke: C.red, sw: 2 }),
        link([[700, Y + 28], [732, Y + 28]]),
        throwing(148), throwing(379), throwing(822),
        text(148, 240, "Refused", { weight: 600, fill: C.text, anchor: "middle" }),
        text(379, 240, "Refused", { weight: 600, fill: C.text, anchor: "middle" }),
        text(263, 268, "Nothing changed. Caught, the transaction goes on, and commits.", { size: 12, anchor: "middle" }),
        code(263, 306, "transaction.CondemnedBy is null", { anchor: "middle" }),
        text(822, 240, "Condemned", { weight: 600, fill: C.red, anchor: "middle" }),
        text(721, 268, "It still reads, and refuses its writes and its commit.", { size: 12, anchor: "middle" }),
        text(721, 285, "Only a rollback is left.", { size: 12, anchor: "middle" }),
        code(721, 306, "transaction.CondemnedBy is the exception", { anchor: "middle" }),
    ]);
}

// Lesson 18: two transactions, three scenarios, the first one committing last.
{
    const ROWS = [
        { name: "The same album", why: ["Both change Low: a rule of the", "album could not be broken by both"], first: "low.Tracks.New()", second: "low.Tracks.New()", conflict: true },
        { name: "Two albums", why: ["The parts of two albums", "never conflict"], first: "low.Tracks.New()", second: "heroes.Tracks.New()", conflict: false },
        { name: "A target removed", why: ["Both change the count of", "the strong references of the track"], first: "Entries.New(track)", second: "bowie.Remove()", conflict: true },
    ];
    const AT = [370, 548, 690, 830], FROM = 322, HEIGHT = 112;
    const lane = (y, name, write, xWrite, xCommit) => [
        text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }),
        line(FROM, y, xCommit, y),
        dot(xWrite, y, 4, C.text2), code(xWrite, y - 10, write, { anchor: "middle", fill: C.text }),
        code(xCommit, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    ];
    card("T002-conflicts.svg", {
        h: 442,
        title: "An aggregate is a unit of conflict",
        sub: "Two concurrent transactions: the later commit conflicts when both changed the same thing",
        alt: "Three scenarios of two concurrent transactions, the second committing first. Both add a track to the same album: the commit of the first conflicts. They add tracks to two albums: both commit. One adds a reference to a track while the other removes its artist: the commit of the first conflicts, and writes nothing.",
    }, ROWS.map((r, i) => {
        const y0 = 96 + i * HEIGHT, first = y0 + 36, second = y0 + 80;
        return [
            i > 0 && line(PAD, y0 - 2, RIGHT, y0 - 2, { stroke: C.grid }),
            text(PAD, y0 + 34, r.name, { weight: 600, fill: C.text }),
            r.why.map((s, k) => text(PAD, y0 + 54 + k * 16, s, { size: 12 })),
            lane(first, "T1", r.first, AT[0], AT[3]),
            lane(second, "T2", r.second, AT[1], AT[2]),
            check(AT[2], second, { r: 6 }),
            r.conflict ? [link([[AT[2] + 12, second - 4], [AT[3] - 10, first + 6]], { weak: true, stroke: C.red }), cross(AT[3], first, { r: 6 }),
                text(AT[3] + 14, first + 4, "conflict", { size: 12, weight: 600, fill: C.red }), text(AT[3] + 14, first + 20, "writes nothing", { size: 11, fill: C.muted })]
                : [check(AT[3], first, { r: 6 }), text(AT[3] + 14, first + 4, "committed", { size: 12 })],
        ];
    }));
}

// Lesson 19: the identities drawn once, by the command, and carried by the event.
{
    const LETTER = { size: 12, weight: 600, fill: C.blue, mono: true };
    const REFS = [["ArtistRef", "a"], ["AlbumRef", "b"], ["ReleaseRef", "c"], ["TrackRef[]", "d, e"]];
    const graph = (y, name) => {
        const chip = (x, cy, w, type, id) => [rect(x, cy, w, 28, { fill: C.raised, stroke: C.base, rx: 6 }), text(x + 10, cy + 18, type, { size: 12, fill: C.text }), text(x + w - 10, cy + 18, id, { ...LETTER, anchor: "end" })];
        return [owner(692, y, 236, 116, name), chip(704, y + 34, 103, "Artist", "a"), chip(813, y + 34, 103, "Album", "b"), chip(704, y + 72, 84, "Release", "c"), chip(794, y + 72, 58, "Track", "d"), chip(858, y + 72, 58, "Track", "e")];
    };
    card("T002-identities.svg", {
        h: 390,
        title: "A command and its Replay create the same graph",
        sub: "The command draws every identity once; the event carries them; one code applies it",
        alt: "A command draws the identities a, b, c, d and e, and writes them in the event AlbumReleased. The event is applied by one code, Apply, in the transaction of the command and in a Replay transaction. Both create the same graph, with the same identities. In a Replay, ArtistRef.New() throws.",
    }, [
        block(PAD, 130, 158, 64, [B("A command"), { s: "draws every identity", size: 11 }]),
        link([[190, 162], [254, 162]]),
        code(222, 152, "writes", { anchor: "middle" }),
        owner(254, 100, 180, 248, "AlbumReleased"),
        REFS.map(([type, id], k) => [rect(268, 132 + k * 50, 152, 38, { fill: C.raised, stroke: C.base, rx: 6 }), code(280, 155 + k * 50, type, { fill: C.text }), text(408, 155 + k * 50, id, { ...LETTER, anchor: "end" })]),
        link([[434, 162], [546, 162]]),
        link([[434, 286], [546, 286]]),
        text(490, 152, "in the command", { size: 12, anchor: "middle" }),
        text(490, 276, "in a Replay", { size: 12, anchor: "middle" }),
        block(546, 130, 110, 188, [M("Apply(event)", { fill: C.text, weight: 600 }), { s: "the same code", size: 11 }]),
        link([[656, 162], [692, 162]]),
        link([[656, 286], [692, 286]]),
        graph(100, "The Store of the command"),
        graph(228, "The Store of the Replay"),
        text(PAD, 262, "In a Replay, every identity", { size: 12, fill: C.muted }),
        text(PAD, 278, "comes from the event:", { size: 12, fill: C.muted }),
        code(PAD, 296, "ArtistRef.New() throws", { fill: C.text }),
        text(PAD, 372, "Same letters, same identities, in both Stores", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 20: every type of the catalog, and the links between them.
{
    const L = PAD, LW = 170, R = 716, RW = 212;
    card("T002-catalog-model.svg", {
        h: 416,
        title: "The whole catalog: what holds what",
        sub: "Each aggregate is a box; the arrows cross from one to another, or reach what no one owns",
        alt: "The model of Lesson 20. An artist, a Root, holds its orphanable albums, which hold their tracks. A playlist holds its entries. A festival designates artists by strong references; a listener designates albums by weak references; an entry designates a track. An album designates a genre, a Root that is never removed. A track designates a recording, shared, and a recording its lyrics, shared.",
    }, [
        node(L, 106, LW, 44, "Festival", "[Root, Removable]"),
        node(L, 158, LW, 44, "Listener", "[Root, Removable]"),
        owner(L, 214, LW, 92, "Playlist", "[Root, Removable]"),
        node(L + 14, 246, LW - 28, 46, "Entry", "a part"),

        owner(346, 100, 288, 212, "Artist", "[Root, Removable]"),
        owner(362, 134, 256, 164, "Album", "[Orphanable]"),
        node(378, 246, 224, 46, "Track", "a part of its album"),

        node(R, 158, RW, 44, "Genre", "[Root]: never removed"),
        node(R, 246, RW, 46, "Recording", "shared: no Owner, no Remove()"),
        node(R, 332, RW, 46, "Lyrics", "shared"),

        link([[L + LW, 128], [346, 128]]),
        code(258, 120, "Lineup", { anchor: "middle" }),
        link([[L + LW, 180], [362, 180]], { weak: true }),
        code(266, 172, "Favorites", { anchor: "middle" }),
        link([[L + LW - 14, 269], [378, 269]]),
        code(274, 261, "Track", { anchor: "middle" }),
        link([[618, 180], [R, 180]]),
        code(667, 172, "Genre", { anchor: "middle" }),
        link([[602, 269], [R, 269]]),
        code(667, 261, "Recording", { anchor: "middle" }),
        link([[R + RW / 2, 292], [R + RW / 2, 332]]),
        code(R + RW / 2 + 10, 316, "Lyrics"),
        legend(398, [STRONG, WEAK, PART]),
    ]);
}

// Lesson 20, FollowEveryRetentionOfACatalog: each entity, a bar that ends at the instruction that removes it.
{
    const LABEL = 236, START = 244, MOMENT = { 1: 400, 2: 544, 3: 694, 4: 832 }, ROW = 22, TOP = 158, RUN = 8, BAR = 12;
    const GROUPS = [
        [["David Bowie", 2, { refused: true }], ["The album Lodger", 2], ["Boys Keep Swinging, its recording", 2]],
        [["The entry of the playlist", 3], ["The album Low", 3, { orphan: true }], ["Sound and Vision, its recording", 3], ["Speed of Life, its recording", 3]],
        [["Brian Eno", 4], ["The album Another Green World", 4], ["Its track, its recording", 4], ["The lyrics", 4]],
        [["The genre Rock"], ["The playlist Favorites"], ["The listener Ada"], ["The festival Montreux"]],
    ];
    const CENSUS = [[288, "(2, 3, 4, 4, 1)"], [400, "(2, 3, 4, 4, 1)"], [544, "(1, 2, 3, 3, 1)"], [694, "(1, 1, 1, 1, 1)"], [832, "(0, 0, 0, 0, 0)"]];
    const bottom = TOP + GROUPS.flat().length * ROW + (GROUPS.length - 1) * RUN;
    let y = TOP;
    const rows = GROUPS.map(group => {
        const drawn = group.map(([name, dies, o = {}]) => {
            const bar0 = y + (ROW - BAR) / 2, cy = y + ROW / 2, row = [text(LABEL, cy + 4, name, { size: 12, fill: dies ? C.text : C.muted, anchor: "end" })];
            if (!dies) row.push(bar(START, RIGHT, bar0, BAR, C.grey));
            else if (o.orphan) row.push(bar(START, MOMENT[2], bar0, BAR, C.blue, { rounded: false }), bar(MOMENT[2], MOMENT[3], bar0, BAR, C.amber), text(MOMENT[3] + 8, cy + 4, "an orphan", { size: 11, fill: C.amber }));
            else row.push(bar(START, MOMENT[dies], bar0, BAR, C.blue));
            if (o.refused) row.push(dot(MOMENT[1], cy, 10, C.surface), cross(MOMENT[1], cy, { r: 5 }));
            y += ROW;
            return row;
        });
        y += RUN;
        return drawn;
    });
    card("T002-what-dies.svg", {
        h: bottom + 62,
        title: "What dies, and when",
        sub: "Each bar ends at the instruction that removes its entity; the grey ones stay, untouched",
        alt: "A chart of the lifetimes of the catalog of Lesson 20, over four instructions. bowie.Remove() is refused. Out of the lineup, Bowie is removed, with Lodger and its track. Low becomes an orphan. The entry removed, Low and its tracks die. eno.Remove() removes Brian Eno, its album, its track and the lyrics. The genre, the playlist, the listener and the festival stay.",
    }, [
        code(288, 124, "the catalog", { anchor: "middle", fill: C.text }),
        [1, 2, 3, 4].map(n => [line(MOMENT[n], 146, MOMENT[n], bottom, { stroke: C.base }), text(MOMENT[n], 104, n, { weight: 600, fill: C.text, anchor: "middle" })]),
        code(MOMENT[1], 124, "bowie.Remove()", { anchor: "middle", fill: C.text }),
        text(MOMENT[1], 140, "refused", { size: 12, weight: 600, fill: C.red, anchor: "middle" }),
        code(MOMENT[2], 124, "Lineup.Remove(bowie)", { anchor: "middle", fill: C.text }),
        code(MOMENT[2], 140, "bowie.Remove()", { anchor: "middle", fill: C.text }),
        code(MOMENT[3], 124, "Entries.Remove(entry)", { anchor: "middle", fill: C.text }),
        code(MOMENT[4], 124, "eno.Remove()", { anchor: "middle", fill: C.text }),
        rows,
        code(226, bottom + 24, "Census()", { anchor: "end" }),
        CENSUS.map(([x, s]) => code(x, bottom + 24, s, { anchor: "middle", fill: C.text })),
        text(PAD, bottom + 46, "Census(): the artists, the albums, the tracks, the recordings and the lyrics of the transaction", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 21, JoinTheArtistsAndTheirCapabilities: which artists a system holds, before and after two capabilities change.
{
    const WIDTH = 432, HEAD = 148, ROW = 40, COLS = [132, 210, 288], MEMBER = 382;
    const has = (x, y, on, column, changed) => [
        changed && rect(x - 26, y - 15, 52, 30, { fill: C.blueSoft, rx: 6 }),
        on ? dot(x, y, 6, column === 2 ? C.red : C.blue) : ring(x, y, 6, C.base),
    ];
    const matrix = (x0, title, rows) => [
        code(x0, 104, title, { fill: C.text }),
        ["Tour", "Verified", "Retired"].map((c, i) => [text(x0 + COLS[i], 128, c, { size: 12, weight: 600, fill: C.text, anchor: "middle" }), text(x0 + COLS[i], 143, ["required", "optional", "excluded"][i], { size: 11, fill: C.muted, anchor: "middle" })]),
        text(x0 + MEMBER, 128, "On tour", { size: 12, weight: 600, fill: C.text, anchor: "middle" }),
        line(x0, HEAD + 4, x0 + WIDTH, HEAD + 4, { stroke: C.base }),
        rows.map(([name, tour, verified, retired, changed], k) => {
            const y = HEAD + 26 + k * ROW, member = tour && !retired;
            return [text(x0, y + 4, name, { fill: C.text }), has(x0 + COLS[0], y, tour, 0, changed === 0), has(x0 + COLS[1], y, verified, 1), has(x0 + COLS[2], y, retired, 2, changed === 2),
                member ? check(x0 + MEMBER, y, { r: 6 }) : line(x0 + MEMBER - 5, y, x0 + MEMBER + 5, y, { stroke: C.muted, sw: 2 }),
                k < rows.length - 1 && line(x0, y + 20, x0 + WIDTH, y + 20, { stroke: C.grid })];
        }),
    ];
    card("T002-system-members.svg", {
        h: 348,
        title: "A system holds the entities that have its components",
        sub: "SOnTour: the artists with a Tour, maybe Verified, never Retired",
        alt: "Two tables of four artists and their components. At first Bowie has a tour and is verified, Eno has a tour, Reed has a tour and is retired, Iggy has nothing: the system holds Bowie and Eno. After iggy.Tour.New() and eno.Retired.New(), it holds Bowie and Iggy, at the next instruction.",
    }, [
        matrix(PAD, "committed", [["Bowie", true, true, false], ["Eno", true, false, false], ["Reed", true, false, true], ["Iggy", false, false, false]]),
        matrix(496, "iggy.Tour.New(); eno.Retired.New();", [["Bowie", true, true, false], ["Eno", true, false, true, 2], ["Reed", true, false, true], ["Iggy", true, false, false, 0]]),
        text(PAD, 330, "A filled dot: the artist has the component. A component that the transaction attaches or removes is seen at the next instruction", { size: 11, fill: C.muted }),
    ]);
}

// The chapter From the need to the link: its questions as two ladders.
{
    const WIDTH = 192, ANSWER = 212, TALL = 64, PITCH = 84, TOP = 118;
    const ladder = (x0, title, rungs, last) => {
        const qx = x0, ax = x0 + WIDTH + 28, y = i => TOP + i * PITCH, cx = qx + WIDTH / 2;
        return [
            text(x0, 104, title, { weight: 600, fill: C.text }),
            rungs.map((r, i) => [
                block(qx, y(i), WIDTH, TALL, r.q.map(s => ({ s, size: 12, fill: C.text }))),
                link([[qx + WIDTH, y(i) + TALL / 2], [ax, y(i) + TALL / 2]]),
                text(qx + WIDTH + 14, y(i) + TALL / 2 - 7, "yes", { size: 11, anchor: "middle" }),
                block(ax, y(i), ANSWER, TALL, r.a, { state: "held" }),
                (i < rungs.length - 1 || last) && [link([[cx, y(i) + TALL], [cx, y(i + 1)]]), text(cx + 8, y(i) + TALL + 14, "no", { size: 11 })],
            ]),
            last && block(qx, y(rungs.length), WIDTH + 28 + ANSWER, TALL, last, { state: "held" }),
        ];
    };
    const D = (s) => M(s, { fill: C.text });
    card("T002-choose-a-link.svg", {
        h: 476,
        title: "From the need to the declaration",
        sub: "Ask the questions in their order: the first yes gives the declaration",
        alt: "Two ladders of questions. What is this type? Created and removed with another entity: a part, a composition T, Option<T> or IEntityCollection<T>, and [Orphanable] to outlive its Owner. Kept only while something uses it: a shared object, designated by [Strong]. Removed by your business: [Root, Removable]. Otherwise [Root]. What is this link? It carries data: a part with a strong reference and its properties. The target must stay while the link designates it: [Strong]. Otherwise [Weak], always an Option or a collection.",
    }, [
        ladder(PAD, "What is this type?", [
            { q: ["Is it created and", "removed with another", "entity?"], a: [B("A part"), M("T, Option<T>", { size: 11 }), M("IEntityCollection<T>", { size: 11 })] },
            { q: ["Is it kept only while", "something uses it?"], a: [B("A shared object"), M("[Strong] on its links", { size: 11 })] },
            { q: ["Does your business", "remove it?"], a: [B("A Root"), D("[Root, Removable]")] },
        ], [B("Reference data"), D("[Root]")]),
        ladder(496, "What is this link?", [
            { q: ["Does it carry data: a", "quantity, a date, a position?"], a: [B("A part of its own"), M("[Strong] T, and its values", { size: 11 })] },
            { q: ["Must the target stay while", "the link designates it?"], a: [B("A strong reference"), M("[Strong] T, [Strong] Option<T>", { size: 11 }), M("[Strong] IEntityCollection<T>", { size: 11 })] },
        ], [B("A weak reference"), D("[Weak] Option<T>, [Weak] IEntityCollection<T>")]),
        text(PAD, 458, "A part that must survive its Owner while something retains it: [Orphanable], its [OwnedFrom] an Option<T>", { size: 11, fill: C.muted }),
    ]);
}
