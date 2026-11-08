// Hookfall - Homing Sparks: missiles seek the nearest enemies within 25 m; 15 damage, every 1.2 s, +1 missile per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class HomingSparks : ProjectileItem
    {
        [SerializeField] private int baseMissiles = 2;
        [SerializeField] private float range = 25f;
        [SerializeField] private float speed = 22f;
        [SerializeField] private float turnRate = 540f;
        [SerializeField] private float lifetime = 2f;

        private readonly HashSet<EnemyBase> targeted = new HashSet<EnemyBase>();

        private int MissileCount => baseMissiles + Level - 1 + ExtraProjectiles;

        protected override void Attack()
        {
            targeted.Clear();
            for (int i = 0; i < MissileCount; i++)
            {
                // Each missile takes a different target while there are enough enemies in range.
                EnemyBase target = EnemyQuery.Nearest(transform.position, range, targeted) ?? EnemyQuery.Nearest(transform.position, range);
                if (target == null) return;
                targeted.Add(target);
                Vector3 launch = (Vector3.up + Random.insideUnitSphere * 0.8f).normalized;
                Fire(transform.position + Vector3.up * 0.5f, launch * speed, lifetime, target, turnRate);
            }
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("{0} missiles", MissileCount);
            yield return Loc.F("Range {0} m", range.ToString("0"));
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} missiles", MissileCount, MissileCount + 1);
    }
}
