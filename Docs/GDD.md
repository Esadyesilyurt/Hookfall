# Hookfall – Midterm GDD

Exported from the Claude Docs GDD on 4 November 2026. Author: Esad Yesilyurt. The PDF submitted for the midterm is the same content with diagrams and images.

## 1. Title page

|  |  |
| --- | --- |
| Game title | **Hookfall** |
| Course | Game Studio: AI in Game Development, Istanbul Kent University |
| Team | Solo project (approved by the instructor) |
| Member and roles | Esad Yesilyurt: game design, programming, AI design, art and audio integration, production |
| Submission date | 07.11.2026 |
| Engine | Unity 6, C#, 3D |

## 2. One-page summary

**Elevator pitch.** Hookfall is a fast 3D survivor-like where you swing between floating islands above a bottomless abyss with a grappling hook. Your weapons fire only while you are airborne, so every swing is an attack and every landing is a risk.

|  |  |
| --- | --- |
| Genre | 3D action roguelite, survivor-like (auto-combat) with grappling-hook movement |
| Platform | PC (Windows), mouse and keyboard |
| Target player | Players of survivor-likes (Vampire Survivors, Megabonk) who also enjoy movement-heavy action; short, intense sessions |
| Play time | Endless runs with no final win state; a typical run ends in 5–10 minutes for a new player and grows longer with skill |

**Unique selling points**

1. **Combat only in the air.** Auto-attacks are active only while the player is swinging or flying, and stop on islands. Movement is the attack button.
2. **Physics-based swinging as the core verb.** Mouse-aimed grappling hook, momentum-driven swings and light A/D air strafe make moving through the sky the main source of fun.
3. **Islands as risk and reward.** Islands are safe ground but leave the player defenceless. Chests with new items spawn there, and the enemy AI reacts to the player landing by closing in.

## 3. Core gameplay

The player swings through the sky to fight and lands on islands only to loot, so the game constantly pushes them back into the air.

**Core loop (diagram as text).** Main loop: Throw the hook (aim with the mouse) → Swing and fly (auto-attacks on) → Kill enemies (drop XP and gold) → Level up (pick an upgrade) → repeat; enemies grow stronger over time.
Island detour: from Swing and fly → land → Land on an island (attacks off) → Open a chest (optional: new item) → hook off again → Throw the hook.

The top row is the main loop and runs every few seconds. The lower row is the optional island detour, where the player trades safety from the fall for being defenceless against enemies.

**Player goal.** Survive as long as possible and grow as strong as possible. There is no final win state: the run is endless and the player's target is their own record.

**Win condition.** None by design. Each run is scored by survival time, level reached and enemies killed, and the best run is saved as the record to beat.

**Lose conditions.**

- Health reaches zero.
- The player falls into the abyss below the islands.

**Controls (mouse and keyboard)**

| Input | In the air | On an island |
| --- | --- | --- |
| Mouse move | Aim the hook, move the camera | Aim the hook, move the camera |
| Left mouse button | Throw the hook; hold to stay attached, release to let go | Throw the hook to leave the island |
| A / D | Air strafe: a light sideways push, momentum stays in control | Walk left and right |
| W / S | Not used | Walk forward and back |
| Space | Not used | Jump |
| E | Not used | Open a chest |
| 1 / 2 / 3 or click | Pick an upgrade on level-up | Pick an upgrade on level-up |
| Esc | Pause | Pause |

Attacks are fully automatic and need no input.

## 4. Mechanics and systems

Hookfall runs on seven systems: hook and swing, island movement, player stats, airborne auto-combat, items, progression and difficulty scaling. All values below are starting points and will be tuned in the playtests in Weeks 12 and 13; every one is exposed in the Unity Inspector or a ScriptableObject so it can be changed without code.

### 4.1 Grappling hook and swing

The player aims with the mouse and throws the hook at any island surface in range. While attached, a physics joint turns the rope into a pendulum, so speed comes from gravity and the swing arc. Releasing keeps the current velocity, which launches the player through the air.

| Value | Start | Notes |
| --- | --- | --- |
| Hook max range | 40 m | Upgradable |
| Hook travel speed | 120 m/s | Time from click to attach |
| Rope reel-in on attach | 15% of rope length | Pulls the player up into the swing |
| Rope spring / damper | 4.5 / 7 | Rope stiffness (SpringJoint) |
| Re-throw cooldown | 0.2 s | Prevents spam |
| Max air speed | 35 m/s | Hard cap to keep control |
| Air strafe acceleration (A/D) | 6 m/s² | Light push; cannot reverse momentum |
| Gravity scale while airborne | 1.0 |  |
| Camera FOV | 70° to 90° | Widens with speed for a sense of velocity |

### 4.2 Island movement

| Value | Start |
| --- | --- |
| Walk speed | 7 m/s |
| Jump velocity | 6 m/s |
| Ground check distance | 0.2 m |

### 4.3 Player stats

| Stat | Start | Notes |
| --- | --- | --- |
| Max health | 100 |  |
| Health regeneration | 0 per s | Only through upgrades |
| Armor | 0 | Each point removes 1 damage per hit (min 1) |
| Invulnerability after a hit | 0.5 s |  |
| Pickup radius (XP, gold) | 4 m | Upgradable |
| Abyss kill height | 60 m below the lowest island | Falling past it ends the run |

