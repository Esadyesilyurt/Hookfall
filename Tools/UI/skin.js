// Hookfall - the UI skin: panels, buttons, card and slot frames, bars, the diamond icon frame, glows and the small
// marks the screens are built from. Baked sprites carry the skin's navy and steel-blue; the ones marked "tint" are
// greyscale so the code colours them (a rarity, the player's cyan, danger red). make_ui.js renders them to
// Art/UI/Resources/UI/UI_<name>.png and writes their 9-slice borders to Art/UI/skin_borders.txt for the importer.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
const S = require('./lib/svg');

const defs = [];
/** border = [left, bottom, right, top] in pixels, as Unity's sprite border. */
const sprite = (name, w, h, border, svg) => defs.push({ name, file: `UI_${name}.png`, w, h, border, svg });
const grad = (id, stops, vertical = true) => `<linearGradient id="${id}" x1="0" y1="0" x2="${vertical ? 0 : 1}" y2="${vertical ? 1 : 0}">` +
  stops.map(([o, c, a]) => `<stop offset="${o}" stop-color="${c}" stop-opacity="${a ?? 1}"/>`).join('') + '</linearGradient>';
const radial = (id, stops) => `<radialGradient id="${id}">` + stops.map(([o, c, a]) => `<stop offset="${o}" stop-color="${c}" stop-opacity="${a ?? 1}"/>`).join('') + '</radialGradient>';
const blur = (id, d) => `<filter id="${id}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="${d}"/></filter>`;
const grey = v => { const c = Math.round(v * 255).toString(16).padStart(2, '0'); return `#${c}${c}${c}`; };
/** A rectangle with its corners cut at 45 degrees. */
const cut = (x, y, w, h, c) => S.poly([[x + c, y], [x + w - c, y], [x + w, y + c], [x + w, y + h - c], [x + w - c, y + h], [x + c, y + h], [x, y + h - c], [x, y + c]]);

// ---------------------------------------------------------------- panels

// The window panel: deep navy glass, a steel-blue rim that is brightest along the top, a soft shadow, gold corner studs.
sprite('Panel', 160, 160, [52, 52, 52, 52], (() => {
  const m = 10, r = 20, w = 160 - m * 2;
  return grad('pb', [[0, '#1E2D4D'], [0.55, '#152039'], [1, '#0E172C']]) + grad('pr', [[0, '#8FB0E6'], [0.4, '#4A6699'], [1, '#2A3C66']]) + blur('ps', 5) +
    `<rect x="${m}" y="${m + 5}" width="${w}" height="${w}" rx="${r}" fill="#03060F" fill-opacity="0.55" filter="url(#ps)"/>` +
    `<rect x="${m + 1.5}" y="${m + 1.5}" width="${w - 3}" height="${w - 3}" rx="${r - 1.5}" fill="url(#pb)" fill-opacity="0.985" stroke="url(#pr)" stroke-width="3"/>` +
    `<rect x="${m + 6.5}" y="${m + 6.5}" width="${w - 13}" height="${w - 13}" rx="${r - 6.5}" fill="none" stroke="#060B18" stroke-opacity="0.55" stroke-width="1.5"/>` +
    `<path d="M${m + r + 2} ${m + 5} H${160 - m - r - 2}" stroke="#DCEBFF" stroke-opacity="0.35" stroke-width="1.5" stroke-linecap="round"/>` +
    [[m + 16, m + 16], [160 - m - 16, m + 16], [m + 16, 160 - m - 16], [160 - m - 16, 160 - m - 16]].map(([x, y]) =>
      `<path d="${S.kite(x, y, 4.5, 4.5, 4.5)}" fill="#FFCC4D" stroke="#7A5A14" stroke-width="1"/>`).join('');
})());

// A lighter plaque for the HUD: no studs, no shadow margin, a little see-through.
sprite('Plaque', 96, 96, [30, 30, 30, 30],
  grad('qb', [[0, '#22345A'], [1, '#111B33']]) + grad('qr', [[0, '#86A6DA'], [1, '#32477A']]) +
  `<rect x="1.5" y="1.5" width="93" height="93" rx="16" fill="url(#qb)" fill-opacity="0.88" stroke="url(#qr)" stroke-width="2.5"/>` +
  `<path d="M18 4.5 H78" stroke="#DCEBFF" stroke-opacity="0.3" stroke-width="1.5" stroke-linecap="round"/>`);

