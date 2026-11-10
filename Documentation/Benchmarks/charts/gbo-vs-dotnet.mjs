// The charts and the diagrams of "GBO vs .NET objects" in the README, in the card, the palette and the type of the other charts: the kit of
// the tutorial diagrams. `node gbo-vs-dotnet.mjs` writes them in this folder. Every figure is in F: the last run of REC01 to REC04 of
// RecordStringsBenchmark; the README repeats them.
import { C, PAD, RIGHT, bar, card, line, link, node, rect, text } from "../../Tutorials/diagrams/kit.mjs";

const F = {
    records: { aliveNet: 4.096, aliveGbo: 0.357, releasedNet: 0.740, releasedGbo: 0.0733, heapNet: 6239, heapGbo: 4656 },
    entities: { aliveNet: 4.038, aliveGbo: 0.0713, removedNet: 0.758, removedGbo: 0.00714, heapNet: 6239, heapGbo: 4239 },
    strings: { createdNet: 24.8, createdGbo: 9.84, dispersedNet: 2.62, dispersedGbo: 4.494, aligned: 8.19 },
    dates: { createdNet: 92.9, createdGbo: 39.1, dispersedNet: 32.3, dispersedGbo: 21.2, aligned: 61.6 },
    reads: [["1", 6.1], ["2", 11.5], ["4", 17.2], ["8", 21.4], ["16", 26.0], ["32", 30.0], ["64", 30.1], ["no limit", 32.1]],
    linkedList: 6.1,
};

const out = file => `../../Benchmarks/charts/${file}.svg`;
const FOOT = "Intel i9-9900K, Windows 10, .NET 10, workstation GC · 10,000,000 items of 10 strings and a date · 2026-11-01";
const X0 = 282, XMAX = 780, ROW = 52;
const small = (x, y, s, o = {}) => text(x, y, s, { size: 11, fill: C.muted, ...o });
const bold = (x, y, s, o = {}) => text(x, y, s, { size: 14, weight: 600, fill: C.text, ...o });
const swatch = (x, name, fill) => [rect(x, 58, 12, 12, { fill, rx: 3 }), text(x + 18, 68, name, { size: 12 })];
const seconds = v => v < 0.1 ? `${(v * 1000).toFixed(0)} ms` : `${v.toFixed(2)} s`;
const gigabytes = v => `${(v / 1024).toFixed(1)} GB`;
const rate = v => `${v.toFixed(1)} M/s`;

// The memory drawn as bands, on the model of the user: H a header, P a reference, the light cells the lengths of the strings, D their contents.
const ORANGE = "#e8743b", GREEN = "#1f7a33", LENGTH = "#1ba1dc", DATA = "#17607f", PURPLE = "#a12a96", BAND = 34, GHOST = 168;
const band = y => [line(PAD, y, RIGHT, y), line(PAD, y + BAND, RIGHT, y + BAND)];
const cell = (x, w, y, fill, s, { dim = false } = {}) => [rect(x, y, w, BAND, { fill: dim ? C.grid : fill, stroke: C.surface, sw: 1.5, rx: 0 }),
    s ? text(x + w / 2, y + BAND / 2 + 4, s, { size: 12, weight: 600, fill: dim ? C.muted : "#ffffff", anchor: "middle" }) : ""];
