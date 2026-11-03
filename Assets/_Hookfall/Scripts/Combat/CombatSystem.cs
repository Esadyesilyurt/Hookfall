// Hookfall - the core rule: items attack only while the player is airborne (GDD 4.4).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
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

        private readonly List<ItemBase> items = new List<ItemBase>();
        private float airborneTime;
        private float groundedTime;

        public bool AttacksActive { get; private set; }
        public IReadOnlyList<ItemBase> Items => items;

        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerController>();
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

        /// <summary>Adds a new item from a chest. Returns false when all slots are full.</summary>
        public bool TryAddItem(ItemBase item)
        {
            if (item == null || items.Count >= MaxItems || items.Contains(item)) return false;
            items.Add(item);
            item.SetActiveVisuals(AttacksActive);
            return true;
        }

        private void SetAttacksActive(bool active)
        {
            AttacksActive = active;
            foreach (ItemBase item in items) item.SetActiveVisuals(active);
        }
    }
}
