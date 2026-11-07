// Hookfall - finds enemies for the items by walking the live enemy list; cheaper than physics queries for 300 enemies.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat
{
    public static class EnemyQuery
    {
        /// <summary>Fills results with every enemy whose body overlaps the sphere.</summary>
        public static void InRadius(Vector3 centre, float radius, List<EnemyBase> results)
        {
            results.Clear();
            List<EnemyBase> active = EnemyBase.Active;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBase e = active[i];
                float r = radius + e.HitRadius;
                if ((e.transform.position - centre).sqrMagnitude <= r * r) results.Add(e);
            }
        }

        /// <summary>The first enemy whose body the point is inside, or null.</summary>
        public static EnemyBase At(Vector3 point, float extraRadius)
        {
            List<EnemyBase> active = EnemyBase.Active;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBase e = active[i];
                float r = e.HitRadius + extraRadius;
                if ((e.transform.position - point).sqrMagnitude <= r * r) return e;
            }
            return null;
        }

        /// <summary>The nearest enemy within range that is not in the skip set, or null.</summary>
        public static EnemyBase Nearest(Vector3 from, float maxRange, ICollection<EnemyBase> skip = null)
        {
            EnemyBase best = null;
            float bestSqr = maxRange * maxRange;
            List<EnemyBase> active = EnemyBase.Active;
            for (int i = 0; i < active.Count; i++)
            {
                EnemyBase e = active[i];
                if (skip != null && skip.Contains(e)) continue;
                float d = (e.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
            return best;
        }
    }
}
