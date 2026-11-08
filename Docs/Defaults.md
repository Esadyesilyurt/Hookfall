# Hookfall – Defaults and deviations

Values the GDD does not give, picked by Claude on 2026-11-07 when building Weeks 9–13 (see Docs/PromptLog.md #16–20). Every value is a serialized field you can change in the Inspector; the script is named in brackets. Playtests in Weeks 12–13 should tune them.

## Values the GDD leaves open

### Hook, movement, camera
| Value | Default | Where |
| --- | --- | --- |
| Rope length after attach | 85% of distance (GDD: 15% reel-in; the skeleton used 80%) | GrappleHook |
| Rope minimum length | 25% of distance | GrappleHook |
| Base health regen | 1 HP/s from the start of a run (0 until 2026-11-08); each Health regen pick adds 0.5 HP/s | PlayerStats |
| Movement soft cap | Bonuses count fully up to +100%; above that each pick counts 25% | PlayerStats |
| Camera | Orbit radius 8 m, target offset 1.6 m, pitch −80° to 80° (−40° to 75° until 2026-11-08), damping 0.1 | Player Camera (Cinemachine) |
| Camera collision | Cinemachine Deoccluder against the World layer (islands, tower, props): pulls the camera towards the Warden, camera radius 0.45 m, at least 0.6 m from the target, 0.35 s return | Player Camera |

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
| Chest reach | E opens a chest within 7 m, standing or flying (3 m and on the ground only until 2026-11-08) | Chest |
| Luck | Multiplies the Rare and Epic weights | ChestSpawner.Roll |
| Rarity effect | Rarer chests offer rarer items, never more levels: new items of the chest's rarity first, then lower rarities; owned items gain +1 level; stats fill what is left (levels per rarity until 2026-11-08) | CombatSystem.RollChest |
| Chest rarity weights | Common 60, Rare 28, Epic 10, Legendary 2; Luck multiplies all but Common. Tower cache: Rare 45, Epic 40, Legendary 15 | ChestSpawner, TowerCache |
| Item slots | 6 attack items and 6 passives | CombatSystem |
| Attack item rarity | Common: Orbit Blades, Shard Burst. Rare: Homing Sparks, Ram Strike. Epic: Arc Laser, Storm Aura. Legendary: Tether Coil | ItemPrefabs |
| Common passives (stat charms, per level) | Heart Locket +20 max health; Iron Hide +1 armor; Gale Feather +10% max flight speed and +8% walk speed; Lodestone +25% pickup radius; Mending Moss +0.5 HP/s; Sage's Lens +10% XP | ItemPrefabs, StatCharm |
| Rare passives | Whetstone +10% damage; Quick Draw +16% attack speed; Lucky Coin +20% luck (per level); Vampire Fang 2% lifesteal per level; Soul Harvest 0.6 HP per kill per level; Hunter's Eye 6% crit chance per level (×2 damage); Thorn Mail 15 damage per level back to enemies within 5 m when hit in flight | Items/ |
| Epic passives | Ward Shell blocks one hit, recharges in 12 s (−1.5 s per level); Wind Sigil refills the double jump and dash in the air every 10 s (−1.25 s per level); Berserker Blood +20% damage per level below half health | Items/ |
| Legendary passives | Phoenix Feather revives at zero health with 40% (+10% per level) health, recharges in 150 s (−20 s per level), not against the abyss; Heartstone Core pulses every 7 s (−0.75 s per level) in flight: 40 damage (+50% per level) within 10 m and heals 4% max health | Items/ |
| Chest choice | 3 different choices of the rolled rarity (single random item until 2026-11-08): new items while a slot is free, levels for owned items that are not maxed; the game pauses until one is picked | CombatSystem.RollChest, ChestResultUI |
| Everything maxed | The 3 choices become stats, each given once per level the chest would have given | CombatSystem |
| Tower cache | Open within 3 m; progress drains at half speed while you are away | TowerCache |
| Low gravity | A vertical cylinder of radius 40 m around the tower | GravityZone |
| Sky features (2026-11-08) | Per run, fewer when the layout has no room: 8 updrafts, 4 air channels, 12 refill crystals, 10 hook anchors | SkyFeatures |
| Updraft | Radius 5 m, 32 m/s² up (about 14 net of air gravity), rising at most 16 m/s, fading over the top 6 m; half beside island rims (28 m below to 6 m above the top), half between islands | SkyFeatures, Updraft |
| Air channel | Radius 3.5 m, 38 m/s, between rims 40–120 m apart, 3 m above the start island; carries the Warden's weight while inside | SkyFeatures, AirChannel |
| Refill crystal | Touch within 2.5 m in flight: refills the double jump and dash if either was spent; back after 8 s | SkyFeatures, RefillCrystal |
| Hook anchor | A floating rock (PF_Rock_1 ×1.6) 5–15 m above the higher of two islands 30–110 m apart, at least 20 m from other features | SkyFeatures, HookAnchor |

### Enemies and difficulty
| Value | Default | Where |
| --- | --- | --- |
| Bat flocks | 4–8 bats, 4 m spread | EnemyDirector |
| Bat bite | 0.25 s recovery, pursuit looks 0.5 s ahead | PF_Bat |
| Wyverns | From minute 1; 5% of spawns, +3% per minute, max 20% | EnemyDirector |
| Wyvern | Chase 20 m/s (14 until 2026-11-08), dash 24 m (0.8 s at 30 m/s), hits within 2 m, recovers at 30% speed, dives from 10 m above an island-bound player | PF_Wyvern |
| Golems | From minute 2 (GDD), one every 15 s up to 2 + 1 per minute, on free inner islands within 110 m | EnemyDirector |
| Golem | Throws from 3 m up; boulder hits within 1.3 m, breaks on islands, lives 3 s | PF_Golem, PF_Boulder |
| Elite wave | Every 5 min (GDD): 15 bats, 3 wyverns, 1 golem; ×3 health, ×1.5 damage, ×1.4 size, ×3 XP and gold, magenta glow | EnemyDirector |
| Spawn distance | 60 m around the player, out of camera view; 90 m ahead of the flight path | EnemyDirector |
| Steering | Acceleration 60 m/s² (40 until 2026-11-08), separation 1.6 m, island raycast 0.5 s ahead; stuck twice in a row → respawn | EnemyBase |
| Catch-up | Chasing bats and wyverns fly at least 80% of the Warden's speed, plus 0.4 m/s for every metre beyond 15 m; bats 18 m/s base (12 until 2026-11-08) | EnemyBase, PF_Bat |
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

### Swing and movement rework (2026-11-08, after Esad's playtest)
| Value | Default | Where |
| --- | --- | --- |
| Rope | Taut rope: spring 60, damper 0.6, slack allowed, 10% reel-in on attach | GrappleHook |
| Swing pump | +10 m/s² along the swing while it is going down | GrappleHook |
| Letting go | Speed ×1.1, +3 m/s up when rising; the speed cap follows, up to 15% over the cap | GrappleHook, PlayerController |
| Air gravity | ×1.8 while airborne (faster swings, harder climbs) | PlayerController |
| Max air speed | 40 m/s, +10% per Max flight speed pick (soft-capped at +100%); Zip and release boosts at most 15% over it (55 m/s with uncapped boosts until later on 2026-11-08; GDD 4.1: 35) | PlayerController, PlayerStats |
| Ledge climb | Touching an island side or rim underside with the feet up to 8 m below the top: a 0.35 s move out past the rim, 1.2 m above the top, then onto the surface 1.5 m inside the rim | PlayerController |
| Air strafe | A/D only: 40 m/s² up to 16 m/s sideways, faster momentum kept (12 m/s² until later on 2026-11-08; GDD 4.1: 6) | PlayerController |
| Rope climb | W shortens the rope at 12 m/s with a 14 m/s² pull (down to 4 m), S lets it out up to hook range | GrappleHook |
| Level damage | +2% damage on every level-up, whatever card is picked | LevelUpSystem |
| Zip (power swing) | Spring 120, pump 22 m/s², 20% reel-in; after 0.9 s held it reels in at 10 m/s with a 16 m/s² pull, down to 4 m; ends after 4 s or on release | GrappleHook |
| Wall jump | Space within 0.2 s of touching an island side: 14 m/s up, 8 m/s off the wall, 9 m/s towards W/A/S/D; 0.3 s cooldown | PlayerController |
| Air tricks | Above 24 m/s, airborne, no rope out, after 0.2 s: front flip, back flip or barrel roll at 620°/s, finishing the turn | WardenAnimator |
| Chest light | Spotlight 14 m above the chest (26°, warm gold, breathing), plus an additive light cone | PF_Chest |
| Speed FOV | Full at 40 m/s | CameraRig |
| Double jump | Space in the air when no wall is near: +11 m/s up (a fall counts as 0), once per flight, refilled on landing | PlayerController |
| Dash | Left Shift in the air: 32 m/s (or the current speed if faster) for 0.18 s towards W/A/S/D relative to the camera, or where the camera looks; once per flight, refilled on landing | PlayerController |
| Props | Rocks, ruins, crystals, bushes, lanterns and statues have mesh colliders (none before 2026-11-08) | ModelSetup |
| Sound mix | Effects 60%, music 45% by default; hits play at most every 0.09 s and two at once, deaths every 0.07 s, gems every 0.06 s; rare events louder | GameSettings, AudioWiring |

### Characters (2026-11-08)
| Character | Start | Unlock |
| --- | --- | --- |
| Warden | 100 health, Orbit Blades, 1 double jump, 1 dash | Always |
| Skyrunner | 80 health, +20% max flight speed, +8% walk speed, Shard Burst and Gale Feather, 2 dashes | Hold the watch for 5:00 in one run |
| Bulwark | 150 health, +2 armor, Orbit Blades and Ward Shell | 1,500 kills over all runs |
| Lumen | 90 health, +10% damage, +8% attack speed, Homing Sparks and Mending Moss, 2 double jumps | Reach level 15 in one run |

## Where the build differs from the GDD

| GDD | What was built | Why |
| --- | --- | --- |
| Island base shapes from Tripo, fixed in Blender MCP (8.3, Part D) | Every model is made by `Tools/Blender/hookfall_models.py`, run headless | Esad's choice on 2026-11-07; Tripo was not available to Claude |
| Warden rigged and animated with Mixamo; enemies with simple rigs (8.3) | Models are split into parts (arms, legs, wings, cape, lid); `WardenAnimator`, `EnemyBase` and `Golem` animate them in code | Mixamo needs Esad's Adobe login; 300 Animators would also cost more than moving a few transforms |
| Toon shader made in Shader Graph (8.1) | Shader Graph, generated from Unity's own Custom Lighting Toon sample and Unlit template by `Tools/ShaderGraph/make_graphs.js` | Keeps Shader Graph (Esad's choice) while the graph stays reproducible from a script |
| Spawn zone beyond 150 m (7.1) | 60 m out of view, 90 m ahead | At 150 m bats need about 12 s to arrive, which empties the screen |
| Own vector icons (8.3) | 3D symbols rendered in Blender as sprites, in diamond and card frames | Esad's choice on 2026-11-07 |
| Licensed sound and music (8.3) | Kenney CC0 recordings for the effects; wind, heartbeat and music synthesised in code by Tools/Audio/make_sfx.js | Esad's choices on 2026-11-08 (Docs/Credits.md); no AI audio model is used |
| Hook values 4.1: spring 4.5, damper 7, max air speed 35, strafe 6 | A taut rope with a downhill pump, 1.8x air gravity, 40 m/s with capped boosts, strafe 12, a power-swing Zip, wall jumps, air tricks | Esad found the swing slow and islands hard to climb (2026-11-08) |
| Spawn rate 2/s, +15% per minute, flocks of 4–8 (4.8) | 2/s, +20% per minute, flocks of 4–8, at most 120 alive | Esad found the first build too calm (2026-11-07: 5/s, flocks of 8–14, 300 alive), then far too crowded (2026-11-08) |
| Six items, eleven stats (4.5, 4.6) | A seventh chest item (Tether Coil) and two Zip stats | Added with the right-click Zip on Esad's request (2026-11-07) |
| Islands at any height, arena edge not described (4.9, 7.1) | Three height bands with a reachability check, and a storm wall at 195 m with far scenery beyond | Esad's feedback: islands could not be climbed and the edge felt empty |
