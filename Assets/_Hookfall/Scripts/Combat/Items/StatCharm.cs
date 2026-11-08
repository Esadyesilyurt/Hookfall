// Hookfall - a stat charm: a passive that gives the same upgrades as level-up stat cards, once per item level
// (Heart Locket, Iron Hide, Gale Feather, Whetstone and the rest; see ItemPrefabs). Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class StatCharm : PassiveItem
    {
        [Tooltip("Stat upgrades applied once for every item level, exactly like a level-up stat card.")]
        [SerializeField] private StatType[] picksPerLevel = { StatType.MaxHealth };

        private int applied;

        protected override void Awake()
        {
            base.Awake();
            CatchUp();
        }

        protected override void OnLevelChanged() => CatchUp();

        private void CatchUp()
        {
            if (Stats == null) return;
            for (; applied < Level; applied++)
                foreach (StatType stat in picksPerLevel) Stats.Apply(stat);
        }

        public override string DescribeNextLevel()
        {
            if (Stats == null) return "";
            var parts = new string[picksPerLevel.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = $"{PlayerStats.NameOf(picksPerLevel[i])} {Stats.Describe(picksPerLevel[i])}";
            return string.Join("\n", parts);
        }
    }
}
