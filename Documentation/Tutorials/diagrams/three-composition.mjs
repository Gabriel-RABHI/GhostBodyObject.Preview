// The diagrams of Tutorial Three. Each one draws the very objects of a test of its lesson: run `node three-composition.mjs` after
// a change of the lesson, or of the kit. Their words stay those of the developer: components, slots, roles, systems, Records.
import { C, PAD, RIGHT, block, card, check, code, cross, dot, legend, line, link, node, owner, rect, ring, steps, text } from "./kit.mjs";

const STRONG = { mark: "strong", name: "strong reference" }, WEAK = { mark: "weak", name: "weak reference" }, PART = { mark: "part", name: "a part, in its Owner" };
const B = s => ({ s, size: 13, weight: 600, fill: C.text }), M = (s, o) => ({ s, mono: true, ...o });

// A component on its entity: a plain line, neither a reference nor a part. The entity does not know it, so nothing points.
const attach = (x1, y1, x2, y2) => line(x1, y1, x2, y2, { stroke: C.base, sw: 1.2 });

// The kit's legend, then the mark of a component.
const marks = (y, items) => {
    const x = items.reduce((at, { mark, name }) => at + (mark === "part" ? 28 : 34) + name.length * 5.8 + 22, PAD);
    return [legend(y, items), rect(x, y - 11, 9, 14, { stroke: C.base, rx: 2 }), attach(x + 9, y - 4, x + 18, y - 4), rect(x + 18, y - 11, 9, 14, { stroke: C.base, rx: 2 }), text(x + 35, y, "a component, on its entity", { size: 11, fill: C.muted })];
};

// Lesson 1, HaveOneComponentOfATypeAtMost: one slot, six instructions.
{
    const STEPS = [
        { code: "new DoorLock", say: "the slot is empty" },
        { code: "New().Level = 40", level: 40, state: "held", say: "born on its lock" },
        { code: "New()", level: 40, state: "refused", say: "refused", more: "nothing changes" },
        { code: "Remove()", level: 40, state: "gone", say: "true", more: "the battery is removed" },
        { code: "Remove()", say: "false", more: "nothing to remove" },
        { code: "New().Level = 100", level: 100, state: "held", say: "a new battery" },
    ];
    const COL = 138, GAP = 13.6, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, BOX = 118, LOCK = 136, SLOT = 200;
    card("three-one-battery.svg", {
        h: 324,
        title: "A lock has one battery at most",
        sub: "The slot front.Battery at each instruction: full, it refuses; empty again, it takes a new one",
        alt: "Six instructions on the slot of the battery of the Front door. new DoorLock: the slot is empty. New().Level = 40: a battery is born on its lock, at level 40. A second New() is refused, and nothing changes. Remove() returns true: the battery is removed. Remove() again returns false: there is nothing to remove. New().Level = 100: the slot takes a new battery.",
    }, [
        steps(94, STEPS.map((_, i) => mid(i))),
        STEPS.map((s, i) => {
            const x = mid(i) - BOX / 2, refused = s.state === "refused";
            return [
                code(mid(i), 118, s.code, { anchor: "middle", fill: C.text }),
                node(x, LOCK, BOX, 36, "Front door"),
                s.level ? [attach(mid(i), LOCK + 36, mid(i), SLOT), node(x, SLOT, BOX, 46, "Battery", `Level ${s.level}`, { state: s.state })]
                    : [link([[mid(i), LOCK + 36], [mid(i), SLOT]], { weak: true, head: false, stroke: C.muted }), text(mid(i), SLOT + 18, "none", { fill: C.text, anchor: "middle" })],
                text(mid(i), 272, s.say, { size: 12, weight: refused ? 600 : 400, fill: refused ? C.red : C.text, anchor: "middle", mono: s.say === "true" || s.say === "false" }),
                s.more && text(mid(i), 290, s.more, { size: 12, fill: C.muted, anchor: "middle" }),
            ];
        }),
    ]);
}

