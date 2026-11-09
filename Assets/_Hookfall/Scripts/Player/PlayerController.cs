// Hookfall - island movement, air strafe and the IsAirborne flag the whole game reads (GDD 4.1, 4.2).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (stat multipliers, current island, Zip overspeed, landing event) and 2026-11-08
// (heavier, faster air movement, the wall jump, the double jump and the dash, the ledge climb and a hard speed cap). Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Tooltip("Sideways acceleration with A/D in the air (12 until 2026-11-08). W/S work the rope instead (GrappleHook).")]
        [SerializeField] private float airStrafeAcceleration = 40f;
        [Tooltip("A/D push up to this sideways speed; faster momentum is kept, and the opposite key brakes it quickly.")]
        [SerializeField] private float airStrafeSpeed = 16f;
        [Tooltip("Air speed cap before Flight speed picks (GDD 35 m/s; 55 for a while on 2026-11-08, lowered to 40 the same day because chained swings got too fast).")]
        [SerializeField] private float maxAirSpeed = 40f;
        [Tooltip("Gravity multiplier in the air: a heavier Warden swings faster down and slows harder going up.")]
        [SerializeField] private float airGravityMultiplier = 1.8f;
        [Tooltip("After a Zip the speed cap drops back to the normal max air speed at this rate (m/s per second).")]
        [SerializeField] private float overspeedDecay = 18f;
        [Tooltip("Zips and rope releases may push past the cap by at most this factor, for a moment.")]
        [SerializeField] private float maxOverspeedFactor = 1.15f;

        [Header("Ledge climb (touching an island's side or rim near its top moves the Warden over the edge)")]
        [Tooltip("How far below an island's top the Warden's feet can be for the climb.")]
        [SerializeField] private float ledgeReach = 8f;
        [Tooltip("How high above the island top the climb carries the Warden over the rim.")]
        [SerializeField] private float ledgeClearance = 1.2f;
        [Tooltip("How long the climb takes: out past the rim and up, then in onto the top.")]
        [SerializeField] private float ledgeClimbTime = 0.35f;
        [Tooltip("How far inside the rim the climb lands.")]
        [SerializeField] private float ledgeLandInset = 1.5f;

        [Header("Wall jump (Space while touching an island side)")]
        [SerializeField] private float wallJumpUp = 14f;
        [SerializeField] private float wallJumpOut = 8f;
        [Tooltip("Extra speed towards the direction held on W/A/S/D.")]
        [SerializeField] private float wallJumpSteer = 9f;
        [Tooltip("How long after touching a wall Space still counts.")]
        [SerializeField] private float wallJumpGrace = 0.2f;
        [SerializeField] private float wallJumpCooldown = 0.3f;

        [Header("Double jump (Space in the air; refilled on landing)")]
        [SerializeField] private int airJumps = 1;
        [Tooltip("Added to the upward speed (falling counts as zero).")]
        [SerializeField] private float airJumpVelocity = 11f;

        [Header("Dash (Left Shift in the air; refilled on landing)")]
        [SerializeField] private int airDashes = 1;
        [Tooltip("Dash speed; a faster Warden keeps its speed and only turns.")]
        [SerializeField] private float dashSpeed = 32f;
        [Tooltip("How long the dash holds a straight line before gravity takes over again.")]
        [SerializeField] private float dashDuration = 0.18f;

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
        private int airJumpsLeft;
        private int dashesLeft;
        private bool airJumpQueued;
        private bool dashQueued;
        private Vector3 dashVelocity;
        private float dashTimer;
        private Island ledgeIsland;
        private float climbT = -1f;   // 0..1 while climbing, -1 otherwise
        private Vector3 climbFrom, climbOver, climbTo;

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
        public event System.Action AirJumped;
        public event System.Action Dashed;
        public event System.Action LedgeClimbed;
        public int AirJumpsLeft => airJumpsLeft;
        public int DashesLeft => dashesLeft;
        /// <summary>True for a moment after touching an island side, while a wall jump is possible.</summary>
        public bool TouchingWall => Time.time - lastWallTime < wallJumpGrace;

        /// <summary>The character's air moves per flight (CharacterLoadout).</summary>
        public void SetAirMoves(int jumps, int dashes)
        {
            airJumps = jumps;
            airDashes = dashes;
        }

        /// <summary>Refills the double jump and the dash in mid-air (refill crystals). False when neither was spent.</summary>
        public bool RefillAirMoves()
        {
            if (airJumpsLeft >= airJumps && dashesLeft >= airDashes) return false;
            airJumpsLeft = airJumps;
            dashesLeft = airDashes;
            return true;
        }

        /// <summary>Lets the player fly faster than the air speed cap for a moment (Zip); the cap decays back.</summary>
        public void Boost(float speed) => overspeedCap = Mathf.Max(overspeedCap, Mathf.Min(speed, MaxAirSpeed * maxOverspeedFactor));

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
            if (keyboard == null || Time.timeScale <= 0f) return;   // nothing queued under a pause, a choice screen or a cutscene

            float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            moveInput = new Vector2(x, y);

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (!IsAirborne) jumpQueued = true;
                else if (TouchingWall && Time.time - lastWallJump >= wallJumpCooldown) wallJumpQueued = true;
                else if (airJumpsLeft > 0) airJumpQueued = true;
            }
            if (keyboard.leftShiftKey.wasPressedThisFrame && IsAirborne && dashesLeft > 0) dashQueued = true;
        }

        private void FixedUpdate()
        {
            if (climbT >= 0f && Climb()) return;

            bool wasAirborne = IsAirborne;
            float fallSpeed = -Body.linearVelocity.y;
            CurrentIsland = FindIslandBelow();
            IsAirborne = CurrentIsland == null;
            if (CurrentIsland != null && CurrentIsland.TryGetComponent(out OrbitingIsland orbit))
                Body.position += orbit.LastStep;   // the boss's circling islands carry the Warden along (2026-11-09)
            if (wasAirborne && !IsAirborne) Landed?.Invoke(fallSpeed);
            overspeedCap = Mathf.MoveTowards(overspeedCap, 0f, overspeedDecay * Time.fixedDeltaTime);
            if (!IsAirborne)
            {
                airJumpsLeft = airJumps;
                dashesLeft = airDashes;
                dashTimer = 0f;
            }

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
            else if (ledgeIsland != null)
            {
                StartClimb(ledgeIsland);
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
                if (dashQueued && dashesLeft > 0)
                {
                    // Towards W/A/S/D relative to the camera (W follows the camera's pitch), or straight where the camera looks.
                    Vector3 look = cameraTransform != null ? cameraTransform.forward : transform.forward;
                    Vector3 dir = look * moveInput.y + right * moveInput.x;
                    if (dir.sqrMagnitude < 0.01f) dir = look;
                    dashVelocity = dir.normalized * Mathf.Max(dashSpeed, Speed);
                    dashTimer = dashDuration;
                    dashesLeft--;
                    Dashed?.Invoke();
                }
                else if (airJumpQueued && airJumpsLeft > 0)
                {
                    Vector3 v = Body.linearVelocity;
                    v.y = Mathf.Max(v.y, 0f) + airJumpVelocity;
                    Body.linearVelocity = v;
                    airJumpsLeft--;
                    AirJumped?.Invoke();
                }

                if (dashTimer > 0f)
                {
                    dashTimer -= Time.fixedDeltaTime;
                    Body.linearVelocity = dashVelocity;
                }
                else
                {
                    Body.AddForce(Physics.gravity * (airGravityMultiplier - 1f), ForceMode.Acceleration);

                    // Air strafe: only A/D, sharp up to airStrafeSpeed; swing momentum beyond it stays.
                    if (!Mathf.Approximately(moveInput.x, 0f) && moveInput.x * Vector3.Dot(Body.linearVelocity, right) < airStrafeSpeed)
                        Body.AddForce(right * (moveInput.x * airStrafeAcceleration), ForceMode.Acceleration);
                }

                float cap = Mathf.Max(MaxAirSpeed, overspeedCap);
                if (Body.linearVelocity.sqrMagnitude > cap * cap)
                    Body.linearVelocity = Body.linearVelocity.normalized * cap;
            }
            jumpQueued = false;
            wallJumpQueued = false;
            airJumpQueued = false;
            dashQueued = false;
            ledgeIsland = null;
        }

        /// <summary>
        /// Plans the climb: out past the rim (so an overhang cannot block it) at a little above the top, then in onto
        /// the island's surface just inside the rim. The collider is off for the move, so nothing snags it.
        /// </summary>
        private void StartClimb(Island island)
        {
            Vector3 centre = island.Top;
            Vector3 flat = Body.position - centre;
            flat.y = 0f;
            float distance = flat.magnitude;
            Vector3 dir = distance > 0.01f ? flat / distance : Vector3.forward;
            float rim = island.Width * 0.5f;
            float pivotAboveFeet = Body.position.y - capsule.bounds.min.y;

            Vector3 landing = centre + dir * Mathf.Max(0f, rim - ledgeLandInset);
            float surface = centre.y;
            foreach (RaycastHit hit in Physics.RaycastAll(landing + Vector3.up * 10f, Vector3.down, 20f,
                         1 << Hookfall.Core.Layers.World, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<Island>() == island) surface = Mathf.Max(surface, hit.point.y);
            landing.y = surface;

            climbFrom = Body.position;
            climbTo = landing + Vector3.up * (pivotAboveFeet + 0.05f);
            climbOver = centre + dir * Mathf.Max(distance, rim + capsule.radius + 0.3f);
            climbOver.y = climbTo.y + ledgeClearance;
            climbT = 0f;
            dashTimer = 0f;
            capsule.enabled = false;
            LedgeClimbed?.Invoke();
        }

        /// <summary>One step of the climb. Returns true while it is still moving; the step after it arrives ends it.</summary>
        private bool Climb()
        {
            float dt = Time.fixedDeltaTime;
            if (climbT >= 1f)
            {
                climbT = -1f;
                capsule.enabled = true;
                Body.linearVelocity = Vector3.zero;
                return false;
            }
            climbT = Mathf.Min(1f, climbT + dt / ledgeClimbTime);
            Vector3 next = climbT < 0.5f ? Vector3.Lerp(climbFrom, climbOver, climbT * 2f) : Vector3.Lerp(climbOver, climbTo, climbT * 2f - 1f);
            Body.linearVelocity = (next - Body.position) / dt;
            jumpQueued = wallJumpQueued = airJumpQueued = dashQueued = false;
            ledgeIsland = null;
            return true;
        }

        // Island sides (steep contacts) arm the wall jump. Any side or underside contact near the top starts a ledge climb.
        private void OnCollisionStay(Collision collision)
        {
            if (!IsAirborne || climbT >= 0f) return;
            Island island = collision.collider.GetComponentInParent<Island>();
            if (island == null) return;
            float feet = capsule.bounds.min.y;
            bool nearTop = feet < island.Top.y && feet >= island.Top.y - ledgeReach;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 n = collision.GetContact(i).normal;
                if (n.y > 0.55f) continue;   // a floor: landing handles it
                if (nearTop) ledgeIsland = island;
                if (n.y < -0.55f) continue;  // an underside: no wall to jump off
                wallNormal = new Vector3(n.x, 0f, n.z).normalized;
                lastWallTime = Time.time;
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
