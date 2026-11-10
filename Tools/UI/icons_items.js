// Hookfall - the item icons: the 14 attack items and the 36 passives, named by their prefab ids (PF_Item_<id>).
// Each one is drawn as what its fantasy name says (a salamander, a monocle, a jar with an imp in it), in the colours
// of its school where that helps to tell builds apart.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
const { S, C, icon, ink, flat, plus, streaks, hook, coin, hourglass, jar, clover, wing } = require('./icons.js');
const { part, line, glow, sparkle, rot, flipX, INK } = S;

const eye = (cx, cy, r, glint = true) => ink(S.circle(cx, cy, r)) + (glint ? flat(S.circle(cx - r * 0.3, cy - r * 0.35, r * 0.34), '#ffffff') : '');
const both = shape => shape + flipX(shape);
const stormCloud = (cx, cy, s, color = '#6268B0') => part(`M${cx - s * 1.5} ${cy + s * 0.7} a${s * 0.8} ${s * 0.8} 0 0 1 ${s * 0.3} ${-s * 1.55} a${s * 1.05} ${s * 1.05} 0 0 1 ${s * 2} ${-s * 0.2} a${s * 0.8} ${s * 0.8} 0 0 1 ${s * 0.3} ${s * 1.75} Z`, color, { stroke: 8 });

// ---------------------------------------------------------------- attack items

icon('OrbitBlades', () => glow(128, 128, 112, C.cyan, 0.35) +
  `<circle cx="128" cy="128" r="66" fill="none" stroke="${INK}" stroke-width="15"/><circle cx="128" cy="128" r="66" fill="none" stroke="${C.cyanD}" stroke-width="6"/>` +
  [0, 120, 240].map(a => rot(part(S.crescent(128, 58, 44, 14, 24, 38), C.frost, { dark: C.cyanD, stroke: 9, shift: [-4, -5] }), a)).join('') +
  part(S.circle(128, 128, 24), C.white, { stroke: 8 }));

icon('ShardBurst', () => [-66, -33, 0, 33, 66].map(a => rot(part(S.kite(128, 84, 22, 62, 44), C.cyan, { stroke: 9, shift: [-5, -6] }), a, 128, 176)).join('') +
  part('M40 188 C40 140 84 124 128 124 C176 124 222 146 232 184 C220 204 180 214 128 214 C80 214 40 208 40 188 Z', '#2A7C8C') +
  ink(S.circle(228, 184, 9)) + eye(176, 164, 10) + part(S.ellipse(88, 214, 20, 10), '#1F5E6B', { stroke: 7 }) + part(S.ellipse(160, 216, 20, 10), '#1F5E6B', { stroke: 7 }));

icon('ArcLaser', () => glow(196, 128, 86, C.gold, 0.5) + part(S.bar(10, 154, 78, 138, 9), C.white, { stroke: 7 }) +
  part(S.poly([[128, 128], [250, 70], [250, 128]]), C.gold, { stroke: 8 }) + part(S.poly([[128, 128], [250, 128], [250, 186]]), C.orange, { stroke: 8 }) +
  part(S.poly([[104, 44], [182, 196], [26, 196]]), C.glass, { depth: 0.22, gloss: [84, 128, 10, 34, 26] }));

icon('RamStrike', () => glow(128, 128, 120, C.cyan, 0.4) + part('M70 76 C58 50 70 30 90 26 C94 46 96 62 100 76 Z', '#6E93B8', { stroke: 8 }) +
  part('M34 124 C34 72 92 52 144 62 C186 70 218 98 228 138 C234 162 222 180 200 184 C170 190 150 174 120 186 C76 202 34 174 34 124 Z', '#8FB6D9', { gloss: [92, 96, 30, 12, -14] }) +
  part('M172 100 C182 62 202 36 236 18 C232 58 226 90 214 122 Z', C.bone, { stroke: 9 }) + part('M140 84 C144 64 156 52 170 46 C170 62 168 78 164 92 Z', C.bone, { stroke: 8 }) +
  eye(116, 118, 11) + `<path d="M94 98 L134 106" stroke="${INK}" stroke-width="8" stroke-linecap="round"/>` + ink(S.ellipse(204, 154, 8, 6)));

