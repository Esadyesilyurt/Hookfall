// Hookfall - the UI skin at run time: the sprites drawn by Tools/UI/make_ui.js (panels, frames, bars, icons), and the
// fonts. The fonts are made here from the .ttf files in Resources/Fonts as dynamic TextMeshPro fonts, so no generated
// font asset sits in the project: Cinzel for titles, Barlow Semi Condensed for everything else, LiberationSans behind
// them for the few signs they lack (the arrow in "1 → 2").
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; new skin, icons and fonts on 2026-11-10.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Player;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Hookfall.UI
{
    public static class UISkin
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private static TMP_FontAsset body, bodyBold, display, displayBlack;
        private static Material bodyOutline, displayOutline, blackOutline, bodyShadow;

        // Baked sprites (use them white).
        public static Sprite Panel => Get("UI/UI_Panel");
        public static Sprite Plaque => Get("UI/UI_Plaque");
        public static Sprite Tooltip => Get("UI/UI_Tooltip");
        public static Sprite CardFill => Get("UI/UI_CardFill");
        public static Sprite Slot => Get("UI/UI_Slot");
        public static Sprite BarBack => Get("UI/UI_BarBack");
        public static Sprite BarGloss => Get("UI/UI_BarGloss");
        public static Sprite DiamondFill => Get("UI/UI_DiamondFill");
        public static Sprite Vignette => Get("UI/UI_Vignette");
        public static Sprite Crosshair => Get("UI/UI_Crosshair");

        // Greyscale sprites (tint them).
        public static Sprite Button => Get("UI/UI_Button");
        public static Sprite Chip => Get("UI/UI_Chip");
        public static Sprite Tab => Get("UI/UI_Tab");
        public static Sprite KeyCap => Get("UI/UI_KeyCap");
        public static Sprite CardFrame => Get("UI/UI_CardFrame");
        public static Sprite CardHeader => Get("UI/UI_CardHeader");
        public static Sprite SlotFrame => Get("UI/UI_SlotFrame");
        public static Sprite GlowFrame => Get("UI/UI_GlowFrame");
        public static Sprite Outline => Get("UI/UI_Outline");
        public static Sprite Pill => Get("UI/UI_Pill");
        public static Sprite PillFrame => Get("UI/UI_PillFrame");
        public static Sprite Diamond => Get("UI/UI_Diamond");
        public static Sprite Circle => Get("UI/UI_Circle");
        public static Sprite CircleFrame => Get("UI/UI_CircleFrame");
        public static Sprite Ring => Get("UI/UI_Ring");
        public static Sprite Dot => Get("UI/UI_Dot");
        public static Sprite Glow => Get("UI/UI_Glow");
        public static Sprite Rays => Get("UI/UI_Rays");
        public static Sprite FadeLine => Get("UI/UI_FadeLine");
        public static Sprite ShadeDown => Get("UI/UI_ShadeDown");
        public static Sprite ShadeRight => Get("UI/UI_ShadeRight");
        public static Sprite Ribbon => Get("UI/UI_Ribbon");
        public static Sprite Pip => Get("UI/UI_Pip");
        public static Sprite Wedge => Get("UI/UI_Wedge");
        /// <summary>The card frame: kept under its old name for the screens that frame a box with it.</summary>
        public static Sprite Card => CardFrame;

        /// <summary>An icon by name, e.g. "OrbitBlades", "MaxHealth", "Gold"; null if there is none.</summary>
        public static Sprite Icon(string name) => Get("Icons/ICON_" + name);
        // By the prefab id (PF_Item_OrbitBlades -> ICON_OrbitBlades): the display name is translated.
        public static Sprite Icon(ItemBase item) => item != null ? Icon(item.name.Replace("PF_Item_", "").Replace("(Clone)", "")) : null;
        public static Sprite Icon(StatType stat) => Icon(stat.ToString());
        public static Sprite Icon(School school) => Icon(school.ToString());

        // ---------------------------------------------------------------- fonts

        /// <summary>Body text and numbers (Barlow Semi Condensed Medium; bold text switches to its Bold face).</summary>
        public static TMP_FontAsset Body
        {
            get
            {
                if (body == null)
                {
                    body = MakeFont("BarlowSemiCondensed-Medium");
                    bodyBold = MakeFont("BarlowSemiCondensed-Bold");
                    if (body != null && bodyBold != null) body.fontWeightTable[7].regularTypeface = bodyBold;   // 700: <b> and FontStyles.Bold
                }
                return body != null ? body : TMP_Settings.defaultFontAsset;
            }
        }

        /// <summary>Titles (Cinzel Bold: capitals and small capitals).</summary>
        public static TMP_FontAsset Display
        {
            get
            {
                if (display == null) display = MakeFont("Cinzel-Bold");
                return display != null ? display : Body;
            }
        }

        /// <summary>The logo and the boss's name (Cinzel Black).</summary>
        public static TMP_FontAsset DisplayBlack
        {
            get
            {
                if (displayBlack == null) displayBlack = MakeFont("Cinzel-Black");
                return displayBlack != null ? displayBlack : Display;
            }
        }

        /// <summary>Body text with a dark outline, for text drawn over the game world.</summary>
        public static Material BodyOutline => bodyOutline != null ? bodyOutline : bodyOutline = Outlined(Body, 0.22f, 0.12f);
        /// <summary>Title text with a dark outline and a soft shadow.</summary>
        public static Material DisplayOutline => displayOutline != null ? displayOutline : displayOutline = Outlined(Display, 0.2f, 0.1f);
        /// <summary>Body text with only a soft drop shadow (labels on panels).</summary>
        public static Material BodyShadow => bodyShadow != null ? bodyShadow : bodyShadow = Shadowed(Body);
        /// <summary>The outlined material for any of the skin's fonts.</summary>
        public static Material OutlineFor(TMP_FontAsset font)
        {
            if (font == Body) return BodyOutline;
            if (font == Display) return DisplayOutline;
            if (blackOutline == null) blackOutline = Outlined(font, 0.18f, 0.08f);
            return blackOutline;
        }

        private static TMP_FontAsset MakeFont(string file)
        {
            var source = Resources.Load<Font>("Fonts/" + file);
            if (source == null) return null;
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 80, 10, GlyphRenderMode.SDFAA, 2048, 2048);
            if (font == null) return null;
            font.name = file;
            // The full distance-field shader that the TextMeshPro resources already ship (always in a build).
            TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null)
            {
                if (fallback.material != null) font.material.shader = fallback.material.shader;
                font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            }
            return font;
        }

        private static Material Outlined(TMP_FontAsset font, float width, float dilate)
        {
            var material = new Material(font.material) { name = font.name + " Outline" };
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.05f, 0.06f, 0.12f, 1f));
            material.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
            Shadow(material, 0.55f);
            return material;
        }

        private static Material Shadowed(TMP_FontAsset font)
        {
            var material = new Material(font.material) { name = font.name + " Shadow" };
            Shadow(material, 0.7f);
            return material;
        }

        private static void Shadow(Material material, float alpha)
        {
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0.03f, alpha));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.45f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.15f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
        }

        private static Sprite Get(string path)
        {
            if (cache.TryGetValue(path, out Sprite sprite) && sprite != null) return sprite;
            sprite = Resources.Load<Sprite>(path);
            cache[path] = sprite;
            return sprite;
        }
    }
}
