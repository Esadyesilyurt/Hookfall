// Hookfall - player health, armor, every stat upgrade and the abyss check (GDD 4.3, 4.6).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with all 11 stat upgrades and on 2026-11-08 with a base health regen and the passive-item hooks
// (shield, revive, crits, low-health damage, healing). Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
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
        private float hookRangeBonus, pickupRadiusBonus, xpBonus, luckBonus;
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
        public event Action ShieldBlocked;
        public event Action Revived;

        public void Heal(float amount)
        {
            if (!IsDead) CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        private void Awake() => CurrentHealth = maxHealth;

        private void Update()
        {
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

        public void Apply(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: maxHealth += maxHealthPerPick; CurrentHealth += maxHealthPerPick; break;
                case StatType.Armor: armor += armorPerPick; break;
                case StatType.Damage: damageBonus += damagePercentPerPick / 100f; break;
                case StatType.AttackSpeed: attackSpeedBonus += attackSpeedPercentPerPick / 100f; break;
                case StatType.WalkSpeed: walkSpeedBonus += walkSpeedPercentPerPick / 100f; break;
                case StatType.FlightSpeed: flightSpeedBonus += flightSpeedPercentPerPick / 100f; break;
                case StatType.HookRange: hookRangeBonus += hookRangePerPick; break;
                case StatType.PickupRadius: pickupRadiusBonus += pickupRadiusPercentPerPick / 100f; break;
                case StatType.Regen: healthRegenPerSecond += regenPerPick; break;
                case StatType.XPGain: xpBonus += xpPercentPerPick / 100f; break;
                case StatType.Luck: luckBonus += luckPercentPerPick / 100f; break;
                case StatType.ZipCooldown: zipCooldownPicks++; break;
                case StatType.ZipRange: zipRangeBonus += zipRangePerPick; break;
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