// Tooltip and small pop-up back: darker, square-ish.
sprite('Tooltip', 64, 64, [20, 20, 20, 20],
  `<rect x="1" y="1" width="62" height="62" rx="9" fill="#0A1124" fill-opacity="0.95" stroke="#5B79AE" stroke-width="2"/>`);

// ---------------------------------------------------------------- buttons (tint)

sprite('Button', 128, 72, [30, 30, 30, 30], (() => {
  const d = cut(1.5, 1.5, 125, 69, 12), inner = cut(6, 6, 116, 60, 9);
  return grad('bb', [[0, grey(0.98)], [0.48, grey(0.78)], [0.52, grey(0.68)], [1, grey(0.56)]]) +
    `<path d="${d}" fill="url(#bb)" stroke="${grey(1)}" stroke-width="3" stroke-linejoin="round"/>` +
    `<path d="${inner}" fill="none" stroke="#000" stroke-opacity="0.28" stroke-width="1.5"/>` +
    `<path d="M16 5.5 H112" stroke="#fff" stroke-opacity="0.75" stroke-width="2" stroke-linecap="round"/>`;
})());

// A flat chip: tags, counters, key hints.
sprite('Chip', 64, 40, [18, 18, 18, 18],
  grad('cb', [[0, grey(0.95)], [1, grey(0.7)]]) + `<rect x="1" y="1" width="62" height="38" rx="12" fill="url(#cb)" stroke="${grey(1)}" stroke-width="2"/>`);

sprite('Tab', 96, 56, [26, 6, 26, 26],
  grad('tb', [[0, grey(0.95)], [1, grey(0.62)]]) + `<path d="M1.5 56 V20 A18 18 0 0 1 19.5 2 H76.5 A18 18 0 0 1 94.5 20 V56 Z" fill="url(#tb)" stroke="${grey(1)}" stroke-width="3"/>`);

sprite('KeyCap', 64, 64, [22, 24, 22, 20],
  `<rect x="2" y="2" width="60" height="60" rx="13" fill="${grey(0.2)}" fill-opacity="0.92"/>` +
  `<rect x="2" y="2" width="60" height="52" rx="12" fill="${grey(0.34)}" fill-opacity="0.95"/>` +
  `<rect x="2" y="2" width="60" height="60" rx="13" fill="none" stroke="${grey(1)}" stroke-width="3"/>` +
  `<path d="M8 54.5 H56" stroke="${grey(1)}" stroke-opacity="0.5" stroke-width="1.5"/>`);

// ---------------------------------------------------------------- cards and slots

// The body of a choice card: baked dark glass with a shadow, lit a little from the top.
sprite('CardFill', 160, 160, [54, 54, 54, 54], (() => {
  const m = 10, w = 140;
  return grad('fb', [[0, '#25365C'], [0.4, '#162240'], [1, '#0C1428']]) + blur('fs', 5) +
    `<rect x="${m}" y="${m + 6}" width="${w}" height="${w}" rx="24" fill="#03060F" fill-opacity="0.6" filter="url(#fs)"/>` +
    `<rect x="${m}" y="${m}" width="${w}" height="${w}" rx="24" fill="url(#fb)" fill-opacity="0.97"/>`;
})());

// Its frame (tint with the rarity or accent colour): a bright rim and a fine inner line.
sprite('CardFrame', 160, 160, [54, 54, 54, 54],
  grad('fr', [[0, grey(1)], [1, grey(0.62)]]) +
  `<rect x="12" y="12" width="136" height="136" rx="22" fill="none" stroke="url(#fr)" stroke-width="4"/>` +
  `<rect x="19.5" y="19.5" width="121" height="121" rx="15" fill="none" stroke="${grey(1)}" stroke-opacity="0.4" stroke-width="1.5"/>`);

// A wash of the card's colour from its top edge (tint).
sprite('CardHeader', 160, 96, [54, 4, 54, 54],
  grad('hw', [[0, '#fff', 0.55], [0.5, '#fff', 0.2], [1, '#fff', 0]]) + `<path d="M14 96 V36 A22 22 0 0 1 36 14 H124 A22 22 0 0 1 146 36 V96 Z" fill="url(#hw)"/>`);

