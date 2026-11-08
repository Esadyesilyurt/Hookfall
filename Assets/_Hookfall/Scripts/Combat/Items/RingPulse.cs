// Hookfall - a ring pulse (2026-11-08): every few seconds, everything within a radius around the Warden takes a hit,
// shown as a flash of light on the ground plane. Used by the Crimson Reaper's Sickle (Blood) and the Thundering Bell
// (Storm): the school decides the status the pulse leaves behind.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.Combat.Items
{
    public class RingPulse : ItemBase
    {
        [SerializeField] private float radius = 6f;
        [SerializeField] private float radiusPerLevel = 0.5f;
        [SerializeField] private float damagePerLevel = 0.2f;
        [SerializeField] private Material ringMaterial;
        [SerializeField] private Color ringColor = Color.white;
        [SerializeField] private float flashTime = 0.25f;

        private readonly List<EnemyBase> inside = new List<EnemyBase>();
        private LineRenderer ring;
        private float flash;

        private float Radius(int level) => radius + radiusPerLevel * (level - 1);
        public override float Damage => base.Damage * (1f + damagePerLevel * (Level - 1));

        protected override void Awake()
        {
            base.Awake();
            var go = new GameObject("Pulse Ring") { layer = Layers.NoOutline };
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            if (ringMaterial != null)
            {
                ring.material = new Material(ringMaterial);
                ring.material.SetTexture("_BaseMap", Texture2D.whiteTexture);   // a solid ring: the particle texture is a soft dot
            }
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.enabled = false;
        }

        private void Update()
        {
            if (!ring.enabled) return;
            flash -= Time.deltaTime;
            Color c = ringColor;
            c.a = Mathf.Clamp01(flash / flashTime);
            ring.startColor = ring.endColor = c;
            if (flash <= 0f) ring.enabled = false;
        }

        protected override void Attack()
        {
            float r = Radius(Level);
            EnemyQuery.InRadius(transform.position, r, inside);
            foreach (EnemyBase enemy in inside) Strike(enemy, Damage);
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            ring.widthMultiplier = 0.35f;
            ring.enabled = true;
            flash = flashTime;
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Radius {0} m", Radius(Level).ToString("0.0"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("+{0}% damage", (damagePerLevel * 100f).ToString("0")) + "\n" + Loc.F("Radius {0} m → {1} m", Radius(Level).ToString("0.0"), Radius(Level + 1).ToString("0.0"));
    }
}
