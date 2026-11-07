// Hookfall - project layers used from code (defined in ProjectSettings/TagManager.asset).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Core
{
    public static class Layers
    {
        /// <summary>Lines, trails, beams and glows: the toon outline pass skips this layer.</summary>
        public const int NoOutline = 8;
        /// <summary>Solid scenery (islands, tower, props): the camera never goes inside it. Enemies stay on Default.</summary>
        public const int World = 9;

        public static void SetRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetRecursive(child.gameObject, layer);
        }
    }
}
