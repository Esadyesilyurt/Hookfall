// Hookfall - mouse-aimed grappling hook with a SpringJoint rope (GDD 4.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class GrappleHook : MonoBehaviour
    {
        [Header("Hook")]
        [SerializeField] private float maxRange = 40f;
        [SerializeField] private float rethrowCooldown = 0.2f;
        // TODO (Week 9): the hook attaches instantly; add a visible hook projectile at 120 m/s.

        [Header("Rope (SpringJoint)")]
        [Tooltip("Rope length right after attaching, as a share of the distance. 0.8 = 20% reel-in.")]
        [SerializeField] private float maxDistanceFactor = 0.8f;
        [SerializeField] private float minDistanceFactor = 0.25f;
        [SerializeField] private float spring = 4.5f;
        [SerializeField] private float damper = 7f;
        [SerializeField] private float massScale = 4.5f;

        [Header("References")]
        [Tooltip("Usually the main camera: the hook fires through the screen centre.")]
        [SerializeField] private Transform aimSource;
        [SerializeField] private LineRenderer rope;

        private SpringJoint joint;
        private Vector3 anchorPoint;
        private float lastThrowTime = -999f;

        public bool IsAttached => joint != null;
        public bool HasTargetInRange { get; private set; }
        public float MaxRange => maxRange;

        private void Awake()
        {
            if (aimSource == null && Camera.main != null) aimSource = Camera.main.transform;
            if (rope != null) rope.enabled = false;
        }

        private void Update()
        {
            HasTargetInRange = TryFindTarget(out _);

            Mouse mouse = Mouse.current;
            if (mouse == null || Time.timeScale <= 0f) return;

            if (mouse.leftButton.wasPressedThisFrame && Time.time - lastThrowTime >= rethrowCooldown)
                TryAttach();
            if (mouse.leftButton.wasReleasedThisFrame)
                Release();
        }

        private void LateUpdate()
        {
            if (rope == null) return;
            rope.enabled = IsAttached;
            if (!IsAttached) return;
            rope.positionCount = 2;
            rope.SetPosition(0, transform.position);
            rope.SetPosition(1, anchorPoint);
        }

        public void AddRange(float metres) => maxRange += metres;

        private bool TryFindTarget(out Vector3 point)
        {
            point = default;
            if (aimSource == null) return false;

            float rayLength = maxRange + Vector3.Distance(aimSource.position, transform.position);
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
                        && Vector3.Distance(transform.position, hit.point) <= maxRange;
                point = hit.point;
            }
            return found;
        }

        private void TryAttach()
        {
            if (!TryFindTarget(out Vector3 point)) return;
            Release();

            lastThrowTime = Time.time;
            anchorPoint = point;
            float distance = Vector3.Distance(transform.position, anchorPoint);

            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = anchorPoint;
            joint.maxDistance = distance * maxDistanceFactor;
            joint.minDistance = distance * minDistanceFactor;
            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;
        }

        public void Release()
        {
            if (joint == null) return;
            Destroy(joint);
            joint = null;
        }
    }
}
