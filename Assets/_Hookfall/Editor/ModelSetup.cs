// Hookfall - imports the Blender models (Tools/Blender/hookfall_models.py) and builds the world prefabs from them:
// toon materials for every model material name, import settings, island and tower prefabs with mesh colliders,
// props and clouds. Menu: Hookfall > Import Models. The scene builder runs it too.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class ModelSetup
    {
        private const string WorldPrefabs = Root + "/Prefabs/Islands";

        /// <summary>Model material names and colours (same palette as the Blender script); glowing ones are unlit.</summary>
        private static readonly (string name, Color color, bool unlit)[] Materials =
        {
            ("M_Grass", new Color(0.42f, 0.58f, 0.36f), false), ("M_Stone", new Color(0.55f, 0.52f, 0.5f), false),
            ("M_Moss", new Color(0.36f, 0.5f, 0.3f), false), ("M_Tower", new Color(0.72f, 0.7f, 0.66f), false),
            ("M_TowerDark", new Color(0.16f, 0.14f, 0.2f), false), ("M_Wood", new Color(0.48f, 0.33f, 0.2f), false),
            ("M_Heartstone", new Color(0.4f, 0.95f, 1f), true), ("M_Crystal", new Color(0.55f, 0.7f, 1f), true),
            ("M_Cloud", new Color(0.96f, 0.97f, 1f), false), ("M_Bat", new Color(0.42f, 0.16f, 0.48f), false),
            ("M_Wyvern", new Color(0.3f, 0.14f, 0.38f), false), ("M_WyvernBelly", new Color(0.7f, 0.28f, 0.62f), false),
            ("M_Golem", new Color(0.33f, 0.31f, 0.34f), false), ("M_EnemyEye", new Color(1f, 0.25f, 0.8f), true),
            ("M_ChestWood", new Color(0.45f, 0.28f, 0.15f), false), ("M_ChestTrim", new Color(1f, 0.72f, 0.2f), false),
            ("M_CacheGlow", new Color(1f, 0.85f, 0.4f), true), ("M_XPGem", new Color(1f, 0.62f, 0.15f), true),
            ("M_Gold", new Color(1f, 0.85f, 0.3f), true), ("M_WardenCloth", new Color(0.12f, 0.42f, 0.46f), false),
            ("M_WardenCape", new Color(0.08f, 0.28f, 0.32f), false), ("M_WardenArmor", new Color(0.42f, 0.45f, 0.5f), false),
            ("M_WardenSkin", new Color(0.85f, 0.66f, 0.52f), false), ("M_PlayerGlow", new Color(0.35f, 0.95f, 1f), true),
            ("M_GrassTuft", new Color(0.52f, 0.68f, 0.4f), false), ("M_Flower", new Color(0.96f, 0.86f, 0.48f), true),
            ("M_Water", new Color(0.62f, 0.86f, 0.96f), true), ("M_StormCloud", new Color(0.4f, 0.44f, 0.56f), false),
            ("M_Root", new Color(0.33f, 0.37f, 0.24f), false),
            ("M_Banner", new Color(0.15f, 0.62f, 0.7f), false), ("M_BatBelly", new Color(0.78f, 0.36f, 0.7f), false),
            ("M_Fang", new Color(0.95f, 0.93f, 0.85f), false), ("M_Bush", new Color(0.36f, 0.55f, 0.32f), false),
            ("M_Chain", new Color(0.3f, 0.31f, 0.34f), false),
            // The characters (2026-11-08).
            ("M_ScoutCloth", new Color(0.8f, 0.64f, 0.4f), false), ("M_ScoutLeather", new Color(0.42f, 0.27f, 0.18f), false),
            ("M_ScoutScarf", new Color(0.96f, 0.46f, 0.2f), false), ("M_BulwarkPlate", new Color(0.64f, 0.66f, 0.72f), false),
            ("M_BulwarkCloth", new Color(0.58f, 0.18f, 0.2f), false), ("M_LumenRobe", new Color(0.38f, 0.28f, 0.64f), false),
            ("M_LumenTrim", new Color(0.96f, 0.86f, 0.56f), false),
            ("M_EmberRobe", new Color(0.78f, 0.2f, 0.12f), false), ("M_EmberTrim", new Color(1f, 0.62f, 0.15f), false),
            ("M_FrostRobe", new Color(0.55f, 0.78f, 0.95f), false), ("M_FrostFur", new Color(0.95f, 0.97f, 1f), false),
            ("M_StormArmor", new Color(0.22f, 0.28f, 0.5f), false), ("M_StormGold", new Color(1f, 0.82f, 0.25f), false),
            // The stage boss (2026-11-08).
            ("M_BossStone", new Color(0.3f, 0.27f, 0.34f), false),
            // The wider horde and the second boss (2026-11-10).
            ("M_Razor", new Color(0.2f, 0.17f, 0.44f), false), ("M_RazorEdge", new Color(0.86f, 0.36f, 0.8f), false),
            ("M_Puff", new Color(0.34f, 0.13f, 0.32f), false), ("M_PuffCore", new Color(1f, 0.42f, 0.16f), true),
            ("M_Wasp", new Color(0.26f, 0.13f, 0.36f), false), ("M_WaspWing", new Color(0.8f, 0.76f, 0.96f), false),
            ("M_Venom", new Color(0.72f, 1f, 0.3f), true),
            ("M_Jelly", new Color(0.62f, 0.44f, 0.86f), false), ("M_JellyDark", new Color(0.36f, 0.22f, 0.58f), false),
            ("M_Wyrm", new Color(0.2f, 0.24f, 0.5f), false), ("M_WyrmBelly", new Color(0.56f, 0.52f, 0.86f), false),
            ("M_WyrmFin", new Color(0.86f, 0.82f, 1f), false), ("M_Lightning", new Color(1f, 0.92f, 0.4f), true),
        };

        /// <summary>Far scenery colours: pale and blue-shifted, so it reads as distant haze (SG_Backdrop has no fog).</summary>
        private static readonly Color BackdropRock = new Color(0.6f, 0.68f, 0.8f);
        private static readonly Color BackdropGrass = new Color(0.62f, 0.76f, 0.78f);

        [MenuItem("Hookfall/Import Models")]
        public static void Run()
        {
            foreach (var (name, color, unlit) in Materials) GetOrCreateMaterial(name, color, unlit);
            BackdropMaterial("M_BackdropRock", BackdropRock);
            BackdropMaterial("M_BackdropGrass", BackdropGrass);
            ToonSetup.ConvertMaterials();
            if (!AssetDatabase.IsValidFolder(ModelsPath)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsPath }))
                ConfigureImporter(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) return;
            string name = Path.GetFileNameWithoutExtension(path);
            bool world = name.StartsWith("Island") || name == "Tower";

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalSmoothingAngle = world || name.StartsWith("Cloud") ? 70f : 50f;
            importer.isReadable = world;   // mesh colliders on islands and the tower
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- world prefabs

        public static GameObject[] IslandPrefabs(string size) => ModelPrefabs($"Island_{size}_", true, false);
        // Props are solid (2026-11-08): the player stands on them and the camera does not pass through them.
        public static GameObject[] PropPrefabs() => Combine(ModelPrefabs("Rock_", true, false), ModelPrefabs("Ruin", true, false),
            ModelPrefabs("Crystals", true, false), ModelPrefabs("Bush", true, false), ModelPrefabs("Lantern", true, false),
            ModelPrefabs("Statue", true, false));
        public static GameObject[] CloudPrefabs() => ModelPrefabs("Cloud_", false, true);
        public static GameObject[] StormCloudPrefabs() => ModelPrefabs("StormCloud_", false, true);
        public static GameObject[] BackdropPrefabs() => ModelPrefabs("Backdrop_", false, true, BackdropFor);

        private static Material BackdropFor(Material source) =>
            Mat(source != null && (source.name.Contains("Grass") || source.name.Contains("Moss")) ? "M_BackdropGrass" : "M_BackdropRock");

        private static void BackdropMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = Shader.Find("Hookfall/SG_Backdrop") ?? Shader.Find(ToonSetup.ToonShader);
            var m = new Material(shader);
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, path);
        }
        public static GameObject TowerPrefab()
        {
            GameObject[] towers = ModelPrefabs("Tower", true, false);
            return towers.Length > 0 ? towers[0] : null;
        }

        /// <summary>One prefab per model whose name starts with prefix: the model plus optional mesh colliders.</summary>
        private static GameObject[] ModelPrefabs(string prefix, bool colliders, bool noOutline, System.Func<Material, Material> remap = null)
        {
            var result = new List<GameObject>();
            if (!AssetDatabase.IsValidFolder(ModelsPath)) return result.ToArray();
            EnsureFolder(WorldPrefabs);
            var names = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsPath }))
            {
                string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (name.StartsWith(prefix)) names.Add(name);
            }
            names.Sort();
            foreach (string name in names)
            {
                var root = new GameObject("PF_" + name);
                GameObject visual = InstantiateModel(name, root.transform);
                foreach (MeshFilter mf in visual.GetComponentsInChildren<MeshFilter>())
                {
                    // Deco (grass tufts, flowers, waterfalls) gets no collider and no ink outline.
                    bool deco = mf.name.StartsWith("Deco") || mf.name.StartsWith("Banners");
                    if (colliders && !deco) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    var r = mf.GetComponent<MeshRenderer>();
                    if (deco) { mf.gameObject.layer = Core.Layers.NoOutline; r.shadowCastingMode = ShadowCastingMode.Off; }
                    if (remap != null)
                    {
                        Material[] mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++) mats[i] = remap(mats[i]);
                        r.sharedMaterials = mats;
                    }
                    if (noOutline)
                    {
                        mf.gameObject.layer = Core.Layers.NoOutline;   // clouds: soft shapes, no ink line
                        r.shadowCastingMode = ShadowCastingMode.Off;
                    }
                }
                result.Add(SavePrefab(root, $"{WorldPrefabs}/PF_{name}.prefab"));
            }
            return result.ToArray();
        }

        private static GameObject[] Combine(params GameObject[][] lists)
        {
            var all = new List<GameObject>();
            foreach (GameObject[] list in lists) all.AddRange(list);
            return all.ToArray();
        }

        /// <summary>Batch-mode check of how the FBX axes arrive: logs where named parts end up.</summary>
        public static void LogOrientation()
        {
            Run();
            foreach (string model in new[] { "Warden", "Bat", "Chest", "Tower", "Island_Large_1" })
            {
                GameObject asset = Model(model);
                if (asset == null) { Debug.Log($"[Hookfall] {model}: missing"); continue; }
                var sb = new System.Text.StringBuilder($"[Hookfall] {model}: root rot {asset.transform.localEulerAngles} ");
                foreach (Transform t in asset.transform)
                {
                    var mf = t.GetComponent<MeshFilter>();
                    sb.Append($"| {t.name} pos {t.localPosition} rot {t.localEulerAngles} scale {t.localScale}" +
                              (mf != null ? $" bounds {mf.sharedMesh.bounds.center}/{mf.sharedMesh.bounds.size}" : ""));
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}
