// Hookfall - what the player may do with a set of cards besides picking one (added 2026-11-10): reroll the whole set
// (a few times per run), banish a card so it is never offered again in this run (a few times), or skip a level-up for
// a little gold. Shared by the level-up screen and the chest screen.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    public class ChoiceTokens : MonoBehaviour
    {
        [Tooltip("Rerolls at the start of a run (the upgrade shop adds to it).")]
        [SerializeField] private int rerolls = 2;
        [Tooltip("Banishes at the start of a run.")]
        [SerializeField] private int banishes = 2;
        [Tooltip("Gold for skipping a level-up.")]
        [SerializeField] private int skipGold = 8;

        private readonly HashSet<string> banished = new HashSet<string>();

        // The upgrades bought in the menu's shop: free stat picks, more rerolls and banishes.
        private void Start() => MetaProgress.ApplyTo(GetComponent<PlayerStats>(), this);

        public int Rerolls => rerolls;
        public int Banishes => banishes;
        public int SkipGold => skipGold;

        /// <summary>More rerolls and banishes for this run (permanent upgrades).</summary>
        public void Add(int moreRerolls, int moreBanishes)
        {
            rerolls += moreRerolls;
            banishes += moreBanishes;
        }

        public bool TryReroll()
        {
            if (rerolls <= 0) return false;
            rerolls--;
            return true;
        }

        public bool TryBanish(StatType stat) => TryBanish(Key(stat));
        public bool TryBanish(ItemBase item) => item != null && TryBanish(Key(item));

        private bool TryBanish(string key)
        {
            if (banishes <= 0 || banished.Contains(key)) return false;
            banishes--;
            banished.Add(key);
            return true;
        }

        public bool IsBanished(StatType stat) => banished.Contains(Key(stat));
        public bool IsBanished(ItemBase item) => item != null && banished.Contains(Key(item));

        private static string Key(StatType stat) => "stat:" + stat;
        // By the prefab's name: an owned item and its prefab are the same card.
        private static string Key(ItemBase item) => "item:" + item.name.Replace("(Clone)", "");
    }
}
