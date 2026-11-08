// Hookfall - a normal chest on an island: come within reach, standing or flying, and press E to pay gold and open it (GDD 4.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; reach widened and opening from the air allowed on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using Hookfall.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.World
{
    public class Chest : MonoBehaviour
    {
        [Tooltip("3 m and grounded only until 2026-11-08; the chest can now be opened in flight too.")]
        [SerializeField] private float openDistance = 7f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;
        [Tooltip("Optional lid that tilts open while the player is close.")]
        [SerializeField] private Transform lid;
        [Tooltip("The spotlight above the chest; it breathes slowly so the eye catches it.")]
        [SerializeField] private Light spot;

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
            if (spot != null) spot.intensity = 9f * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.5f));
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight + bobHeight);
            if (player == null || Time.timeScale <= 0f) return;

            bool close = Vector3.Distance(player.transform.position, basePosition) <= openDistance;
            if (lid != null) lid.localRotation = Quaternion.Slerp(lid.localRotation, Quaternion.Euler(close ? -25f : 0f, 0f, 0f), Time.deltaTime * 8f);
            if (!close) return;

            int cost = spawner.CurrentCost;
            bool canPay = spawner.CanAfford;
            HUD.InteractPrompt = canPay ? Hookfall.Core.Loc.F("Open chest  ·  {0} gold", cost) : "<color=#F2403A>" + Hookfall.Core.Loc.F("Chest  ·  {0} gold needed", cost) + "</color>";
            if (canPay && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) spawner.Open(this);
        }
    }
}
