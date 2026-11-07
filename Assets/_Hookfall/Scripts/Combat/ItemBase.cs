// Hookfall - base class for the six auto-attack items: cooldown, level, damage, lore (GDD 4.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (lore, level text, chest levels). Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public abstract class ItemBase : MonoBehaviour
    {
        public const int MaxLevel = 5;

        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string lore = "";
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField, Range(1, MaxLevel)] private int level = 1;

        private float cooldownTimer;

        protected PlayerStats Stats { get; private set; }

        public string DisplayName => displayName;
        public string Lore => lore;
        public int Level => level;
        public bool IsMaxed => level >= MaxLevel;
        public virtual float Damage => baseDamage * (Stats != null ? Stats.DamageMultiplier : 1f);
        protected float BaseDamage => baseDamage;
        protected float EffectiveCooldown => cooldown / (Stats != null ? Mathf.Max(0.1f, Stats.AttackSpeedMultiplier) : 1f);

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

        public void LevelUp() => SetLevel(level + 1);

        public void SetLevel(int newLevel)
        {
            newLevel = Mathf.Clamp(newLevel, 1, MaxLevel);
            if (newLevel == level) return;
            level = newLevel;
            OnLevelChanged();
        }

        /// <summary>Text for the level-up card, e.g. "2 → 3 blades".</summary>
        public abstract string DescribeNextLevel();

        /// <summary>Show or hide the item's visuals when combat turns on or off.</summary>
        public virtual void SetActiveVisuals(bool active) { }

        protected virtual void OnActiveTick(float deltaTime) { }
        protected virtual void OnLevelChanged() { }
        protected abstract void Attack();
    }
}
