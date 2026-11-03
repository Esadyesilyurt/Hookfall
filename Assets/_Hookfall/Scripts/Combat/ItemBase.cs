// Hookfall - base class for the six auto-attack items: cooldown, level, damage (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public abstract class ItemBase : MonoBehaviour
    {
        public const int MaxLevel = 5;

        [SerializeField] private string displayName = "Item";
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField, Range(1, MaxLevel)] private int level = 1;

        private float cooldownTimer;

        protected PlayerStats Stats { get; private set; }

        public string DisplayName => displayName;
        public int Level => level;
        public float Damage => baseDamage * (Stats != null ? Stats.DamageMultiplier : 1f);
        private float EffectiveCooldown => cooldown / (Stats != null ? Mathf.Max(0.1f, Stats.AttackSpeedMultiplier) : 1f);

        protected virtual void Awake() => Stats = GetComponentInParent<PlayerStats>();

        /// <summary>Called every frame by CombatSystem, only while attacks are active (player airborne).</summary>
        public void Tick(float deltaTime)
        {
            OnActiveTick(deltaTime);
            cooldownTimer -= deltaTime;
            if (cooldownTimer > 0f) return;
            cooldownTimer = EffectiveCooldown;
            Attack();
        }

        public void LevelUp()
        {
            if (level >= MaxLevel) return;
            level++;
            OnLevelChanged();
        }

        /// <summary>Show or hide the item's visuals when combat turns on or off.</summary>
        public virtual void SetActiveVisuals(bool active) { }

        protected virtual void OnActiveTick(float deltaTime) { }
        protected virtual void OnLevelChanged() { }
        protected abstract void Attack();
    }
}
