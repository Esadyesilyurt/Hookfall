// Hookfall - a normal chest on an island: stand next to it and press E to pay gold and open it (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using Hookfall.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.World
{
    public class Chest : MonoBehaviour
    {
        [SerializeField] private float openDistance = 3f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;
        [Tooltip("Optional lid that tilts open while the player is close.")]
        [SerializeField] private Transform lid;

        private ChestSpawner spawner;
        private PlayerController player;
        private Vector3 basePosition;

        public Island Island { get; private set; }

        public void Init(ChestSpawner owner, PlayerController target, Island island)
        {
            spawner = owner;
            player = target;
            Island = island;
            basePosition = transform.position;
        }

        private void Update()
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight + bobHeight);
            if (player == null || Time.timeScale <= 0f) return;

            bool close = !player.IsAirborne && Vector3.Distance(player.transform.position, basePosition) <= openDistance;
            if (lid != null) lid.localRotation = Quaternion.Slerp(lid.localRotation, Quaternion.Euler(close ? -25f : 0f, 0f, 0f), Time.deltaTime * 8f);
            if (!close) return;

            int cost = spawner.CurrentCost;
            bool canPay = spawner.CanAfford;
            HUD.InteractPrompt = canPay ? $"[E] Open chest  ·  {cost} gold" : $"<color=#F2403A>Chest  ·  {cost} gold needed</color>";
            if (canPay && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) spawner.Open(this);
        }
    }
}