// Lesson 2, RemoveACapabilityWithItsParts and RemoveTheCapabilitiesWithTheirEntity: one removal in each direction.
{
    const LOCK = 104, OFF = 124, COMP = 226, TALL = 44, LOG = 74, TOP = 100, BOTTOM = 262, RIGHT0 = 578;
    const LOGY = TALL + 8, HEIGHT = LOGY + LOG;
    const scene = (x, y, { lock = "alive", battery = "alive", log = "alive", alarms, back }) => [
        node(x, y, LOCK, HEIGHT, "Front door", null, { state: lock }),
        attach(x + LOCK, y + TALL / 2, x + OFF, y + TALL / 2),
        attach(x + LOCK, y + LOGY + LOG / 2, x + OFF, y + LOGY + LOG / 2),
        node(x + OFF, y, COMP, TALL, "Battery", "Level 80", { state: battery }),
        owner(x + OFF, y + LOGY, COMP, LOG, "AlarmLog", null, { state: log }),
        alarms.map((a, k) => node(x + OFF + 12 + k * 104, y + LOGY + 32, 98, 30, a, null, { size: 12, state: log === "gone" ? "gone" : "alive" })),
        back && [
            node(x, y + HEIGHT + 14, LOCK, TALL, "Back door"),
            attach(x + LOCK, y + HEIGHT + 14 + TALL / 2, x + OFF, y + HEIGHT + 14 + TALL / 2),
            node(x + OFF, y + HEIGHT + 14, COMP, TALL, "Battery", "Level 60"),
        ],
    ];
    const gutter = (y, instruction, stays, more) => [
        code(480, y - 10, instruction, { anchor: "middle", fill: C.text }),
        link([[394, y], [566, y]], { stroke: C.base, sw: 1.2 }),
        text(480, y + 24, stays, { weight: 600, fill: C.text, anchor: "middle" }),
        text(480, y + 42, more, { size: 12, anchor: "middle" }),
    ];
    card("three-what-leaves.svg", {
        h: 494,
        title: "A capability never outlives its entity, and never ends it",
        sub: "Two removals: the target in blue, and what leaves with it",
        alt: "Two removals. The Front door has a battery at level 80 and an alarm log of two alarms. front.AlarmLog.Remove() removes the log with its alarms; the lock stays, with its battery. In the second one, the Front door has a battery and an alarm log of one alarm, and the Back door a battery at level 60. front.Remove() removes the lock with its battery, its log and its alarm; the Back door and its battery stay.",
    }, [
        scene(PAD, TOP, { log: "held", alarms: ["Forced", "Left open"] }),
        gutter(TOP + HEIGHT / 2, "front.AlarmLog.Remove()", "The lock stays", "with its other capabilities"),
        scene(RIGHT0, TOP, { log: "gone", alarms: ["Forced", "Left open"] }),
        line(PAD, 244, RIGHT, 244, { stroke: C.grid }),
        scene(PAD, BOTTOM, { lock: "held", alarms: ["Forced"], back: true }),
        gutter(BOTTOM + HEIGHT / 2, "front.Remove()", "Its capabilities go with it", "and their parts"),
        scene(RIGHT0, BOTTOM, { lock: "gone", battery: "gone", log: "gone", alarms: ["Forced"], back: true }),
        marks(476, [PART]),
    ]);
}

// Lesson 3, RetainACapabilityAndItsEntity and ForgetARemovedCapability: a work order, a link of each nature to a capability.
{
    const COL = 416, x0 = i => PAD + i * (COL + 64), ORDER = 120, ARROW = 92, COMP = 104, STUB = 16, LOCK = 84, TALL = 46;
    const heading = (i, name, decl) => [text(x0(i), 108, name, { weight: 600, fill: C.text }), code(x0(i), 127, decl), line(x0(i), 140, x0(i) + COL, 140, { stroke: C.grid })];
    const chain = (i, y, order, comp, { weak = false, compState = "alive", lockState = "alive", stroke = C.text2, none = false } = {}) => {
        const x = x0(i), cx = x + ORDER + ARROW, lx = cx + COMP + STUB, cy = y + TALL / 2;
        return [
            node(x, y, ORDER, TALL, "Work order", order),
            none ? [link([[x + ORDER, cy], [x + ORDER + 24, cy]], { weak: true, head: false, stroke: C.muted }), text(x + ORDER + 32, cy + 4.5, "none", { fill: C.text })]
                : [link([[x + ORDER, cy], [cx, cy]], { weak, stroke }), code(x + ORDER + ARROW / 2, cy - 8, comp, { anchor: "middle" })],
            node(cx, y, COMP, TALL, comp, null, { state: compState }),
            attach(cx + COMP, cy, lx, cy),
            node(lx, y, LOCK, TALL, "Front door", null, { state: lockState }),
        ];
    };
    const removal = (i, lines) => {
        const center = x0(i) + COL / 2;
        return [lines.map((s, k) => code(center, 246 + k * 16 + (2 - lines.length) * 8, s, { anchor: "middle", fill: C.text })), link([[center, 268], [center, 288]], { stroke: C.base, sw: 1.2 })];
    };
    card("three-strong-weak.svg", {
        h: 440,
        title: "A strong reference keeps a capability and its entity; a weak one forgets it",
        sub: "Two work orders, each designating a capability of the Front door, and the removal of that capability",
        alt: "Two columns. A work order, Fix the bolt, designates the maintenance of the Front door by a strong reference: front.Maintenance.Remove() and front.Remove() are both refused, and nothing changes; once the order is removed, the lock can go, with its maintenance. A work order, Replace the battery, designates the battery of the Front door by a weak reference: front.Battery.Remove() removes the battery, the lock stays, and the link of the order becomes none.",
    }, [
        heading(0, "A strong reference", "[Strong] Option<CMaintenance> Maintenance"),
        chain(0, 160, "Fix the bolt", "Maintenance"),
        removal(0, ["front.Maintenance.Remove()", "front.Remove()"]),
        chain(0, 298, "Fix the bolt", "Maintenance", { compState: "refused", lockState: "refused", stroke: C.red }),
        text(x0(0) + COL / 2, 372, "Both refused: nothing changes", { weight: 600, fill: C.red, anchor: "middle" }),
        text(x0(0) + COL / 2, 392, "Once the order is removed, the lock can go, with its maintenance", { size: 12, anchor: "middle" }),

        heading(1, "A weak reference", "[Weak] Option<CBattery> Battery"),
        chain(1, 160, "Replace the battery", "Battery", { weak: true }),
        removal(1, ["front.Battery.Remove()"]),
        chain(1, 298, "Replace the battery", "Battery", { none: true, compState: "gone" }),
        text(x0(1) + COL / 2, 372, "The link becomes none", { weight: 600, fill: C.text, anchor: "middle" }),
        text(x0(1) + COL / 2, 392, "The work order forgets the battery; the lock stays", { size: 12, anchor: "middle" }),
        marks(422, [STRONG, WEAK]),
    ]);
}

