// Hookfall - procedural arena: a fixed tower island and two random island rings, new every run (GDD 4.9, 7.1),
// dressed with rocks, ruins and crystals, with clouds below and around. Uses the Blender island models when assigned,
// otherwise primitive placeholders.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (models, props, clouds, tower top, abyss height). Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class IslandGenerator : MonoBehaviour
    {
        [Tooltip("0 = a new random layout every run.")]
        [SerializeField] private int seed = 0;

        [Header("Tower island")]
        [SerializeField] private float towerIslandWidth = 20f;
        [SerializeField] private float towerHeight = 18f;

        [Header("Inner ring: large islands")]
        [SerializeField] private Vector2Int innerCount = new Vector2Int(8, 12);
        [SerializeField] private Vector2 innerRadius = new Vector2(40f, 100f);
        [SerializeField] private Vector2 innerWidth = new Vector2(10f, 20f);

        [Header("Outer ring: small hook points")]
        [SerializeField] private Vector2Int outerCount = new Vector2Int(15, 25);
        [SerializeField] private Vector2 outerRadius = new Vector2(100f, 150f);
        [SerializeField] private Vector2 outerWidth = new Vector2(4f, 8f);

        [Header("Placement")]
        [SerializeField] private float minSpacing = 15f;
        [SerializeField] private Vector2 heightRange = new Vector2(-10f, 10f);
        [SerializeField] private int attemptsPerIsland = 30;

        [Header("Models (optional; primitives are used when empty)")]
        [SerializeField] private GameObject[] largeIslandModels;
        [Tooltip("Top width of the large island models in metres, used to scale them to the rolled width.")]
        [SerializeField] private float largeModelWidth = 14f;
        [SerializeField] private GameObject[] smallIslandModels;
        [SerializeField] private float smallModelWidth = 6f;
        [Tooltip("Tower model with a child named \"TowerTop\" where the tower cache sits.")]
        [SerializeField] private GameObject towerModel;
        [SerializeField] private GameObject[] propModels;
        [SerializeField] private Vector2Int propsPerLargeIsland = new Vector2Int(1, 3);
        [SerializeField] private GameObject[] cloudModels;
        [SerializeField] private int cloudCount = 26;

        [Header("Placeholder materials")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material stoneMaterial;
        [SerializeField] private Material towerMaterial;

        [Header("Abyss")]
        [SerializeField] private PlayerStats player;
        [Tooltip("The run ends this far below the lowest island top (GDD 4.3).")]
        [SerializeField] private float abyssDepth = 60f;

        private readonly List<Vector3> placed = new List<Vector3>();

        public int UsedSeed { get; private set; }
        public float AbyssKillHeight { get; private set; } = -70f;
        /// <summary>Where the tower cache sits.</summary>
        public Vector3 TowerTop { get; private set; }

        private void Awake() => Generate();

        public void Generate()
        {
            UsedSeed = seed != 0 ? seed : System.Environment.TickCount;
            Random.State previous = Random.state;
            Random.InitState(UsedSeed);

            placed.Clear();
            BuildIsland(Vector3.zero, towerIslandWidth, IslandKind.Tower);
            BuildTower();

            PlaceRing(Random.Range(innerCount.x, innerCount.y + 1), innerRadius, innerWidth, IslandKind.Inner);
            PlaceRing(Random.Range(outerCount.x, outerCount.y + 1), outerRadius, outerWidth, IslandKind.Outer);
            PlaceClouds();

            Random.state = previous;

            // GDD 4.3: falling 60 m below the lowest island ends the run.
            float lowest = 0f;
            foreach (Vector3 p in placed) lowest = Mathf.Min(lowest, p.y);
            AbyssKillHeight = transform.position.y + lowest - abyssDepth;
            if (player != null) player.SetAbyssKillHeight(AbyssKillHeight);
        }

        private void PlaceRing(int count, Vector2 radius, Vector2 width, IslandKind kind)
        {
            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < attemptsPerIsland; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float r = Random.Range(radius.x, radius.y);
                    Vector3 position = new Vector3(Mathf.Cos(angle) * r, Random.Range(heightRange.x, heightRange.y), Mathf.Sin(angle) * r);
                    if (!FarEnough(position)) continue;

                    BuildIsland(position, Random.Range(width.x, width.y), kind);
                    break;
                }
            }
        }

        private bool FarEnough(Vector3 position)
        {
            foreach (Vector3 other in placed)
                if (Vector3.Distance(position, other) < minSpacing) return false;
            return true;
        }

        /// <summary>Top surface sits at the given position; the rocky underside hangs below it.</summary>
        private void BuildIsland(Vector3 topCentre, float width, IslandKind kind)
        {
            placed.Add(topCentre);

            var root = new GameObject($"Island ({kind})");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = topCentre;
            root.AddComponent<Island>().Setup(kind, width);

            bool small = kind == IslandKind.Outer;
            GameObject[] models = small ? smallIslandModels : largeIslandModels;
            if (models != null && models.Length > 0)
            {
                GameObject model = Instantiate(models[Random.Range(0, models.Length)], root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                model.transform.localScale = Vector3.one * (width / (small ? smallModelWidth : largeModelWidth));
                if (kind != IslandKind.Outer) AddProps(root.transform, width, kind == IslandKind.Tower);
                return;
            }

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "Top";
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            top.transform.localScale = new Vector3(width, 0.5f, width);
            SetMaterial(top, grassMaterial);
            UseMeshCollider(top);

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Underside";
            rock.transform.SetParent(root.transform, false);
            float depth = width * 0.6f;
            rock.transform.localPosition = new Vector3(0f, -1f - depth * 0.35f, 0f);
            rock.transform.localScale = new Vector3(width * 0.9f, depth, width * 0.9f);
            SetMaterial(rock, stoneMaterial);
        }

        private void AddProps(Transform island, float width, bool towerIsland)
        {
            if (propModels == null || propModels.Length == 0) return;
            int count = Random.Range(propsPerLargeIsland.x, propsPerLargeIsland.y + 1) + (towerIsland ? 2 : 0);
            for (int i = 0; i < count; i++)
            {
                // Keep the centre clear for golems and chests, and clear of the tower.
                Vector2 dir = Random.insideUnitCircle.normalized;
                float distance = Random.Range(0.28f, 0.42f) * width;
                GameObject prop = Instantiate(propModels[Random.Range(0, propModels.Length)], island);
                prop.transform.position = island.position + new Vector3(dir.x * distance, 0f, dir.y * distance);
                prop.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                prop.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            }
        }

        private void BuildTower()
        {
            if (towerModel != null)
            {
                GameObject tower = Instantiate(towerModel, transform);
                tower.name = "Tower";
                tower.transform.localPosition = Vector3.zero;
                tower.AddComponent<Island>().Setup(IslandKind.Tower, 6f);   // the tower can be hooked and stood on
                Transform top = FindDeep(tower.transform, "TowerTop");
                TowerTop = top != null ? top.position : transform.position + Vector3.up * towerHeight;
                return;
            }

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Tower";
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = new Vector3(0f, towerHeight * 0.5f, 0f);
            cylinder.transform.localScale = new Vector3(4f, towerHeight * 0.5f, 4f);
            cylinder.AddComponent<Island>().Setup(IslandKind.Tower, 4f);
            SetMaterial(cylinder, towerMaterial != null ? towerMaterial : stoneMaterial);
            UseMeshCollider(cylinder);
            TowerTop = transform.position + Vector3.up * towerHeight;
        }

        /// <summary>Big soft clouds: a layer below the islands (the abyss haze) and a far ring at the horizon.</summary>
        private void PlaceClouds()
        {
            if (cloudModels == null || cloudModels.Length == 0) return;
            var clouds = new GameObject("Clouds").transform;
            clouds.SetParent(transform, false);
            for (int i = 0; i < cloudCount; i++)
            {
                bool below = i % 2 == 0;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float r = below ? Random.Range(0f, 220f) : Random.Range(230f, 320f);
                float y = below ? Random.Range(-55f, -35f) : Random.Range(-30f, 40f);
                GameObject cloud = Instantiate(cloudModels[Random.Range(0, cloudModels.Length)], clouds);
                cloud.transform.localPosition = new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                cloud.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                cloud.transform.localScale = Vector3.one * Random.Range(below ? 3f : 4f, below ? 6f : 9f);
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>A Unity cylinder comes with a capsule collider, which makes a flat top into a dome; use its mesh instead.</summary>
        private static void UseMeshCollider(GameObject go)
        {
            Destroy(go.GetComponent<Collider>());
            go.AddComponent<MeshCollider>();
        }

        private static void SetMaterial(GameObject go, Material material)
        {
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
