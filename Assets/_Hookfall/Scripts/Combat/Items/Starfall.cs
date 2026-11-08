// Hookfall - Starfall (Epic, added 2026-11-08): calls stars down on random enemies up to 35 m away; each hits its
// target and bursts over everything close by when it lands. One more star per level.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections;
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class Starfall : ProjectileItem
    {
        [SerializeField] private float range = 35f;
        [SerializeField] private int baseStars = 2;
        [SerializeField] private float splashRadius = 4f;
        [Tooltip("Share of the damage dealt around the landing point.")]
        [SerializeField] private float splashShare = 0.6f;
        [SerializeField] private float fallHeight = 18f;
        [SerializeField] private float fallSpeed = 45f;

        private readonly List<EnemyBase> inRange = new List<EnemyBase>();
        private readonly List<EnemyBase> near = new List<EnemyBase>();

        private int Stars(int level) => baseStars + level - 1 + ExtraProjectiles;

        protected override void Attack()
        {
            EnemyQuery.InRadius(transform.position, range, inRange);
            for (int i = 0; i < Stars(Level) && inRange.Count > 0; i++)
            {
                int pick = Random.Range(0, inRange.Count);
                EnemyBase target = inRange[pick];
                inRange.RemoveAt(pick);
                Vector3 point = target.transform.position;
                float fallTime = fallHeight / fallSpeed;
                Fire(point + Vector3.up * fallHeight, Vector3.down * fallSpeed, fallTime + 0.1f, target, 360f);
                StartCoroutine(Splash(point, fallTime));
            }
        }

        private IEnumerator Splash(Vector3 point, float delay)
        {
            yield return new WaitForSeconds(delay);
            EnemyQuery.InRadius(point, splashRadius, near);
            float damage = Damage * splashShare;
            foreach (EnemyBase enemy in near) Strike(enemy, damage);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("{0} stars, range {1} m", Stars(Level), range.ToString("0"));
            yield return Loc.F("Burst radius {0} m", splashRadius.ToString("0"));
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} stars", Stars(Level), Stars(Level + 1));
    }
}
