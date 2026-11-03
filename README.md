# Hookfall

A fast 3D survivor-like: swing between floating islands above a bottomless abyss with a grappling hook. Your weapons fire only while you are airborne, so every swing is an attack and every landing is a risk.

Solo project by Esad Yesilyurt for *Game Studio: AI in Game Development* (Istanbul Kent University, Fall 2026). This repository is the midterm pre-production skeleton; the final game is built in Weeks 9 to 14.

## Open and run

1. Open the folder in Unity Hub with **Unity 6000.4.0f1** (Universal Render Pipeline).
2. In the menu bar choose **Hookfall > Build Arena Scene**. This creates `Assets/_Hookfall/Scenes/Arena.unity`, its placeholder materials and the Bat prefab, and puts the scene first in Build Profiles.
3. Press **Play**.

## Controls

| Input | Action |
| --- | --- |
| Mouse | Aim and turn the camera |
| Left mouse button | Throw the hook at an island; hold to swing, release to let go |
| A / D | Walk on islands; light air strafe while flying |
| W / S | Walk on islands |
| Space | Jump (on islands) |
| Esc | Pause |
| R | Retry after the run ends |

Attacks are automatic and only fire while you are in the air.

## What the skeleton contains

- Grappling hook with a SpringJoint rope, island movement and air strafe
- Airborne-only combat with the first item, Orbit Blades
- The in-game AI: Bat finite state machine (Chase, Bite, Frenzy) and the EnemyDirector that spawns the horde ahead of the player and scales difficulty
- Procedural island rings around a fixed tower, generated every run
- XP, levels, gold, a placeholder level-up reward, death by health or by falling, and a debug HUD

## Repository layout

```
Assets/_Hookfall/   all game content (Art, Audio, Data, Editor, Prefabs, Scenes, Scripts)
Docs/               prompt log for AI use
CLAUDE.md           project context for Claude Code sessions
```

## AI use

Scripts in this skeleton were written with Claude (Anthropic); each one says so in its header comment. All AI use is logged in [Docs/PromptLog.md](Docs/PromptLog.md). The game itself calls no AI service and needs no API keys.
