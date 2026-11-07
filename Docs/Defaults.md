# Hookfall – Defaults and deviations

Values the GDD does not give, picked by Claude on 2026-11-07 when building Weeks 9–13 (see Docs/PromptLog.md #16–20). Every value is a serialized field you can change in the Inspector; the script is named in brackets. Playtests in Weeks 12–13 should tune them.

## Values the GDD leaves open

### Hook, movement, camera
| Value | Default | Where |
| --- | --- | --- |
| Rope length after attach | 85% of distance (GDD: 15% reel-in; the skeleton used 80%) | GrappleHook |
| Rope minimum length | 25% of distance | GrappleHook |
| Movement soft cap | Bonuses count fully up to +100%; above that each pick counts 25% | PlayerStats |
| Camera | Orbit radius 8 m, target offset 1.6 m, pitch −40° to 75°, damping 0.1 | Player Camera (Cinemachine) |

### Pickups and level-up
| Value | Default | Where |
| --- | --- | --- |
| Gem size | XP ≥ 3 medium gem, XP ≥ 5 large gem | PickupSpawner |
| Magnet | 80 m/s² pull once inside the pickup radius, collected within 1.2 m | Pickup |
| Pickup lifetime | 90 s, then it disappears | Pickup |
| Level-up cards | 3 cards drawn evenly from the 11 stats and every owned item below level 5 | LevelUpSystem |

### Chests and the tower cache
| Value | Default | Where |
| --- | --- | --- |
| First chest | 20 s into the run, then every 45 s (GDD) | ChestSpawner |
| Chest islands | Any non-tower island at least 5 m wide | ChestSpawner |
| Luck | Multiplies the Rare and Epic weights | ChestSpawner.Roll |
| Rarity effect | New item starts at level 1 / 2 / 3 (Common / Rare / Epic); an owned item gains +1 / +2 / +3 levels | CombatSystem |
| Everything maxed | The chest gives a random stat, once per level it would have given | CombatSystem |
| Tower cache | Open within 3 m; progress drains at half speed while you are away | TowerCache |
| Low gravity | A vertical cylinder of radius 40 m around the tower | GravityZone |

### Enemies and difficulty
| Value | Default | Where |
| --- | --- | --- |
| Bat flocks | 4–8 bats, 4 m spread | EnemyDirector |
| Bat bite | 0.25 s recovery, pursuit looks 0.5 s ahead | PF_Bat |
| Wyverns | From minute 1; 5% of spawns, +3% per minute, max 20% | EnemyDirector |
| Wyvern | Chase 14 m/s, dash 24 m (0.8 s at 30 m/s), hits within 2 m, recovers at 30% speed, dives from 10 m above an island-bound player | PF_Wyvern |
| Golems | From minute 2 (GDD), one every 15 s up to 2 + 1 per minute, on free inner islands within 110 m | EnemyDirector |
| Golem | Throws from 3 m up; boulder hits within 1.3 m, breaks on islands, lives 3 s | PF_Golem, PF_Boulder |
| Elite wave | Every 5 min (GDD): 15 bats, 3 wyverns, 1 golem; ×3 health, ×1.5 damage, ×1.4 size, ×3 XP and gold, magenta glow | EnemyDirector |
| Spawn distance | 60 m around the player, out of camera view; 90 m ahead of the flight path | EnemyDirector |
| Steering | Acceleration 40 m/s², separation 1.6 m, island raycast 0.5 s ahead; stuck twice in a row → respawn | EnemyBase |
| Low FPS | Spawning pauses below 50 FPS (smoothed); hit sparks drop to a quarter | EnemyDirector, VfxManager |

### Items
| Item | Defaults beyond GDD 4.5 |
| --- | --- |
| Orbit Blades | 2 blades at level 1 |
| Shard Burst | 8 shards at level 1, 1 s life (30 m), spread flattened towards the horizon |
| Arc Laser | Sweep takes 0.4 s; hits enemies up to 4 m above or below |
| Ram Strike | Contact radius 1.6 m; the same enemy can be rammed again after 0.5 s |
| Homing Sparks | 2 missiles at level 1, 22 m/s, turn 540°/s, 2 s life |

### Look and sound
| Value | Default | Where |
| --- | --- | --- |
| Light bands | 3 (GDD: 2–3) | LightToon subgraph `_Steps` |
| Outline | 0.035 object units, colour #1C1724 | M_Outline |
| Music | One more layer every 2 minutes; all layers for 40 s after an elite wave | AudioManager |
| Heartbeat | Below 30% health | AudioManager |
| Wind | Full volume at 35 m/s | AudioManager |

### Added after the first playtest (2026-11-07)
| Value | Default | Where |
| --- | --- | --- |
| Island height bands | −32…−14 m, −6…10 m, 18…38 m, plus 4–6 small perches at 28–46 m within 24–60 m of the tower | IslandGenerator |
| Reachability | Every island within 32 m (centre to centre) of one the player can reach; hook range is 40 m | IslandGenerator |
| Storm wall | 195 m from the tower, 5 cloud layers from −120 m to 30 m, turns 1.5°/s, lightning every 2.5–7 s | StormWall |
| Storm wind | Starts pushing at 170 m, up to 80 m/s² at the wall; outward speed removed inside it | StormWall |
| Spawns | Kept within 180 m of the tower (inside the storm) | EnemyDirector |
| Far scenery | 14 pieces at 430–600 m, scaled 1.6–3.2, drifting 0.4°/s; no fog, pale colours | Backdrop |
| Zip | 50 m range, 4 s cooldown, hook head 220 m/s, pull 48 m/s, ends within 3 m or after 1.6 s, +6 m/s lift at the end; the speed cap then decays 18 m/s per second | GrappleHook, PlayerController |
| Zip stats | Zip cooldown ×0.9 per pick (minimum 0.6 s), Zip range +5 m per pick | PlayerStats |
| Tether Coil | Cooldown ×0.9/0.85/0.8/0.75/0.7 at levels 1–5, +1 charge at levels 3 and 5, 25 damage (+25% per level) to enemies zipped through, 30-damage 6 m shockwave on landing from level 4 | PF_Item_TetherCoil |
| Shake | Hook catch 0.15, Zip 0.35, hurt 0.6, elite or golem kill 0.4, landing 0.1–0.5 by fall speed | FeelDirector |
| Hit-stop | Hurt 0.06 s, elite or golem kill 0.04 s, at 5% time speed | FeelDirector |
| FOV kick | Zip +14°, landing up to −4° | FeelDirector, CameraRig |
| Knockback | Bat 7 m/s, wyvern 2.5 m/s, golem 0; hit squash 0.3 / 0.15 / 0.08 | Enemy prefabs |
| Damage numbers | At most 8 per frame (2 when the frame rate is low), big hits from 40 damage | DamageNumbers |
| Post-processing | Bloom threshold 1.05, intensity 0.9; vignette 0.2 (0.45 at low health); saturation +8, contrast +6; glowing materials ×2.2 HDR | PP_Hookfall, FeelSetup |

## Where the build differs from the GDD

| GDD | What was built | Why |
| --- | --- | --- |
| Island base shapes from Tripo, fixed in Blender MCP (8.3, Part D) | Every model is made by `Tools/Blender/hookfall_models.py`, run headless | Esad's choice on 2026-11-07; Tripo was not available to Claude |
| Warden rigged and animated with Mixamo; enemies with simple rigs (8.3) | Models are split into parts (arms, legs, wings, cape, lid); `WardenAnimator`, `EnemyBase` and `Golem` animate them in code | Mixamo needs Esad's Adobe login; 300 Animators would also cost more than moving a few transforms |
| Toon shader made in Shader Graph (8.1) | Shader Graph, generated from Unity's own Custom Lighting Toon sample and Unlit template by `Tools/ShaderGraph/make_graphs.js` | Keeps Shader Graph (Esad's choice) while the graph stays reproducible from a script |
| Spawn zone beyond 150 m (7.1) | 60 m out of view, 90 m ahead | At 150 m bats need about 12 s to arrive, which empties the screen |
| Own vector icons (8.3) | 3D symbols rendered in Blender as sprites, in diamond and card frames | Esad's choice on 2026-11-07 |
| Licensed sound and music (8.3) | Placeholder sound effects and three music stems synthesised in code by Tools/Audio/make_sfx.js | Esad's choice on 2026-11-08 until licensed files are chosen (Docs/Credits.md); no AI audio model is used |
| Spawn rate 2/s, +15% per minute, flocks of 4–8 (4.8) | 5/s, +20% per minute, flocks of 8–14 | Esad found the first build too calm (2026-11-07) |
| Six items, eleven stats (4.5, 4.6) | A seventh chest item (Tether Coil) and two Zip stats | Added with the right-click Zip on Esad's request (2026-11-07) |
| Islands at any height, arena edge not described (4.9, 7.1) | Three height bands with a reachability check, and a storm wall at 195 m with far scenery beyond | Esad's feedback: islands could not be climbed and the edge felt empty |
