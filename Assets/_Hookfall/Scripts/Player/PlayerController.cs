// Hookfall - island movement, air strafe and the IsAirborne flag the whole game reads (GDD 4.1, 4.2).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (stat multipliers, current island, Zip overspeed, landing event) and 2026-11-08
// (heavier, faster air movement and the wall jump). Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Tooltip("Sideways push with A/D; momentum from the swing stays in charge.")]
        [SerializeField] private float airStrafeAcceleration = 12f;
        [Tooltip("Raised from the GDD's 35 m/s on 2026-11-08 so swings can build real speed.")]
        [SerializeField] private float maxAirSpeed = 55f;
        [Tooltip("Gravity multiplier in the air: a heavier Warden swings faster down and slows harder going up.")]
        [SerializeField] private float airGravityMultiplier = 1.8f;
        [Tooltip("After a Zip the speed cap drops back to the normal max air speed at this rate (m/s per second).")]
        [SerializeField] private float overspeedDecay = 18f;

        [Header("Wall jump (Space while touching an island side)")]
        [SerializeField] private float wallJumpUp = 14f;
        [SerializeField] private float wallJumpOut = 8f;
        [Tooltip("Extra speed towards the direction held on W/A/S/D.")]
        [SerializeField] private float wallJumpSteer = 9f;
        [Tooltip("How long after touching a wall Space still counts.")]
        [SerializeField] private float wallJumpGrace = 0.2f;
        [SerializeField] private float wallJumpCooldown = 0.3f;

        [SerializeField] private Transform cameraTransform;

        private CapsuleCollider capsule;
        private PlayerStats stats;
        private Vector2 moveInput;
        private bool jumpQueued;
        private float overspeedCap;
        private bool wallJumpQueued;
        private Vector3 wallNormal;
        private float lastWallTime = -999f;
        private float lastWallJump = -999f;

        /// <summary>True when the player is not standing on an island. Turns combat on and enemy Frenzy off.</summary>
        public bool IsAirborne { get; private set; } = true;
        /// <summary>The island the player stands on, or null while airborne.</summary>
        public Island CurrentIsland { get; private set; }
        public Rigidbody Body { get; private set; }
        public float Speed => Body.linearVelocity.magnitude;
        public float MaxAirSpeed => maxAirSpeed * (stats != null ? stats.FlightSpeedMultiplier : 1f);
        /// <summary>Vertical speed of the last landing (positive), for landing effects.</summary>
        public event System.Action<float> Landed;
        public event System.Action WallJumped;
        /// <summary>True for a moment after touching an island side, while a wall jump is possible.</summary>
        public bool TouchingWall => Time.time - lastWallTime < wallJumpGrace;

        /// <summary>Lets the player fly faster than the air speed cap for a moment (Zip); the cap decays back.</summary>
        public void Boost(float speed) => overspeedCap = Mathf.Max(overspeedCap, speed);

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            stats = GetComponent<PlayerStats>();
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

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (!IsAirborne) jumpQueued = true;
                else if (TouchingWall && Time.time - lastWallJump >= wallJumpCooldown) wallJumpQueued = true;
            }
        }

        private void FixedUpdate()
        {
            bool wasAirborne = IsAirborne;
            float fallSpeed = -Body.linearVelocity.y;
            CurrentIsland = FindIslandBelow();
            IsAirborne = CurrentIsland == null;
            if (wasAirborne && !IsAirborne) Landed?.Invoke(fallSpeed);
            overspeedCap = Mathf.MoveTowards(overspeedCap, 0f, overspeedDecay * Time.fixedDeltaTime);

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(right, Vector3.up).normalized;

            if (!IsAirborne)
            {
                float speed = walkSpeed * (stats != null ? stats.WalkSpeedMultiplier : 1f);
                Vector3 target = (right * moveInput.x + forward * moveInput.y) * speed;
                Vector3 velocity = Body.linearVelocity;
                velocity.x = target.x;
                velocity.z = target.z;
                if (jumpQueued) velocity.y = jumpVelocity;
                Body.linearVelocity = velocity;
            }
            else if (wallJumpQueued)
            {
                // Off the wall, up, and towards whatever direction is held.
                Vector3 steer = right * moveInput.x + forward * moveInput.y;
                Body.linearVelocity = wallNormal * wallJumpOut + Vector3.up * wallJumpUp + steer.normalized * (steer.sqrMagnitude > 0.01f ? wallJumpSteer : 0f);
                lastWallJump = Time.time;
                WallJumped?.Invoke();
            }
            else
            {
                Body.AddForce(Physics.gravity * (airGravityMultiplier - 1f), ForceMode.Acceleration);

                // Air strafe: only A/D. Momentum from the swing stays in charge.
                if (!Mathf.Approximately(moveInput.x, 0f))
                    Body.AddForce(right * (moveInput.x * airStrafeAcceleration), ForceMode.Acceleration);

                float cap = Mathf.Max(MaxAirSpeed, overspeedCap);
                if (Body.linearVelocity.sqrMagnitude > cap * cap)
                    Body.linearVelocity = Body.linearVelocity.normalized * cap;
            }
            jumpQueued = false;
            wallJumpQueued = false;
        }

        // Island sides (steep contacts) arm the wall jump.
        private void OnCollisionStay(Collision collision)
        {
            if (!IsAirborne || collision.collider.GetComponentInParent<Island>() == null) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 n = collision.GetContact(i).normal;
                if (Mathf.Abs(n.y) > 0.55f) continue;
                wallNormal = new Vector3(n.x, 0f, n.z).normalized;
                lastWallTime = Time.time;
                return;
            }
        }

        private Island FindIslandBelow()
        {
            Bounds bounds = capsule.bounds;
            float radius = capsule.radius * 0.95f;
            float distance = bounds.extents.y - radius + groundCheckDistance;

            if (Physics.SphereCast(bounds.center, radius, Vector3.down, out RaycastHit hit, distance,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<Island>();
            }
            return null;
        }
    }
}
