// Hookfall - the wind loop, heartbeat and music stems, synthesised from waveform maths (oscillators, noise, filters,
// envelopes). No recorded samples and no AI audio model. The one-shot effects are Kenney CC0 recordings since 2026-11-08
// (Tools/Audio/use_kenney.js). The AudioManager picks files up by name (Docs/Credits.md).
// Run from the project root: node Tools/Audio/make_sfx.js
// AI disclosure: written with Claude (Anthropic) on 2026-11-08 at Esad's request. Prompts and changes are recorded in Docs/PromptLog.md.
const fs = require('fs');
const path = require('path');

const RATE = 44100;
const root = process.cwd();
const sfxDir = path.join(root, 'Assets', '_Hookfall', 'Audio', 'SFX');
const musDir = path.join(root, 'Assets', '_Hookfall', 'Audio', 'Music');

// ---------------------------------------------------------------- DSP helpers

let seed = 12345;
const noise = () => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x3fffffff - 1; };
const sine = p => Math.sin(2 * Math.PI * p);
const saw = p => 2 * (p - Math.floor(p + 0.5));
const square = p => (p % 1) < 0.5 ? 1 : -1;
const tri = p => 1 - 4 * Math.abs((p % 1) - 0.5);
const clamp = (x, a, b) => Math.max(a, Math.min(b, x));
const lerp = (a, b, t) => a + (b - a) * t;

/** Attack-decay envelope: rises over a seconds, falls with an exponential of rate k. */
const env = (t, a, k) => (t < a ? t / a : Math.exp(-(t - a) * k));

function buffer(seconds) { return new Float32Array(Math.floor(seconds * RATE)); }

/** Adds an oscillator whose frequency follows freq(t); returns the buffer for chaining. */
function osc(buf, wave, freq, amp, start = 0, length = null) {
  let phase = 0;
  const s0 = Math.floor(start * RATE);
  const n = length == null ? buf.length - s0 : Math.min(buf.length - s0, Math.floor(length * RATE));
  for (let i = 0; i < n; i++) {
    const t = i / RATE;
    phase += freq(t) / RATE;
    buf[s0 + i] += wave(phase) * amp(t);
  }
  return buf;
}

/** Adds noise through a one-pole low-pass (cutoff(t) in Hz) and an optional high-pass. */
function noiseBand(buf, cutoff, amp, start = 0, length = null, highpass = 0) {
  let lp = 0, hpPrev = 0, hpOut = 0;
  const s0 = Math.floor(start * RATE);
  const n = length == null ? buf.length - s0 : Math.min(buf.length - s0, Math.floor(length * RATE));
  for (let i = 0; i < n; i++) {
    const t = i / RATE;
    const a = 1 - Math.exp(-2 * Math.PI * cutoff(t) / RATE);
    lp += (noise() - lp) * a;
    let v = lp;
    if (highpass > 0) {
      const rc = 1 / (2 * Math.PI * highpass), dt = 1 / RATE, al = rc / (rc + dt);
      hpOut = al * (hpOut + v - hpPrev);
      hpPrev = v;
      v = hpOut;
    }
    buf[s0 + i] += v * amp(t);
  }
  return buf;
}

function lowpass(buf, cutoff) {
  let lp = 0;
  const a = 1 - Math.exp(-2 * Math.PI * cutoff / RATE);
  for (let i = 0; i < buf.length; i++) { lp += (buf[i] - lp) * a; buf[i] = lp; }
  return buf;
}

function drive(buf, amount) {
  for (let i = 0; i < buf.length; i++) buf[i] = Math.tanh(buf[i] * amount);
  return buf;
}

/** Fades the start and end so a loop has no click, then normalises to peak. */
function finish(buf, peak = 0.89, fade = 0.004) {
  const f = Math.floor(fade * RATE);
  for (let i = 0; i < f && i < buf.length; i++) { buf[i] *= i / f; buf[buf.length - 1 - i] *= i / f; }
  let max = 0;
  for (const v of buf) max = Math.max(max, Math.abs(v));
  if (max > 0) for (let i = 0; i < buf.length; i++) buf[i] *= peak / max;
  return buf;
}

/** Makes the end of a loop blend into its start. */
function loopify(buf, crossfade = 0.25) {
  const n = Math.floor(crossfade * RATE);
  const out = buf.slice(0, buf.length - n);
  for (let i = 0; i < n; i++) {
    const t = i / n;
    out[i] = buf[i] * t + buf[buf.length - n + i] * (1 - t);
  }
  return out;
}

