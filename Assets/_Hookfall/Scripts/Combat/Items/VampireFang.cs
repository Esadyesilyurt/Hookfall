// Hookfall - Vampire Fang (Rare passive): heals the Warden for a share of all damage dealt. Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class VampireFang : PassiveItem
    {
        [Tooltip("Share of damage dealt healed back, per level.")]
        [SerializeField] private float lifestealPerLevel = 0.02f;

        private float Lifesteal => lifestealPerLevel * Level;

        private void OnEnable() => EnemyDirector.Hit += OnHit;
        private void OnDisable() => EnemyDirector.Hit -= OnHit;

        private void OnHit(EnemyBase enemy, float damage)
        {
            if (Stats != null) Stats.Heal(damage * Lifesteal);
        }

        public override string DescribeNextLevel() => $"Lifesteal {Lifesteal * 100f:0}% → {(Lifesteal + lifestealPerLevel) * 100f:0}%";
    }
}
