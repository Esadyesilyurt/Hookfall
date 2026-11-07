// Hookfall - procedural animation for the Warden model's parts (no rig): faces the direction of travel, leans into
// the swing, reaches the hook arm towards the anchor, walks on islands, trails the legs and flutters the cape in flight.
// Flying fast with no rope out, the Warden does tricks: front flips, back flips, barrel rolls (added 2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Player
{
    public class WardenAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private GrappleHook hook;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform hookArm;
        [SerializeField] private Transform otherArm;
        [SerializeField] private Transform legLeft;
        [SerializeField] private Transform legRight;
        [SerializeField] private Transform cape;
        [SerializeField] private Transform scarf;
        [SerializeField] private Transform head;

        [SerializeField] private float turnSpeed = 10f;
        [SerializeField] private float walkCycleSpeed = 9f;
        [SerializeField] private float walkSwing = 32f;
        [SerializeField] private float maxLean = 35f;

        [Header("Air tricks")]
        [Tooltip("Tricks start above this speed, in the air, with no rope out.")]
        [SerializeField] private float trickSpeed = 24f;
        [SerializeField] private float trickStartDelay = 0.2f;
        [SerializeField] private float trickTurnRate = 620f;
        [Tooltip("Distance from the model's feet to its middle, the point it spins around.")]
        [SerializeField] private float pivotHeight = 1f;

        private float walkPhase;
        private float lean;
        private Quaternion bodyRotation = Quaternion.identity;
        private float trickAngle;
        private float trickTimer;
        private Vector3 trickAxis = Vector3.right;
        private bool tricking;

        /// <summary>Raised when a trick starts, for effects and sound.</summary>
        public event System.Action TrickStarted;

        private void LateUpdate()
        {
            if (controller == null) return;
            float dt = Time.deltaTime;
            Vector3 velocity = controller.Body.linearVelocity;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            bool airborne = controller.IsAirborne;

            // Facing: direction of travel; standing still, the camera's forward.
            Vector3 face = flat.sqrMagnitude > 1f ? flat : cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up) : transform.forward;
            if (face.sqrMagnitude > 0.001f)
            {
                Quaternion yaw = Quaternion.LookRotation(face.normalized);
                float targetLean = airborne ? Mathf.Clamp(flat.magnitude * 1.2f, 0f, maxLean) : 0f;
                lean = Mathf.Lerp(lean, targetLean, dt * 6f);
                bodyRotation = Quaternion.Slerp(bodyRotation, yaw * Quaternion.Euler(lean, 0f, 0f), dt * turnSpeed);
            }
            UpdateTrick(dt, airborne, velocity.magnitude);
            // Spin around the middle of the body, not the feet: the model's origin is at its feet.
            Quaternion spin = bodyRotation * Quaternion.AngleAxis(trickAngle, trickAxis);
            transform.rotation = spin;
            transform.localPosition = -(spin * Vector3.up) * pivotHeight;

            // Legs: walk cycle on islands, trailing behind in the air.
            float walk = !airborne ? Mathf.Clamp01(flat.magnitude / 7f) : 0f;
            walkPhase += dt * walkCycleSpeed * Mathf.Max(walk, 0.0001f);
            float swing = Mathf.Sin(walkPhase) * walkSwing * walk;
            float trail = airborne ? 25f : 0f;
            SetX(legLeft, swing + trail, dt);
            SetX(legRight, -swing + trail * 0.6f, dt);

            // Arms: the hook arm points at the anchor while the hook is out; otherwise they swing with the walk.
            if (hookArm != null)
            {
                if (hook != null && hook.IsOut)
                {
                    Vector3 dir = hook.AnchorPoint - hookArm.position;
                    if (dir.sqrMagnitude > 0.01f)
                        hookArm.rotation = Quaternion.Slerp(hookArm.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(-90f, 0f, 0f), dt * 20f);
                }
                else SetX(hookArm, -swing * 0.8f + (airborne ? -20f : 0f), dt);
            }
            SetX(otherArm, swing * 0.8f + (airborne ? -50f : 0f), dt);

            // Cape: lifts with speed and flutters.
            if (cape != null)
            {
                float speed = velocity.magnitude;
                float angle = -Mathf.Clamp(8f + speed * 2.2f, 0f, 75f) + Mathf.Sin(Time.time * (6f + speed * 0.3f)) * Mathf.Min(10f, speed * 0.5f);
                SetX(cape, angle, dt);
                SetX(scarf, angle * 0.8f + Mathf.Sin(Time.time * 11f) * Mathf.Min(14f, speed), dt);
            }
            if (head != null && airborne) SetX(head, -lean * 0.5f, dt);
            else SetX(head, 0f, dt);
        }

        private void UpdateTrick(float dt, bool airborne, float speed)
        {
            bool canTrick = airborne && speed >= trickSpeed && (hook == null || !hook.IsOut);
            trickTimer = canTrick ? trickTimer + dt : 0f;
            if (!tricking && trickTimer >= trickStartDelay)
            {
                tricking = true;
                int kind = Random.Range(0, 3);   // front flip, back flip, barrel roll
                trickAxis = kind == 0 ? Vector3.right : kind == 1 ? Vector3.left : Vector3.forward;
                TrickStarted?.Invoke();
            }
            if (tricking)
            {
                trickAngle += trickTurnRate * dt;
                // Stop only at a whole turn, so the Warden never snaps out of a flip.
                if (!canTrick && trickAngle >= 360f) tricking = false;
                if (trickAngle >= 360f) trickAngle -= 360f;
                if (!tricking) trickAngle = 0f;
            }
            else if (trickAngle != 0f)
            {
                trickAngle = 0f;
            }
        }

        private static void SetX(Transform t, float angle, float dt)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(angle, 0f, 0f), dt * 12f);
        }
    }
}
