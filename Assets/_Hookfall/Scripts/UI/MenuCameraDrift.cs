// Hookfall - the main menu's camera: a slow sway around the tower, so the scene behind the menu is alive.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.UI
{
    public class MenuCameraDrift : MonoBehaviour
    {
        [Tooltip("The point the camera circles and looks at (the tower's middle).")]
        [SerializeField] private Vector3 focus = new Vector3(0f, 9f, 0f);
        [Tooltip("How far the camera swings to each side, in degrees.")]
        [SerializeField] private float swing = 14f;
        [Tooltip("Seconds for one swing there and back.")]
        [SerializeField] private float period = 46f;
        [SerializeField] private float bob = 1.2f;

        private Vector3 offset;

        private void Awake() => offset = transform.position - focus;

        private void LateUpdate()
        {
            float t = Time.unscaledTime * Mathf.PI * 2f / period;
            Vector3 p = Quaternion.AngleAxis(Mathf.Sin(t) * swing, Vector3.up) * offset;
            p.y += Mathf.Sin(t * 1.7f) * bob;
            transform.position = focus + p;
            // The tower stays right of the middle, clear of the menu on the left.
            transform.rotation = Quaternion.LookRotation(focus - transform.position) * Quaternion.Euler(0f, -9f, 0f);
        }
    }
}
