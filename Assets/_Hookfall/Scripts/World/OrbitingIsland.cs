// Hookfall - an island that circles the stage boss: the hook holds on it (GrappleHook follows its anchor) and the Warden
// standing on it is carried along (PlayerController adds LastStep). Moves as a kinematic body in its parent's space, so
// the arena can still be raised by the boss's entrance cutscene.
// AI disclosure: written with Claude (Anthropic) on 2026-11-09. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    [DefaultExecutionOrder(-50)]   // moves before the Warden reads LastStep in the same physics step
    public class OrbitingIsland : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 6f;

        private Rigidbody body;
        private Vector3 axis;        // the orbit's centre, in the parent's space
        private float radius, height, angle;

        /// <summary>How far the island moved in the last physics step.</summary>
        public Vector3 LastStep { get; private set; }

        /// <summary>Starts circling the vertical axis through worldCentre, from where the island is now.</summary>
        public void Init(Vector3 worldCentre, float speed)
        {
            degreesPerSecond = speed;
            Transform parent = transform.parent;
            axis = parent != null ? parent.InverseTransformPoint(worldCentre) : worldCentre;
            Vector3 local = transform.localPosition - axis;
            radius = new Vector2(local.x, local.z).magnitude;
            height = local.y;
            angle = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            angle += degreesPerSecond * Time.fixedDeltaTime;
            float a = angle * Mathf.Deg2Rad;
            Vector3 local = axis + new Vector3(Mathf.Cos(a) * radius, height, Mathf.Sin(a) * radius);
            Vector3 target = transform.parent != null ? transform.parent.TransformPoint(local) : local;
            LastStep = target - body.position;
            body.MovePosition(target);
        }
    }
}
