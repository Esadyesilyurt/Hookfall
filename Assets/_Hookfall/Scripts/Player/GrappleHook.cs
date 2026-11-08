// Hookfall - mouse-aimed grappling hook (GDD 4.1). Left click: a 120 m/s hook head, then a taut rope to swing on; the
// swing gains speed on the way down and lets go with a boost, so momentum carries the Warden into flight.
// Right click: the Zip, a power swing with its own cooldown and charges: a stiffer, faster swing that, held for about
// one swing, starts reeling the Warden up towards the anchor to close the distance (reworked 2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with the hook projectile and the Zip, reworked on 2026-11-08 for faster swings. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class GrappleHook : MonoBehaviour
    {
        [Header("Hook")]
        [SerializeField] private float travelSpeed = 120f;
        [SerializeField] private float rethrowCooldown = 0.2f;

        [Header("Rope (SpringJoint)")]
        [Tooltip("Rope length right after attaching, as a share of the distance. 0.9 = 10% reel-in.")]
        [SerializeField] private float maxDistanceFactor = 0.9f;
        [Tooltip("A stiff, barely damped spring behaves like a rope: a real pendulum instead of a soft bungee.")]
        [SerializeField] private float spring = 60f;
        [SerializeField] private float damper = 0.6f;
        [Tooltip("Push along the swing while it is going down (m/s²): each swing gains speed.")]
        [SerializeField] private float swingPump = 10f;
        [Tooltip("Speed kept and multiplied when letting go of a swing.")]
        [SerializeField] private float releaseBoost = 1.1f;
        [Tooltip("Lift added when letting go on the way up, to turn a swing into flight.")]
        [SerializeField] private float releaseLift = 3f;

        [Header("Zip: power swing (right click, held)")]
        [SerializeField] private float zipTravelSpeed = 220f;
        [SerializeField] private float zipSpring = 120f;
        [SerializeField] private float zipPump = 22f;
        [SerializeField] private float zipMaxDistanceFactor = 0.8f;
        [Tooltip("After about one swing of holding, the rope starts reeling in.")]
        [SerializeField] private float zipReelDelay = 0.9f;
        [SerializeField] private float zipReelSpeed = 10f;
        [Tooltip("Pull towards the anchor while reeling (m/s²), to climb up onto islands.")]
        [SerializeField] private float zipReelPull = 16f;
        [SerializeField] private float zipMinRope = 4f;
        [SerializeField] private float zipMaxTime = 4f;
        [SerializeField] private float zipEndLift = 4f;

        [Header("Climbing the rope (W up, S down)")]
        [SerializeField] private float ropeClimbSpeed = 12f;
        [Tooltip("Pull towards the anchor while W is held (m/s²).")]
        [SerializeField] private float ropeClimbPull = 14f;

        [Header("References")]
        [Tooltip("Usually the main camera: the hook fires through the screen centre.")]
        [SerializeField] private Transform aimSource;
        [SerializeField] private LineRenderer rope;
        [Tooltip("Visible hook head; flies out at travelSpeed, then sits on the anchor.")]
        [SerializeField] private Transform hookHead;

        [Header("Rope feel")]
        [SerializeField] private int ropePoints = 16;
        [SerializeField] private float ropeWobble = 0.6f;
        [SerializeField] private float throwWobbleTime = 0.35f;
        [SerializeField] private float attachTwangTime = 0.2f;

        private PlayerController controller;
        private PlayerStats stats;
        private SpringJoint joint;
        private Vector3 anchorPoint;
        private Vector3 headPosition;
        private bool travelling;
        private bool zipThrow;
        private float zipTimer;
        private float lastThrowTime = -999f;
        private int zipCharges;
        private float wobbleTimer;
        private float wobbleDuration = 1f;
        private float zipRecharge;

        public bool IsAttached => joint != null;
        public bool IsZipping { get; private set; }
        public bool IsOut => travelling || IsAttached || IsZipping;
        public bool HasTargetInRange { get; private set; }
        public bool HasZipTarget { get; private set; }
        public float MaxRange => stats != null ? stats.HookRange : 40f;
        public float ZipRange => stats != null ? stats.ZipRange : 50f;
        public Vector3 AnchorPoint => anchorPoint;
        public Vector3 HeadPosition => headPosition;
        public int ZipCharges => zipCharges;
        public int ZipMaxCharges => stats != null ? stats.ZipCharges : 1;
        /// <summary>0..1 progress of the next Zip charge; 1 when every charge is ready.</summary>
        public float ZipRecharge01 => zipCharges >= ZipMaxCharges ? 1f : Mathf.Clamp01(zipRecharge / ZipCooldownSeconds);
        private float ZipCooldownSeconds => stats != null ? stats.ZipCooldown : 4f;

        public event Action Thrown;
        public event Action Attached;
        public event Action Released;
        public event Action ZipStarted;
        public event Action ZipEnded;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            stats = GetComponent<PlayerStats>();
            if (aimSource == null && Camera.main != null) aimSource = Camera.main.transform;
            if (rope != null) rope.enabled = false;
            if (hookHead != null) hookHead.gameObject.SetActive(false);
            zipCharges = ZipMaxCharges;
        }

        private void Update()
        {
            HasTargetInRange = TryFindTarget(MaxRange, out _);
            HasZipTarget = TryFindTarget(ZipRange, out _);
            RechargeZip(Time.deltaTime);

            // The hook head flies on even with no mouse (a gamepad, or a test driving the hook).
            if (travelling)
            {
                float speed = zipThrow ? zipTravelSpeed : travelSpeed;
                headPosition = Vector3.MoveTowards(headPosition, anchorPoint, speed * Time.deltaTime);
                if ((headPosition - anchorPoint).sqrMagnitude < 0.0001f) Attach();
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || Time.timeScale <= 0f) return;

            if (mouse.leftButton.wasPressedThisFrame && Time.time - lastThrowTime >= rethrowCooldown)
                TryThrow(false);
            if (mouse.rightButton.wasPressedThisFrame && zipCharges > 0)
                TryThrow(true);
            if (mouse.leftButton.wasReleasedThisFrame && !zipThrow && !IsZipping)
                Release();
            if (IsZipping && !mouse.rightButton.isPressed)
                Release();   // without a mouse (tests, gamepad) the power swing runs until zipMaxTime
        }

        private void FixedUpdate()
        {
            if (joint == null) return;
            float dt = Time.fixedDeltaTime;
            Rigidbody body = controller.Body;
            Vector3 ropeDir = (anchorPoint - body.position).normalized;

            // Pump the swing while it falls: speed builds on the way down, gravity takes it back on the way up.
            Vector3 velocity = body.linearVelocity;
            Vector3 along = Vector3.ProjectOnPlane(velocity, ropeDir);
            if (velocity.y < 0f && along.sqrMagnitude > 0.25f)
                body.AddForce(along.normalized * (IsZipping ? zipPump : swingPump), ForceMode.Acceleration);

            // W climbs the rope, S lets it out.
            Keyboard keyboard = Keyboard.current;
            float climb = keyboard == null ? 0f : (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            if (climb != 0f)
            {
                joint.maxDistance = Mathf.Clamp(joint.maxDistance - climb * ropeClimbSpeed * dt, zipMinRope, Mathf.Max(MaxRange, ZipRange));
                if (climb > 0f) body.AddForce(ropeDir * ropeClimbPull, ForceMode.Acceleration);
            }

            if (!IsZipping) return;
            zipTimer += dt;
            if (zipTimer >= zipMaxTime) { EndZip(); return; }
            if (zipTimer < zipReelDelay) return;
            joint.maxDistance = Mathf.Max(zipMinRope, joint.maxDistance - zipReelSpeed * dt);
            body.AddForce(ropeDir * zipReelPull, ForceMode.Acceleration);
        }

        private void LateUpdate()
        {
            bool visible = IsOut;
            if (IsAttached || IsZipping) headPosition = anchorPoint;
            if (hookHead != null)
            {
                hookHead.gameObject.SetActive(visible);
                if (visible)
                {
                    hookHead.position = headPosition;
                    Vector3 headDir = headPosition - transform.position;
                    if (headDir.sqrMagnitude > 0.01f) hookHead.rotation = Quaternion.LookRotation(headDir);
                }
            }
            if (rope == null) return;
            rope.enabled = visible;
            if (!visible) return;
            // A thrown rope snakes out and a caught one twangs, then it pulls straight.
            wobbleTimer = Mathf.Max(0f, wobbleTimer - Time.deltaTime);
            float amp = ropeWobble * (wobbleTimer / wobbleDuration);
            Vector3 from = transform.position;
            Vector3 dir = headPosition - from;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            side = side.sqrMagnitude > 0.001f ? side.normalized : Vector3.right;
            rope.positionCount = amp > 0.01f ? ropePoints : 2;
            for (int i = 0; i < rope.positionCount; i++)
            {
                float t = i / (rope.positionCount - 1f);
                float wave = Mathf.Sin(t * Mathf.PI * 3f + Time.time * 40f) * Mathf.Sin(t * Mathf.PI) * amp;
                rope.SetPosition(i, from + dir * t + side * wave);
            }
            rope.widthMultiplier = IsZipping ? 1.8f : 1f;
        }

        private void RechargeZip(float dt)
        {
            int max = ZipMaxCharges;
            if (zipCharges > max) zipCharges = max;
            if (zipCharges >= max) { zipRecharge = 0f; return; }
            zipRecharge += dt;
            if (zipRecharge < ZipCooldownSeconds) return;
            zipRecharge = 0f;
            zipCharges++;
        }

        /// <summary>The hook catches islands (with their tower and props) and the floating hook anchors.</summary>
        public static bool IsHookable(Collider collider) =>
            collider.GetComponentInParent<Island>() != null || collider.GetComponentInParent<HookAnchor>() != null;

        private bool TryFindTarget(float range, out Vector3 point)
        {
            point = default;
            if (aimSource == null) return false;

            float rayLength = range + Vector3.Distance(aimSource.position, transform.position);
            RaycastHit[] hits = Physics.RaycastAll(aimSource.position, aimSource.forward, rayLength,
                ~0, QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;   // skip the player
                if (hit.distance >= best) continue;
                best = hit.distance;
                found = IsHookable(hit.collider) && Vector3.Distance(transform.position, hit.point) <= range;
                point = hit.point;
            }
            return found;
        }

        private void TryThrow(bool zip)
        {
            if (!TryFindTarget(zip ? ZipRange : MaxRange, out Vector3 point)) return;
            Release();
            if (zip) zipCharges--;

            lastThrowTime = Time.time;
            anchorPoint = point;
            headPosition = transform.position;
            travelling = true;
            zipThrow = zip;
            Wobble(throwWobbleTime);
            Thrown?.Invoke();
        }

        private void Wobble(float seconds)
        {
            wobbleTimer = seconds;
            wobbleDuration = seconds;
        }

        private void Attach()
        {
            travelling = false;
            Wobble(attachTwangTime);

            float distance = Vector3.Distance(transform.position, anchorPoint);
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = anchorPoint;
            joint.maxDistance = distance * (zipThrow ? zipMaxDistanceFactor : maxDistanceFactor);
            joint.minDistance = 0f;   // a rope: slack when closer, taut at full length
            joint.spring = zipThrow ? zipSpring : spring;
            joint.damper = damper;
            joint.massScale = 1f;
            Attached?.Invoke();

            if (!zipThrow) return;
            IsZipping = true;
            zipTimer = 0f;
            ZipStarted?.Invoke();
        }

        private void EndZip()
        {
            IsZipping = false;
            DropJoint(zipEndLift);
            ZipEnded?.Invoke();
            Released?.Invoke();
        }

        /// <summary>Lets go of the rope, keeping (and boosting) the swing speed so the Warden flies on.</summary>
        private void DropJoint(float lift)
        {
            travelling = false;
            zipThrow = false;
            if (joint == null) return;
            Destroy(joint);
            joint = null;
            Rigidbody body = controller.Body;
            Vector3 v = body.linearVelocity * releaseBoost;
            if (v.y > 0f) v += Vector3.up * lift;
            body.linearVelocity = v;
            controller.Boost(v.magnitude);
        }

        public void Release()
        {
            bool wasOut = IsOut;
            if (IsZipping)
            {
                EndZip();
                return;
            }
            DropJoint(releaseLift);
            if (wasOut) Released?.Invoke();
        }
    }
}
