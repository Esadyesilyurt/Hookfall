// Hookfall - a refill crystal floating in open air: touching it in flight refills the double jump and the dash without
// landing. It is used only when something was spent, then grows back after a few seconds. Placed by SkyFeatures.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class RefillCrystal : MonoBehaviour
    {
        private PlayerController player;
        private SkyFeatures owner;
        private Transform visual;
        private float radius, respawn, timer;
        private Vector3 basePosition;

        public bool Ready { get; private set; } = true;

        public void Init(SkyFeatures features, PlayerController target, Transform crystal, float touchRadius, float respawnTime)
        {
            owner = features;
            player = target;
            visual = crystal;
            radius = touchRadius;
            respawn = respawnTime;
            basePosition = transform.position;
        }

        private void Update()
        {
            if (player == null) return;
            if (!Ready)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f) SetReady(true);
                return;
            }
            visual.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 1.6f + basePosition.x) * 0.35f);

            if (!player.IsAirborne || (player.Body.position - transform.position).sqrMagnitude > radius * radius) return;
            if (!player.RefillAirMoves()) return;   // nothing spent: leave it for later
            SetReady(false);
            owner.CrystalTaken(transform.position);
        }

        private void SetReady(bool ready)
        {
            Ready = ready;
            timer = respawn;
            visual.gameObject.SetActive(ready);
        }
    }
}
