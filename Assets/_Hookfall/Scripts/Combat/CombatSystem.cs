// Hookfall - the core rule: items attack only while the player is airborne (GDD 4.4); also gives chest items (GDD 4.5, 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with chest rewards. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Combat
{
    public class CombatSystem : MonoBehaviour
    {
        public const int MaxItems = 6;

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
        [Tooltip("Starting level of a new item per rarity, and levels added to an owned one: Common, Rare, Epic.")]
        [SerializeField] private int[] levelsPerRarity = { 1, 2, 3 };

        private readonly List<ItemBase> items = new List<ItemBase>();
        private float airborneTime;
        private float groundedTime;

        public bool AttacksActive { get; private set; }
        public IReadOnlyList<ItemBase> Items => items;

        public event Action<bool> AttacksChanged;

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

            if (!AttacksActive) return;
            foreach (ItemBase item in items) item.Tick(Time.deltaTime);
        }

        public bool Owns(ItemBase prefab) => Find(prefab) != null;

        /// <summary>Opens a chest: a new item, or levels for an owned item when it rolls one you have or the slots are full.</summary>
        public ChestReward Grant(Rarity rarity, PlayerStats stats)
        {
            int levels = levelsPerRarity[Mathf.Clamp((int)rarity, 0, levelsPerRarity.Length - 1)];
            ItemBase rolled = itemPrefabs.Count > 0 ? itemPrefabs[UnityEngine.Random.Range(0, itemPrefabs.Count)] : null;

            if (rolled != null && !Owns(rolled) && items.Count < MaxItems)
            {
                ItemBase item = Instantiate(rolled, itemsRoot);
                item.name = rolled.name;
                item.SetLevel(levels);
                items.Add(item);
                item.SetActiveVisuals(AttacksActive);
                return new ChestReward(rarity, item, true, levels);
            }

            ItemBase owned = rolled != null ? Find(rolled) : null;
            if (owned == null || owned.IsMaxed) owned = RandomUnmaxedItem();
            if (owned != null)
            {
                int before = owned.Level;
                owned.SetLevel(before + levels);
                return new ChestReward(rarity, owned, false, owned.Level - before);
            }

            // Every owned item is maxed and the slots are full: give a stat instead.
            var stat = (StatType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(StatType)).Length);
            for (int i = 0; i < levels; i++) stats.Apply(stat);
            return new ChestReward(rarity, null, false, levels, stat);
        }

        private ItemBase Find(ItemBase prefab)
        {
            foreach (ItemBase item in items)
                if (item.GetType() == prefab.GetType()) return item;
            return null;
        }

        private ItemBase RandomUnmaxedItem()
        {
            var open = items.FindAll(i => !i.IsMaxed);
            return open.Count > 0 ? open[UnityEngine.Random.Range(0, open.Count)] : null;
        }

        private void SetAttacksActive(bool active)
        {
            AttacksActive = active;
            foreach (ItemBase item in items) item.SetActiveVisuals(active);
            AttacksChanged?.Invoke(active);
        }
    }
}
