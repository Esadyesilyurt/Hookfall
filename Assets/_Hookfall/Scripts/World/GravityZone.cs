// Hookfall - the Heartstone's low-gravity circle: gravity scale 0.7 within 40 m of the tower (GDD 7.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    public class GravityZone : MonoBehaviour
    {
        [SerializeField] private Rigidbody target;
        [SerializeField] private float radius = 40f;
        [SerializeField, Range(0f, 1f)] private float gravityScale = 0.7f;

        public bool TargetInside { get; private set; }

        private void FixedUpdate()
        {
            if (target == null) return;
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;   // a vertical cylinder around the tower
            TargetInside = offset.sqrMagnitude <= radius * radius;
            if (TargetInside) target.AddForce(-Physics.gravity * (1f - gravityScale), ForceMode.Acceleration);
        }
    }
}