const wisp = (cx, cy, s, angle) => rot(part(S.flame(cx, cy, s), '#6CF5C4', { dark: '#2BA98A', stroke: 9 }) + flat(S.flame(cx, cy + s * 0.34, s * 0.52), C.white) +
  ink(S.circle(cx - s * 0.2, cy + s * 0.34, s * 0.1)) + ink(S.circle(cx + s * 0.2, cy + s * 0.34, s * 0.1)), angle, cx, cy);
icon('HomingSparks', () => glow(128, 128, 120, '#6CF5C4', 0.35) + wisp(150, 80, 34, 14) + wisp(182, 178, 38, 20) + wisp(84, 160, 50, -14));

icon('StormAura', () => part('M86 40 L128 62 L170 40 C204 78 222 150 228 216 C202 204 186 224 162 212 C142 202 132 228 112 214 C92 202 60 224 28 216 C34 150 52 78 86 40 Z', C.indigo, { gloss: [86, 110, 12, 40, 14] }) +
  part(S.bolt(128, 142, 62), C.gold, { stroke: 8 }) + part(S.circle(128, 62, 14), C.gold, { stroke: 8 }));

icon('TetherCoil', () => [190, 162, 134].map(y => line(S.ellipse(108, y, 70, 24), '#7FBF8F', 17)).join('') +
  line('M176 126 C214 110 222 80 214 52', '#7FBF8F', 17) + hook(170, 84, 78, C.cyan, { t: 'rotate(28 170 84)' }));

icon('SkyboltCrossbow', () => rot(
  `<path d="M91.5 41.5 L64 128 L91.5 214.5" fill="none" stroke="${INK}" stroke-width="10" stroke-linejoin="round"/><path d="M91.5 41.5 L64 128 L91.5 214.5" fill="none" stroke="${C.white}" stroke-width="4" stroke-linejoin="round"/>` +
  part(S.arc(60, 128, 92, 20, 160, 20), C.forest, { stroke: 9 }) + part(S.circle(91.5, 41.5, 11), C.gold, { stroke: 7 }) + part(S.circle(91.5, 214.5, 11), C.gold, { stroke: 7 }) +
  part(S.bar(64, 128, 214, 128, 10), C.wood, { stroke: 8 }) + part(S.poly([[206, 104], [252, 128], [206, 152]]), C.cyan, { stroke: 8 }) +
  part(S.poly([[58, 128], [38, 110], [72, 110], [86, 128], [72, 146], [38, 146]]), C.gold, { stroke: 7 }), -32));

icon('ChainLightning', () => { const [back, front] = jar('#C4D2FF'); return back + stormCloud(128, 124, 22) + part(S.bolt(126, 178, 36), C.gold, { stroke: 7 }) + front +
  part(S.bolt(214, 60, 30), C.gold, { stroke: 7 }) + part(S.bolt(40, 72, 26), C.gold, { stroke: 7 }); });

icon('Starfall', () => line('M214 20 L186 50', C.gold, 7) + part(S.star(176, 62, 5, 30, 13), C.gold, { stroke: 8 }) + line('M110 18 L92 36', C.gold, 6) + part(S.star(86, 44, 5, 20, 9), C.gold, { stroke: 7 }) +
  part(S.rrect(52, 92, 156, 142, 13), C.violet) + part(S.rrect(64, 214, 144, 20, 6), C.bone, { stroke: 7 }) + part(S.rrect(52, 92, 28, 142, 8), C.indigo, { stroke: 8 }) +
  part(S.star(146, 152, 5, 40, 17), C.gold, { stroke: 7 }));

