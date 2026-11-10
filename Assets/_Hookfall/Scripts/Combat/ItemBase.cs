// Hookfall - base class for every chest item: cooldown, level, damage, lore, rarity (GDD 4.5). Attack items act only
// in flight; passive items (PassiveItem) act all the time and use their own six slots.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (lore, level text, chest levels) and 2026-11-08 (rarity, passives). Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public abstract class ItemBase : MonoBehaviour
    {
        public const int MaxLevel = 5;

        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string lore = "";
        [Tooltip("What the item does, in one plain sentence, for the cards.")]
        [SerializeField, TextArea] private string summary = "";
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField, Range(1, MaxLevel)] private int level = 1;
        [Tooltip("How rare the item is: rarer chests offer rarer items.")]
        [SerializeField] private Rarity rarity = Rarity.Common;
        [Tooltip("The schools the item belongs to (set bonuses, and the status its hits leave: Flame burns, Frost chills, Storm shocks, Blood bleeds).")]
        [SerializeField] private School[] schools = new School[0];

        private float cooldownTimer;

        protected PlayerStats Stats { get; private set; }
        protected SynergySystem Synergy { get; private set; }
        public IReadOnlyList<School> Schools => schools;
        /// <summary>Extra shots per volley from the Arcane set.</summary>
        protected int ExtraProjectiles => Synergy != null ? Synergy.ExtraProjectiles : 0;

        public string DisplayName => Loc.T(displayName);
        public string Lore => Loc.T(lore);
        public string Summary => Loc.T(summary);
        /// <summary>Damage this item has dealt in the run with its own hits (the run-end screen ranks items by it).</summary>
        public float DamageDealt { get; private set; }
        public int Level => level;
        public bool IsMaxed => level >= MaxLevel;
        public Rarity Rarity => rarity;
        /// <summary>Passive items tick all the time and take a passive slot instead of an attack slot.</summary>
        public virtual bool IsPassive => false;
        public virtual float Damage => baseDamage * (Stats != null ? Stats.DamageMultiplier : 1f)
            * (Synergy != null ? Synergy.DamageMultiplier * (1f + Synergy.SchoolDamageBonus(this)) : 1f);
        protected float BaseDamage => baseDamage;
        protected float Cooldown => cooldown;
        protected virtual float EffectiveCooldown => cooldown / (Stats != null ? Mathf.Max(0.1f, Stats.AttackSpeedMultiplier) : 1f)
            * (Synergy != null ? Synergy.CooldownMultiplier : 1f);

        protected virtual void Awake()
        {
            Stats = GetComponentInParent<PlayerStats>();
            Synergy = GetComponentInParent<SynergySystem>();
        }

        /// <summary>Hits an enemy as this item: its schools' statuses first (so the hit counts them), then the damage.</summary>
        protected void Strike(EnemyBase enemy, float damage)
        {
            if (enemy == null || enemy.IsDead) return;
            if (Synergy != null) Synergy.OnItemHit(this, enemy, damage);
            DamageDealt += damage;
            enemy.TakeDamage(damage);
        }

        /// <summary>Called every frame by CombatSystem: attack items only while attacks are active (player airborne), passives always.</summary>
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

        /// <summary>One-glance lines for the cards ("Damage 12", "Every 1.5 s"); items add their own after these.</summary>
        public virtual IEnumerable<string> StatLines()
        {
            if (baseDamage > 0f) yield return Loc.F("Damage {0}", Damage.ToString("0"));
            if (!IsPassive && cooldown > 0f) yield return Loc.F("Every {0} s", EffectiveCooldown.ToString("0.0#"));
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
