// Hookfall - every UI icon as a 256 x 256 cel-shaded sticker: stats, HUD symbols, schools, screen glyphs here; the
// items in icons_items.js; power-ups, elite traits and the rest in icons_more.js. make_ui.js renders them to
// Art/UI/Resources/Icons/ICON_<name>.png, the names the game looks up (UISkin.Icon).
// Palette (GDD 8.1): cyan for what is the player's, warm gold for rewards, magenta for enemies, red for health and danger.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
const S = require('./lib/svg');
const { part, line, glow, sparkle, rot, flipX, INK } = S;

const C = {
  cyan: '#4DF2FF', cyanD: '#1FA8C9', teal: '#2FD0C8', gold: '#FFCC4D', goldD: '#D9941C', amber: '#FFA62B',
  red: '#F0485A', rose: '#FF8A9B', crimson: '#C9263F', magenta: '#E64BD0', purple: '#9A6BFF', violet: '#6B4BD6',
  indigo: '#3E3A8C', green: '#7BDB6A', lime: '#B7F06A', forest: '#3FA05A', white: '#F4F7FB', steel: '#B8C4D6',
  iron: '#7C8AA3', dark: '#3A4660', orange: '#FF7A2E', fire: '#FFB02E', ember: '#FF5A2A', ice: '#9BE8FF',
  frost: '#D9F7FF', blue: '#4D8DFF', navy: '#2B3F7A', wood: '#A9703F', woodD: '#7A4E2A', leather: '#8B5A3C',
  bone: '#F2E9D0', stone: '#9AA3B5', stoneD: '#6E778A', skin: '#F2C39B', pink: '#FF9AD5', glass: '#CFEFFF',
};

const defs = {};
const icon = (name, build) => { defs[name] = build; };
const ink = (d, o = {}) => part(d, o.color || INK, { flat: true, stroke: 0, ...o });
const flat = (d, color, o = {}) => part(d, color, { flat: true, stroke: 0, ...o });

// ---------------------------------------------------------------- shared shapes

const plus = (cx, cy, r, t) => S.poly([[cx - t, cy - r], [cx + t, cy - r], [cx + t, cy - t], [cx + r, cy - t], [cx + r, cy + t], [cx + t, cy + t],
  [cx + t, cy + r], [cx - t, cy + r], [cx - t, cy + t], [cx - r, cy + t], [cx - r, cy - t], [cx - t, cy - t]]);

const chevron = (cx, cy, w, h, t) => S.poly([[cx - w / 2, cy - h / 2], [cx - w / 2 + t, cy - h / 2], [cx + w / 2, cy], [cx - w / 2 + t, cy + h / 2], [cx - w / 2, cy + h / 2], [cx + w / 2 - t, cy]]);

/** Speed streaks trailing to the left of (x, y). */
const streaks = (x, y, lengths, gap = 34, color = C.white) => lengths.map((len, i) => part(S.bar(x - len, y + i * gap, x, y + i * gap, 13), color, { stroke: 8, shift: [-3, -4] })).join('');

/** A J-shaped grappling hook: eye at the top, the point rising on the right. */
function hook(cx, cy, s, color, o = {}) {
  const w = s * 0.2, r = s * 0.38, x2 = cx + r * 2;
  const d = `M${cx} ${cy - s * 0.72} V${cy + s * 0.35} a${r} ${r} 0 0 0 ${r * 2} 0 v${-s * 0.22}`;
  const tip = S.poly([[x2 - w * 1.25, cy + s * 0.16], [x2, cy - s * 0.3], [x2 + w * 1.25, cy + s * 0.16]]);
  const out = line(d, color, w, { outline: o.outline ?? 10 }) + part(tip, color, { stroke: 8, shift: [-3, -4] }) +
    part(S.ring(cx, cy - s * 0.9, s * 0.21, s * 0.08), color, { rule: 'evenodd', stroke: 8, shift: [-3, -4] });
  return o.t ? S.group(out, o.t) : out;
}

function coin(cx, cy, r, emboss = true, color = C.gold) {
  return part(S.circle(cx, cy, r), color, { gloss: [cx - r * 0.38, cy - r * 0.42, r * 0.28, r * 0.15] }) +
    `<circle cx="${cx}" cy="${cy}" r="${r * 0.68}" fill="none" stroke="${C.goldD}" stroke-width="${Math.max(4, r * 0.08)}"/>` +
    (emboss ? flat(S.kite(cx, cy, r * 0.26, r * 0.4, r * 0.4), C.goldD) : '');
}

