// Hookfall - Ward Shell (Epic passive): a bubble shield that blocks one hit completely, then recharges on a cooldown
// that shortens with each level. Shown as a faint bubble around the Warden while charged. Always on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.Combat.Items
{
    public class WardShell : PassiveItem
    {
        [Tooltip("Recharge time at level 1, in seconds; each level takes rechargePerLevel off.")]
        [SerializeField] private float rechargeTime = 12f;
        [SerializeField] private float rechargePerLevel = 1.5f;
        [SerializeField] private Material bubbleMaterial;

        private float timer;
        private GameObject bubble;

        private float Recharge => Mathf.Max(3f, rechargeTime - rechargePerLevel * (Level - 1));

        protected override void Awake()
        {
            base.Awake();
            if (Stats != null) Stats.ShieldCharges = 1;
            if (bubbleMaterial == null) return;
            bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(bubble.GetComponent<Collider>());   // before parenting: on the Warden it would join its rigidbody
            bubble.name = "Ward Shell Bubble";
            bubble.layer = Layers.NoOutline;
            bubble.transform.SetParent(transform, false);
            bubble.transform.localScale = Vector3.one * 2.6f;
            var renderer = bubble.GetComponent<Renderer>();
            renderer.sharedMaterial = bubbleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            if (Stats != null) Stats.ShieldCharges = 0;
        }

        protected override void OnActiveTick(float deltaTime)
        {
            if (Stats == null) return;
            if (Stats.ShieldCharges > 0) timer = Recharge;
            else if ((timer -= deltaTime) <= 0f) Stats.ShieldCharges = 1;
            if (bubble != null) bubble.SetActive(Stats.ShieldCharges > 0);
        }

        public override IEnumerable<string> StatLines()
        {
            foreach (string text in base.StatLines()) yield return text;
            yield return Loc.T("Blocks 1 hit completely");
            yield return Loc.F("Recharges in {0} s", Recharge.ToString("0.#"));
        }

        public override string DescribeNextLevel() =>
            Loc.F("Recharges in {0} s → {1} s", Recharge.ToString("0.#"), Mathf.Max(3f, rechargeTime - rechargePerLevel * Level).ToString("0.#"));
    }
}
