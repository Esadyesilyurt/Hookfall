// Hookfall - mouse-aimed grappling hook (GDD 4.1). Left click: a 120 m/s hook head, then a SpringJoint rope to swing on.
// Right click: the Zip, a faster hook that hauls the Warden straight to the anchor at high speed, with its own
// cooldown and charges (added 2026-11-07 to escape the horde and cross the arena quickly).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with the hook projectile and the Zip. Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Tooltip("Rope length right after attaching, as a share of the distance. 0.85 = 15% reel-in.")]
        [SerializeField] private float maxDistanceFactor = 0.85f;
        [SerializeField] private float minDistanceFactor = 0.25f;
        [SerializeField] private float spring = 4.5f;
        [SerializeField] private float damper = 7f;
        [SerializeField] private float massScale = 4.5f;

        [Header("Zip (right click)")]
        [SerializeField] private float zipTravelSpeed = 220f;
        [Tooltip("Speed the Zip hauls the Warden towards the anchor.")]
        [SerializeField] private float zipSpeed = 48f;
        [Tooltip("The Zip ends this close to the anchor; the Warden keeps the momentum.")]
        [SerializeField] private float zipArriveDistance = 3f;
        [SerializeField] private float zipMaxTime = 1.6f;
        [Tooltip("Lift added at the end so a Zip onto an island edge carries the Warden over it.")]
        [SerializeField] private float zipEndLift = 6f;

        [Header("References")]
        [Tooltip("Usually the main camera: the hook fires through the screen centre.")]
        [SerializeField] private Transform aimSource;
        [SerializeField] private LineRenderer rope;
        [Tooltip("Visible hook head; flies out at travelSpeed, then sits on the anchor.")]
        [SerializeField] private Transform hookHead;

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

        }

        private void FixedUpdate()
        {
            if (!IsZipping) return;
            zipTimer += Time.fixedDeltaTime;
            Vector3 toAnchor = anchorPoint - controller.Body.position;
            float distance = toAnchor.magnitude;
            if (distance <= zipArriveDistance || zipTimer >= zipMaxTime || (!controller.IsAirborne && zipTimer > 0.15f))
            {
                EndZip();
                return;
            }
            controller.Body.linearVelocity = toAnchor / distance * zipSpeed;
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
                    Vector3 dir = headPosition - transform.position;
                    if (dir.sqrMagnitude > 0.01f) hookHead.rotation = Quaternion.LookRotation(dir);
                }
            }
            if (rope == null) return;
            rope.enabled = visible;
            if (!visible) return;
            rope.positionCount = 2;
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, headPosition);
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
                found = hit.collider.GetComponentInParent<Island>() != null
                        && Vector3.Distance(transform.position, hit.point) <= range;
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
            Thrown?.Invoke();
        }

        private void Attach()
        {
            travelling = false;
            if (zipThrow)
            {
                IsZipping = true;
                zipTimer = 0f;
                controller.Boost(zipSpeed);
                Attached?.Invoke();
                ZipStarted?.Invoke();
                return;
            }

            float distance = Vector3.Distance(transform.position, anchorPoint);
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = anchorPoint;
            joint.maxDistance = distance * maxDistanceFactor;
            joint.minDistance = distance * minDistanceFactor;
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
            Attached?.Invoke();
        }

        private void EndZip()
        {
            IsZipping = false;
            zipThrow = false;
            controller.Boost(zipSpeed);
            controller.Body.linearVelocity += Vector3.up * zipEndLift;
            ZipEnded?.Invoke();
            Released?.Invoke();
        }

        public void Release()
        {
            bool wasOut = IsOut;
            if (IsZipping)
            {
                EndZip();
                return;
            }
            travelling = false;
            zipThrow = false;
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }
            if (wasOut) Released?.Invoke();
        }
    }
}
