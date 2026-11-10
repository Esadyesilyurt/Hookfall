// Hookfall - imports the UI art as sprites: the skin (panels, frames, bars) with the 9-slice borders that
// Tools/UI/make_ui.js wrote to Art/UI/skin_borders.txt, and the icons. The art itself is drawn by that script
// (node Tools/UI/make_ui.js); this only sets the import settings, and only when they differ.
// UI sprites use 100 pixels per unit; icons are 256 x 256.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07 (it drew the skin from distance functions then);
// rewritten on 2026-11-10 for the drawn skin. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class UISetup
    {
        private const string SkinFolder = Root + "/Art/UI/Resources/UI";
        private const string IconFolder = Root + "/Art/UI/Resources/Icons";
        private const string BordersFile = Root + "/Art/UI/skin_borders.txt";

        [MenuItem("Hookfall/Build UI Skin")]
        public static void Run()
        {
            Dictionary<string, Vector4> borders = ReadBorders();
            if (AssetDatabase.IsValidFolder(SkinFolder))
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SkinFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string name = Path.GetFileNameWithoutExtension(path).Replace("UI_", "");
                    if (!borders.TryGetValue(name, out Vector4 border))
                    {
                        // A sprite the skin no longer draws (left from the first, generated skin): remove it.
                        AssetDatabase.DeleteAsset(path);
                        continue;
                    }
                    SpriteImport(path, border, false);
                }
            if (AssetDatabase.IsValidFolder(IconFolder))
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
                    SpriteImport(AssetDatabase.GUIDToAssetPath(guid), Vector4.zero, true);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Lines of "Name left bottom right top", in pixels.</summary>
        private static Dictionary<string, Vector4> ReadBorders()
        {
            var borders = new Dictionary<string, Vector4>();
            if (!File.Exists(BordersFile))
            {
                Debug.LogError($"[Hookfall] {BordersFile} is missing: run node Tools/UI/make_ui.js.");
                return borders;
            }
            foreach (string line in File.ReadAllLines(BordersFile))
            {
                string[] parts = line.Split(' ');
                if (parts.Length != 5) continue;
                borders[parts[0]] = new Vector4(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
            }
            return borders;
        }

        private static void SpriteImport(string path, Vector4 border, bool icon)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            // Icons are shown far smaller than they are drawn: mip maps keep their outlines clean. The skin is drawn at
            // its on-screen size and its soft gradients must not band, so it stays uncompressed.
            TextureImporterCompression compression = icon ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && importer.alphaIsTransparency && importer.mipmapEnabled == icon && importer.textureCompression == compression
                && importer.spriteBorder == border && Mathf.Approximately(importer.spritePixelsPerUnit, 100f)
                && importer.filterMode == (icon ? FilterMode.Trilinear : FilterMode.Bilinear)) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = icon;
            importer.filterMode = icon ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.textureCompression = compression;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }
}