icon('SalamanderFamiliar', () => line('M96 150 L70 168 M150 118 L174 96 M118 196 L100 218 M168 164 L196 172', C.orange, 13) +
  line('M84 84 C130 60 196 84 184 130 C174 168 112 156 104 196 C100 220 132 234 160 222', C.orange, 32) +
  part(S.flame(190, 200, 22), C.gold, { dark: C.ember, stroke: 8, t: 'rotate(70 176 216)' }) +
  [[128, 84], [166, 100], [164, 142], [124, 172]].map(p => flat(S.circle(p[0], p[1], 7), C.ember)).join('') +
  part(S.ellipse(66, 86, 36, 28), C.orange, { stroke: 9, t: 'rotate(-18 66 86)' }) + eye(58, 76, 9) + ink(S.circle(36, 92, 4)));

icon('FrostWyrmBreath', () => [[196, 62, 40], [224, 112, 62], [156, 40, 14]].map(([x, y, a]) => rot(part(S.kite(x, y, 15, 34, 24), C.ice, { stroke: 8, shift: [-4, -5] }), a, x, y)).join('') +
  sparkle(232, 46, 20) + part('M30 214 C80 202 130 160 150 88 L194 128 C170 198 102 234 30 214 Z', C.bone, { gloss: [112, 168, 26, 9, -40] }) +
  rot(part(S.rrect(142, 96, 62, 20, 7), C.gold, { stroke: 8 }), 42, 173, 106) + rot(part(S.rrect(72, 180, 34, 14, 6), C.gold, { stroke: 7 }), -30, 89, 187));

icon('CrimsonSickle', () => part(S.bar(58, 232, 126, 130, 22), C.woodD) +
  part('M118 138 C62 112 66 38 142 28 C194 22 230 60 236 100 C202 72 170 62 146 76 C122 90 128 116 142 130 Z', C.red, { dark: C.crimson, gloss: [112, 64, 26, 9, -40] }) +
  part(S.drop(226, 150, 13), C.crimson, { stroke: 7 }));

icon('ThunderingBell', () => part(S.ring(128, 30, 17, 7), C.goldD, { rule: 'evenodd', stroke: 8 }) +
  part('M128 40 C92 40 78 70 76 110 C74 152 62 170 44 188 H212 C194 170 182 152 180 110 C178 70 164 40 128 40 Z', C.gold, { gloss: [102, 96, 11, 38, 10] }) +
  part(S.circle(128, 216, 16), C.goldD, { stroke: 8 }) + part(S.rrect(34, 180, 188, 24, 11), C.gold) +
  part(S.bolt(34, 86, 30), C.cyan, { stroke: 7 }) + part(S.bolt(224, 86, 30), C.cyan, { stroke: 7 }));

// ---------------------------------------------------------------- school charms and bridge passives

icon('EmberBrazier', () => line('M96 180 L72 232 M160 180 L184 232 M128 186 V232', C.dark, 13) +
  part(S.flame(92, 100, 28), C.orange, { dark: C.ember, stroke: 8 }) + part(S.flame(166, 100, 28), C.orange, { dark: C.ember, stroke: 8 }) +
  part(S.flame(128, 86, 48), C.orange, { dark: C.ember }) + flat(S.flame(128, 104, 24), C.gold) +
  part('M46 118 H210 C210 166 176 190 128 190 C80 190 46 166 46 118 Z', C.iron, { gloss: [90, 142, 22, 8, 0] }) + part(S.rrect(38, 108, 180, 22, 9), C.dark));

icon('FrostbiteRune', () => part(S.poly([[80, 26], [176, 26], [222, 92], [198, 230], [58, 230], [34, 92]]), '#8FD3F5', { dark: '#4E9CCB', gloss: [88, 70, 22, 9, -30] }) +
  `<path d="M128 70 V192 M128 122 L92 84 M128 122 L164 84" fill="none" stroke="${INK}" stroke-width="22" stroke-linecap="round" stroke-linejoin="round"/>` +
  `<path d="M128 70 V192 M128 122 L92 84 M128 122 L164 84" fill="none" stroke="${C.white}" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>` + sparkle(204, 44, 22) + sparkle(50, 196, 16));

