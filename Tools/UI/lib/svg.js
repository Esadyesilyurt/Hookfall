// Hookfall - a small SVG drawing kit for the UI icons and skin: path builders, colour maths and the cel-shaded
// "sticker" part (dark tone, lit tone shifted up-left, ink outline) that every icon is made of.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.

const INK = '#141826';
let serial = 0;
const id = prefix => `${prefix}${++serial}`;
const n = v => Math.round(v * 100) / 100;
const rad = deg => deg * Math.PI / 180;

// ---------------------------------------------------------------- colour

function rgb(hex) {
  const h = hex.replace('#', '');
  const full = h.length === 3 ? h.split('').map(c => c + c).join('') : h;
  return [0, 2, 4].map(i => parseInt(full.slice(i, i + 2), 16));
}
const hex = c => '#' + c.map(v => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
const mix = (a, b, t) => { const x = rgb(a), y = rgb(b); return hex(x.map((v, i) => v + (y[i] - v) * t)); };
const darken = (c, t = 0.3) => mix(c, '#0a0c1c', t);
const lighten = (c, t = 0.35) => mix(c, '#ffffff', t);

// ---------------------------------------------------------------- paths (each returns a path "d" string)

const poly = (points, closed = true) => points.map((p, i) => `${i ? 'L' : 'M'}${n(p[0])} ${n(p[1])}`).join(' ') + (closed ? ' Z' : '');

const circle = (cx, cy, r) => `M${n(cx - r)} ${n(cy)} A${n(r)} ${n(r)} 0 1 0 ${n(cx + r)} ${n(cy)} A${n(r)} ${n(r)} 0 1 0 ${n(cx - r)} ${n(cy)} Z`;

const ellipse = (cx, cy, rx, ry) => `M${n(cx - rx)} ${n(cy)} A${n(rx)} ${n(ry)} 0 1 0 ${n(cx + rx)} ${n(cy)} A${n(rx)} ${n(ry)} 0 1 0 ${n(cx - rx)} ${n(cy)} Z`;

function rrect(x, y, w, h, r) {
  r = Math.min(r, w / 2, h / 2);
  return `M${n(x + r)} ${n(y)} H${n(x + w - r)} A${n(r)} ${n(r)} 0 0 1 ${n(x + w)} ${n(y + r)} V${n(y + h - r)} A${n(r)} ${n(r)} 0 0 1 ${n(x + w - r)} ${n(y + h)} ` +
    `H${n(x + r)} A${n(r)} ${n(r)} 0 0 1 ${n(x)} ${n(y + h - r)} V${n(y + r)} A${n(r)} ${n(r)} 0 0 1 ${n(x + r)} ${n(y)} Z`;
}

/** A star or burst: `points` tips at r1, valleys at r2; rot 0 puts a tip straight up. */
function star(cx, cy, points, r1, r2, rot = 0) {
  const out = [];
  for (let i = 0; i < points * 2; i++) {
    const a = rad(rot - 90) + i * Math.PI / points, r = i % 2 ? r2 : r1;
    out.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]);
  }
  return poly(out);
}

/** A regular polygon with one corner straight up at rot 0. */
function ngon(cx, cy, sides, r, rot = 0) {
  const out = [];
  for (let i = 0; i < sides; i++) { const a = rad(rot - 90) + i * 2 * Math.PI / sides; out.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); }
  return poly(out);
}

/** A ring; draw it with rule 'evenodd'. */
const ring = (cx, cy, r1, r2) => circle(cx, cy, r1) + ' ' + circle(cx, cy, r2);

/** A thick arc band from angle a0 to a1 (degrees, 0 = up, clockwise), as a closed shape. */
function arc(cx, cy, r, a0, a1, width) {
  const pt = (radius, a) => [cx + Math.sin(rad(a)) * radius, cy - Math.cos(rad(a)) * radius];
  const ro = r + width / 2, ri = r - width / 2, large = Math.abs(a1 - a0) > 180 ? 1 : 0;
  const [x0, y0] = pt(ro, a0), [x1, y1] = pt(ro, a1), [x2, y2] = pt(ri, a1), [x3, y3] = pt(ri, a0);
  return `M${n(x0)} ${n(y0)} A${n(ro)} ${n(ro)} 0 ${large} 1 ${n(x1)} ${n(y1)} L${n(x2)} ${n(y2)} A${n(ri)} ${n(ri)} 0 ${large} 0 ${n(x3)} ${n(y3)} Z`;
}