// An arc from a reference to what it designates: above a band, y its top; below it, h negative, y its bottom.
const arc = (x0, x1, y, h, stroke = C.text2) => {
    const head = Math.sign(h) * 7;
    return [`<path d="M${x0} ${y}C${x0} ${y - h} ${x1} ${y - h} ${x1} ${y - head}" fill="none" stroke="${stroke}" stroke-width="1.6"/>`,
        `<path d="M${x1 - 4} ${y - head}L${x1 + 4} ${y - head}L${x1} ${y}Z" fill="${stroke}"/>`];
};
const down = (x, y) => `<path d="M${x - 18} ${y}H${x + 18}V${y + 30}H${x + 36}L${x} ${y + 58}L${x - 36} ${y + 30}H${x - 18}Z" fill="${C.base}"/>`;
// The references of a collection, in its order, at the left of a band.
const references = (y, count) => Array.from({ length: count }, (_, i) => cell(40 + i * 24, 24, y, GREEN, `P${i + 1}`));
const reference = i => 52 + i * 24;
// A Ghost of three strings, as many as a diagram can show: its header, the lengths, then the contents, side by side.
const ghost = (x, y, n) => [cell(x, 28, y, PURPLE, `H${n}`), [0, 1, 2].map(i => cell(x + 28 + i * 8, 8, y, LENGTH)),
    cell(x + 52, 38, y, DATA, "D"), cell(x + 90, 50, y, DATA, "D"), cell(x + 140, 28, y, DATA, "D")];

// Rows of two bars, GBO then .NET, each on its own scale or on the same one, as the other charts draw them.
function chart(file, { title, sub, alt, rows, shared = false }) {
    const h = 100 + rows.length * ROW + 48, top = shared ? Math.max(...rows.flatMap(r => [r.gbo, r.net])) : 0;
    const body = [swatch(800, "GBO", C.blue), swatch(858, ".NET", C.grey), line(X0, 100, X0, 100 + rows.length * ROW - 4)];
    rows.forEach((r, i) => {
        const y = 110 + i * ROW, scale = (XMAX - X0) / (shared ? top : Math.max(r.gbo, r.net));
        const xGbo = X0 + r.gbo * scale, xNet = X0 + r.net * scale;
        body.push(text(X0 - 16, y + 13, r.label, { fill: C.text, anchor: "end" }), small(X0 - 16, y + 29, r.note, { anchor: "end" }));
        body.push(bar(X0, xGbo, y, 14, C.blue), text(xGbo + 8, y + 11.5, r.show(r.gbo), { weight: 600, fill: C.text }));
        body.push(bar(X0, xNet, y + 18, 14, C.grey), text(xNet + 8, y + 29.5, r.show(r.net)));
        const wins = r.lower ? r.gbo <= r.net : r.gbo >= r.net, ratio = wins ? (r.lower ? r.net / r.gbo : r.gbo / r.net) : (r.lower ? r.gbo / r.net : r.net / r.gbo);
        body.push(text(RIGHT, y + 23, r.ratio ?? (wins ? `×${ratio.toFixed(1)}` : `.NET ×${ratio.toFixed(1)}`), { size: r.ratio ? 14 : wins ? 18 : 14, weight: 600, fill: wins ? C.text : C.text2, anchor: "end" }));
    });
    body.push(small(PAD, h - 18, FOOT));
    card(out(file), { h, title, sub, alt }, body);
}

// Rows of one bar, on the same scale.
function bars(file, { title, sub, alt, rows, foot = FOOT, step = 44 }) {
    const h = 100 + rows.length * step + 44, top = Math.max(...rows.map(r => r.value)), scale = (XMAX - X0) / top;
    const body = [line(X0, 96, X0, 96 + rows.length * step)];
    rows.forEach((r, i) => {
        const y = 104 + i * step, x = X0 + Math.max(r.value * scale, 3);
        body.push(text(X0 - 16, y + 11, r.label, { fill: C.text, anchor: "end" }), r.note ? small(X0 - 16, y + 26, r.note, { anchor: "end" }) : "");
        body.push(bar(X0, x, y, 18, r.fill ?? C.grey), text(x + 8, y + 14, r.show, { weight: 600, fill: C.text }));
    });
    body.push(small(PAD, h - 18, foot));
    card(out(file), { h, title, sub, alt }, body);
}