// Lesson 4, ShareACapabilityBetweenTypes: one component on two types, one collection.
{
    const ROWS = [
        { name: "Hall camera", type: "a Camera", level: 30, camera: "Hall camera" },
        { name: "Front door", type: "a DoorLock", level: 90, door: "Front door" },
        { name: "Office thermostat", type: "a Thermostat" },
    ];
    const Y0 = 136, PITCH = 58, TALL = 44, DEVICE = 170, BOX = 276, COMP = 190, TABLE = 512, COLS = [524, 730], y = k => Y0 + k * PITCH;
    const option = (x, cy, name) => name ? [dot(x, cy, 6, C.blue), text(x + 14, cy + 4, name, { fill: C.text })] : [ring(x, cy, 6, C.base), text(x + 14, cy + 4, "none", { fill: C.muted })];
    card("three-one-collection.svg", {
        h: 338,
        title: "One collection holds every battery, whatever its device",
        sub: "[ComponentFor] repeated: one component on two types, without a base class",
        alt: "Three devices: the Hall camera, a Camera, has a battery at level 30; the Front door, a DoorLock, has a battery at level 90; the Office thermostat, a Thermostat, has none. BatteryCollection holds the two batteries, and its Count() is 2. Each battery reads its device as an Option of each type, one of them set: the first one reads battery.Camera, the Hall camera, and battery.DoorLock, none; the second one reads none and the Front door.",
    }, [
        owner(BOX - 14, 100, COMP + 28, y(1) + TALL + 14 - 100, "BatteryCollection", "Count(): 2", { state: "held" }),
        ROWS.map((r, k) => [
            node(PAD, y(k), DEVICE, TALL, r.name, r.type),
            r.level ? [attach(PAD + DEVICE, y(k) + TALL / 2, BOX, y(k) + TALL / 2), node(BOX, y(k), COMP, TALL, "Battery", `Level ${r.level}`, { state: "held" })]
                : [link([[PAD + DEVICE, y(k) + TALL / 2], [PAD + DEVICE + 24, y(k) + TALL / 2]], { weak: true, head: false, stroke: C.muted }), text(PAD + DEVICE + 32, y(k) + TALL / 2 + 4.5, "none", { fill: C.text })],
        ]),
        code(COLS[0] - 6, 122, "battery.Camera", { fill: C.text }),
        code(COLS[1] - 6, 122, "battery.DoorLock", { fill: C.text }),
        line(TABLE, 132, RIGHT, 132, { stroke: C.base }),
        ROWS.slice(0, 2).map((r, k) => [option(COLS[0], y(k) + TALL / 2, r.camera), option(COLS[1], y(k) + TALL / 2, r.door)]),
        line(TABLE, y(1) - 7, RIGHT, y(1) - 7, { stroke: C.grid }),
        line(TABLE, y(1) + TALL + 7, RIGHT, y(1) + TALL + 7, { stroke: C.grid }),
        text(TABLE, y(2) + TALL / 2 + 4, "Each battery reads its device: an Option of each type, one of them set", { size: 12 }),
        marks(320, []),
    ]);
}