function hourglass(sand, o = {}) {
  const frame = o.frame || C.wood;
  return part(S.poly([[82, 52], [174, 52], [174, 76], [142, 128], [174, 180], [174, 204], [82, 204], [82, 180], [114, 128], [82, 76]]), C.glass, { depth: 0.2 }) +
    flat(S.poly([[96, 78], [160, 78], [130, 122], [126, 122]]), sand) + flat(S.poly([[92, 198], [164, 198], [128, 152]]), sand) +
    `<path d="M128 122 V156" stroke="${sand}" stroke-width="6" stroke-linecap="round"/>` +
    part(S.rrect(60, 28, 136, 28, 9), frame) + part(S.rrect(60, 200, 136, 28, 9), frame);
}

function skull(cx, cy, s, color = C.bone, eye = INK) {
  const k = v => v * s;
  const d = `M${cx - k(74)} ${cy - k(8)} A${k(74)} ${k(74)} 0 0 1 ${cx + k(74)} ${cy - k(8)} V${cy + k(30)} C${cx + k(74)} ${cy + k(50)} ${cx + k(58)} ${cy + k(56)} ${cx + k(48)} ${cy + k(58)} ` +
    `V${cy + k(88)} H${cx - k(48)} V${cy + k(58)} C${cx - k(58)} ${cy + k(56)} ${cx - k(74)} ${cy + k(50)} ${cx - k(74)} ${cy + k(30)} Z`;
  return part(d, color, { gloss: [cx - k(34), cy - k(50), k(22), k(12)] }) +
    flat(S.ellipse(cx - k(29), cy + k(4), k(20), k(24)), eye) + flat(S.ellipse(cx + k(29), cy + k(4), k(20), k(24)), eye) +
    flat(S.poly([[cx, cy + k(30)], [cx + k(11), cy + k(50)], [cx - k(11), cy + k(50)]]), INK) +
    [-24, 0, 24].map(x => `<path d="M${cx + k(x)} ${cy + k(64)} V${cy + k(88)}" stroke="${INK}" stroke-width="${k(7)}" stroke-linecap="round"/>`).join('');
}

/** A glass jar with a cork: returns [back, front] so the contents can sit between them. */
function jar(tint = C.glass) {
  const body = `M74 96 C74 80 86 74 96 70 V58 H160 V70 C170 74 182 80 182 96 V204 A20 20 0 0 1 162 224 H94 A20 20 0 0 1 74 204 Z`;
  return [part(body, tint, { depth: 0.18 }),
    `<path d="M92 104 V196" stroke="#fff" stroke-opacity="0.75" stroke-width="10" stroke-linecap="round"/>` +
    part(S.rrect(88, 30, 80, 34, 9), C.wood) + part(S.rrect(82, 56, 92, 18, 7), C.iron, { stroke: 8 })];
}

function sword(o = {}) {
  const blade = o.blade || C.steel;
  return part(S.poly([[128, 14], [150, 46], [150, 162], [106, 162], [106, 46]]), blade, { gloss: [118, 70, 7, 40, 0] }) +
    `<path d="M128 40 V156" stroke="${S.darken(blade, 0.3)}" stroke-width="5" stroke-linecap="round"/>` +
    part(S.rrect(70, 158, 116, 24, 10), o.guard || C.gold) + part(S.rrect(113, 180, 30, 44, 6), o.grip || C.leather) +
    part(S.circle(128, 232, 16), o.guard || C.gold);
}

