// Hookfall - an updraft: a vertical column of rising wind between or beside islands. Flying into it lifts the Warden,
// fading out near the top so it bobs there instead of shooting past. Placed by SkyFeatures.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class Updraft : MonoBehaviour
    {
        private PlayerController player;
        private float radius, height, lift, maxRise;
        private const float FadeHeight = 6f;   // the lift fades out over the column's top 6 m

        /// <summary>Centre of the column's base.</summary>
        public Vector3 Bottom => transform.position;
        public float Height => height;

        public void Init(PlayerController target, float columnRadius, float columnHeight, float liftAcceleration, float maxRiseSpeed)
        {
            player = target;
            radius = columnRadius;
            height = columnHeight;
            lift = liftAcceleration;
            maxRise = maxRiseSpeed;
        }

        private void FixedUpdate()
        {
            if (player == null || !player.IsAirborne) return;
            Rigidbody body = player.Body;
            Vector3 offset = body.position - Bottom;
            float h = offset.y;
            offset.y = 0f;
            if (h < 0f || h > height || offset.sqrMagnitude > radius * radius) return;
            if (body.linearVelocity.y >= maxRise) return;
            float fade = Mathf.Clamp01((height - h) / FadeHeight);
            body.AddForce(Vector3.up * (lift * fade), ForceMode.Acceleration);
        }
    }
}
