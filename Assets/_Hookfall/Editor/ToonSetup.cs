// Hookfall - menu "Hookfall > Setup Toon Rendering": switches every lit material to the SG_Toon Shader Graph, makes the
// outline material from SG_Outline, adds a "Toon Outline" Render Objects pass to the URP renderers, and makes the soft
// particle material. Safe to run again. The scene builder runs it too.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.IO;
using Hookfall.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class ToonSetup
    {
        public const string ToonShader = "Hookfall/SG_Toon";
        public const string OutlineShader = "Hookfall/SG_Outline";
        private const string FeatureName = "Toon Outline";
        private static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

        [MenuItem("Hookfall/Setup Toon Rendering")]
        public static void Run()
        {
            EnsureFolder(MaterialsPath);
            ConvertMaterials();
            Material outline = OutlineMaterial();
            foreach (string path in Renderers) AddOutlinePass(path, outline);
            ParticleMaterial();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Every URP/Lit material in Art/Materials becomes toon-shaded; its _BaseColor is kept (same property name).</summary>
        public static void ConvertMaterials()
        {
            Shader toon = Shader.Find(ToonShader);
            if (toon == null) { Debug.LogError("[Hookfall] " + ToonShader + " not found; run Tools/ShaderGraph/make_graphs.js."); return; }
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader.name != "Universal Render Pipeline/Lit") continue;
                Color color = m.GetColor("_BaseColor");
                m.shader = toon;
                m.SetColor("_BaseColor", color);
                m.SetColor("_EmissionColor", Color.black);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
        }

        private static Material OutlineMaterial()
        {
            string path = MaterialsPath + "/M_Outline.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find(OutlineShader));
            m.SetColor("_OutlineColor", new Color(0.11f, 0.09f, 0.14f));
            m.SetFloat("_OutlineWidth", 0.035f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void AddOutlinePass(string rendererPath, Material outline)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (data == null) return;
            foreach (ScriptableRendererFeature f in data.rendererFeatures)
                if (f != null && f.name == FeatureName) return;

            var feature = ScriptableObject.CreateInstance<RenderObjects>();
            feature.name = FeatureName;
            feature.settings.passTag = FeatureName;
            feature.settings.Event = RenderPassEvent.AfterRenderingOpaques;
            feature.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
            // Everything except UI and the NoOutline layer (lines, trails, beams).
            feature.settings.filterSettings.LayerMask = ~((1 << 5) | (1 << Layers.NoOutline));
            feature.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.Material;
            feature.settings.overrideMaterial = outline;
            feature.settings.overrideMaterialPassIndex = 0;

            AssetDatabase.AddObjectToAsset(feature, data);
            data.rendererFeatures.Add(feature);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            var serialized = new SerializedObject(data);
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            EditorUtility.SetDirty(data);
        }

        /// <summary>Soft round particle material for bursts, speed lines and trails.</summary>
        public static Material ParticleMaterial()
        {
            string path = MaterialsPath + "/M_Particle.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetTexture("_BaseMap", SoftDotTexture());
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Texture2D SoftDotTexture()
        {
            string folder = Root + "/Art/Textures";
            string path = folder + "/T_SoftDot.png";
            if (!File.Exists(path))
            {
                EnsureFolder(folder);
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
