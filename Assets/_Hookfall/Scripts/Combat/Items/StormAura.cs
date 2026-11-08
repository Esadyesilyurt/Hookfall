// Hookfall - Storm Aura: a damage field around the player; 4 damage every 0.25 s in 4 m, +0.5 m radius per level (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using System.Collections.Generic;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class StormAura : ItemBase
    {
        [SerializeField] private float baseRadius = 4f;
        [SerializeField] private float radiusPerLevel = 0.5f;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private Material auraMaterial;

        private readonly List<EnemyBase> inside = new List<EnemyBase>();
        private Transform field;

        private float Radius => baseRadius + radiusPerLevel * (Level - 1);

        protected override void Awake()
        {
            base.Awake();
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Storm Field";
            sphere.layer = Hookfall.Core.Layers.NoOutline;
            Destroy(sphere.GetComponent<Collider>());
            var r = sphere.GetComponent<Renderer>();
            r.sharedMaterial = auraMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            field = sphere.transform;
            field.SetParent(transform, false);
            field.gameObject.SetActive(false);
        }

        protected override void Attack()
        {
            EnemyQuery.InRadius(transform.position, Radius, inside);
            foreach (EnemyBase enemy in inside) enemy.TakeDamage(Damage);
        }

        protected override void OnActiveTick(float deltaTime)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.03f;
            field.localScale = Vector3.one * (Radius * 2f * pulse);
            field.Rotate(Vector3.up, 40f * deltaTime, Space.World);
        }

        public override void SetActiveVisuals(bool active)
        {
            if (field != null) field.gameObject.SetActive(active);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.F("Radius {0} m", Radius.ToString("0.0"));
        }

        public override string DescribeNextLevel() => Loc.F("Radius {0} m → {1} m", Radius.ToString("0.0"), (Radius + radiusPerLevel).ToString("0.0"));
    }
}
