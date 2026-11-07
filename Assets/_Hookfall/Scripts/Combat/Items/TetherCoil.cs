// Hookfall - Tether Coil: the Zip item (added 2026-11-07). Zipping through enemies cuts them; each level shortens the
// Zip cooldown, levels 3 and 5 add a charge, and from level 4 the Warden lands with a shockwave.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Enemies;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class TetherCoil : ItemBase
    {
        [Tooltip("Zip cooldown multiplier per level (index 0 = level 1).")]
        [SerializeField] private float[] cooldownMultiplier = { 0.9f, 0.85f, 0.8f, 0.75f, 0.7f };
        [Tooltip("Extra Zip charges per level.")]
        [SerializeField] private int[] extraCharges = { 0, 0, 1, 1, 2 };
        [SerializeField] private float damagePerLevel = 0.25f;
        [SerializeField] private float contactRadius = 2f;
        [SerializeField] private float shockwaveFromLevel = 4;
        [SerializeField] private float shockwaveRadius = 6f;
        [SerializeField] private float shockwaveDamage = 30f;

        private readonly HashSet<EnemyBase> hitThisZip = new HashSet<EnemyBase>();
        private readonly List<EnemyBase> touching = new List<EnemyBase>();
        private GrappleHook hook;

        public override float Damage => base.Damage * (1f + damagePerLevel * (Level - 1));

        protected override void Awake()
        {
            base.Awake();
            hook = GetComponentInParent<GrappleHook>();
            if (hook != null)
            {
                hook.ZipStarted += OnZipStarted;
                hook.ZipEnded += OnZipEnded;
            }
            ApplyBonus();
        }

        private void OnDestroy()
        {
            if (hook != null)
            {
                hook.ZipStarted -= OnZipStarted;
                hook.ZipEnded -= OnZipEnded;
            }
            if (Stats != null) Stats.SetZipItemBonus(1f, 0);
        }

        protected override void OnLevelChanged() => ApplyBonus();

        private void ApplyBonus()
        {
            if (Stats == null) return;
            int i = Mathf.Clamp(Level - 1, 0, cooldownMultiplier.Length - 1);
            Stats.SetZipItemBonus(cooldownMultiplier[i], extraCharges[Mathf.Clamp(Level - 1, 0, extraCharges.Length - 1)]);
        }

        protected override void Attack() { }   // works through the Zip instead of a cooldown

        // Runs only while airborne, which a Zip always is.
        protected override void OnActiveTick(float deltaTime)
        {
            if (hook == null || !hook.IsZipping) return;
            EnemyQuery.InRadius(transform.position, contactRadius, touching);
            foreach (EnemyBase enemy in touching)
                if (hitThisZip.Add(enemy)) enemy.TakeDamage(Damage);
        }

        private void OnZipStarted() => hitThisZip.Clear();

        private void OnZipEnded()
        {
            if (Level < shockwaveFromLevel) return;
            EnemyQuery.InRadius(transform.position, shockwaveRadius, touching);
            float damage = shockwaveDamage * (Stats != null ? Stats.DamageMultiplier : 1f);
            foreach (EnemyBase enemy in touching) enemy.TakeDamage(damage);
            ShockwaveFired?.Invoke(transform.position, shockwaveRadius);
        }

        /// <summary>For effects: position and radius of a landing shockwave.</summary>
        public static event System.Action<Vector3, float> ShockwaveFired;

        public override string DescribeNextLevel()
        {
            int next = Mathf.Min(Level, cooldownMultiplier.Length - 1);
            string text = $"Zip cooldown ×{cooldownMultiplier[next]:0.00}";
            if (extraCharges[next] > extraCharges[Level - 1]) text += ", +1 charge";
            if (Level + 1 == (int)shockwaveFromLevel) text += ", landing shockwave";
            return text;
        }
    }
}
