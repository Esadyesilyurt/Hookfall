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
| Core | `GameManager` (run state, timer, kills, pause, death, restart) |
| Player | `PlayerController`, `GrappleHook`, `PlayerStats`, `ThirdPersonCamera` (placeholder for Cinemachine) |
| Combat | `CombatSystem`, `ItemBase`, `Items/OrbitBlades` |
| Enemies | `StateMachine` + `IEnemyState`, `EnemyBase`, `Bat`, `States/ChaseState`, `FrenzyState`, `BiteState`, `EnemyDirector` |
| World | `Island`, `IslandGenerator` |
| Progression | `XPSystem`, `GoldWallet`, `LevelUpSystem` (placeholder: random stat) |
| UI | `HUD` (IMGUI debug HUD, placeholder) |
| Editor | `ArenaSceneBuilder`: menu **Hookfall > Build Arena Scene** rebuilds the test scene |

## Schedule (from the production plan)

| Week | Goal |
| --- | --- |
| 9 | Hook, swing and air strafe feel good; hook projectile; Cinemachine camera |
| 10 | Core loop playable with placeholders: XP gems and gold pickups, 3-card level-up screen, run-end screen |
| 11 | Wyvern and Golem state machines, steering separation and island avoidance, object pooling, 300-enemy test at 60 FPS; Tripo + Blender MCP island pipeline |
| 12 | All 6 items, all stats, chests and gold costs, tower cache, gravity zone, elite waves; first playtest |
| 13 | Toon shader and outline, VFX, licensed audio, final uGUI HUD; second playtest |
| 14 | Bug fixes, optimisation, final build |

## Known placeholders (TODO in code)

- Hook attaches instantly (planned: 120 m/s projectile).
- Enemies grant XP and gold directly on death (planned: gems and coins to collect).
- Bats have no separation or island avoidance yet.
- Level-up picks a random stat (planned: pause + 3 cards).
- Islands are primitives (planned: Tripo + Blender MCP models).
