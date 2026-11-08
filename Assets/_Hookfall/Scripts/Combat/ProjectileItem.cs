// Hookfall - base for items that fire pooled projectiles (Shard Burst, Homing Sparks).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Hookfall.Combat
{
    public abstract class ProjectileItem : ItemBase
    {
        [SerializeField] private Projectile projectilePrefab;

        private ObjectPool<Projectile> pool;
        private Transform container;
        private Action<Enemies.EnemyBase, float> strike;

        protected override void Awake()
        {
            base.Awake();
            strike = Strike;
            // Projectiles live outside the player so they do not move with it.
            container = new GameObject($"{name} Projectiles").transform;
            pool = new ObjectPool<Projectile>(
                () => Instantiate(projectilePrefab, container),
                p => p.gameObject.SetActive(true),
                p => p.gameObject.SetActive(false),
                p => Destroy(p.gameObject), false, 32, 400);
        }

        protected void Fire(Vector3 position, Vector3 velocity, float life, Enemies.EnemyBase homingTarget = null, float turnRate = 0f)
        {
            if (projectilePrefab == null) return;
            pool.Get().Launch(position, velocity, Damage, life, p => pool.Release(p), homingTarget, turnRate, strike);
        }

        private void OnDestroy()
        {
            if (container != null) Destroy(container.gameObject);
        }
    }
}