icon('LightningRod', () => part(S.rrect(84, 210, 88, 24, 8), C.stoneD) + line('M128 212 V70', '#D98A4E', 15) +
  [116, 156].map(y => part(S.rrect(104, y, 48, 14, 6), '#B86A34', { stroke: 7 })).join('') + part(S.circle(128, 56, 20), C.gold, { stroke: 8 }) +
  part(S.bolt(196, 74, 60), C.gold, { stroke: 8, t: 'rotate(18 196 74)' }));

icon('HemomancerKnot', () => line('M52 60 C60 150 196 150 204 60', C.crimson, 19) + line('M52 196 C44 100 128 80 128 128 C128 80 212 100 204 196', C.red, 19) +
  line('M96 152 L160 104 M96 104 L160 152', C.crimson, 15) + part(S.drop(128, 214, 15), C.crimson, { stroke: 7 }));

icon('ArcaneFocus', () => glow(128, 128, 118, C.purple, 0.5) + line('M36 150 C60 96 180 76 222 106', C.cyan, 7, { outline: 7 }) +
  part(S.poly([[128, 22], [178, 84], [178, 172], [128, 234], [78, 172], [78, 84]]), C.purple, { gloss: [104, 98, 10, 34, 18] }) +
  `<path d="M78 84 L128 112 L178 84 M128 112 V234" fill="none" stroke="${C.violet}" stroke-width="5" stroke-linejoin="round"/>` +
  line('M36 150 C70 196 186 170 222 106', C.cyan, 7, { outline: 7 }) + sparkle(210, 48, 22) + sparkle(46, 206, 16));

icon('SteamGauntlet', () => [[94, 26, 13], [140, 16, 16], [176, 34, 11]].map(([x, y, r]) => part(S.circle(x, y, r), C.white, { stroke: 7, shift: [-3, -4] })).join('') +
  part('M84 232 L74 168 H182 L172 232 Z', C.steel) + part(S.rrect(42, 112, 42, 62, 18), C.orange) +
  [0, 1, 2, 3].map(i => part(S.rrect(72 + i * 28, 46, 28, 60, 12), i < 2 ? C.orange : C.ice, { stroke: 8 })).join('') +
  part('M72 96 H128 V176 H90 A18 18 0 0 1 72 158 Z', C.orange) + part('M128 96 H184 V158 A18 18 0 0 1 166 176 H128 Z', C.ice) +
  `<path d="M128 96 V176" stroke="${INK}" stroke-width="8"/>`);

icon('ThunderTotem', () => both(part('M92 50 L26 28 L48 70 L22 84 L92 96 Z', C.red, { stroke: 8 })) +
  part(S.rrect(82, 30, 92, 70, 10), C.wood) + part(S.rrect(72, 98, 112, 66, 10), '#C08A52') + part(S.rrect(82, 162, 92, 68, 10), C.wood) +
  eye(108, 58, 9) + eye(148, 58, 9) + part(S.poly([[128, 62], [142, 84], [114, 84]]), C.gold, { stroke: 7 }) +
  part(S.bolt(128, 132, 28), C.gold, { stroke: 7 }) + eye(108, 190, 8, false) + eye(148, 190, 8, false) +
  `<path d="M104 212 H152" stroke="${INK}" stroke-width="8" stroke-linecap="round"/>`);

icon('GlacierHeart', () => glow(128, 128, 118, C.ice, 0.4) + part(S.heart(128, 132, 90), C.ice, { dark: '#4E9CCB' }) +
  `<path d="M128 92 L84 122 L128 214 L172 122 Z M84 122 H172 M128 92 V214" fill="none" stroke="#4E9CCB" stroke-width="5" stroke-linejoin="round"/>` + sparkle(196, 60, 24) + sparkle(64, 78, 14));

