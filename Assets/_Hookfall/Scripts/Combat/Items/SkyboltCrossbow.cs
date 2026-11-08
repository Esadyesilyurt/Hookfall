// Hookfall - Skybolt Crossbow (Common, added 2026-11-08; also the Salamander Familiar's fireballs): a long-range attack for safe play: shots at the nearest
// enemies up to 40 m away. Damage grows each level; levels 3 and 5 add a bolt.
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

        private readonly HashSet<EnemyBase> targeted = new HashSet<EnemyBase>();

        private int Bolts(int level) => 1 + (level >= 3 ? 1 : 0) + (level >= 5 ? 1 : 0) + ExtraProjectiles;
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
                Fire(from, (target.transform.position - from).normalized * boltSpeed, range / boltSpeed + 0.1f);
            }
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string line in base.StatLines()) yield return line;
            yield return Loc.F("Range {0} m", range.ToString("0"));
            if (Bolts(Level) > 1) yield return Loc.F("{0} bolts", Bolts(Level));
        }

        public override string DescribeNextLevel() =>
            Loc.F("+{0}% damage", (damagePerLevel * 100f).ToString("0")) + (Bolts(Level + 1) > Bolts(Level) ? "\n" + Loc.T("+1 bolt") : "");
    }
}
