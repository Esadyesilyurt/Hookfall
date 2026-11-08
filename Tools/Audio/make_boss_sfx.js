// Hookfall - the boss, portal and stage sounds (2026-11-08), synthesised from waveform maths like make_sfx.js:
// oscillators, noise, resonant filters and envelopes. No recorded samples and no AI audio model. The roar is a
// sawtooth "voice" with a jittery falling pitch pushed through vowel formants, with breath noise and a sub-octave.
// Run from the project root: node Tools/Audio/make_boss_sfx.js
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
const fs = require('fs');
const path = require('path');

const RATE = 44100;
const sfxDir = path.join(process.cwd(), 'Assets', '_Hookfall', 'Audio', 'SFX');

let seed = 90210;
const noise = () => { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x3fffffff - 1; };
const sine = p => Math.sin(2 * Math.PI * p);
const saw = p => 2 * (p - Math.floor(p + 0.5));
const env = (t, a, k) => (t < a ? t / a : Math.exp(-(t - a) * k));
const smooth = (a, b, t) => { const x = Math.max(0, Math.min(1, (t - a) / (b - a))); return x * x * (3 - 2 * x); };

function buffer(seconds) { return new Float32Array(Math.floor(seconds * RATE)); }

/** Adds an oscillator whose frequency follows freq(t). */
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

/** Adds noise through a one-pole low-pass (cutoff(t) in Hz) and an optional fixed high-pass. */
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
      const rc = 1 / (2 * Math.PI * highpass), al = rc / (rc + 1 / RATE);
      hpOut = al * (hpOut + v - hpPrev);
      hpPrev = v;
      v = hpOut;
    }
    buf[s0 + i] += v * amp(t);
  }
  return buf;
}

/** Resonant band-pass (RBJ biquad) whose centre follows freq(t); returns a new buffer. */
function bandpass(input, freq, q) {
  const out = new Float32Array(input.length);
  let x1 = 0, x2 = 0, y1 = 0, y2 = 0;
  for (let i = 0; i < input.length; i++) {
    const w = 2 * Math.PI * freq(i / RATE) / RATE, alpha = Math.sin(w) / (2 * q), cos = Math.cos(w);
    const a0 = 1 + alpha;
    const y = (alpha * input[i] - alpha * x2 + 2 * cos * y1 - (1 - alpha) * y2) / a0;
    x2 = x1; x1 = input[i]; y2 = y1; y1 = y;
    out[i] = y;
  }
  return out;
}

function mix(target, source, gain, start = 0) {
  const s0 = Math.floor(start * RATE);
  for (let i = 0; i < source.length && s0 + i < target.length; i++) target[s0 + i] += source[i] * gain;
  return target;
}

function drive(buf, amount) { for (let i = 0; i < buf.length; i++) buf[i] = Math.tanh(buf[i] * amount) / Math.tanh(amount); return buf; }

function lowpass(buf, cutoff) {
  const a = 1 - Math.exp(-2 * Math.PI * cutoff / RATE);
  let y = 0;
  for (let i = 0; i < buf.length; i++) { y += (buf[i] - y) * a; buf[i] = y; }
  return buf;
}

function finish(buf, peak = 0.89, fade = 0.006) {
  let max = 0;
  for (const v of buf) max = Math.max(max, Math.abs(v));
  const g = max > 0 ? peak / max : 1, f = Math.floor(fade * RATE);
  for (let i = 0; i < buf.length; i++) {
    let v = buf[i] * g;
    if (i < f) v *= i / f;
    if (i > buf.length - f) v *= (buf.length - i) / f;
    buf[i] = v;
  }
  return buf;
}

function writeWav(file, buf) {
  const data = Buffer.alloc(44 + buf.length * 2);
  data.write('RIFF', 0); data.writeUInt32LE(36 + buf.length * 2, 4); data.write('WAVE', 8);
  data.write('fmt ', 12); data.writeUInt32LE(16, 16); data.writeUInt16LE(1, 20); data.writeUInt16LE(1, 22);
  data.writeUInt32LE(RATE, 24); data.writeUInt32LE(RATE * 2, 28); data.writeUInt16LE(2, 32); data.writeUInt16LE(16, 34);
  data.write('data', 36); data.writeUInt32LE(buf.length * 2, 40);
  for (let i = 0; i < buf.length; i++) data.writeInt16LE(Math.round(Math.max(-1, Math.min(1, buf[i])) * 32767), 44 + i * 2);
  fs.writeFileSync(file, data);
  console.log('wrote', path.relative(process.cwd(), file), (buf.length / RATE).toFixed(2) + ' s');
}

// ---------------------------------------------------------------- the voice

/**
 * A monster voice: sawtooth source with a pitch contour, jitter and vibrato, plus breath noise, through "ah" formants,
 * with a sub-octave underneath. pitch(t) in Hz, amp(t) 0..1.
 */
function voice(seconds, pitch, amp, grit = 2.6) {
  const src = buffer(seconds);
  let wobble = 0;
  osc(src, saw, t => {
    wobble += (noise() * 0.5 - wobble) * 0.002;
    return pitch(t) * (1 + 0.035 * Math.sin(2 * Math.PI * 7.3 * t) + 0.08 * wobble);
  }, amp);
  noiseBand(src, () => 2600, t => amp(t) * 0.55);
  const out = buffer(seconds);
  mix(out, bandpass(src, t => 680 + 60 * Math.sin(t * 3), 4), 1.0);
  mix(out, bandpass(src, t => 1150 - 120 * Math.min(1, t), 5), 0.7);
  mix(out, bandpass(src, () => 2500, 7), 0.25);
  mix(out, lowpass(Float32Array.from(src), 320), 0.9);
  osc(out, sine, t => pitch(t) * 0.5, t => amp(t) * 0.45);
  return drive(out, grit);
}

