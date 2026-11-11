// Hookfall - what a Warden keeps between runs (added 2026-11-11): every run pays Heartstone shards, and the menu's
// upgrade shop turns them into small permanent upgrades - a free pick of a stat at the start of every run, or one
// more reroll or banish. Three levels each, saved in PlayerPrefs ("hf_shards", "hf_up_<id>").
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    public static class MetaProgress
    {
        public const string ShardsKey = "hf_shards";
        public const string UpgradeKeyPrefix = "hf_up_";
        public const int MaxLevel = 3;

        /// <summary>The price of level 1, 2 and 3 of any upgrade.</summary>
        public static readonly int[] Costs = { 40, 90, 160 };

        public readonly struct Upgrade
        {
            public readonly string Id, Name, Icon;
            public readonly StatType? Stat;   // a free pick of this stat per level, or null for the two below
            public readonly int Rerolls, Banishes;

            public Upgrade(string id, string name, StatType stat) { Id = id; Name = name; Icon = null; Stat = stat; Rerolls = 0; Banishes = 0; }
            public Upgrade(string id, string name, string icon, int rerolls, int banishes) { Id = id; Name = name; Icon = icon; Stat = null; Rerolls = rerolls; Banishes = banishes; }
        }

        public static readonly Upgrade[] Upgrades =
        {
            new Upgrade("health", "Vigour", StatType.MaxHealth),
            new Upgrade("damage", "Might", StatType.Damage),
            new Upgrade("flight", "Tailwind", StatType.FlightSpeed),
            new Upgrade("pickup", "Reach", StatType.PickupRadius),
            new Upgrade("luck", "Fortune", StatType.Luck),
            new Upgrade("xp", "Insight", StatType.XPGain),
            new Upgrade("reroll", "Second Thoughts", "Reroll", 1, 0),
            new Upgrade("banish", "Exile", "Banish", 0, 1),
        };

        public static int Shards => PlayerPrefs.GetInt(ShardsKey, 0);
        public static int LevelOf(string id) => Mathf.Clamp(PlayerPrefs.GetInt(UpgradeKeyPrefix + id, 0), 0, MaxLevel);
        /// <summary>The price of the next level, or -1 when there is none.</summary>
        public static int CostOf(string id) => LevelOf(id) >= MaxLevel ? -1 : Costs[LevelOf(id)];

        public static void Add(int shards)
        {
            if (shards <= 0) return;
            PlayerPrefs.SetInt(ShardsKey, Shards + shards);
            PlayerPrefs.Save();
        }

        public static bool TryBuy(string id)
        {
            int cost = CostOf(id);
            if (cost < 0 || Shards < cost) return false;
            PlayerPrefs.SetInt(ShardsKey, Shards - cost);
            PlayerPrefs.SetInt(UpgradeKeyPrefix + id, LevelOf(id) + 1);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Shards for a run: one per level, two per minute of the watch, ten per stage cleared.</summary>
        public static int Reward(int level, int stage, float seconds) => level + Mathf.FloorToInt(seconds / 30f) + 10 * Mathf.Max(0, stage - 1);

        /// <summary>The bought upgrades, given to a Warden at the start of a run.</summary>
        public static void ApplyTo(PlayerStats stats, ChoiceTokens tokens)
        {
            foreach (Upgrade upgrade in Upgrades)
            {
                int level = LevelOf(upgrade.Id);
                if (level == 0) continue;
                if (upgrade.Stat.HasValue) { if (stats != null) stats.Apply(upgrade.Stat.Value, level); }
                else if (tokens != null) tokens.Add(upgrade.Rerolls * level, upgrade.Banishes * level);
            }
        }
    }
}
