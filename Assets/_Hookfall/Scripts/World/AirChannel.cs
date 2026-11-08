// Hookfall - an air channel: a straight wind tunnel from one island's rim to another's. Inside it the Warden is steered
// along the channel at its speed and pulled towards its middle, then flies on with that momentum. Placed by SkyFeatures.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class AirChannel : MonoBehaviour
    {
        private const float Steering = 6f;      // how fast the velocity turns into the channel's (per second)
        private const float Centring = 4f;      // pull towards the middle, per metre off it
        private const float MaxAcceleration = 90f;

        private PlayerController player;
        private SkyFeatures owner;
        private float radius, speed;
        private bool inside;

        public Vector3 Entry { get; private set; }
        public Vector3 Exit { get; private set; }
        public float Speed => speed;

        public void Init(SkyFeatures features, PlayerController target, Vector3 from, Vector3 to, float tunnelRadius, float tunnelSpeed)
        {
            owner = features;
            player = target;
            Entry = from;
            Exit = to;
            radius = tunnelRadius;
            speed = tunnelSpeed;
        }

        private void FixedUpdate()
        {
            if (player == null) return;
            Rigidbody body = player.Body;
            Vector3 along = Exit - Entry;
            float length = along.magnitude;
            Vector3 dir = along / length;
            float t = Vector3.Dot(body.position - Entry, dir);
            Vector3 toAxis = Entry + dir * t - body.position;
            if (t < 0f || t > length || toAxis.sqrMagnitude > radius * radius)
            {
                inside = false;
                return;
            }
            if (!inside)
            {
                inside = true;
                player.Boost(speed);
                owner.ChannelEntered(body.position);
            }
            Vector3 desired = dir * speed + toAxis * Centring;
            Vector3 accel = Vector3.ClampMagnitude((desired - body.linearVelocity) * Steering, MaxAcceleration);
            body.AddForce(accel - Physics.gravity, ForceMode.Acceleration);   // the channel carries the Warden's weight
        }
    }
}
