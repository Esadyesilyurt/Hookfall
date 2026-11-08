// Hookfall - Phoenix Feather (Legendary passive): when health reaches zero, the Warden comes back with part of their
// health instead of dying; the feather then recharges on a long cooldown. A fall into the abyss still ends the run.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class PhoenixFeather : PassiveItem
    {
        [Tooltip("Recharge time after a revive at level 1, in seconds; each level takes rechargePerLevel off.")]
        [SerializeField] private float rechargeTime = 150f;
        [SerializeField] private float rechargePerLevel = 20f;
        [Tooltip("Share of max health after a revive at level 1, plus revivePerLevel for each level above.")]
        [SerializeField] private float reviveHealth = 0.4f;
        [SerializeField] private float revivePerLevel = 0.1f;

        private float timer;

        private float Recharge => Mathf.Max(30f, rechargeTime - rechargePerLevel * (Level - 1));
        private float ReviveShare(int level) => Mathf.Min(1f, reviveHealth + revivePerLevel * (level - 1));

        protected override void Awake()
        {
            base.Awake();
            if (Stats == null) return;
            Stats.Revives = 1;
            Stats.ReviveHealth = ReviveShare(Level);
        }

        protected override void OnLevelChanged()
        {
            if (Stats != null) Stats.ReviveHealth = ReviveShare(Level);
        }

        private void OnDestroy()
        {
            if (Stats != null) Stats.Revives = 0;
        }

        protected override void OnActiveTick(float deltaTime)
        {
            if (Stats == null) return;
            if (Stats.Revives > 0) timer = Recharge;
            else if ((timer -= deltaTime) <= 0f) Stats.Revives = 1;
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Revive with {0}% health", (ReviveShare(Level) * 100f).ToString("0"));
            yield return Loc.F("Recharges in {0} s", Recharge.ToString("0"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("Revive with {0}% → {1}% health", (ReviveShare(Level) * 100f).ToString("0"), (ReviveShare(Level + 1) * 100f).ToString("0"));
    }
}