// One record in memory, on the drawing of the user: a band of memory for each side, five of the ten strings. `at` takes the places of that
// drawing, in its pixels.
function layout() {
    const top = 160, bottom = 300;
    const at = x => Math.round((PAD + (x - 37) * (RIGHT - PAD) / 1458) * 10) / 10;
    const place = (x0, x1, y, fill, s) => cell(at(x0), at(x1) - at(x0), y, fill, s);
   const label = (x, y, strong, rest, anchor = "middle") => `<text x="${x}" y="${y}" font-size="15" fill="${C.text2}" text-anchor="${anchor}">${strong ? `<tspan font-weight="700" fill="${C.text}">${strong}</tspan>` : ""}${rest}</text>`;
    const pointer = points => link(points, { stroke: C.muted, sw: 1.3 });
    const strings = [[317, 68], [510, 102], [702, 50], [947, 229], [1315, 101]];

    const body = [band(top), band(bottom), place(61, 104, top, ORANGE, "H")];
    strings.forEach(([x, data], i) => {
        const pointerAt = 104 + i * 32;
        body.push(place(pointerAt, pointerAt + 32, top, GREEN, "P"), place(x, x + 43, top, ORANGE, "H"), place(x + 43, x + 61, top, LENGTH), place(x + 61, x + 61 + data, top, DATA, "D"));
    });
    strings.forEach(([x], i) => body.push(arc(at(120 + i * 32), at(x + 21), top, 38 + i * 6)));
    body.push(label(PAD, 104, "POCO", " header", "start"), label(at(458), 104, "", "string lengths"), label(at(786), 104, "", "string content"));
    body.push(pointer([[at(125), 112], [at(84), top - 3]]), pointer([[at(457), 112], [at(370), top - 3]]), pointer([[at(786), 112], [at(788), top - 3]]));

    body.push(down(499, 206));

    body.push(place(170, 203, bottom, GREEN, "P"), place(607, 650, bottom, PURPLE, "H"));
    let x = 650 + strings.length * 18;
    strings.forEach(([, data], i) => {
        body.push(place(650 + i * 18, 668 + i * 18, bottom, LENGTH), place(x, x + data, bottom, DATA, "D"));
        x += data;
    });
    body.push(arc(at(186), at(628), bottom, 44));
    body.push(pointer([[at(186), 372], [at(186), bottom + BAND + 3]]), pointer([[at(628), 372], [at(628), bottom + BAND + 3]]));
    body.push(label(at(186), 392, "Body", " (stack)"), label(at(628), 392, "Ghost", " header"));
    card(out("gbo-vs-dotnet-layout"), {
        h: 412, title: "One record of ten strings, in memory", sub: "In .NET, an object, and one object per string, each reached by a reference. In GBO, one block. Five of the ten strings are drawn.",
        alt: "Above, the memory of .NET: an object with its header and its references, then each string further away, an object of its own with a header, a length and its content, an arc from each reference to its string. Below, the memory of GBO: the Body points to one Ghost, its header, then the lengths of its strings side by side, then their contents side by side.",
    }, body);
}