### 4.4 Airborne auto-combat

The core rule: all weapons fire on their own cooldowns while the player is airborne and stop as soon as the player stands on an island. A short grace time stops combat from flickering on and off when the player touches an island for a moment.

| Value | Start |
| --- | --- |
| Combat turns on after leaving ground | 0.15 s |
| Combat turns off after landing | 0.3 s |
| Item slots | 6 |
| Max level per item | 5 |

### 4.5 Items (found in chests)

Items are the player's attacks. Each one attacks automatically and is found only in chests. Levelling an item raises the value in the last column.

| Weapon | Behaviour | Damage | Cooldown | Range | Per level |
| --- | --- | --- | --- | --- | --- |
| Orbit Blades | Blades circle the player and cut what they touch | 10 per hit | Hits each enemy max every 0.5 s | 3 m radius | +1 blade |
| Shard Burst | Fires shards in all directions | 12 | 1.5 s | 30 m/s shards | +2 shards |
| Arc Laser | A beam sweeps a full circle around the player | 20 | 2 s | 8 m | +15% damage |
| Ram Strike | Melee: hitting an enemy at speed deals damage | Speed × 1.5 | Above 20 m/s | Contact | +20% damage |
| Homing Sparks | Missiles seek the nearest enemies | 15 | 1.2 s | 25 m | +1 missile |
| Storm Aura | A damage field around the player | 4 per 0.25 s | Constant | 4 m | +0.5 m radius |

The player starts each run with Orbit Blades. If a chest rolls an item the player already owns, or all 6 slots are full, it gives a level for an owned item instead.

### 4.6 Stat upgrades (level-up)

Stats can be picked again and again with no cap, which is what makes growth endless. Movement stats are soft-capped at +100% so the swing stays controllable.

| Stat | Per pick |
| --- | --- |
| Max health | +20 |
| Armor (durability) | +1 |
| Base damage | +10% |
| Attack speed | +8% |
| Walk speed | +8% |
| Flight speed (max air speed) | +5% |
| Hook range | +5 m |
| Pickup radius | +25% |
| Health regeneration | +0.5 per s |
| XP gain | +10% |
| Luck (better chest rarity) | +10% |

### 4.7 Progression: XP, levels, gold and chests

- **XP.** Enemies drop XP gems: Bat 1, Wyvern 3, Golem 5. Pickup is automatic inside the pickup radius.
- **Levels.** XP needed for level *n* = 10 × n^1.4, rounded. On level-up the game pauses and offers 3 random choices, each either a level for an item the player owns or a stat upgrade from 4.6. Items themselves never come from level-ups.
- **Gold.** Enemies drop gold (Bat 1, Wyvern 2, Golem 5). Gold is spent on chests.
- **Chests.** The only source of new items. A chest spawns on a random island every 45 s, max 3 at once. Opening costs 15 gold, +10 for each chest opened this run. Rarity: Common 70%, Rare 25%, Epic 5% (Luck shifts the odds). Rarer items start at a higher level.

### 4.8 Spawning and difficulty scaling

The run gets harder with time so the endless growth always has pressure against it.

| Value | Start | Growth |
| --- | --- | --- |
| Enemy spawn rate | 2 per s | +15% per minute |
| Enemy health | Base value | × (1 + 0.15 × minutes survived) |
| Enemy damage | Base value | × (1 + 0.08 × minutes survived) |
| Max enemies alive | 300 | Fixed; object pooling and GPU instancing keep it running |
| Golems appear | From minute 2 |  |
| Elite wave (bigger, tougher enemies) | Every 5 minutes |  |

### 4.9 Islands

Islands are placed in a 300 m wide play area, 25 to 40 islands per run, spaced 15 to 35 m apart so a new hook point is always in range. Island size ranges from 4 m (a hook point) to 20 m (room for a chest and a golem).

## 5. In-game AI design

Enemy AI is Hookfall's in-game AI feature, and its goal is controlled chaos: hundreds of enemies that never wait their turn and all hunt the player at once, so the player survives by hit-and-run through the horde. Every enemy runs a finite state machine that reads the player's position, speed and whether they stand on an island. Landing makes every enemy more aggressive, so the safest place is always in motion.

### 5.1 Technique

- **Finite state machines in C#**, built with the State pattern: each state is a class with `Enter`, `Tick` and `Exit`. All enemies share the same framework with different states.
- **Steering behaviours** for flying enemies: pursuit (fly at where the player will be, not where they are), separation (just enough to stop the horde merging into one blob) and island avoidance with short raycasts. Enemies fly in open 3D air, so there is no NavMesh.
- **EnemyDirector**: one central script that spawns the horde (4.8). It places new waves ahead of the player's flight path and all around them, so there is never a quiet direction to escape to, and it raises the pressure over time. It never limits how many enemies attack at once.
- **Procedural islands**: the island layout is randomised every run (4.9). This is a small secondary AI element; enemy behaviour is the main one.

### 5.2 What enemies perceive