// Lesson 6, EnumerateTheEntitiesThatHaveTheCapabilities: four locks, and the system that holds two of them.
{
    const COLS = [{ x: 296, name: "Battery", kind: "required" }, { x: 452, name: "Location", kind: "optional" }, { x: 608, name: "Maintenance", kind: "excluded" }];
    const ROWS = [
        { name: "Front door", cells: ["Level 15", "Floor 0", null], member: true },
        { name: "Office door", cells: ["Level 70", null, null], member: true },
        { name: "Lab door", cells: ["Level 5", null, "Jammed bolt"], why: "under maintenance" },
        { name: "Server room door", cells: [null, null, null], why: "wired" },
    ];
    const HEAD = 150, ROW = 44, y = k => HEAD + 26 + k * ROW, MEMBER = 714;
    card("three-powered-lock.svg", {
        h: 384,
        title: "A system joins each lock and its capabilities, and keeps out the others",
        sub: "SPoweredLock: the locks with a Battery, maybe a Location, never under Maintenance",
        alt: "Four locks. The Front door has a battery at level 15 and a location on floor 0: it is in the system. The Office door has a battery at level 70 and no location, an optional capability: it is in the system. The Lab door has a battery at level 5 and a maintenance, Jammed bolt, an excluded capability: it is out. The Server room door is wired: without the required battery, it is out.",
    }, [
        COLS.map(c => [text(c.x, 118, c.name, { size: 12, weight: 600, fill: C.text, anchor: "middle" }), text(c.x, 134, c.kind, { size: 11, fill: C.muted, anchor: "middle" })]),
        code(MEMBER, 118, "PoweredLockSystemCollection", { fill: C.text }),
        line(PAD, HEAD, RIGHT, HEAD, { stroke: C.base }),
        ROWS.map((r, k) => [
            text(PAD, y(k) + 4, r.name, { fill: C.text }),
            r.cells.map((value, i) => {
                const { x, kind } = COLS[i], excluded = kind === "excluded";
                return value ? [rect(x - 54, y(k) - 15, 108, 30, { fill: excluded ? C.redSoft : C.blueSoft, rx: 6 }), text(x, y(k) + 4, value, { size: 12, fill: C.text, anchor: "middle" })] : ring(x, y(k), 6, C.base);
            }),
            r.member ? check(MEMBER + 6, y(k), { r: 6 }) : [line(MEMBER + 1, y(k), MEMBER + 11, y(k), { stroke: C.muted, sw: 2 }), text(MEMBER + 26, y(k) + 4, r.why, { size: 12 })],
            k < ROWS.length - 1 && line(PAD, y(k) + 22, RIGHT, y(k) + 22, { stroke: C.grid }),
        ]),
        text(PAD, 366, "A ring: the lock has none. The members of a system are Views: it reads many entities, and creates no Body", { size: 11, fill: C.muted }),
    ]);
}

