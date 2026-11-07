// Hookfall - places a chest on a random island every 45 s (max 3), charges 15 gold +10 per chest opened,
// and rolls rarity: Common 70%, Rare 25%, Epic 5%, shifted by Luck (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hookfall.World
{
    public class ChestSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Chest chestPrefab;

        [Header("Spawning")]
        [SerializeField] private float interval = 45f;
        [SerializeField] private int maxChests = 3;
        [Tooltip("Chests appear on islands at least this wide (tiny hook points have no room).")]
        [SerializeField] private float minIslandWidth = 5f;
        [Tooltip("The first chest appears this many seconds into the run.")]
        [SerializeField] private float firstChestAfter = 20f;

        [Header("Cost")]
        [SerializeField] private int baseCost = 15;
        [SerializeField] private int costIncrease = 10;

        [Header("Rarity weights (Common, Rare, Epic); Luck multiplies Rare and Epic")]
        [SerializeField] private Vector3 rarityWeights = new Vector3(70f, 25f, 5f);

        private readonly List<Chest> chests = new List<Chest>();
        private PlayerStats stats;
        private GoldWallet gold;
        private CombatSystem combat;
        private float timer;

        public int Opened { get; private set; }
        public int CurrentCost => baseCost + costIncrease * Opened;
        public bool CanAfford => gold != null && gold.Gold >= CurrentCost;
        public IReadOnlyList<Chest> Chests => chests;

        public event Action<Vector3, Rarity> ChestOpened;

        private void Awake()
        {
            stats = player.GetComponent<PlayerStats>();
            gold = player.GetComponent<GoldWallet>();
            combat = player.GetComponent<CombatSystem>();
            timer = firstChestAfter;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = interval;
            if (chests.Count < maxChests) Spawn();
        }

        private void Spawn()
        {
            var candidates = new List<Island>();
            foreach (Island island in Island.All)
                if (island.Kind != IslandKind.Tower && island.Width >= minIslandWidth && chests.Find(c => c.Island == island) == null)
                    candidates.Add(island);
            if (candidates.Count == 0) return;

            Island chosen = candidates[Random.Range(0, candidates.Count)];
            Vector2 offset = Random.insideUnitCircle * chosen.Width * 0.2f;
            Chest chest = Instantiate(chestPrefab, chosen.Top + new Vector3(offset.x, 0f, offset.y),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            chest.Init(this, player, chosen);
            chests.Add(chest);
        }

        public void Open(Chest chest)
        {
            if (!gold.TrySpend(CurrentCost)) return;
            Opened++;
            Rarity rarity = Roll(rarityWeights, stats.LuckMultiplier);
            chests.Remove(chest);
            ChestOpened?.Invoke(chest.transform.position, rarity);
            Destroy(chest.gameObject);
            ChestResultUI.Offer(combat, stats, rarity);
        }

        /// <summary>Weighted roll; luck multiplies the Rare and Epic weights.</summary>
        public static Rarity Roll(Vector3 weights, float luck)
        {
            float common = weights.x, rare = weights.y * luck, epic = weights.z * luck;
            float roll = Random.value * (common + rare + epic);
            if (roll < epic) return Rarity.Epic;
            if (roll < epic + rare) return Rarity.Rare;
            return Rarity.Common;
        }
    }
}
