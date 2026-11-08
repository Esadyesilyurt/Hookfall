// Hookfall - Hunter's Eye (Rare passive): a chance for any hit to deal double damage. Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class HuntersEye : PassiveItem
    {
        [Tooltip("Critical hit chance per level (crits deal double damage).")]
        [SerializeField] private float critChancePerLevel = 0.06f;

        protected override void Awake()
        {
            base.Awake();
            Apply();
        }

        protected override void OnLevelChanged() => Apply();

        private void OnDestroy()
        {
            if (Stats != null) Stats.CritChance = 0f;
        }

        private void Apply()
        {
            if (Stats != null) Stats.CritChance = critChancePerLevel * Level;
        }

        public override string DescribeNextLevel() =>
            $"Critical hits {critChancePerLevel * Level * 100f:0}% → {critChancePerLevel * (Level + 1) * 100f:0}%";
    }
}
