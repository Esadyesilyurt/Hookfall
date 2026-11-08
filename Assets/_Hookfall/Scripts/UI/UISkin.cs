// Hookfall - the UI skin at run time: grayscale 9-slice sprites (made by Hookfall > Build UI Skin), the Blender-rendered
// icons, and the outlined font material, loaded from Resources and cached.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.UI
{
    public static class UISkin
    {
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private static Material outlineFont;

        public static Sprite Panel => Get("UI/UI_Panel");
        public static Sprite Button => Get("UI/UI_Button");
        public static Sprite Card => Get("UI/UI_Card");
        public static Sprite Pill => Get("UI/UI_Pill");
        public static Sprite PillFrame => Get("UI/UI_PillFrame");
        public static Sprite Diamond => Get("UI/UI_Diamond");
        public static Sprite Ring => Get("UI/UI_Ring");
        public static Sprite Glow => Get("UI/UI_Glow");
        public static Sprite Rays => Get("UI/UI_Rays");
        public static Sprite Crosshair => Get("UI/UI_Crosshair");
        public static Sprite KeyCap => Get("UI/UI_KeyCap");
        public static Sprite Dot => Get("UI/UI_Dot");

        /// <summary>An icon by name, e.g. "OrbitBlades", "MaxHealth", "Gold"; null if it was not rendered.</summary>
        public static Sprite Icon(string name) => Get("Icons/ICON_" + name);
        // By the prefab id (PF_Item_OrbitBlades -> ICON_OrbitBlades): the display name is translated.
        public static Sprite Icon(ItemBase item) => item != null ? Icon(item.name.Replace("PF_Item_", "").Replace("(Clone)", "")) : null;
        public static Sprite Icon(StatType stat) => Icon(stat.ToString());

        /// <summary>LiberationSans with a dark outline, for text drawn over the game world.</summary>
        public static Material OutlineFont
        {
            get
            {
                if (outlineFont == null) outlineFont = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
                return outlineFont;
            }
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