const snowflake = (cx, cy, r, color, width = 12) => [0, 60, 120].map(a => {
  const x = Math.sin(S.rad(a)) * r, y = Math.cos(S.rad(a)) * r;
  const tick = t => { const px = cx + x * t, py = cy - y * t, bx = Math.sin(S.rad(a + 55)) * r * 0.26, by = Math.cos(S.rad(a + 55)) * r * 0.26,
    cx2 = Math.sin(S.rad(a - 55)) * r * 0.26, cy2 = Math.cos(S.rad(a - 55)) * r * 0.26; const sgn = t > 0 ? 1 : -1;
    return `M${S.n(px + bx * sgn)} ${S.n(py - by * sgn)} L${S.n(px)} ${S.n(py)} L${S.n(px + cx2 * sgn)} ${S.n(py - cy2 * sgn)}`; };
  return `M${S.n(cx - x)} ${S.n(cy + y)} L${S.n(cx + x)} ${S.n(cy - y)} ${tick(0.62)} ${tick(-0.62)}`;
}).map(d => `<path d="${d}" fill="none" stroke="${INK}" stroke-width="${width + 10}" stroke-linecap="round" stroke-linejoin="round"/>`).join('') +
  [0, 60, 120].map(a => {
    const x = Math.sin(S.rad(a)) * r, y = Math.cos(S.rad(a)) * r;
    const tick = t => { const px = cx + x * t, py = cy - y * t, bx = Math.sin(S.rad(a + 55)) * r * 0.26, by = Math.cos(S.rad(a + 55)) * r * 0.26,
      cx2 = Math.sin(S.rad(a - 55)) * r * 0.26, cy2 = Math.cos(S.rad(a - 55)) * r * 0.26; const sgn = t > 0 ? 1 : -1;
      return `M${S.n(px + bx * sgn)} ${S.n(py - by * sgn)} L${S.n(px)} ${S.n(py)} L${S.n(px + cx2 * sgn)} ${S.n(py - cy2 * sgn)}`; };
    return `<path d="M${S.n(cx - x)} ${S.n(cy + y)} L${S.n(cx + x)} ${S.n(cy - y)} ${tick(0.62)} ${tick(-0.62)}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"/>`;
  }).join('');

const clover = (cx, cy, s, color = C.green) =>
  line(`M${cx} ${cy} Q${cx + s * 0.1} ${cy + s * 0.8} ${cx + s * 0.4} ${cy + s * 1.2}`, C.forest, s * 0.14) +
  [0, 90, 180, 270].map(a => rot(part(S.heart(cx, cy - s * 0.56, s * 0.46), color, { stroke: 9, shift: a === 0 ? [-5, -6] : a === 90 ? [-6, 5] : a === 180 ? [5, 6] : [6, -5] }), a, cx, cy)).join('');

const wing = (color = C.white, dark = '#9FD8EC') => part('M48 180 C52 96 136 40 232 44 C218 68 202 82 182 92 C200 94 212 98 220 108 C200 126 178 136 156 140 C170 146 178 152 184 160 C150 182 96 192 48 180 Z', color, { dark, gloss: [112, 96, 30, 12, -32] });

module.exports = { S, C, defs, icon, ink, flat, plus, chevron, streaks, hook, coin, hourglass, skull, jar, sword, snowflake, clover, wing };

// ---------------------------------------------------------------- stats

icon('Health', () => part(S.heart(128, 130, 88), C.red, { gloss: [86, 92, 26, 15] }));
icon('MaxHealth', () => part(S.heart(116, 126, 80), C.red, { gloss: [80, 92, 22, 13] }) + part(S.arrow(198, 236, 198, 124, 30, 76), C.gold));
icon('Regen', () => part(S.heart(114, 136, 78), C.red, { gloss: [80, 104, 22, 13] }) + part(plus(196, 70, 42, 14), C.green));
icon('Lifesteal', () => part(S.drop(128, 162, 54), C.crimson, { gloss: [108, 150, 14, 22, 20] }) +
  [86, 170].map(x => part(S.poly([[x - 20, 22], [x + 20, 22], [x + 8, 64], [x, 100], [x - 8, 64]]), C.bone)).join(''));
icon('Armor', () => part(S.shield(128, 124, 88, 100), C.steel, { gloss: [84, 62, 28, 14] }) + part(S.shield(128, 120, 50, 62), C.cyan, { stroke: 8 }));
icon('Damage', () => rot(sword(), 42) + sparkle(204, 62, 30, C.gold, 7));
icon('AttackSpeed', () => streaks(78, 94, [52, 66, 52]) + part(chevron(132, 128, 76, 150, 40), C.gold) + part(chevron(204, 128, 76, 150, 40), C.gold));
icon('WalkSpeed', () => streaks(72, 104, [44, 60, 44], 40) +
  part('M104 44 H166 V138 C204 142 232 160 232 190 V208 H96 V150 Z', C.leather, { gloss: [124, 96, 9, 34, 0] }) +
  part(S.rrect(96, 34, 78, 30, 8), C.gold) + part(S.rrect(88, 200, 152, 24, 9), C.woodD));
