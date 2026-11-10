// Measures the diagrams of this folder in headless Chrome, in their fonts and in wider ones, as a machine without Segoe UI draws
// them: a text over a text, out of the card, cut by a box, crossed by a line. `node check.mjs <outDir>` also writes a PNG of each
// diagram there, to look at it: the measures say what overlaps, not what reads well.
import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const CHROME = process.env.CHROME ?? "C:/Program Files/Google/Chrome/Application/chrome.exe";
const here = dirname(fileURLToPath(import.meta.url)), out = resolve(process.argv[2] ?? join(here, "..", "..", "..", "TestResults", "diagrams"));
const profile = join(out, "chrome"), url = file => `file:///${file.replace(/\\/g, "/")}`;
mkdirSync(out, { recursive: true });
const chrome = (...args) => execFileSync(CHROME, ["--headless=new", "--disable-gpu", `--user-data-dir=${profile}`, ...args], { encoding: "utf8", maxBuffer: 1 << 26, stdio: ["ignore", "pipe", "ignore"] });
const files = readdirSync(here).filter(f => f.endsWith(".svg"));

const PROBE = `
const found = [];
for (const box of document.querySelectorAll('div[data-file]')) {
  const svg = box.querySelector('svg'), file = box.dataset.file, card = svg.viewBox.baseVal;
  const texts = [...svg.querySelectorAll('text')].filter(t => t.textContent).map(t => ({ s: t.textContent, b: t.getBBox() }));
  const hit = (a, b, m = 0) => a.x + m < b.x + b.width && b.x + m < a.x + a.width && a.y + m < b.y + b.height && b.y + m < a.y + a.height;
  const within = (a, b) => a.x >= b.x && a.x + a.width <= b.x + b.width && a.y >= b.y && a.y + a.height <= b.y + b.height;
  texts.forEach(({ s, b }, i) => {
    if (b.x < 8 || b.x + b.width > card.width - 8 || b.y < 8 || b.y + b.height > card.height - 4) found.push([file, 'out of the card', s]);
    for (const other of texts.slice(i + 1)) if (hit(b, other.b, 2)) found.push([file, 'text over text', s, other.s]);
  });
  for (const r of [...svg.querySelectorAll('rect')].slice(1)) {
    const b = r.getBBox();
    if (b.height >= 10 && b.width >= 10)
      for (const t of texts) if (hit(t.b, b, 1) && !within(t.b, b) && !within(b, t.b)) found.push([file, 'text cut by a box', t.s]);
  }
  for (const stroke of svg.querySelectorAll('path[fill="none"], line'))
    for (let d = 0, length = stroke.getTotalLength(); d <= length; d += 2) {
      const p = stroke.getPointAtLength(d);
      for (const t of texts) if (p.x > t.b.x && p.x < t.b.x + t.b.width && p.y > t.b.y + 2 && p.y < t.b.y + t.b.height - 2) found.push([file, 'line through a text', t.s]);
    }
}
const pre = document.createElement('pre');
pre.id = 'found';
pre.textContent = JSON.stringify(found);
document.body.replaceChildren(pre);`;

const measure = (name, widen) => {
    const page = join(out, `${name}.html`);
    const svgs = files.map(f => `<div data-file="${f}">${widen(readFileSync(join(here, f), "utf8"))}</div>`).join("\n");
    writeFileSync(page, `<!doctype html><meta charset="utf-8"><body>${svgs}<script>${PROBE}</script></body>`);
    const dom = chrome("--virtual-time-budget=4000", "--dump-dom", url(page));
    const found = JSON.parse(dom.match(/<pre id="found">([\s\S]*?)<\/pre>/)[1].replace(/&gt;/g, ">").replace(/&lt;/g, "<").replace(/&amp;/g, "&"));
    const lines = [...new Set(found.map(f => f.join(" | ")))];
    lines.forEach(l => console.log(l));
    console.log(`${files.length} diagrams, ${lines.length} problems, ${name}`);
    return lines.length;
};

const problems = measure("their fonts", s => s)
    + measure("wider fonts", s => s.replace(/font-family="'Segoe UI'[^"]*"/, 'font-family="Arial"').replace(/font-family="'Cascadia Mono'[^"]*"/g, 'font-family="Courier New"'));

for (const f of files) {
    const svg = readFileSync(join(here, f), "utf8"), [, w, h] = svg.match(/width="(\d+)" height="(\d+)"/), page = join(out, `${f}.html`);
    writeFileSync(page, `<!doctype html><meta charset="utf-8"><style>html,body{margin:0;background:#fff}</style>${svg}`);
    chrome("--hide-scrollbars", `--window-size=${w},${h}`, `--screenshot=${join(out, f.replace(".svg", ".png"))}`, url(page));
}
process.exitCode = problems ? 1 : 0;
