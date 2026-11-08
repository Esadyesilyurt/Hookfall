// Hookfall - places a chest on an island every 30 s (max 7; GDD: 45 s, max 3), charges 15 gold +10 per chest opened,
// and rolls rarity: Common 70%, Rare 25%, Epic 5%, shifted by Luck (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; on 2026-11-09 more chests, placed within reach of the
// Warden and never on large islands. Prompts and changes are recorded in Docs/PromptLog.md.
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
        [SerializeField] private float interval = 30f;   // 45 until 2026-11-09
        [SerializeField] private int maxChests = 7;   // 3 until the map doubled (2026-11-08), 4 until 2026-11-09
        [Tooltip("Chests appear on islands at least this wide (tiny hook points have no room).")]
        [SerializeField] private float minIslandWidth = 5f;
        [Tooltip("The first chest appears this many seconds into the stage (20 until 2026-11-09).")]
        [SerializeField] private float firstChestAfter = 15f;
        [Tooltip("A new chest goes on an island this far from the Warden (min, max) when one is free, so it can be " +
                 "found on the big map; otherwise anywhere.")]
        [SerializeField] private Vector2 nearRange = new Vector2(30f, 170f);

        [Header("Cost")]
        [SerializeField] private int baseCost = 15;
        [SerializeField] private int costIncrease = 10;

        [Header("Rarity weights (Common, Rare, Epic, Legendary); Luck multiplies all but Common")]
        [SerializeField] private Vector4 rarityWeights = new Vector4(60f, 28f, 10f, 2f);

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
            var near = new List<Island>();
            Vector3 p = player.transform.position;
            foreach (Island island in Island.All)
            {
                // Never on the tower, the boss's islands or the large islands, which stay empty.
                if (island.Kind == IslandKind.Tower || island.Kind == IslandKind.Boss || island.Kind == IslandKind.Large) continue;
                if (island.Width < minIslandWidth || chests.Find(c => c.Island == island) != null) continue;
                candidates.Add(island);
                float d = Vector3.Distance(p, island.Top);
                if (d >= nearRange.x && d <= nearRange.y) near.Add(island);
            }
            if (near.Count > 0) candidates = near;
            if (candidates.Count == 0) return;

            Island chosen = candidates[Random.Range(0, candidates.Count)];
            Vector2 offset = Random.insideUnitCircle * chosen.Width * 0.2f;
            Chest chest = Instantiate(chestPrefab, chosen.Top + new Vector3(offset.x, 0f, offset.y),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            chest.Init(this, player, chosen);
            chests.Add(chest);
        }

        /// <summary>A new stage: the old chests go with the old map; the first new one comes after the usual wait.
        /// The price keeps rising with every chest opened in the run.</summary>
        public void ResetForStage()
        {
            foreach (Chest chest in chests) if (chest != null) Destroy(chest.gameObject);
            chests.Clear();
            timer = firstChestAfter;
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

        /// <summary>Weighted roll; luck multiplies the Rare, Epic and Legendary weights.</summary>
        public static Rarity Roll(Vector4 weights, float luck)
        {
            float common = weights.x, rare = weights.y * luck, epic = weights.z * luck, legendary = weights.w * luck;
            float roll = Random.value * (common + rare + epic + legendary);
            if (roll < legendary) return Rarity.Legendary;
            if (roll < legendary + epic) return Rarity.Epic;
            if (roll < legendary + epic + rare) return Rarity.Rare;
            return Rarity.Common;
        }
    }
}
