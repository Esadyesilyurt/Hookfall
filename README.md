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
| Left mouse button | Throw the hook at an island; hold to swing, release to let go |
| A / D | Walk on islands; light air strafe while flying |
| W / S | Walk on islands |
| Space | Jump (on islands) |
| E | Open a chest |
| 1 / 2 / 3 or click | Pick a level-up card |
| Esc | Pause |
| R | Retry after the run ends |

Attacks are automatic and only fire while you are in the air.

## What is in the game

- Grappling hook with a 120 m/s hook head and a SpringJoint rope, island movement, air strafe, Cinemachine camera with speed FOV
- Airborne-only combat with all six items: Orbit Blades, Shard Burst, Arc Laser, Ram Strike, Homing Sparks, Storm Aura
- Enemy AI: Bat, Wyvern and Golem finite state machines, steering with separation and island avoidance, Frenzy when you land, elite waves, the GDD 5.7 fallbacks, pooling for 300 enemies
- XP gems and gold coins, 3-card level-up with 11 stats, chests with rarity and rising cost, the tower cache, the low-gravity circle around the tower
- Procedural island rings around the fixed tower, dressed with rocks, ruins, crystals and clouds
- Main menu with intro, pause with lore, settings, run-end screen, saved best run, uGUI HUD
- Toon Shader Graph with outlines, particle effects, and an audio system waiting for licensed sound files

## Tools

| Tool | What it does |
| --- | --- |
| `Tools/Blender/hookfall_models.py` | Builds every model in Blender and exports FBX to `Art/Models`: `blender -b --factory-startup -P Tools/Blender/hookfall_models.py -- Assets/_Hookfall/Art/Models Docs/ModelPreviews` |
| `Tools/ShaderGraph/make_graphs.js` | Builds `SG_Toon` and `SG_Outline` from Unity's Shader Graph samples: `node Tools/ShaderGraph/make_graphs.js` |
| Hookfall > Build All Scenes | Rebuilds scenes, prefabs and materials from code |
| Window > General > Test Runner > PlayMode | Smoke tests: both scenes run with no Console errors; every enemy, item and the tower cache work |

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
