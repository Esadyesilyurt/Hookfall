// Hookfall - level-up: pauses the game and offers 3 random cards, each a level for an owned item or a stat (GDD 4.6, 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 for the 3-card choice. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    [RequireComponent(typeof(XPSystem), typeof(PlayerStats))]
    public class LevelUpSystem : MonoBehaviour
    {
        [SerializeField] private int choices = 3;
        [Tooltip("Damage gained on every level-up regardless of the card picked (added 2026-11-08), as a share: 0.02 = +2%.")]
        [SerializeField] private float damagePerLevel = 0.02f;

        private XPSystem xp;
        private PlayerStats stats;
        private CombatSystem combat;
        private int pendingLevels;
        private readonly List<LevelUpOption> pool = new List<LevelUpOption>();
        private readonly List<LevelUpOption> offer = new List<LevelUpOption>();

        public IReadOnlyList<LevelUpOption> Offer => offer;
        public int PendingLevels => pendingLevels;

        /// <summary>Raised when a new set of cards is ready to show.</summary>
        public event Action OfferReady;
        public event Action<LevelUpOption> Picked;

        private void Awake()
        {
            xp = GetComponent<XPSystem>();
            stats = GetComponent<PlayerStats>();
            combat = GetComponent<CombatSystem>();
        }

        private void OnEnable() => xp.LeveledUp += OnLeveledUp;
        private void OnDisable() => xp.LeveledUp -= OnLeveledUp;

        private void OnLeveledUp(int newLevel)
        {
            pendingLevels++;
            stats.AddLevelDamage(damagePerLevel);
            TryShowNext();
        }

        private void Update()
        {
            // A level gained while another screen was open waits here until the game runs again.
            if (pendingLevels > 0 && offer.Count == 0) TryShowNext();
        }

        private void TryShowNext()
        {
            if (offer.Count > 0 || pendingLevels == 0) return;
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.OpenModal(RunState.LevelUp)) return;

            BuildOffer();
            OfferReady?.Invoke();
        }

        private void BuildOffer()
        {
            pool.Clear();
            foreach (StatType stat in Enum.GetValues(typeof(StatType))) pool.Add(new LevelUpOption(stat));
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                    if (item.Level < ItemBase.MaxLevel) pool.Add(new LevelUpOption(item));

            offer.Clear();
            for (int i = 0; i < choices && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                offer.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        public void Pick(int index)
        {
            if (index < 0 || index >= offer.Count) return;
            LevelUpOption option = offer[index];
            if (option.IsItem) option.Item.LevelUp();
            else stats.Apply(option.Stat);

            offer.Clear();
            pendingLevels--;
            Picked?.Invoke(option);
            GameManager.Instance?.CloseModal();
            TryShowNext();
        }
    }
}
