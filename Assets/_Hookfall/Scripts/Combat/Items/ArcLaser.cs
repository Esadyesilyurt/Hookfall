// Hookfall - Arc Laser: a beam sweeps a full circle around the player; 20 damage, every 2 s, 8 m, +15% damage per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class ArcLaser : ItemBase
    {
        [SerializeField] private float range = 8f;
        [Tooltip("Enemies this far above or below the player are still hit by the sweep.")]
        [SerializeField] private float verticalReach = 4f;
        [SerializeField] private float sweepDuration = 0.4f;
        [SerializeField] private float damagePerLevel = 0.15f;
        [SerializeField] private Material beamMaterial;

        private readonly List<EnemyBase> inRange = new List<EnemyBase>();
        private readonly HashSet<EnemyBase> hitThisSweep = new HashSet<EnemyBase>();
        private LineRenderer beam;
        private float sweepTime = -1f;
        private float startAngle;

        public override float Damage => base.Damage * (1f + damagePerLevel * (Level - 1));

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Beam");
            go.layer = Hookfall.Core.Layers.NoOutline;
            go.transform.SetParent(transform, false);
            beam = go.AddComponent<LineRenderer>();
            beam.sharedMaterial = beamMaterial;
            beam.positionCount = 2;
            beam.startWidth = 0.35f;
            beam.endWidth = 0.1f;
            beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam.enabled = false;
        }

        protected override void Attack()
        {
            sweepTime = 0f;
            startAngle = Random.value * 360f;
            hitThisSweep.Clear();
        }

        protected override void OnActiveTick(float deltaTime)
        {
            if (sweepTime < 0f) return;
            float previous = sweepTime / sweepDuration * 360f;
            sweepTime += deltaTime;
            float current = Mathf.Min(360f, sweepTime / sweepDuration * 360f);

            Vector3 origin = transform.position;
            Vector3 dir = Quaternion.Euler(0f, startAngle + current, 0f) * Vector3.forward;
            beam.enabled = true;
            beam.SetPosition(0, origin);
            beam.SetPosition(1, origin + dir * range);

            // Hit every enemy whose bearing the beam passed this frame.
            EnemyQuery.InRadius(origin, range, inRange);
            foreach (EnemyBase enemy in inRange)
            {
                Vector3 offset = enemy.transform.position - origin;
                if (Mathf.Abs(offset.y) > verticalReach || hitThisSweep.Contains(enemy)) continue;
                float bearing = Mathf.Repeat(Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg - startAngle, 360f);
                if (bearing < previous || bearing > current) continue;
                hitThisSweep.Add(enemy);
                enemy.TakeDamage(Damage);
            }

            if (current >= 360f) EndSweep();
        }

        public override void SetActiveVisuals(bool active)
        {
            if (!active) EndSweep();
        }

        private void EndSweep()
        {
            sweepTime = -1f;
            if (beam != null) beam.enabled = false;
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Range {0} m", range.ToString("0"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("{0} → {1} damage", (BaseDamage * (1f + damagePerLevel * (Level - 1))).ToString("0"), (BaseDamage * (1f + damagePerLevel * Level)).ToString("0"));
    }
}