function writeWav(file, buf) {
  const data = Buffer.alloc(buf.length * 2);
  for (let i = 0; i < buf.length; i++) data.writeInt16LE(Math.round(clamp(buf[i], -1, 1) * 32767), i * 2);
  const header = Buffer.alloc(44);
  header.write('RIFF', 0); header.writeUInt32LE(36 + data.length, 4); header.write('WAVE', 8);
  header.write('fmt ', 12); header.writeUInt32LE(16, 16); header.writeUInt16LE(1, 20); header.writeUInt16LE(1, 22);
  header.writeUInt32LE(RATE, 24); header.writeUInt32LE(RATE * 2, 28); header.writeUInt16LE(2, 32); header.writeUInt16LE(16, 34);
  header.write('data', 36); header.writeUInt32LE(data.length, 40);
  fs.writeFileSync(file, Buffer.concat([header, data]));
  console.log('wrote', path.relative(root, file), (buf.length / RATE).toFixed(2) + ' s');
}

// ---------------------------------------------------------------- sound effects

const sfx = {
  // Soft air past the ears: low-passed noise, slow gusts, quiet. Seamless loop; volume follows speed in game.
  WindLoop() {
    const b = buffer(8.25);
    noiseBand(b, t => 260 + 160 * Math.sin(t * 2 * Math.PI / 8) ** 2, t => 0.7 + 0.3 * Math.sin(t * 2 * Math.PI / 4), 0, null, 60);
    return finish(lowpass(loopify(b), 900), 0.35, 0);
  },
  // A muffled heartbeat at 72 beats a minute for low health: pure sines, low-passed, seamless.
  HeartbeatLoop() {
    const b = buffer(0.833);
    for (const [start, f, a] of [[0, 52, 1], [0.2, 46, 0.7]])
      osc(b, sine, t => f * (1 + 0.4 * Math.exp(-t * 30)), t => env(t, 0.008, 14) * a, start, 0.35);
    return finish(lowpass(b, 300), 0.5, 0.002);
  },
};

// ---------------------------------------------------------------- music: three calm stems of one 16 s loop
// Reworked 2026-11-08 after the first playtest: no square or saw waves, soft attacks, low-passed, quieter.

const BPM = 100, BEAT = 60 / BPM, BARS = 8, LENGTH = BARS * 4 * BEAT;   // 19.2 s
const A = 110;
const note = semis => A * Math.pow(2, semis / 12);
const chords = [[0, 3, 7], [0, 3, 7], [-4, 0, 3], [-4, 0, 3], [-7, -3, 0], [-7, -3, 0], [-2, 2, 5], [-2, 2, 5]];   // Am F C G

function softKick(b, at, amp = 0.6) { osc(b, sine, t => 45 + 50 * Math.exp(-t * 30), t => env(t, 0.004, 10) * amp, at, 0.35); }
function shaker(b, at, amp = 0.06) { noiseBand(b, () => 7000, t => env(t, 0.004, 40) * amp, at, 0.08, 3000); }

const music = {
  Layer1() {   // a warm pad: the chord sustained, slow attack, gentle movement
    const b = buffer(LENGTH);
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      for (const semis of chords[bar]) {
        osc(b, sine, () => note(semis + 12), t => Math.min(1, t / 0.8) * Math.exp(-Math.max(0, t - 4 * BEAT + 0.4) * 6) * 0.18, t0, 4 * BEAT);
        osc(b, tri, () => note(semis + 12) * 1.003, t => Math.min(1, t / 1.2) * Math.exp(-Math.max(0, t - 4 * BEAT + 0.4) * 6) * 0.06, t0, 4 * BEAT);
      }
    }
    return finish(lowpass(b, 1800), 0.55, 0.002);
  },
  Layer2() {   // a soft pulse: round bass on each beat, a light kick and shaker
    const b = buffer(LENGTH);
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      const root = note(chords[bar][0] - 12);
      for (let k = 0; k < 4; k++) {
        osc(b, sine, () => root, t => env(t, 0.01, 5) * 0.45, t0 + k * BEAT, BEAT);
        if (k % 2 === 0) softKick(b, t0 + k * BEAT);
        shaker(b, t0 + k * BEAT + BEAT / 2);
      }
    }
    return finish(lowpass(b, 1200), 0.5, 0.002);
  },
  Layer3() {   // a gentle plucked arpeggio for later minutes and elite waves
    const b = buffer(LENGTH);
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      const c = chords[bar];
      const pattern = [c[0], c[1], c[2], c[1] + 12, c[2], c[1], c[0] + 12, c[2]];
      pattern.forEach((semis, k) =>
        osc(b, tri, () => note(semis + 24), t => env(t, 0.004, 7) * 0.16, t0 + k * BEAT / 2, BEAT));
    }
    return finish(lowpass(b, 2600), 0.4, 0.002);
  },
};

fs.mkdirSync(sfxDir, { recursive: true });
fs.mkdirSync(musDir, { recursive: true });
for (const [name, make] of Object.entries(sfx)) writeWav(path.join(sfxDir, `SFX_${name}.wav`), make());
for (const [name, make] of Object.entries(music)) writeWav(path.join(musDir, `MUS_${name}.wav`), make());
