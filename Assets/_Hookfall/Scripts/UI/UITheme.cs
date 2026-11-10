// Hookfall - UI colours from the GDD palette (8.1): cyan for the player, warm yellow for rewards, red for danger,
// on the skin's navy panels.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; reworked for the baked skin on 2026-11-10.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using UnityEngine;

namespace Hookfall.UI
{
    public static class UITheme
    {
        // Panels are baked sprites: these are plain tints (white keeps the skin's own colours).
        public static readonly Color Panel = Color.white;
        public static readonly Color PanelLight = new Color(0.62f, 0.74f, 0.95f, 1f);
        public static readonly Color PanelDeep = new Color(0.05f, 0.08f, 0.16f, 0.92f);
        public static readonly Color Track = new Color(0.06f, 0.08f, 0.14f, 0.9f);
        public static readonly Color Dim = new Color(0.01f, 0.02f, 0.06f, 0.78f);
        public static readonly Color Ink = new Color(0.05f, 0.07f, 0.13f);

        public static readonly Color Text = new Color(0.94f, 0.96f, 0.98f);
        public static readonly Color TextMuted = new Color(0.64f, 0.71f, 0.8f);
        public static readonly Color TextDark = new Color(0.07f, 0.1f, 0.18f);

        public static readonly Color Player = new Color(0.3f, 0.95f, 1f);
        public static readonly Color Xp = new Color(0.35f, 0.9f, 1f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.3f);
        public static readonly Color Reward = new Color(1f, 0.76f, 0.25f);
        public static readonly Color Danger = new Color(0.95f, 0.27f, 0.25f);
        public static readonly Color Health = new Color(0.94f, 0.28f, 0.35f);
        public static readonly Color Heal = new Color(0.48f, 0.86f, 0.42f);
        public static readonly Color Enemy = new Color(0.9f, 0.3f, 0.82f);

        // Button plates (tints of the greyscale button sprite).
        public static readonly Color Button = new Color(0.36f, 0.47f, 0.66f, 1f);
        public static readonly Color ButtonPrimary = new Color(0.16f, 0.66f, 0.74f, 1f);
        public static readonly Color ButtonDanger = new Color(0.66f, 0.27f, 0.31f, 1f);
        public static readonly Color ButtonHover = new Color(0.3f, 0.65f, 0.75f, 1f);

        public static Color ForRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return new Color(0.36f, 0.64f, 1f);
                case Rarity.Epic: return new Color(0.8f, 0.45f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.64f, 0.16f);
                default: return new Color(0.82f, 0.86f, 0.92f);
            }
        }

        public static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
        public static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);
    }
}
