// Hookfall - Shard Burst: fires crystal shards in all directions; 12 damage, every 1.5 s, 30 m/s, +2 shards per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class ShardBurst : ProjectileItem
    {
        [SerializeField] private int baseShards = 8;
        [SerializeField] private int shardsPerLevel = 2;
        [SerializeField] private float shardSpeed = 30f;
        [SerializeField] private float shardLifetime = 1f;
        [Tooltip("0 = shards spread over a full sphere, 1 = a flat ring around the player.")]
        [SerializeField, Range(0f, 1f)] private float flatten = 0.6f;
        [Tooltip("Below 360: shards fan out in a cone of this angle ahead of the Warden instead (Frost Wyrm Breath).")]
        [SerializeField] private float coneAngle = 360f;

        private int ShardCount => baseShards + shardsPerLevel * (Level - 1) + ExtraProjectiles * (coneAngle < 360f ? 1 : 2);

        protected override void Attack()
        {
            int count = ShardCount;
            if (coneAngle < 360f) { Breathe(count); return; }
            float spin = Random.value * 360f;
            for (int i = 0; i < count; i++)
            {
                // Fibonacci sphere: an even spread in every direction, squashed towards the horizon.
                float y = 1f - (i + 0.5f) / count * 2f;
                float r = Mathf.Sqrt(1f - y * y);
                float theta = i * 2.39996f + spin * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(theta) * r, y * (1f - flatten), Mathf.Sin(theta) * r).normalized;
                Fire(transform.position, dir * shardSpeed, shardLifetime);
            }
        }

        /// <summary>A fan of shards at the nearest enemy in reach, led a little. It used to follow the flight, but the
        /// horde chases from behind a flying Warden, so the breath missed (the Frost Seer's weak start, 2026-11-08).</summary>
        private void Breathe(int count)
        {
            float reach = shardSpeed * shardLifetime;
            Enemies.EnemyBase target = EnemyQuery.Nearest(transform.position, reach * 1.2f);
            if (target == null) return;
            float time = Vector3.Distance(transform.position, target.transform.position) / Mathf.Max(1f, shardSpeed);
            Vector3 ahead = target.transform.position + target.Velocity * time - transform.position;
            if (ahead.sqrMagnitude < 0.01f) ahead = transform.forward;
            Quaternion facing = Quaternion.LookRotation(ahead.normalized);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (count - 1f);
                Vector3 dir = facing * Quaternion.Euler(Random.Range(-6f, 6f), Mathf.Lerp(-coneAngle, coneAngle, t) * 0.5f, 0f) * Vector3.forward;
                Fire(transform.position, dir * shardSpeed, shardLifetime);
            }
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("{0} shards", ShardCount);
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} shards", ShardCount, ShardCount + shardsPerLevel);
    }
}