// Lesson 8, GainAndLoseRoles, and the opening of the lesson: the same three stages, in a class per combination and in one entity.
{
    const LEFT = 196, PANEL = 236, GAP = 12, px = i => LEFT + i * (PANEL + GAP), mid = i => px(i) + PANEL / 2, SUPPLIER = 130, CUSTOMER = 96, ACME = 226, ROLE = 280, TALL = 72;
    const STAGES = ["A supplier", "It buys from us as well", "It stops supplying"];
    const label = (y, name, say) => [text(PAD, y, name, { weight: 600, fill: C.text }), say.map((s, k) => text(PAD, y + 18 + k * 16, s, { size: 12 }))];
    const supplier = (i, state) => [
        attach(px(i) + SUPPLIER / 2, ACME + 36, px(i) + SUPPLIER / 2, ROLE),
        owner(px(i), ROLE, SUPPLIER, TALL, "Supplier", null, { state }),
        node(px(i) + 12, ROLE + 32, SUPPLIER - 24, 30, "Steel", null, { size: 12, state }),
    ];
    const customer = i => [attach(px(i) + 140 + CUSTOMER / 2, ACME + 36, px(i) + 140 + CUSTOMER / 2, ROLE), node(px(i) + 140, ROLE, CUSTOMER, TALL, "Customer", "50,000")];
    card("three-roles.svg", {
        h: 414,
        title: "A role comes and goes; the party stays",
        sub: "The same three stages of Acme: with inheritance, a class at each one; with components, one entity",
        alt: "Three stages of Acme: a supplier; a supplier that buys from us as well; a customer that stops supplying. With inheritance, Acme is a class Supplier : Party, then needs a third class, or two objects for one party, then changes of class, so of identity, to a class Customer : Party. With components, Acme is one entity, with the same identity and the same links all along: acme.Supplier.New() gives it the role of supplier, with its offer Steel; acme.Customer.New() a second role, with a credit limit of 50,000; acme.Supplier.Remove() takes the first role away, with its offer.",
    }, [
        steps(94, STAGES.map((_, i) => mid(i))),
        STAGES.map((s, i) => text(mid(i), 118, s, { weight: 600, fill: C.text, anchor: "middle" })),
        label(150, "With inheritance", ["Each combination of roles", "is a class"]),
        [M("class Supplier : Party", { size: 11 }), { s: "a third class, or two objects", size: 11 }, M("class Customer : Party", { size: 11 })].map((sub, i) => block(px(i) + 16, 134, 204, 58, [B("Acme"), sub])),
        text(mid(2), 208, "a change of class, so of identity", { size: 11, fill: C.muted, anchor: "middle" }),
        line(PAD, 218, RIGHT, 218, { stroke: C.grid }),
        label(250, "With components", ["One entity, whose roles", "are its capabilities"]),
        node(LEFT, ACME, RIGHT - LEFT, 36, "Acme: one party, the same identity and the same links", null, { state: "held" }),
        supplier(0, "alive"),
        supplier(1, "alive"), customer(1),
        supplier(2, "gone"), customer(2),
        code(mid(0), 374, "acme.Supplier.New()", { anchor: "middle", fill: C.text }),
        code(mid(1), 374, "acme.Customer.New()", { anchor: "middle", fill: C.text }),
        code(mid(2), 374, "acme.Supplier.Remove()", { anchor: "middle", fill: C.text }),
        marks(396, [PART]),
    ]);
}

// Lesson 9, ChangeARoleAndItsPartyConcurrently and ConflictOnTheCapabilitiesOfOneEntity: two transactions, the first one committing last.
{
    const AT = [330, 430, 560, 680], FROM = 260, RETRY = [782, 890];
    const lane = (y, name, write, xWrite, xCommit) => [
        text(FROM - 12, y + 4, name, { size: 12, fill: C.muted, anchor: "end" }),
        line(FROM, y, xCommit, y),
        dot(xWrite, y, 4, C.text2), code(xWrite, y - 10, write, { anchor: "middle", fill: C.text }),
        code(xCommit, y - 12, "Commit()", { anchor: "middle", fill: C.text }),
    ];
    const head = (y0, name, why) => [text(PAD, y0 + 34, name, { weight: 600, fill: C.text }), why.map((s, k) => text(PAD, y0 + 54 + k * 16, s, { size: 12 }))];
    const Y1 = 96, Y2 = 208, F1 = Y1 + 36, S1 = Y1 + 80, F2 = Y2 + 36, S2 = Y2 + 80, R2 = Y2 + 124;
    card("three-concurrency.svg", {
        h: 382,
        title: "A role changes in parallel with its party, until it is given or taken",
        sub: "Two concurrent transactions, the first one committing last",
        alt: "Two scenarios of two concurrent transactions, the second committing first. The sales team sets the credit limit of the customer role to 25,000, the legal team renames the party Acme Corporation: two aggregates, both commit, and both values stay. In the second one, both transactions give the party its customer role, by GrantCredit(5_000m) and GrantCredit(8_000m): the second commits, the first conflicts. The retry runs the same code: GetOrNew finds the role of 8,000 that the other transaction gave, changes it to 5,000, and commits.",
    }, [
        head(Y1, "A role and its party", ["Two aggregates: each one", "conflicts only with itself"]),
        lane(F1, "sales", "CreditLimit = 25_000m", AT[0], AT[3]),
        lane(S1, "legal", "Name = \"Acme Corporation\"", AT[1], AT[2]),
        check(AT[2], S1, { r: 6 }), check(AT[3], F1, { r: 6 }),
        text(AT[3] + 14, F1 + 4, "committed", { size: 12 }),
        text(AT[3] + 14, F1 + 20, "both values stay", { size: 11, fill: C.muted }),
        line(PAD, Y2 - 2, RIGHT, Y2 - 2, { stroke: C.grid }),
        head(Y2, "A role given twice", ["Giving a capability changes", "the capabilities of its entity"]),
        lane(F2, "first", "GrantCredit(5_000m)", AT[0], AT[3]),
        lane(S2, "second", "GrantCredit(8_000m)", AT[1], AT[2]),
        check(AT[2], S2, { r: 6 }),
        link([[AT[2] + 32, S2 - 6], [AT[3] - 10, F2 + 6]], { weak: true, stroke: C.red }),
        cross(AT[3], F2, { r: 6 }),
        text(AT[3] + 14, F2 + 4, "conflict", { size: 12, weight: 600, fill: C.red }),
        link([[AT[3], F2 + 12], [AT[3], R2], [RETRY[1], R2]], { head: false, stroke: C.base, sw: 1 }),
        text(AT[3] - 12, R2 + 4, "retry", { size: 12, fill: C.muted, anchor: "end" }),
        dot(RETRY[0], R2, 4, C.text2), code(RETRY[0], R2 - 10, "GrantCredit(5_000m)", { anchor: "middle", fill: C.text }),
        code(RETRY[1], R2 - 12, "Commit()", { anchor: "middle", fill: C.text }),
        check(RETRY[1], R2, { r: 6 }),
        text(RIGHT, R2 + 24, "The same code: GetOrNew finds the role of 8,000, and changes it", { size: 12, anchor: "end" }),
    ]);
}

