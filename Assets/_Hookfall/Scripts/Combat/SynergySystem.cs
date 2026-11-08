// Hookfall - builds (2026-11-08): counts the owned items of every school (plus the character's affinity), grants the
// 2- and 4-item set bonuses, applies the school statuses when items hit (burn, chill, shock, bleed), and runs the
// chain effects: burns spreading from the dead, lightning bursts, freezes, bleeding on every hit, guardian shockwaves.
// Chain effects are queued and run over the next frames, so a chain reaction never recurses inside one hit.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat.Items;
using Hookfall.Enemies;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public class SynergySystem : MonoBehaviour
    {
        public const int FirstTier = 2;
        public const int SecondTier = 4;

        [Header("Statuses items apply (by their schools)")]
        [Tooltip("Burn per second as a share of the hit's damage.")]
        [SerializeField] private float burnShare = 0.3f;
        [SerializeField] private float burnTime = 3f;
        [SerializeField] private float chillSlow = 0.3f;
        [SerializeField] private float chillTime = 2f;
        [Tooltip("Extra damage shocked enemies take.")]
        [SerializeField] private float shockAmp = 0.1f;
        [SerializeField] private float shockTime = 2f;
        [Tooltip("Bleed per second as a share of the hit's damage.")]
        [SerializeField] private float bleedShare = 0.2f;
        [SerializeField] private float bleedTime = 3f;

        [Header("Set bonuses")]
        [SerializeField] private float flameBurnMultiplier = 1.5f;
        [SerializeField] private float flameSpreadRadius = 5f;
        [SerializeField] private float frostChillSlow = 0.5f;
        [SerializeField] private float frostChillAmp = 0.15f;
        [SerializeField] private float freezeChance = 0.12f;
        [SerializeField] private float freezeTime = 1.5f;
        [SerializeField] private float stormShockAmp = 0.25f;
        [SerializeField] private float stormBurstRadius = 4f;
        [SerializeField] private float stormBurstDamage = 25f;
        [SerializeField] private float guardianWaveRadius = 6f;
        [SerializeField] private float guardianWaveDamage = 30f;
        [SerializeField] private float guardianWaveCooldown = 2f;
        [Tooltip("Wild 4: +damage per m/s of speed above wildSpeedStart, up to wildMaxDamage.")]
        [SerializeField] private float wildSpeedStart = 10f;
        [SerializeField] private float wildDamagePerSpeed = 0.012f;
        [SerializeField] private float wildMaxDamage = 0.4f;
        [SerializeField] private float arcaneCooldown = 0.8f;
        [SerializeField] private int maxChainsPerFrame = 24;

        private readonly int[] counts = new int[SchoolInfo.All.Length];
        private readonly bool[,] granted = new bool[SchoolInfo.All.Length, 2];
        private readonly Queue<(Vector3 at, School school, float strength)> chains = new Queue<(Vector3, School, float)>();
        private readonly List<EnemyBase> near = new List<EnemyBase>();
        private CombatSystem combat;
        private PlayerStats stats;
        private PlayerController player;
        private CharacterLoadout loadout;
        private float lastWave = -999f;

        /// <summary>Raised when the counts change (an item was gained), for the HUD.</summary>
        public event Action Changed;
        /// <summary>For effects: a chain effect went off (position, radius, school).</summary>
        public static event Action<Vector3, float, School> ChainFired;

        /// <summary>How many chain effects have gone off this run (for tests and tuning).</summary>
        public int ChainsFired { get; private set; }

        public int Count(School school) => counts[(int)school];
        public bool Has(School school, int tier) => counts[(int)school] >= tier;
        public School? Affinity => loadout != null && loadout.Current != null ? loadout.Current.affinity : (School?)null;

        // ---------- What items and enemies read ----------

        public int ExtraProjectiles => Has(School.Arcane, FirstTier) ? 1 : 0;
        public float CooldownMultiplier => Has(School.Arcane, SecondTier) ? arcaneCooldown : 1f;
        public float DamageMultiplier => Has(School.Wild, SecondTier) && player != null
            ? 1f + Mathf.Clamp((player.Speed - wildSpeedStart) * wildDamagePerSpeed, 0f, wildMaxDamage) : 1f;
        public float ShockAmp => shockAmp + (Has(School.Storm, FirstTier) ? stormShockAmp : 0f) + CharmBoost(School.Storm);
        public float ChillAmp => (Has(School.Frost, FirstTier) ? frostChillAmp : 0f) + CharmBoost(School.Frost);
        public float LifestealMultiplier => Has(School.Blood, SecondTier) && stats != null && stats.CurrentHealth < stats.MaxHealth * 0.5f ? 2f : 1f;
        private float BurnMultiplier => (Has(School.Flame, FirstTier) ? flameBurnMultiplier : 1f) * (1f + CharmBoost(School.Flame));
        private float BleedMultiplier => 1f + CharmBoost(School.Blood);
        private float ChillSlow => Has(School.Frost, FirstTier) ? frostChillSlow : chillSlow;

        /// <summary>Extra damage for an item from the school charms of its schools (the Arcane focus).</summary>
        public float SchoolDamageBonus(ItemBase item)
        {
            float bonus = 0f;
            foreach (School school in item.Schools)
                if (school == School.Arcane) bonus += CharmBoost(school);
            return bonus;
        }

        private float CharmBoost(School school)
        {
            if (combat == null) return 0f;
            float boost = 0f;
            foreach (ItemBase item in combat.Items)
                if (item is SchoolCharm charm && charm.Boosted == school) boost += charm.Boost;
            return boost;
        }

        // ---------- Counting and set bonuses ----------

        private void Awake()
        {
            combat = GetComponent<CombatSystem>();
            stats = GetComponent<PlayerStats>();
            player = GetComponent<PlayerController>();
            loadout = GetComponent<CharacterLoadout>();
        }

        private void OnEnable()
        {
            EnemyDirector.Hit += OnEnemyHit;
            EnemyDirector.Died += OnEnemyDied;
            if (stats != null) stats.Damaged += OnPlayerHurt;
            if (combat != null) combat.ItemsChanged += Recount;
        }

        private void OnDisable()
        {
            EnemyDirector.Hit -= OnEnemyHit;
            EnemyDirector.Died -= OnEnemyDied;
            if (stats != null) stats.Damaged -= OnPlayerHurt;
            if (combat != null) combat.ItemsChanged -= Recount;
        }

        private void Start() => Recount();

        public void Recount()
        {
            Array.Clear(counts, 0, counts.Length);
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                {
                    var seen = new HashSet<School>(item.Schools);   // an item counts once per school
                    foreach (School school in seen) counts[(int)school]++;
                }
            if (Affinity.HasValue) counts[(int)Affinity.Value]++;

            // Items are never lost, so a bonus that is reached stays: grant the stat ones once.
            foreach (School school in SchoolInfo.All)
            {
                if (Has(school, FirstTier) && !granted[(int)school, 0]) { granted[(int)school, 0] = true; GrantFirst(school); }
                if (Has(school, SecondTier) && !granted[(int)school, 1]) { granted[(int)school, 1] = true; GrantSecond(school); }
            }
            Changed?.Invoke();
        }

        private void GrantFirst(School school)
        {
            if (stats == null) return;
            switch (school)
            {
                case School.Blood: stats.Apply(StatType.Lifesteal); stats.Apply(StatType.Lifesteal); break;
                case School.Guardian: for (int i = 0; i < 3; i++) stats.Apply(StatType.Armor); break;
                case School.Wild: stats.Apply(StatType.FlightSpeed); stats.Apply(StatType.WalkSpeed); break;
                case School.Fortune: stats.Apply(StatType.XPGain); stats.Apply(StatType.XPGain); stats.Apply(StatType.Luck); stats.Apply(StatType.Luck); break;
            }
        }

        private void GrantSecond(School school)
        {
            if (school == School.Fortune && combat != null) combat.SetChestChoices(4);
        }

        // ---------- Statuses and chain effects ----------

        /// <summary>An item of these schools hit an enemy: apply the schools' statuses (before the damage lands).</summary>
        public void OnItemHit(ItemBase item, EnemyBase enemy, float damage)
        {
            foreach (School school in item.Schools)
            {
                switch (school)
                {
                    case School.Flame: enemy.Burn(damage * burnShare * BurnMultiplier, burnTime); break;
                    case School.Frost: enemy.Chill(ChillSlow, chillTime); break;
                    case School.Storm: enemy.Shock(shockTime); break;
                    case School.Blood: enemy.Bleed(damage * bleedShare * BleedMultiplier, bleedTime); break;
                }
            }
        }

        private void OnEnemyHit(EnemyBase enemy, float damage)
        {
            if (enemy.IsDead) return;
            if (Has(School.Frost, SecondTier) && enemy.Chilled && !enemy.Frozen && UnityEngine.Random.value < freezeChance)
                enemy.Freeze(freezeTime);
            if (Has(School.Blood, SecondTier) && !enemy.Bleeding)   // not while bleeding: bleed ticks are hits too
                enemy.Bleed(damage * bleedShare * BleedMultiplier, bleedTime);
        }

        private void OnEnemyDied(EnemyBase enemy, bool killed)
        {
            if (!killed) return;
            if (Has(School.Flame, SecondTier) && enemy.Burning) chains.Enqueue((enemy.transform.position, School.Flame, enemy.BurnDps));
            if (Has(School.Storm, SecondTier) && enemy.Shocked) chains.Enqueue((enemy.transform.position, School.Storm, 0f));
        }

        private void OnPlayerHurt(float amount)
        {
            // Like every damaging effect, the guardian shockwave works only in flight.
            if (!Has(School.Guardian, SecondTier) || player == null || !player.IsAirborne || Time.time - lastWave < guardianWaveCooldown) return;
            lastWave = Time.time;
            chains.Enqueue((transform.position, School.Guardian, 0f));
        }

        private void Update()
        {
            for (int i = 0; i < maxChainsPerFrame && chains.Count > 0; i++)
            {
                (Vector3 at, School school, float strength) = chains.Dequeue();
                ChainsFired++;
                float damageScale = stats != null ? stats.DamageMultiplier : 1f;
                switch (school)
                {
                    case School.Flame:
                        EnemyQuery.InRadius(at, flameSpreadRadius, near);
                        foreach (EnemyBase e in near) if (!e.Burning) e.Burn(strength, burnTime);
                        ChainFired?.Invoke(at, flameSpreadRadius, school);
                        break;
                    case School.Storm:
                        EnemyQuery.InRadius(at, stormBurstRadius, near);
                        foreach (EnemyBase e in near) { e.Shock(shockTime); e.TakeDamage(stormBurstDamage * damageScale); }
                        ChainFired?.Invoke(at, stormBurstRadius, school);
                        break;
                    case School.Guardian:
                        EnemyQuery.InRadius(at, guardianWaveRadius, near);
                        foreach (EnemyBase e in near) e.TakeDamage(guardianWaveDamage * damageScale);
                        ChainFired?.Invoke(at, guardianWaveRadius, school);
                        break;
                }
            }
        }
    }
}
