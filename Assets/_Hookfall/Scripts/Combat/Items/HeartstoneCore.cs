// Hookfall - Heartstone Core (Legendary passive): while the Warden flies, a pulse of the Heartstone's light every few
// seconds damages every enemy close by and heals the Warden. Skipped on islands (the airborne rule).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class HeartstoneCore : PassiveItem
    {
        [SerializeField] private float radius = 10f;
        [Tooltip("Share of max health healed per pulse.")]
        [SerializeField] private float healShare = 0.04f;
        [Tooltip("Seconds taken off the pulse cooldown per level.")]
        [SerializeField] private float cooldownPerLevel = 0.75f;
        [Tooltip("Extra damage per level above 1.")]
        [SerializeField] private float damagePerLevel = 0.5f;

        private readonly List<EnemyBase> near = new List<EnemyBase>();

        /// <summary>For effects: position and radius of a pulse.</summary>
        public static event System.Action<Vector3, float> Pulsed;

        public override float Damage => base.Damage * (1f + damagePerLevel * (Level - 1));
        protected override float EffectiveCooldown => PulseCooldown(Level);

        private float PulseCooldown(int level) => Mathf.Max(2f, Cooldown - cooldownPerLevel * (level - 1));

        protected override void Attack()
        {
            if (Player == null || !Player.IsAirborne) return;
            EnemyQuery.InRadius(transform.position, radius, near);
            foreach (EnemyBase enemy in near) enemy.TakeDamage(Damage);
            if (Stats != null) Stats.Heal(Stats.MaxHealth * healShare);
            Pulsed?.Invoke(transform.position, radius);
        }

        public override string DescribeNextLevel() =>
            $"Pulse every {PulseCooldown(Level):0.#} s → {PulseCooldown(Level + 1):0.#} s, {BaseDamage * (1f + damagePerLevel * Level):0} damage";
    }
}
