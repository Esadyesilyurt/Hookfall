// Hookfall - mouse-aimed grappling hook: a 120 m/s hook projectile, then a SpringJoint rope (GDD 4.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 with the hook projectile. Prompts and changes are recorded in Docs/PromptLog.md.
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

        [Header("References")]
        [Tooltip("Usually the main camera: the hook fires through the screen centre.")]
        [SerializeField] private Transform aimSource;
        [SerializeField] private LineRenderer rope;
        [Tooltip("Visible hook head; flies out at travelSpeed, then sits on the anchor.")]
        [SerializeField] private Transform hookHead;

        private PlayerStats stats;
        private SpringJoint joint;
        private Vector3 anchorPoint;
        private Vector3 headPosition;
        private bool travelling;
        private float lastThrowTime = -999f;

        public bool IsAttached => joint != null;
        public bool IsOut => travelling || IsAttached;
        public bool HasTargetInRange { get; private set; }
        public float MaxRange => stats != null ? stats.HookRange : 40f;
        public Vector3 AnchorPoint => anchorPoint;

        public event Action Thrown;
        public event Action Attached;
        public event Action Released;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            if (aimSource == null && Camera.main != null) aimSource = Camera.main.transform;
            if (rope != null) rope.enabled = false;
            if (hookHead != null) hookHead.gameObject.SetActive(false);
        }

        private void Update()
        {
            HasTargetInRange = TryFindTarget(out _);

            Mouse mouse = Mouse.current;
            if (mouse == null || Time.timeScale <= 0f) return;

            if (mouse.leftButton.wasPressedThisFrame && Time.time - lastThrowTime >= rethrowCooldown)
                TryThrow();
            if (mouse.leftButton.wasReleasedThisFrame)
                Release();

            if (travelling)
            {
                headPosition = Vector3.MoveTowards(headPosition, anchorPoint, travelSpeed * Time.deltaTime);
                if ((headPosition - anchorPoint).sqrMagnitude < 0.0001f) Attach();
            }
        }

        private void LateUpdate()
        {
            bool visible = IsOut;
            if (IsAttached) headPosition = anchorPoint;
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

        private bool TryFindTarget(out Vector3 point)
        {
            point = default;
            if (aimSource == null) return false;

            float range = MaxRange;
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

        private void TryThrow()
        {
            if (!TryFindTarget(out Vector3 point)) return;
            Release();

            lastThrowTime = Time.time;
            anchorPoint = point;
            headPosition = transform.position;
            travelling = true;
            Thrown?.Invoke();
        }

        private void Attach()
        {
            travelling = false;
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

        public void Release()
        {
            bool wasOut = IsOut;
            travelling = false;
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }
            if (wasOut) Released?.Invoke();
        }
    }
}
