// Hookfall - editor setup for game feel: the post-processing profile (bloom, vignette, colour, chromatic aberration),
// HDR glow on the materials that should bloom, and the global volume and camera settings.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class FeelSetup
    {
        private const string ProfilePath = Root + "/Art/PostFX/PP_Hookfall.asset";

        /// <summary>Glowing materials (the player's cyan, rewards, enemy eyes, crystals) get HDR colour so they bloom.</summary>
        private static readonly string[] GlowMaterials =
            { "M_PlayerGlow", "M_Heartstone", "M_Crystal", "M_EnemyEye", "M_CacheGlow", "M_XPGem", "M_Shard", "M_Spark", "M_Beam", "M_Warning" };
        private const float GlowBoost = 2.2f;

        public static void BoostGlow()
        {
            foreach (string name in GlowMaterials)
            {
                Material m = Mat(name);
                if (m == null || !m.HasProperty("_BaseColor")) continue;
                Color c = m.GetColor("_BaseColor");
                if (c.maxColorComponent > 1.01f) continue;   // already boosted
                m.SetColor("_BaseColor", new Color(c.r * GlowBoost, c.g * GlowBoost, c.b * GlowBoost, c.a));
                EditorUtility.SetDirty(m);
            }
        }

        public static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile != null) return profile;
            EnsureFolder(Root + "/Art/PostFX");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.9f);
            bloom.scatter.Override(0.65f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);
            var chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.Override(0f);
            var colour = profile.Add<ColorAdjustments>(true);
            colour.saturation.Override(8f);
            colour.contrast.Override(6f);
            foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        public static Volume AddVolume()
        {
            var volume = new GameObject("Post FX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = Profile();
            return volume;
        }

        public static void EnablePostProcessing(Camera camera)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camera.allowHDR = true;
        }
    }
}