// Lesson 10, the model of Lesson 8: what a party is made of, and what each system asks of its components.
{
    const ENTITY = 100, OFF = 164, COMP = 130, ROWS = [{ y: 136, h: 46 }, { y: 194, h: 88 }, { y: 294, h: 46 }, { y: 352, h: 46 }], Y = ROWS.map(r => r.y + r.h / 2), COLS = [566, 750], LAST = ROWS.at(-1).y + ROWS.at(-1).h;
    const SYSTEMS = [["required", "required"], ["required", null], [null, "optional"], [null, "excluded"]];
    const cell = (x, y, kind) => kind && [kind === "optional" ? ring(x + 5, y, 5, C.text2) : dot(x + 5, y, 5, kind === "excluded" ? C.red : C.blue), text(x + 18, y + 4, kind, { size: 12, fill: kind === "excluded" ? C.red : C.text })];
    card("three-party-model.svg", {
        h: 450,
        title: "What composes a party: components, parts, references and systems",
        sub: "The components of a party on the left; what each of its systems asks of them on the right",
        alt: "The model of Lessons 8 to 10. A party, a Root, has four components: a customer, a supplier, an employee and a block. The offers are parts of the supplier. An order, a Root, retains the customer by a strong reference. The system STradingPartner, the customers and suppliers at once, requires the customer and the supplier. The system SActiveCustomer, the customers not blocked, requires the customer, reads the employee as optional, and excludes the block.",
    }, [
        node(PAD, ROWS[0].y, ENTITY, LAST - ROWS[0].y, "Party", "[Root, Removable]"),
        Y.map(y => attach(PAD + ENTITY, y, OFF, y)),
        node(OFF, ROWS[0].y, COMP, ROWS[0].h, "Customer", "CreditLimit"),
        node(384, ROWS[0].y, 120, ROWS[0].h, "Order", "[Root, Removable]"),
        link([[384, Y[0]], [OFF + COMP, Y[0]]]),
        code(339, Y[0] - 8, "Customer", { anchor: "middle" }),
        owner(OFF, ROWS[1].y, 340, ROWS[1].h, "Supplier", "PaymentDays"),
        node(OFF + 12, ROWS[1].y + 34, 316, 44, "Offer", "a part of its role"),
        node(OFF, ROWS[2].y, COMP, ROWS[2].h, "Employee", "Badge"),
        node(OFF, ROWS[3].y, COMP, ROWS[3].h, "Blocked", "Reason"),
        code(COLS[0], 104, "STradingPartner", { fill: C.text }),
        text(COLS[0], 120, "customers and suppliers at once", { size: 11, fill: C.muted }),
        code(COLS[1], 104, "SActiveCustomer", { fill: C.text }),
        text(COLS[1], 120, "the customers not blocked", { size: 11, fill: C.muted }),
        line(OFF, 128, RIGHT, 128, { stroke: C.base }),
        ROWS.slice(1).map(r => line(OFF, r.y - 6, RIGHT, r.y - 6, { stroke: C.grid })),
        Y.map((y, k) => [cell(COLS[0], y, SYSTEMS[k][0]), cell(COLS[1], y, SYSTEMS[k][1])]),
        marks(432, [STRONG, PART]),
    ]);
}