/** A cog: `teeth` flat teeth between radius r2 (root) and r1 (tip). */
function gear(cx, cy, teeth, r1, r2) {
  const out = [], step = 360 / teeth;
  for (let i = 0; i < teeth; i++) {
    const a = i * step;
    for (const [da, r] of [[-step * 0.3, r2], [-step * 0.17, r1], [step * 0.17, r1], [step * 0.3, r2]])
      out.push([cx + Math.sin(rad(a + da)) * r, cy - Math.cos(rad(a + da)) * r]);
  }
  return poly(out);
}

/** A thick arc from a0 to a1 (degrees, 0 = up, clockwise) that ends in an arrow head, as one closed shape. */
function arcArrow(cx, cy, r, a0, a1, width, head) {
  const pt = (radius, a) => [cx + Math.sin(rad(a)) * radius, cy - Math.cos(rad(a)) * radius];
  const ro = r + width / 2, ri = r - width / 2, large = Math.abs(a1 - a0) > 180 ? 1 : 0;
  const reach = head / r * 180 / Math.PI * 0.9;   // how far past a1 the point of the head lies
  const [x0, y0] = pt(ro, a0), [x1, y1] = pt(ro, a1), [hx1, hy1] = pt(r + head / 2, a1), [tx, ty] = pt(r, a1 + reach),
    [hx2, hy2] = pt(r - head / 2, a1), [x2, y2] = pt(ri, a1), [x3, y3] = pt(ri, a0);
  return `M${n(x0)} ${n(y0)} A${n(ro)} ${n(ro)} 0 ${large} 1 ${n(x1)} ${n(y1)} L${n(hx1)} ${n(hy1)} L${n(tx)} ${n(ty)} L${n(hx2)} ${n(hy2)} L${n(x2)} ${n(y2)} A${n(ri)} ${n(ri)} 0 ${large} 0 ${n(x3)} ${n(y3)} Z`;
}

/** A heart about (cx, cy), s = half width. */
const heart = (cx, cy, s) => `M${n(cx)} ${n(cy + s * 0.95)} C${n(cx - s * 1.6)} ${n(cy - s * 0.1)} ${n(cx - s * 0.75)} ${n(cy - s * 1.25)} ${n(cx)} ${n(cy - s * 0.45)} ` +
  `C${n(cx + s * 0.75)} ${n(cy - s * 1.25)} ${n(cx + s * 1.6)} ${n(cy - s * 0.1)} ${n(cx)} ${n(cy + s * 0.95)} Z`;

/** A drop with its point up; s = radius of the round end. */
const drop = (cx, cy, s) => `M${n(cx)} ${n(cy - s * 1.7)} C${n(cx + s * 0.5)} ${n(cy - s * 0.8)} ${n(cx + s)} ${n(cy - s * 0.3)} ${n(cx + s)} ${n(cy + s * 0.3)} ` +
  `A${n(s)} ${n(s)} 0 1 1 ${n(cx - s)} ${n(cy + s * 0.3)} C${n(cx - s)} ${n(cy - s * 0.3)} ${n(cx - s * 0.5)} ${n(cy - s * 0.8)} ${n(cx)} ${n(cy - s * 1.7)} Z`;

/** A flame with its tip up; s = half width at the base. */
const flame = (cx, cy, s) => `M${n(cx)} ${n(cy - s * 2.1)} C${n(cx + s * 0.25)} ${n(cy - s * 1.3)} ${n(cx + s * 1.25)} ${n(cy - s * 0.75)} ${n(cx + s * 1.05)} ${n(cy + s * 0.25)} ` +
  `C${n(cx + s * 0.95)} ${n(cy + s * 0.95)} ${n(cx + s * 0.45)} ${n(cy + s * 1.3)} ${n(cx)} ${n(cy + s * 1.3)} C${n(cx - s * 0.5)} ${n(cy + s * 1.3)} ${n(cx - s * 1.05)} ${n(cy + s * 0.9)} ${n(cx - s * 1.05)} ${n(cy + s * 0.2)} ` +
  `C${n(cx - s * 1.05)} ${n(cy - s * 0.35)} ${n(cx - s * 0.6)} ${n(cy - s * 0.6)} ${n(cx - s * 0.5)} ${n(cy - s * 1.15)} C${n(cx - s * 0.2)} ${n(cy - s * 0.95)} ${n(cx - s * 0.15)} ${n(cy - s * 1.5)} ${n(cx)} ${n(cy - s * 2.1)} Z`;

