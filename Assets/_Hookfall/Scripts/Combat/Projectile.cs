// Hookfall - a pooled player projectile (Shard Burst shards, Homing Sparks): flies, optionally homes, hits the first enemy it touches.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 0.3f;

        private Action<Projectile> release;
        private Vector3 velocity;
        private float damage;
        private float lifetime;
        private float age;
        private EnemyBase target;
        private float turnRate;
        private TrailRenderer trail;

        private void Awake() => trail = GetComponentInChildren<TrailRenderer>();

        public void Launch(Vector3 position, Vector3 launchVelocity, float hitDamage, float life, Action<Projectile> onRelease,
            EnemyBase homingTarget = null, float homingTurnRate = 0f)
        {
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(launchVelocity));
            velocity = launchVelocity;
            damage = hitDamage;
            lifetime = life;
            age = 0f;
            release = onRelease;
            target = homingTarget;
            turnRate = homingTurnRate;
            if (trail != null) trail.Clear();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age > lifetime) { release(this); return; }

            if (target != null)
            {
                if (target.IsDead || !target.isActiveAndEnabled) target = null;
                else
                {
                    Vector3 desired = (target.transform.position - transform.position).normalized * velocity.magnitude;
                    velocity = Vector3.RotateTowards(velocity, desired, turnRate * Mathf.Deg2Rad * dt, 0f);
                }
            }

            transform.position += velocity * dt;
            transform.rotation = Quaternion.LookRotation(velocity);

            EnemyBase hit = EnemyQuery.At(transform.position, hitRadius);
            if (hit == null) return;
            hit.TakeDamage(damage);
            release(this);
        }
    }
}
