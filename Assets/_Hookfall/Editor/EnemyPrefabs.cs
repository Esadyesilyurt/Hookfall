// Hookfall - builds the enemy prefabs (Bat, Wyvern, Golem, Boulder, and since 2026-11-08 the stage Boss and its Portal)
// with their GDD 5 stats. Uses the Blender
// models from Art/Models when they exist, otherwise primitive placeholders with the same part names.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies;
using Hookfall.World;
using UnityEditor;
using UnityEngine;
using static Hookfall.EditorTools.BuildUtil;

namespace Hookfall.EditorTools
{
    public static class EnemyPrefabs
    {
        private const string Folder = Root + "/Prefabs/Enemies";

        public static void CreateMaterials()
        {
            GetOrCreateMaterial("M_Bat", new Color(0.42f, 0.16f, 0.48f), false);
            GetOrCreateMaterial("M_Wyvern", new Color(0.3f, 0.14f, 0.38f), false);
            GetOrCreateMaterial("M_Golem", new Color(0.33f, 0.31f, 0.34f), false);
            GetOrCreateMaterial("M_EnemyEye", new Color(1f, 0.25f, 0.8f), true);
            GetOrCreateMaterial("M_Warning", new Color(1f, 0.15f, 0.1f), true);
        }

        public static Bat Bat()
        {
            var root = new GameObject("PF_Bat");
            var bat = root.AddComponent<Bat>();
            GameObject visual = InstantiateModel("Bat", root.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.6f, 0.55f, 0.7f), Mat("M_Bat"));
                Part(visual.transform, "Eye", PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0.3f), new Vector3(0.3f, 0.12f, 0.1f), Mat("M_EnemyEye"));
                Transform l = Pivot(visual.transform, "Wing_L", new Vector3(-0.25f, 0.05f, 0f));
                Part(l, "Membrane", PrimitiveType.Cube, new Vector3(-0.45f, 0f, 0f), new Vector3(0.9f, 0.04f, 0.5f), Mat("M_Bat"));
                Transform r = Pivot(visual.transform, "Wing_R", new Vector3(0.25f, 0.05f, 0f));
                Part(r, "Membrane", PrimitiveType.Cube, new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.04f, 0.5f), Mat("M_Bat"));
            }
            // 12 m/s until 2026-11-08: bats never caught up. 10 health until 2026-11-09: with the stage's growth a fresh bat
            // had 10.1-10.6, so a 10-damage starting item needed two hits, and the first one knocked it out of reach.
            Stats(bat, "Bat", 8f, 18f, 5f, 1f, 1, 0.6f);
            SetFloat(bat, "knockback", 7f);
            Wings(bat, visual.transform, 16f, 40f);
            NoShadows(root);
            return SavePrefab(root, Folder + "/PF_Bat.prefab").GetComponent<Bat>();
        }

        public static Wyvern Wyvern()
        {
            var root = new GameObject("PF_Wyvern");
            var wyvern = root.AddComponent<Wyvern>();
            GameObject visual = InstantiateModel("Wyvern", root.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(0.9f, 0.9f, 1.6f), Mat("M_Wyvern"), new Vector3(90f, 0f, 0f));
                Part(visual.transform, "Head", PrimitiveType.Cube, new Vector3(0f, 0.3f, 1.6f), new Vector3(0.5f, 0.45f, 0.8f), Mat("M_Wyvern"));
                Part(visual.transform, "Eye", PrimitiveType.Sphere, new Vector3(0f, 0.42f, 1.95f), new Vector3(0.4f, 0.1f, 0.1f), Mat("M_EnemyEye"));
                Part(visual.transform, "Tail", PrimitiveType.Cube, new Vector3(0f, 0f, -1.7f), new Vector3(0.25f, 0.2f, 1.6f), Mat("M_Wyvern"));
                Transform l = Pivot(visual.transform, "Wing_L", new Vector3(-0.4f, 0.25f, 0.2f));
                Part(l, "Membrane", PrimitiveType.Cube, new Vector3(-1.6f, 0f, 0f), new Vector3(3.2f, 0.06f, 1.4f), Mat("M_Wyvern"));
                Transform r = Pivot(visual.transform, "Wing_R", new Vector3(0.4f, 0.25f, 0.2f));
                Part(r, "Membrane", PrimitiveType.Cube, new Vector3(1.6f, 0f, 0f), new Vector3(3.2f, 0.06f, 1.4f), Mat("M_Wyvern"));
            }
            Stats(wyvern, "Wyvern", 60f, 20f, 15f, 3f, 2, 1.3f);
            SetFloat(wyvern, "knockback", 2.5f);
            SetFloat(wyvern, "hitPunch", 0.15f);
            Wings(wyvern, visual.transform, 7f, 35f);
            NoShadows(root);
            return SavePrefab(root, Folder + "/PF_Wyvern.prefab").GetComponent<Wyvern>();
        }

        public static Golem Golem()
        {
            var root = new GameObject("PF_Golem");
            var golem = root.AddComponent<Golem>();
            GameObject visual = InstantiateModel("Golem", root.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Legs", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.2f, 1f), Mat("M_Golem"));
                Part(visual.transform, "Body", PrimitiveType.Sphere, new Vector3(0f, 1.9f, 0f), new Vector3(2.1f, 1.7f, 1.6f), Mat("M_Golem"));
                Part(visual.transform, "Head", PrimitiveType.Cube, new Vector3(0f, 3.0f, 0.15f), new Vector3(0.9f, 0.7f, 0.8f), Mat("M_Golem"));
                Part(visual.transform, "Eyes", PrimitiveType.Cube, new Vector3(0f, 3.05f, 0.56f), new Vector3(0.6f, 0.12f, 0.05f), Mat("M_EnemyEye"));
                Transform l = Pivot(visual.transform, "Arm_L", new Vector3(-1.25f, 2.4f, 0f));
                Part(l, "Arm", PrimitiveType.Cube, new Vector3(0f, -0.8f, 0f), new Vector3(0.6f, 1.8f, 0.6f), Mat("M_Golem"));
                Transform r = Pivot(visual.transform, "Arm_R", new Vector3(1.25f, 2.4f, 0f));
                Part(r, "Arm", PrimitiveType.Cube, new Vector3(0f, -0.8f, 0f), new Vector3(0.6f, 1.8f, 0.6f), Mat("M_Golem"));
            }
            Stats(golem, "Golem", 150f, 0f, 0f, 20f, 15, 1.4f);   // 5 XP, 5 gold until 2026-11-08: island fights must pay
            SetFloat(golem, "knockback", 0f);   // golems stand on their island
            SetFloat(golem, "hitPunch", 0.08f);
            SetRef(golem, "warningMaterial", Mat("M_Warning"));
            SetRef(golem, "armLeft", FindDeep(visual.transform, "Arm_L"));
            SetRef(golem, "armRight", FindDeep(visual.transform, "Arm_R"));
            return SavePrefab(root, Folder + "/PF_Golem.prefab").GetComponent<Golem>();
        }

        public static Boulder Boulder()
        {
            var root = new GameObject("PF_Boulder");
            root.AddComponent<Boulder>();
            GameObject visual = InstantiateModel("Boulder", root.transform);
            if (visual == null) Part(root.transform, "Visual", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.2f, Mat("M_Golem"));
            return SavePrefab(root, Folder + "/PF_Boulder.prefab").GetComponent<Boulder>();
        }

        /// <summary>The Abyss Colossus: the Blender model with its parts wired, a kinematic body the Warden cannot fly
        /// through (World layer, so the camera stays out too) and a light in its core.</summary>
        public static Boss Boss()
        {
            var root = new GameObject("PF_Boss");
            var boss = root.AddComponent<Boss>();
            GameObject visual = InstantiateModel("Boss", root.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(9f, 12f, 7f), Mat("M_Golem"));
                Part(visual.transform, "Core", PrimitiveType.Sphere, new Vector3(0f, 0.9f, 3.2f), Vector3.one * 2.4f, Mat("M_EnemyEye"));
                Transform h = Pivot(visual.transform, "Head", new Vector3(0f, 4.3f, 0.4f));
                Part(h, "Skull", PrimitiveType.Cube, new Vector3(0f, 1.4f, 0.6f), new Vector3(3.6f, 3f, 3.4f), Mat("M_Golem"));
                Transform l = Pivot(visual.transform, "Arm_L", new Vector3(-5.4f, 2.8f, 0f));
                Part(l, "Arm", PrimitiveType.Cube, new Vector3(0f, -3.5f, 0.5f), new Vector3(2.4f, 7f, 2.4f), Mat("M_Golem"));
                Transform r = Pivot(visual.transform, "Arm_R", new Vector3(5.4f, 2.8f, 0f));
                Part(r, "Arm", PrimitiveType.Cube, new Vector3(0f, -3.5f, 0.5f), new Vector3(2.4f, 7f, 2.4f), Mat("M_Golem"));
            }
            Stats(boss, "Abyss Colossus", 14000f, 0f, 20f, 160f, 120, 7f);   // a mid-run build killed 9000 in 51 s, 12000 in 57 s
            SetFloat(boss, "knockback", 0f);
            SetFloat(boss, "hitPunch", 0.02f);
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("flashColor").colorValue = new Color(0.35f, 0.28f, 0.34f);   // many hits a second: no strobe
            SerializedProperty rocks = serialized.FindProperty("orbitRocks");
            rocks.arraySize = 0;
            for (int i = 1; i <= 3; i++)
            {
                Transform rock = FindDeep(visual.transform, "Orbit_" + i);
                if (rock == null) continue;
                rocks.arraySize++;
                rocks.GetArrayElementAtIndex(rocks.arraySize - 1).objectReferenceValue = rock;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetRef(boss, "head", FindDeep(visual.transform, "Head"));
            SetRef(boss, "jaw", FindDeep(visual.transform, "Jaw"));
            SetRef(boss, "armLeft", FindDeep(visual.transform, "Arm_L"));
            SetRef(boss, "armRight", FindDeep(visual.transform, "Arm_R"));
            Transform core = FindDeep(visual.transform, "Core");
            SetRef(boss, "core", core);
            SetRef(boss, "shardMaterial", Mat("M_EnemyEye"));
            SetRef(boss, "effectMaterial", ToonSetup.ParticleMaterial());
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 5.2f;
            capsule.height = 17f;
            capsule.center = new Vector3(0f, -0.5f, 0f);
            root.layer = Hookfall.Core.Layers.World;
            var glow = new GameObject("Core Light").AddComponent<Light>();
            glow.transform.SetParent(core != null ? core : root.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.35f, 0.85f);
            glow.range = 32f;
            glow.intensity = 4f;
            return SavePrefab(root, Folder + "/PF_Boss.prefab").GetComponent<Boss>();
        }

        /// <summary>The portal the boss leaves: the model, a collider on its dais to stand on, a light in the ring.</summary>
        public static Portal Portal()
        {
            var root = new GameObject("PF_Portal");
            GameObject visual = InstantiateModel("Portal", root.transform);
            if (visual == null)
            {
                visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                Part(visual.transform, "Dais", PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0f), new Vector3(8f, 0.25f, 8f), Mat("M_Tower"));
                Part(visual.transform, "Disc", PrimitiveType.Cylinder, new Vector3(0f, 3.6f, 0f), new Vector3(5.2f, 0.04f, 5.2f), Mat("M_Heartstone"), new Vector3(90f, 0f, 0f));
            }
            Transform dais = FindDeep(visual.transform, "Dais");
            if (dais != null && dais.GetComponent<Collider>() == null)
                dais.gameObject.AddComponent<MeshCollider>().sharedMesh = dais.GetComponent<MeshFilter>().sharedMesh;
            if (dais != null) dais.gameObject.layer = Hookfall.Core.Layers.World;
            Transform disc = FindDeep(visual.transform, "Disc");
            if (disc != null)
            {
                foreach (Collider c in disc.GetComponents<Collider>()) Object.DestroyImmediate(c);
                disc.gameObject.layer = Hookfall.Core.Layers.NoOutline;
            }
            var portal = root.AddComponent<Portal>();
            SetRef(portal, "disc", disc);
            SetRef(portal, "particleMaterial", ToonSetup.ParticleMaterial());
            var glow = new GameObject("Ring Light").AddComponent<Light>();
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 3.6f, 0f);
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 0.95f, 1f);
            glow.range = 20f;
            glow.intensity = 3f;
            return SavePrefab(root, Root + "/Prefabs/World/PF_Portal.prefab").GetComponent<Portal>();
        }

        private static void Stats(EnemyBase enemy, string name, float health, float speed, float damage, float xp, int gold, float hitRadius)
        {
            SetString(enemy, "displayName", name);
            SetFloat(enemy, "maxHealth", health);
            SetFloat(enemy, "moveSpeed", speed);
            SetFloat(enemy, "contactDamage", damage);
            SetFloat(enemy, "xpReward", xp);
            SetInt(enemy, "goldReward", gold);
            SetFloat(enemy, "hitRadius", hitRadius);
        }

        /// <summary>Flyers cast no shadow (2026-11-09): in the open sky it never lands on anything, and once the horde
        /// kept up with the Warden, 300 bats on screen drew every part again for each shadow cascade.</summary>
        private static void NoShadows(GameObject root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Wings(EnemyBase enemy, Transform visual, float speed, float angle)
        {
            SetRef(enemy, "wingLeft", FindDeep(visual, "Wing_L"));
            SetRef(enemy, "wingRight", FindDeep(visual, "Wing_R"));
            SetFloat(enemy, "flapSpeed", speed);
            SetFloat(enemy, "flapAngle", angle);
        }

        public static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
