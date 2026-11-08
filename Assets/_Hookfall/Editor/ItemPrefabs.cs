// Hookfall - builds the item prefabs (the six GDD 4.5 attack items, the Tether Coil and the passive items added on
// 2026-11-08, each with a rarity), the projectile prefabs, and the chest and tower cache prefabs.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using UnityEditor;
using Hookfall.Combat;
using Hookfall.Combat.Items;
using Hookfall.Player;
using Hookfall.World;
using UnityEngine;
using UnityEngine.Rendering;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class ItemPrefabs
    {
        private const string Folder = Root + "/Prefabs/Items";

        public static void CreateMaterials()
        {
            GetOrCreateMaterial("M_Shard", new Color(0.45f, 1f, 1f), true);
            GetOrCreateMaterial("M_Spark", new Color(0.6f, 1f, 0.95f), true);
            GetOrCreateMaterial("M_Beam", new Color(0.4f, 0.98f, 1f), true);
            Transparent(GetOrCreateMaterial("M_Aura", new Color(0.3f, 0.95f, 1f, 0.18f), true));
            GetOrCreateMaterial("M_ChestWood", new Color(0.45f, 0.28f, 0.15f), false);
            GetOrCreateMaterial("M_ChestTrim", new Color(1f, 0.72f, 0.2f), false);
            GetOrCreateMaterial("M_CacheGlow", new Color(1f, 0.85f, 0.4f), true);
            Transparent(GetOrCreateMaterial("M_CacheBeam", new Color(1f, 0.85f, 0.4f, 0.35f), true));
        }

        /// <summary>All chest items: attack items, then passives; index 0 is Orbit Blades, the starting item.</summary>
        public static List<ItemBase> CreateItems()
        {
            Projectile shard = CreateProjectile("PF_Shard", PrimitiveType.Cube, new Vector3(0.12f, 0.12f, 0.7f), "M_Shard", 0.25f);
            Projectile spark = CreateProjectile("PF_Spark", PrimitiveType.Sphere, Vector3.one * 0.3f, "M_Spark", 0.35f);

            var items = new List<ItemBase>
            {
                Item<OrbitBlades>("OrbitBlades", "Orbit Blades", 10f, 0.5f,
                    "Twin vault blades bound to the Warden by a Tether rune; they circle and cut whatever comes close.",
                    i => SetRef(i, "bladeMaterial", Mat("M_Shard")), Rarity.Common),
                Item<ShardBurst>("ShardBurst", "Shard Burst", 12f, 1.5f,
                    "Splinters of a cracked Heartstone ward that burst outwards whenever the Warden takes flight.",
                    i => SetRef(i, "projectilePrefab", shard), Rarity.Common),
                Item<ArcLaser>("ArcLaser", "Arc Laser", 20f, 2f,
                    "A lens from the fallen archer tower; it bends the Heartstone's light into a sweeping blade.",
                    i => SetRef(i, "beamMaterial", Mat("M_Beam")), Rarity.Epic),
                Item<RamStrike>("RamStrike", "Ram Strike", 0f, 0.5f,
                    "The Order's oldest lesson: a Warden at full speed is the weapon.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura")), Rarity.Rare),
                Item<HomingSparks>("HomingSparks", "Homing Sparks", 15f, 1.2f,
                    "Sparks from the vault forge that still remember who their enemies are.",
                    i => SetRef(i, "projectilePrefab", spark), Rarity.Rare),
                Item<StormAura>("StormAura", "Storm Aura", 4f, 0.25f,
                    "A storm sealed in a guardian's cloak; it crackles around the Warden while they fly.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura")), Rarity.Epic),
                Item<TetherCoil>("TetherCoil", "Tether Coil", 25f, 1f,
                    "A coil of the Order's own tether rope, wound with Heartstone wire; it hauls the Warden through anything in the way.",
                    i => { }, Rarity.Legendary),

                // Passive items (2026-11-08): rarer means more useful.
                Charm("HeartLocket", "Heart Locket", Rarity.Common, "A locket with a pressed Heartstone petal; the heart beats stronger for it.", StatType.MaxHealth),
                Charm("IronHide", "Iron Hide", Rarity.Common, "Plates hammered from the vault doors, light enough to fly in.", StatType.Armor),
                Charm("GaleFeather", "Gale Feather", Rarity.Common, "A feather from the storm's edge that never stops pulling forward.", StatType.FlightSpeed, StatType.WalkSpeed),
                Charm("Lodestone", "Lodestone", Rarity.Common, "A stone that drags every loose shard and coin towards its keeper.", StatType.PickupRadius),
                Charm("MendingMoss", "Mending Moss", Rarity.Common, "Moss from the tower's shaded side; it closes wounds while nobody watches.", StatType.Regen),
                Charm("SagesLens", "Sage's Lens", Rarity.Common, "An archivist's lens that shows what every fight has to teach.", StatType.XPGain),
                Charm("Whetstone", "Whetstone", Rarity.Rare, "Every edge the Warden carries comes away sharper.", StatType.Damage),
                Charm("QuickDraw", "Quick Draw", Rarity.Rare, "An archer's glove from the fallen tower; every item fires sooner.", StatType.AttackSpeed, StatType.AttackSpeed),
                Charm("LuckyCoin", "Lucky Coin", Rarity.Rare, "A coin that always lands on the side of rarer chests.", StatType.Luck, StatType.Luck),
                Item<VampireFang>("VampireFang", "Vampire Fang", 0f, 1f,
                    "A wyvern's fang on a cord; what it cuts, the Warden drinks.", i => { }, Rarity.Rare),
                Item<SoulHarvest>("SoulHarvest", "Soul Harvest", 0f, 1f,
                    "A lantern that keeps a spark of everything the Warden brings down.", i => { }, Rarity.Rare),
                Item<HuntersEye>("HuntersEye", "Hunter's Eye", 0f, 1f,
                    "A bat-hunter's monocle that finds the weak spot in every wing.", i => { }, Rarity.Rare),
                Item<ThornMail>("ThornMail", "Thorn Mail", 15f, 1f,
                    "Mail grown from storm thorns; whatever bites the Warden bleeds for it.", i => { }, Rarity.Rare),
                Item<WardShell>("WardShell", "Ward Shell", 0f, 1f,
                    "A ward of the old Order folded into a bubble that turns one blow aside.",
                    i => SetRef(i, "bubbleMaterial", Mat("M_Aura")), Rarity.Epic),
                Item<WindSigil>("WindSigil", "Wind Sigil", 0f, 1f,
                    "A sigil cut into a storm stone; it gives the Warden a second breath in the air.", i => { }, Rarity.Epic),
                Item<BerserkerBlood>("BerserkerBlood", "Berserker Blood", 0f, 1f,
                    "A vial of golem blood; the closer to falling, the harder the Warden hits.", i => { }, Rarity.Epic),
                Item<PhoenixFeather>("PhoenixFeather", "Phoenix Feather", 0f, 1f,
                    "The last feather of the tower's firebird; once in a long while it refuses to let its keeper die.", i => { }, Rarity.Legendary),
                Item<HeartstoneCore>("HeartstoneCore", "Heartstone Core", 40f, 7f,
                    "A shard of the Heartstone itself; while the Warden flies it pulses with light that burns and heals.", i => { }, Rarity.Legendary),
            };
            return items;
        }

        /// <summary>A passive stat charm: the given stat cards, once per item level.</summary>
        private static StatCharm Charm(string id, string displayName, Rarity rarity, string lore, params StatType[] picks) =>
            Item<StatCharm>(id, displayName, 0f, 1f, lore, charm =>
            {
                var serialized = new SerializedObject(charm);
                SerializedProperty list = serialized.FindProperty("picksPerLevel");
                list.arraySize = picks.Length;
                for (int i = 0; i < picks.Length; i++) list.GetArrayElementAtIndex(i).enumValueIndex = (int)picks[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }, rarity);

        private static T Item<T>(string id, string displayName, float damage, float cooldown, string lore, System.Action<T> extra, Rarity rarity)
            where T : ItemBase
        {
            var root = new GameObject("PF_Item_" + id);
            var item = root.AddComponent<T>();
            SetString(item, "displayName", displayName);
            SetEnum(item, "rarity", (int)rarity);
            SetString(item, "lore", lore);
            SetFloat(item, "baseDamage", damage);
            SetFloat(item, "cooldown", cooldown);
            extra(item);
            return SavePrefab(root, $"{Folder}/PF_Item_{id}.prefab").GetComponent<T>();
        }

        private static Projectile CreateProjectile(string name, PrimitiveType shape, Vector3 scale, string material, float hitRadius)
        {
            var root = new GameObject(name);
            var projectile = root.AddComponent<Projectile>();
            SetFloat(projectile, "hitRadius", hitRadius);
            GameObject visual = Part(root.transform, "Visual", shape, Vector3.zero, scale, Mat(material));
            visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Hookfall.Core.Layers.SetRecursive(root, Hookfall.Core.Layers.NoOutline);
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = Mat(material);
            trail.time = 0.15f;
            trail.startWidth = scale.x * 0.8f;
            trail.endWidth = 0f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            return SavePrefab(root, $"{Folder}/{name}.prefab").GetComponent<Projectile>();
        }

        public static Chest CreateChest()
        {
            var root = new GameObject("PF_Chest");
            var chest = root.AddComponent<Chest>();
            GameObject visual = InstantiateModel("Chest", root.transform);
            Transform lid;
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Base", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), Mat("M_ChestWood"));
                Part(visual.transform, "Band", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.24f, 0.12f, 0.84f), Mat("M_ChestTrim"));
                lid = Pivot(visual.transform, "Lid", new Vector3(0f, 0.7f, -0.4f));
                Part(lid, "Top", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0.4f), new Vector3(1.2f, 0.3f, 0.8f), Mat("M_ChestWood"));
                Part(lid, "Lock", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.82f), new Vector3(0.2f, 0.25f, 0.06f), Mat("M_ChestTrim"));
            }
            else lid = EnemyPrefabs.FindDeep(visual.transform, "Lid");
            SetRef(chest, "lid", lid);

            // A spotlight from above and a visible cone of light, so a chest island is found at a glance.
            var lightGo = new GameObject("Spotlight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, BeamHeight, 0f);
            lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var spot = lightGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 26f;
            spot.innerSpotAngle = 14f;
            spot.range = BeamHeight + 8f;
            spot.intensity = 9f;
            spot.color = new Color(1f, 0.86f, 0.55f);
            spot.shadows = LightShadows.None;
            SetRef(chest, "spot", spot);

            var beam = new GameObject("Light Beam") { layer = Hookfall.Core.Layers.NoOutline };
            beam.transform.SetParent(root.transform, false);
            beam.AddComponent<MeshFilter>().sharedMesh = BeamMesh();
            var beamRenderer = beam.AddComponent<MeshRenderer>();
            beamRenderer.sharedMaterial = BeamMaterial();
            beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
            return SavePrefab(root, $"{Root}/Prefabs/World/PF_Chest.prefab").GetComponent<Chest>();
        }

        private const float BeamHeight = 14f;

        /// <summary>An open cone, narrow at the top and wide at the chest, fading in towards the ground.</summary>
        private static Mesh BeamMesh()
        {
            string path = Root + "/Art/Meshes/MESH_ChestBeam.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            EnsureFolder(Root + "/Art/Meshes");
            const int segments = 24;
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var triangles = new int[segments * 6];
            Color top = new Color(1f, 0.86f, 0.5f, 0f), bottom = new Color(1f, 0.86f, 0.5f, 0.32f);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i] = dir * 0.35f + Vector3.up * BeamHeight;
                vertices[segments + i] = dir * 2.4f + Vector3.up * 0.05f;
                colors[i] = top;
                colors[segments + i] = bottom;
                int n = (i + 1) % segments, t = i * 6;
                triangles[t] = i; triangles[t + 1] = n; triangles[t + 2] = segments + i;
                triangles[t + 3] = n; triangles[t + 4] = segments + n; triangles[t + 5] = segments + i;
            }
            var mesh = new Mesh { name = "MESH_ChestBeam", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Unlit, see-through both ways, coloured by the mesh's vertex colours.</summary>
        private static Material BeamMaterial()
        {
            string path = MaterialsPath + "/M_LightBeam.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);   // additive: light adds up, never darkens
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>The cache box and its beam, as children of the TowerCache object in the scene.</summary>
        public static void BuildCacheVisuals(TowerCache cache)
        {
            GameObject visual = InstantiateModel("TowerCache", cache.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(cache.transform, false);
                Part(visual.transform, "Box", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 1f), Mat("M_ChestTrim"));
                Part(visual.transform, "Gem", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), Vector3.one * 0.5f, Mat("M_CacheGlow"));
            }
            GameObject beam = Part(cache.transform, "Beam", PrimitiveType.Cylinder, new Vector3(0f, 60f, 0f), new Vector3(1.6f, 60f, 1.6f), Mat("M_CacheBeam"));
            beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            SetRef(cache, "cacheVisual", visual);
            SetRef(cache, "beam", beam);
        }

        private static void Transparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}