// What a commit does.
function commit() {
    const body = [rect(PAD, 96, 262, 222, { stroke: C.base, rx: 10 }), text(PAD + 14, 118, "The transaction", { weight: 600, fill: C.text }), small(PAD + 248, 118, "private", { anchor: "end" })];
    [136, 192, 248].forEach(y => body.push(node(48, y, 84, 40, "Body", null), node(176, y, 104, 40, "Ghost", null, { state: "held" }), link([[132, y + 20], [176, y + 20]], { sw: 1.4 })));
    [["1. Check", "no conflict with the other transactions"], ["2. Write", "each Ghost copied to the end of the Store"], ["3. Index", "the indexes take the changes"], ["4. Publish", "visible at once to the next transactions"]].forEach(([name, what], i) => {
        const y = 96 + i * 58;
        body.push(rect(326, y, 308, 48, { fill: C.raised, stroke: C.base }), text(342, y + 20, name, { weight: 600, fill: C.text }), small(342, y + 37, what));
    });
    body.push(rect(666, 96, 262, 222, { fill: "none", stroke: C.blue, sw: 1.5, rx: 10 }), text(680, 118, "The Store", { weight: 600, fill: C.text }), small(914, 118, "shared", { anchor: "end" }));
    [136, 192, 248].forEach(y => [680, 762, 844].forEach(x => body.push(node(x, y, 74, 40, "Ghost", null, { state: "held", size: 12 }))));
    body.push(link([[294, 207], [326, 207]]), link([[634, 207], [666, 207]]));
    body.push(text(PAD, 350, "Then the Bodies and the memory of the transaction go to the GC. Only the bytes of the Store remain: not one object per entity.", { size: 13, fill: C.text }));
    card(out("gbo-vs-dotnet-commit"), {
        h: 372, title: "What a commit does", sub: "It turns the private work of a transaction into shared data, all of it or nothing.",
        alt: "On the left, the transaction holds Bodies and their Ghosts, private to it. A commit checks for conflicts, copies each Ghost to the end of the Store, updates the indexes, then publishes it all at once. On the right, the Store holds the Ghosts alone. The Bodies are then left to the GC.",
    }, body);
}

// After the swaps, on both sides: the memory of .NET, then that of GBO. Above a band, the references of a collection to its items; below it,
// those of one record to its strings, in color.
function dispersion() {
    const top = 172, bottom = 346, heads = {}, strings = [];
    const body = [bold(PAD, 104, ".NET"), text(RIGHT, 104, "one record to read: its object, then one jump per string, each address read from the jump before", { size: 12, anchor: "end" })];
    body.push(band(top), references(top, 4), small(40, top + BAND + 18, "the List"));
    let x = 146, object = 0;
    for (const item of [0, 2, "first", 4, 0, 1, "second", 3, 0, "third"]) {
        if (typeof item === "number" && item > 0) {
            const dim = item !== 1;
            body.push(cell(x, 28, top, ORANGE, `H${item}`, { dim }), [0, 1, 2].map(i => cell(x + 28 + i * 16, 16, top, GREEN, "P", { dim })));
            heads[item] = x + 14;
            if (item === 1) object = x;
            x += 76;
        } else {
            const dim = item === 0;
            body.push(cell(x, 16, top, ORANGE, "H", { dim }), cell(x + 16, 6, top, LENGTH, "", { dim }), cell(x + 22, 36, top, DATA, "D", { dim }));
            if (!dim) strings.push(x + 8);
            x += 58;
        }
        x += 14;
    }
    [1, 2, 3, 4].forEach(n => body.push(arc(reference(n - 1), heads[n], top, 44 + n * 6, n === 1 ? C.text : C.muted)));
    strings.forEach((target, i) => body.push(arc(object + 36 + i * 16, target, top + BAND, -(34 + i * 6), C.text)));
    body.push(small(RIGHT, top + BAND + 62, "in color, one record and three of its strings", { anchor: "end" }));

    body.push(bold(PAD, 290, "GBO"), text(RIGHT, 290, "one entity to read: one jump, its strings are in it", { size: 12, anchor: "end" }));
    body.push(band(bottom), references(bottom, 4), small(40, bottom + BAND + 18, "the index"));
    [3, 1, 4, 2].forEach((n, i) => {
        const at = 146 + i * (GHOST + 24);
        body.push(ghost(at, bottom, n), arc(reference(n - 1), at + 14, bottom, 34 + n * 5));
    });
    body.push(small(146, bottom + BAND + 18, "the numbers give the order of the collection"));
    card(out("gbo-vs-dotnet-dispersion"), {
        h: 424, title: "After the swaps, nothing is in order", sub: "Each side went through the same dispersion. An object of .NET lost its strings; a Ghost took them with it.",
        alt: "Above, the memory of .NET: the List points to its objects in another order, and one record points to three strings far apart among the others. Below, the memory of GBO: the index points to Ghosts in another order, each one holding its strings side by side.",
    }, body);
}

