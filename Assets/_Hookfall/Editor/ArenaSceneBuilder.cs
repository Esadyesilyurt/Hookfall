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
using Hookfall.Audio;
using Hookfall.VFX;
using Hookfall.Feel;
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
            FeelSetup.EnablePostProcessing(cam);
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 14f, -46f), Quaternion.Euler(8f, 0f, 0f));

            var generator = new GameObject("IslandGenerator").AddComponent<IslandGenerator>();
            WireIslandGenerator(generator, null);
            AddArenaEdge(null);
            FeelSetup.AddVolume();

            var menu = new GameObject("Main Menu").AddComponent<MainMenuUI>();
            foreach (CharacterDefinition character in CreateCharacters()) AddToList(menu, "characters", character);
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
            List<ItemBase> itemPrefabs = ItemPrefabs.CreateItems();
            Chest chestPrefab = ItemPrefabs.CreateChest();

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
            player.AddComponent<SynergySystem>();   // set bonuses and school statuses (2026-11-08)
            var levelUp = player.AddComponent<LevelUpSystem>();

            var ropeGo = new GameObject("Rope") { layer = Layers.NoOutline };
            ropeGo.transform.SetParent(player.transform, false);
            var rope = ropeGo.AddComponent<LineRenderer>();
            rope.sharedMaterial = Mat("M_PlayerGlow");
            rope.startWidth = 0.06f;
            rope.endWidth = 0.06f;
            rope.positionCount = 2;
            rope.shadowCastingMode = ShadowCastingMode.Off;
            rope.enabled = false;

            var hookHead = new GameObject("Hook Head");
            if (InstantiateModel("Hook", hookHead.transform) == null)
                Part(hookHead.transform, "Visual", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.35f, Mat("M_PlayerGlow"));
            Layers.SetRecursive(hookHead, Layers.NoOutline);
            var hookTrail = hookHead.AddComponent<TrailRenderer>();
            hookTrail.sharedMaterial = ToonSetup.ParticleMaterial();
            hookTrail.time = 0.12f;
            hookTrail.startWidth = 0.25f;
            hookTrail.endWidth = 0f;
            hookTrail.startColor = new Color(0.3f, 0.95f, 1f, 0.8f);
            hookTrail.endColor = new Color(0.3f, 0.95f, 1f, 0f);

            // Every character's model, each with its own animator; CharacterLoadout shows the selected one.
            CharacterDefinition[] characters = CreateCharacters();
            var loadout = player.AddComponent<CharacterLoadout>();
            foreach (CharacterDefinition character in characters)
            {
                AddToList(loadout, "characters", character);
                AddToList(loadout, "models", AddWarden(player, controller, hook, character.id));
            }

            var itemsRoot = new GameObject("Items");
            itemsRoot.transform.SetParent(player.transform, false);
            // The run starts with Orbit Blades (GDD 4.5); the other items come from chests.
            var blades = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefabs[0].gameObject, itemsRoot.transform);
            blades.name = itemPrefabs[0].name;
            SetRef(combat, "itemsRoot", itemsRoot.transform);
            SetRef(combat, "player", controller);
            foreach (ItemBase item in itemPrefabs) AddToList(combat, "itemPrefabs", item);

            // Camera: Cinemachine orbit camera driven by CameraRig.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            FeelSetup.EnablePostProcessing(cam);
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
            orbit.VerticalAxis.Range = new Vector2(-80f, 80f);   // -80: the camera dips under the Warden to look straight up
            orbit.VerticalAxis.Value = 15f;
            orbit.VerticalAxis.Center = 15f;
            var composer = vcamGo.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = new Vector3(0f, 1.6f, 0f);
            composer.Damping = Vector2.zero;
            // Islands, the tower and props block the camera: it slides in towards the Warden instead of going inside them.
            var deoccluder = vcamGo.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = 1 << Layers.World;
            deoccluder.IgnoreTag = "Player";
            deoccluder.MinimumDistanceFromTarget = 0.6f;
            deoccluder.AvoidObstacles.Enabled = true;
            deoccluder.AvoidObstacles.DistanceLimit = 0f;
            deoccluder.AvoidObstacles.CameraRadius = 0.45f;
            deoccluder.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            deoccluder.AvoidObstacles.SmoothingTime = 0.1f;
            deoccluder.AvoidObstacles.Damping = 0.35f;
            deoccluder.AvoidObstacles.DampingWhenOccluded = 0f;
            var rig = vcamGo.AddComponent<CameraRig>();
            vcamGo.AddComponent<CinemachineImpulseListener>();
            var impulse = vcamGo.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseDuration = 0.18f;
            SetRef(rig, "targetBody", body);

            SetRef(controller, "cameraTransform", camGo.transform);
            foreach (WardenAnimator wardenAnimator in player.GetComponentsInChildren<WardenAnimator>(true))
                SetRef(wardenAnimator, "cameraTransform", camGo.transform);
            SetRef(hook, "aimSource", camGo.transform);
            SetRef(hook, "rope", rope);
            SetRef(hook, "hookHead", hookHead.transform);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(gm, "playerXP", xp);

            var generator = new GameObject("IslandGenerator").AddComponent<IslandGenerator>();
            WireIslandGenerator(generator, stats);
            AddArenaEdge(body);

            // Pickups
            var pickups = new GameObject("Pickups").AddComponent<PickupSpawner>();
            SetRef(pickups, "player", stats);
            SetRef(pickups, "smallGem", GetOrCreatePickup("PF_XPGem_Small", PickupKind.XP, 0.35f));
            SetRef(pickups, "mediumGem", GetOrCreatePickup("PF_XPGem_Medium", PickupKind.XP, 0.5f));
            SetRef(pickups, "largeGem", GetOrCreatePickup("PF_XPGem_Large", PickupKind.XP, 0.7f));
            SetRef(pickups, "coin", GetOrCreatePickup("PF_GoldCoin", PickupKind.Gold, 0.5f));

            // Chests, tower cache and the low-gravity zone (GDD 4.7, 7.1, 7.2)
            var chests = new GameObject("ChestSpawner").AddComponent<ChestSpawner>();
            SetRef(chests, "player", controller);
            SetRef(chests, "chestPrefab", chestPrefab);
            var cache = new GameObject("TowerCache").AddComponent<TowerCache>();
            SetRef(cache, "player", controller);
            SetRef(cache, "islands", generator);
            ItemPrefabs.BuildCacheVisuals(cache);
            var gravity = new GameObject("GravityZone").AddComponent<GravityZone>();
            SetRef(gravity, "target", body);

            // Enemies
            var director = new GameObject("EnemyDirector").AddComponent<EnemyDirector>();
            SetRef(director, "player", controller);
            SetRef(director, "islands", generator);
            SetRef(director, "batPrefab", batPrefab);
            SetRef(director, "wyvernPrefab", wyvernPrefab);
            SetRef(director, "golemPrefab", golemPrefab);
            SetRef(director, "boulderPrefab", boulderPrefab);

            // Effects and sound
            var vfx = new GameObject("VFX").AddComponent<VfxManager>();
            SetRef(vfx, "particleMaterial", ToonSetup.ParticleMaterial());
            SetRef(vfx, "player", controller);
            SetRef(vfx, "cameraTransform", camGo.transform);
            SetRef(vfx, "pickups", pickups);
            SetRef(vfx, "chests", chests);
            SetRef(vfx, "towerCache", cache);
            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            SetRef(audio, "player", controller);
            SetRef(audio, "hook", hook);
            SetRef(audio, "pickups", pickups);
            SetRef(audio, "chests", chests);
            SetRef(audio, "towerCache", cache);
            SetRef(audio, "levelUp", levelUp);
            AudioWiring.AssignClips(audio);

            // Open-sky features between the islands: updrafts, air channels, refill crystals, hook anchors.
            var sky = new GameObject("Sky Features").AddComponent<SkyFeatures>();
            SetRef(sky, "islands", generator);
            SetRef(sky, "player", controller);
            SetRef(sky, "audioManager", audio);
            SetRef(sky, "vfx", vfx);
            SetRef(sky, "particleMaterial", ToonSetup.ParticleMaterial());
            SetRef(sky, "crystalMaterial", Mat("M_Crystal"));
            SetRef(sky, "anchorModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hookfall/Prefabs/Islands/PF_Rock_1.prefab"));

            // Game feel
            Volume postFx = FeelSetup.AddVolume();
            var feel = new GameObject("Game Feel").AddComponent<FeelDirector>();
            SetRef(feel, "player", controller);
            SetRef(feel, "hook", hook);
            SetRef(feel, "combat", combat);
            SetRef(feel, "levelUp", levelUp);
            SetRef(feel, "cameraRig", rig);
            SetRef(feel, "impulse", impulse);
            SetRef(feel, "volume", postFx);
            SetRef(feel, "vfx", vfx);
            var numbers = new GameObject("Damage Numbers").AddComponent<DamageNumbers>();
            SetRef(numbers, "fontMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat"));

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
            EnsureFolder(Root + "/Prefabs/World");
            ItemPrefabs.CreateMaterials();
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
            ModelSetup.Run();
            ToonSetup.Run();
            FeelSetup.BoostGlow();
            UISetup.Run();
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
            RenderSettings.fogColor = SkyColor;   // distant islands melt into the sky haze (GDD 8.1)
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;
        }

        private static void WireIslandGenerator(IslandGenerator generator, PlayerStats stats)
        {
            SetRef(generator, "grassMaterial", Mat("M_Grass"));
            SetRef(generator, "stoneMaterial", Mat("M_Stone"));
            SetRef(generator, "towerMaterial", Mat("M_Tower"));
            if (stats != null) SetRef(generator, "player", stats);

            // Blender models (Tools/Blender/hookfall_models.py); the generator falls back to primitives without them.
            SetList(generator, "largeIslandModels", ModelSetup.IslandPrefabs("Large"));
            SetList(generator, "smallIslandModels", ModelSetup.IslandPrefabs("Small"));
            SetList(generator, "propModels", ModelSetup.PropPrefabs());
            SetList(generator, "cloudModels", ModelSetup.CloudPrefabs());
            SetRef(generator, "towerModel", ModelSetup.TowerPrefab());
        }

        /// <summary>The storm wall that closes the arena, and the far scenery beyond it.</summary>
        private static void AddArenaEdge(Rigidbody body)
        {
            var storm = new GameObject("StormWall").AddComponent<StormWall>();
            if (body != null) SetRef(storm, "player", body);
            SetList(storm, "cloudPrefabs", ModelSetup.StormCloudPrefabs());
            SetRef(storm, "lightningMaterial", ToonSetup.ParticleMaterial());
            var backdrop = new GameObject("Backdrop").AddComponent<Backdrop>();
            SetList(backdrop, "prefabs", ModelSetup.BackdropPrefabs());
        }

        private static void SetList(Object target, string field, GameObject[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(field);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A character model replaces the capsule's renderer; WardenAnimator moves its parts (all models share them).</summary>
        private static GameObject AddWarden(GameObject player, PlayerController controller, GrappleHook hook, string model)
        {
            GameObject warden = InstantiateModel(model, player.transform);
            if (warden == null) return null;
            warden.name = model;
            warden.transform.localPosition = new Vector3(0f, -1f, 0f);   // capsule centre to feet
            if (player.TryGetComponent(out MeshRenderer capsuleRenderer)) Object.DestroyImmediate(capsuleRenderer);
            if (player.TryGetComponent(out MeshFilter capsuleMesh)) Object.DestroyImmediate(capsuleMesh);
            var animator = warden.AddComponent<WardenAnimator>();
            SetRef(animator, "controller", controller);
            SetRef(animator, "hook", hook);
            SetRef(animator, "hookArm", EnemyPrefabs.FindDeep(warden.transform, "Arm_R"));
            SetRef(animator, "otherArm", EnemyPrefabs.FindDeep(warden.transform, "Arm_L"));
            SetRef(animator, "legLeft", EnemyPrefabs.FindDeep(warden.transform, "Leg_L"));
            SetRef(animator, "legRight", EnemyPrefabs.FindDeep(warden.transform, "Leg_R"));
            SetRef(animator, "cape", EnemyPrefabs.FindDeep(warden.transform, "Cape"));
            SetRef(animator, "scarf", EnemyPrefabs.FindDeep(warden.transform, "Scarf"));
            SetRef(animator, "head", EnemyPrefabs.FindDeep(warden.transform, "Head"));
            return warden;
        }

        /// <summary>The four characters as SO_Character_ assets (rewritten on every build: tune them here).</summary>
        private static CharacterDefinition[] CreateCharacters()
        {
            EnsureFolder(Root + "/Data");
            ItemBase Item(string id) => AssetDatabase.LoadAssetAtPath<ItemBase>($"{Root}/Prefabs/Items/PF_Item_{id}.prefab");
            CharacterDefinition Make(string id, string name, string description, School affinity, float health, StatType[] picks,
                ItemBase[] items, int jumps, int dashes, UnlockKind unlock, float target, string unlockText, params string[] perks)
            {
                string path = $"{Root}/Data/SO_Character_{id}.asset";
                var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (c == null)
                {
                    c = ScriptableObject.CreateInstance<CharacterDefinition>();
                    AssetDatabase.CreateAsset(c, path);
                }
                c.id = id;
                c.displayName = name;
                c.description = description;
                c.maxHealthMultiplier = health;
                c.statPicks = picks;
                c.startingItems = items;
                c.airJumps = jumps;
                c.airDashes = dashes;
                c.unlock = unlock;
                c.unlockTarget = target;
                c.unlockText = unlockText;
                c.perks = perks;
                c.affinity = affinity;
                EditorUtility.SetDirty(c);
                return c;
            }

            var all = new[]
            {
                Make("Warden", "Warden", "A Warden of the Order of the Tether: steady in every way; frost-edged blades keep foes at bay.", School.Guardian, 1f,
                    new StatType[0], new[] { Item("OrbitBlades") }, 1, 1, UnlockKind.None, 0f, "",
                    "Balanced in everything", "1 double jump, 1 dash"),
                Make("Skyrunner", "Skyrunner", "A scout of the storm roads: fastest in the air, two dashes, a bow that makes foes bleed; fragile.", School.Wild, 0.8f,
                    new[] { StatType.FlightSpeed, StatType.FlightSpeed, StatType.WalkSpeed }, new[] { Item("SkyboltCrossbow"), Item("GaleFeather") },
                    1, 2, UnlockKind.SurviveSeconds, 300f, "Hold the watch for 5:00 in one run",
                    "+20% Max flight speed", "+8% Walk speed", "2 dashes per flight"),
                Make("Bulwark", "Bulwark", "A knight of the vault gates: heavy plate, a turtle shell, and the most health.", School.Guardian, 1.5f,
                    new[] { StatType.Armor, StatType.Armor }, new[] { Item("OrbitBlades"), Item("WardShell") },
                    1, 1, UnlockKind.TotalKills, 1500f, "Defeat 1,500 enemies over all runs",
                    "+2 Armor", "Starts with a Turtle Spirit Shell"),
                Make("Lumen", "Lumen", "A mystic of the Heartstone: arcane volleys that hit harder and faster; jumps twice.", School.Arcane, 0.9f,
                    new[] { StatType.Damage, StatType.AttackSpeed }, new[] { Item("HomingSparks"), Item("SagesLens") },
                    2, 1, UnlockKind.ReachLevel, 15f, "Reach level 15 in one run",
                    "+10% Damage", "+8% Attack speed", "2 double jumps per flight"),
                Make("EmberWitch", "Ember Witch", "A witch of the cinder peaks: her salamander sets the sky alight, and her fires burn hotter.", School.Flame, 0.9f,
                    new[] { StatType.Damage }, new[] { Item("SalamanderFamiliar"), Item("EmberBrazier") },
                    1, 1, UnlockKind.ReachLevel, 10f, "Reach level 10 in one run",
                    "+10% Damage", "Burns 25% hotter from the start"),
                Make("FrostSeer", "Frost Seer", "A seer of the frozen north: her breath slows the horde, and cold bites deeper.", School.Frost, 1.1f,
                    new[] { StatType.Armor }, new[] { Item("FrostWyrmBreath"), Item("FrostbiteRune") },
                    1, 1, UnlockKind.SurviveSeconds, 420f, "Hold the watch for 7:00 in one run",
                    "+1 Armor", "Chilled foes take more damage"),
                Make("StormHerald", "Storm Herald", "A herald of the thunder giants: every bell toll shocks the sky, and two dashes keep him moving.", School.Storm, 1f,
                    new[] { StatType.FlightSpeed }, new[] { Item("ThunderingBell"), Item("LightningRod") },
                    1, 2, UnlockKind.TotalKills, 4000f, "Defeat 4,000 enemies over all runs",
                    "+10% Max flight speed", "2 dashes per flight"),
            };
            AssetDatabase.SaveAssets();
            return all;
        }

        private static Pickup GetOrCreatePickup(string name, PickupKind kind, float size)
        {
            string path = $"{Root}/Prefabs/Items/{name}.prefab";
            bool gem = kind == PickupKind.XP;
            var root = new GameObject(name);
            GameObject visual = InstantiateModel(gem ? "XPGem" : "Coin", root.transform);
            if (visual != null) visual.transform.localScale = Vector3.one * (gem ? size * 2f : size * 2.2f);
            else visual = Part(root.transform, "Visual", gem ? PrimitiveType.Cube : PrimitiveType.Cylinder, Vector3.zero,
                gem ? Vector3.one * size : new Vector3(size, 0.06f, size), Mat(gem ? "M_XPGem" : "M_Gold"),
                gem ? new Vector3(45f, 0f, 45f) : new Vector3(90f, 0f, 0f));
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            var pickup = root.AddComponent<Pickup>();
            SetEnum(pickup, "kind", (int)kind);
            return SavePrefab(root, path).GetComponent<Pickup>();
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
