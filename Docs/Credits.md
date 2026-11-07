# Hookfall – Credits and licences

## Sound and music (to fill in)

No audio is AI-generated (GDD 8.3). Put licensed files in these folders with these exact names, then run **Hookfall > Build All Scenes**; the AudioManager picks them up by name. Any format Unity imports works (.wav, .ogg, .mp3).

`Assets/_Hookfall/Audio/SFX/`

| File name | Plays when |
| --- | --- |
| SFX_HookThrow | The hook is thrown |
| SFX_HookAttach | The hook bites into an island |
| SFX_HookRelease | The rope is let go |
| SFX_WindLoop | Loop; volume rises with speed |
| SFX_WeaponHit | Any weapon hits an enemy |
| SFX_EnemyDeath | An enemy dies |
| SFX_XPPickup | An XP gem is collected |
| SFX_GoldPickup | A gold coin is collected |
| SFX_ChestOpen | A chest opens |
| SFX_TowerCacheBeam | The tower cache appears |
| SFX_TowerCacheOpen | The tower cache opens |
| SFX_LevelUp | The level-up cards appear |
| SFX_PlayerHurt | The Warden takes damage |
| SFX_HeartbeatLoop | Loop; plays below 30% health |
| SFX_GolemSlam | A golem slams the ground |
| SFX_BoulderBreak | A boulder breaks |
| SFX_EliteWave | An elite wave arrives |
| SFX_RunEnd | The run ends |

`Assets/_Hookfall/Audio/Music/`: `MUS_Layer1`, `MUS_Layer2`, … — stems of one track, all the same length. Layer 1 plays from the start; one more layer joins every 2 minutes.

| File | Source | Author | Licence |
| --- | --- | --- | --- |
| | | | |

## Code and assets

| What | Source | Licence |
| --- | --- | --- |
| Toon lighting subgraphs (`Art/Shaders/CustomLighting`) and the base of SG_Toon / SG_Outline | Unity Shader Graph package, Custom Lighting sample and Unlit template | Unity Companion License |
| LiberationSans font (`TextMesh Pro`) | TextMesh Pro Essential Resources | SIL Open Font License |
| Models (`Art/Models`) | Made with `Tools/Blender/hookfall_models.py` (Blender 5.2, scripted with Claude) | Own work |
| Game code | Written with Claude (Anthropic); see Docs/PromptLog.md | Own work |