icon('WildfireTorch', () => part(S.bar(96, 236, 130, 132, 24), C.wood) + rot(part(S.rrect(94, 126, 62, 24, 9), C.leather, { stroke: 8 }), 18, 125, 138) +
  rot(part(S.flame(146, 82, 52), C.orange, { dark: C.ember }) + flat(S.flame(146, 102, 28), C.lime), 16, 146, 100));

icon('StormglassOrb', () => part('M82 230 L98 190 H158 L174 230 Z', C.goldD) + part(S.circle(128, 110, 88), '#C4D2FF', { depth: 0.2 }) +
  stormCloud(128, 96, 24) + part(S.bolt(126, 152, 34), C.gold, { stroke: 7 }) +
  `<path d="M72 96 A62 62 0 0 1 110 46" fill="none" stroke="#fff" stroke-opacity="0.85" stroke-width="11" stroke-linecap="round"/>`);

icon('MidasLeech', () => line('M70 190 C40 110 120 60 162 110 C184 140 214 132 218 100', C.gold, 44) +
  [[72, 132], [104, 96], [146, 96], [182, 128]].map(([x, y]) => `<path d="M${x - 14} ${y - 16} L${x + 14} ${y + 16}" stroke="${C.goldD}" stroke-width="6" stroke-linecap="round"/>`).join('') +
  ink(S.circle(70, 190, 13)) + flat(S.circle(70, 190, 6), C.crimson) + coin(192, 196, 36));

icon('LuckyHorseshoe', () => glow(128, 110, 110, C.pink, 0.4) + part('M46 218 V116 A82 82 0 0 1 210 116 V218 H166 V116 A38 38 0 0 0 90 116 V218 Z', C.steel, { gloss: [84, 70, 20, 9, -40] }) +
  [[68, 190], [68, 148], [84, 92], [128, 58], [172, 92], [188, 148], [188, 190]].map(([x, y]) => ink(S.circle(x, y, 6))).join('') + sparkle(128, 150, 34, C.gold, 7) + sparkle(210, 44, 20));

// ---------------------------------------------------------------- stat charms

icon('HeartLocket', () => glow(128, 130, 120, C.orange, 0.45) + part(S.flame(128, 62, 28), C.gold, { dark: C.orange, stroke: 8 }) +
  part(S.heart(128, 146, 86), C.ember, { dark: C.crimson, gloss: [88, 110, 24, 13] }) + flat(S.heart(128, 152, 40), C.gold) +
  `<path d="M100 150 q14 -12 28 0 q14 12 28 0 M106 174 q11 -10 22 0 q11 10 22 0" fill="none" stroke="${C.ember}" stroke-width="5" stroke-linecap="round"/>`);

icon('GiantsRing', () => part(S.ring(128, 146, 88, 54), C.gold, { rule: 'evenodd', gloss: [78, 106, 20, 8, -40] }) +
  part(S.rrect(96, 40, 64, 40, 10), C.goldD, { stroke: 8 }) + part(S.poly([[128, 18], [160, 44], [128, 74], [96, 44]]), C.red, { gloss: [116, 38, 8, 5] }));

icon('IronHide', () => part('M60 36 L104 58 L152 58 L196 36 L190 96 L228 122 L196 152 L206 218 L152 198 L128 230 L104 198 L50 218 L60 152 L28 122 L66 96 Z', '#7FA37A', { gloss: [96, 100, 22, 10, -30] }) +
  `<path d="M128 70 V196" stroke="${INK}" stroke-width="6" stroke-dasharray="14 12" stroke-linecap="round"/>` +
  [[88, 128], [170, 110], [160, 166], [96, 176]].map(([x, y]) => flat(S.circle(x, y, 8), '#5E8262')).join(''));

icon('GaleFeather', () => line('M46 232 L76 196', C.goldD, 9) + part(S.leaf(60, 216, 208, 30, 96), C.white, { dark: '#B9C9DD', gloss: [118, 104, 28, 9, -46] }) +
  flat(S.leaf(150, 116, 208, 30, 46), C.gold) + `<path d="M66 208 L200 40" stroke="${C.goldD}" stroke-width="6" stroke-linecap="round"/>` +
  `<path d="M96 196 L112 172 M70 164 L94 158 M132 160 L150 138" stroke="${INK}" stroke-width="6" stroke-linecap="round"/>`);

