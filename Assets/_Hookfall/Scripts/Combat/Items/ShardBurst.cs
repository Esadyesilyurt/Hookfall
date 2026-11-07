// Hookfall - Shard Burst: fires crystal shards in all directions; 12 damage, every 1.5 s, 30 m/s, +2 shards per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
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

        private int ShardCount => baseShards + shardsPerLevel * (Level - 1);

        protected override void Attack()
        {
            int count = ShardCount;
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

        public override string DescribeNextLevel() => $"{ShardCount} → {ShardCount + shardsPerLevel} shards";
    }
}