| Input | How it is read | Used by |
| --- | --- | --- |
| Player on an island or airborne | Flag from the player controller | All enemies (Frenzy) |
| Player position and velocity | Read on every AI tick | All enemies (pursuit aims at the predicted position) |
| Distance to the player | Read on every AI tick | Wyverns (charge), Golems (range) |
| Line of sight | Raycast from the golem to the player | Golems |

Enemies within 60 m of the player think 10 times per second; enemies further away think 2 times per second, which keeps a 300-enemy horde cheap.

### 5.3 Bat: the horde

Bats are the horde. They spawn in large flocks, chase the player non-stop and bite on contact; nothing makes them back off.

**Bat state machine (diagram as text).** Spawn in a flock → Chase (pursuit). Chase → Bite when within 1.5 m; Bite → back to Chase right after. Chase → Frenzy (+30% speed) when the player lands; Frenzy → Chase when the player takes off; Frenzy → Bite when within 1.5 m. Any state → Dead at 0 health (drops XP and gold). No limit on how many bats attack at once.

Chase uses pursuit: bats fly at the player's predicted position, so a straight-line escape does not work and the player has to swing past them. While the player stands on an island the whole flock switches to Frenzy and moves 30% faster. Stats: 10 health, 12 m/s, 5 bite damage, no limit on how many bite at once.

### 5.4 Wyvern: heavy flyer

Wyverns are tougher flyers that charge through the horde at the player.

| State | Behaviour | Leaves when |
| --- | --- | --- |
| Chase | Pursues the player like a bat | Within 15 m |
| Charge | 0.3 s wind-up, then a straight dash at 30 m/s | Dash ends |
| Recover | Slows down for 0.5 s, then straight back to Chase | Time is up |

If the player is on an island, the wyvern charges from above and its wind-up drops to 0.15 s. Stats: 60 health, 15 charge damage.

### 5.5 Golem: ranged tank on islands

Golems stand on islands and throw boulders at the airborne player. They are the reason flying is never fully safe.

**Golem state machine (diagram as text).** Idle → Aim (warning line) when the player is in range and sight; Aim → Idle if it loses sight; Aim → Throw boulder after 0.4 s; Throw boulder → Cooldown; Cooldown → Aim again after 1.5 s. Idle → Ground slam when the player lands on its island; Ground slam → Cooldown. Any state → Dead at 0 health (drops XP and gold).

The golem aims at where the player will be, with a random error of up to 2 m, and shows a short 0.4 s aim line before each throw so the boulder stays readable inside the chaos. If the player lands on its island, it switches to a ground slam. Stats: 150 health, boulder 20 damage at 25 m/s, 35 m range, 1.5 s between throws, slam 25 damage in a 3 m radius.

### 5.6 Why it is fun and believable

- **Chaos is the point.** Up to 300 enemies, none waiting for a turn. The fun is surviving and cutting through the horde, as in Megabonk.
- **Hit-and-run is the only answer.** Pursuit aims where the player is going, so hovering or flying straight gets punished. The player has to keep swinging, slice through the flock and change direction.
- **It feeds the core choice.** Landing sends every enemy into Frenzy; flying exposes the player to boulders and charges. There is no fully safe place.
- **Short warnings keep the chaos readable.** Wyverns and golems wind up for 0.3 s and 0.4 s, so the player loses to the horde, not to attacks they could not see.
- **The horde feels alive.** Imperfect golem aim, a flock that spreads instead of stacking, and waves that appear ahead of the player make enemies feel like they are hunting, not moving on rails.

### 5.7 Fallback behaviour

| What goes wrong | How it is detected | Fallback |
| --- | --- | --- |
| A flyer is stuck behind an island | Moved less than 1 m in 3 s | Short upward avoidance; if still stuck, respawn outside the camera view |
| An enemy is too far from the player | More than 120 m away | Despawn and respawn closer, outside the camera view |
| A golem loses sight while aiming | Line-of-sight raycast fails | Cancel the throw, back to Idle |
| An enemy falls into the abyss | Below the abyss kill height | Despawn with no reward |
| A state machine reaches an invalid state | No current state | Reset to Chase (golem: Idle) |
| Too many enemies or low frame rate | 300 alive or below 50 FPS | Pause spawning, far enemies think only 2 times per second, fewer hit effects |

## 6. World, story and characters

The player is a Warden guarding the Heartstone, a gravity-bending relic kept in a lone tower at the centre of a shattered sky, and every creature that attacks them is drawn to its power. The story frames the action; it is told through a short intro and item descriptions, with no cutscenes.

**Setting.** Long ago a kingdom built a fortress around the Heartstone, a relic strong enough to bend gravity. When the fortress fell, the land around it broke apart, but the pieces never fell: the Heartstone's pull holds them in the sky above an abyss with no visible bottom. Of the old fortress, only one archer tower still stands, on the large island at the centre.

**Premise.** The Order of the Tether, last guardians of the fallen kingdom, sends one warrior at a time to protect the Heartstone. Each run is one Warden's watch: how long they hold before the horde or the abyss takes them.

**Characters**

