// Hookfall - an XP gem or gold coin left by a dead enemy; pulled in once the player is inside the pickup radius (GDD 4.3, 4.7).
// Since 2026-11-08 coins fly to the Warden on their own a moment after they drop, and gems within ten pickup radii
// follow after a short wait: long-range kills (the Ember Witch's salamander) left their loot out of reach.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Progression
{
    public class Pickup : MonoBehaviour
    {
        [SerializeField] private PickupKind kind = PickupKind.XP;
        [SerializeField] private float magnetAcceleration = 80f;
        [SerializeField] private float collectDistance = 1.2f;
        [SerializeField] private float spinSpeed = 120f;
        [Tooltip("Seconds before an uncollected pickup disappears, so the sky does not fill up.")]
        [SerializeField] private float lifetime = 90f;
        [Tooltip("Coins fly to the Warden on their own after this many seconds, from anywhere.")]
        [SerializeField] private float goldHomeDelay = 0.5f;
        [Tooltip("Gems follow the Warden after this many seconds when they lie within homeRadii pickup radii.")]
        [SerializeField] private float xpHomeDelay = 1.5f;
        [SerializeField] private float homeRadii = 10f;

        private PickupSpawner spawner;
        private Transform player;
        private float value;
        private float age;
        private float speed;
        private bool magnetised;

        public PickupKind Kind => kind;

        /// <summary>Starts flying to the Warden from wherever it is (Treasure Gale).</summary>
        public void Magnetise() => magnetised = true;

        public void Init(PickupSpawner owner, Transform target, float amount)
        {
            spawner = owner;
            player = target;
            value = amount;
            age = 0f;
            speed = 0f;
            magnetised = false;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            age += Time.deltaTime;
            if (age > lifetime) { spawner.Release(this); return; }
            if (player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            if (!magnetised && (distance <= spawner.PickupRadius
                || (kind == PickupKind.Gold && age >= goldHomeDelay)
                || (kind == PickupKind.XP && age >= xpHomeDelay && distance <= spawner.PickupRadius * homeRadii))) magnetised = true;
            if (!magnetised) return;

            if (distance <= collectDistance)
            {
                spawner.Collect(this, value);
                return;
            }
            speed += magnetAcceleration * Time.deltaTime;
            transform.position += toPlayer / distance * Mathf.Min(speed * Time.deltaTime, distance);
        }
    }
}
