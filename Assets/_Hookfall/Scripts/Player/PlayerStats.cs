// Hookfall - player health, armor, multipliers and the abyss check (GDD 4.3, 4.6).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using UnityEngine;

namespace Hookfall.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float healthRegenPerSecond = 0f;
        [SerializeField] private float armor = 0f;
        [SerializeField] private float invulnerabilityAfterHit = 0.5f;

        [Header("Multipliers (raised by stat upgrades)")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float attackSpeedMultiplier = 1f;
        [SerializeField] private float xpMultiplier = 1f;

        [Header("World")]
        [Tooltip("Falling below this height ends the run (60 m below the lowest island).")]
        [SerializeField] private float abyssKillHeight = -70f;

        private float lastHitTime = -999f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float DamageMultiplier => damageMultiplier;
        public float AttackSpeedMultiplier => attackSpeedMultiplier;
        public float XPMultiplier => xpMultiplier;

        private void Awake() => CurrentHealth = maxHealth;

        private void Update()
        {
            if (healthRegenPerSecond > 0f && CurrentHealth > 0f)
                CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healthRegenPerSecond * Time.deltaTime);

            if (transform.position.y < abyssKillHeight)
                GameManager.Instance?.EndRun("Fell into the abyss");
        }

        public void TakeDamage(float amount)
        {
            if (CurrentHealth <= 0f || Time.time - lastHitTime < invulnerabilityAfterHit) return;
            lastHitTime = Time.time;

            float damage = Mathf.Max(1f, amount - armor);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            if (CurrentHealth <= 0f) GameManager.Instance?.EndRun("Health reached zero");
        }

        // Stat upgrades (GDD 4.6). Values per pick match the GDD table.
        public void AddMaxHealth(float amount) { maxHealth += amount; CurrentHealth += amount; }
        public void AddArmor(float amount) => armor += amount;
        public void AddDamagePercent(float percent) => damageMultiplier += percent / 100f;
        public void AddAttackSpeedPercent(float percent) => attackSpeedMultiplier += percent / 100f;
        public void AddRegen(float perSecond) => healthRegenPerSecond += perSecond;
        public void AddXPPercent(float percent) => xpMultiplier += percent / 100f;
    }
}
