// Hookfall - placeholder sound effects and music, synthesised from waveform maths (oscillators, noise, filters,
// envelopes). No recorded samples and no AI audio model: every sound is a few lines of DSP below. They stand in until
// licensed files replace them (Docs/Credits.md); the AudioManager picks files up by name either way.
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
  HookThrow() {   // a rising whoosh
    const b = buffer(0.28);
    noiseBand(b, t => 800 + t * 9000, t => env(t, 0.05, 14) * 0.9, 0, null, 400);
    osc(b, sine, t => 300 + t * 1500, t => env(t, 0.01, 20) * 0.15);
    return finish(b);
  },
  HookAttach() {   // a metallic clank with a short ring
    const b = buffer(0.45);
    noiseBand(b, () => 6000, t => env(t, 0.001, 80) * 1.0);
    for (const [f, a] of [[880, 0.5], [1342, 0.35], [2210, 0.25], [3120, 0.15]])
      osc(b, sine, () => f, t => env(t, 0.001, 12) * a);
    osc(b, sine, t => 140 * Math.exp(-t * 8), t => env(t, 0.002, 18) * 0.6);
    return finish(b);
  },
  HookRelease() {   // a short falling swish
    const b = buffer(0.18);
    noiseBand(b, t => 5000 - t * 20000, t => env(t, 0.01, 22) * 0.8, 0, null, 600);
    return finish(b, 0.6);
  },
  WeaponHit() {   // a quick thwack
    const b = buffer(0.1);
    noiseBand(b, () => 3500, t => env(t, 0.001, 60));
    osc(b, tri, t => 320 * Math.exp(-t * 25), t => env(t, 0.001, 40) * 0.8);
    return finish(b, 0.7);
  },
  EnemyDeath() {   // a squeaky pop
    const b = buffer(0.3);
    osc(b, square, t => 1300 * Math.exp(-t * 9), t => env(t, 0.002, 14) * 0.35);
    noiseBand(b, t => 4000 * Math.exp(-t * 6), t => env(t, 0.001, 22) * 0.7);
    return finish(lowpass(b, 7000), 0.75);
  },
  XPPickup() {   // a bright blip
    const b = buffer(0.1);
    osc(b, sine, t => 1200 + t * 7000, t => env(t, 0.003, 40));
    return finish(b, 0.5);
  },
  GoldPickup() {   // two coin tones
    const b = buffer(0.22);
    osc(b, square, () => 988, t => env(t, 0.002, 30) * 0.3, 0, 0.06);
    osc(b, square, () => 1319, t => env(t, 0.002, 14) * 0.3, 0.06);
    return finish(lowpass(b, 6000), 0.55);
  },
  ChestOpen() {   // a wooden creak, then a rising chime
    const b = buffer(1.0);
    osc(b, saw, t => 90 + Math.sin(t * 40) * 15, t => (t < 0.3 ? env(t, 0.05, 6) : 0) * 0.3, 0, 0.3);
    [523, 659, 784, 1047].forEach((f, i) => osc(b, tri, () => f, t => env(t, 0.005, 4) * 0.35, 0.25 + i * 0.08));
    return finish(lowpass(b, 8000));
  },
  TowerCacheBeam() {   // a rising shimmer
    const b = buffer(1.6);
    [440, 554, 659, 880].forEach((f, i) => osc(b, sine, t => f * (1 + t * 0.12), t => env(t, 0.5, 2.5) * 0.25 * (1 + 0.3 * Math.sin(t * 30 + i))));
    noiseBand(b, () => 9000, t => env(t, 0.6, 3) * 0.1, 0, null, 3000);
    return finish(b);
  },
  TowerCacheOpen() {   // a big bell chord
    const b = buffer(1.8);
    [392, 494, 587, 784, 988].forEach((f, i) => {
      osc(b, sine, () => f, t => env(t, 0.003, 2.2) * 0.3, i * 0.04);
      osc(b, sine, () => f * 2.76, t => env(t, 0.003, 6) * 0.08, i * 0.04);
    });
    return finish(b);
  },
  LevelUp() {   // an arpeggio up
    const b = buffer(0.75);
    [523, 659, 784, 1047, 1319].forEach((f, i) => osc(b, square, () => f, t => env(t, 0.004, 9) * 0.22, i * 0.07));
    return finish(lowpass(b, 7000));
  },
  PlayerHurt() {   // a low, crunchy thud
    const b = buffer(0.3);
    osc(b, sine, t => 110 * Math.exp(-t * 6), t => env(t, 0.002, 12));
    noiseBand(b, () => 1500, t => env(t, 0.001, 30) * 0.8);
    return finish(drive(b, 2.5), 0.85);
  },
  GolemSlam() {   // a deep boom with rubble
    const b = buffer(0.9);
    osc(b, sine, t => 70 * Math.exp(-t * 2.5), t => env(t, 0.004, 4));
    noiseBand(b, t => 900 * Math.exp(-t * 3), t => env(t, 0.003, 5) * 0.9);
    return finish(drive(b, 1.8));
  },
  BoulderBreak() {   // a stony crunch
    const b = buffer(0.4);
    for (let k = 0; k < 6; k++) noiseBand(b, () => 2500 - k * 250, t => env(t, 0.001, 30) * 0.6, k * 0.035, 0.2);
    osc(b, sine, t => 90 * Math.exp(-t * 5), t => env(t, 0.002, 12) * 0.5);
    return finish(b);
  },
  EliteWave() {   // a war horn with vibrato
    const b = buffer(1.6);
    osc(b, saw, t => 110 * (1 + 0.012 * Math.sin(t * 2 * Math.PI * 5.5)), t => clamp(t / 0.15, 0, 1) * Math.exp(-Math.max(0, t - 1.1) * 6) * 0.5);
    osc(b, saw, t => 165 * (1 + 0.012 * Math.sin(t * 2 * Math.PI * 5.3)), t => clamp(t / 0.2, 0, 1) * Math.exp(-Math.max(0, t - 1.1) * 6) * 0.35);
    return finish(lowpass(b, 1400));
  },
  RunEnd() {   // a falling, fading phrase
    const b = buffer(1.8);
    [659, 587, 523, 440].forEach((f, i) => osc(b, tri, () => f, t => env(t, 0.01, 2.5) * 0.35, i * 0.3));
    return finish(lowpass(b, 5000));
  },
  WindLoop() {   // airy noise with slow gusts, seamless
    const b = buffer(6.25);
    noiseBand(b, t => 700 + 500 * Math.sin(t * 2 * Math.PI / 6) ** 2, t => 0.6 + 0.4 * Math.sin(t * 2 * Math.PI / 3), 0, null, 120);
    return finish(loopify(b), 0.7, 0);
  },
  HeartbeatLoop() {   // lub-dub at 75 beats a minute, seamless
    const b = buffer(0.8);
    for (const [start, f, a] of [[0, 55, 1], [0.18, 48, 0.75]])
      osc(b, sine, t => f * (1 + Math.exp(-t * 30)), t => env(t, 0.005, 18) * a, start, 0.3);
    return finish(b, 0.9, 0.002);
  },
};

