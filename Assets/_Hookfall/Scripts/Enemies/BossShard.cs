// Hookfall - one crystal shard from the boss's volley: flies at the Warden, turning towards them a little, shatters on
// islands (cover!) and on a hit, and fades out after a few seconds.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
using Hookfall.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.Enemies
{
    public class BossShard : MonoBehaviour
    {
        private const float HitRadius = 1.4f;

        private PlayerController player;
        private PlayerStats stats;
        private Vector3 velocity;
        private float damage, turnRate, life, age;
        private Action<Vector3> shattered;

        /// <summary>A new shard: a glowing crystal with a short trail.</summary>
        public static BossShard Create(Transform parent, Material material, Material trailMaterial, Color color)
        {
            var go = new GameObject("Boss Shard") { layer = Layers.NoOutline };
            go.transform.SetParent(parent, false);
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(crystal.GetComponent<Collider>());
            crystal.transform.SetParent(go.transform, false);
            crystal.transform.localScale = new Vector3(0.55f, 0.55f, 1.5f);
            crystal.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var renderer = crystal.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.startWidth = 0.5f;
            trail.endWidth = 0f;
            if (trailMaterial != null) trail.sharedMaterial = trailMaterial;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            return go.AddComponent<BossShard>();
        }

        public void Launch(PlayerController target, Vector3 position, Vector3 launchVelocity, float hitDamage, float homing, float lifetime,
            Action<Vector3> onShatter)
        {
            player = target;
            stats = target.GetComponent<PlayerStats>();
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(launchVelocity));
            velocity = launchVelocity;
            damage = hitDamage;
            turnRate = homing;
            life = lifetime;
            age = 0f;
            shattered = onShatter;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || player == null) return;
            age += dt;
            if (age > life) { Destroy(gameObject); return; }

            Vector3 target = player.Body.position;
            if (turnRate > 0f)
                velocity = Vector3.RotateTowards(velocity, (target - transform.position).normalized * velocity.magnitude, turnRate * Mathf.Deg2Rad * dt, 0f);
            Vector3 from = transform.position, to = from + velocity * dt;
            if (Physics.Linecast(from, to, out RaycastHit hit, 1 << Layers.World, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Boss>() == null)
            {
                Break(hit.point);   // islands are cover
                return;
            }
            transform.SetPositionAndRotation(to, Quaternion.LookRotation(velocity));
            if ((target - to).sqrMagnitude > HitRadius * HitRadius) return;
            if (stats != null) stats.TakeDamage(damage);
            Break(to);
        }

        private void Break(Vector3 at)
        {
            shattered?.Invoke(at);
            Destroy(gameObject);
        }
    }
}
