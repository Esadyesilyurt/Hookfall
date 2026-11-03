// Hookfall - island movement, air strafe and the IsAirborne flag the whole game reads (GDD 4.1, 4.2).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("On islands")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float jumpVelocity = 6f;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("In the air")]
        [Tooltip("Light sideways push with A/D; it cannot reverse momentum.")]
        [SerializeField] private float airStrafeAcceleration = 6f;
        [SerializeField] private float maxAirSpeed = 35f;

        [SerializeField] private Transform cameraTransform;

        private CapsuleCollider capsule;
        private Vector2 moveInput;
        private bool jumpQueued;

        /// <summary>True when the player is not standing on an island. Turns combat on and enemy Frenzy off.</summary>
        public bool IsAirborne { get; private set; } = true;
        public Rigidbody Body { get; private set; }

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            Body.freezeRotation = true;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            moveInput = new Vector2(x, y);

            if (keyboard.spaceKey.wasPressedThisFrame && !IsAirborne) jumpQueued = true;
        }

        private void FixedUpdate()
        {
            IsAirborne = !IsOnIsland();

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(right, Vector3.up).normalized;

            if (!IsAirborne)
            {
                Vector3 target = (right * moveInput.x + forward * moveInput.y) * walkSpeed;
                Vector3 velocity = Body.linearVelocity;
                velocity.x = target.x;
                velocity.z = target.z;
                if (jumpQueued) velocity.y = jumpVelocity;
                Body.linearVelocity = velocity;
            }
            else
            {
                // Air strafe: only A/D, only a gentle push. Momentum from the swing stays in charge.
                if (!Mathf.Approximately(moveInput.x, 0f))
                    Body.AddForce(right * (moveInput.x * airStrafeAcceleration), ForceMode.Acceleration);

                if (Body.linearVelocity.sqrMagnitude > maxAirSpeed * maxAirSpeed)
                    Body.linearVelocity = Body.linearVelocity.normalized * maxAirSpeed;
            }
            jumpQueued = false;
        }

        private bool IsOnIsland()
        {
            Bounds bounds = capsule.bounds;
            float radius = capsule.radius * 0.95f;
            float distance = bounds.extents.y - radius + groundCheckDistance;

            if (Physics.SphereCast(bounds.center, radius, Vector3.down, out RaycastHit hit, distance,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<Island>() != null;
            }
            return false;
        }
    }
}