| Character | Role | Description |
| --- | --- | --- |
| The Warden | Player | A warrior of the Order who fights with a grappling hook and enchanted weapons from the fortress vaults (the items). |
| The Heartstone | Relic | A glowing core inside the tower. It holds the islands in the sky and weakens gravity around the tower. |
| Bats | The horde | Small creatures twisted by the Heartstone's magic; they swarm anything near it. |
| Wyverns | Heavy flyers | Larger beasts drawn by the same power; they charge at intruders. |
| Golems | Fallen guardians | Stone sentries of the old fortress that now attack anyone, the Warden included. |

**Tone.** Mysterious, slightly melancholic fantasy under a calm, soft daylight, played at high speed: a lonely tower, a glowing relic and an endless sky. Serious, not cartoonish.

**How the story is told.** A short intro text before the first run, one line of lore for every item and enemy (readable in the pause menu), and a run-end screen that says how long the Warden held the watch.

## 7. Level and content design

Hookfall has one arena, rebuilt every run: a fixed tower island at the centre, procedural rings of islands around it and the abyss below.

**Arena map (diagram as text, top-down, not to scale).** Centre: the tower island with the Heartstone and the tower cache, inside a low-gravity circle. Around it the inner ring of big islands (golems and normal chests), then the outer ring of small hook-point islands. Enemies spawn outside the arena, ahead of the player's flight path. The abyss is below everything; falling ends the run. Island positions are procedural every run; the tower never moves.

The tower is the only landmark that never moves, so the player can always orient themselves; everything around it changes from run to run.

### 7.1 Arena zones

| Zone | Distance from centre | What is there |
| --- | --- | --- |
| Tower island | 0 to 20 m | The tower and the Heartstone; the tower cache on top; gravity scale 0.7 within 40 m of the tower |
| Inner ring | 40 to 100 m | 8 to 12 large islands (10 to 20 m wide) with golems and normal chests |
| Outer ring | 100 to 150 m | 15 to 25 small islands (4 to 8 m wide), mainly hook points for fast traversal |
| Spawn zone | Beyond 150 m and ahead of the player | Enemy spawns, out of camera view |
| Abyss | Below all islands | Falling past the kill height ends the run |

### 7.2 The tower cache

The tower cache is the rarest and most dangerous chest: a strong item in exchange for standing still in the middle of the horde.

- Spawns on top of the tower every 3 minutes, at most one at a time.
- Opening takes 5 seconds of standing on the tower top (normal chests open instantly), while every enemy is in Frenzy.
- Costs no gold and is always Rare or better: Rare 60%, Epic 40%.
- A beam of light above the tower shows when it is ready, visible from anywhere in the arena.

### 7.3 Content list

| Type | Content | Count |
| --- | --- | --- |
| Enemies | Bat, Wyvern, Golem, plus an elite version of each (bigger, more health, glowing) | 3 + 3 elites |
| Items | Orbit Blades, Shard Burst, Arc Laser, Ram Strike, Homing Sparks, Storm Aura | 6 |
| Stat upgrades | The stats in 4.6 | 11 |
| Chests | Normal chest, tower cache | 2 |
| Pickups | XP gem (small, medium, large), gold coin | 4 |
| Environment | The tower, large and small island shapes (3 each), rocks, ruins, crystals | About 10 models |
| Text | Intro text, one lore line per item and enemy, UI text | About 15 lines |

## 8. Art and audio direction

Hookfall uses toon-shaded fantasy in calm, soft daylight: clean silhouettes, hard light-and-shadow bands and outlines, so a 300-enemy horde stays readable against the sky. Not dark, not sunny and bright: a calm, neutral light. Models are made in Blender with AI assistance; sound and music are licensed, not generated.

### 8.1 Visual style

- **Shading.** Cel shading with 2 to 3 light bands and a thin outline, built in Unity URP with Shader Graph. Most of the look comes from the shader, so models can stay simple.
- **Shapes.** Chunky, slightly exaggerated forms. Floating islands have flat grassy tops and long rocky undersides that hang into the abyss, like the stylized floating-island models collected in the mood board.
- **Not low-poly, not cartoon.** Smooth, rounded shapes with clear outlines; proportions stay grounded and the palette stays muted.
- **Motion effects.** Speed lines and a wider camera at high speed, trails behind the hook and the Warden, a white flash on every hit.

**Palette and readability rules**

| Element | Colour | Why |
| --- | --- | --- |
| Sky | Soft blue with stylized, hard-edged clouds; calm daylight | Calm, neutral background; nothing is lost in darkness or glare |
| Abyss | Fades into a pale blue haze below the islands | Shows depth and danger without a black void |
| Islands | Warm grey stone, muted moss green | Calm, so they never compete with enemies |
| Heartstone, Warden, player weapons | Cyan and teal glow | One hue for everything that is the player's |
| Enemies | Purple and magenta accents, dark bodies | Saturated and darker than the sky, so they always stand out |
| Rewards (XP, gold, chests) | Warm yellow and orange | Easy to spot in the chaos |
| Attack warnings | Red | Only used for danger |

### 8.2 Mood board

Nine references collected on Pinterest; each row says what Hookfall takes from the image, not a copy of it.

