// Hookfall - procedural arena: a fixed tower island and random island rings, new every run and every stage
// (GDD 4.9, 7.1), dressed with rocks, ruins and crystals, with clouds below and around. Uses the Blender island models
// when assigned, otherwise primitive placeholders.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// extended on 2026-11-07 (models, props, clouds, tower top, abyss height), 2026-11-08 (World layer for the camera) and
// 2026-11-08 again: the map is twice as wide, with large golem-free islands, reachability measured rim to rim,
// a new layout for every stage and extra islands for the boss arena. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
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

        [Header("Large islands (2026-11-08): wide places to land; golems never spawn on them")]
        [SerializeField] private Vector2Int largeCount = new Vector2Int(6, 8);
        [SerializeField] private Vector2 largeRadius = new Vector2(70f, 270f);
        [SerializeField] private Vector2 largeWidth = new Vector2(26f, 40f);

        [Header("Inner ring: big islands (40-100 m and 8-12 islands until the map doubled on 2026-11-08)")]
        [SerializeField] private Vector2Int innerCount = new Vector2Int(22, 30);
        [SerializeField] private Vector2 innerRadius = new Vector2(40f, 210f);
        [SerializeField] private Vector2 innerWidth = new Vector2(10f, 20f);

        [Header("Outer ring: small hook points (100-150 m and 15-25 until 2026-11-08)")]
        [SerializeField] private Vector2Int outerCount = new Vector2Int(42, 58);
        [SerializeField] private Vector2 outerRadius = new Vector2(190f, 310f);
        [SerializeField] private Vector2 outerWidth = new Vector2(4f, 8f);

        [Header("Placement")]
        [SerializeField] private float minSpacing = 16f;
        [SerializeField] private int attemptsPerIsland = 30;
        [Tooltip("Height bands (low, middle, high) so islands stack in layers and the hook can lift you onto higher ones.")]
        [SerializeField] private Vector2[] heightTiers = { new Vector2(-32f, -14f), new Vector2(-6f, 10f), new Vector2(18f, 38f) };
        [Tooltip("Small high perches near the tower, above its top, for vertical play.")]
        [SerializeField] private Vector2Int perchCount = new Vector2Int(8, 12);
        [SerializeField] private Vector2 perchRadius = new Vector2(24f, 120f);
        [SerializeField] private Vector2 perchHeight = new Vector2(28f, 46f);
        [Tooltip("Every island's rim must be this close to the rim of one the player can already reach; hook range is 40 m. " +
                 "Measured rim to rim since 2026-11-08 (32 m centre to centre before), so large islands do not overlap.")]
        [SerializeField] private float maxLinkGap = 22f;

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
        [Tooltip("No prop within this radius of the Warden's start on the tower island (x, z): a prop there pushed the Warden off.")]
        [SerializeField] private Vector2 startClearing = new Vector2(0f, -7f);
        [SerializeField] private float startClearingRadius = 4f;
        [SerializeField] private GameObject[] cloudModels;
        [SerializeField] private int cloudCount = 60;

        [Header("Placeholder materials")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material stoneMaterial;
        [SerializeField] private Material towerMaterial;

        [Header("Abyss")]
        [SerializeField] private PlayerStats player;
        [Tooltip("The run ends this far below the lowest island top (GDD 4.3).")]
        [SerializeField] private float abyssDepth = 60f;

        private readonly List<Vector3> placed = new List<Vector3>();
        private int regenerations;

        public int UsedSeed { get; private set; }
        public float AbyssKillHeight { get; private set; } = -70f;
        /// <summary>Where the tower cache sits.</summary>
        public Vector3 TowerTop { get; private set; }

        private void Awake() => Generate();

        public void Generate()
        {
            UsedSeed = (seed != 0 ? seed : System.Environment.TickCount) + regenerations * 7919;
            Random.State previous = Random.state;
            Random.InitState(UsedSeed);

            placed.Clear();
            plans.Clear();
            BuildIsland(Vector3.zero, towerIslandWidth, IslandKind.Tower, transform, true);
            BuildTower();

            // The widest first, so they find room.
            PlanRing(Random.Range(largeCount.x, largeCount.y + 1), largeRadius, largeWidth, IslandKind.Large);
            PlanRing(Random.Range(innerCount.x, innerCount.y + 1), innerRadius, innerWidth, IslandKind.Inner);
            PlanRing(Random.Range(outerCount.x, outerCount.y + 1), outerRadius, outerWidth, IslandKind.Outer);
            PlanPerches();
            MakeReachable();
            foreach (IslandPlan plan in plans) BuildIsland(plan.position, plan.width, plan.kind, transform, true);
            PlaceClouds();
            foreach (Collider c in GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.gameObject.layer = Layers.World;   // the camera's deoccluder collides with this layer

            Random.state = previous;

            // GDD 4.3: falling 60 m below the lowest island ends the run.
            float lowest = 0f;
            foreach (Vector3 p in placed) lowest = Mathf.Min(lowest, p.y);
            AbyssKillHeight = transform.position.y + lowest - abyssDepth;
            if (player != null) player.SetAbyssKillHeight(AbyssKillHeight);
        }

        /// <summary>A new stage: every island, prop and cloud is replaced by a new layout (the tower comes back where it was).</summary>
        public void Regenerate()
        {
            regenerations++;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);   // leaves Island.All and the physics scene now, not at the end of the frame
                Destroy(child);
            }
            Generate();
        }

        /// <summary>An island outside the layout (the boss arena, the portal island), under the given parent.</summary>
        public Island BuildExtraIsland(Transform parent, Vector3 topCentre, float width, IslandKind kind, bool props)
        {
            Island island = BuildIsland(topCentre, width, kind, parent, false, props);
            foreach (Collider c in island.GetComponentsInChildren<Collider>())
                if (!c.isTrigger) c.gameObject.layer = Layers.World;
            return island;
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

        /// <summary>Gap between two islands' rims (centre distance less both radii).</summary>
        private static float RimGap(Vector3 a, float widthA, Vector3 b, float widthB) =>
            Vector3.Distance(a, b) - (widthA + widthB) * 0.5f;

        /// <summary>Walks out from the tower island; any island out of hook reach of the reachable ones is pulled
        /// towards the nearest reachable island until it is in reach, so every layer can be climbed.</summary>
        private void MakeReachable()
        {
            var reachable = new List<(Vector3 position, float width)>
            {
                (Vector3.zero, towerIslandWidth), (Vector3.up * towerHeight, 7f)
            };
            var open = new List<int>();
            for (int i = 0; i < plans.Count; i++) open.Add(i);

            while (open.Count > 0)
            {
                bool grew = false;
                for (int k = open.Count - 1; k >= 0; k--)
                {
                    IslandPlan plan = plans[open[k]];
                    foreach ((Vector3 position, float width) r in reachable)
                    {
                        if (RimGap(plan.position, plan.width, r.position, r.width) > maxLinkGap) continue;
                        reachable.Add((plan.position, plan.width));
                        open.RemoveAt(k);
                        grew = true;
                        break;
                    }
                }
                if (grew) continue;

                // Nothing new is reachable: move the closest open island into reach of its nearest reachable one.
                // shortcut: the moved island is not re-checked for overlap; rare, and only a visual clip if it happens.
                int best = -1;
                (Vector3 position, float width) anchor = default;
                float bestGap = float.MaxValue;
                foreach (int index in open)
                    foreach ((Vector3 position, float width) r in reachable)
                    {
                        float gap = RimGap(plans[index].position, plans[index].width, r.position, r.width);
                        if (gap < bestGap) { bestGap = gap; best = index; anchor = r; }
                    }
                IslandPlan moved = plans[best];
                Vector3 offset = moved.position - anchor.position;
                moved.position = anchor.position + offset.normalized * ((moved.width + anchor.width) * 0.5f + maxLinkGap * 0.85f);
                plans[best] = moved;
            }
        }

        /// <summary>Top surface sits at the given position; the rocky underside hangs below it.</summary>
        private Island BuildIsland(Vector3 topCentre, float width, IslandKind kind, Transform parent, bool partOfLayout, bool props = true)
        {
            if (partOfLayout) placed.Add(topCentre);

            var root = new GameObject($"Island ({kind})");
            root.transform.SetParent(parent, false);
            root.transform.position = topCentre;
            Island island = root.AddComponent<Island>();
            island.Setup(kind, width);

            bool small = kind == IslandKind.Outer;
            GameObject[] models = small ? smallIslandModels : largeIslandModels;
            if (models != null && models.Length > 0)
            {
                GameObject model = Instantiate(models[Random.Range(0, models.Length)], root.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                model.transform.localScale = Vector3.one * (width / (small ? smallModelWidth : largeModelWidth));
                if (kind != IslandKind.Outer && props) AddProps(root.transform, width, kind == IslandKind.Tower);
                return island;
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
            return island;
        }

        private void AddProps(Transform island, float width, bool towerIsland)
        {
            if (propModels == null || propModels.Length == 0) return;
            // Wider islands get more (a large island has room for a small ruin field).
            int count = Random.Range(propsPerLargeIsland.x, propsPerLargeIsland.y + 1) + (towerIsland ? 2 : 0) + Mathf.FloorToInt(width / 14f);
            for (int i = 0; i < count; i++)
            {
                // Keep the centre clear for golems and chests, and clear of the tower and of the Warden's start.
                Vector2 dir = Random.insideUnitCircle.normalized;
                float distance = Random.Range(0.28f, 0.42f) * width;
                for (int attempt = 0; towerIsland && attempt < 8 && Vector2.Distance(dir * distance, startClearing) < startClearingRadius; attempt++)
                    dir = Random.insideUnitCircle.normalized;
                if (towerIsland && Vector2.Distance(dir * distance, startClearing) < startClearingRadius) continue;
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
                Transform top = FindDeep(tower.transform, "TowerTop");
                TowerTop = top != null ? top.position : transform.position + Vector3.up * towerHeight;
                // The tower can be hooked and stood on. Its top is the deck and its width reaches the parapet, so touching
                // the parapet or the corbels under it climbs the Warden over onto the deck (2026-11-08).
                tower.AddComponent<Island>().Setup(IslandKind.Tower, 7.2f, TowerTop.y - tower.transform.position.y);
                return;
            }

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Tower";
            cylinder.transform.SetParent(transform, false);
            cylinder.transform.localPosition = new Vector3(0f, towerHeight * 0.5f, 0f);
            cylinder.transform.localScale = new Vector3(4f, towerHeight * 0.5f, 4f);
            cylinder.AddComponent<Island>().Setup(IslandKind.Tower, 4f, towerHeight * 0.5f);
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
                float r = below ? Random.Range(0f, 420f) : Random.Range(440f, 620f);
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
