// Hookfall - Skybolt Crossbow (Common, added 2026-11-08; also the Salamander Familiar's fireballs): a long-range attack for safe play: shots at the nearest
// enemies up to 40 m away. Damage grows each level; levels 3 and 5 add a bolt. Bolts can home in and splash (the
// salamander's fireballs do both since 2026-11-08: they used to miss fast bats and kill one at a time).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class SkyboltCrossbow : ProjectileItem
    {
        [SerializeField] private float range = 40f;
        [SerializeField] private float boltSpeed = 70f;
        [SerializeField] private float damagePerLevel = 0.15f;
        [Tooltip("Bolts at level 1; levels 3 and 5 add one each.")]
        [SerializeField] private int baseBolts = 1;
        [Tooltip("Degrees per second a bolt turns towards its target (0 = straight).")]
        [SerializeField] private float homingTurnRate;
        [Tooltip("Enemies this close to the one hit take splashShare of the damage (0 = no splash).")]
        [SerializeField] private float splashRadius;
        [SerializeField] private float splashShare = 0.5f;

        private readonly List<EnemyBase> splashed = new List<EnemyBase>();

        private readonly HashSet<EnemyBase> targeted = new HashSet<EnemyBase>();

        private int Bolts(int level) => baseBolts + (level >= 3 ? 1 : 0) + (level >= 5 ? 1 : 0) + ExtraProjectiles;
        public override float Damage => base.Damage * (1f + damagePerLevel * (Level - 1));

        protected override void Attack()
        {
            targeted.Clear();
            for (int i = 0; i < Bolts(Level); i++)
            {
                EnemyBase target = EnemyQuery.Nearest(transform.position, range, targeted) ?? EnemyQuery.Nearest(transform.position, range);
                if (target == null) return;
                targeted.Add(target);
                Vector3 from = transform.position + Vector3.up * 0.6f;
                Fire(from, (target.transform.position - from).normalized * boltSpeed, range / boltSpeed + 0.4f, homingTurnRate > 0f ? target : null, homingTurnRate);
            }
        }

        protected override void OnProjectileHit(EnemyBase enemy, float damage)
        {
            Vector3 at = enemy.transform.position;
            Strike(enemy, damage);
            if (splashRadius <= 0f) return;
            EnemyQuery.InRadius(at, splashRadius, splashed);
            foreach (EnemyBase other in splashed)
                if (other != enemy && !other.IsDead) Strike(other, damage * splashShare);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string line in base.StatLines()) yield return line;
            yield return Loc.F("Range {0} m", range.ToString("0"));
            if (splashRadius > 0f) yield return Loc.F("Splash {0} m", splashRadius.ToString("0.#"));
            if (Bolts(Level) > 1) yield return Loc.F("{0} bolts", Bolts(Level));
        }

        public override string DescribeNextLevel() =>
            Loc.F("+{0}% damage", (damagePerLevel * 100f).ToString("0")) + (Bolts(Level + 1) > Bolts(Level) ? "\n" + Loc.T("+1 bolt") : "");
    }
}
