// Hookfall - the flight chain on the HUD (bottom centre, over the ability row): the reward multiplier, the name of
// the tier with the kills in the chain, and a bar that drains until the next kill. Hidden until the chain is a few
// kills long; when a named chain breaks it says so for a moment.
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class HudFlow
    {
        private const int ShowFrom = 3;       // shorter chains come and go all the time: not worth the eye
        private const float LostTime = 1.4f;  // seconds the "broken" line stays

        private static readonly Color[] TierColors =
        {
            new Color(0.94f, 0.96f, 0.98f), new Color(0.3f, 0.95f, 1f), new Color(0.55f, 1f, 0.6f),
            new Color(1f, 0.8f, 0.3f), new Color(1f, 0.5f, 0.3f),
        };

        private readonly RectTransform rect;
        private readonly CanvasGroup group;
        private readonly TextMeshProUGUI value, label;
        private readonly Image icon;
        private readonly UIBar drain;

        private FlowChain chain;
        private int shownCount = -1;
        private float punch, lost;
        private int lostCount;

        public HudFlow(Transform canvas, Vector2 position)
        {
            Vector2 bottom = new Vector2(0.5f, 0f);
            rect = UIFactory.Rect(canvas, "Flow", bottom, bottom, position, new Vector2(360f, 84f), bottom);
            group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            label = UIFactory.Text(UIFactory.Centre(rect, "Label", new Vector2(0f, 30f), new Vector2(360f, 24f)), "", 18f, UITheme.Text, TextAlignmentOptions.Center, true);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 5f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            value = UIFactory.Title(UIFactory.Centre(rect, "Value", new Vector2(14f, -2f), new Vector2(220f, 44f)), "", 38f, UITheme.Text);
            icon = UIFactory.Image(UIFactory.Centre(rect, "Icon", new Vector2(-72f, -2f), new Vector2(38f, 38f)), Color.white, UISkin.Icon("Flow"));
            icon.preserveAspect = true;
            drain = UIBar.Create(rect, "Drain", bottom, new Vector2(0f, 2f), new Vector2(220f, 10f), UITheme.Player, false, bottom);
        }

        public void Tick(float dt)
        {
            if (chain != FlowChain.Instance)
            {
                if (chain != null) { chain.Broken -= OnBroken; chain.TierReached -= OnTier; }
                chain = FlowChain.Instance;
                if (chain != null) { chain.Broken += OnBroken; chain.TierReached += OnTier; }
            }
            int count = chain != null ? chain.Count : 0;
            bool live = count >= ShowFrom;
            lost = Mathf.MoveTowards(lost, 0f, dt / LostTime);
            punch = Mathf.MoveTowards(punch, 0f, dt * 5f);

            if (live)
            {
                lost = 0f;
                Color color = TierColors[Mathf.Min(chain.Tier, TierColors.Length - 1)];
                if (count != shownCount)
                {
                    if (count > shownCount) punch = Mathf.Max(punch, 0.35f);
                    shownCount = count;
                    value.text = "×" + chain.Multiplier.ToString("0.00");
                    string name = chain.Tier > 0 ? Loc.Upper(chain.TierName) : Loc.T("FLOW");
                    label.text = name + "  ·  " + count;
                }
                value.color = color;
                label.color = color;
                icon.color = color;
                drain.FillImage.color = color;
                drain.Set(chain.TimeLeft01);
                drain.gameObject.SetActive(true);
                value.gameObject.SetActive(true);
                icon.gameObject.SetActive(true);
            }
            else
            {
                shownCount = -1;
                if (lost > 0f)
                {
                    // The chain is gone: only the line that says so, fading.
                    label.text = Loc.F("FLOW BROKEN  ·  {0}", lostCount);
                    label.color = UITheme.Danger;
                    drain.gameObject.SetActive(false);
                    value.gameObject.SetActive(false);
                    icon.gameObject.SetActive(false);
                }
            }
            float target = live ? 1f : Mathf.Clamp01(lost * 2.5f);
            group.alpha = Mathf.MoveTowards(group.alpha, target, dt * 6f);
            rect.localScale = Vector3.one * (1f + punch * 0.22f);
        }

        private void OnTier(int tier) => punch = 1f;   // a new name: a bigger jump than one more kill

        private void OnBroken(int was)
        {
            if (was < 10) return;   // only a named chain is worth mourning
            lost = 1f;
            lostCount = was;
        }
    }
}
