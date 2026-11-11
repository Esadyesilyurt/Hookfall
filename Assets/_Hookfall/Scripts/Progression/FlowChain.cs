// Hookfall - the flight chain (added 2026-11-10): every enemy killed while the Warden stays in the air adds to a
// chain, and the chain pays: more XP and gold from each kill. It breaks when the Warden lands (once the attacks have
// switched off), when they are hit, or when no kill comes for a few seconds. It rewards what the game is about:
// staying up, staying fast, not getting touched.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    public class FlowChain : MonoBehaviour
    {
        [Tooltip("Extra XP and gold per kill in the chain: 0.01 = +1%.")]
        [SerializeField] private float bonusPerKill = 0.01f;
        [Tooltip("The bonus stops growing here: 0.75 = +75%.")]
        [SerializeField] private float maxBonus = 0.75f;
        [Tooltip("Seconds without a kill before the chain breaks.")]
        [SerializeField] private float window = 5f;
        [Tooltip("Chain lengths at which the flow is named (Gust, Gale, Storm, Tempest).")]
        [SerializeField] private int[] tiers = { 10, 25, 50, 75 };

        private static readonly string[] TierNames = { "Gust", "Gale", "Storm", "Tempest" };

        private PlayerStats stats;
        private CombatSystem combat;
        private float timer;

        public static FlowChain Instance { get; private set; }

        /// <summary>Kills in the chain right now.</summary>
        public int Count { get; private set; }
        /// <summary>The longest chain of the run.</summary>
        public int Best { get; private set; }
        /// <summary>What a kill's XP and gold are multiplied by.</summary>
        public float Multiplier => 1f + Mathf.Min(maxBonus, Count * bonusPerKill);
        /// <summary>1 right after a kill, 0 when the chain is about to break.</summary>
        public float TimeLeft01 => Count > 0 ? Mathf.Clamp01(timer / window) : 0f;
        /// <summary>0 before the first named tier, then 1..4.</summary>
        public int Tier
        {
            get
            {
                int tier = 0;
                while (tier < tiers.Length && Count >= tiers[tier]) tier++;
                return tier;
            }
        }
        public string TierName => Tier > 0 ? Loc.T(TierNames[Mathf.Min(Tier, TierNames.Length) - 1]) : "";

        /// <summary>The multiplier for rewards, 1 when there is no chain (or no FlowChain in the scene).</summary>
        public static float Reward => Instance != null ? Instance.Multiplier : 1f;

        /// <summary>The chain reached a new named tier (1..4).</summary>
        public event Action<int> TierReached;
        /// <summary>The chain broke; the argument is how long it was.</summary>
        public event Action<int> Broken;

        private void Awake()
        {
            Instance = this;
            stats = GetComponent<PlayerStats>();
            combat = GetComponent<CombatSystem>();
        }

        private void OnEnable()
        {
            EnemyDirector.Died += OnEnemyDied;
            if (stats != null) stats.Damaged += OnHurt;
            if (combat != null) combat.AttacksChanged += OnAttacksChanged;
        }

        private void OnDisable()
        {
            EnemyDirector.Died -= OnEnemyDied;
            if (stats != null) stats.Damaged -= OnHurt;
            if (combat != null) combat.AttacksChanged -= OnAttacksChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Count == 0) return;
            timer -= Time.deltaTime;
            if (timer <= 0f) Break();
        }

        private void OnEnemyDied(EnemyBase enemy, bool killed)
        {
            if (!killed || combat == null || !combat.AttacksActive) return;
            int before = Tier;
            Count++;
            Best = Mathf.Max(Best, Count);
            timer = window;
            if (Tier > before) TierReached?.Invoke(Tier);
        }

        private void OnHurt(float amount) => Break();

        private void OnAttacksChanged(bool on)
        {
            if (!on) Break();   // landed for good: the attacks are off, the chain is over
        }

        private void Break()
        {
            if (Count == 0) return;
            int was = Count;
            Count = 0;
            timer = 0f;
            Broken?.Invoke(was);
        }
    }
}
