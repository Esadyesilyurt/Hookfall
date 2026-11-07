// Hookfall - one level-up card: a level for an owned item, or a stat upgrade (GDD 4.6, 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Player;

namespace Hookfall.Progression
{
    public readonly struct LevelUpOption
    {
        public readonly ItemBase Item;   // null for a stat card
        public readonly StatType Stat;

        public LevelUpOption(ItemBase item) { Item = item; Stat = default; }
        public LevelUpOption(StatType stat) { Item = null; Stat = stat; }

        public bool IsItem => Item != null;
    }
}