// The compactor, on the same bands.
function compactor() {
    const before = 160, after = 320, scattered = [3, 1, 4, 2];
    const body = [bold(PAD, 100, "Before"), band(before), references(before, 4), small(40, before + BAND + 18, "the index"), small(146, before + BAND + 18, "the Ghosts, in the order of their last writes")];
    scattered.forEach((n, i) => {
        const at = 146 + i * (GHOST + 24);
        body.push(ghost(at, before, n), arc(reference(n - 1), at + 14, before, 34 + n * 5));
    });
    body.push(down(499, 212), text(548, 246, "the compactor", { size: 14, weight: 600, fill: C.text }));
    body.push(bold(PAD, 280, "After"), band(after), references(after, 4), small(40, after + BAND + 18, "the index"), small(146, after + BAND + 18, "the Ghosts, side by side, in the order of the collection"));
    [1, 2, 3, 4].forEach(n => {
        const at = 146 + (n - 1) * GHOST;
        body.push(ghost(at, after, n), arc(reference(n - 1), at + 14, after, 30 + n * 4));
    });
    body.push(text(PAD, 400, "It copies the current version of each entity to the head of the Store, in the order of the identifiers, about 64 KB at a time, then releases the Segments it emptied.", { size: 12 }));
    body.push(text(PAD, 420, "Readers are not blocked: a copy is the same version, and each transaction keeps reading what it saw. It runs in the background, or on demand: 4.5 GB moved in 6.7 s.", { size: 12 }));
    card(out("gbo-vs-dotnet-compactor"), {
        h: 442, title: "The compactor puts the entities back in order", sub: "The order of a collection becomes the order of the memory again: a scan reads straight ahead.",
        alt: "Before, the index points to Ghosts in the order 3, 1, 4, 2, apart from each other. The compactor copies them; after, the same Ghosts are side by side in the order 1, 2, 3, 4. Readers are not blocked, and the emptied Segments are released.",
    }, body);
}

chart("gbo-vs-dotnet-gc-records", {
    title: "The GC and 10,000,000 Records", sub: "A full blocking collection · less is better · each row on its own scale",
    alt: `Full GC with every Record alive: GBO ${F.records.aliveGbo} s, .NET ${F.records.aliveNet} s. Once released: GBO ${F.records.releasedGbo} s, .NET ${F.records.releasedNet} s. Managed heap: GBO ${F.records.heapGbo} MB, .NET ${F.records.heapNet} MB.`,
    rows: [
        { label: "Full GC, all alive", note: "the GC visits every object", gbo: F.records.aliveGbo, net: F.records.aliveNet, show: seconds, lower: true },
        { label: "Full GC, all released", note: "the GC walks through what died", gbo: F.records.releasedGbo, net: F.records.releasedNet, show: seconds, lower: true },
        { label: "Managed heap", note: "with every Record alive", gbo: F.records.heapGbo, net: F.records.heapNet, show: gigabytes, lower: true, ratio: "−25 %" },
    ],
});

bars("gbo-vs-dotnet-gc-visits", {
    title: "What a full GC has to visit", sub: "Objects on the managed heap, for 10,000,000 items, on the same scale",
    alt: ".NET: 110,000,000 objects. GBO Records: 10,000,000 Bodies and about 34,000 pages. GBO entities after their commit: 146 Segments, 74 once compacted, and no object per entity.",
    rows: [
        { label: ".NET", note: "each record and its 10 strings", value: 110_000_000, show: "110,000,000 objects" },
        { label: "GBO Records", note: "each Body, and the pages of bytes", value: 10_034_000, show: "10,000,000 Bodies + about 34,000 pages", fill: C.blue },
        { label: "GBO entities", note: "committed: Segments of bytes, no Body", value: 146, show: "146 Segments, 74 once compacted", fill: C.blue },
    ],
});

