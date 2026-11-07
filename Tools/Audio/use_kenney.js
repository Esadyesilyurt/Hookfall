// Hookfall - copies the chosen Kenney sounds (CC0, www.kenney.nl) into Audio/SFX under the names the AudioManager reads,
// and removes the synthesised placeholder each one replaces.
// Usage from the project root:  node Tools/Audio/use_kenney.js <folder with the extracted Kenney packs>
// Packs (CC0, from Kenney's uploads on opengameart.org): RPGsounds_Kenney, kenney_interfaceSounds, jingleSounds_Kenney.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
const fs = require('fs');
const path = require('path');

const source = process.argv[2];
if (!source || !fs.existsSync(source)) { console.error('Give the folder with the extracted Kenney packs.'); process.exit(1); }
const sfxDir = path.join(process.cwd(), 'Assets', '_Hookfall', 'Audio', 'SFX');

// SoundId -> Kenney file (without extension). Soft, short sounds for everything that repeats.
const map = {
  HookThrow: 'cloth1', HookAttach: 'metalLatch', HookRelease: 'cloth3',
  WeaponHit: 'knifeSlice', EnemyDeath: 'pluck_001', XPPickup: 'glass_002', GoldPickup: 'handleCoins',
  ChestOpen: 'doorOpen_1', TowerCacheBeam: 'maximize_006', TowerCacheOpen: 'jingles_STEEL00',
  LevelUp: 'confirmation_002', PlayerHurt: 'dropLeather', GolemSlam: 'doorClose_4', BoulderBreak: 'chop',
  EliteWave: 'bong_001', RunEnd: 'minimize_009', WallJump: 'cloth4', Trick: 'cloth2',
};

function find(dir, base) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) { const hit = find(p, base); if (hit) return hit; }
    else if (/\.(ogg|wav|mp3)$/i.test(entry.name) && path.parse(entry.name).name === base) return p;
  }
  return null;
}

for (const [id, base] of Object.entries(map)) {
  const file = find(source, base);
  if (!file) { console.error(`missing ${base} for ${id}`); process.exitCode = 1; continue; }
  // One file per sound: drop any other format of the same name (the synthesised .wav placeholder).
  for (const ext of ['.wav', '.ogg', '.mp3']) {
    const old = path.join(sfxDir, `SFX_${id}${ext}`);
    if (fs.existsSync(old)) { fs.unlinkSync(old); if (fs.existsSync(old + '.meta')) fs.unlinkSync(old + '.meta'); }
  }
  const target = path.join(sfxDir, `SFX_${id}${path.extname(file).toLowerCase()}`);
  fs.copyFileSync(file, target);
  console.log(`${id} <- ${path.basename(file)}`);
}