// ---------------------------------------------------------------- the sounds

const sounds = {
  BossRoar() {
    const len = 2.6;
    const b = voice(len, t => 58 + 26 * smooth(0, 0.35, t) - 30 * smooth(0.5, 2.4, t),
      t => smooth(0, 0.22, t) * (1 - smooth(1.7, 2.55, t)), 1.5);
    noiseBand(b, t => 140, t => env(t, 0.05, 1.2) * 0.5);   // the rumble in the chest
    return finish(lowpass(b, 3200), 0.85);   // soft top end: harsh synth tones tired the first playtest
  },
  BossSlam() {
    const b = buffer(1.4);
    osc(b, sine, t => 30 + 40 * Math.exp(-t * 9), t => env(t, 0.004, 3.2) * 1.0);
    noiseBand(b, t => 380 * Math.exp(-t * 3) + 60, t => env(t, 0.003, 4.5) * 0.9);
    noiseBand(b, () => 5000, t => env(t, 0.001, 30) * 0.5, 0, 0.2, 1800);   // the crack
    noiseBand(b, () => 120, t => env(t, 0.1, 2.2) * 0.6, 0.05);
    return finish(drive(b, 1.6));
  },
  BossVolley() {
    const b = buffer(0.8);
    for (let k = 0; k < 3; k++) {
      const f0 = 2600 - k * 260;
      osc(b, sine, t => f0 * (0.45 + 0.55 * Math.exp(-t * 9)), t => env(t, 0.003, 14) * 0.55, k * 0.07, 0.6);
      osc(b, sine, t => f0 * 2.01 * (0.45 + 0.55 * Math.exp(-t * 9)), t => env(t, 0.002, 22) * 0.18, k * 0.07, 0.4);
    }
    noiseBand(b, t => 1200 + 3200 * Math.sin(Math.min(1, t / 0.5) * Math.PI), t => Math.sin(Math.min(1, t / 0.55) * Math.PI) * 0.35, 0, 0.55, 600);
    return finish(b, 0.8);
  },
  BossDeath() {
    const len = 3.6;
    const b = voice(len, t => 64 - 38 * smooth(0, 2.6, t), t => smooth(0, 0.15, t) * (1 - smooth(1.4, 2.8, t)), 1.4);
    noiseBand(b, t => 220 * Math.exp(-t * 0.6) + 50, t => env(t, 0.2, 0.9) * 0.9);
    osc(b, sine, t => 34 - 8 * t / len, t => env(t, 0.05, 0.8) * 0.6);
    for (let k = 0; k < 14; k++) {   // the crystal shatters
      const at = 1.3 + k * 0.11 + (noise() + 1) * 0.04, f = 1600 + (noise() + 1) * 1500;
      osc(b, sine, () => f, t => env(t, 0.002, 7) * 0.22, at, 1.2);
      osc(b, sine, () => f * 2.7, t => env(t, 0.002, 12) * 0.08, at, 0.8);
    }
    return finish(b);
  },
  PortalOpen() {
    const b = buffer(2.6);
    const chord = [440, 659.3, 880, 1108.7, 1318.5];
    chord.forEach((f, i) => osc(b, sine, t => f * (1 + 0.003 * Math.sin(2 * Math.PI * (4 + i) * t)),
      t => smooth(0, 0.9, t) * (1 - smooth(1.8, 2.6, t)) * (0.2 - i * 0.025) * (0.75 + 0.25 * Math.sin(2 * Math.PI * 6 * t + i)), i * 0.08));
    noiseBand(b, t => 600 + 3600 * smooth(0, 1.6, t), t => smooth(0, 1.2, t) * (1 - smooth(1.6, 2.5, t)) * 0.25, 0, null, 900);
    osc(b, sine, t => 110, t => smooth(0, 0.6, t) * (1 - smooth(1.5, 2.5, t)) * 0.3);
    return finish(b, 0.8);
  },
  PortalEnter() {
    const b = buffer(2.0);
    noiseBand(b, t => 200 + 7000 * smooth(0, 1.2, t), t => smooth(0, 0.9, t) * (1 - smooth(1.15, 1.6, t)) * 0.9);
    osc(b, sine, t => 180 + 1300 * smooth(0, 1.3, t), t => smooth(0.1, 0.9, t) * (1 - smooth(1.2, 1.6, t)) * 0.45);
    osc(b, sine, t => 270 + 1950 * smooth(0, 1.3, t), t => smooth(0.2, 1.0, t) * (1 - smooth(1.2, 1.6, t)) * 0.2);
    osc(b, sine, () => 55, t => env(t - 1.25, 0.01, 3) * (t > 1.25 ? 0.7 : 0), 0);   // the arrival thump
    return finish(b);
  },
  StageStart() {
    const b = buffer(3.0);
    const f0 = 98;
    [[1, 0.6, 0.9], [2.76, 0.35, 1.4], [5.4, 0.18, 2.2], [8.93, 0.1, 3.4]].forEach(([r, a, k]) =>
      osc(b, sine, () => f0 * r, t => env(t, 0.006, k) * a));
    noiseBand(b, () => 900, t => env(t, 0.002, 18) * 0.3);
    [196, 293.7, 392].forEach((f, i) => osc(b, sine, () => f, t => smooth(0, 0.8, t) * (1 - smooth(1.8, 2.9, t)) * 0.12, 0.35 + i * 0.12));
    return finish(b, 0.85);
  },
};

for (const [name, make] of Object.entries(sounds)) writeWav(path.join(sfxDir, `SFX_${name}.wav`), make());