// Lesson 10, RemoveWhatTheDeclarationsAllow: the removals of the test, in their order.
{
    const COL = 278, GAP = 31, x0 = i => PAD + i * (COL + GAP), mid = i => x0(i) + COL / 2, TOP = 150;
    const stage = (i, { acme = "alive", customer = "alive", order = "alive", supplier = "alive", red = false }) => {
        const x = x0(i), cx = x + 90;
        return [
            node(x, TOP, 72, 134, "Acme", null, { state: acme }),
            attach(x + 72, TOP + 22, cx, TOP + 22),
            attach(x + 72, TOP + 95, cx, TOP + 95),
            node(cx, TOP, 92, 44, "Customer", null, { state: customer }),
            node(x + 212, TOP, 66, 44, "Order", "1,200", { state: order }),
            order !== "gone" && link([[x + 212, TOP + 22], [cx + 92, TOP + 22]], { stroke: red ? C.red : C.text2 }),
            owner(cx, TOP + 56, 188, 78, "Supplier", null, { state: supplier }),
            node(cx + 12, TOP + 88, 78, 34, "Steel", null, { size: 12, state: supplier }),
            node(cx + 98, TOP + 88, 78, 34, "Copper", null, { size: 12, state: supplier }),
        ];
    };
    const instruction = (i, lines, fill) => lines.map((s, k) => code(mid(i), 118 + k * 16, s, { anchor: "middle", fill }));
    const outcome = (i, s, more, fill) => [text(mid(i), 308, s, { weight: 600, fill, anchor: "middle" }), text(mid(i), 327, more, { size: 12, anchor: "middle" })];
    card("three-removal.svg", {
        h: 372,
        title: "What a removal does follows from the declarations",
        sub: "A party that supplies two offers, and that placed an order: the removals of the test, in their order",
        alt: "Three stages. Acme has a customer role, retained by an order of 1,200, and a supplier role with its offers Steel and Copper. acme.Supplier.Remove() removes the role with its offers: a part follows its role. acme.Customer.Remove() and acme.Remove() are refused: the strong reference of the order retains the role and its party, and nothing changes. order.Remove() then acme.Remove(): the order closed, the role and the party go.",
    }, [
        steps(94, [0, 1, 2].map(mid)),
        instruction(0, ["acme.Supplier.Remove()"], C.text),
        instruction(1, ["acme.Customer.Remove()", "acme.Remove()"], C.red),
        instruction(2, ["order.Remove()", "acme.Remove()"], C.text),
        stage(0, { supplier: "gone" }),
        stage(1, { supplier: "gone", customer: "refused", acme: "refused", red: true }),
        stage(2, { supplier: "gone", customer: "gone", acme: "gone", order: "gone" }),
        outcome(0, "A part follows its role", "The customer role and the party stay", C.text),
        outcome(1, "Refused: nothing changes", "A strong reference retains the role and its party", C.red),
        outcome(2, "The role and the party go", "The order closed, nothing retains them", C.text),
        marks(354, [STRONG, PART]),
    ]);
}