// ---------------------------------------------------------------- music: three stems of one 16 s loop

const BPM = 120, BEAT = 60 / BPM, BARS = 8, LENGTH = BARS * 4 * BEAT;   // 16 s
const A = 110, scale = [0, 3, 5, 7, 10, 12];   // A minor pentatonic, in semitones
const note = semis => A * Math.pow(2, semis / 12);

function kick(b, at, amp = 1) { osc(b, sine, t => 50 + 120 * Math.exp(-t * 35), t => env(t, 0.002, 9) * amp, at, 0.4); }
function snare(b, at, amp = 0.6) { noiseBand(b, () => 6000, t => env(t, 0.001, 18) * amp, at, 0.25, 900); osc(b, tri, () => 190, t => env(t, 0.001, 25) * amp * 0.5, at, 0.15); }
function hat(b, at, amp = 0.25) { noiseBand(b, () => 12000, t => env(t, 0.001, 60) * amp, at, 0.06, 6000); }
function tom(b, at, f, amp = 0.6) { osc(b, sine, t => f * (1 + Math.exp(-t * 20)), t => env(t, 0.002, 10) * amp, at, 0.35); }

const music = {
  Layer1() {   // drums: driving kick and hats, the base of every run
    const b = buffer(LENGTH);
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      [0, 1.5, 2, 3].forEach(x => kick(b, t0 + x * BEAT));
      for (let k = 0; k < 8; k++) hat(b, t0 + k * BEAT / 2, k % 2 ? 0.18 : 0.28);
    }
    return finish(b, 0.85, 0.001);
  },
  Layer2() {   // snare backbeat, toms and a pulsing bass
    const b = buffer(LENGTH);
    const line = [0, 0, 7, 5, 3, 3, 10, 7];
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      snare(b, t0 + BEAT); snare(b, t0 + 3 * BEAT);
      if (bar % 2 === 1) [3.25, 3.5, 3.75].forEach((x, i) => tom(b, t0 + x * BEAT, 140 - i * 25));
      const root = note(line[bar] - 12);
      for (let k = 0; k < 8; k++)
        osc(b, saw, () => root, t => env(t, 0.005, 9) * 0.5, t0 + k * BEAT / 2, BEAT / 2);
    }
    return finish(lowpass(b, 2500), 0.8, 0.001);
  },
  Layer3() {   // a high arpeggio lead for the hardest minutes and elite waves
    const b = buffer(LENGTH);
    const chords = [[0, 3, 7, 12], [0, 3, 7, 12], [7, 10, 14, 19], [5, 9, 12, 17], [3, 7, 10, 15], [3, 7, 10, 15], [10, 14, 17, 22], [7, 10, 14, 19]];
    for (let bar = 0; bar < BARS; bar++) {
      const t0 = bar * 4 * BEAT;
      for (let k = 0; k < 16; k++) {
        const semis = chords[bar][k % 4] + (k >= 8 ? 12 : 0) + 12;
        osc(b, square, () => note(semis), t => env(t, 0.003, 14) * 0.18, t0 + k * BEAT / 4, BEAT / 4);
      }
    }
    return finish(lowpass(b, 4500), 0.7, 0.001);
  },
};

fs.mkdirSync(sfxDir, { recursive: true });
fs.mkdirSync(musDir, { recursive: true });
for (const [name, make] of Object.entries(sfx)) writeWav(path.join(sfxDir, `SFX_${name}.wav`), make());
for (const [name, make] of Object.entries(music)) writeWav(path.join(musDir, `MUS_${name}.wav`), make());