icon('Lodestone', () => { const [back, front] = jar('#C8F0D8'); return back + both(part('M100 124 L86 92 L116 112 Z', C.crimson, { stroke: 7 })) + part(S.circle(128, 152, 42), C.red, { stroke: 8 }) +
  flat(S.ellipse(112, 144, 11, 13), C.white) + flat(S.ellipse(144, 144, 11, 13), C.white) + ink(S.circle(115, 147, 5)) + ink(S.circle(141, 147, 5)) +
  `<path d="M104 166 Q128 190 152 166 Z" fill="${C.white}" stroke="${INK}" stroke-width="6" stroke-linejoin="round"/>` + front + coin(196, 204, 26, false); });

icon('FairyDust', () => sparkle(128, 30, 26, C.gold, 6) + sparkle(184, 44, 18) + sparkle(78, 46, 16) +
  part('M82 100 L66 58 L104 78 L128 50 L152 78 L190 58 L174 100 Z', C.pink, { stroke: 9 }) +
  part('M74 110 C40 150 44 230 128 230 C212 230 216 150 182 110 Z', C.pink, { dark: '#C25DA0', gloss: [88, 150, 13, 30, 16] }) +
  part(S.rrect(80, 94, 96, 22, 10), C.gold, { stroke: 8 }) + part(S.star(128, 172, 5, 32, 14), C.gold, { stroke: 7 }));

icon('MendingMoss', () => both(part(S.circle(68, 60, 30), '#B98A5A') + flat(S.circle(68, 60, 14), C.pink)) + part(S.ellipse(128, 226, 64, 34), '#B98A5A') +
  part(S.circle(128, 122, 76), '#B98A5A', { gloss: [92, 80, 22, 11] }) + part(S.ellipse(128, 146, 34, 27), C.bone, { stroke: 7 }) + ink(S.ellipse(128, 134, 12, 9)) +
  `<path d="M128 142 V156 M114 160 Q128 170 142 160" fill="none" stroke="${INK}" stroke-width="6" stroke-linecap="round"/>` + eye(100, 110, 10) + eye(156, 110, 10) +
  part(S.leaf(150, 62, 206, 22, 44), C.green, { stroke: 7 }));

icon('SagesLens', () => line('M186 170 C222 196 214 232 176 238', C.gold, 7, { outline: 7 }) + part(S.circle(128, 112, 90), C.gold, { gloss: [76, 60, 20, 8, -40] }) +
  part(S.circle(128, 112, 66), C.glass, { stroke: 7, depth: 0.2 }) + part(S.circle(128, 112, 42), C.amber, { stroke: 7 }) + ink(S.circle(128, 112, 22)) + flat(S.circle(116, 100, 10), '#ffffff'));

icon('KrakenTentacle', () => line('M72 240 C50 150 110 104 152 128 C196 152 206 86 164 66 C136 54 112 76 126 100', '#C05C8E', 38) +
  [[80, 196], [92, 152], [124, 130], [164, 150], [186, 110]].map(([x, y]) => part(S.circle(x, y, 9), C.pink, { stroke: 5, shift: [-2, -2] })).join(''));

icon('LeechJar', () => { const [back, front] = jar('#C8F0D8'); return back + line('M100 196 C96 150 156 160 158 116', '#8E2438', 30) +
  `<path d="M104 176 L120 168 M118 156 L138 152 M140 140 L156 132" stroke="#5E1422" stroke-width="5" stroke-linecap="round"/>` + flat(S.circle(152, 108, 4), C.white) + flat(S.circle(164, 112, 4), C.white) + front; });

