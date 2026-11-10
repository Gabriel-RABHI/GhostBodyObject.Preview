// The drawing kit of the tutorial diagrams: the card, the palette and the type of the charts of the README, and the marks of a
// model, one meaning each. A strong reference is a solid arrow, a weak one a dashed arrow; a part is a box in its Owner.
import { writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

// The palette of Documentation/Benchmarks/charts. Red and amber carry a state, and nothing else does.
export const C = {
    surface: "#1a252b", raised: "#233139", grid: "#2b3a42", base: "#4a5a62",
    text: "#e8eef1", text2: "#aebcc4", muted: "#7f9199",
    blue: "#3987e5", blueSoft: "#1f3a55", grey: "#66757d",
    red: "#f07a78", redSoft: "#47292d", amber: "#e4a93c", amberSoft: "#46391f",
};
const SANS = "'Segoe UI', -apple-system, BlinkMacSystemFont, 'Helvetica Neue', Arial, sans-serif";
const MONO = "'Cascadia Mono', Consolas, Menlo, 'DejaVu Sans Mono', monospace";

export const W = 960, PAD = 32, RIGHT = W - PAD;

const esc = s => String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
const r1 = v => Math.round(v * 10) / 10;

export const text = (x, y, s, { size = 13, fill = C.text2, weight = 400, anchor = "start", mono = false } = {}) =>
    `<text x="${r1(x)}" y="${r1(y)}" font-size="${size}" font-weight="${weight}" fill="${fill}" text-anchor="${anchor}"${mono ? ` font-family="${MONO}"` : ""}>${esc(s)}</text>`;
export const code = (x, y, s, o = {}) => text(x, y, s, { size: 12, mono: true, ...o });

export const rect = (x, y, w, h, { fill = "none", stroke = "none", sw = 1, rx = 8, dash } = {}) =>
    `<rect x="${r1(x)}" y="${r1(y)}" width="${r1(w)}" height="${r1(h)}" rx="${rx}" fill="${fill}"${stroke === "none" ? "" : ` stroke="${stroke}" stroke-width="${sw}"`}${dash ? ` stroke-dasharray="${dash}"` : ""}/>`;
export const line = (x1, y1, x2, y2, { stroke = C.base, sw = 1, dash } = {}) =>
    `<line x1="${r1(x1)}" y1="${r1(y1)}" x2="${r1(x2)}" y2="${r1(y2)}" stroke="${stroke}" stroke-width="${sw}"${dash ? ` stroke-dasharray="${dash}"` : ""}/>`;

// The states of an entity. A removed one stays drawn, dashed and dim: the picture shows what left.
const STATES = {
    alive: { fill: C.raised, stroke: C.base, sw: 1, name: C.text, sub: C.muted },
    held: { fill: C.blueSoft, stroke: C.blue, sw: 1.5, name: C.text, sub: C.text2 },
    orphan: { fill: C.amberSoft, stroke: C.amber, sw: 1.5, name: C.text, sub: C.text2 },
    refused: { fill: C.redSoft, stroke: C.red, sw: 1.5, name: C.text, sub: C.text2 },
    gone: { fill: "none", stroke: C.base, sw: 1, name: C.muted, sub: C.muted, dash: "4 4" },
};

// An entity: a box with its name, and a second line when there is one.
export const node = (x, y, w, h, name, sub, { state = "alive", size = 13, mono = false } = {}) => {
    const s = STATES[state], cx = x + w / 2, cy = y + h / 2;
    return rect(x, y, w, h, { fill: s.fill, stroke: s.stroke, sw: s.sw, dash: s.dash })
        + (sub ? text(cx, cy - 3, name, { size, weight: 600, fill: s.name, anchor: "middle", mono }) + text(cx, cy + 13, sub, { size: 11, fill: s.sub, anchor: "middle" })
            : text(cx, cy + size * 0.36, name, { size, weight: 600, fill: s.name, anchor: "middle", mono }));
};

// A box of several lines: each one a string, or the options of its text.
export const block = (x, y, w, h, lines, { state = "alive", border = true } = {}) => {
    const s = STATES[state], gap = 17, top = y + h / 2 - (lines.length - 1) * gap / 2 + 4;
    return (border ? rect(x, y, w, h, { fill: s.fill, stroke: s.stroke, sw: s.sw, dash: s.dash }) : "")
        + lines.map((l, i) => {
            const { s: value, ...o } = typeof l === "string" ? { s: l } : l;
            return text(x + w / 2, top + i * gap, value, { size: 12, fill: s.sub, anchor: "middle", ...o });
        }).join("");
};

// An Owner: a box that holds its parts, its name at the top left, and on the right what the diagram says of it.
export const owner = (x, y, w, h, name, sub, { state = "alive", tint = false } = {}) => {
    const s = STATES[state];
    return rect(x, y, w, h, { fill: tint ? s.fill : "none", stroke: s.stroke, sw: s.sw, dash: s.dash, rx: 10 })
        + text(x + 12, y + 20, name, { weight: 600, fill: s.name })
        + (sub ? text(x + w - 12, y + 20, sub, { size: 11, fill: s.sub, anchor: "end" }) : "");
};

// A route through its points, its corners rounded.
const route = (points, radius) => {
    const parts = [`M${r1(points[0][0])} ${r1(points[0][1])}`];
    for (let i = 1; i < points.length - 1; i++) {
        const [px, py] = points[i - 1], [x, y] = points[i], [nx, ny] = points[i + 1];
        const a = Math.hypot(x - px, y - py), b = Math.hypot(nx - x, ny - y), r = Math.min(radius, a / 2, b / 2);
        parts.push(`L${r1(x - (x - px) / a * r)} ${r1(y - (y - py) / a * r)}Q${r1(x)} ${r1(y)} ${r1(x + (nx - x) / b * r)} ${r1(y + (ny - y) / b * r)}`);
    }
    const [ex, ey] = points.at(-1);
    parts.push(`L${r1(ex)} ${r1(ey)}`);
    return parts.join("");
};

// A link along its points. Strong: solid, a full head. Weak: dashed, an open head. Both read without their color.
export const link = (points, { weak = false, stroke = C.text2, sw = weak ? 1.4 : 1.6, head = true } = {}) => {
    const [ax, ay] = points.at(-2), [bx, by] = points.at(-1), a = Math.atan2(by - ay, bx - ax), size = 8;
    const wing = t => [bx - size * Math.cos(a + t), by - size * Math.sin(a + t)];
    const [lx, ly] = wing(0.46), [rx, ry] = wing(-0.46);
    // The line stops under a full head: its width must not show past the point.
    const end = head && !weak ? [bx - (size - 1) * Math.cos(a), by - (size - 1) * Math.sin(a)] : [bx, by];
    const body = `<path d="${route([...points.slice(0, -1), end], 8)}" fill="none" stroke="${stroke}" stroke-width="${sw}"${weak ? ` stroke-dasharray="5 4"` : ""}/>`;
    if (!head) return body;
    return body + (weak
        ? `<path d="M${r1(lx)} ${r1(ly)}L${r1(bx)} ${r1(by)}L${r1(rx)} ${r1(ry)}" fill="none" stroke="${stroke}" stroke-width="${sw}" stroke-linejoin="round"/>`
        : `<path d="M${r1(lx)} ${r1(ly)}L${r1(bx)} ${r1(by)}L${r1(rx)} ${r1(ry)}Z" fill="${stroke}"/>`);
};

// A bar as the charts draw them: its data end rounded, square at its start.
export const bar = (x0, x1, y, h, fill, { rounded = true } = {}) => {
    const r = rounded ? Math.min(4, (x1 - x0) / 2, h / 2) : 0;
    return `<path d="M${r1(x0)} ${y}H${r1(x1 - r)}Q${r1(x1)} ${y} ${r1(x1)} ${y + r}V${y + h - r}Q${r1(x1)} ${y + h} ${r1(x1 - r)} ${y + h}H${r1(x0)}Z" fill="${fill}"/>`;
};

// A cross and a check, drawn: no glyph depends on a font.
export const cross = (x, y, { r = 5, stroke = C.red, sw = 2 } = {}) =>
    `<path d="M${x - r} ${y - r}L${x + r} ${y + r}M${x + r} ${y - r}L${x - r} ${y + r}" fill="none" stroke="${stroke}" stroke-width="${sw}" stroke-linecap="round"/>`;
export const check = (x, y, { r = 5, stroke = C.blue, sw = 2 } = {}) =>
    `<path d="M${x - r} ${y}L${x - r * 0.25} ${y + r * 0.75}L${x + r} ${y - r * 0.75}" fill="none" stroke="${stroke}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round"/>`;
export const dot = (x, y, r, fill) => `<circle cx="${r1(x)}" cy="${r1(y)}" r="${r}" fill="${fill}"/>`;
export const ring = (x, y, r, stroke) => `<circle cx="${r1(x)}" cy="${r1(y)}" r="${r}" fill="none" stroke="${stroke}" stroke-width="1"/>`;

// The line of the steps of a strip: one dot per instruction, in their order.
export const steps = (y, centers) => line(PAD, y, RIGHT, y) + centers.map(x => dot(x, y, 3.5, C.text2)).join("");

// A count of strong references, in the type of the ratios of the charts.
export const count = (x, y, n, { fill = C.text } = {}) => text(x, y, n, { size: 18, weight: 600, fill, anchor: "middle" });

// The marks a diagram uses, named on its last line, where the charts name their machine.
export const legend = (y, items, { x = PAD } = {}) => {
    const out = [];
    for (const { mark, name } of items) {
        if (mark === "strong") { out.push(link([[x, y - 4], [x + 26, y - 4]])); x += 34; }
        else if (mark === "weak") { out.push(link([[x, y - 4], [x + 26, y - 4]], { weak: true })); x += 34; }
        else if (mark === "part") { out.push(rect(x, y - 11, 20, 14, { stroke: C.base, rx: 3 }), rect(x + 5, y - 7, 10, 6, { fill: C.base, rx: 1.5 })); x += 28; }
        out.push(text(x, y, name, { size: 11, fill: C.muted }));
        x += name.length * 5.8 + 22;
    }
    return out.join("");
};

const dir = dirname(fileURLToPath(import.meta.url));

// A card: the title says what the picture shows, the subtitle how to read it; alt is what a reader who cannot see it reads.
export const card = (file, { h, title, sub, alt }, body) => {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${h}" viewBox="0 0 ${W} ${h}" role="img" font-family="${SANS}">
<title>${esc(title)}</title>
<desc>${esc(alt)}</desc>
<rect width="${W}" height="${h}" rx="14" fill="${C.surface}"/>
${text(PAD, 44, title, { size: 20, weight: 600, fill: C.text })}
${sub ? text(PAD, 68, sub) : ""}
${body.flat(Infinity).filter(Boolean).join("\n")}
</svg>
`;
    writeFileSync(join(dir, file), svg);
};
