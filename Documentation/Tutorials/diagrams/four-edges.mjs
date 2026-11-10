// The diagrams of Tutorial Four. Each one draws the very objects of a test of its lesson: run `node four-edges.mjs` after a change
// of the lesson, or of the kit. Their words stay those of the developer: edges, ends, Views, Bodies, strong references.
import { C, PAD, RIGHT, bar, block, card, check, code, count, cross, dot, legend, line, link, node, owner, rect, steps, text } from "./kit.mjs";

const STRONG = { mark: "strong", name: "strong reference" };
const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o });

// An edge is an entity between its two ends: a chip on the line that joins them. It retains neither, so the line has no arrow
// head; only a graph of one type, where the direction is the point, has one. The chip carries its values, or stays small without.
const chip = (cx, cy, s, { w = 64, state = "alive" } = {}) => s === undefined
    ? rect(cx - 8, cy - 5, 16, 10, { fill: state === "held" ? C.blueSoft : C.raised, stroke: state === "held" ? C.blue : C.base, rx: 3 })
    : node(cx - w / 2, cy - 12, w, 24, s, null, { size: 12, state });
const tie = (points, { state, stroke, sw, head = false } = {}) => state === "gone"
    ? link(points, { weak: true, head: false, stroke: C.base, sw: 1 })
    : link(points, { head, stroke: stroke ?? (state === "held" ? C.blue : C.base), sw: sw ?? 1.4 });
const edge = (before, after, [cx, cy], s, { w = 64, ...o } = {}) => [tie(before, { ...o, head: false }), tie(after, o), chip(cx, cy, s, { w, state: o.state })];
const across = (x1, x2, y, s, { w = 64, cx = (x1 + x2) / 2, ...o } = {}) => {
    const half = Math.sign(x2 - x1) * (s === undefined ? 8 : w / 2);
    return edge([[x1, y], [cx - half, y]], [[cx + half, y], [x2, y]], [cx, y], s, { w, ...o });
};
const along = (x, y1, y2, s, { w = 64, ...o } = {}) => {
    const cy = (y1 + y2) / 2, half = Math.sign(y2 - y1) * 12;
    return edge([[x, y1], [x, cy - half]], [[x, cy + half], [x, y2]], [x, cy], s, { w, ...o });
};
// The mark of an edge on the last line, then the marks of the kit.
const legendOf = (y, name, rest = [], { head = false, x = PAD } = {}) => [
    line(x, y - 4, x + 8, y - 4, { stroke: C.text2, sw: 1.4 }), rect(x + 8, y - 9, 18, 10, { fill: C.raised, stroke: C.base, rx: 3 }),
    link([[x + 26, y - 4], [x + (head ? 44 : 34), y - 4]], { head, stroke: C.text2, sw: 1.4 }),
    text(x + (head ? 52 : 42), y, name, { size: 11, fill: C.muted }),
    legend(y, rest, { x: x + (head ? 52 : 42) + name.length * 5.8 + 22 }),
];
const toll = (x, y) => [rect(x, y - 9, 18, 10, { fill: C.amberSoft, stroke: C.amber, sw: 1.5, rx: 3 }), text(x + 26, y, "a road with a toll", { size: 11, fill: C.muted })];
// What a line of code reads: the code on the left, its result on the right.
const reads = (x, y, width, rows, pitch = 22) => rows.map(([s, result], k) => [code(x, y + k * pitch, s), text(x + width, y + k * pitch, result, { size: 12, weight: 600, fill: C.text, anchor: "end" })]);
const heading = (x, width, name, decl) => [text(x, 108, name, { weight: 600, fill: C.text }), code(x, 127, decl), line(x, 140, x + width, 140, { stroke: C.grid })];

// Lesson 1, ReadTheRatingsFromBothEnds: the same three ratings, read from a critic, then from a movie.
{
    const WIDTH = 432, GAP = 32, col = i => PAD + i * (WIDTH + GAP), BOX = 84, TALL = 40, TOP = 156, BOTTOM = 256, CHIP = 72;
    const graph = (i, { ada, bob, alien, dune, three, five, four }) => {
        const x = col(i), left = x + BOX / 2, right = x + WIDTH - BOX / 2, cx = x + WIDTH / 2, mid = (TOP + TALL + BOTTOM) / 2;
        return [
            node(x, TOP, BOX, TALL, "Ada", null, { state: ada }),
            node(x, BOTTOM, BOX, TALL, "Bob", null, { state: bob }),
            node(x + WIDTH - BOX, TOP, BOX, TALL, "Alien", null, { state: alien }),
            node(x + WIDTH - BOX, BOTTOM, BOX, TALL, "Dune", null, { state: dune }),
            across(x + BOX, x + WIDTH - BOX, TOP + TALL / 2, "3 stars", { w: CHIP, state: three }),
            edge([[left, TOP + TALL], [left, mid], [cx - CHIP / 2, mid]], [[cx + CHIP / 2, mid], [right, mid], [right, BOTTOM]], [cx, mid], "5 stars", { w: CHIP, state: five }),
            across(x + BOX, x + WIDTH - BOX, BOTTOM + TALL / 2, "4 stars", { w: CHIP, state: four }),
        ];
    };
    card("four-both-ends.svg", {
        h: 396,
        title: "A rating stands between its two ends, and both read it",
        sub: "The same three ratings, read from a critic, then from a movie",
        alt: "Two critics, Ada and Bob, and two movies, Alien and Dune. Ada gave Alien 3 stars and Dune 5 stars; Bob gave Dune 4 stars. Each rating is an entity between its critic and its movie. From the critic, ada.RatingEdges holds two ratings, whose Targets are Alien and Dune, and ada.RatingEdges.To(alien) is the rating of 3 stars. From the movie, dune.RatingEdges holds two ratings, whose Sources are Ada and Bob, and dune.RatingEdges.From(bob) is the rating of 4 stars. Bob did not rate Alien: bob.RatingEdges.To(alien) is none.",
    }, [
        heading(col(0), WIDTH, "From the critic", "ada.RatingEdges"),
        graph(0, { ada: "held", bob: "alive", alien: "held", dune: "held", three: "held", five: "held", four: "alive" }),
        reads(col(0), 324, WIDTH, [["ada.RatingEdges.Targets", "Alien, Dune"], ["ada.RatingEdges.To(alien)", "3 stars"]]),
        heading(col(1), WIDTH, "From the movie", "dune.RatingEdges"),
        graph(1, { ada: "held", bob: "held", alien: "alive", dune: "held", three: "alive", five: "held", four: "held" }),
        reads(col(1), 324, WIDTH, [["dune.RatingEdges.Sources", "Ada, Bob"], ["dune.RatingEdges.From(bob)", "4 stars"]]),
        legendOf(378, "an edge: an entity between its two ends"),
        text(RIGHT, 378, "A pair without rating: bob.RatingEdges.To(alien) is none", { size: 11, fill: C.muted, anchor: "end" }),
    ]);
}

