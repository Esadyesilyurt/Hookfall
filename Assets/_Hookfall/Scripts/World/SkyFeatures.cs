// Hookfall - fills the open sky between islands every run: updrafts that lift, air channels that carry the Warden from
// island to island, crystals that refill the double jump and dash in mid-air, and floating rocks the hook can catch.
// Placed from the run's island layout and seed once the IslandGenerator has built it (added on 2026-11-08 because
// the map felt empty between islands). Rebuilt for every stage's new layout, doubled with the map, and asked for the
// updrafts and anchors that lead up to the boss arena (2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Audio;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.VFX;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hookfall.World
{
    public class SkyFeatures : MonoBehaviour
    {
        private static readonly Color WindColor = new Color(0.9f, 0.97f, 1f, 0.6f);
        private static readonly Color ChannelColor = new Color(0.55f, 0.92f, 1f, 0.75f);
        private static readonly Color CrystalColor = new Color(0.45f, 1f, 0.95f);

        [SerializeField] private IslandGenerator islands;
        [SerializeField] private PlayerController player;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private VfxManager vfx;
        [SerializeField] private Material particleMaterial;
        [SerializeField] private Material crystalMaterial;
        [Tooltip("Rock model for the hook anchors (an island prop prefab); a primitive is used without it.")]
        [SerializeField] private GameObject anchorModel;

        [Header("How many per run (fewer when the layout has no room)")]
        [SerializeField] private int updrafts = 16;     // 8, 4, 12 and 10 until the map doubled (2026-11-08)
        [SerializeField] private int airChannels = 8;
        [SerializeField] private int crystals = 24;
        [SerializeField] private int anchors = 20;

        [Header("Updrafts")]
        [SerializeField] private float updraftRadius = 5f;
        [Tooltip("Upward acceleration inside the column; air gravity is about 17.7 m/s², so the difference is the lift.")]
        [SerializeField] private float updraftLift = 32f;
        [SerializeField] private float updraftMaxRise = 16f;

        [Header("Air channels")]
        [SerializeField] private float channelRadius = 3.5f;
        [Tooltip("Speed inside a channel; kept under the 40 m/s air speed cap.")]
        [SerializeField] private float channelSpeed = 38f;
        [Tooltip("Gap between the two islands' rims a channel may span (min, max).")]
        [SerializeField] private Vector2 channelGap = new Vector2(40f, 120f);

        [Header("Refill crystals")]
        [SerializeField] private float crystalTouchRadius = 2.5f;
        [SerializeField] private float crystalRespawn = 8f;

        [Header("Hook anchors")]
        [SerializeField] private float anchorScale = 1.6f;
        [SerializeField] private float anchorSpacing = 20f;

        private readonly List<Island> all = new List<Island>();
        private readonly List<Vector3> taken = new List<Vector3>();
        private Material lineMaterial;   // the particle material without its soft-dot texture, so ring lines stay solid all the way round

        private void Start() => Build();

        /// <summary>A new stage: the old features go with the old map and new ones are placed for the new layout.</summary>
        public void Rebuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            Build();
        }

        private void Build()
        {
            all.Clear();
            taken.Clear();
            all.AddRange(Island.All);
            if (player == null || all.Count < 2) return;
            if (lineMaterial == null)
            {
                lineMaterial = new Material(particleMaterial) { name = "Sky Lines" };
                lineMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }
            Random.State previous = Random.state;
            Random.InitState((islands != null ? islands.UsedSeed : 0) + 7919);

            PlaceUpdrafts();
            PlaceChannels();
            PlacePoints(crystals, new Vector2(15f, 90f), -3f, 10f, 4f, 12f, MakeCrystal);
            PlacePoints(anchors, new Vector2(30f, 110f), 5f, 15f, 6f, anchorSpacing, MakeAnchor, true);

            Random.state = previous;
        }

        /// <summary>
        /// The way up to the boss: three updrafts from low above the tower island to just over the boss's lowest island,
        /// between its islands, and a spiral of hook anchors. All under parent, so they go when the arena goes.
        /// </summary>
        public void BuildBossAccess(Transform parent, Vector3 lowestIsland, float ring, float angleOffset)
        {
            if (player == null) return;
            if (lineMaterial == null)
            {
                lineMaterial = new Material(particleMaterial) { name = "Sky Lines" };
                lineMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = (angleOffset + i * 120f) * Mathf.Deg2Rad;
                var bottom = new Vector3(lowestIsland.x + Mathf.Cos(a) * (ring + 2f), 2f, lowestIsland.z + Mathf.Sin(a) * (ring + 2f));
                MakeUpdraft(parent, bottom, lowestIsland.y + 8f - bottom.y);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = (angleOffset + 60f + i * 50f) * Mathf.Deg2Rad;
                float r = Mathf.Lerp(16f, ring - 4f, (i % 4) / 3f);
                MakeAnchor(new Vector3(lowestIsland.x + Mathf.Cos(a) * r, Mathf.Lerp(26f, lowestIsland.y - 4f, i / 7f), lowestIsland.z + Mathf.Sin(a) * r), parent);
            }
        }

        // ---------- Effects the features ask for ----------

        public void CrystalTaken(Vector3 position)
        {
            if (vfx != null) vfx.Ring(position, 20, CrystalColor, 6f);
            if (audioManager != null) audioManager.Play(SoundId.TowerCacheBeam, position);
        }

        public void ChannelEntered(Vector3 position)
        {
            if (audioManager != null) audioManager.Play(SoundId.Trick, position);
        }

        // ---------- Placement ----------

        private void PlaceUpdrafts()
        {
            int placed = 0;
            // Half of them beside an island's rim, reaching above its top: they help the Warden up onto it.
            var shuffled = new List<Island>(all);
            Shuffle(shuffled);
            foreach (Island island in shuffled)
            {
                if (placed >= updrafts / 2) break;
                if (island.Kind == IslandKind.Tower) continue;
                Vector2 d = Random.insideUnitCircle.normalized;
                Vector3 dir = new Vector3(d.x, 0f, d.y);
                Vector3 bottom = island.Top + dir * (island.Width * 0.5f + updraftRadius + 1.5f) + Vector3.down * 28f;
                if (TryUpdraft(bottom, 34f)) placed++;
            }
            // The rest in the gaps between two islands, from below the lower top to above the higher one.
            foreach ((Island a, Island b) in Pairs(new Vector2(20f, 80f)))
            {
                if (placed >= updrafts) break;
                Vector3 mid = Midpoint(a, b);
                float low = Mathf.Min(a.Top.y, b.Top.y), high = Mathf.Max(a.Top.y, b.Top.y);
                if (TryUpdraft(new Vector3(mid.x, low - 30f, mid.z), high - low + 38f)) placed++;
            }
        }

        private bool TryUpdraft(Vector3 bottom, float height)
        {
            Vector3 top = bottom + Vector3.up * height;
            if (Physics.CheckCapsule(bottom, top, updraftRadius * 0.6f, 1 << Layers.World, QueryTriggerInteraction.Ignore)) return false;
            if (!Spaced(bottom + Vector3.up * (height * 0.5f), 15f)) return false;

            MakeUpdraft(transform, bottom, height);
            taken.Add(bottom + Vector3.up * (height * 0.5f));
            return true;
        }

        private void MakeUpdraft(Transform parent, Vector3 bottom, float height)
        {
            var go = new GameObject("Updraft");
            go.transform.SetParent(parent, false);
            go.transform.position = bottom;
            go.AddComponent<Updraft>().Init(player, updraftRadius, height, updraftLift, updraftMaxRise);
            Stream(go.transform, Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), updraftRadius * 0.9f, 12f, height / 12f, 45f, WindColor, new Vector2(0.5f, 1.1f));
            FlowingRings(go.transform, Quaternion.Euler(-90f, 0f, 0f), Mathf.Max(4, Mathf.CeilToInt(height / 14f)), updraftRadius * 0.85f, height, 6f, WindColor, 0.22f);
        }

        private void PlaceChannels()
        {
            int placed = 0;
            foreach ((Island a, Island b) in Pairs(channelGap))
            {
                if (placed >= airChannels) break;
                (Island from, Island to) = Random.value < 0.5f ? (a, b) : (b, a);
                Vector3 dir = Flat(to.Top - from.Top).normalized;
                Vector3 entry = from.Top + dir * (from.Width * 0.5f + 2f) + Vector3.up * 3f;
                Vector3 exit = to.Top - dir * (to.Width * 0.5f + 2f) + Vector3.up * 3f;
                Vector3 along = exit - entry;
                if (Physics.SphereCast(entry, channelRadius * 0.7f, along.normalized, out _, along.magnitude, 1 << Layers.World, QueryTriggerInteraction.Ignore)) continue;
                if (!Spaced((entry + exit) * 0.5f, 25f)) continue;

                var go = new GameObject("Air Channel");
                go.transform.SetParent(transform, false);
                go.transform.SetPositionAndRotation(entry, Quaternion.LookRotation(along));
                go.AddComponent<AirChannel>().Init(this, player, entry, exit, channelRadius, channelSpeed);
                float streamSpeed = channelSpeed * 0.8f;
                Stream(go.transform, Vector3.zero, Quaternion.identity, channelRadius * 0.8f, streamSpeed, along.magnitude / streamSpeed, 90f, ChannelColor, new Vector2(0.4f, 0.9f));
                Ring(go.transform, Vector3.zero, Quaternion.identity, channelRadius, ChannelColor, 0.35f);
                FlowingRings(go.transform, Quaternion.identity, Mathf.CeilToInt(along.magnitude / 12f), channelRadius * 0.9f, along.magnitude, 14f, ChannelColor, 0.2f);
                taken.Add((entry + exit) * 0.5f);
                placed++;
            }
        }

        /// <summary>Points in the gap between two islands, at a height around theirs, clear of islands and of each other.</summary>
        private void PlacePoints(int count, Vector2 gap, float minAbove, float maxAbove, float clearance, float spacing,
            System.Action<Vector3> make, bool aboveHigher = false)
        {
            int placed = 0;
            foreach ((Island a, Island b) in Pairs(gap))
            {
                if (placed >= count) break;
                Vector3 p = Midpoint(a, b);
                p.y = (aboveHigher ? Mathf.Max(a.Top.y, b.Top.y) : (a.Top.y + b.Top.y) * 0.5f) + Random.Range(minAbove, maxAbove);
                if (!ClearOfIslands(p, clearance) || !Spaced(p, spacing)) continue;
                make(p);
                taken.Add(p);
                placed++;
            }
        }

        private void MakeCrystal(Vector3 position)
        {
            var go = new GameObject("Refill Crystal");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var visual = new GameObject("Crystal", typeof(MeshFilter), typeof(MeshRenderer));
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(1.3f, 2f, 1.3f);
            visual.GetComponent<MeshFilter>().sharedMesh = Octahedron();
            visual.GetComponent<MeshRenderer>().sharedMaterial = crystalMaterial;
            Stream(visual.transform, Vector3.down * 0.4f, Quaternion.Euler(-90f, 0f, 0f), 0.6f, 1.2f, 1.5f, 6f, CrystalColor, new Vector2(0.15f, 0.3f));   // hides with the crystal
            go.AddComponent<RefillCrystal>().Init(this, player, visual.transform, crystalTouchRadius, crystalRespawn);
        }

        private void MakeAnchor(Vector3 position) => MakeAnchor(position, transform);

        private void MakeAnchor(Vector3 position, Transform parent)
        {
            GameObject rock = anchorModel != null ? Instantiate(anchorModel, parent) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "Hook Anchor";
            rock.transform.SetParent(parent, true);
            rock.transform.SetPositionAndRotation(position, Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(0f, 360f), 180f));   // upside down: the flat face points down
            rock.transform.localScale = Vector3.one * anchorScale * (anchorModel != null ? 1f : 2.5f);
            foreach (Collider c in rock.GetComponentsInChildren<Collider>()) c.gameObject.layer = Layers.World;   // the camera stays out of it
            Transform ring = Ring(parent, Vector3.zero, Quaternion.Euler(75f, 0f, 0f), 2.6f * anchorScale, ChannelColor, 0.3f);
            ring.position = position;
            rock.AddComponent<HookAnchor>().Init(ring);
        }

        // ---------- Layout helpers ----------

        /// <summary>Island pairs whose rims are this far apart (min, max), in random order.</summary>
        private List<(Island, Island)> Pairs(Vector2 gap)
        {
            var pairs = new List<(Island, Island)>();
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                {
                    float rims = Flat(all[i].Top - all[j].Top).magnitude - (all[i].Width + all[j].Width) * 0.5f;
                    if (rims >= gap.x && rims <= gap.y) pairs.Add((all[i], all[j]));
                }
            Shuffle(pairs);
            return pairs;
        }

        /// <summary>Halfway between the two rims, at ground level.</summary>
        private static Vector3 Midpoint(Island a, Island b)
        {
            Vector3 dir = Flat(b.Top - a.Top).normalized;
            Vector3 rimA = a.Top + dir * (a.Width * 0.5f);
            Vector3 rimB = b.Top - dir * (b.Width * 0.5f);
            return (rimA + rimB) * 0.5f;
        }

        /// <summary>False inside any island's column from 40 m below its top to this margin above it (the whole column for the tower).</summary>
        private bool ClearOfIslands(Vector3 p, float margin)
        {
            foreach (Island island in all)
            {
                if (Flat(p - island.Top).magnitude > island.Width * 0.5f + margin) continue;
                if (island.Kind == IslandKind.Tower) return false;
                if (p.y < island.Top.y + margin && p.y > island.Top.y - 40f) return false;
            }
            return !Physics.CheckSphere(p, margin * 0.5f, 1 << Layers.World, QueryTriggerInteraction.Ignore);
        }

        private bool Spaced(Vector3 p, float distance)
        {
            foreach (Vector3 other in taken)
                if ((other - p).sqrMagnitude < distance * distance) return false;
            return true;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---------- Visuals ----------

        /// <summary>A stream of soft streaks flowing along local +Z from a disc of this radius.</summary>
        private ParticleSystem Stream(Transform parent, Vector3 localPosition, Quaternion localRotation, float radius, float speed,
            float life, float rate, Color color, Vector2 size)
        {
            var go = new GameObject("Wind") { layer = Layers.NoOutline };
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, localRotation);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.startSpeed = speed;
            main.startLifetime = life;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.maxParticles = Mathf.CeilToInt(rate * life * 1.3f) + 8;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = radius;
            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 1.5f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        /// <summary>A glowing circle in the local XY plane.</summary>
        private Transform Ring(Transform parent, Vector3 position, Quaternion rotation, float radius, Color color, float width)
        {
            var go = new GameObject("Ring") { layer = Layers.NoOutline };
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(position, rotation);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2f / 40f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            }
            line.widthMultiplier = width;
            line.sharedMaterial = lineMaterial;
            line.startColor = line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>Rings that keep travelling along local +Z of a child rotated by rotation (see WindRings).</summary>
        private void FlowingRings(Transform parent, Quaternion rotation, int count, float radius, float length, float speed, Color color, float width)
        {
            var holder = new GameObject("Wind Rings") { layer = Layers.NoOutline };
            holder.transform.SetParent(parent, false);
            holder.transform.localRotation = rotation;
            var lines = new LineRenderer[count];
            for (int i = 0; i < count; i++) lines[i] = Ring(holder.transform, Vector3.zero, Quaternion.identity, radius, color, width).GetComponent<LineRenderer>();
            holder.AddComponent<WindRings>().Init(lines, length, speed, color);
        }

        private static Mesh octahedron;

        /// <summary>A flat-shaded octahedron, unit size, for the crystals.</summary>
        private static Mesh Octahedron()
        {
            if (octahedron != null) return octahedron;
            Vector3[] p = { Vector3.up * 0.5f, Vector3.down * 0.5f, Vector3.right * 0.5f, Vector3.forward * 0.5f, Vector3.left * 0.5f, Vector3.back * 0.5f };
            int[][] faces = { new[] { 0, 3, 2 }, new[] { 0, 4, 3 }, new[] { 0, 5, 4 }, new[] { 0, 2, 5 },
                              new[] { 1, 2, 3 }, new[] { 1, 3, 4 }, new[] { 1, 4, 5 }, new[] { 1, 5, 2 } };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            foreach (int[] f in faces)
                foreach (int i in f) { triangles.Add(vertices.Count); vertices.Add(p[i]); }
            octahedron = new Mesh { name = "Crystal" };
            octahedron.SetVertices(vertices);
            octahedron.SetTriangles(triangles, 0);
            octahedron.RecalculateNormals();
            octahedron.RecalculateBounds();
            return octahedron;
        }
    }
}
