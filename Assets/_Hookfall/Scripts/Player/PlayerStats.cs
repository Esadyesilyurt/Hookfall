// Hookfall - player health, armor, every stat upgrade and the abyss check (GDD 4.3, 4.6).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with all 11 stat upgrades and on 2026-11-08 with a base health regen and the passive-item hooks
// (shield, revive, crits, low-health damage, healing). Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
using Hookfall.Enemies;
using UnityEngine;

namespace Hookfall.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Base regen from the start of a run (0 until 2026-11-08); Health regen picks add to it.")]
        [SerializeField] private float healthRegenPerSecond = 1f;
        [SerializeField] private float armor = 0f;
        [SerializeField] private float invulnerabilityAfterHit = 0.5f;

        [Header("Base values")]
        [SerializeField] private float basePickupRadius = 4f;
        [SerializeField] private float baseHookRange = 40f;

        [Header("Per pick (GDD 4.6)")]
        [SerializeField] private float maxHealthPerPick = 20f;
        [SerializeField] private float armorPerPick = 1f;
        [SerializeField] private float damagePercentPerPick = 10f;
        [SerializeField] private float attackSpeedPercentPerPick = 8f;
        [SerializeField] private float walkSpeedPercentPerPick = 8f;
        [Tooltip("Raises the air speed cap (5% until 2026-11-08, when the cap became a real limit).")]
        [SerializeField] private float flightSpeedPercentPerPick = 10f;
        [SerializeField] private float hookRangePerPick = 5f;
        [SerializeField] private float pickupRadiusPercentPerPick = 25f;
        [SerializeField] private float regenPerPick = 0.5f;
        [Tooltip("Lifesteal per pick, in percent of damage dealt (added 2026-11-08).")]
        [SerializeField] private float lifestealPercentPerPick = 2f;
        [Tooltip("The most that lifesteal and heal-on-kill can give back per second, as a share of max health. Without it a " +
                 "late build healed faster than anything could hurt it (2026-11-12).")]
        [SerializeField] private float combatHealPerSecond = 0.04f;
        [SerializeField] private float xpPercentPerPick = 10f;
        [SerializeField] private float luckPercentPerPick = 10f;

        [Header("Zip: right-click hook (added 2026-11-07)")]
        [SerializeField] private float baseZipCooldown = 4f;
        [SerializeField] private float baseZipRange = 50f;
        [Tooltip("Each Zip cooldown pick multiplies the cooldown by (1 - this %).")]
        [SerializeField] private float zipCooldownPercentPerPick = 10f;
        [SerializeField] private float zipRangePerPick = 5f;
        [SerializeField] private float minZipCooldown = 0.6f;

        [Header("Movement soft cap")]
        [Tooltip("Movement bonuses count fully up to +100%; above that only this share of each extra pick counts.")]
        [SerializeField] private float movementSoftCapRate = 0.25f;

        [Header("World")]
        [Tooltip("Falling below this height ends the run. IslandGenerator sets it to 60 m below the lowest island.")]
        [SerializeField] private float abyssKillHeight = -70f;

        private float lastHitTime = -999f;
        private float damageBonus, attackSpeedBonus, walkSpeedBonus, flightSpeedBonus;
        private float hookRangeBonus, pickupRadiusBonus, xpBonus, luckBonus, lifesteal;
        private int zipCooldownPicks;
        private float zipRangeBonus;
        private float itemZipCooldownMultiplier = 1f;
        private int itemZipCharges;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float Armor => armor;
        public float Regen => healthRegenPerSecond;
        public float DamageMultiplier => 1f + damageBonus + (CurrentHealth < maxHealth * 0.5f ? LowHealthDamageBonus : 0f);

        // Set by passive items.
        /// <summary>Chance for an enemy hit to deal double damage.</summary>
        public float CritChance { get; set; }
        /// <summary>Extra damage multiplier while below half health.</summary>
        public float LowHealthDamageBonus { get; set; }
        /// <summary>Each charge blocks one hit completely.</summary>
        public int ShieldCharges { get; set; }
        /// <summary>Each one brings the Warden back when health reaches zero, with this share of max health.</summary>
        public int Revives { get; set; }
        public float ReviveHealth { get; set; } = 0.5f;
        public float AttackSpeedMultiplier => 1f + attackSpeedBonus;
        public float XPMultiplier => 1f + xpBonus;
        public float LuckMultiplier => 1f + luckBonus;
        public float WalkSpeedMultiplier => 1f + SoftCap(walkSpeedBonus);
        public float FlightSpeedMultiplier => 1f + SoftCap(flightSpeedBonus);
        public float HookRange => baseHookRange + hookRangeBonus;
        public float PickupRadius => basePickupRadius * (1f + pickupRadiusBonus);
        public bool IsDead => CurrentHealth <= 0f;
        public float ZipCooldown => Mathf.Max(minZipCooldown,
            baseZipCooldown * Mathf.Pow(1f - zipCooldownPercentPerPick / 100f, zipCooldownPicks) * itemZipCooldownMultiplier);
        public float ZipRange => baseZipRange + zipRangeBonus;
        public int ZipCharges => 1 + itemZipCharges;

        /// <summary>Set by the Tether Coil item: a cooldown multiplier and extra Zip charges.</summary>
        public void SetZipItemBonus(float cooldownMultiplier, int extraCharges)
        {
            itemZipCooldownMultiplier = cooldownMultiplier;
            itemZipCharges = extraCharges;
        }

        public event Action<float> Damaged;
        /// <summary>Where the hit that raised Damaged came from, when the attacker said so (the HUD points at it).</summary>
        public Vector3? LastDamageSource { get; private set; }
        private Vector3? damageSource;

        /// <summary>Damage from a known place: an enemy's bite, a boulder's flight path.</summary>
        public void TakeDamageFrom(float amount, Vector3 source)
        {
            damageSource = source;
            TakeDamage(amount);
            damageSource = null;
        }
        /// <summary>The character's health at the start of a run (CharacterLoadout); also fills health.</summary>
        public void ScaleMaxHealth(float multiplier)
        {
            maxHealth *= multiplier;
            CurrentHealth = maxHealth;
        }

        /// <summary>The small damage gain every level-up gives (LevelUpSystem), on top of any Damage cards.</summary>
        public void AddLevelDamage(float bonus) => damageBonus += bonus;

        public event Action ShieldBlocked;
        public event Action Revived;

        private float lastIronWill = -999f;

        public void Heal(float amount)
        {
            if (IsDead) return;
            // Iron Will (Blood + Guardian duo): healing past full raises a one-hit shield, at most every 6 s.
            if (CurrentHealth + amount > maxHealth && ShieldCharges == 0 && synergy != null && synergy.HasDuo(Hookfall.Combat.Duo.IronWill) && Time.time - lastIronWill > 6f)
            {
                ShieldCharges = 1;
                lastIronWill = Time.time;
                ShieldRaised?.Invoke();
            }
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        public event Action ShieldRaised;

        private Hookfall.Combat.SynergySystem synergy;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            synergy = GetComponent<Hookfall.Combat.SynergySystem>();
        }

        /// <summary>Share of all damage dealt that comes back as health (Lifesteal cards and items).</summary>
        public float Lifesteal => lifesteal;

        private void OnEnable() => EnemyDirector.Hit += OnEnemyHit;
        private void OnDisable() => EnemyDirector.Hit -= OnEnemyHit;

        private void OnEnemyHit(EnemyBase enemy, float damage)
        {
            if (lifesteal > 0f) HealFromCombat(damage * lifesteal);
        }

        private float combatHealBudget;

        /// <summary>Healing earned by fighting (lifesteal, heal on kill): at most combatHealPerSecond of max health per
        /// second, twice that while the Blood set doubles lifesteal. The budget holds one second's worth.</summary>
        public void HealFromCombat(float amount)
        {
            float given = Mathf.Min(amount * (synergy != null ? synergy.LifestealMultiplier : 1f), combatHealBudget);
            if (given <= 0f) return;
            combatHealBudget -= given;
            Heal(given);
        }

        private void Update()
        {
            float healCap = maxHealth * combatHealPerSecond * (synergy != null ? synergy.LifestealMultiplier : 1f);
            combatHealBudget = Mathf.Min(healCap, combatHealBudget + healCap * Time.deltaTime);
            if (healthRegenPerSecond > 0f && CurrentHealth > 0f)
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healthRegenPerSecond * Time.deltaTime);

            if (transform.position.y < abyssKillHeight)
                GameManager.Instance?.EndRun("Fell into the abyss");
        }

        public void SetAbyssKillHeight(float height) => abyssKillHeight = height;

        public void TakeDamage(float amount)
        {
            if (IsDead || Time.time - lastHitTime < invulnerabilityAfterHit) return;
            lastHitTime = Time.time;
            if (ShieldCharges > 0)
            {
                ShieldCharges--;
                ShieldBlocked?.Invoke();
                return;
            }

            float damage = Mathf.Max(1f, amount - armor);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            LastDamageSource = damageSource;
            Damaged?.Invoke(damage);
            if (IsDead && Revives > 0)
            {
                Revives--;
                CurrentHealth = maxHealth * ReviveHealth;
                Revived?.Invoke();
                return;
            }
            if (IsDead) GameManager.Instance?.EndRun("Health reached zero");
        }

        private float SoftCap(float bonus) => bonus <= 1f ? bonus : 1f + (bonus - 1f) * movementSoftCapRate;

        /// <summary>Adds one pick of a stat; times -1 takes one back (a stat charm given up for another item).</summary>
        public void Apply(StatType stat, int times = 1)
        {
            switch (stat)
            {
                case StatType.MaxHealth:
                    maxHealth += maxHealthPerPick * times;
                    CurrentHealth = Mathf.Clamp(CurrentHealth + maxHealthPerPick * times, 1f, maxHealth);
                    break;
                case StatType.Armor: armor += armorPerPick * times; break;
                case StatType.Damage: damageBonus += damagePercentPerPick / 100f * times; break;
                case StatType.AttackSpeed: attackSpeedBonus += attackSpeedPercentPerPick / 100f * times; break;
                case StatType.WalkSpeed: walkSpeedBonus += walkSpeedPercentPerPick / 100f * times; break;
                case StatType.FlightSpeed: flightSpeedBonus += flightSpeedPercentPerPick / 100f * times; break;
                case StatType.HookRange: hookRangeBonus += hookRangePerPick * times; break;
                case StatType.PickupRadius: pickupRadiusBonus += pickupRadiusPercentPerPick / 100f * times; break;
                case StatType.Regen: healthRegenPerSecond += regenPerPick * times; break;
                case StatType.Lifesteal: lifesteal += lifestealPercentPerPick / 100f * times; break;
                case StatType.XPGain: xpBonus += xpPercentPerPick / 100f * times; break;
                case StatType.Luck: luckBonus += luckPercentPerPick / 100f * times; break;
                case StatType.ZipCooldown: zipCooldownPicks += times; break;
                case StatType.ZipRange: zipRangeBonus += zipRangePerPick * times; break;
            }
        }

        /// <summary>What one pick of a stat gives, at a glance: "+20 Max health", "+10% Damage".</summary>
        public string PickText(StatType stat)
        {
            string name = Loc.T(NameOf(stat));
            switch (stat)
            {
                case StatType.MaxHealth: return $"+{maxHealthPerPick:0} {name}";
                case StatType.Armor: return $"+{armorPerPick:0} {name}";
                case StatType.Damage: return $"+{damagePercentPerPick:0}% {name}";
                case StatType.AttackSpeed: return $"+{attackSpeedPercentPerPick:0}% {name}";
                case StatType.WalkSpeed: return $"+{walkSpeedPercentPerPick:0}% {name}";
                case StatType.FlightSpeed: return $"+{flightSpeedPercentPerPick:0}% {name}";
                case StatType.HookRange: return $"+{hookRangePerPick:0} m {name}";
                case StatType.PickupRadius: return $"+{pickupRadiusPercentPerPick:0}% {name}";
                case StatType.Regen: return $"+{regenPerPick:0.0}/s {name}";
                case StatType.Lifesteal: return $"+{lifestealPercentPerPick:0}% {name}";
                case StatType.XPGain: return $"+{xpPercentPerPick:0}% {name}";
                case StatType.ZipCooldown: return $"-{zipCooldownPercentPerPick:0}% {name}";
                case StatType.ZipRange: return $"+{zipRangePerPick:0} m {name}";
                default: return $"+{luckPercentPerPick:0}% {name}";
            }
        }

        public static string NameOf(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return "Max health";
                case StatType.Armor: return "Armor";
                case StatType.Damage: return "Damage";
                case StatType.AttackSpeed: return "Attack speed";
                case StatType.WalkSpeed: return "Walk speed";
                case StatType.FlightSpeed: return "Max flight speed";
                case StatType.HookRange: return "Hook range";
                case StatType.PickupRadius: return "Pickup radius";
                case StatType.Regen: return "Health regen";
                case StatType.Lifesteal: return "Lifesteal";
                case StatType.XPGain: return "XP gain";
                case StatType.ZipCooldown: return "Zip cooldown";
                case StatType.ZipRange: return "Zip range";
                default: return "Luck";
            }
        }

        /// <summary>The value shown on a level-up card: current value, and the value after one more pick.</summary>
        public string Describe(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return $"{maxHealth:0} → {maxHealth + maxHealthPerPick:0}";
                case StatType.Armor: return $"{armor:0} → {armor + armorPerPick:0}";
                case StatType.Damage: return Percent(damageBonus, damagePercentPerPick);
                case StatType.AttackSpeed: return Percent(attackSpeedBonus, attackSpeedPercentPerPick);
                case StatType.WalkSpeed: return Percent(walkSpeedBonus, walkSpeedPercentPerPick);
                case StatType.FlightSpeed: return Percent(flightSpeedBonus, flightSpeedPercentPerPick);
                case StatType.HookRange: return $"{HookRange:0} m → {HookRange + hookRangePerPick:0} m";
                case StatType.PickupRadius:
                    return $"{PickupRadius:0.0} m → {basePickupRadius * (1f + pickupRadiusBonus + pickupRadiusPercentPerPick / 100f):0.0} m";
                case StatType.Regen: return $"{healthRegenPerSecond:0.0}/s → {healthRegenPerSecond + regenPerPick:0.0}/s";
                case StatType.Lifesteal: return $"{lifesteal * 100f:0}% → {lifesteal * 100f + lifestealPercentPerPick:0}%";
                case StatType.XPGain: return Percent(xpBonus, xpPercentPerPick);
                case StatType.ZipCooldown:
                    return $"{ZipCooldown:0.0} s → {Mathf.Max(minZipCooldown, ZipCooldown * (1f - zipCooldownPercentPerPick / 100f)):0.0} s";
                case StatType.ZipRange: return $"{ZipRange:0} m → {ZipRange + zipRangePerPick:0} m";
                default: return Percent(luckBonus, luckPercentPerPick);
            }
        }

        private static string Percent(float bonus, float perPick) => $"+{bonus * 100f:0}% → +{bonus * 100f + perPick:0}%";
    }
}
