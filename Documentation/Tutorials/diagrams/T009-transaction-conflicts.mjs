// The diagrams of Tutorial T009. Each one draws the very objects of a test of its lesson: run `node T009-transaction-conflicts.mjs` after
// a change of the lesson, or of the kit. Their words stay those of the developer: transactions, commits, conflicts, values, links.
import { C, PAD, RIGHT, card, code, count, line, link, node, rect, text } from "./kit.mjs";

const note = (y, s) => text(PAD, y, s, { size: 11, fill: C.muted });

// Three points and what they give: the epoch, then the head and the new value, each a branch of it, and the value committed.
const threePoints = (x, y, { epoch, head, added, next, by, formula, result, state = "held", word }) => {
    const BOX = 96, TALL = 48, E = x, B = x + 350, R = x + 736, MID = y + 74, U = y, D = y + 100;
    return [
        node(E, MID - TALL / 2 + 24, BOX, TALL, String(epoch), "the epoch", { size: 18 }),
        link([[E + BOX, MID + 18], [B, U + TALL / 2]]),
        link([[E + BOX, MID + 30], [B, D + TALL / 2]]),
        text(E + BOX + 92, U + 24, `${by}, ${added[0]}`, { size: 12 }),
        text(E + BOX + 92, D + 44, `this transaction, ${added[1]}`, { size: 12 }),
        node(B, U, BOX, TALL, String(head), "the head", { size: 18 }),
        node(B, D, BOX, TALL, String(next), "new", { size: 18 }),
        link([[B + BOX, U + TALL / 2], [R, MID + 18]]),
        link([[B + BOX, D + TALL / 2], [R, MID + 30]]),
        node(R, MID - TALL / 2 + 24, 160, TALL, String(result), word, { size: 18, state }),
        code(R + 80, MID + 72, formula, { anchor: "middle", fill: C.text }),
    ];
};

// Lesson 1, AddTheSalesOfTwoTills: head + (new - epoch).
card("T009-three-points.svg", {
    h: 268,
    title: "A merge has three points: the epoch, the head and the new value",
    sub: "Two tills sell at the same time, on a [Delta]. The commit adds what the transaction changed to the last commit",
    alt: "The value at the opening of the transaction, the epoch, is 10 tickets. Another till commits 13, the head. This transaction writes 12, its new value. Its commit writes the head plus the new value minus the epoch, 13 plus 12 minus 10, 15 tickets: both sales are counted, without a conflict.",
}, [threePoints(PAD, 96, { epoch: 10, head: 13, added: ["+3", "+2"], next: 12, by: "another till", formula: "13 + (12 - 10)", result: 15, word: "committed" })]);

// Lesson 2, KeepTheLatestSaleAndTheLowestPrice and KeepACorrectionThatTheTransactionDidNotMake: the four cases, in a table of values.
{
    const X = [316, 470, 624, 778], BOX = 134, TALL = 42, TOP = 120, PITCH = 56;
    const heads = ["the epoch", "the head", "new", "committed"];
    const row = (y, name, sub, values, from) => [
        text(PAD, y + 18, name, { weight: 600, fill: C.text }),
        text(PAD, y + 36, sub, { size: 12 }),
        values.map((v, i) => node(X[i], y, BOX, TALL, v, null, { size: 13, state: i === 3 || i === from ? "held" : "alive" })),
    ];
    card("T009-max-min.svg", {
        h: 372,
        title: "[Max] and [Min] read the three points too",
        sub: "A transaction that changed the value keeps the best of two. One that did not takes the head",
        alt: "When both sides changed the value: the last sale at epoch 09:00, head 09:20, new 09:10 gives 09:20; the lowest price at epoch 40, head 35, new 30 gives 30. When only the head changed it, a correction: the last sale at epoch 09:00, head 08:00, new unchanged at 09:00 gives 08:00; the lowest price at epoch 40, head 45, new unchanged at 40 gives 45. A plain maximum would have undone the correction.",
    }, [
        heads.map((s, i) => text(X[i] + BOX / 2, TOP - 12, s, { size: 12, fill: C.muted, anchor: "middle" })),
        row(TOP, "[Max] LastSale", "both changed it", ["09:00", "09:20", "09:10", "09:20"], 1),
        row(TOP + PITCH, "[Min] LowestPrice", "both changed it", ["40", "35", "30", "30"], 2),
        line(PAD, TOP + 2 * PITCH - 7, RIGHT, TOP + 2 * PITCH - 7, { stroke: C.grid }),
        row(TOP + 2 * PITCH, "[Max] LastSale", "a correction, not by this transaction", ["09:00", "08:00", "09:00", "08:00"], 1),
        row(TOP + 3 * PITCH, "[Min] LowestPrice", "a correction, not by this transaction", ["40", "45", "40", "45"], 1),
        note(356, "The blue box is the side the value comes from. Without a concurrent change nothing merges, and the value written is committed."),
    ]);
}