| # | Reference | What we take from it |
| --- | --- | --- |
| 1 | Floating island with trees | Silhouette: flat grassy top, tapering rocky underside (not its faceted low-poly surface) |
| 2 | Cliff edge above the clouds, cel-shaded illustration | The target look: hard shadow bands, clean outlines, green moss over grey-violet stone |
| 3 | Stylized sky with painted clouds | The sky: soft blue with hard-edged, cel-shaded clouds |
| 4 | Stone watchtower | The tower: round stone tower, battlements and a wooden top deck where the tower cache sits |
| 5 | Glowing crystal cluster | The Heartstone: cyan-to-violet crystal with a soft glow and sparkles |
| 6 | Moss-covered stone golem | The Golem: stacked boulders with moss and grass, glowing eyes (recoloured to the enemy magenta) |
| 7 | Purple bat creatures | The Bat: long ears, wide wings, purple-pink body, simplified for the toon shader |
| 8 | Flat-shaded wyvern | The Wyvern: lean body and large wings, recoloured to the enemy purple range |
| 9 | Rune-style upgrade icons | UI: diamond icon frames and pale, rune-like backgrounds for level-up and item cards |
| — | Megabonk | Horde density and run pacing (gameplay reference, not art) |

*(Mood board image: 9 Pinterest references, numbered as in the table above. Only in the PDF and the Claude Docs version.)*

Mood board images collected on Pinterest; numbers match the table above. They are references only and will not appear in the game.

### 8.3 What is made, generated or licensed

| Asset | Source | How |
| --- | --- | --- |
| Islands (3 large, 3 small), rocks, ruins, crystals | Made | Base shapes generated in Tripo, then fixed and reshaped in Blender through the Blender MCP connection with Claude |
| The tower | Made | Blender MCP, same pipeline |
| The Warden | Made | Blender MCP model; rig and animations from Mixamo |
| Bat, Wyvern, Golem | Made | Blender MCP, simple rigs and a few animations each |
| Toon shader and outline | Made | Unity Shader Graph |
| Visual effects | Made | Unity Particle System / VFX Graph |
| UI and icons | Made | Own simple vector icons |
| Sound effects | Licensed | Royalty-free libraries (for example Kenney, Sonniss GDC bundles, Freesound files with CC0 or CC-BY); each licence recorded in a credits list |
| Music | Licensed or collaborator | Royalty-free music library or a composer friend; no AI-generated audio |

### 8.4 Audio direction

- **Music.** Fast, percussion-heavy adventure fantasy. Layers are added as difficulty rises, so the music is fullest during elite waves.
- **Key sounds.** The hook (throw, attach, release) is heard most and must feel best, followed by wind that rises with speed, weapon hits, enemy deaths, XP and gold pickups, chest opening, the tower cache beam and a low-health heartbeat.
- **Mixing the chaos.** The same sound plays at most 8 times at once, so 300 enemies do not turn into noise.

## 9. User interface

The HUD sits on the screen edges and leaves the centre to the horde; the only centre elements are the hook crosshair and the attack status.

**HUD wireframe (as text).** Top edge: XP bar across the screen. Top left: level and health bar with "Health 72 / 100". Top centre: watch time (mm:ss). Top right: gold and kills. Right edge: "Tower cache ready" marker pointing to the tower. Centre: hook crosshair (lights up when an island is in range) and the ATTACKS ON / ATTACKS OFF pill below it. Under that: "[E] Open chest, cost" prompt only near a chest. Bottom left: 6 item slots with levels. Bottom right: "Esc Pause".

The two centre signals matter most during play: whether the hook will reach, and whether attacks are on.

### 9.1 Screens

| Screen | Contents | Reached from |
| --- | --- | --- |
| Main menu | Play, Settings, Credits, Quit; best run (watch time, level, kills) | Game start |
| Intro | Three lines of story and a "Hold the watch" button; skippable | First Play of a session |
| HUD | XP bar, level, health, watch time, gold, kills, crosshair, attack status, item slots, tower cache marker | Run start |
| Level-up | Game pauses; 3 cards, each an item level or a stat, with icon, name and current → new value; pick by click or 1, 2, 3 | Reaching a new level |
| Chest result | One item card in its rarity colour, marked as new item or level-up | Opening a chest |
| Pause | Resume, Settings, Quit to menu; owned items and stats; lore lines | Esc |
| Run end | Watch time, level, kills, gold, items; "New record" when beaten; Retry or Menu | Death or falling into the abyss |
| Settings | Mouse sensitivity, music and effects volume, speed effects on or off, resolution | Main menu, Pause |

## 10. Scope

The plan is to build all three tiers, but the must-have list alone is a complete, playable game, so it is finished first and the other tiers are added on top of it.

| Priority | Features |
| --- | --- |
| **Must have** (playable at the final) | Grappling hook, swinging and air strafe · airborne-only auto-combat · Bat, Wyvern and Golem with their state machines, Frenzy and fallbacks · EnemyDirector spawning and difficulty scaling · 3 to 4 items · XP, level-up and stat upgrades · gold and normal chests · procedural island rings around the tower · death by health or falling, run-end screen · HUD and a basic main menu · basic sound effects |
| **Should have** | All 6 items · tower cache · low-gravity zone around the tower · elite waves · final toon shader and outlines · saved best run · music |
| **Nice to have** | Intro text and lore lines · layered music that rises with difficulty · extra visual effects (speed lines, trails) · full settings menu |

