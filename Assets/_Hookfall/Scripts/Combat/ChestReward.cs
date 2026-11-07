// Hookfall - what a chest gave: a new item, levels for an owned item, or a stat when every item is maxed.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;

namespace Hookfall.Combat
{
    public readonly struct ChestReward
    {
        public readonly Rarity Rarity;
        public readonly ItemBase Item;      // null when the fallback stat was given
        public readonly bool IsNewItem;
        public readonly int LevelsGained;
        public readonly StatType FallbackStat;

        public ChestReward(Rarity rarity, ItemBase item, bool isNew, int levels, StatType fallback = default)
        {
            Rarity = rarity;
            Item = item;
            IsNewItem = isNew;
            LevelsGained = levels;
            FallbackStat = fallback;
        }
    }
}