// Lesson 3, RefuseAMergedValueOutOfItsBoundsAtTheCommit: the bound of a merged value.
card("T009-bound.svg", {
    h: 292,
    title: "A bound holds on the merged value, at the commit",
    sub: "[Delta(Min = 0)] Remaining: two seats. Each sale fits on what it read, and they do not fit together",
    alt: "Two seats remain, the epoch. Another till sells one and commits: the head is 1. This transaction sold two, and its new value is 0, which its Setter accepts. The merged value would be 1 plus 0 minus 2, minus 1, below the bound: the commit conflicts, of the kind Bound, nothing written. Its next attempt reads one seat, and the code of the sale refuses.",
}, [
    threePoints(PAD, 96, { epoch: 2, head: 1, added: ["-1", "-2"], next: 0, by: "another till", formula: "1 + (0 - 2)", result: -1, state: "refused", word: "below the bound" }),
    text(RIGHT, 274, "Bound: writes nothing", { size: 12, weight: 600, fill: C.red, anchor: "end" }),
    note(274, "The Setter accepted 0. Its next attempt reads the seat that remains, and the sale refuses."),
]);

// Lesson 4, the rule of the three points: the order in which a value without a rule of merge is settled.
{
    const Q = 340, A = 620, TALL = 40, TOP = 96, PITCH = 52;
    const step = (i, question, answer, state, word) => {
        const y = TOP + i * PITCH;
        return [
            count(PAD + 12, y + 27, String(i + 1)),
            node(PAD + 36, y, Q - PAD - 36, TALL, question, null, { size: 13 }),
            text(Q + 14, y + 25, word, { size: 12, fill: C.text }),
            link([[Q + 60, y + TALL / 2], [A, y + TALL / 2]]),
            node(A, y, RIGHT - A, TALL, answer, null, { size: 13, state }),
            i < 3 ? link([[PAD + 128, y + TALL], [PAD + 128, y + PITCH]]) : "",
        ];
    };
    card("T009-three-point-rule.svg", {
        h: 348,
        title: "A value without a rule of merge, at the commit",
        sub: "The first answer settles it. On a type [Mergeable], the unit of conflict is the value, no longer the entity",
        alt: "Four questions in their order. Did the transaction change the value? No: it takes the head. Did the head change it? No: it takes the new value. Did both give the same value? Yes: that value. Otherwise: a Merger decides if the type has one, and without a Merger the commit conflicts, of the kind Change. A link is outside this rule: changed by the head, it is always a conflict.",
    }, [
        step(0, "The transaction left it unchanged?", "the head", "alive", "yes"),
        step(1, "The head left it unchanged?", "the new value", "alive", "yes"),
        step(2, "Both gave the same value?", "that value", "alive", "yes"),
        step(3, "A Merger on the type?", "what the Merger chooses", "held", "yes"),
        code(PAD + 36, TOP + 4 * PITCH + 2, "no Merger: the commit conflicts, Change", { fill: C.red }),
        note(TOP + 4 * PITCH + 28, "A link is outside this rule: changed by the head, it is a conflict, even for a transaction that changed a [Delta] only."),
    ]);
}

// Lesson 5: what a commit does with an entity merged, in its order.
{
    const BOX = 166, GAP = 14, TOP = 104, TALL = 112;
    const parts = [
        ["The links", ["changed by the head:", "a conflict"], "alive"],
        ["The Merger", ["a value in conflict:", "Merge(epoch, head)"], "held"],
        ["The other values", ["the head, unless the", "value differs from the epoch"], "alive"],
        ["[Delta], [Max], [Min]", ["merged from what", "the Merger left"], "alive"],
        ["The bounds", ["a merged value out", "of them: Bound"], "alive"],
    ];
    card("T009-merge-order.svg", {
        h: 268,
        title: "A commit settles a merged entity in this order",
        sub: "Before it writes anything: a refusal at any step leaves nothing written",
        alt: "Five steps in their order. The links: changed by the head, a conflict. The Merger: called when a value is in conflict. The other values: the head, unless the value differs from the epoch. Then Delta, Max and Min, merged from what the Merger left, so that it reads Posted as its transaction wrote it. Then the bounds: a merged value out of them is a conflict of the kind Bound.",
    }, [
        parts.map(([name, lines, state], i) => {
            const x = PAD + i * (BOX + GAP);
            return [
                rect(x, TOP, BOX, TALL, { fill: state === "held" ? C.blueSoft : C.raised, stroke: state === "held" ? C.blue : C.base, sw: state === "held" ? 1.5 : 1 }),
                text(x + BOX / 2, TOP + 36, name, { size: 13, weight: 600, fill: C.text, anchor: "middle" }),
                lines.map((s, k) => text(x + BOX / 2, TOP + 62 + k * 18, s, { size: 11, anchor: "middle" })),
                i < parts.length - 1 ? link([[x + BOX, TOP + TALL / 2], [x + BOX + GAP, TOP + TALL / 2]]) : "",
            ];
        }),
        note(250, "The Merger runs first of the values: Posted is a [Max], and the Merger reads it as its transaction wrote it."),
    ]);
}
