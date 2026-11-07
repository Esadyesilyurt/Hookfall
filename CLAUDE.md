# Hookfall: project context for Claude

Read this first in every session. The full design is in the GDD (Claude Docs artifact "Hookfall – Midterm GDD"); ask Esad for it when you need detail beyond this file.

## The game in one paragraph

3D survivor-like (Megabonk / Vampire Survivors pacing) in Unity 6000.4.0f1 with URP. The player, a Warden, swings between floating islands above a bottomless abyss with a mouse-aimed grappling hook. **Auto-attacks fire only while the player is airborne** and stop on islands. Islands are safe from the fall but leave the player defenceless. Endless runs, no win condition; the goal is the player's own record. Lose by health reaching zero or falling into the abyss. A fixed tower with the Heartstone stands on the central island; islands around it are procedural every run.

## Design rules that must not be broken

- `PlayerController.IsAirborne` is the core flag: it turns combat on (airborne) and puts enemies into Frenzy (grounded).
- Enemy AI is **controlled chaos**: no limits on how many enemies attack at once, no waiting for turns, enemies never back off. Up to 300 alive. The player wins by hit-and-run. Only very short attack warnings (0.3–0.4 s) for Wyvern and Golem.
- Items come **only from chests**. Level-ups give either a level for an owned item or a stat upgrade. Stats have no cap (movement stats soft-capped at +100%).
- No AI API, LLM or online model runs inside the game. In-game AI is our own C#.
- No AI-generated audio.

## Code conventions

- All game content lives in `Assets/_Hookfall/`. Scripts in `Scripts/<System>/`, namespace `Hookfall.<System>`.
- One class per file; file name = class name, PascalCase.
- Asset prefixes: `PF_` prefabs, `M_` materials, `T_` textures, `SO_` ScriptableObjects, `SFX_` / `MUS_` audio.
- Tunable values are serialized fields or ScriptableObjects, never hard-coded. Starting values come from GDD section 4.
- Input: the project uses the **Input System package only** (`activeInputHandler: 1`). Use `Keyboard.current` / `Mouse.current` or Input Actions, never `UnityEngine.Input`.
- Unity 6 API: `Rigidbody.linearVelocity`, not `velocity`.
- Every AI-assisted script starts with the AI disclosure header used in the existing scripts, and the work is added to `Docs/PromptLog.md`.
- `main` must always open and play with no Console errors. Work on `feature/<name>` branches.

## Systems and where they live

