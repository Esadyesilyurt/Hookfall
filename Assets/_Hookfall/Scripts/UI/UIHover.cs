// Hookfall - grows a button or card a little while the pointer is over it, and lights its halo if it has one
// (unscaled time, so it works when paused).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; the halo was added on 2026-11-10.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float hoverScale = 1.06f;
        public RectTransform target;
        [Tooltip("Optional: a graphic that fades in while the pointer is over the element.")]
        public Graphic glow;
        public float glowAlpha = 0.85f;

        private bool over;
        private UIPop pop;
        private float current = 1f;

        /// <summary>True while the pointer is over the element.</summary>
        public bool Over => over;

        private void Awake()
        {
            if (target == null) target = (RectTransform)transform;
            pop = GetComponent<UIPop>();
        }

        public void OnPointerEnter(PointerEventData eventData) => over = true;
        public void OnPointerExit(PointerEventData eventData) => over = false;

        private void OnDisable()
        {
            over = false;
            current = 1f;
            SetGlow(0f);
        }

        private void LateUpdate()
        {
            if (pop != null && !pop.Done) return;   // let the entrance animation finish first
            current = Mathf.MoveTowards(current, over ? hoverScale : 1f, Time.unscaledDeltaTime * 1.2f);
            target.localScale = Vector3.one * current;
            if (glow != null) SetGlow(Mathf.MoveTowards(glow.color.a, over ? glowAlpha : 0f, Time.unscaledDeltaTime * 6f));
        }

        private void SetGlow(float alpha)
        {
            if (glow == null) return;
            Color c = glow.color;
            c.a = alpha;
            glow.color = c;
        }
    }
}
