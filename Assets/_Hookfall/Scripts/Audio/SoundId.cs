// Hookfall - every sound effect the game plays (GDD 8.4). A clip named SFX_<id> in Audio/SFX is wired automatically
// by the scene builder.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Audio
{
    public enum SoundId
    {
        HookThrow,
        HookAttach,
        HookRelease,
        WeaponHit,
        EnemyDeath,
        XPPickup,
        GoldPickup,
        ChestOpen,
        TowerCacheBeam,
        TowerCacheOpen,
        LevelUp,
        PlayerHurt,
        GolemSlam,
        BoulderBreak,
        EliteWave,
        RunEnd,
        WallJump,
        Trick
    }
}
