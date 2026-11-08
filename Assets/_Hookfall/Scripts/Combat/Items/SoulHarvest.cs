// Hookfall - Soul Harvest (Rare passive): every kill heals the Warden a little. Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class SoulHarvest : PassiveItem
    {
        [Tooltip("Health healed per kill, per level.")]
        [SerializeField] private float healPerKillPerLevel = 0.6f;

        private float HealPerKill => healPerKillPerLevel * Level;

        private void OnEnable() => EnemyDirector.Died += OnDied;
        private void OnDisable() => EnemyDirector.Died -= OnDied;

        private void OnDied(EnemyBase enemy, bool killed)
        {
            if (killed && Stats != null) Stats.Heal(HealPerKill);
        }

        public override string DescribeNextLevel() => $"{HealPerKill:0.0} → {HealPerKill + healPerKillPerLevel:0.0} health per kill";
    }
}
