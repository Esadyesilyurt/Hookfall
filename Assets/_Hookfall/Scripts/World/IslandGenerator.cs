// Hookfall - procedural arena: a fixed tower island and two random island rings, new every run (GDD 4.9, 7.1).
// Placeholder shapes (cylinder top + rock underside) until the Tripo + Blender MCP models are ready.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
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

        [Header("Materials")]
        [SerializeField] private Material grassMaterial;
        [SerializeField] private Material stoneMaterial;
        [SerializeField] private Material towerMaterial;

        private readonly List<Vector3> placed = new List<Vector3>();

        public int UsedSeed { get; private set; }

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

            Random.state = previous;
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

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "Top";
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            top.transform.localScale = new Vector3(width, 0.5f, width);
            SetMaterial(top, grassMaterial);

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Underside";
            rock.transform.SetParent(root.transform, false);
            float depth = width * 0.6f;
            rock.transform.localPosition = new Vector3(0f, -1f - depth * 0.35f, 0f);
            rock.transform.localScale = new Vector3(width * 0.9f, depth, width * 0.9f);
            SetMaterial(rock, stoneMaterial);
        }

        private void BuildTower()
        {
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "Tower";
            tower.transform.SetParent(transform, false);
            tower.transform.localPosition = new Vector3(0f, towerHeight * 0.5f, 0f);
            tower.transform.localScale = new Vector3(4f, towerHeight * 0.5f, 4f);
            tower.AddComponent<Island>().Setup(IslandKind.Tower, 4f);   // the tower can be hooked and stood on
            SetMaterial(tower, towerMaterial != null ? towerMaterial : stoneMaterial);
        }

        private static void SetMaterial(GameObject go, Material material)
        {
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
