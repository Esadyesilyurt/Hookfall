// Hookfall - Berserker Blood (Epic passive): more damage while the Warden is below half health. Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class BerserkerBlood : PassiveItem
    {
        [Tooltip("Extra damage below half health, per level.")]
        [SerializeField] private float damagePerLevel = 0.2f;

        protected override void Awake()
        {
            base.Awake();
            Apply();
        }

        protected override void OnLevelChanged() => Apply();

        private void OnDestroy()
        {
            if (Stats != null) Stats.LowHealthDamageBonus = 0f;
        }

        private void Apply()
        {
            if (Stats != null) Stats.LowHealthDamageBonus = damagePerLevel * Level;
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("+{0}% damage below half health", (damagePerLevel * Level * 100f).ToString("0"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("+{0}% → +{1}% damage below half health", (damagePerLevel * Level * 100f).ToString("0"), (damagePerLevel * (Level + 1) * 100f).ToString("0"));
    }
}
