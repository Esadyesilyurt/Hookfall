// Hookfall - editor tool: menu "Hookfall > Build Arena Scene" creates the playable test scene,
// its placeholder materials and the Bat prefab, and wires every reference.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Combat.Items;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using Hookfall.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.EditorTools
{
    public static class ArenaSceneBuilder
    {
        private const string Root = "Assets/_Hookfall";
        private const string ScenePath = Root + "/Scenes/Arena.unity";

        [MenuItem("Hookfall/Build Arena Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root + "/Art/Materials");
            EnsureFolder(Root + "/Prefabs/Enemies");
            EnsureFolder(Root + "/Scenes");

            // Placeholder materials in the GDD palette (section 8.1).
            Material grass = GetOrCreateMaterial("M_Grass", new Color(0.42f, 0.58f, 0.36f), false);
            Material stone = GetOrCreateMaterial("M_Stone", new Color(0.55f, 0.52f, 0.5f), false);
            Material tower = GetOrCreateMaterial("M_Tower", new Color(0.72f, 0.7f, 0.66f), false);
            Material playerMat = GetOrCreateMaterial("M_Player", new Color(0.2f, 0.75f, 0.8f), false);
            Material batMat = GetOrCreateMaterial("M_Bat", new Color(0.55f, 0.2f, 0.6f), false);
            Material glow = GetOrCreateMaterial("M_PlayerGlow", new Color(0.35f, 0.95f, 1f), true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Bat batPrefab = GetOrCreateBatPrefab(batMat);

            // Light, sky and abyss haze: calm, soft daylight.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.82f, 0.92f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;

            new GameObject("GameManager").AddComponent<GameManager>();

            var generator = new GameObject("IslandGenerator").AddComponent<IslandGenerator>();
            SetRef(generator, "grassMaterial", grass);
            SetRef(generator, "stoneMaterial", stone);
            SetRef(generator, "towerMaterial", tower);

            // Player
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.6f, -7f);
            player.GetComponent<Renderer>().sharedMaterial = playerMat;
            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            var controller = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerStats>();
            var hook = player.AddComponent<GrappleHook>();
            player.AddComponent<XPSystem>();
            player.AddComponent<GoldWallet>();
            player.AddComponent<LevelUpSystem>();
            var combat = player.AddComponent<CombatSystem>();

            var rope = player.AddComponent<LineRenderer>();
            rope.sharedMaterial = glow;
            rope.startWidth = 0.06f;
            rope.endWidth = 0.06f;
            rope.positionCount = 2;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.enabled = false;

            var itemsRoot = new GameObject("Items");
            itemsRoot.transform.SetParent(player.transform, false);
            var blades = itemsRoot.AddComponent<OrbitBlades>();
            SetRef(blades, "bladeMaterial", glow);

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.76f, 0.93f);
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = player.transform.position + new Vector3(0f, 3f, -8f);
            var follow = camGo.AddComponent<ThirdPersonCamera>();
            SetRef(follow, "target", player.transform);
            SetRef(follow, "targetBody", body);
            SetRef(controller, "cameraTransform", camGo.transform);
            SetRef(hook, "aimSource", camGo.transform);
            SetRef(hook, "rope", rope);
            SetRef(combat, "player", controller);

            // Enemies
            var director = new GameObject("EnemyDirector").AddComponent<EnemyDirector>();
            SetRef(director, "batPrefab", batPrefab);
            SetRef(director, "player", controller);

            // HUD
            var hud = new GameObject("HUD").AddComponent<HUD>();
            SetRef(hud, "stats", player.GetComponent<PlayerStats>());
            SetRef(hud, "xp", player.GetComponent<XPSystem>());
            SetRef(hud, "gold", player.GetComponent<GoldWallet>());
            SetRef(hud, "combat", combat);
            SetRef(hud, "hook", hook);
            SetRef(hud, "director", director);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PutSceneFirstInBuild(ScenePath);
            Debug.Log("[Hookfall] Arena scene built at " + ScenePath + ". Press Play.");
        }

        private static Bat GetOrCreateBatPrefab(Material material)
        {
            const string path = Root + "/Prefabs/Enemies/PF_Bat.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                temp.name = "PF_Bat";
                temp.transform.localScale = Vector3.one * 0.8f;
                temp.GetComponent<Collider>().isTrigger = true;   // bats fly through each other and bite by distance
                temp.GetComponent<Renderer>().sharedMaterial = material;
                temp.AddComponent<Bat>();

                PrefabUtility.SaveAsPrefabAsset(temp, path);
                Object.DestroyImmediate(temp);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            // Always load the saved asset from disk, so the scene references the real prefab.
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Bat bat = asset != null ? asset.GetComponent<Bat>() : null;
            if (bat == null) Debug.LogError("[Hookfall] Could not load the Bat component from " + path);
            return bat;
        }

        private static Material GetOrCreateMaterial(string name, Color color, bool unlit)
        {
            string path = $"{Root}/Art/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[Hookfall] Field '{field}' not found on {target.GetType().Name}.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (value != null && property.objectReferenceValue == null)
                Debug.LogError($"[Hookfall] Could not assign {value.name} to '{field}' on {target.GetType().Name}.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        private static void PutSceneFirstInBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path != path) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