// Item slot: a recess in the HUD, and its rim (tint).
sprite('Slot', 96, 96, [30, 30, 30, 30],
  grad('sb', [[0, '#0A1122'], [1, '#1A2744']]) + `<rect x="2" y="2" width="92" height="92" rx="16" fill="url(#sb)" fill-opacity="0.9"/>` +
  `<rect x="3" y="3" width="90" height="90" rx="15" fill="none" stroke="#000" stroke-opacity="0.5" stroke-width="2"/>`);
sprite('SlotFrame', 96, 96, [30, 30, 30, 30],
  grad('sr', [[0, grey(1)], [1, grey(0.6)]]) + `<rect x="2" y="2" width="92" height="92" rx="16" fill="none" stroke="url(#sr)" stroke-width="3.5"/>`);

// Selection and hover: a soft halo around a rounded box (tint), and a thin outline.
sprite('GlowFrame', 160, 160, [64, 64, 64, 64],
  blur('gf', 7) + `<rect x="24" y="24" width="112" height="112" rx="22" fill="none" stroke="#fff" stroke-width="9" filter="url(#gf)"/>` +
  `<rect x="24" y="24" width="112" height="112" rx="22" fill="none" stroke="#fff" stroke-opacity="0.5" stroke-width="2"/>`);
sprite('Outline', 64, 64, [24, 24, 24, 24], `<rect x="2" y="2" width="60" height="60" rx="14" fill="none" stroke="#fff" stroke-width="3"/>`);

// ---------------------------------------------------------------- bars

sprite('BarBack', 64, 32, [15, 15, 15, 15],
  grad('kb', [[0, '#050912'], [1, '#18223C']]) + `<rect x="0" y="0" width="64" height="32" rx="16" fill="url(#kb)" fill-opacity="0.9"/>`);
sprite('Pill', 64, 32, [15, 15, 15, 15], `<rect x="0" y="0" width="64" height="32" rx="16" fill="#fff"/>`);
sprite('PillFrame', 64, 32, [15, 15, 15, 15],
  grad('kr', [[0, grey(1)], [1, grey(0.55)]]) + `<rect x="1.5" y="1.5" width="61" height="29" rx="14.5" fill="none" stroke="url(#kr)" stroke-width="3"/>`);
// The shine laid over a bar's fill.
sprite('BarGloss', 64, 32, [15, 15, 15, 15],
  grad('kg', [[0, '#fff', 0.55], [0.45, '#fff', 0.12], [0.5, '#000', 0.0], [1, '#000', 0.28]]) + `<rect x="0" y="0" width="64" height="32" rx="16" fill="url(#kg)"/>`);

// ---------------------------------------------------------------- frames for icons

sprite('DiamondFill', 160, 160, [0, 0, 0, 0],
  radial('df', [[0, '#2B4170'], [1, '#0D152B']]) + `<path d="${S.kite(80, 80, 72, 72, 72)}" fill="url(#df)"/>`);
sprite('Diamond', 160, 160, [0, 0, 0, 0],
  grad('dr', [[0, grey(1)], [1, grey(0.6)]]) + `<path d="${S.kite(80, 80, 73, 73, 73)}" fill="none" stroke="url(#dr)" stroke-width="7" stroke-linejoin="round"/>` +
  `<path d="${S.kite(80, 80, 61, 61, 61)}" fill="none" stroke="${grey(1)}" stroke-opacity="0.45" stroke-width="2" stroke-linejoin="round"/>`);
sprite('Circle', 128, 128, [0, 0, 0, 0], `<circle cx="64" cy="64" r="63" fill="#fff"/>`);
sprite('CircleFrame', 128, 128, [0, 0, 0, 0],
  grad('or', [[0, grey(1)], [1, grey(0.6)]]) + `<circle cx="64" cy="64" r="59" fill="none" stroke="url(#or)" stroke-width="7"/>`);
sprite('Ring', 128, 128, [0, 0, 0, 0], `<circle cx="64" cy="64" r="52" fill="none" stroke="#fff" stroke-width="8"/>`);
sprite('Dot', 64, 64, [0, 0, 0, 0], `<circle cx="32" cy="32" r="29" fill="#fff"/>`);

