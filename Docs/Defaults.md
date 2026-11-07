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

## Where the build differs from the GDD

| GDD | What was built | Why |
| --- | --- | --- |
| Island base shapes from Tripo, fixed in Blender MCP (8.3, Part D) | Every model is made by `Tools/Blender/hookfall_models.py`, run headless | Esad's choice on 2026-11-07; Tripo was not available to Claude |
| Warden rigged and animated with Mixamo; enemies with simple rigs (8.3) | Models are split into parts (arms, legs, wings, cape, lid); `WardenAnimator`, `EnemyBase` and `Golem` animate them in code | Mixamo needs Esad's Adobe login; 300 Animators would also cost more than moving a few transforms |
| Toon shader made in Shader Graph (8.1) | Shader Graph, generated from Unity's own Custom Lighting Toon sample and Unlit template by `Tools/ShaderGraph/make_graphs.js` | Keeps Shader Graph (Esad's choice) while the graph stays reproducible from a script |
| Spawn zone beyond 150 m (7.1) | 60 m out of view, 90 m ahead | At 150 m bats need about 12 s to arrive, which empties the screen |
| Own vector icons (8.3) | Diamond frames with the first letter of the name | Placeholder until icons are drawn |
| Licensed sound and music (8.3) | AudioManager and file-name wiring are done; no audio files yet | Audio must be chosen and licensed by Esad (see Docs/Credits.md) |
