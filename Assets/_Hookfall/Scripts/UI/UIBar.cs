// Hookfall - a HUD bar with a value and, if asked for, a pale trail that follows a falling value a moment later
// (health, the boss's health). Runs on unscaled time.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class UIBar : MonoBehaviour
    {
        [Tooltip("How fast the trail catches up with a falling value, in bar lengths per second.")]
        public float trailSpeed = 0.6f;

        private RectTransform fill, trail;
        private float value = 1f, trailValue = 1f;

        public Image FillImage { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        public static UIBar Create(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color,
            bool withTrail = false, Vector2? pivot = null)
        {
            RectTransform rt = UIFactory.Rect(parent, name, anchor, anchor, position, size, pivot);
            UIFactory.Image(rt, Color.white, UISkin.BarBack);
            var bar = rt.gameObject.AddComponent<UIBar>();
            if (withTrail)
            {
                bar.trail = UIFactory.Rect(rt, "Trail", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
                UIFactory.Image(bar.trail, new Color(1f, 0.93f, 0.86f, 0.9f), UISkin.Pill);
            }
            bar.fill = UIFactory.Rect(rt, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            bar.FillImage = UIFactory.Image(bar.fill, color, UISkin.Pill);
            UIFactory.Image(UIFactory.Fill(bar.fill, "Gloss"), Color.white, UISkin.BarGloss);
            UIFactory.Image(UIFactory.Fill(rt, "Frame"), new Color(1f, 1f, 1f, 0.42f), UISkin.PillFrame);
            return bar;
        }

        /// <summary>Sets the bar to 0..1; snap also moves the trail at once (a new target, a refill).</summary>
        public void Set(float amount, bool snap = false)
        {
            value = Mathf.Clamp01(amount);
            UIFactory.SetFill(fill, value);
            if (snap || value > trailValue) trailValue = value;
        }

        private void Update()
        {
            if (trail == null) return;
            trailValue = Mathf.MoveTowards(trailValue, value, Time.unscaledDeltaTime * trailSpeed);
            UIFactory.SetFill(trail, trailValue);
        }
    }
}