## 11. References

**Games**

- vedinad. (2025). *[Megabonk](https://store.steampowered.com/app/3405340)* \[PC game\]. Main reference for horde density, endless growth and run pacing.
- poncle. (2022). *Vampire Survivors* \[PC game\]. Origin of the auto-attack survivor-like loop.

**Game AI and programming**

- Reynolds, C. W. (1987). Flocks, herds and schools: A distributed behavioral model. *Computer Graphics (SIGGRAPH '87)*, 21(4), 25–34. Basis of separation in the bat flocks.
- Reynolds, C. W. (1999). Steering behaviors for autonomous characters. *Game Developers Conference 1999*. Seek, pursuit and evade steering.
- Buckland, M. (2004). *Programming Game AI by Example*. Wordware. Finite state machines and steering for game agents.
- Nystrom, R. (2014). *Game Programming Patterns*, chapter "State". Genever Benning. The State pattern used for enemy AI.
- Yannakakis, G. N., & Togelius, J. (2018). *Artificial Intelligence and Games*. Springer. Game AI and procedural content generation.

**Tools**

- Unity 6, Universal Render Pipeline and Shader Graph (Unity Technologies)
- Blender (Blender Foundation) with the Blender MCP connection
- Mixamo (Adobe), character rig and animations; Tripo, AI 3D model generation
- Claude (Anthropic), AI assistant for design writing, code and Blender modelling (see the AI Tool Plan)

Sound and music sources are listed with their licences in the project's credits file.

## AI Tool Plan

AI appears in Hookfall in two places: inside the game as enemy behaviour written in my own C#, and in my workflow as Claude for code, writing and Blender modelling. Audio is deliberately made without AI.

### Part A: AI in the game

| AI feature | What the player experiences | Technique and tool | Why AI makes the game better |
| --- | --- | --- | --- |
| Enemy behaviour (Bat, Wyvern, Golem) | A horde that chases, bites, charges and throws, and gets more aggressive when the player lands | Finite state machines and steering behaviours (pursuit, separation, island avoidance) in my own C# | Turns the airborne-combat rule into a real decision and creates the chaos the game is built on (section 5) |
| EnemyDirector | New waves keep appearing ahead of and around the player; pressure rises over time | Rule-based director in my own C#: spawn placement from the player's velocity, difficulty curves from 4.8 | Prevents safe corners and keeps the endless run tense |
| Procedural island layout | A different arena every run around the same tower | Seeded random placement with spacing rules in my own C# (4.9, 7.1) | Every run plays differently; no two routes are the same |

No AI API, LLM or online model runs inside the game. Every in-game AI feature is my own code, so the game works offline, costs nothing per player and needs no API keys.

### Part B: AI in my workflow

| Task area | Tool | What I will use it for | How I will check the output | Cost and access |
| --- | --- | --- | --- | --- |
| Coding (C#) | Claude (chat now, Claude Code in Weeks 10 to 14) | Script skeletons, the state machine framework, steering maths, refactoring | I read every line before committing; test in Play mode with no Console errors; AI-assisted scripts carry a disclosure comment at the top; I must be able to explain each method without looking | My Claude subscription |
| Art and 3D models | Tripo (AI 3D generation), then Claude with the Blender MCP connection | Island and rock base shapes generated in Tripo, then fixed and reshaped in Blender through Blender MCP; tower, Warden and enemy base meshes built in Blender from prompts | Compare against the mood board (8.2); check scale, normals and triangle budget in Unity (enemies under 5,000 triangles, large islands under 10,000); clean up by hand | Blender and Blender MCP are free; Claude subscription; Tripo plan to be chosen after checking its licence terms |
| Shaders and visual effects | Claude for guidance on Shader Graph and particles | Toon shader, outline, hit flash, trails | Visual check against the mood board; frame-rate check with the Unity Profiler | Included above |
| Audio and music | None (deliberately no AI) | — | Every file comes from a licensed library or a collaborator and is listed with its licence in the credits file | Free royalty-free libraries |
| Writing and dialogue | Claude | Drafting GDD sections, item and enemy names, lore lines | I review and edit every section; numbers are checked against the actual game; Part C is written by me, not generated | Claude subscription |
| Testing and debugging | Claude, plus the Unity Profiler (not AI) | Reading error logs and stack traces, proposing fixes, finding performance problems | A fix counts only after I reproduce the bug, apply the fix and see it gone in Play mode; performance target: 60 FPS with 300 enemies, confirmed in the Profiler | Claude subscription; Profiler is part of Unity |
| In-game runtime AI | None (own C#) | — | Covered by Part A and the fallback table in 5.7 | Free |

All AI conversations are recorded in the prompt log (date, tool, prompt, result, what I changed).

### Part C: Limits and risks

**Where will I deliberately not use AI, and why?** I deliberately do not use AI for audio, because in my view AI-generated sounds do not give the right feel.

**What could go wrong, and what is the fallback?** The point that could go wrong is the models I will make with Blender MCP through Claude; the models may contain errors. If there is a problem here, I plan to use Tripo instead.

**Who owns and can use the generated assets?** The generated assets are owned either by the AI or by me. At this point it comes down to ethics.

**How will I keep my work understandable?** I have no condition before adding what the AI writes or makes to the project. The condition starts afterwards: if something we added breaks the flow of the game, its fun factor or anything else, we remove it.

### Part D: Evidence of a first test

The first test showed that Blender MCP follows technical specifications exactly but does not reach the art style on its own, so island shapes will now start in Tripo and be fixed in Blender MCP.

**Tool and date.** Claude with the Blender MCP connection, Blender 5.2.3, 4 November 2026.

**Prompt.**

> Using Blender, create a stylized floating island for a 3D game: a flat grassy top about 12 m wide and a rocky underside that tapers to a point about 10 m below. Use smooth, rounded rock forms, not faceted low-poly. Use separate materials for grass (muted moss green) and stone (warm grey). Keep it under 10,000 triangles, put the origin at the top centre and apply scale. Then take a viewport screenshot.

**Result.**

| Check | Asked for | Got |
| --- | --- | --- |
| Triangles | Under 10,000 | 5,632 |
| Top surface | Flat, about 12 m wide | Flat at z = 0, about 12.5 m wide including the rounded grass edge |
| Underside | Tapers to a point about 10 m below | Rounded rock forms tapering to one point at z = −10 m |
| Materials | Grass and stone | Grass (muted moss green) and Stone (warm grey) |
| Origin and scale | Top centre, scale applied | Origin (0, 0, 0), scale (1, 1, 1) |

Blender also saved the scene (island.blend), a viewport render and the build script (make\_island.py), which rebuilds the island with new parameters. The script also had to work around Blender's first-start dialog to capture the screenshot.

*(Screenshot of the first Blender MCP island test. Only in the PDF and the Claude Docs version.)*

**What it taught me.**

- Every number in the prompt was met: size, triangle budget, materials, origin and scale.
- The look did not satisfy me. The top is a plain flat disc and the underside is smooth and blobby, far from the chunky stylized islands in the mood board (8.2, references 1 and 2).
- Blender MCP is reliable for exact, technical, repeatable work, but weak at artistic shape.
- New pipeline: generate island and rock base shapes in Tripo, then use Blender MCP to fix errors and reshape them, set scale and origin, assign materials and keep the triangle budget. Part B and 8.3 are updated to match.

## Technical and production plan

Hookfall is built solo in Unity 6 with URP, versioned on GitHub, in six weeks after the midterm: the core loop first (Weeks 9 and 10), the enemy AI next (Week 11), then content, playtests and polish.

### Technical setup

| Item | Choice |
| --- | --- |
| Engine | Unity 6000.4.0f1 |
| Render pipeline | Universal Render Pipeline (URP), Shader Graph for the toon shader and outline |
| Dimension | 3D |
| Input | Unity Input System package only (old Input Manager disabled), mouse and keyboard |
| Camera | Own ThirdPersonCamera script with speed-based field of view in the skeleton; Cinemachine third-person camera from Week 9 |
| Physics | Unity physics (PhysX); SpringJoint for the rope; Rigidbody player |
| UI text | TextMeshPro |
| Repository | GitHub: [Esadyesilyurt/Hookfall](https://github.com/Esadyesilyurt/Hookfall) |
| Branching rule | `main` always opens and runs. New work goes on a `feature/<name>` branch and is merged into `main` only when the scene plays with no Console errors. |
| Large files | Git LFS for models, textures and audio (.fbx, .blend, .png, .wav, .ogg) through Unity's standard .gitattributes; Unity's standard .gitignore. The repository also holds README.md, CLAUDE.md (context for Claude Code sessions) and Docs/PromptLog.md |

### Folder structure and naming rules

```
Assets/_Hookfall/
  Art/          Models, Materials, Shaders, Textures, VFX
  Audio/        Music, SFX
  Prefabs/      Player, Enemies, Islands, Items, UI
  Scenes/       MainMenu, Arena
  Data/         ScriptableObjects: Items, Stats, Enemies, Difficulty
  Scripts/
    Core/       GameManager, RunTimer
    Player/     PlayerController, GrappleHook, PlayerStats
    Combat/     CombatSystem, ItemBase, Items/
    Enemies/    EnemyDirector, EnemyBase, StateMachine, States/
    World/      IslandGenerator, ChestSpawner, Chest, TowerCache, GravityZone
    Progression/ XPSystem, LevelUpSystem, GoldWallet
    UI/         HUD, LevelUpUI, RunEndUI, MainMenuUI
```

- One class per file; the file name equals the class name, in PascalCase.
- Namespaces follow the folders: `Hookfall.Player`, `Hookfall.Enemies` and so on.
- Asset prefixes: `PF_` prefabs, `M_` materials, `T_` textures, `SO_` ScriptableObject assets, `SFX_` and `MUS_` audio.
- Every tunable value from section 4 lives in a ScriptableObject or a serialized field, never hard-coded.
- Every AI-assisted script starts with a disclosure comment naming the tool and date.

### Main scripts and how they connect

**Core systems (diagram as text).** Player (PlayerController with IsAirborne, GrappleHook, PlayerStats) → IsAirborne → Combat (CombatSystem, ItemBase, six item classes) → damage → Enemies (EnemyDirector, EnemyBase + StateMachine, states) → XP, gold → Progression (XPSystem, LevelUpSystem, GoldWallet) → upgrades → Player. Enemies also read the player's position, velocity and IsAirborne every AI tick. GameManager starts, pauses and ends the run.

The whole game turns on one flag: `PlayerController.IsAirborne` switches combat on and off and puts enemies into Frenzy.

| Script | Folder | Responsibility |
| --- | --- | --- |
| GameManager | Core | Run state (menu, playing, paused, level-up, dead), timer, record saving |
| PlayerController | Player | Walking, jumping, air strafe, ground check; exposes `IsAirborne` |
| GrappleHook | Player | Mouse aiming, hook throw, SpringJoint rope, release |
| PlayerStats | Player | Health, armor, speeds, damage and all stat upgrades |
| CombatSystem | Combat | Enables items only while airborne, with the grace times from 4.4 |
| ItemBase | Combat | Base class for the 6 items: cooldown, level, damage; each item inherits it |
| EnemyDirector | Enemies | Spawn timing and placement, difficulty scaling, enemy cap, object pooling |
| EnemyBase, StateMachine, States | Enemies | Health, steering, the finite state machine and its states (section 5) |
| IslandGenerator | World | Seeded procedural island rings around the tower |
| ChestSpawner, Chest, TowerCache, GravityZone | World | Chests, the tower cache and the low-gravity zone |
| XPSystem, LevelUpSystem, GoldWallet | Progression | XP and levels, the 3-choice level-up, gold |
| HUD, LevelUpUI, RunEndUI, MainMenuUI | UI | The screens in section 9 |

**External packages, APIs and keys.** Only Unity packages are used: URP and Input System now, Cinemachine and TextMeshPro from Week 9. The skeleton's Arena scene is built by an editor menu (Hookfall > Build Arena Scene) so it can be rebuilt at any time. The game calls no external API and needs no API keys, so nothing secret can be committed to the repository.

### Production schedule

| Week | Course topic | Milestone | My tasks |
| --- | --- | --- | --- |
| 8 | Midterm | Pre-production delivered (07.11.2026) | GDD, AI Tool Plan, this plan, repository skeleton, prompt log |
| 9 | Break | First playable core loop (optional) | Hook, swing and air strafe; camera; one test island |
| 10 | Embedding AI APIs into Games | Core loop playable with placeholder art | Airborne-only combat, Orbit Blades, XP and level-up, death and run end, procedural island rings |
| 11 | PCG and Generative AI | In-game AI feature working | Bat, Wyvern and Golem state machines, EnemyDirector, Frenzy, fallbacks, 300-enemy performance test; Tripo + Blender MCP island pipeline |
| 12 | LLM-Powered NPCs and Game Characters | Content complete, first playtest | All items and stats, chests and gold, tower cache, gravity zone, elite waves, final models; first playtest |
| 13 | Open-Source, Local AI and Workshop | Feature freeze, polish, second playtest | Toon shader and outline, visual effects, sound and music, final UI, tuning from playtest notes |
| 14 | Finalization | Final build delivered | Bug fixes, optimisation, final build, delivery |

### Task board

Tasks are tracked as GitHub Issues in the project repository: 25 tasks, each assigned to its owner (Esad Yesilyurt, solo developer), placed in its week with a milestone (Week 9 to Week 14) and tagged by area and by scope priority (must / should / nice to have).

**Board link:** [github.com/Esadyesilyurt/Hookfall/milestones](https://github.com/Esadyesilyurt/Hookfall/milestones) (all tasks: [issues](https://github.com/Esadyesilyurt/Hookfall/issues))

| Week | Tasks | Examples |
| --- | --- | --- |
| 9 | 3 | Hook, swing and air strafe feel; hook projectile; Cinemachine camera |
| 10 | 4 | XP gems and gold pickups; 3-choice level-up screen; run-end screen; main menu |
| 11 | 5 | Wyvern and Golem state machines; steering; pooling and 300-enemy test; island art pipeline |
| 12 | 6 | All 6 items; chests; tower cache and gravity zone; elite waves; final models; first playtest |
| 13 | 5 | Toon shader; VFX; licensed audio; final HUD; second playtest |
| 14 | 2 | Bug fixes and optimisation; final build and delivery |

### Risks

| Risk | Early warning | What I cut if it happens |
| --- | --- | --- |
| The swing does not feel good, and the whole game depends on it | Still feels stiff at the end of Week 10 | Drop the nice-to-have list and spend that time on tuning movement; the tower cache and gravity zone move to after the final |
| 300 enemies drop the frame rate below 60 FPS | Profiler test in Week 11 | Lower the cap to 150, simpler enemy meshes, fewer hit effects |
| The Tripo + Blender MCP art pipeline is too slow or inconsistent | Fewer than half of the models done by the end of Week 12 | Use free, licensed stylized asset packs for rocks and ruins and let the toon shader unify the look; keep only the Warden, tower and enemies custom |
