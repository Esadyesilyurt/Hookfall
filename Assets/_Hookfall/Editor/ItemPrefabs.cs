// Hookfall - builds the six item prefabs with their GDD 4.5 values and lore, the projectile prefabs,
// and the chest and tower cache prefabs.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Combat.Items;
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

        /// <summary>All six items; index 0 is Orbit Blades, the starting item.</summary>
        public static List<ItemBase> CreateItems()
        {
            Projectile shard = CreateProjectile("PF_Shard", PrimitiveType.Cube, new Vector3(0.12f, 0.12f, 0.7f), "M_Shard", 0.25f);
            Projectile spark = CreateProjectile("PF_Spark", PrimitiveType.Sphere, Vector3.one * 0.3f, "M_Spark", 0.35f);

            var items = new List<ItemBase>
            {
                Item<OrbitBlades>("OrbitBlades", "Orbit Blades", 10f, 0.5f,
                    "Twin vault blades bound to the Warden by a Tether rune; they circle and cut whatever comes close.",
                    i => SetRef(i, "bladeMaterial", Mat("M_Shard"))),
                Item<ShardBurst>("ShardBurst", "Shard Burst", 12f, 1.5f,
                    "Splinters of a cracked Heartstone ward that burst outwards whenever the Warden takes flight.",
                    i => SetRef(i, "projectilePrefab", shard)),
                Item<ArcLaser>("ArcLaser", "Arc Laser", 20f, 2f,
                    "A lens from the fallen archer tower; it bends the Heartstone's light into a sweeping blade.",
                    i => SetRef(i, "beamMaterial", Mat("M_Beam"))),
                Item<RamStrike>("RamStrike", "Ram Strike", 0f, 0.5f,
                    "The Order's oldest lesson: a Warden at full speed is the weapon.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura"))),
                Item<HomingSparks>("HomingSparks", "Homing Sparks", 15f, 1.2f,
                    "Sparks from the vault forge that still remember who their enemies are.",
                    i => SetRef(i, "projectilePrefab", spark)),
                Item<StormAura>("StormAura", "Storm Aura", 4f, 0.25f,
                    "A storm sealed in a guardian's cloak; it crackles around the Warden while they fly.",
                    i => SetRef(i, "auraMaterial", Mat("M_Aura")))
            };
            return items;
        }

        private static T Item<T>(string id, string displayName, float damage, float cooldown, string lore, System.Action<T> extra)
            where T : ItemBase
        {
            var root = new GameObject("PF_Item_" + id);
            var item = root.AddComponent<T>();
            SetString(item, "displayName", displayName);
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
            return SavePrefab(root, $"{Root}/Prefabs/World/PF_Chest.prefab").GetComponent<Chest>();
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