/** A leaf or feather blade from (x0, y0) to (x1, y1), `w` wide at the middle. */
function leaf(x0, y0, x1, y1, w) {
  const mx = (x0 + x1) / 2, my = (y0 + y1) / 2, dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy) || 1;
  const px = -dy / len * w / 2, py = dx / len * w / 2;
  return `M${n(x0)} ${n(y0)} Q${n(mx + px * 2)} ${n(my + py * 2)} ${n(x1)} ${n(y1)} Q${n(mx - px * 2)} ${n(my - py * 2)} ${n(x0)} ${n(y0)} Z`;
}

/** A zigzag lightning bolt in a box, pointing down. */
const bolt = (cx, cy, s) => poly([[cx + s * 0.25, cy - s], [cx - s * 0.55, cy + s * 0.12], [cx - s * 0.05, cy + s * 0.12], [cx - s * 0.3, cy + s], [cx + s * 0.6, cy - s * 0.2], [cx + s * 0.08, cy - s * 0.2], [cx + s * 0.5, cy - s]]);

/** A kite/gem: top, right, bottom, left half sizes. */
const kite = (cx, cy, w, up, down) => poly([[cx, cy - up], [cx + w, cy], [cx, cy + down], [cx - w, cy]]);

/** A shield: flat top with shoulders, pointed bottom. */
const shield = (cx, cy, w, h) => `M${n(cx - w)} ${n(cy - h)} H${n(cx + w)} V${n(cy - h * 0.1)} C${n(cx + w)} ${n(cy + h * 0.55)} ${n(cx + w * 0.4)} ${n(cy + h * 0.85)} ${n(cx)} ${n(cy + h)} ` +
  `C${n(cx - w * 0.4)} ${n(cy + h * 0.85)} ${n(cx - w)} ${n(cy + h * 0.55)} ${n(cx - w)} ${n(cy - h * 0.1)} Z`;

/** A crescent: a disc of radius r with a disc of radius r2 at (dx, dy) cut out. */
function crescent(cx, cy, r, dx, dy, r2) {
  // Intersection points of the two circles.
  const d = Math.hypot(dx, dy), a = (r * r - r2 * r2 + d * d) / (2 * d), h = Math.sqrt(Math.max(0, r * r - a * a));
  const mx = cx + dx * a / d, my = cy + dy * a / d;
  const x1 = mx - dy * h / d, y1 = my + dx * h / d, x2 = mx + dy * h / d, y2 = my - dx * h / d;
  // The outer arc is the long way round when the chord lies on the cut-out's side; the inner arc only when the chord
  // lies beyond the cut-out's centre.
  return `M${n(x1)} ${n(y1)} A${n(r)} ${n(r)} 0 ${a > 0 ? 1 : 0} 1 ${n(x2)} ${n(y2)} A${n(r2)} ${n(r2)} 0 ${d - a < 0 ? 1 : 0} 0 ${n(x1)} ${n(y1)} Z`;
}

/** An arrow from (x0, y0) to (x1, y1): shaft `w` wide, head `head` wide. */
function arrow(x0, y0, x1, y1, w, head) {
  const dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy) || 1, ux = dx / len, uy = dy / len, px = -uy, py = ux;
  const neck = len - head * 0.9, bx = x0 + ux * neck, by = y0 + uy * neck;
  return poly([[x0 + px * w / 2, y0 + py * w / 2], [bx + px * w / 2, by + py * w / 2], [bx + px * head / 2, by + py * head / 2], [x1, y1],
    [bx - px * head / 2, by - py * head / 2], [bx - px * w / 2, by - py * w / 2], [x0 - px * w / 2, y0 - py * w / 2]]);
}

