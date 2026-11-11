// Hookfall - level-up: pauses the game and offers 3 random cards, each a level for an owned item or a stat (GDD 4.6, 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 for the 3-card choice; on 2026-11-10 item levels became likelier than stats, and the set
// can be rerolled, a card banished, or the level skipped (ChoiceTokens). Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Tooltip("How much likelier a card for an owned item is than a card for one stat. With fourteen stats in the pool " +
                 "and a few items, equal odds showed the build's own items too rarely (2026-11-10).")]
        [SerializeField] private float itemWeight = 3f;

        private XPSystem xp;
        private PlayerStats stats;
        private CombatSystem combat;
        private ChoiceTokens tokens;
        private GoldWallet gold;
        private int pendingLevels;
        private readonly List<LevelUpOption> pool = new List<LevelUpOption>();
        private readonly List<LevelUpOption> offer = new List<LevelUpOption>();

        public IReadOnlyList<LevelUpOption> Offer => offer;
        public int PendingLevels => pendingLevels;
        public ChoiceTokens Tokens => tokens;

        /// <summary>A level was gained and its cards are ready (the jingle and the flash hang on this).</summary>
        public event Action OfferReady;
        /// <summary>The cards on the table changed (a reroll, a banish).</summary>
        public event Action OfferChanged;
        public event Action<LevelUpOption> Picked;
        /// <summary>The level was skipped for gold.</summary>
        public event Action Skipped;

        private void Awake()
        {
            xp = GetComponent<XPSystem>();
            stats = GetComponent<PlayerStats>();
            combat = GetComponent<CombatSystem>();
            tokens = GetComponent<ChoiceTokens>();
            gold = GetComponent<GoldWallet>();
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

        /// <summary>Draws the cards: every stat once, every owned item below its top level itemWeight times as likely,
        /// nothing that was banished.</summary>
        private void BuildOffer()
        {
            pool.Clear();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                if (tokens == null || !tokens.IsBanished(stat)) pool.Add(new LevelUpOption(stat));
            int statCount = pool.Count;
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                    if (item.Level < ItemBase.MaxLevel && (tokens == null || !tokens.IsBanished(item))) pool.Add(new LevelUpOption(item));

            offer.Clear();
            for (int i = 0; i < choices && pool.Count > 0; i++)
            {
                // A weighted draw without putting the card back: stats come first in the pool, items after them.
                float total = statCount + (pool.Count - statCount) * itemWeight;
                float roll = UnityEngine.Random.value * total;
                int index = roll < statCount ? Mathf.Min(statCount - 1, Mathf.FloorToInt(roll))
                    : Mathf.Min(pool.Count - 1, statCount + Mathf.FloorToInt((roll - statCount) / itemWeight));
                offer.Add(pool[index]);
                pool.RemoveAt(index);
                if (index < statCount) statCount--;
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

        /// <summary>Draws the cards again; costs a reroll.</summary>
        public bool Reroll()
        {
            if (offer.Count == 0 || tokens == null || !tokens.TryReroll()) return false;
            BuildOffer();
            OfferChanged?.Invoke();
            return true;
        }

        /// <summary>Removes a card from this run for good and draws again; costs a banish.</summary>
        public bool Banish(int index)
        {
            if (index < 0 || index >= offer.Count || tokens == null) return false;
            LevelUpOption option = offer[index];
            if (!(option.IsItem ? tokens.TryBanish(option.Item) : tokens.TryBanish(option.Stat))) return false;
            BuildOffer();
            OfferChanged?.Invoke();
            return true;
        }

        /// <summary>Takes no card and a little gold instead.</summary>
        public void Skip()
        {
            if (offer.Count == 0) return;
            if (gold != null && tokens != null) gold.Add(tokens.SkipGold);
            offer.Clear();
            pendingLevels--;
            Skipped?.Invoke();
            GameManager.Instance?.CloseModal();
            TryShowNext();
        }
    }
}
