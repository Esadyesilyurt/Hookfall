// Hookfall - draws the UI art: every icon (Tools/UI/icons.js) and the skin's panels, frames and bars
// (Tools/UI/skin.js) are written as SVG, rendered in one sheet by headless Chrome and cut into the sprites the game
// loads from Art/UI/Resources. A sprite file is rewritten only when its pixels change.
//   node Tools/UI/make_ui.js                     everything
//   node Tools/UI/make_ui.js icons Flame Frost   only the icons whose name contains one of the words
//   node Tools/UI/make_ui.js --preview <dir>     also writes review sheets (dark background, names) to <dir>
// Needs Chrome or Edge (set CHROME to the executable if it is not in the usual place). Sprites use 100 pixels per unit;
// icons are 256 x 256.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');
const { pathToFileURL } = require('url');
const png = require('./lib/png');

const ROOT = path.resolve(__dirname, '..', '..');
const OUT = { icons: path.join(ROOT, 'Assets/_Hookfall/Art/UI/Resources/Icons'), skin: path.join(ROOT, 'Assets/_Hookfall/Art/UI/Resources/UI') };

function findChrome() {
  const candidates = [process.env.CHROME,
    'C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Microsoft/Edge/Application/msedge.exe',
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome', '/usr/bin/google-chrome', '/usr/bin/chromium'];
  const found = candidates.find(c => c && fs.existsSync(c));
  if (!found) throw new Error('Chrome or Edge not found: set CHROME to the browser executable.');
  return found;
}

function screenshot(html, width, height, background = '00000000') {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'hookfall-ui-'));
  const page = path.join(dir, 'sheet.html'), shot = path.join(dir, 'sheet.png');
  fs.writeFileSync(page, html);
  // Chrome now and then exits without writing the file (seen on the first launch after a long pause): try again.
  for (let attempt = 0; attempt < 3 && !fs.existsSync(shot); attempt++)
    execFileSync(findChrome(), ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
      `--default-background-color=${background}`, `--window-size=${width},${height}`, `--screenshot=${shot}`, pathToFileURL(page).href],
      { stdio: 'ignore', timeout: 120000 });
  const image = png.decode(fs.readFileSync(shot));
  fs.rmSync(dir, { recursive: true, force: true });
  if (image.width < width || image.height < height) throw new Error(`sheet came back ${image.width}x${image.height}, wanted ${width}x${height}`);
  return image;
}

/** Shelf-packs the sprites, renders them in one page and returns each sprite's pixels. */
function render(sprites) {
  const gap = 4, maxWidth = 4000;
  let x = 0, y = 0, rowHeight = 0, sheetWidth = 0;
  const placed = [...sprites].sort((a, b) => b.h - a.h).map(s => {
    if (x + s.w > maxWidth) { x = 0; y += rowHeight + gap; rowHeight = 0; }
    const spot = { sprite: s, x, y };
    x += s.w + gap; rowHeight = Math.max(rowHeight, s.h); sheetWidth = Math.max(sheetWidth, x);
    return spot;
  });
  const sheetHeight = y + rowHeight;
  const html = '<!doctype html><html><body style="margin:0;background:transparent">' + placed.map(p =>
    `<svg xmlns="http://www.w3.org/2000/svg" style="position:absolute;left:${p.x}px;top:${p.y}px;overflow:hidden" width="${p.sprite.w}" height="${p.sprite.h}" viewBox="0 0 ${p.sprite.w} ${p.sprite.h}">${p.sprite.svg}</svg>`).join('') + '</body></html>';
  const sheet = screenshot(html, sheetWidth, sheetHeight);
  return placed.map(p => ({ sprite: p.sprite, image: png.crop(sheet, p.x, p.y, p.sprite.w, p.sprite.h) }));
}

function writeIfChanged(file, bytes) {
  if (fs.existsSync(file) && fs.readFileSync(file).equals(bytes)) return false;
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, bytes);
  return true;
}

/** Review sheets: the sprites at `cell` pixels on the UI's dark panel colour, with their names. */
function preview(sprites, dir, label, cell = 128, columns = 8, rows = 4) {
  fs.mkdirSync(dir, { recursive: true });
  const per = columns * rows, pad = 26;
  for (let page = 0; page * per < sprites.length; page++) {
    const batch = sprites.slice(page * per, (page + 1) * per);
    const width = columns * (cell + 12) + 12, height = Math.ceil(batch.length / columns) * (cell + pad + 12) + 12;
    const html = `<!doctype html><html><body style="margin:0;background:#18223a;font:13px sans-serif;color:#cfd8e6">` + batch.map((s, i) => {
      const cx = 12 + (i % columns) * (cell + 12), cy = 12 + Math.floor(i / columns) * (cell + pad + 12);
      const scale = Math.min(cell / s.w, cell / s.h), w = Math.round(s.w * scale), h = Math.round(s.h * scale);
      return `<div style="position:absolute;left:${cx}px;top:${cy}px;width:${cell}px;height:${cell}px;background:#223153;border-radius:8px"></div>` +
        `<svg xmlns="http://www.w3.org/2000/svg" style="position:absolute;left:${cx + (cell - w) / 2}px;top:${cy + (cell - h) / 2}px" width="${w}" height="${h}" viewBox="0 0 ${s.w} ${s.h}">${s.svg}</svg>` +
        `<div style="position:absolute;left:${cx}px;top:${cy + cell + 4}px;width:${cell}px;text-align:center;overflow:hidden;white-space:nowrap">${s.name}</div>`;
    }).join('') + '</body></html>';
    const image = screenshot(html, width, height, 'ff18223a');
    const file = path.join(dir, `${label}_${String(page + 1).padStart(2, '0')}.png`);
    fs.writeFileSync(file, png.encode(image.width, image.height, png.crop(image, 0, 0, width, height).data));
    console.log('preview', file);
  }
}

function main() {
  const args = process.argv.slice(2);
  const previewAt = args.indexOf('--preview');
  const previewDir = previewAt >= 0 ? args.splice(previewAt, 2)[1] : null;
  const kinds = ['icons', 'skin'].filter(k => args.includes(k));
  const words = args.filter(a => a !== 'icons' && a !== 'skin').map(a => a.toLowerCase());
  for (const kind of kinds.length ? kinds : ['icons', 'skin']) {
    const all = require(`./${kind}.js`).sprites();
    const chosen = words.length ? all.filter(s => words.some(w => s.name.toLowerCase().includes(w))) : all;
    if (chosen.length === 0) continue;
    let written = 0;
    for (const { sprite, image } of render(chosen))
      if (writeIfChanged(path.join(OUT[kind], sprite.file), png.encode(image.width, image.height, image.data))) written++;
    if (kind === 'skin') writeIfChanged(path.join(ROOT, 'Assets/_Hookfall/Art/UI/skin_borders.txt'), Buffer.from(require('./skin.js').borders()));
    console.log(`${kind}: ${chosen.length} drawn, ${written} written`);
    if (previewDir) preview(chosen, previewDir, kind, kind === 'icons' ? 128 : 200, kind === 'icons' ? 8 : 5, kind === 'icons' ? 4 : 3);
  }
}

main();