icon('FlightSpeed', () => streaks(86, 196, [56, 40], 30) + wing());
icon('HookRange', () => hook(96, 96, 104, C.steel) + part(S.arrow(128, 214, 30, 214, 18, 46), C.gold, { stroke: 8 }) + part(S.arrow(128, 214, 226, 214, 18, 46), C.gold, { stroke: 8 }));
icon('PickupRadius', () => rot(part('M40 196 V120 A88 88 0 0 1 216 120 V196 H166 V120 A38 38 0 0 0 90 120 V196 Z', C.red, { gloss: [82, 70, 20, 10, -40] }) +
  part(S.rrect(40, 160, 50, 40, 4), C.white) + part(S.rrect(166, 160, 50, 40, 4), C.white), -24) + sparkle(196, 214, 30, C.gold, 7) + sparkle(120, 232, 18, C.gold, 6));
icon('XPGain', () => part(S.poly([[100, 30], [156, 92], [156, 164], [100, 226], [44, 164], [44, 92]]), C.amber, { gloss: [78, 92, 12, 34, 20] }) +
  `<path d="M44 92 L100 122 L156 92 M100 122 V226" fill="none" stroke="${S.darken(C.amber, 0.35)}" stroke-width="5" stroke-linejoin="round"/>` +
  part(S.arrow(206, 214, 206, 74, 28, 70), C.green));
icon('Luck', () => clover(124, 116, 104));
icon('Zip', () => hook(92, 122, 124, C.cyan) + part(S.bolt(196, 100, 74), C.gold));
icon('ZipRange', () => hook(100, 92, 100, C.cyan) + part(S.bolt(200, 78, 46), C.gold, { stroke: 8 }) +
  part(S.arrow(128, 216, 30, 216, 18, 46), C.gold, { stroke: 8 }) + part(S.arrow(128, 216, 226, 216, 18, 46), C.gold, { stroke: 8 }));
icon('ZipCooldown', () => hourglass(C.cyan) + part(S.bolt(214, 64, 46), C.gold, { stroke: 8 }));

// ---------------------------------------------------------------- HUD and world

icon('Gold', () => coin(128, 128, 94));
icon('Kills', () => skull(128, 112, 1.12));
icon('Timer', () => hourglass(C.gold));
icon('Chest', () => part(S.rrect(36, 116, 184, 100, 14), C.wood) + part('M36 126 V98 A44 44 0 0 1 80 54 H176 A44 44 0 0 1 220 98 V126 Z', '#C08A52') +
  [58, 178].map(x => part(S.rrect(x, 54, 20, 162, 3), C.gold, { stroke: 7, shift: [-3, -4] })).join('') +
  part(S.rrect(30, 112, 196, 20, 7), C.gold, { stroke: 8 }) + part(S.rrect(108, 104, 40, 46, 9), C.gold, { stroke: 8 }) + ink(S.circle(128, 122, 7)) + ink(S.poly([[124, 124], [132, 124], [135, 140], [121, 140]])));
icon('Tower', () => line('M128 54 V18', C.woodD, 7) + part(S.poly([[128, 14], [176, 28], [128, 44]]), C.cyan, { stroke: 8 }) +
  part(S.poly([[88, 228], [96, 96], [160, 96], [168, 228]]), C.stone, { gloss: [112, 150, 7, 40, 4] }) +
  part(S.poly([[78, 100], [78, 52], [100, 52], [100, 68], [117, 68], [117, 52], [139, 52], [139, 68], [156, 68], [156, 52], [178, 52], [178, 100]]), C.stone) +
  ink(S.rrect(118, 118, 20, 36, 10)) + glow(128, 208, 34, C.cyan, 0.9) + flat('M112 228 V198 A16 16 0 0 1 144 198 V228 Z', C.cyan));
