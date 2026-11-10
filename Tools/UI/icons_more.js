// Hookfall - the remaining icons: the five power-ups, the elite traits, and the symbols the menus use.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
const { S, C, icon, ink, flat, plus, chevron, streaks, coin, snowflake, wing } = require('./icons.js');
const { part, line, glow, sparkle, rot, flipX, INK } = S;

// ---------------------------------------------------------------- power-ups (PowerUpKind order)

icon('TreasureGale', () => line('M26 96 H150 a28 28 0 1 0 -28 -28', C.white, 14) + line('M26 150 H120', C.white, 14) + line('M58 198 H104', C.white, 14) +
  coin(186, 150, 44) + coin(132, 206, 28, false) + sparkle(222, 70, 20, C.gold));
icon('HealingDew', () => glow(128, 140, 116, C.green, 0.45) + part(S.drop(128, 152, 62), C.green, { dark: C.forest, gloss: [104, 150, 14, 24, 20] }) + part(plus(128, 160, 34, 11), C.white, { stroke: 7 }));
icon('BerserkRage', () => glow(128, 140, 118, C.red, 0.45) + part(S.flame(128, 150, 70), C.red, { dark: C.crimson }) + flat(S.flame(128, 180, 34), C.orange) +
  [0, 1].map(i => { const e = flat(S.poly([[84, 128], [120, 146], [116, 160], [86, 150]]), C.white); return i ? flipX(e) : e; }).join(''));
icon('Timefrost', () => part(S.circle(128, 128, 102), C.ice, { dark: '#4E9CCB', gloss: [82, 66, 24, 10] }) + part(S.circle(128, 128, 76), C.white, { stroke: 8 }) +
  `<path d="M128 128 V78 M128 128 L162 146" fill="none" stroke="${INK}" stroke-width="11" stroke-linecap="round"/>` + ink(S.circle(128, 128, 9)) +
  [0, 90, 180, 270].map(a => rot(ink(S.rrect(124, 58, 8, 14, 3)), a)).join('') + sparkle(206, 46, 26) + sparkle(46, 208, 18));
icon('HeartstoneNova', () => glow(128, 128, 126, C.cyan, 0.6) + part(S.star(128, 128, 12, 118, 56), C.cyan, { stroke: 9 }) + flat(S.star(128, 128, 12, 70, 34), C.white));

// ---------------------------------------------------------------- elite traits

icon('Shielded', () => `<circle cx="128" cy="128" r="104" fill="${C.blue}" fill-opacity="0.28" stroke="${INK}" stroke-width="16"/><circle cx="128" cy="128" r="104" fill="none" stroke="${C.ice}" stroke-width="7"/>` +
  part(S.shield(128, 126, 58, 68), C.blue, { gloss: [100, 84, 18, 9] }));
icon('Swift', () => streaks(72, 188, [44, 30], 28) + wing(C.gold, C.goldD));
icon('Explosive', () => line('M150 70 C160 40 186 34 198 50', C.wood, 8) + part(S.star(206, 46, 8, 30, 13), C.gold, { stroke: 7 }) + part(S.rrect(124, 60, 44, 30, 6), C.iron, { stroke: 8 }) +
  part(S.circle(124, 150, 82), '#3B4256', { gloss: [88, 110, 22, 11] }));
icon('Splitting', () => part(S.circle(88, 150, 58), C.magenta, { gloss: [66, 124, 16, 8] }) + part(S.circle(172, 110, 50), C.pink, { dark: C.magenta, gloss: [152, 88, 14, 7] }) +
  ink(S.circle(72, 146, 7)) + ink(S.circle(100, 146, 7)) + ink(S.circle(160, 106, 6)) + ink(S.circle(184, 106, 6)));

// ---------------------------------------------------------------- menu symbols