// ---------------------------------------------------------------- light, lines and marks

sprite('Glow', 128, 128, [0, 0, 0, 0], radial('gl', [[0, '#fff', 1], [0.5, '#fff', 0.32], [1, '#fff', 0]]) + `<circle cx="64" cy="64" r="64" fill="url(#gl)"/>`);
sprite('Rays', 512, 512, [0, 0, 0, 0], (() => {
  let rays = '';
  for (let i = 0; i < 14; i++) {
    const a = i / 14 * Math.PI * 2, w = i % 2 ? 0.075 : 0.12;
    rays += `<path d="M256 256 L${S.n(256 + Math.cos(a - w) * 262)} ${S.n(256 + Math.sin(a - w) * 262)} L${S.n(256 + Math.cos(a + w) * 262)} ${S.n(256 + Math.sin(a + w) * 262)} Z" fill="#fff"/>`;
  }
  return radial('rm', [[0, '#fff', 0], [0.16, '#fff', 0.9], [0.6, '#fff', 0.35], [1, '#fff', 0]]) +
    `<mask id="rk"><circle cx="256" cy="256" r="256" fill="url(#rm)"/></mask><g mask="url(#rk)">${rays}</g>`;
})());
sprite('Vignette', 256, 256, [0, 0, 0, 0], radial('vg', [[0, '#000', 0], [0.55, '#000', 0.12], [1, '#000', 0.85]]) + `<rect width="256" height="256" fill="url(#vg)"/>`);
// A line that fades out to the left; flip it for the other side of a title.
sprite('FadeLine', 128, 8, [0, 0, 0, 0], grad('fl', [[0, '#fff', 0], [1, '#fff', 1]], false) + `<rect x="0" y="2.5" width="128" height="3" rx="1.5" fill="url(#fl)"/>`);
// Shades: opaque at the top, and opaque at the left.
sprite('ShadeDown', 16, 128, [0, 0, 0, 0], grad('sd', [[0, '#fff', 1], [1, '#fff', 0]]) + `<rect width="16" height="128" fill="url(#sd)"/>`);
sprite('ShadeRight', 128, 16, [0, 0, 0, 0], grad('sx', [[0, '#fff', 1], [1, '#fff', 0]], false) + `<rect width="128" height="16" fill="url(#sx)"/>`);
sprite('Crosshair', 96, 96, [0, 0, 0, 0], (() => {
  const ticks = 'M48 10 V30 M48 66 V86 M10 48 H30 M66 48 H86';
  return `<path d="${ticks}" stroke="#000" stroke-opacity="0.6" stroke-width="7" stroke-linecap="round"/><circle cx="48" cy="48" r="5.5" fill="#000" fill-opacity="0.6"/>` +
    `<path d="${ticks}" stroke="#fff" stroke-width="3.5" stroke-linecap="round"/><circle cx="48" cy="48" r="3" fill="#fff"/>`;
})());
// A banner with notched ends (tint): records, tags, "NEW".
sprite('Ribbon', 160, 56, [46, 0, 46, 0],
  grad('rb', [[0, grey(1)], [1, grey(0.72)]]) + `<path d="M2 6 H158 L140 28 L158 50 H2 L20 28 Z" fill="url(#rb)" stroke="${grey(1)}" stroke-width="2.5" stroke-linejoin="round"/>` +
  `<path d="M26 12 H134 M26 44 H134" stroke="#000" stroke-opacity="0.22" stroke-width="1.5"/>`);
// A small solid diamond: title ornaments, level pips, list bullets.
sprite('Pip', 32, 32, [0, 0, 0, 0], `<path d="${S.kite(16, 16, 13, 13, 13)}" fill="#fff"/>`);
// The damage-direction wedge: points up, fades towards the centre of the screen.
sprite('Wedge', 128, 64, [0, 0, 0, 0], grad('wg', [[0, '#fff', 1], [1, '#fff', 0]]) + `<path d="M6 30 Q64 -6 122 30 L104 60 Q64 34 24 60 Z" fill="url(#wg)"/>`);

module.exports.sprites = () => defs;
module.exports.borders = () => defs.map(d => `${d.name} ${d.border.join(' ')}`).join('\n') + '\n';
