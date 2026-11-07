// Hookfall - procedural arena: a fixed tower island and two random island rings, new every run (GDD 4.9, 7.1),
// dressed with rocks, ruins and crystals, with clouds below and around. Uses the Blender island models when assigned,
// otherwise primitive placeholders.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (models, props, clouds, tower top, abyss height) and 2026-11-08 (World layer for the camera). Prompts and changes are recorded in Docs/PromptLog.md.
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
        [SerializeField] private int attemptsPerIsland = 30;
        [Tooltip("Height bands (low, middle, high) so islands stack in layers and the hook can lift you onto higher ones.")]
        [SerializeField] private Vector2[] heightTiers = { new Vector2(-32f, -14f), new Vector2(-6f, 10f), new Vector2(18f, 38f) };
        [Tooltip("Small high perches near the tower, above its top, for vertical play.")]
        [SerializeField] private Vector2Int perchCount = new Vector2Int(4, 6);
        [SerializeField] private Vector2 perchRadius = new Vector2(24f, 60f);
        [SerializeField] private Vector2 perchHeight = new Vector2(28f, 46f);
        [Tooltip("Every island must be this close (centre to centre) to one the player can already reach; hook range is 40 m.")]
        [SerializeField] private float maxLinkDistance = 32f;

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
            plans.Clear();
            BuildIsland(Vector3.zero, towerIslandWidth, IslandKind.Tower);
            BuildTower();

            PlanRing(Random.Range(innerCount.x, innerCount.y + 1), innerRadius, innerWidth, IslandKind.Inner);
            PlanRing(Random.Range(outerCount.x, outerCount.y + 1), outerRadius, outerWidth, IslandKind.Outer);
            PlanPerches();
            MakeReachable();
            foreach (IslandPlan plan in plans) BuildIsland(plan.position, plan.width, plan.kind);
            PlaceClouds();
            foreach (Collider c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.gameObject.layer = Hookfall.Core.Layers.World;   // the camera's deoccluder collides with this layer

            Random.state = previous;

            // GDD 4.3: falling 60 m below the lowest island ends the run.
            float lowest = 0f;
            foreach (Vector3 p in placed) lowest = Mathf.Min(lowest, p.y);
            AbyssKillHeight = transform.position.y + lowest - abyssDepth;
            if (player != null) player.SetAbyssKillHeight(AbyssKillHeight);
        }

        private struct IslandPlan
        {
            public Vector3 position;
            public float width;
            public IslandKind kind;
        }

        private readonly List<IslandPlan> plans = new List<IslandPlan>();

        private void PlanRing(int count, Vector2 radius, Vector2 width, IslandKind kind)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 tier = heightTiers[Random.Range(0, heightTiers.Length)];
                float w = Random.Range(width.x, width.y);
                for (int attempt = 0; attempt < attemptsPerIsland; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float r = Random.Range(radius.x, radius.y);
                    var position = new Vector3(Mathf.Cos(angle) * r, Random.Range(tier.x, tier.y), Mathf.Sin(angle) * r);
                    if (!FarEnough(position, w)) continue;
                    plans.Add(new IslandPlan { position = position, width = w, kind = kind });
                    break;
                }
            }
        }

        private void PlanPerches()
        {
            int count = Random.Range(perchCount.x, perchCount.y + 1);
            for (int i = 0; i < count; i++)
                for (int attempt = 0; attempt < attemptsPerIsland; attempt++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float r = Random.Range(perchRadius.x, perchRadius.y);
                    var position = new Vector3(Mathf.Cos(angle) * r, Random.Range(perchHeight.x, perchHeight.y), Mathf.Sin(angle) * r);
                    float w = Random.Range(outerWidth.x, outerWidth.y);
                    if (!FarEnough(position, w)) continue;
                    plans.Add(new IslandPlan { position = position, width = w, kind = IslandKind.Outer });
                    break;
                }
        }

        /// <summary>Islands hang deep below their tops, so the check is mostly horizontal: bodies must not overlap.</summary>
        private bool FarEnough(Vector3 position, float width)
        {
            foreach (IslandPlan other in plans)
            {
                Vector2 d = new Vector2(position.x - other.position.x, position.z - other.position.z);
                float needed = Mathf.Max(minSpacing, (width + other.width) * 0.6f);
                bool stackedClear = Mathf.Abs(position.y - other.position.y) > (width + other.width) * 1.4f;
                if (d.magnitude < needed && !stackedClear) return false;
            }
            return Vector2.Distance(new Vector2(position.x, position.z), Vector2.zero) > towerIslandWidth * 0.5f + width * 0.6f + 4f;
        }

        /// <summary>Walks out from the tower island; any island out of hook reach of the reachable ones is pulled
        /// towards the nearest reachable island until it is in reach, so every layer can be climbed.</summary>
        private void MakeReachable()
        {
            var reachable = new List<Vector3> { Vector3.zero, Vector3.up * towerHeight };
            var open = new List<int>();
            for (int i = 0; i < plans.Count; i++) open.Add(i);

            while (open.Count > 0)
            {
                bool grew = false;
                for (int k = open.Count - 1; k >= 0; k--)
                {
                    IslandPlan plan = plans[open[k]];
                    foreach (Vector3 r in reachable)
                    {
                        if (Vector3.Distance(plan.position, r) > maxLinkDistance) continue;
                        reachable.Add(plan.position);
                        open.RemoveAt(k);
                        grew = true;
                        break;
                    }
                }
                if (grew) continue;

                // Nothing new is reachable: move the closest open island into reach of its nearest reachable one.
                // shortcut: the moved island is not re-checked for overlap; rare, and only a visual clip if it happens.
                int best = -1;
                Vector3 anchor = Vector3.zero;
                float bestDistance = float.MaxValue;
                foreach (int index in open)
                    foreach (Vector3 r in reachable)
                    {
                        float d = Vector3.Distance(plans[index].position, r);
                        if (d < bestDistance) { bestDistance = d; best = index; anchor = r; }
                    }
                IslandPlan moved = plans[best];
                Vector3 offset = moved.position - anchor;
                moved.position = anchor + offset.normalized * (maxLinkDistance * 0.9f);
                plans[best] = moved;
            }
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
                float y = below ? Random.Range(-85f, -62f) : Random.Range(-40f, 60f);
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