| System | Scripts |
| --- | --- |
| Core | `GameManager` (run state, modal screens, death, record, scene changes), `RunState`, `GameSettings`, `RunRecord`, `Layers` |
| Player | `PlayerController` (`IsAirborne`, `CurrentIsland`, 1.8x air gravity, wall jump, double jump and dash once per flight), `GrappleHook` (120 m/s hook head, taut-rope swing with downhill pump, right-click power swing that reels in), `PlayerStats` + `StatType` (11 stats), `CameraRig` (drives the Cinemachine orbit camera; a Deoccluder keeps it out of the `World` layer), `WardenAnimator` (procedural animation of the Warden model, air tricks) |
| Combat | `CombatSystem` (airborne rule, chest rewards), `ItemBase`, `ProjectileItem`, `Projectile`, `EnemyQuery`, `Rarity`, `ChestReward`, `Items/` (the 6 attack items and `TetherCoil`, the Zip item) |
| Enemies | `StateMachine`, `IEnemyState`, `EnemyBase` (steering, catch-up speed, think rate, pooling, glow, fallbacks), `Bat`, `Wyvern`, `Golem`, `Boulder`, `States/`, `EnemyDirector` (spawns, mix, golems, elite waves, pools) |
| World | `Island`, `IslandKind`, `IslandGenerator` (height bands, reachability, models, props, clouds), `Chest`, `ChestSpawner`, `TowerCache`, `GravityZone`, `StormWall` (arena edge, wind push-back), `Backdrop` (far scenery) |
| Progression | `XPSystem`, `GoldWallet`, `LevelUpSystem` (3 cards), `LevelUpOption`, `Pickup`, `PickupKind`, `PickupSpawner` |
| UI | uGUI + TextMeshPro built from code by `UIFactory` with the generated skin (`UISkin`, `UIPop`, `UIHover`): `HUD`, `LevelUpUI`, `ChestResultUI`, `PauseMenuUI`, `RunEndUI`, `SettingsPanel`, `MainMenuUI`, `UITheme` |
| VFX, Audio, Feel | `VfxManager` (one world-space particle system for all bursts), `AudioManager` + `SoundId` (clips wired by file name), `FeelDirector` (shake, hit-stop, FOV kicks, post-FX pulses), `DamageNumbers` |
| Editor | Menu **Hookfall > Build All Scenes** (`ArenaSceneBuilder`) rebuilds MainMenu and Arena; `EnemyPrefabs`, `ItemPrefabs`, `ModelSetup` (**Import Models**), `ToonSetup` (**Setup Toon Rendering**), `UISetup` (**Build UI Skin**), `FeelSetup`, `AudioWiring`, `RecordTools` (**Clear Best Run**), `BuildUtil` |
| Tests | `Tests/PlayMode/SceneSmokeTests` (scenes run clean, kills happen, every enemy and item, tower cache), `PerformanceTests` (300 enemies, asserts 60 FPS), `ScreenshotCapture` (runs only when `HOOKFALL_SHOT_DIR` is set), `RecordGuard` (tests never touch the saved best run) |
| Tools | `Tools/Blender/hookfall_models.py` (+ `hookfall_models_v2.py`) makes every model and the UI icons; `Tools/ShaderGraph/make_graphs.js` makes `SG_Toon`, `SG_Backdrop` and `SG_Outline` from Unity's samples; `Tools/Audio/make_sfx.js` synthesises the placeholder audio |

Scenes and prefabs are generated: change the builder code, not the scene, then run **Build All Scenes**. Defaults the GDD does not give, and deviations from it, are in `Docs/Defaults.md`.

## Checking work without the editor

- Compile and build: `Unity.exe -batchmode -nographics -quit -projectPath . -executeMethod Hookfall.EditorTools.ArenaSceneBuilder.BatchBuild -logFile <log>`, then search the log for `error CS`.
- Play-mode tests: `Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>` (no `-nographics`, no `-quit`).
- In batch mode `AssetDatabase.ImportPackage` does nothing and `WaitForEndOfFrame` never fires. `EditorSceneManager.NewScene` unloads assets loaded before it, so load prefabs after it.

## Schedule (from the production plan)

| Week | Goal | State on 2026-11-07 |
| --- | --- | --- |
| 9 | Hook, swing and air strafe feel good; hook projectile; Cinemachine camera | Built; feel needs playtesting |
| 10 | Core loop playable with placeholders: XP gems and gold pickups, 3-card level-up screen, run-end screen | Built |
| 11 | Wyvern and Golem state machines, steering separation and island avoidance, object pooling, 300-enemy test at 60 FPS; island model pipeline | Built; 300 enemies: 87 FPS average at 1080p on an RTX 2060 (PerformanceTests, 2026-11-08; 118 before bats could keep up) |
| 12 | All 6 items, all stats, chests and gold costs, tower cache, gravity zone, elite waves; first playtest | Built; playtest to do |
| 13 | Toon shader and outline, VFX, licensed audio, final uGUI HUD; second playtest | Built; Kenney CC0 effects, synthesised music |
| 14 | Bug fixes, optimisation, final build | To do |

## Known gaps

- Effects are Kenney CC0 recordings (`Tools/Audio/use_kenney.js`); wind, heartbeat and music are synthesised by `Tools/Audio/make_sfx.js`.
- The Warden and enemies are animated procedurally (no Mixamo rig); see `Docs/Defaults.md`.
- The 300-enemy number comes from a batch-mode test; confirm in the Profiler on the target PC.
- Play-mode tests share PlayerPrefs with the editor: wrap anything that saves in `RecordGuard`.