icon('Whetstone', () => part(S.poly([[30, 156], [176, 112], [226, 142], [80, 190]]), C.stone, { shift: [0, 0] }) + part(S.poly([[30, 156], [80, 190], [80, 226], [30, 192]]), C.stoneD) +
  part(S.poly([[80, 190], [226, 142], [226, 178], [80, 226]]), '#858EA2') +
  rot(part(S.poly([[128, 20], [150, 46], [150, 130], [106, 130], [106, 46]]), C.steel, { gloss: [118, 64, 6, 28, 0] }) + part(S.rrect(92, 126, 72, 18, 7), C.gold, { stroke: 7 }) +
    part(S.rrect(116, 142, 24, 34, 5), C.leather, { stroke: 7 }), 58, 128, 100) + sparkle(74, 126, 26, C.gold, 6) + sparkle(40, 96, 14, C.gold, 5));

icon('QuickDraw', () => streaks(60, 190, [34, 46], 28) + both(part(S.leaf(126, 132, 26, 40, 76), '#B8F5FF', { dark: C.pink, stroke: 8 }) + part(S.leaf(126, 140, 56, 214, 52), '#B8F5FF', { dark: C.pink, stroke: 8 })) +
  part(S.ellipse(128, 136, 12, 30), C.gold, { stroke: 7 }) + sparkle(128, 70, 16));

icon('LuckyCoin', () => coin(128, 128, 96, false) + clover(128, 120, 66, C.green) + sparkle(210, 50, 22));

icon('WizardHourglass', () => hourglass(C.purple, { frame: C.gold }) + part(S.star(214, 60, 5, 30, 13), C.gold, { stroke: 7 }) + sparkle(40, 196, 18));

icon('VampireFang', () => line('M36 26 C56 72 100 82 128 82 C156 82 200 72 220 26', C.steel, 7, { outline: 7 }) +
  part('M92 104 H164 C164 156 148 204 128 238 C108 204 92 156 92 104 Z', C.bone, { gloss: [112, 140, 8, 30, 4] }) + flat('M112 196 C118 212 123 226 128 238 C133 226 138 212 144 196 Z', C.crimson) +
  part(S.rrect(84, 80, 88, 30, 9), C.steel, { stroke: 8 }));

icon('SoulHarvest', () => glow(128, 140, 110, C.cyan, 0.45) + part(S.ring(128, 34, 20, 9), C.iron, { rule: 'evenodd', stroke: 8 }) + part('M90 54 H166 L180 86 H76 Z', C.iron) +
  part(S.rrect(78, 84, 100, 122, 9), '#16384A') + part(S.flame(128, 158, 30), C.cyan, { dark: C.cyanD, stroke: 7 }) + flat(S.flame(128, 170, 15), C.white) +
  `<path d="M78 84 V206 M178 84 V206 M128 84 V100 M128 196 V206" stroke="${INK}" stroke-width="8"/>` + part(S.rrect(68, 202, 120, 26, 8), C.iron));

icon('HuntersEye', () => part('M18 128 C56 54 200 54 238 128 C200 202 56 202 18 128 Z', C.amber, { gloss: [76, 88, 22, 8, -24] }) + part(S.circle(128, 128, 56), C.lime, { stroke: 8 }) +
  ink(S.ellipse(128, 128, 15, 48)) + flat(S.circle(108, 104, 12), '#ffffff'));

icon('ThornMail', () => [[44, 60, -40], [212, 60, 40], [34, 132, -80], [222, 132, 80], [128, 30, 0]].map(([x, y, a]) => rot(part(S.poly([[x - 12, y + 16], [x, y - 22], [x + 12, y + 16]]), C.bone, { stroke: 7 }), a, x, y)).join('') +
  part('M72 44 L104 60 H152 L184 44 L214 80 L190 106 V198 C170 216 150 224 128 226 C106 224 86 216 66 198 V106 L42 80 Z', '#5FA873', { gloss: [96, 110, 12, 34, 8] }) +
  part(S.leaf(98, 176, 158, 96, 54), C.lime, { stroke: 8 }) + `<path d="M104 170 L152 102" stroke="${C.forest}" stroke-width="6" stroke-linecap="round"/>`);