// Lesson 2, RateAMovieOnce: four instructions, and the ratings counted after each one.
{
    const STEPS = [
        { code: "ada.NewRating(dune, 4)", stars: "4 stars", n: 1, say: "rating" },
        { code: "ada.NewRating(dune, 2)", stars: "4 stars", refused: true, n: 1, say: "refused: nothing changes" },
        { code: "Rate(ada, dune, 5)", stars: "5 stars", changed: true, n: 1, say: "the rating of the pair" },
        { code: "Rate(ada, alien, 3)", stars: "5 stars", alien: true, n: 2, say: "a new one" },
    ];
    const COL = 206, GAP = 24, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, ROW1 = 134, ROW2 = 234, TALL = 34, BOX = 96, CHIP = 66, CY = 201;
    card("four-one-per-pair.svg", {
        h: 366,
        title: "One rating per pair: the second is refused, GetOrNew finds the first",
        sub: "The ratings, counted at each instruction",
        alt: "Four steps. ada.NewRating(dune, 4) makes a rating of 4 stars: 1 rating. ada.NewRating(dune, 2) is refused: the pair has its rating, nothing changes, and the count stays 1. Rate(ada, dune, 5) finds the rating of the pair and gives it 5 stars: still 1 rating. Rate(ada, alien, 3) finds no rating for this pair, and makes a new one of 3 stars: 2 ratings. Rate is critic.RatingEdges.GetOrNew(movie).Stars = stars.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => {
            const x = x0(i), dune = x + BOX / 2, second = x + COL - BOX / 2;
            return [
                code(mid(i), 118, s.code, { anchor: "middle", fill: C.text }),
                node(x, ROW1, COL, TALL, "Ada"),
                node(x, ROW2, BOX, TALL, "Dune"),
                s.alien && node(x + COL - BOX, ROW2, BOX, TALL, "Alien", null, { state: "held" }),
                along(dune, ROW1 + TALL, ROW2, s.stars, { w: CHIP, state: s.changed ? "held" : "alive" }),
                s.refused && [
                    edge([[second, ROW1 + TALL], [second, CY - 12]], [[second, CY + 12], [second, ROW2 + TALL / 2], [x + BOX + 20, ROW2 + TALL / 2]], [second, CY], "2 stars", { w: CHIP, state: "refused", stroke: C.red }),
                    cross(x + BOX + 12, ROW2 + TALL / 2, { r: 4 }),
                ],
                s.alien && along(second, ROW1 + TALL, ROW2, "3 stars", { w: CHIP, state: "held" }),
                count(mid(i), 296, s.n, { fill: s.refused ? C.red : C.text }),
                text(mid(i), 316, s.say, { size: 12, weight: s.refused ? 600 : 400, fill: s.refused ? C.red : C.muted, anchor: "middle" }),
            ];
        }),
        text(PAD, 348, "Rate, the same instruction for the first time and the next ones:", { size: 11, fill: C.muted }),
        code(352, 348, "critic.RatingEdges.GetOrNew(movie).Stars = stars", { size: 11 }),
    ]);
}

// Lesson 3, RemoveAnEdge and RemoveTheEdgesWithTheirEnd: what each removal takes.
{
    const WIDTH = 432, GAP = 32, col = i => PAD + i * (WIDTH + GAP), BOX = 84, TALL = 40, TOP = 164, BOTTOM = 284, CHIP = 84;
    const name = (x, y, s, state = "held") => node(x, y, BOX, TALL, s, null, { state });
    const a = col(0), b = col(1), cxA = a + WIDTH / 2, cxB = b + WIDTH / 2, top = TOP + TALL / 2, bottom = BOTTOM + TALL / 2;
    const yA = 244, y1 = 224, y2 = 264, GONE = { w: CHIP, state: "gone" };
    card("four-edge-and-ends.svg", {
        h: 424,
        title: "An edge goes with either end, and its ends never go with it",
        sub: "What each removal takes stays drawn, dashed; what stays is in blue",
        alt: "Two panels. On the left, Ada rated Dune 5 stars and Alien 1 star. rating.Remove() and ada.RatingEdges.Remove(alien) remove the two ratings: Ada, Dune and Alien stay, one critic and two movies. On the right, Ada rated Dune 5 stars and Alien 3 stars, Bob rated Dune 4 stars, and Ada saw Dune once. dune.Remove() removes Dune, its two ratings and its screening at that instruction: Ada, Bob and Alien stay, with the rating of Alien by Ada. Bob has no rating left, and no screening is left.",
    }, [
        heading(a, WIDTH, "An edge removed", "rating.Remove(); ada.RatingEdges.Remove(alien);"),
        name(a, TOP, "Ada"), name(a + WIDTH - BOX, TOP, "Alien"), name(a + WIDTH - BOX, BOTTOM, "Dune"),
        across(a + BOX, a + WIDTH - BOX, top, "1 star", GONE),
        edge([[a + BOX / 2, TOP + TALL], [a + BOX / 2, yA], [cxA - CHIP / 2, yA]], [[cxA + CHIP / 2, yA], [a + WIDTH - BOX / 2, yA], [a + WIDTH - BOX / 2, BOTTOM]], [cxA, yA], "5 stars", GONE),
        text(a, 354, "Its ends stay", { weight: 600, fill: C.text }),
        reads(a, 378, WIDTH, [["ada.RatingEdges.Count()", "0"], ["(CriticCollection.Count(), MovieCollection.Count())", "(1, 2)"]]),

        heading(b, WIDTH, "An end removed", "dune.Remove();"),
        name(b, TOP, "Ada"), name(b, BOTTOM, "Bob"), name(b + WIDTH - BOX, TOP, "Alien"), name(b + WIDTH - BOX, BOTTOM, "Dune", "gone"),
        across(b + BOX, b + WIDTH - BOX, top, "3 stars", { w: CHIP, state: "held" }),
        edge([[b + 60, TOP + TALL], [b + 60, y1], [cxB - CHIP / 2, y1]], [[cxB + CHIP / 2, y1], [b + WIDTH - 24, y1], [b + WIDTH - 24, BOTTOM]], [cxB, y1], "5 stars", GONE),
        edge([[b + 24, TOP + TALL], [b + 24, y2], [cxB - CHIP / 2, y2]], [[cxB + CHIP / 2, y2], [b + WIDTH - 60, y2], [b + WIDTH - 60, BOTTOM]], [cxB, y2], "Screening", GONE),
        across(b + BOX, b + WIDTH - BOX, bottom, "4 stars", GONE),
        text(b, 354, "Its edges go at that instruction", { weight: 600, fill: C.text }),
        reads(b, 378, WIDTH, [["ScreeningCollection.Count()", "0"], ["bob.RatingEdges.Count()", "0"]]),
    ]);
}

