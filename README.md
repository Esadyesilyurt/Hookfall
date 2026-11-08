# Hookfall

A fast 3D survivor-like: swing between floating islands above a bottomless abyss with a grappling hook. Your weapons fire only while you are airborne, so every swing is an attack and every landing is a risk.

Solo project by Esad Yesilyurt for *Game Studio: AI in Game Development* (Istanbul Kent University, Fall 2026).

## Open and run

1. Open the folder in Unity Hub with **Unity 6000.4.0f1** (Universal Render Pipeline).
2. In the menu bar choose **Hookfall > Build All Scenes**. This imports the models, sets up the toon rendering, builds every prefab, creates `Scenes/MainMenu.unity` and `Scenes/Arena.unity`, and puts both in Build Profiles (MainMenu first).
3. Open `MainMenu` (or `Arena` to skip the menu) and press **Play**.

## Controls

| Input | Action |
| --- | --- |
| Mouse | Aim and turn the camera |
| Left mouse button | Throw the hook at an island; hold to swing (faster on the way down), release to fly on |
| Right mouse button | Zip: a power swing; keep holding and it reels you up to the island (own cooldown and charges) |
| A / D | Walk on islands; light air strafe while flying |
| W / S | Walk on islands |
| Space | Jump on islands; wall jump when touching an island side in the air (W/A/S/D steers it); otherwise one double jump per flight |
| Left Shift | Dash in the air, once per flight, towards W/A/S/D or where the camera looks; refilled on landing |
| E | Open a chest within 7 m, on foot or in flight |
| 1 / 2 / 3 or click | Pick a level-up card or a chest item |
| Esc | Pause |
| R | Retry after the run ends |

Attacks are automatic and only fire while you are in the air.

## What is in the game

- Grappling hook with a 120 m/s hook head and a SpringJoint rope, island movement, air strafe, Cinemachine camera with speed FOV
- Right-click Zip: a power swing that reels you in when held, with its own cooldown and charges, upgraded by two stats and the Tether Coil item
- Six attack items and eighteen passives (lifesteal, shield, revive, crits and more) in four rarities; rarer chests offer rarer items
- Updrafts, air channels between islands, refill crystals for the double jump and dash, and floating hook anchors in the open sky
- Wall jumps off island sides, an automatic ledge climb onto islands, a double jump and a dash per flight, and flips and barrel rolls when flying fast
- Airborne-only combat with all six attack items (Orbit Blades, Shard Burst, Arc Laser, Ram Strike, Homing Sparks, Storm Aura) and the Tether Coil
- Enemy AI: Bat, Wyvern and Golem finite state machines, steering with separation and island avoidance, Frenzy when you land, elite waves, the GDD 5.7 fallbacks, pooling for 300 enemies
- XP gems and gold coins, 3-card level-up with 11 stats, chests with rarity and rising cost, the tower cache, the low-gravity circle around the tower
- Procedural islands in three height layers around the fixed tower, with waterfalls, ruins, lanterns and crystals; a storm wall closes the arena and far scenery fills the horizon
- Main menu with intro, pause with items, stats and lore, settings, run-end screen, saved best run, an animated uGUI HUD with rendered icons
- Toon Shader Graph with outlines, bloom and colour grading, game feel (shake, hit-stop, FOV kicks, damage numbers, knockback), particle effects
- Kenney CC0 sound effects with a quiet mix, and calm layered music synthesised in code

## Tools

| Tool | What it does |
| --- | --- |
| `Tools/Blender/hookfall_models.py` | Builds every model in Blender and exports FBX to `Art/Models`: `blender -b --factory-startup -P Tools/Blender/hookfall_models.py -- Assets/_Hookfall/Art/Models Docs/ModelPreviews` |
| `Tools/ShaderGraph/make_graphs.js` | Builds `SG_Toon` and `SG_Outline` from Unity's Shader Graph samples: `node Tools/ShaderGraph/make_graphs.js` |
| `Tools/Audio/use_kenney.js` | Copies the chosen Kenney CC0 sounds into `Audio/SFX`: `node Tools/Audio/use_kenney.js <extracted packs>` |
| `Tools/Audio/make_sfx.js` | Synthesises the wind loop, heartbeat and music stems: `node Tools/Audio/make_sfx.js` |
| Hookfall > Build All Scenes | Rebuilds scenes, prefabs, materials, the UI skin and audio wiring from code |
| Hookfall > Clear Best Run | Forgets the saved best run |
| Window > General > Test Runner > PlayMode | Smoke tests (both scenes run clean, every enemy, item, the Zip and the tower cache work) and the 300-enemy performance test |

Defaults chosen where the GDD gives no number, and where the build differs from the GDD: [Docs/Defaults.md](Docs/Defaults.md). Audio file names and licences: [Docs/Credits.md](Docs/Credits.md).

## Repository layout

```
Assets/_Hookfall/   all game content (Art, Audio, Data, Editor, Prefabs, Scenes, Scripts, Tests)
Tools/              Blender model script and Shader Graph generator
Docs/               GDD, prompt log, defaults, credits
CLAUDE.md           project context for Claude Code sessions
```

## AI use

Scripts, models and the shader generator were written with Claude (Anthropic); each script says so in its header comment. All AI use is logged in [Docs/PromptLog.md](Docs/PromptLog.md). The game itself calls no AI service and needs no API keys.
