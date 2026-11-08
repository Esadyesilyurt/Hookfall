// Hookfall - the core rule: items attack only while the player is airborne (GDD 4.4); also gives chest items (GDD 4.5, 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with chest rewards and on 2026-11-08 with the 3-choice chest, item rarity and passive slots. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public class CombatSystem : MonoBehaviour
    {
        public const int MaxItems = 6;
        public const int MaxPassives = 6;

        [SerializeField] private PlayerController player;
        [Tooltip("Airborne time before attacks switch on.")]
        [SerializeField] private float enableDelay = 0.15f;
        [Tooltip("Grounded time before attacks switch off, so a brief touch does not flicker combat.")]
        [SerializeField] private float disableDelay = 0.3f;

        [Header("Chest items")]
        [Tooltip("Parent of the owned items (a child of the player).")]
        [SerializeField] private Transform itemsRoot;
        [Tooltip("Every item a chest can give, as prefabs.")]
        [SerializeField] private List<ItemBase> itemPrefabs = new List<ItemBase>();
        [Tooltip("How many different choices a chest offers.")]
        [SerializeField] private int chestChoices = 3;

        private readonly List<ItemBase> items = new List<ItemBase>();
        private float airborneTime;
        private float groundedTime;

        public bool AttacksActive { get; private set; }
        public IReadOnlyList<ItemBase> Items => items;

        public event Action<bool> AttacksChanged;
        /// <summary>An item was gained or the starting items were set (SynergySystem recounts).</summary>
        public event Action ItemsChanged;

        /// <summary>How many choices a chest offers (the Fortune set raises it to 4).</summary>
        public void SetChestChoices(int count) => chestChoices = Mathf.Max(chestChoices, count);

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
            if (itemsRoot == null) itemsRoot = transform;
            GetComponentsInChildren(true, items);
        }

        private void Start()
        {
            // In Start, not Awake, so every item has built its visuals before they are hidden.
            foreach (ItemBase item in items) item.SetActiveVisuals(false);
        }

        private void Update()
        {
            if (player == null) return;

            if (player.IsAirborne)
            {
                airborneTime += Time.deltaTime;
                groundedTime = 0f;
                if (!AttacksActive && airborneTime >= enableDelay) SetAttacksActive(true);
            }
            else
            {
                groundedTime += Time.deltaTime;
                airborneTime = 0f;
                if (AttacksActive && groundedTime >= disableDelay) SetAttacksActive(false);
            }

            foreach (ItemBase item in items)
                if (item.IsPassive || AttacksActive) item.Tick(Time.deltaTime);
        }

        public bool Owns(ItemBase prefab) => Find(prefab) != null;

        /// <summary>Opens a chest with no choice: the first of its rolled choices.</summary>
        public ChestReward Grant(Rarity rarity, PlayerStats stats) => Claim(RollChest(rarity)[0], stats);

        /// <summary>
        /// A chest's different choices, nothing given yet. A rarer chest offers rarer items, never more levels: new items
        /// of the chest's own rarity first, then of the rarities below it; then one more level for owned items that are not
        /// maxed; stats only fill what is left. Never empty.
        /// </summary>
        public List<ChestReward> RollChest(Rarity rarity)
        {
            var offer = new List<ChestReward>();
            var pool = new List<ChestReward>();
            for (int tier = (int)rarity; tier >= 0 && offer.Count < chestChoices; tier--)
            {
                pool.Clear();
                foreach (ItemBase prefab in itemPrefabs)
                    if ((int)prefab.Rarity == tier && Find(prefab) == null && HasFreeSlot(prefab.IsPassive))
                        pool.Add(new ChestReward(prefab.Rarity, prefab, true, 1));
                TakeRandom(pool, offer);
            }
            pool.Clear();
            foreach (ItemBase item in items)
                if (!item.IsMaxed) pool.Add(new ChestReward(item.Rarity, item, false, 1));
            TakeRandom(pool, offer);
            pool.Clear();
            foreach (StatType stat in Enum.GetValues(typeof(StatType))) pool.Add(new ChestReward(rarity, null, false, 1, stat));
            TakeRandom(pool, offer);
            return offer;
        }

        /// <summary>Replaces the items the run starts with by a character's own (CharacterLoadout, at run start).</summary>
        public void SetStartingItems(ItemBase[] prefabs)
        {
            foreach (ItemBase item in items) Destroy(item.gameObject);
            items.Clear();
            foreach (ItemBase prefab in prefabs)
                if (prefab != null) Claim(new ChestReward(prefab.Rarity, prefab, true, 1), null);
            ItemsChanged?.Invoke();
        }

        public bool HasFreeSlot(bool passive)
        {
            int used = 0;
            foreach (ItemBase item in items) if (item.IsPassive == passive) used++;
            return used < (passive ? MaxPassives : MaxItems);
        }

        private void TakeRandom(List<ChestReward> pool, List<ChestReward> offer)
        {
            while (offer.Count < chestChoices && pool.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, pool.Count);
                offer.Add(pool[i]);
                pool.RemoveAt(i);
            }
        }

        /// <summary>Gives one choice from RollChest and returns what it became (the new item's instance, the levels really gained).</summary>
        public ChestReward Claim(ChestReward choice, PlayerStats stats)
        {
            if (choice.IsNewItem)
            {
                ItemBase item = Instantiate(choice.Item, itemsRoot);
                item.name = choice.Item.name;
                item.SetLevel(choice.LevelsGained);
                items.Add(item);
                item.SetActiveVisuals(AttacksActive);
                ItemsChanged?.Invoke();
                return new ChestReward(choice.Rarity, item, true, choice.LevelsGained);
            }
            if (choice.Item != null)
            {
                int before = choice.Item.Level;
                choice.Item.SetLevel(before + choice.LevelsGained);
                return new ChestReward(choice.Rarity, choice.Item, false, choice.Item.Level - before);
            }
            for (int i = 0; i < choice.LevelsGained; i++) stats.Apply(choice.FallbackStat);
            return choice;
        }

        private ItemBase Find(ItemBase prefab)
        {
            foreach (ItemBase item in items)
                if (item.name == prefab.name) return item;   // by prefab, not by class: several items share a class
            return null;
        }

        private void SetAttacksActive(bool active)
        {
            AttacksActive = active;
            foreach (ItemBase item in items) item.SetActiveVisuals(active);
            AttacksChanged?.Invoke(active);
        }
    }
}
