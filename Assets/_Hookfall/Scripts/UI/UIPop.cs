// Hookfall - a small UI entrance animation: scales up with a little overshoot, fades in and slides from an offset,
// each time the object is enabled. Runs on unscaled time so it works on paused screens.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPop : MonoBehaviour
    {
        public float delay;
        public float duration = 0.28f;
        public float startScale = 0.7f;
        public Vector2 slideFrom;

        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 home;
        private float start;
        private bool homeSet;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (!homeSet) { home = rect.anchoredPosition; homeSet = true; }
            start = Time.unscaledTime + delay;
            Done = false;
            Apply(0f);
        }

        /// <summary>True once the entrance has finished; after that the pop leaves the transform alone.</summary>
        public bool Done { get; private set; }

        private void Update()
        {
            if (Done) return;
            float t = Mathf.Clamp01((Time.unscaledTime - start) / duration);
            Apply(t);
            Done = t >= 1f;
        }

        private void Apply(float t)
        {
            // Back-out easing: overshoots slightly, then settles.
            float s = 1.70158f;
            float e = t - 1f;
            float eased = 1f + e * e * ((s + 1f) * e + s);
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
            // Only slide when asked: inside a layout group the layout owns the position.
            if (slideFrom != Vector2.zero) rect.anchoredPosition = home + slideFrom * (1f - Mathf.Clamp01(eased));
            group.alpha = Mathf.Clamp01(t * 2f);
        }

        /// <summary>Adds a pop to a UI element.</summary>
        public static UIPop Add(Component target, float delay = 0f, float startScale = 0.7f, Vector2 slideFrom = default)
        {
            var pop = target.gameObject.AddComponent<UIPop>();
            pop.delay = delay;
            pop.startScale = startScale;
            pop.slideFrom = slideFrom;
            return pop;
        }
    }
}