// Lesson 11, AnAddressIsAValue: four addresses, an instruction per line, in the order of the second transaction.
{
    const RIVOLI = ["1 rue de Rivoli", "Paris 75001"], LYON = ["1 rue de Rivoli", "Lyon 69001"], QUAI = ["2 quai Saint-Antoine", "Lyon 69001"];
    const LANES = ["before", "ada.Home", "home", "ada.Billing"];
    const ROWS = [
        { code: ["before = ada.Home"], cells: [RIVOLI, RIVOLI, null, "none"], changed: 0, from: 1 },
        { code: ["home = ada.Home.ToBody()"], cells: [RIVOLI, RIVOLI, RIVOLI, "none"], changed: 2, from: 1 },
        { code: ["home.City = \"Lyon\"", "home.PostalCode = \"69001\""], cells: [RIVOLI, RIVOLI, LYON, "none"], changed: 2 },
        { code: ["ada.Home = home"], cells: [RIVOLI, LYON, LYON, "none"], changed: 1, from: 2 },
        { code: ["home.Street = \"2 quai Saint-Antoine\""], cells: [RIVOLI, LYON, QUAI, "none"], changed: 2 },
        { code: ["ada.Billing = home"], cells: [RIVOLI, LYON, QUAI, QUAI], changed: 3, from: 2 },
    ];
    const X = 300, BOX = 130, GAP = 36, lx = i => X + i * (BOX + GAP), TOP = 134, PITCH = 52, TALL = 42, y = k => TOP + k * PITCH;
    card("three-address-value.svg", {
        h: 462,
        title: "An address is a value: each assignment copies it",
        sub: "One instruction per line: the blue address has just changed, and an arrow carries the values",
        alt: "Six instructions. before reads ada.Home: 1 rue de Rivoli, Paris 75001. home is a copy, by ToBody(). home.City and home.PostalCode change the copy: Lyon 69001; ada.Home and before do not change. ada.Home = home copies the values into the client: ada.Home is now in Lyon, and before stays in Paris. home.Street changes the copy again: ada.Home keeps 1 rue de Rivoli. ada.Billing = home copies the values of the copy into the billing address, none until then.",
    }, [
        LANES.map((s, i) => code(lx(i) + BOX / 2, 116, s, { anchor: "middle", fill: C.text })),
        line(PAD, 124, RIGHT, 124, { stroke: C.base }),
        ROWS.map((r, k) => {
            const cy = y(k) + TALL / 2 + 1;
            return [
                r.code.map((s, n) => code(PAD, cy + 4 + (n - (r.code.length - 1) / 2) * 16, s, { fill: C.text })),
                r.cells.map((c, i) => c === "none" ? text(lx(i) + BOX / 2, cy + 4, "none", { size: 12, fill: C.muted, anchor: "middle" })
                    : c && block(lx(i), y(k) + 1, BOX, TALL, c.map(s => ({ s, size: 11, fill: i === r.changed ? C.text : C.text2 })), { state: i === r.changed ? "held" : "alive" })),
                r.from !== undefined && link(r.from < r.changed ? [[lx(r.from) + BOX, cy], [lx(r.changed), cy]] : [[lx(r.from), cy], [lx(r.changed) + BOX, cy]]),
            ];
        }),
    ]);
}

// Lesson 11, CarryMessagesForSeveralServices: one bag, three readers.
{
    const X = 334, BOX = 190, GAP = 12, cx = i => X + i * (BOX + GAP), TALL = 40, TOP = 108, PITCH = 68, y = k => TOP + PITCH + 8 + k * PITCH;
    const INBOX = [["SendBrochure", "Winter offers"], ["CallBack", "Damaged parcel"], ["SendBrochure", "New depots"]];
    const ROWS = [
        { who: "The mailing service knows one type", code: ["Enumerate<SendBrochureView>()"], cells: [["Winter offers", "held"], ["skipped"], ["New depots", "held"]] },
        { who: "A service that knows both", code: ["Enumerate(work, …, …)"], say: "one walk, a handler per type", cells: [["mail Winter offers", "held"], ["call Damaged parcel", "held"], ["mail New depots", "held"]] },
        { who: "The call center takes its message", code: ["IndexOf<CallBackView>(out at)", "RemoveAt(at)"], cells: [["Winter offers", "alive"], ["Damaged parcel", "gone"], ["New depots", "alive"]] },
    ];
    card("three-inbox.svg", {
        h: 396,
        title: "Each service reads the types it knows, and leaves the others",
        sub: "ada.Inbox, a bag of any Record: three messages for two services, in the order of their Add",
        alt: "The inbox of Ada holds three Records in the order of their Add: a SendBrochure, Winter offers; a CallBack, Damaged parcel; a SendBrochure, New depots. The mailing service reads Enumerate<SendBrochureView>(): the two brochures, the other message skipped. A service that knows both walks the inbox once, a handler per type: mail Winter offers, call Damaged parcel, mail New depots. The call center finds its message by IndexOf<CallBackView> and removes it by RemoveAt: the two brochures stay in their order.",
    }, [
        text(PAD, TOP + 16, "ada.Inbox", { size: 13, weight: 600, fill: C.text, mono: true }),
        text(PAD, TOP + 34, "RecordBag: any Record of the model", { size: 12 }),
        INBOX.map(([type, value], i) => node(cx(i), TOP, BOX, 46, type, value)),
        ROWS.map((r, k) => [
            line(PAD, y(k) - 14, RIGHT, y(k) - 14, { stroke: C.grid }),
            text(PAD, y(k) + 8, r.who, { weight: 600, fill: C.text }),
            r.code.map((s, n) => code(PAD, y(k) + 26 + n * 16, s)),
            r.say && text(PAD, y(k) + 42, r.say, { size: 12 }),
            r.cells.map(([value, state], i) => state ? node(cx(i), y(k), BOX, TALL, value, null, { size: 12, state }) : text(cx(i) + BOX / 2, y(k) + TALL / 2 + 4, value, { size: 12, fill: C.muted, anchor: "middle" })),
        ]),
    ]);
}
