// Hookfall - UI colours from the GDD palette (8.1): cyan for the player, warm yellow for rewards, red for danger.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using UnityEngine;

namespace Hookfall.UI
{
    public static class UITheme
    {
        public static readonly Color Panel = new Color(0.07f, 0.09f, 0.13f, 0.88f);
        public static readonly Color PanelLight = new Color(0.16f, 0.19f, 0.25f, 0.95f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);
        public static readonly Color Text = new Color(0.93f, 0.95f, 0.97f);
        public static readonly Color TextMuted = new Color(0.65f, 0.7f, 0.76f);
        public static readonly Color Player = new Color(0.3f, 0.95f, 1f);
        public static readonly Color Reward = new Color(1f, 0.76f, 0.25f);
        public static readonly Color Danger = new Color(0.95f, 0.25f, 0.22f);
        public static readonly Color Health = new Color(0.9f, 0.3f, 0.3f);
        public static readonly Color Button = new Color(0.2f, 0.25f, 0.32f, 1f);
        public static readonly Color ButtonHover = new Color(0.25f, 0.55f, 0.62f, 1f);

        public static Color ForRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return new Color(0.35f, 0.62f, 1f);
                case Rarity.Epic: return new Color(0.78f, 0.42f, 1f);
                default: return new Color(0.85f, 0.87f, 0.9f);
            }
        }
    }
}
