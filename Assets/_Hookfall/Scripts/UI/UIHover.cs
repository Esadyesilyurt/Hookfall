// Hookfall - grows a button or card a little while the pointer is over it (unscaled time, so it works when paused).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hookfall.UI
{
    public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float hoverScale = 1.06f;
        public RectTransform target;

        private bool over;
        private UIPop pop;
        private float current = 1f;

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
        }

        private void LateUpdate()
        {
            if (pop != null && !pop.Done) return;   // let the entrance animation finish first
            current = Mathf.MoveTowards(current, over ? hoverScale : 1f, Time.unscaledDeltaTime * 1.2f);
            target.localScale = Vector3.one * current;
        }
    }
}