chart("gbo-vs-dotnet-gc-entities", {
    title: "The GC and 10,000,000 entities", sub: "A full blocking collection · less is better · each row on its own scale",
    alt: `Full GC with every entity alive: GBO ${F.entities.aliveGbo} s, .NET ${F.entities.aliveNet} s. Once all removed: GBO ${F.entities.removedGbo} s, .NET ${F.entities.removedNet} s. Managed heap: GBO ${F.entities.heapGbo} MB, .NET ${F.entities.heapNet} MB.`,
    rows: [
        { label: "Full GC, all alive", note: "after the commit", gbo: F.entities.aliveGbo, net: F.entities.aliveNet, show: seconds, lower: true },
        { label: "Full GC, all removed", note: "nothing left to walk through", gbo: F.entities.removedGbo, net: F.entities.removedNet, show: seconds, lower: true, ratio: "about ×100" },
        { label: "Managed heap", note: "with every entity alive", gbo: F.entities.heapGbo, net: F.entities.heapNet, show: gigabytes, lower: true, ratio: "−32 %" },
    ],
});

chart("gbo-vs-dotnet-query-strings", {
    title: "A query on 5 strings", sub: "Items read per second · one thread · the same scale for every row", shared: true,
    alt: `As created: GBO ${F.strings.createdGbo} M/s, .NET ${F.strings.createdNet} M/s. Dispersed: GBO ${F.strings.dispersedGbo} M/s, .NET ${F.strings.dispersedNet} M/s. GBO aligned by the compactor ${F.strings.aligned} M/s, .NET still dispersed ${F.strings.dispersedNet} M/s.`,
    rows: [
        { label: "As created", note: "strings next to their object", gbo: F.strings.createdGbo, net: F.strings.createdNet, show: rate },
        { label: "Dispersed", note: "the same swaps on both sides", gbo: F.strings.dispersedGbo, net: F.strings.dispersedNet, show: rate },
        { label: "GBO aligned", note: "by the compactor; .NET stays dispersed", gbo: F.strings.aligned, net: F.strings.dispersedNet, show: rate },
    ],
});

chart("gbo-vs-dotnet-query-date", {
    title: "A query on a date", sub: "Items read per second · one thread · the same scale for every row", shared: true,
    alt: `As created: GBO ${F.dates.createdGbo} M/s, .NET ${F.dates.createdNet} M/s. Dispersed: GBO ${F.dates.dispersedGbo} M/s, .NET ${F.dates.dispersedNet} M/s. GBO aligned by the compactor ${F.dates.aligned} M/s, .NET still dispersed ${F.dates.dispersedNet} M/s.`,
    rows: [
        { label: "As created", note: "objects in the order of the List", gbo: F.dates.createdGbo, net: F.dates.createdNet, show: rate },
        { label: "Dispersed", note: "the same swaps on both sides", gbo: F.dates.dispersedGbo, net: F.dates.dispersedNet, show: rate },
        { label: "GBO aligned", note: "by the compactor; .NET stays dispersed", gbo: F.dates.aligned, net: F.dates.dispersedNet, show: rate },
    ],
});

bars("gbo-vs-dotnet-overlapping-reads", {
    title: "Random reads overlap", sub: "10,000,000 objects of .NET read at random: the same objects, the same order, the reads that may run at once", step: 30,
    alt: `With one read at a time, ${F.reads[0][1]} M/s, as a linked list at ${F.linkedList} M/s. The throughput rises with the reads allowed at once, up to ${F.reads.at(-1)[1]} M/s without limit.`,
    rows: [
        { label: "Linked list", value: F.linkedList, show: rate(F.linkedList) },
        ...F.reads.map(([n, value]) => ({ label: n === "no limit" ? "No limit" : n === "1" ? "1 read at a time" : `${n} reads at a time`, value, show: rate(value) })),
    ],
});

layout();
commit();
dispersion();
compactor();