icon('Boss', () => [0, 1].map(i => { const h = part('M62 110 C26 96 14 56 28 18 C48 52 72 66 98 76 Z', C.violet, { stroke: 9 }); return i ? flipX(h) : h; }).join('') +
  skull(128, 128, 1.0, '#EBD9F5', '#3A0B3F') + glow(99, 132, 20, C.magenta, 1) + glow(157, 132, 20, C.magenta, 1));
icon('Portal', () => glow(128, 128, 122, C.cyan, 0.45) + part(S.ring(128, 128, 100, 66), C.stone, { rule: 'evenodd' }) + flat(S.circle(128, 128, 64), '#123B63') +
  `<path d="M128 128 m-38 0 a38 38 0 1 1 38 38 a24 24 0 1 1 -24 -24 a12 12 0 1 1 12 12" fill="none" stroke="${C.cyan}" stroke-width="9" stroke-linecap="round"/>` +
  [0, 90, 180, 270].map(a => rot(flat(S.kite(128, 45, 8, 12, 12), C.cyan), a)).join(''));
icon('Shrine', () => glow(128, 110, 96, C.purple, 0.5) + part(S.rrect(68, 204, 120, 28, 8), C.stoneD) +
  part(S.poly([[98, 208], [108, 60], [128, 26], [148, 60], [158, 208]]), C.stone, { gloss: [118, 120, 6, 50, 3] }) +
  flat(S.kite(128, 104, 13, 22, 22), C.cyan) + `<path d="M114 146 H142 M118 166 H138" stroke="${C.cyan}" stroke-width="7" stroke-linecap="round"/>`);
icon('Flow', () => line('M30 92 H150 a28 28 0 1 0 -28 -28', C.cyan, 15) + line('M30 138 H186 a32 32 0 1 1 -32 32', C.white, 15) + line('M58 184 H112', C.cyan, 15));
icon('Jump', () => [158, 96].map((y, i) => part(S.poly([[128, y - 56], [206, y + 14], [206, y + 50], [128, y - 14], [50, y + 50], [50, y + 14]]), i ? C.cyan : C.white)).join(''));
icon('Dash', () => streaks(92, 86, [60, 76, 60], 42) + part(S.arrow(84, 128, 236, 128, 44, 112), C.cyan));
icon('Shard', () => glow(128, 128, 118, C.cyan, 0.4) + part(S.poly([[180, 64], [214, 104], [196, 168], [164, 130]]), C.cyanD, { stroke: 8 }) +
  part(S.poly([[112, 22], [164, 96], [130, 234], [70, 110]]), C.cyan, { gloss: [104, 88, 10, 34, 24] }) +
  `<path d="M112 22 L118 112 L130 234 M70 110 L118 112 L164 96" fill="none" stroke="${C.cyanD}" stroke-width="5" stroke-linejoin="round"/>` + sparkle(54, 58, 22) + sparkle(204, 208, 16));
icon('Star', () => part(S.star(128, 134, 5, 108, 46), C.gold, { gloss: [100, 96, 20, 10] }));
icon('Elite', () => part(S.poly([[36, 196], [30, 76], [84, 126], [128, 46], [172, 126], [226, 76], [220, 196]]), C.gold, { gloss: [84, 150, 26, 9, 0] }) +
  part(S.rrect(36, 186, 184, 30, 8), C.goldD) + [[128, 46], [30, 76], [226, 76]].map(p => part(S.circle(p[0], p[1], 14), C.magenta, { stroke: 7, shift: [-3, -4] })).join(''));
icon('Stage', () => line('M70 30 V232', C.woodD, 14) + part('M78 40 H214 L186 84 L214 128 H78 Z', C.cyan, { gloss: [116, 62, 26, 9, 0] }) + part(S.circle(70, 28, 13), C.gold, { stroke: 8 }));

// ---------------------------------------------------------------- schools

