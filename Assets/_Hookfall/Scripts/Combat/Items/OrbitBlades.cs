// Hookfall - starting item: blades circle the player and cut what they touch (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// changed on 2026-11-07 to use the enemy list instead of physics.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class OrbitBlades : ItemBase
    {
        [SerializeField] private float orbitRadius = 3f;
        [SerializeField] private float orbitSpeed = 240f;   // degrees per second
        [SerializeField] private float bladeHitRadius = 0.9f;
        [SerializeField] private Material bladeMaterial;

        private readonly List<Transform> blades = new List<Transform>();
        private readonly List<EnemyBase> hits = new List<EnemyBase>();
        private Transform pivot;

        // Level 1 has 2 blades; each level adds one (GDD: +1 blade per level).
        private int BladeCount => Level + 1;

        protected override void Awake()
        {
            base.Awake();
            pivot = new GameObject("BladePivot").transform;
            pivot.SetParent(transform, false);
            RebuildBlades();
        }

        protected override void OnActiveTick(float deltaTime)
        {
            pivot.Rotate(Vector3.up, orbitSpeed * deltaTime, Space.World);
        }

        protected override void Attack()
        {
            // The blades sweep the whole ring several times per second, so every enemy inside the ring
            // counts as touched. Checking only the blades' exact positions twice a second missed most hits
            // (found in the first Unity test, see Docs/PromptLog.md #14).
            // Each attack hits every enemy in the ring once, so an enemy is hit at most every cooldown (0.5 s).
            EnemyQuery.InRadius(transform.position, orbitRadius + bladeHitRadius, hits);
            foreach (EnemyBase enemy in hits) enemy.TakeDamage(Damage);
        }

        public override void SetActiveVisuals(bool active)
        {
            if (pivot != null) pivot.gameObject.SetActive(active);
        }

        protected override void OnLevelChanged() => RebuildBlades();

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("{0} blades", BladeCount);
            yield return Loc.F("Radius {0} m", orbitRadius.ToString("0"));
        }

        public override string DescribeNextLevel() => Loc.F("{0} → {1} blades", BladeCount, BladeCount + 1);

        private void RebuildBlades()
        {
            foreach (Transform blade in blades) Destroy(blade.gameObject);
            blades.Clear();

            for (int i = 0; i < BladeCount; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade {i + 1}";
                blade.layer = Hookfall.Core.Layers.NoOutline;
                Destroy(blade.GetComponent<Collider>());   // hits come from OverlapSphere, not physics
                if (bladeMaterial != null) blade.GetComponent<Renderer>().sharedMaterial = bladeMaterial;

                float angle = 360f / BladeCount * i;
                Transform t = blade.transform;
                t.SetParent(pivot, false);
                t.localPosition = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * orbitRadius;
                t.localRotation = Quaternion.Euler(0f, angle, 0f);
                t.localScale = new Vector3(0.15f, 0.1f, 1.2f);
                blades.Add(t);
            }
        }
    }
}
