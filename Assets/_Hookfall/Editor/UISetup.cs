// Hookfall - generates the UI skin: grayscale 9-slice sprites (panel, button, card, diamond, pill bar, ring, glow,
// crosshair, key cap) drawn from signed distance functions, so the code tints them with the GDD palette. Also imports
// the Blender-rendered icons as sprites. Everything lands in Art/UI/Resources so the UI can load it at run time.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
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

        [MenuItem("Hookfall/Build UI Skin")]
        public static void Run()
        {
            EnsureFolder(SkinFolder);
            // Rounded panel: soft top light, darker bottom, a 3 px rim.
            Make("Panel", 96, 96, 28, (x, y) => RoundRect(x, y, 96, 96, 22, 3f, 0.75f, 1f, 0.55f));
            Make("Button", 96, 64, 24, (x, y) => RoundRect(x, y, 96, 64, 18, 3f, 0.82f, 1f, 0.7f));
            Make("Card", 128, 128, 40, (x, y) => CardFrame(x, y, 128, 128));
            Make("Pill", 64, 32, 15, (x, y) => RoundRect(x, y, 64, 32, 15.5f, 0f, 1f, 1f, 1f));
            Make("PillFrame", 64, 32, 15, (x, y) => RoundRect(x, y, 64, 32, 15.5f, 2.5f, 0.35f, 1f, 0.35f));
            Make("Diamond", 128, 128, 0, (x, y) => Diamond(x, y, 128));
            Make("Ring", 128, 128, 0, (x, y) => Ring(x, y, 128, 52f, 8f));
            Make("Glow", 128, 128, 0, (x, y) => Glow(x, y, 128));
            Make("Rays", 256, 256, 0, (x, y) => Rays(x, y, 256));
            Make("Crosshair", 64, 64, 0, (x, y) => Crosshair(x, y, 64));
            Make("KeyCap", 48, 48, 14, (x, y) => RoundRect(x, y, 48, 48, 10, 2.5f, 0.3f, 1f, 0.3f));
            Make("Dot", 32, 32, 0, (x, y) => Dot(x, y, 32));
            ImportIcons();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- shapes (return grayscale value and alpha)

        private static Color RoundRect(float x, float y, int w, int h, float r, float rim, float fillTop, float rimValue, float fillBottom)
        {
            float d = SdRoundRect(x - w / 2f, y - h / 2f, w / 2f - 1f, h / 2f - 1f, r);
            float alpha = Mathf.Clamp01(0.5f - d);
            float t = y / h;
            float fill = Mathf.Lerp(fillBottom, fillTop, t);
            float v = rim > 0f && d > -rim ? rimValue : fill;
            return new Color(v, v, v, alpha);
        }

        private static Color CardFrame(float x, float y, int w, int h)
        {
            float d = SdRoundRect(x - w / 2f, y - h / 2f, w / 2f - 1f, h / 2f - 1f, 20f);
            float alpha = Mathf.Clamp01(0.5f - d);
            float v = d > -3f ? 1f : d > -7f ? 0.25f : d > -9f ? 0.8f : Mathf.Lerp(0.42f, 0.62f, y / h);
            return new Color(v, v, v, alpha);
        }

        private static Color Diamond(float x, float y, int s)
        {
            float c = s / 2f;
            float d = Mathf.Abs(x - c) + Mathf.Abs(y - c) - (c - 2f);
            float alpha = Mathf.Clamp01(0.5f - d * 0.7f);
            float v = d > -5f ? 1f : d > -9f ? 0.3f : Mathf.Lerp(0.45f, 0.7f, y / s);
            return new Color(v, v, v, alpha);
        }

        private static Color Ring(float x, float y, int s, float radius, float width)
        {
            float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) - radius) - width / 2f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
        }

        private static Color Glow(float x, float y, int s)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) / (s / 2f);
            float a = Mathf.Clamp01(1f - d);
            return new Color(1f, 1f, 1f, a * a);
        }

        private static Color Rays(float x, float y, int s)
        {
            Vector2 p = new Vector2(x - s / 2f, y - s / 2f);
            float r = p.magnitude / (s / 2f);
            float angle = Mathf.Atan2(p.y, p.x);
            float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 6f)), 6f);
            float a = ray * Mathf.Clamp01(1f - r) * Mathf.Clamp01(r * 4f);
            return new Color(1f, 1f, 1f, a);
        }

        private static Color Crosshair(float x, float y, int s)
        {
            float c = s / 2f;
            float dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
            bool tick = (dx < 1.6f && dy > 9f && dy < 22f) || (dy < 1.6f && dx > 9f && dx < 22f);
            bool dot = dx * dx + dy * dy < 4f;
            bool shadow = (dx < 3.2f && dy > 7.5f && dy < 23.5f) || (dy < 3.2f && dx > 7.5f && dx < 23.5f) || dx * dx + dy * dy < 9f;
            if (tick || dot) return Color.white;
            return shadow ? new Color(0f, 0f, 0f, 0.6f) : Color.clear;
        }

        private static Color Dot(float x, float y, int s)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f)) - (s / 2f - 2f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
        }

        private static float SdRoundRect(float px, float py, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px) - hx + r, qy = Mathf.Abs(py) - hy + r;
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // ---------------------------------------------------------------- files

        private static void Make(string name, int w, int h, int border, Func<float, float, Color> shade)
        {
            string path = $"{SkinFolder}/UI_{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, shade(x + 0.5f, y + 0.5f));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            SpriteImport(path, border);
        }

        private static void ImportIcons()
        {
            if (!AssetDatabase.IsValidFolder(IconFolder)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
                SpriteImport(AssetDatabase.GUIDToAssetPath(guid), 0);
        }

        private static void SpriteImport(string path, int border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }
    }
}