icon('WardShell', () => glow(128, 128, 122, C.cyan, 0.45) + part(S.ellipse(128, 132, 100, 88), C.forest) + part(S.ellipse(128, 124, 84, 72), C.green, { stroke: 8, gloss: [92, 86, 22, 9] }) +
  `<path d="${S.ngon(128, 124, 6, 34)} M128 90 V54 M157 107 L196 84 M157 141 L196 166 M128 158 V194 M99 141 L60 166 M99 107 L60 84" fill="none" stroke="${C.forest}" stroke-width="7" stroke-linejoin="round" stroke-linecap="round"/>`);

icon('WindSigil', () => part(S.circle(128, 128, 104), '#1E5A6E', { gloss: [84, 70, 24, 10] }) + `<circle cx="128" cy="128" r="84" fill="none" stroke="${C.cyanD}" stroke-width="5"/>` +
  [0, 120, 240].map(a => rot(`<path d="M128 128 C128 84 164 62 196 84 C176 78 160 90 158 108" fill="none" stroke="${a ? C.cyan : C.white}" stroke-width="13" stroke-linecap="round"/>`, a)).join(''));

icon('BerserkerBlood', () => both(part('M96 84 C60 86 40 62 38 30 C56 50 78 56 104 58 Z', C.bone, { stroke: 8 })) + part(S.rrect(104, 54, 48, 50, 6), C.glass, { depth: 0.2 }) +
  part(S.circle(128, 160, 74), C.red, { dark: C.crimson, gloss: [94, 126, 20, 10] }) + [[150, 180, 9], [116, 196, 6], [156, 150, 5]].map(([x, y, r]) => flat(S.circle(x, y, r), C.rose)).join('') +
  part(S.rrect(98, 30, 60, 30, 9), C.wood));

icon('BloodMoonChalice', () => part(S.circle(160, 78, 62), C.red, { dark: C.crimson, stroke: 8 }) + [[142, 60, 10], [180, 92, 13], [176, 52, 6]].map(([x, y, r]) => flat(S.circle(x, y, r), C.crimson)).join('') +
  part(S.rrect(114, 168, 28, 44, 4), C.goldD) + part('M66 100 H190 C190 154 164 178 128 178 C92 178 66 154 66 100 Z', C.gold, { gloss: [92, 132, 9, 24, 8] }) +
  part(S.ellipse(128, 100, 62, 14), C.crimson, { stroke: 8, shift: [0, 0] }) + part(S.rrect(80, 206, 96, 26, 10), C.gold));

icon('PhoenixFeather', () => glow(128, 128, 120, C.orange, 0.4) + line('M40 236 L74 198', C.goldD, 9) + part(S.leaf(58, 218, 210, 28, 100), C.orange, { dark: C.ember, gloss: [116, 104, 28, 9, -46] }) +
  flat(S.leaf(84, 190, 198, 46, 44), C.gold) + `<path d="M64 212 L202 36" stroke="${C.ember}" stroke-width="6" stroke-linecap="round"/>` +
  part(S.flame(214, 78, 16), C.gold, { dark: C.orange, stroke: 7, t: 'rotate(40 214 78)' }) + part(S.flame(60, 128, 14), C.gold, { dark: C.orange, stroke: 7 }) + sparkle(190, 190, 18, C.gold));

icon('HeartstoneCore', () => glow(128, 128, 126, C.cyan, 0.6) + [0, 90, 180, 270].map(a => rot(flat(S.kite(128, 22, 7, 16, 16), C.white), a + 45)).join('') +
  line('M34 148 C60 100 196 100 222 148', C.white, 6, { outline: 6 }) + part(S.kite(128, 128, 62, 100, 100), C.cyan, { gloss: [106, 84, 9, 30, 24] }) +
  `<path d="M66 128 L128 112 L190 128 M128 28 V228" fill="none" stroke="${C.cyanD}" stroke-width="5" stroke-linejoin="round"/>` + line('M34 148 C70 196 186 196 222 148', C.white, 6, { outline: 6 }));