icon('Flame', () => part(S.flame(128, 150, 66), C.orange, { dark: C.ember }) + flat(S.flame(128, 176, 34), C.gold));
icon('Frost', () => snowflake(128, 128, 100, C.ice, 15));
icon('Storm', () => part(S.bolt(128, 128, 112), C.gold, { gloss: [136, 70, 8, 26, 22] }));
icon('Blood', () => part(S.drop(128, 148, 70), C.crimson, { gloss: [102, 150, 16, 26, 20] }));
icon('Arcane', () => glow(128, 128, 112, C.purple, 0.5) + part(S.star(128, 128, 4, 108, 34), C.purple, { gloss: [116, 104, 10, 22, 0] }) + sparkle(204, 56, 26) + sparkle(54, 196, 20));
icon('Guardian', () => part(S.shield(128, 124, 88, 100), C.cyanD, { gloss: [84, 62, 28, 14] }) + part(plus(128, 116, 46, 15), C.white, { stroke: 8 }));
icon('Wild', () => part(S.leaf(46, 214, 214, 40, 104), C.green, { gloss: [118, 108, 30, 10, -45] }) + `<path d="M56 204 L196 60" stroke="${C.forest}" stroke-width="7" stroke-linecap="round"/>` +
  `<path d="M96 164 L92 124 M130 130 L128 88 M110 150 L150 152 M146 114 L184 116" stroke="${C.forest}" stroke-width="6" stroke-linecap="round"/>`);
icon('Fortune', () => coin(128, 128, 94, false) + rot(clover(128, 122, 62, C.goldD), 0));

// ---------------------------------------------------------------- screen glyphs (drawn white so the UI can tint them)

icon('Lock', () => part(S.arc(128, 104, 46, -90, 90, 24), C.steel) + part(S.rrect(62, 104, 132, 114, 16), C.gold, { gloss: [92, 128, 18, 8, 0] }) + ink(S.circle(128, 150, 13)) + ink(S.poly([[121, 152], [135, 152], [140, 186], [116, 186]])));
icon('Check', () => part(S.poly([[38, 132], [72, 98], [108, 134], [186, 52], [222, 88], [108, 208]]), C.green));
icon('Reroll', () => [0, 180].map(a => rot(part(S.arcArrow(128, 128, 74, -70, 62, 30, 78), C.cyan), a)).join(''));
icon('Skip', () => part(S.poly([[36, 52], [112, 128], [36, 204]]), C.white) + part(S.poly([[112, 52], [188, 128], [112, 204]]), C.white) + part(S.rrect(190, 52, 30, 152, 8), C.white));
icon('Banish', () => part(S.ring(128, 128, 104, 70), C.red, { rule: 'evenodd' }) + rot(part(S.rrect(40, 111, 176, 34, 6), C.red), 45));
icon('Codex', () => part(S.rrect(48, 30, 160, 196, 14), C.navy) + part(S.rrect(60, 206, 148, 22, 6), C.bone, { stroke: 7 }) + part(S.rrect(48, 30, 30, 196, 8), C.indigo, { stroke: 8 }) +
  part(S.star(142, 112, 4, 48, 16), C.gold, { stroke: 7 }));
icon('Settings', () => part(S.gear(128, 128, 8, 112, 84) + ' ' + S.circle(128, 128, 34), C.steel, { rule: 'evenodd', gloss: [92, 82, 18, 9] }));
icon('Leaderboard', () => line('M72 70 C24 70 30 132 84 134 M184 70 C232 70 226 132 172 134', C.gold, 13) +
  part('M70 36 H186 V96 C186 140 160 160 128 160 C96 160 70 140 70 96 Z', C.gold, { gloss: [98, 74, 10, 28, 0] }) + part(S.rrect(114, 156, 28, 44, 4), C.goldD) + part(S.rrect(76, 196, 104, 30, 8), C.gold) +
  flat(S.star(128, 94, 5, 30, 13), C.goldD));
icon('MouseLeft', () => part(S.rrect(66, 26, 124, 204, 60), C.white) + part('M128 26 H126 A60 60 0 0 0 66 86 V118 H128 Z', C.cyan, { stroke: 8 }) + `<path d="M66 118 H190" stroke="${INK}" stroke-width="8"/>`);
icon('MouseRight', () => part(S.rrect(66, 26, 124, 204, 60), C.white) + part('M128 26 H130 A60 60 0 0 1 190 86 V118 H128 Z', C.cyan, { stroke: 8 }) + `<path d="M66 118 H190" stroke="${INK}" stroke-width="8"/>`);

/** Every icon as a sprite for make_ui.js. */
module.exports.sprites = () => {
  require('./icons_items.js');
  require('./icons_more.js');
  return Object.keys(defs).map(name => ({ name, file: `ICON_${name}.png`, w: 256, h: 256, svg: defs[name]() }));
};
