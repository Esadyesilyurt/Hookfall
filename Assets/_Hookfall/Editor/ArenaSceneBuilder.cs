// Hookfall - editor tool: menu "Hookfall > Build All Scenes" creates the MainMenu and Arena scenes,
// their materials and prefabs, and wires every reference. "Build Arena Scene" rebuilds only the arena.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (Cinemachine, uGUI screens, pickups, main menu, all enemies). Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Combat.Items;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using Hookfall.World;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class ArenaSceneBuilder
    {
        private const string ArenaPath = Root + "/Scenes/Arena.unity";
        private const string MenuPath = Root + "/Scenes/MainMenu.unity";
        private const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        private static readonly Color SkyColor = new Color(0.55f, 0.76f, 0.93f);

        [MenuItem("Hookfall/Build All Scenes")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMenu();
            BuildArenaScene();
            SetBuildScenes();
            EditorSceneManager.OpenScene(ArenaPath);
        }

        [MenuItem("Hookfall/Build Arena Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildArenaScene();
            SetBuildScenes();
        }

        /// <summary>Entry point for batch mode: -executeMethod Hookfall.EditorTools.ArenaSceneBuilder.BatchBuild</summary>
        public static void BatchBuild()
        {
            BuildMenu();
            BuildArenaScene();
            SetBuildScenes();
        }

        // ---------------------------------------------------------------- scenes

        private static void BuildMenu()
        {
            Prepare();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddLightAndSky();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 14f, -46f), Quaternion.Euler(8f, 0f, 0f));

            var generator = new GameObject("IslandGenerator").AddComponent<IslandGenerator>();
            WireIslandGenerator(generator, null);

            new GameObject("Main Menu").AddComponent<MainMenuUI>();
            EditorSceneManager.SaveScene(scene, MenuPath);
        }

        private static void BuildArenaScene()
        {
            Prepare();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddLightAndSky();

            // After NewScene: it unloads assets loaded before it, which would leave these references empty.
            Bat batPrefab = EnemyPrefabs.Bat();
            Wyvern wyvernPrefab = EnemyPrefabs.Wyvern();
            Golem golemPrefab = EnemyPrefabs.Golem();
            Boulder boulderPrefab = EnemyPrefabs.Boulder();

            // Player
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.6f, -7f);
            player.GetComponent<Renderer>().sharedMaterial = Mat("M_Player");
            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            var controller = player.AddComponent<PlayerController>();
            var stats = player.AddComponent<PlayerStats>();
            var hook = player.AddComponent<GrappleHook>();
            var xp = player.AddComponent<XPSystem>();
            var gold = player.AddComponent<GoldWallet>();
            var combat = player.AddComponent<CombatSystem>();
            var levelUp = player.AddComponent<LevelUpSystem>();

            var rope = player.AddComponent<LineRenderer>();
            rope.sharedMaterial = Mat("M_PlayerGlow");
            rope.startWidth = 0.06f;
            rope.endWidth = 0.06f;
            rope.positionCount = 2;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.enabled = false;

            GameObject hookHead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hookHead.name = "Hook Head";
            Object.DestroyImmediate(hookHead.GetComponent<Collider>());
            hookHead.transform.localScale = Vector3.one * 0.35f;
            hookHead.GetComponent<Renderer>().sharedMaterial = Mat("M_PlayerGlow");

            var itemsRoot = new GameObject("Items");
            itemsRoot.transform.SetParent(player.transform, false);
            var blades = itemsRoot.AddComponent<OrbitBlades>();
            SetRef(blades, "bladeMaterial", Mat("M_PlayerGlow"));
            SetString(blades, "displayName", "Orbit Blades");
            SetString(blades, "lore", "Twin vault blades bound to the Warden by a Tether rune; they circle and cut whatever comes close.");
            SetRef(combat, "itemsRoot", itemsRoot.transform);
            SetRef(combat, "player", controller);

            // Camera: Cinemachine orbit camera driven by CameraRig.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();
            camGo.transform.position = player.transform.position + new Vector3(0f, 3f, -8f);

            var vcamGo = new GameObject("Player Camera");
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = player.transform;
            vcam.Lens.FieldOfView = 70f;
            vcam.Lens.NearClipPlane = 0.1f;
            vcam.Lens.FarClipPlane = 600f;
            var orbit = vcamGo.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = 8f;
            orbit.TargetOffset = new Vector3(0f, 1.6f, 0f);
            orbit.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            orbit.TrackerSettings.PositionDamping = new Vector3(0.1f, 0.1f, 0.1f);
            orbit.VerticalAxis.Range = new Vector2(-40f, 75f);
            orbit.VerticalAxis.Value = 15f;
            orbit.VerticalAxis.Center = 15f;
            var composer = vcamGo.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = new Vector3(0f, 1.6f, 0f);
            composer.Damping = Vector2.zero;
            var rig = vcamGo.AddComponent<CameraRig>();
            SetRef(rig, "targetBody", body);

            SetRef(controller, "cameraTransform", camGo.transform);
            SetRef(hook, "aimSource", camGo.transform);
            SetRef(hook, "rope", rope);
            SetRef(hook, "hookHead", hookHead.transform);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(gm, "playerXP", xp);

            var generator = new GameObject("IslandGenerator").AddComponent<IslandGenerator>();
            WireIslandGenerator(generator, stats);

            // Pickups
            var pickups = new GameObject("Pickups").AddComponent<PickupSpawner>();
            SetRef(pickups, "player", stats);
            SetRef(pickups, "smallGem", GetOrCreatePickup("PF_XPGem_Small", PickupKind.XP, 0.35f));
            SetRef(pickups, "mediumGem", GetOrCreatePickup("PF_XPGem_Medium", PickupKind.XP, 0.5f));
            SetRef(pickups, "largeGem", GetOrCreatePickup("PF_XPGem_Large", PickupKind.XP, 0.7f));
            SetRef(pickups, "coin", GetOrCreatePickup("PF_GoldCoin", PickupKind.Gold, 0.5f));

            // Enemies
            var director = new GameObject("EnemyDirector").AddComponent<EnemyDirector>();
            SetRef(director, "player", controller);
            SetRef(director, "islands", generator);
            SetRef(director, "batPrefab", batPrefab);
            SetRef(director, "wyvernPrefab", wyvernPrefab);
            SetRef(director, "golemPrefab", golemPrefab);
            SetRef(director, "boulderPrefab", boulderPrefab);

            // UI
            var hud = new GameObject("HUD").AddComponent<HUD>();
            SetRef(hud, "stats", stats);
            SetRef(hud, "xp", xp);
            SetRef(hud, "gold", gold);
            SetRef(hud, "combat", combat);
            SetRef(hud, "hook", hook);
            var levelUpUI = new GameObject("Level Up UI").AddComponent<LevelUpUI>();
            SetRef(levelUpUI, "levelUp", levelUp);
            SetRef(levelUpUI, "stats", stats);
            new GameObject("Chest Result UI").AddComponent<ChestResultUI>();
            var pause = new GameObject("Pause Menu UI").AddComponent<PauseMenuUI>();
            SetRef(pause, "combat", combat);
            SetRef(pause, "stats", stats);
            var runEnd = new GameObject("Run End UI").AddComponent<RunEndUI>();
            SetRef(runEnd, "xp", xp);
            SetRef(runEnd, "gold", gold);
            SetRef(runEnd, "combat", combat);

            EditorSceneManager.SaveScene(scene, ArenaPath);
            Debug.Log("[Hookfall] Arena scene built at " + ArenaPath + ". Press Play.");
        }

        // ---------------------------------------------------------------- shared pieces

        private static void Prepare()
        {
            EnsureFolder(MaterialsPath);
            EnsureFolder(Root + "/Prefabs/Enemies");
            EnsureFolder(Root + "/Prefabs/Items");
            EnsureFolder(Root + "/Scenes");
            ImportTmpEssentials();

            // Placeholder materials in the GDD palette (section 8.1).
            GetOrCreateMaterial("M_Grass", new Color(0.42f, 0.58f, 0.36f), false);
            GetOrCreateMaterial("M_Stone", new Color(0.55f, 0.52f, 0.5f), false);
            GetOrCreateMaterial("M_Tower", new Color(0.72f, 0.7f, 0.66f), false);
            GetOrCreateMaterial("M_Player", new Color(0.2f, 0.75f, 0.8f), false);
            GetOrCreateMaterial("M_PlayerGlow", new Color(0.35f, 0.95f, 1f), true);
            GetOrCreateMaterial("M_XPGem", new Color(1f, 0.62f, 0.15f), true);
            GetOrCreateMaterial("M_Gold", new Color(1f, 0.85f, 0.3f), true);
            EnemyPrefabs.CreateMaterials();
        }

        private static void AddLightAndSky()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = sun;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.7f, 0.82f, 0.92f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;
        }

        private static void WireIslandGenerator(IslandGenerator generator, PlayerStats stats)
        {
            SetRef(generator, "grassMaterial", Mat("M_Grass"));
            SetRef(generator, "stoneMaterial", Mat("M_Stone"));
            SetRef(generator, "towerMaterial", Mat("M_Tower"));
            if (stats != null) SetRef(generator, "player", stats);
        }

        private static Pickup GetOrCreatePickup(string name, PickupKind kind, float size)
        {
            string path = $"{Root}/Prefabs/Items/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                bool gem = kind == PickupKind.XP;
                var root = new GameObject(name);
                GameObject visual = Part(root.transform, "Visual", gem ? PrimitiveType.Cube : PrimitiveType.Cylinder, Vector3.zero,
                    gem ? Vector3.one * size : new Vector3(size, 0.06f, size), Mat(gem ? "M_XPGem" : "M_Gold"),
                    gem ? new Vector3(45f, 0f, 45f) : new Vector3(90f, 0f, 0f));
                visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var pickup = root.AddComponent<Pickup>();
                SetEnum(pickup, "kind", (int)kind);
                SavePrefab(root, path);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Pickup>();
        }

        private static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
            AssetDatabase.ImportPackage(TmpEssentials, false);
            AssetDatabase.Refresh();
        }

        private static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            if (System.IO.File.Exists(MenuPath)) scenes.Add(new EditorBuildSettingsScene(MenuPath, true));
            scenes.Add(new EditorBuildSettingsScene(ArenaPath, true));
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
                if (s.path != MenuPath && s.path != ArenaPath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
