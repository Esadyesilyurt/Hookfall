// Hookfall - Thorn Mail (Rare passive): when the Warden is hit in the air, enemies close by take damage back.
// Like every damaging item, it does nothing on islands.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class ThornMail : PassiveItem
    {
        [SerializeField] private float radius = 5f;

        private readonly List<EnemyBase> near = new List<EnemyBase>();

        // BaseDamage is per level.
        public override float Damage => base.Damage * Level;

        private void OnEnable()
        {
            if (Stats != null) Stats.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (Stats != null) Stats.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount)
        {
            if (Player == null || !Player.IsAirborne) return;
            EnemyQuery.InRadius(transform.position, radius, near);
            foreach (EnemyBase enemy in near) enemy.TakeDamage(Damage);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Radius {0} m", radius.ToString("0"));
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} damage back", (BaseDamage * Level).ToString("0"), (BaseDamage * (Level + 1)).ToString("0"));
    }
}
