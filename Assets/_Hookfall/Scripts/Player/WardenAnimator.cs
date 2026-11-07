// Hookfall - procedural animation for the Warden model's parts (no rig): faces the direction of travel, leans into
// the swing, reaches the hook arm towards the anchor, walks on islands, trails the legs and flutters the cape in flight.
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

        private float walkPhase;
        private float lean;

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
                transform.rotation = Quaternion.Slerp(transform.rotation, yaw * Quaternion.Euler(lean, 0f, 0f), dt * turnSpeed);
            }

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

        private static void SetX(Transform t, float angle, float dt)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(angle, 0f, 0f), dt * 12f);
        }
    }
}