// Lesson 4, WriteTheFewByTheirBody: a search by the Views, and the few that cross to a Body.
{
    const R1 = 176, R2 = 278, RATINGS = [["Ada, 5 stars", 146], ["Bob, 4 stars", 176], ["Cid, 0 stars", 206]], VIEW = 90, ITEM = 112;
    card("four-views-and-bodies.svg", {
        h: 372,
        title: "A search by the Views finds the few to write by their Body",
        sub: "The moderation removes the ratings without a star; then Cid rates Alien, found by its View",
        alt: "On the left, by the Views, no Body is created: the View of Dune gives the Views of its three ratings, 5, 4 and 0 stars; Where keeps the one below 1 star, the rating of Cid. Its ToBody() crosses to the right, to its Body, whose Remove() removes it. Below, the View of Alien is given to the Body of Cid: cid.NewRating(alien, 4) makes a rating of 4 stars. After the commit, Dune has 2 ratings and Alien 8 stars.",
    }, [
        owner(PAD, 100, 540, 226, "By the Views", "no Body is created"),
        owner(644, 100, 284, 226, "By a Body", "the few to write", { state: "held" }),

        node(48, R1 - 22, VIEW, 44, "Dune", "a View"),
        link([[48 + VIEW, R1], [244, R1]]),
        code(191, R1 - 8, "RatingEdges", { anchor: "middle" }),
        RATINGS.map(([s, y]) => node(244, y - 12, ITEM, 24, s, null, { size: 12 })),
        link([[356, R1 + 30], [436, R1 + 30], [436, R1], [452, R1]], { stroke: C.blue, sw: 2 }),
        code(398, R1 + 22, "Where(...)", { anchor: "middle" }),
        text(398, R1 + 50, "Stars < 1", { size: 11, fill: C.muted, anchor: "middle" }),
        node(452, R1 - 12, ITEM, 24, "Cid, 0 stars", null, { size: 12, state: "held" }),
        link([[452 + ITEM, R1], [660, R1]], { stroke: C.blue, sw: 2 }),
        code(612, R1 - 8, "ToBody()", { anchor: "middle" }),
        node(660, R1 - 22, 96, 44, "Cid, 0 stars", "a Body", { state: "held" }),
        link([[756, R1], [824, R1]]),
        code(790, R1 - 8, "Remove()", { anchor: "middle" }),
        node(824, R1 - 22, 88, 44, "0 stars", "removed", { state: "gone" }),

        node(48, R2 - 22, VIEW, 44, "Alien", "a View"),
        link([[48 + VIEW, R2], [660, R2]]),
        text(400, R2 - 8, "A birth takes its other end by its View", { size: 12, anchor: "middle" }),
        node(660, R2 - 22, 96, 44, "Cid", "a Body", { state: "held" }),
        link([[756, R2], [824, R2]]),
        node(824, R2 - 22, 88, 44, "4 stars", "new", { state: "held" }),
        code(660, R2 - 34, "cid.NewRating(alien, 4)", { fill: C.text }),

        text(PAD, 354, "After the commit: Dune has 2 ratings, and Alien 8 stars", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 5, ConnectTwoCitiesInOneDirection, GoBothWays and HaveSeveralRoadsBetweenTwoCities: the same two cities, three maps.
{
    const COL = 278, GAP = 31, col = i => PAD + i * (COL + GAP), BOX = 76, TOP = 156, TALL = 72, CHIP = 86, Y = TOP + TALL / 2, Y1 = Y - 18, Y2 = Y + 18;
    const cities = i => [node(col(i), TOP, BOX, TALL, "Paris"), node(col(i) + COL - BOX, TOP, BOX, TALL, "Lyon")];
    const road = (i, y, s, o = {}) => across(col(i) + (o.back ? COL - BOX : BOX), col(i) + (o.back ? BOX : COL - BOX), y, s, { w: CHIP, head: true, stroke: C.text2, sw: 1.6, ...o });
    card("four-one-way-roads.svg", {
        h: 372,
        title: "A road goes one way: a way back is another road",
        sub: "Each road is an edge of its own, from its From to its To, and two cities may have several",
        alt: "Three maps of Paris and Lyon. One road of 465 km from Paris to Lyon: paris.RoadEdges.Targets gives Lyon, lyon.IncomingRoadEdges.Sources gives Paris, and neither city has a road the other way. A way back: a second road from Lyon to Paris; paris.RoadEdges.Targets and paris.IncomingRoadEdges.Sources both give Lyon, and the map has 2 roads. Two roads from Paris to Lyon, 465 km with a toll and 490 km free: paris.RoadEdges.To(lyon) holds 2 roads, and Lyon is one target.",
    }, [
        heading(col(0), COL, "One road", "paris.NewRoad(lyon, 465)"),
        cities(0),
        road(0, Y, "465 km"),
        reads(col(0), 258, COL, [["paris.RoadEdges.Targets", "Lyon"], ["lyon.IncomingRoadEdges.Sources", "Paris"], ["lyon.RoadEdges.Count()", "0"], ["paris.IncomingRoadEdges.Count()", "0"]]),

        heading(col(1), COL, "A way back", "lyon.NewRoad(paris, 465)"),
        cities(1),
        road(1, Y1, "465 km"),
        road(1, Y2, "465 km", { back: true, stroke: C.blue, state: "held" }),
        reads(col(1), 258, COL, [["paris.RoadEdges.Targets", "Lyon"], ["paris.IncomingRoadEdges.Sources", "Lyon"], ["RoadCollection.Count()", "2"]]),

        heading(col(2), COL, "Two roads, one pair", "paris.NewRoad(lyon, 490)"),
        cities(2),
        road(2, Y1, "465 km", { state: "orphan" }),
        road(2, Y2, "490 km", { stroke: C.blue, state: "held" }),
        reads(col(2), 258, COL, [["paris.RoadEdges.To(lyon).Count()", "2"], ["To(lyon).Min(r => r.Distance)", "465"], ["paris.RoadEdges.Targets", "Lyon"]]),

        legendOf(354, "a road: an edge in one direction", [], { head: true }),
        toll(280, 354),
    ]);
}

// Lesson 6, ReachTheCitiesWithinTwoRoads: the map of the lesson, each frontier giving the next one.
{
    const BOX = 104, TALL = 40, C0 = 32, C1 = 296, C2 = 560, C3 = 824, R0 = 136, R1 = 232, R2 = 328, CHIP = 62, MIDDLE = 190, cy = y => y + TALL / 2, MET = { stroke: C.blue, sw: 2, head: true, w: CHIP }, REST = { stroke: C.text2, sw: 1.6, head: true, w: CHIP };
    const city = (x, y, s, state = "held") => node(x, y, BOX, TALL, s, null, { state });
    const header = (x, s, sub) => [text(x + BOX / 2, 104, s, { weight: 600, fill: C.text, anchor: "middle" }), sub && code(x + BOX / 2, 120, sub, { anchor: "middle" })];
    const RESULT = C1 + BOX + 40;
    card("four-breadth-walk.svg", {
        h: 408,
        title: "A walk in breadth: each frontier gives the next one",
        sub: "The cities within two roads of Paris, each met once",
        alt: "The map of the lesson, from Paris. One road away, Paris reaches Lyon, Dijon and Lille. Two roads away, Lyon reaches Marseille and Grenoble; Dijon reaches only Lyon, met already, which the walk does not take again. Reachable(paris, roads: 1) gives Dijon, Lille and Lyon. Reachable(paris, roads: 2) gives Dijon, Grenoble, Lille, Lyon and Marseille. Nice, a third road away, is not reached. The roads from Paris to Lyon, from Lyon to Marseille and from Marseille to Nice have a toll.",
    }, [
        header(C0, "The start", "paris"),
        header(C1, "One road", "roads: 1"),
        header(C2, "Two roads", "roads: 2"),
        header(C3, "Not reached"),

        edge([[C0 + BOX / 2, R1], [C0 + BOX / 2, cy(R0)], [MIDDLE - CHIP / 2, cy(R0)]], [[MIDDLE + CHIP / 2, cy(R0)], [C1, cy(R0)]], [MIDDLE, cy(R0)], "465 km", { ...MET, state: "orphan" }),
        across(C0 + BOX, C1, cy(R1), "315 km", MET),
        edge([[C0 + BOX / 2, R1 + TALL], [C0 + BOX / 2, cy(R2)], [MIDDLE - CHIP / 2, cy(R2)]], [[MIDDLE + CHIP / 2, cy(R2)], [C1, cy(R2)]], [MIDDLE, cy(R2)], "225 km", MET),
        along(C1 + BOX / 2, R1, R0 + TALL, "195 km", REST),
        text(C1 + BOX / 2 - CHIP / 2 - 10, (R0 + TALL + R1) / 2 + 4, "Lyon, met already", { size: 11, fill: C.muted, anchor: "end" }),
        across(C1 + BOX, C2, R0 + 10, "315 km", { ...MET, cx: 486, state: "orphan" }),
        edge([[C1 + BOX, R0 + 30], [442, R0 + 30], [442, cy(R1)], [500 - CHIP / 2, cy(R1)]], [[500 + CHIP / 2, cy(R1)], [C2, cy(R1)]], [500, cy(R1)], "110 km", MET),
        along(C2 + BOX / 2, R1, R0 + TALL, "270 km", REST),
        across(C2 + BOX, C3, cy(R0), "200 km", { ...REST, state: "orphan" }),

        city(C0, R1, "Paris"), city(C1, R0, "Lyon"), city(C1, R1, "Dijon"), city(C1, R2, "Lille"), city(C2, R0, "Marseille"), city(C2, R1, "Grenoble"), city(C3, R0, "Nice", "alive"),

        code(RESULT, 342, "Reachable(paris, roads: 1)"), text(RIGHT, 342, "Dijon, Lille, Lyon", { size: 12, weight: 600, fill: C.text, anchor: "end" }),
        code(RESULT, 364, "Reachable(paris, roads: 2)"), text(RIGHT, 364, "Dijon, Grenoble, Lille, Lyon, Marseille", { size: 12, weight: 600, fill: C.text, anchor: "end" }),
        toll(PAD, 390),
    ]);
}

// Lesson 6, FindTheShortestRoute and AvoidTheTolls: the same map, and a condition in the walk.
{
    const BOX = 88, TALL = 32, CHIP = 56, X = { paris: 32, north: 176, lyon: 328, grenoble: 480, marseille: 632, nice: 840 };
    const map = (y0, { shown, result, length, on, skipped, roads }) => {
        const top = y0 + TALL / 2, mid = y0 + 56 + TALL / 2, bottom = y0 + 112 + TALL / 2, low = mid + TALL / 2;
        const city = (x, tier, s, key) => node(x, tier - TALL / 2, BOX, TALL, s, null, { state: on.has(key) ? "held" : "alive" });
        const style = (key, toll) => on.has(key)
            ? { stroke: C.blue, sw: 2, head: true, w: CHIP, state: toll ? "orphan" : "held" }
            : { stroke: skipped.has(key) ? C.base : C.text2, sw: skipped.has(key) ? 1.2 : 1.4, head: true, w: CHIP, state: toll ? "orphan" : "alive" };
        const skip = (x, y) => [dot(x, y, 9, C.surface), cross(x, y, { r: 4 })];
        return [
            code(X.lyon, y0 + 12, shown, { fill: C.text }),
            text(X.lyon, y0 + 30, result, { weight: 600, fill: C.text }),
            count(872, y0 + 26, length),
            edge([[X.paris + BOX / 2, mid - TALL / 2], [X.paris + BOX / 2, top], [122 - CHIP / 2, top]], [[122 + CHIP / 2, top], [X.north, top]], [122, top], roads[0], style("paris-lille")),
            edge([[X.paris + BOX / 2, low], [X.paris + BOX / 2, bottom], [122 - CHIP / 2, bottom]], [[122 + CHIP / 2, bottom], [X.north, bottom]], [122, bottom], roads[1], style("paris-dijon")),
            across(X.paris + BOX, X.lyon, mid, roads[2], style("paris-lyon", true)),
            edge([[X.north + BOX, bottom], [308 - CHIP / 2, bottom]], [[308 + CHIP / 2, bottom], [352, bottom], [352, low]], [308, bottom], roads[3], style("dijon-lyon")),
            edge([[392, low], [392, bottom], [434 - CHIP / 2, bottom]], [[434 + CHIP / 2, bottom], [X.grenoble, bottom]], [434, bottom], roads[4], style("lyon-grenoble")),
            across(X.lyon + BOX, X.marseille, mid, roads[5], style("lyon-marseille", true)),
            edge([[X.grenoble + BOX, bottom], [612 - CHIP / 2, bottom]], [[612 + CHIP / 2, bottom], [656, bottom], [656, low]], [612, bottom], roads[6], style("grenoble-marseille")),
            across(X.marseille + BOX, X.nice, mid, roads[7], style("marseille-nice", true)),
            city(X.paris, mid, "Paris", "paris"), city(X.north, top, "Lille", "lille"), city(X.north, bottom, "Dijon", "dijon"), city(X.lyon, mid, "Lyon", "lyon"),
            city(X.grenoble, bottom, "Grenoble", "grenoble"), city(X.marseille, mid, "Marseille", "marseille"), city(X.nice, mid, "Nice", "nice"),
            skipped.has("paris-lyon") && skip(160, mid),
            skipped.has("lyon-marseille") && skip(456, mid),
        ];
    };
    const ROADS = ["225 km", "315 km", "465 km", "195 km", "110 km", "315 km", "270 km", "200 km"];
    card("four-shortest-route.svg", {
        h: 472,
        title: "A rule of the walk is a condition in your code",
        sub: "The same map and the same Dijkstra's algorithm: without tolls, the shortest route is another one",
        alt: "The map twice. With avoidTolls false, the shortest route from Paris to Marseille goes through Lyon, on two roads with a toll, 465 km and 315 km: 780 km. With avoidTolls true, the walk skips the roads from Paris to Lyon and from Lyon to Marseille, and the route goes through Dijon, Lyon and Grenoble, 315, 195, 110 and 270 km: 890 km.",
    }, [
        map(104, {
            shown: "ShortestRoute(paris, marseille, avoidTolls: false)", result: "Paris, Lyon, Marseille", length: "780 km", roads: ROADS,
            on: new Set(["paris", "lyon", "marseille", "paris-lyon", "lyon-marseille"]), skipped: new Set(),
        }),
        line(PAD, 268, RIGHT, 268, { stroke: C.grid }),
        map(284, {
            shown: "ShortestRoute(paris, marseille, avoidTolls: true)", result: "Paris, Dijon, Lyon, Grenoble, Marseille", length: "890 km", roads: ROADS,
            on: new Set(["paris", "dijon", "lyon", "grenoble", "marseille", "paris-dijon", "dijon-lyon", "lyon-grenoble", "grenoble-marseille"]), skipped: new Set(["paris-lyon", "lyon-marseille"]),
        }),
        toll(PAD, 454),
        cross(164, 450, { r: 4 }),
        text(176, 454, "a road that the condition skips", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 7, SuggestThePeopleToKnow: two steps from Ada, and the friends shared with Ada.
{
    const BOX = 110, TALL = 44, C0 = 32, C1 = 252, C2 = 472, R1 = 142, R2 = 242, Y1 = R1 + TALL / 2, Y2 = R2 + TALL / 2, ADA = (Y1 + Y2) / 2, TURN1 = C0 + BOX + 55, TURN2 = C1 + BOX + 55, HELD = { state: "held", sw: 1.6 };
    const BAR = 668, UNIT = 100;
    const person = (x, y, s, state = "alive") => node(x, y, BOX, TALL, s, null, { state });
    // A friendship that turns: out of a side, along a vertical, its chip in the middle, into a side.
    const turn = (x1, y1, x, y2, x2) => {
        const cy = (y1 + y2) / 2, s = Math.sign(y2 - y1);
        return edge([[x1, y1], [x, y1], [x, cy - s * 5]], [[x, cy + s * 5], [x, y2], [x2, y2]], [x, cy], undefined, HELD);
    };
    const rank = (y, n, names) => [bar(BAR, BAR + n * UNIT, y - 9, 14, C.blue), text(BAR + n * UNIT + 12, y + 3, n, { weight: 600, fill: C.text }), text(BAR, y + 25, names, { size: 12 })];
    card("four-people-to-know.svg", {
        h: 348,
        title: "Two steps give the friends of friends, and your code ranks them",
        sub: "A friendship has no direction: the walk reads the Others of each person",
        alt: "Ada is the friend of Bob and of Cid. The friends of Bob are Ada and Dan; those of Cid are Ada, Dan and Eve. Leaving out Ada and the friends of Ada, two people are left: Dan, who shares 2 friends with Ada, Bob and Cid, and Eve, who shares 1, Cid. PeopleToKnow(ada) gives Dan, then Eve.",
    }, [
        code(C1 + BOX / 2, 116, "ada.FriendshipEdges.Others", { anchor: "middle", fill: C.text }),
        code(C2 + BOX / 2 + 8, 116, "friend.FriendshipEdges.Others", { anchor: "middle", fill: C.text }),
        text(BAR, 116, "Friends shared with Ada", { size: 12, weight: 600, fill: C.text }),

        turn(C0 + BOX, ADA - 10, TURN1, Y1, C1),
        turn(C0 + BOX, ADA + 10, TURN1, Y2, C1),
        across(C1 + BOX, C2, Y1 - 10, undefined, { ...HELD, cx: TURN2 }),
        turn(C1 + BOX, Y2 - 10, TURN2, Y1 + 10, C2),
        across(C1 + BOX, C2, Y2 + 10, undefined, { ...HELD, cx: TURN2 }),

        person(C0, ADA - TALL / 2, "Ada"),
        person(C1, R1, "Bob"), person(C1, R2, "Cid"),
        person(C2, R1, "Dan", "held"), person(C2, R2, "Eve", "held"),

        rank(Y1, 2, "Bob, Cid"),
        rank(Y2, 1, "Cid"),

        code(BAR, 330, "PeopleToKnow(ada)"), text(RIGHT, 330, "Dan, Eve", { size: 12, weight: 600, fill: C.text, anchor: "end" }),
        text(PAD, 330, "Ada and the friends of Ada are left out; the most friends shared come first", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 8, GiveAnEdgeARequiredLink: a membership and its role, at each instruction.
{
    const STEPS = [
        { code: ["ada.NewMembership(", "club, member)"], role: "member", say: "The role is retained" },
        { code: ["member.Remove()"], role: "member", refused: true, say: "Refused" },
        { code: ["TryGetOrNew(club,", "admin, out found)"], role: "member", say: "The membership, and false" },
        { code: ["found.Role = admin"], role: "admin", say: "The role changes" },
        { code: ["member.Remove()"], role: "admin", gone: true, say: "Removed" },
    ];
    const COL = 168, GAP = 14.5, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, TOP = 150, EDGE = 214, ROLE = 268, TALL = 32;
    card("four-required-link.svg", {
        h: 372,
        title: "A membership retains its role, until it is given another",
        sub: "Ada in the chess club: the role is a required link of the edge, given to its birth",
        alt: "Five steps. ada.NewMembership(club, member) makes the membership of Ada in the chess club, which retains the role Member by a strong reference. member.Remove() is refused. TryGetOrNew(club, admin, out found) compares the role: it gives the membership, and false. found.Role = admin moves the strong reference to the role Admin. member.Remove() passes: the role Member is free.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => {
            const x = x0(i), left = x + 50, right = x + 118, cx = mid(i), onMember = s.role === "member", red = s.refused;
            return [
                s.code.map((c, k) => code(cx, 118 + k * 16, c, { anchor: "middle", fill: C.text })),
                node(x, TOP, 74, TALL, "Ada"),
                node(x + 80, TOP, 88, TALL, "Chess club"),
                tie([[left, TOP + TALL], [left, EDGE - 12]], { stroke: C.base }),
                tie([[right, TOP + TALL], [right, EDGE - 12]], { stroke: C.base }),
                chip(cx, EDGE, "Membership", { w: 100 }),
                link([[onMember ? x + 60 : x + 108, EDGE + 12], [onMember ? x + 60 : x + 108, ROLE]], { stroke: red ? C.red : C.blue, sw: 2 }),
                node(x, ROLE, 78, TALL, "Member", null, { state: s.gone ? "gone" : red ? "refused" : onMember ? "held" : "alive" }),
                node(x + COL - 78, ROLE, 78, TALL, "Admin", null, { state: onMember ? "alive" : "held" }),
                text(cx, 326, s.say, { size: 12, weight: red ? 600 : 400, fill: red ? C.red : C.text2, anchor: "middle" }),
            ];
        }),
        legendOf(354, "an edge", [STRONG]),
    ]);
}

// Lesson 8, RetainAnEdgeAndItsEnds: a certificate, its revocation, then the club.
{
    const COL = 278, GAP = 31, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, TOP = 134, ROW = 212, TALL = 40, CHIP = 92;
    const stage = (i, { certificate, ada, club, membership }) => {
        const x = x0(i), cx = x + 131, y = ROW + TALL / 2;
        return [
            node(cx - 80, TOP, 160, TALL, "Certificate", null, { state: certificate }),
            node(x, ROW, 70, TALL, "Ada", null, { state: ada }),
            node(x + COL - 86, ROW, 86, TALL, "Chess club", null, { state: club }),
            across(x + 70, x + COL - 86, y, "Membership", { w: CHIP, cx, state: membership }),
            certificate !== "gone" && link([[cx, TOP + TALL], [cx, y - 12]], { stroke: C.blue, sw: 2 }),
        ];
    };
    const refuse = (x, y, s) => [code(x + 16, y, s, { fill: C.red }), cross(x + 4, y - 4, { r: 4 })];
    card("four-edge-retained.svg", {
        h: 352,
        title: "A strong reference to an edge retains it, and its two ends",
        sub: "A certificate of membership keeps the membership, Ada and the chess club, until it is revoked",
        alt: "Three stages. A certificate designates the membership of Ada in the chess club by a strong reference: membership.Remove(), club.Remove() and ada.Remove() are each refused, and the exception of the club names the edge. After certificate.Remove(), nothing retains them. club.Remove() then removes the club, with the membership; Ada stays, and MembershipCollection.Count() is 0.",
    }, [
        steps(94, [0, 1, 2].map(mid)),
        code(mid(0), 118, "the certificate", { anchor: "middle", fill: C.text }),
        code(mid(1), 118, "certificate.Remove()", { anchor: "middle", fill: C.text }),
        code(mid(2), 118, "club.Remove()", { anchor: "middle", fill: C.text }),
        stage(0, { certificate: "alive", ada: "held", club: "held", membership: "held" }),
        stage(1, { certificate: "gone", ada: "alive", club: "alive", membership: "alive" }),
        stage(2, { certificate: "gone", ada: "alive", club: "gone", membership: "gone" }),
        refuse(x0(0), 272, "membership.Remove()"),
        refuse(x0(0), 292, "club.Remove()"),
        refuse(x0(0), 312, "ada.Remove()"),
        text(mid(1), 272, "Nothing retains them now", { size: 12, anchor: "middle" }),
        text(mid(2), 272, "The club goes, with the membership", { size: 12, anchor: "middle" }),
        reads(x0(2), 294, COL, [["MembershipCollection.Count()", "0"]]),
        legendOf(336, "an edge", [STRONG]),
        text(RIGHT, 336, "The exception of club.Remove() names the edge", { size: 11, fill: C.muted, anchor: "end" }),
    ]);
}

// Lesson 9: an edge and its end, then the same pair, its retry running the same code.
{
    const W1 = 330, W2 = 430, C2 = 552, C1 = 660, FROM = 250;
    const lane = (y, name, write, xWrite, xCommit) => [
        text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }),
        line(FROM, y, xCommit, y),
        dot(xWrite, y, 4, C.text2), code(xWrite, y - 10, write, { anchor: "middle", fill: C.text }),
        code(xCommit, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    ];
    const say = (y, name, why) => [text(PAD, y, name, { weight: 600, fill: C.text }), why.map((s, k) => text(PAD, y + 20 + k * 16, s, { size: 12 }))];
    const first = 132, second = 176, f2 = 244, s2 = 288, retry = 332, RETRY = 760, END = 880;
    card("four-concurrency.svg", {
        h: 382,
        title: "An edge is an aggregate of its own",
        sub: "Two concurrent transactions, the first one committing last: the same pair conflicts, and retries",
        alt: "Two scenarios of two concurrent transactions, the second committing first. One changes the date of the friendship while the other renames Ada: two aggregates, both commit. Both give the friendship of Ada and Bob: the commit of the first conflicts. Its retry runs the same code: GetOrNew finds the friendship that the second gave, dates it, and commits. The pair has one friendship.",
    }, [
        say(130, "An edge and its end", ["The friendship and Ada:", "two aggregates"]),
        lane(first, "T1", "friendship.Since = ...", W1, C1),
        lane(second, "T2", 'ada.Name = "Ada Lovelace"', W2, C2),
        check(C2, second, { r: 6 }),
        check(C1, first, { r: 6 }),
        text(C1 + 14, first + 4, "committed", { size: 12 }),

        line(PAD, 206, RIGHT, 206, { stroke: C.grid }),

        say(242, "The same pair", ["Both give the friendship", "of Ada and Bob"]),
        lane(f2, "T1", 'Befriend("Ada", "Bob")', W1, C1),
        lane(s2, "T2", 'Befriend("Bob", "Ada")', W2, C2),
        check(C2, s2, { r: 6 }),
        link([[C2 + 14, s2 - 2], [C1 - 10, f2 + 6]], { weak: true, stroke: C.red }),
        cross(C1, f2, { r: 6 }),
        text(C1 + 14, f2 + 4, "conflict", { size: 12, weight: 600, fill: C.red }),
        link([[C1, f2 + 12], [C1, retry], [END, retry]], { head: false, stroke: C.base, sw: 1 }),
        text(C1 - 12, retry + 4, "the retry", { size: 12, fill: C.muted, anchor: "end" }),
        dot(RETRY, retry, 4, C.text2),
        code(RETRY, retry - 10, 'Befriend("Ada", "Bob")', { anchor: "middle", fill: C.text }),
        code(END, retry - 12, "Commit()", { anchor: "middle", fill: C.text }),
        check(END, retry, { r: 6 }),
        text(RIGHT, 364, "GetOrNew finds the friendship that T2 gave, and dates it", { size: 12, anchor: "end" }),
    ]);
}

// Lesson 10, RemoveAnEndOfEachRelation: the same removal, three answers.
{
    const COL = 278, GAP = 31, col = i => PAD + i * (COL + GAP), mid = i => col(i) + COL / 2, NODE = 76, TALL = 40;
    const removal = (i, s) => [code(mid(i), 252, s, { anchor: "middle", fill: C.text }), link([[mid(i), 260], [mid(i), 282]], { stroke: C.base, sw: 1.2 })];
    const outcome = (i, s, read, fill) => [text(mid(i), 392, s, { weight: 600, fill, anchor: "middle" }), code(mid(i), 412, read, { fill: C.muted, anchor: "middle" })];
    const pair = (i, y, state) => [node(col(i), y, NODE, TALL, "Ada"), node(col(i) + COL - NODE, y, NODE, TALL, "Post", null, { state })];
    const transfer = (i, y, state, stroke) => {
        const x = col(i), right = x + COL - 92;
        return [
            node(x, y, 92, 78, "Transfer", "120"),
            node(right, y, 92, 34, "savings", null, { state }),
            node(right, y + 44, 92, 34, "current"),
            link([[x + 92, y + 17], [right, y + 17]], { stroke }),
            link([[x + 92, y + 61], [right, y + 61]]),
            code(x + 139, y + 10, "From", { anchor: "middle" }),
            code(x + 139, y + 54, "To", { anchor: "middle" }),
        ];
    };
    card("four-edge-or-reference.svg", {
        h: 434,
        title: "Three relations, three answers when an end goes",
        sub: "One removal, and what the link chosen for the relation makes of it",
        alt: "Three columns. A like is a collection of weak references from the user Ada to a post: post.Remove() removes the post, and the like leaves the collection. A reaction is an edge between Ada and the post, of the kind Insightful: post.Remove() removes the post, and the reaction with it. A transfer of 120 is a Root with a strong reference to the account savings and one to the account current: savings.Remove() is refused, and nothing changes.",
    }, [
        heading(col(0), COL, "A like", "[Weak] IEntityCollection<MPost> Likes"),
        pair(0, 171, "alive"),
        link([[col(0) + NODE, 191], [col(0) + COL - NODE, 191]], { weak: true }),
        code(mid(0), 183, "Likes", { anchor: "middle" }),
        removal(0, "post.Remove()"),
        pair(0, 309, "gone"),
        link([[col(0) + NODE, 329], [col(0) + NODE + 24, 329]], { weak: true, head: false, stroke: C.muted }),
        text(col(0) + NODE + 32, 333.5, "none", { fill: C.text }),
        outcome(0, "The like leaves the collection", "ada.Likes.Count(): 0", C.text),

        heading(col(1), COL, "A reaction", "[Edge(nameof(MUser), nameof(MPost))]"),
        pair(1, 171, "alive"),
        across(col(1) + NODE, col(1) + COL - NODE, 191, "Insightful", { w: 84 }),
        removal(1, "post.Remove()"),
        pair(1, 309, "gone"),
        across(col(1) + NODE, col(1) + COL - NODE, 329, "Insightful", { w: 84, state: "gone" }),
        outcome(1, "The reaction leaves with its post", "ReactionCollection.Count(): 0", C.text),

        heading(col(2), COL, "A transfer", "[Strong] MAccount From, To"),
        transfer(2, 152, "alive", C.text2),
        removal(2, "savings.Remove()"),
        transfer(2, 290, "refused", C.red),
        outcome(2, "Refused: nothing changes", "TransferCollection.Count(): 1", C.red),
    ]);
}

// The chapter From the need to the link: its first three questions as a ladder, an answer on the side ending it; then the fourth
// one, whose three answers are the three declarations of its table.
{
    const WIDTH = 192, ANSWER = 212, TALL = 64, PITCH = 84, TOP = 118, y = i => TOP + i * PITCH, RUNGS = 3, LAST = y(RUNGS), RIGHT0 = 496, ASIDE = 28;
    const rung = (x, i, { q, side, down, a }) => [
        block(x, y(i), WIDTH, TALL, q.map(s => ({ s, size: 12, fill: C.text }))),
        link([[x + WIDTH, y(i) + TALL / 2], [x + WIDTH + ASIDE, y(i) + TALL / 2]]),
        text(x + WIDTH + 14, y(i) + TALL / 2 - 7, side, { size: 11, anchor: "middle" }),
        block(x + WIDTH + ASIDE, y(i), ANSWER, TALL, a),
        link([[x + WIDTH / 2, y(i) + TALL], [x + WIDTH / 2, y(i + 1)]]),
        text(x + WIDTH / 2 + 8, y(i) + TALL + 14, down, { size: 11 }),
    ];
    const kind = (i, a) => [link([[RIGHT0 + WIDTH, y(i) + TALL / 2], [RIGHT0 + WIDTH + ASIDE, y(i) + TALL / 2]]), block(RIGHT0 + WIDTH + ASIDE, y(i), ANSWER, TALL, a, { state: "held" })];
    const D = s => M(s, { fill: C.text });
    const DECLARED = "[Edge(nameof(MA), nameof(MB))]", QUESTION = y(RUNGS - 1) + TALL;
    card("four-choose-a-link.svg", {
        h: 476,
        title: "From the need to the link",
        sub: "Three answers make an edge; then its declaration says how many, and in which direction",
        alt: "Questions in their order. A relation that carries no value is a collection of references, [Weak] IEntityCollection<T>, with its reverse property. One that belongs to one of its ends is a composition. One that must not vanish with either end is a Root with strong references, which retain its ends. Otherwise it is an edge. Which edge? One per pair: [Edge(nameof(MA), nameof(MB))]. Several per pair: Multiple = true. No direction: Symmetric = true, between one type.",
    }, [
        text(PAD, 104, "Is it an edge?", { weight: 600, fill: C.text }),
        rung(PAD, 0, { q: ["Does the relation", "carry values?"], side: "no", down: "yes", a: [B("A collection of references"), M("[Weak] IEntityCollection<T>", { size: 11 }), { s: "and its reverse property", size: 11 }] }),
        rung(PAD, 1, { q: ["Does it belong to", "one of its ends?"], side: "yes", down: "no", a: [B("A composition"), { s: "born in its Owner, leaves with it only", size: 11 }] }),
        rung(PAD, 2, { q: ["Must it vanish", "with either end?"], side: "no", down: "yes", a: [B("A Root"), { s: "with strong references,", size: 11 }, { s: "which retain its ends", size: 11 }] }),
        block(PAD, LAST, WIDTH + ASIDE + ANSWER, TALL, [B("An edge"), { s: "a relation between two ends, removed with either", size: 11 }], { state: "held" }),

        text(RIGHT0, 104, "Which edge?", { weight: 600, fill: C.text }),
        block(RIGHT0, TOP, WIDTH, QUESTION - TOP, [{ s: "One per pair, several,", size: 12, fill: C.text }, { s: "or no direction?", size: 12, fill: C.text }]),
        kind(0, [B("One per pair"), M(DECLARED, { size: 11, fill: C.text })]),
        kind(1, [B("Several per pair"), D("Multiple = true")]),
        kind(2, [B("No direction"), D("Symmetric = true"), { s: "between one type", size: 11 }]),
        link([[PAD + WIDTH + ASIDE + ANSWER, LAST + TALL / 2], [RIGHT0 + WIDTH / 2, LAST + TALL / 2], [RIGHT0 + WIDTH / 2, QUESTION]]),

        text(PAD, 458, "A question that goes beyond one edge is your walk, by the Views, over the edges of each end", { size: 11, fill: C.muted }),
    ]);
}
