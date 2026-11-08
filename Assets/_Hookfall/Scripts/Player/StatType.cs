// Hookfall - the stat upgrades offered on level-up: the eleven from GDD 4.6, the two Zip stats added on 2026-11-07, and
// Lifesteal added on 2026-11-08 (keep new values at the end: the order is saved in assets).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Player
{
    public enum StatType
    {
        MaxHealth,
        Armor,
        Damage,
        AttackSpeed,
        WalkSpeed,
        FlightSpeed,
        HookRange,
        PickupRadius,
        Regen,
        XPGain,
        Luck,
        ZipCooldown,
        ZipRange,
        Lifesteal
    }
}