/** A thick line (capsule) from a to b. */
function bar(x0, y0, x1, y1, w) {
  const dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy) || 1, px = -dy / len * w / 2, py = dx / len * w / 2, r = w / 2;
  return `M${n(x0 + px)} ${n(y0 + py)} L${n(x1 + px)} ${n(y1 + py)} A${n(r)} ${n(r)} 0 0 1 ${n(x1 - px)} ${n(y1 - py)} L${n(x0 - px)} ${n(y0 - py)} A${n(r)} ${n(r)} 0 0 1 ${n(x0 + px)} ${n(y0 + py)} Z`;
}

// ---------------------------------------------------------------- drawing

/**
 * One cel-shaded part: the dark tone, the lit tone shifted up and to the left inside the shape (which leaves a shadow
 * along the lower right edge), an optional gloss, and the ink outline on top.
 */
function part(d, fill, o = {}) {
  const dark = o.dark || darken(fill, o.depth ?? 0.34), stroke = o.stroke ?? 10, rule = o.rule ? ` fill-rule="${o.rule}" clip-rule="${o.rule}"` : '';
  const [sx, sy] = o.shift || [-7, -9];
  const clip = id('c');
  let out = `<clipPath id="${clip}"><path d="${d}"${o.rule ? ` clip-rule="${o.rule}"` : ''}/></clipPath>`;
  if (o.flat) out += `<path d="${d}" fill="${fill}"${rule}/>`;
  else out += `<path d="${d}" fill="${dark}"${rule}/><g clip-path="url(#${clip})"><path d="${d}" fill="${fill}" transform="translate(${sx} ${sy})"${rule}/>` +
    (o.gloss ? `<ellipse cx="${n(o.gloss[0])}" cy="${n(o.gloss[1])}" rx="${n(o.gloss[2])}" ry="${n(o.gloss[3] ?? o.gloss[2] * 0.6)}" fill="#fff" fill-opacity="${o.glossAlpha ?? 0.5}" transform="rotate(${o.gloss[4] ?? -30} ${n(o.gloss[0])} ${n(o.gloss[1])})"/>` : '') + '</g>';
  if (stroke > 0) out += `<path d="${d}" fill="none" stroke="${o.ink || INK}" stroke-width="${stroke}" stroke-linejoin="round" stroke-linecap="round"/>`;
  return o.t ? `<g transform="${o.t}">${out}</g>` : out;
}

/** An outlined stroke: a coloured line with an ink line under it (ropes, whiskers, strings). */
function line(d, color, width = 8, o = {}) {
  const out = `<path d="${d}" fill="none" stroke="${o.ink || INK}" stroke-width="${width + (o.outline ?? 9)}" stroke-linecap="round" stroke-linejoin="round"/>` +
    `<path d="${d}" fill="none" stroke="${color}" stroke-width="${width}" stroke-linecap="round" stroke-linejoin="round"/>`;
  return o.t ? `<g transform="${o.t}">${out}</g>` : out;
}

/** A soft glow disc behind an icon (magic, light). */
function glow(cx, cy, r, color, alpha = 0.55) {
  const g = id('g');
  return `<radialGradient id="${g}"><stop offset="0" stop-color="${color}" stop-opacity="${alpha}"/><stop offset="1" stop-color="${color}" stop-opacity="0"/></radialGradient>` +
    `<circle cx="${n(cx)}" cy="${n(cy)}" r="${n(r)}" fill="url(#${g})"/>`;
}

/** Small sparkle (four-point star), no outline by default. */
const sparkle = (cx, cy, r, color = '#ffffff', stroke = 0) => part(star(cx, cy, 4, r, r * 0.28), color, { flat: true, stroke });

const group = (content, transform) => `<g transform="${transform}">${content}</g>`;
const rot = (content, deg, cx = 128, cy = 128) => group(content, `rotate(${deg} ${cx} ${cy})`);
const flipX = (content, cx = 128) => group(content, `translate(${cx * 2} 0) scale(-1 1)`);
const scale = (content, s, cx = 128, cy = 128) => group(content, `translate(${n(cx - cx * s)} ${n(cy - cy * s)}) scale(${s})`);

module.exports = {
  INK, id, n, rad, rgb, hex, mix, darken, lighten,
  poly, circle, ellipse, rrect, star, ngon, ring, arc, gear, arcArrow, heart, drop, flame, leaf, bolt, kite, shield, crescent, arrow, bar,
  part, line, glow, sparkle, group, rot, flipX, scale,
};