icon('Arrow', () => part(S.poly([[40, 44], [224, 128], [40, 212], [84, 128]]), C.white));
icon('Play', () => part(S.poly([[72, 36], [212, 128], [72, 220]]), C.cyan, { gloss: [104, 96, 12, 34, -28] }));
icon('Back', () => part(S.arrow(226, 128, 30, 128, 44, 116), C.white));
icon('Quit', () => part(S.rrect(46, 30, 112, 196, 10), C.wood) + ink(S.circle(134, 132, 9)) + part(S.arrow(124, 128, 236, 128, 30, 82), C.red));
icon('Credits', () => part(S.rrect(60, 46, 136, 176, 8), C.bone) + part(S.rrect(46, 26, 164, 34, 16), '#E2D4AE') + part(S.rrect(46, 204, 164, 34, 16), '#E2D4AE') +
  [92, 122, 152].map(y => `<path d="M86 ${y} H170" stroke="${C.woodD}" stroke-width="8" stroke-linecap="round"/>`).join('') + `<path d="M86 182 H138" stroke="${C.woodD}" stroke-width="8" stroke-linecap="round"/>`);
icon('Language', () => part(S.circle(128, 128, 102), C.blue, { gloss: [82, 68, 24, 10] }) +
  `<path d="M26 128 H230 M128 26 V230 M128 26 C70 70 70 186 128 230 M128 26 C186 70 186 186 128 230 M48 72 C96 96 160 96 208 72 M48 184 C96 160 160 160 208 184" fill="none" stroke="${INK}" stroke-width="7" stroke-linecap="round"/>`);
icon('Sound', () => part(S.poly([[40, 96], [84, 96], [140, 44], [140, 212], [84, 160], [40, 160]]), C.white) + [[30, 164], [58, 172]].map(([r, x]) => line(`M${x} ${128 - r} A${r} ${r} 0 0 1 ${x} ${128 + r}`, C.cyan, 13)).join(''));
icon('Music', () => line('M96 190 V58 L196 38 V170', C.white, 14) + part(S.ellipse(74, 192, 32, 24), C.cyan, { stroke: 9 }) + part(S.ellipse(174, 172, 32, 24), C.cyan, { stroke: 9 }));
icon('Sensitivity', () => part(S.rrect(72, 44, 112, 184, 56), C.white) + `<path d="M72 124 H184 M128 44 V124" stroke="${INK}" stroke-width="8"/>` + part(S.rrect(116, 66, 24, 40, 12), C.cyan, { stroke: 7 }) +
  part(S.arrow(60, 128, 14, 128, 12, 36), C.gold, { stroke: 7 }) + part(S.arrow(196, 128, 242, 128, 12, 36), C.gold, { stroke: 7 }));
icon('Screen', () => part(S.rrect(28, 44, 200, 136, 12), C.dark) + flat(S.rrect(44, 60, 168, 104, 4), C.cyan) + part(S.rrect(112, 178, 32, 28, 3), C.iron, { stroke: 8 }) + part(S.rrect(76, 202, 104, 22, 9), C.iron, { stroke: 8 }));
icon('Trophy', () => part(S.star(128, 110, 5, 86, 38), C.gold, { gloss: [104, 80, 16, 8] }) + part(S.poly([[96, 168], [84, 234], [128, 208], [172, 234], [160, 168]]), C.red, { stroke: 9 }));
icon('Quest', () => part(S.rrect(52, 36, 152, 196, 12), C.bone) + part(S.rrect(92, 22, 72, 34, 10), C.gold, { stroke: 8 }) +
  [96, 140, 184].map((y, i) => (i < 2 ? part(S.poly([[76, y], [86, y - 10], [96, y + 2], [116, y - 22], [126, y - 12], [96, y + 22]]), C.green, { stroke: 6, shift: [-2, -3] })
    : part(S.rrect(78, y - 14, 30, 30, 5), C.white, { stroke: 6 })) + `<path d="M138 ${y} H184" stroke="${C.woodD}" stroke-width="8" stroke-linecap="round"/>`).join(''));
