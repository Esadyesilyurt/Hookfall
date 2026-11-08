// Hookfall - spawns the horde ahead of and around the player, mixes in wyverns and island golems, sends elite waves,
// scales difficulty over time and pools every enemy (GDD 4.8, 5.1, 5.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 (pooling, wyverns, golems, elites, performance fallback); stages on 2026-11-08: time and
// growth restart every stage on top of the StageDirector's multipliers, the boss calls escorts, a new stage clears
// the sky. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.World;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Hookfall.Enemies
{
    public class EnemyDirector : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private IslandGenerator islands;
        [SerializeField] private EnemyBase batPrefab;
        [SerializeField] private EnemyBase wyvernPrefab;
        [SerializeField] private Golem golemPrefab;
        [SerializeField] private Boulder boulderPrefab;

        [Header("Spawning (GDD 4.8)")]
        [Tooltip("GDD 4.8: 2 per second. Raised to 5 on 2026-11-07, back to 2 on 2026-11-08 (too many enemies).")]
        [SerializeField] private float baseSpawnPerSecond = 2f;
        [Tooltip("+20% spawn rate per minute survived.")]
        [SerializeField] private float spawnGrowthPerMinute = 0.2f;
        [Tooltip("300 until 2026-11-08; the performance test still checks 300.")]
        [SerializeField] private int maxAlive = 120;
        [Tooltip("Distance for spawns around the player, placed out of camera view.")]
        [SerializeField] private float spawnDistance = 60f;
        [Tooltip("Distance for spawns ahead of the flight path; they come out of the haze.")]
        [SerializeField] private float aheadSpawnDistance = 90f;
        [Tooltip("Share of spawns placed ahead of the player's flight path.")]
        [SerializeField, Range(0f, 1f)] private float aheadSpawnShare = 0.6f;
        [Tooltip("Bats spawn in flocks of this many (min, max).")]
        [SerializeField] private Vector2Int flockSize = new Vector2Int(4, 8);
        [SerializeField] private float flockSpread = 4f;

        [Header("Wyverns")]
        [SerializeField] private float wyvernFromMinute = 1f;
        [SerializeField] private float wyvernShareStart = 0.05f;
        [SerializeField] private float wyvernSharePerMinute = 0.03f;
        [SerializeField] private float wyvernShareMax = 0.2f;

        [Header("Golems (GDD 4.8: from minute 2)")]
        [SerializeField] private float golemFromMinute = 2f;
        [SerializeField] private float golemInterval = 15f;
        [SerializeField] private int golemsMaxBase = 2;
        [SerializeField] private float golemsPerMinute = 1f;
        [SerializeField] private float golemIslandMaxDistance = 110f;

        [Header("Elite waves (GDD 4.8: every 5 minutes)")]
        [SerializeField] private float eliteInterval = 300f;
        [SerializeField] private int eliteBats = 15;
        [SerializeField] private int eliteWyverns = 3;
        [SerializeField] private int eliteGolems = 1;
        [SerializeField] private float eliteHealthMultiplier = 3f;
        [SerializeField] private float eliteDamageMultiplier = 1.5f;
        [SerializeField] private float eliteScale = 1.4f;
        [SerializeField] private float eliteRewardMultiplier = 3f;
        [SerializeField, ColorUsage(false, true)] private Color eliteGlow = new Color(0.9f, 0.2f, 0.85f);

        [Header("Scaling")]
        [SerializeField] private float healthGrowthPerMinute = 0.15f;
        [SerializeField] private float damageGrowthPerMinute = 0.08f;
        [Tooltip("Extra enemy damage per Warden level above 1, so lifesteal and armor do not make the horde harmless.")]
        [SerializeField] private float damageGrowthPerLevel = 0.1f;

        [Header("Performance fallback (GDD 5.7)")]
        [SerializeField] private float minFps = 50f;

        [Header("Arena")]
        [Tooltip("Spawns stay inside this horizontal radius (inside the storm wall).")]
        [SerializeField] private float arenaRadius = 370f;   // 180 until the map doubled (2026-11-08)

        private readonly Dictionary<EnemyBase, ObjectPool<EnemyBase>> pools = new Dictionary<EnemyBase, ObjectPool<EnemyBase>>();
        private ObjectPool<Boulder> boulders;
        private PlayerStats playerStats;
        private Hookfall.Progression.XPSystem playerXP;
        private float spawnBudget;
        private int flockLeft;
        private Vector3 flockCentre;
        private float golemTimer;
        private float nextEliteTime;
        private float smoothedFps = 60f;

        public static bool LowPerformance { get; private set; }
        public static event Action<EnemyBase, float> Hit;
        public static event Action<EnemyBase, bool> Died;
        public static event Action<Golem> Slammed;
        public static event Action<Vector3> BoulderBroke;
        public static event Action EliteWave;

        public int AliveCount => EnemyBase.Active.Count;
        public float AbyssHeight => islands != null ? islands.AbyssKillHeight : -70f;

        /// <summary>Minutes into the current stage (the run before stages existed).</summary>
        private float MinutesSurvived =>
            (StageDirector.Instance != null ? StageDirector.Instance.StageTime
                : GameManager.Instance != null ? GameManager.Instance.RunTime : Time.timeSinceLevelLoad) / 60f;

        private static float StageHealth => StageDirector.Instance != null ? StageDirector.Instance.HealthScale : 1f;
        private static float StageDamage => StageDirector.Instance != null ? StageDirector.Instance.DamageScale : 1f;
        private static float StageGrowth => StageDirector.Instance != null ? StageDirector.Instance.GrowthScale : 1f;
        private static float StageSpawns => StageDirector.Instance != null ? StageDirector.Instance.SpawnScale : 1f;

        /// <summary>Enemy health at spawn: the stage's base, growing with time (faster on later stages).</summary>
        private float HealthScaleAt(float minutes) => StageHealth * (1f + healthGrowthPerMinute * StageGrowth * minutes);

        /// <summary>Enemy damage at spawn: the stage's base, growing with time and with the Warden's level.</summary>
        private float DamageScaleAt(float minutes) =>
            StageDamage * (1f + StageGrowth * (damageGrowthPerMinute * minutes + damageGrowthPerLevel * ((playerXP != null ? playerXP.Level : 1) - 1)));

        public static void RaiseHit(EnemyBase enemy, float damage) => Hit?.Invoke(enemy, damage);
        public static void RaiseSlam(Golem golem) => Slammed?.Invoke(golem);

        private void Awake()
        {
            EnemyBase.Active.Clear();
            LowPerformance = false;
            nextEliteTime = eliteInterval;
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
            if (player != null) playerXP = player.GetComponent<Hookfall.Progression.XPSystem>();
            if (batPrefab == null) Debug.LogWarning("[Hookfall] EnemyDirector has no Bat prefab; no enemies will spawn.");
            boulders = new ObjectPool<Boulder>(
                () => Instantiate(boulderPrefab, transform),
                b => b.gameObject.SetActive(true),
                b => b.gameObject.SetActive(false),
                b => Destroy(b.gameObject), false, 16, 200);
        }

        private void Update()
        {
            if (player == null || batPrefab == null || Time.timeScale <= 0f) return;

            smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            LowPerformance = smoothedFps < minFps;

            float minutes = MinutesSurvived;
            bool full = AliveCount >= maxAlive;
            if (!full && !LowPerformance)
            {
                float spawnRate = baseSpawnPerSecond * StageSpawns * Mathf.Pow(1f + spawnGrowthPerMinute, minutes);
                spawnBudget += spawnRate * Time.deltaTime;
            }
            else spawnBudget = 0f;   // fallback: cap reached or frame rate low, pause spawning

            while (spawnBudget >= 1f && AliveCount < maxAlive)
            {
                spawnBudget -= 1f;
                if (wyvernPrefab != null && Random.value < WyvernShare(minutes)) SpawnFlyer(wyvernPrefab, PickSpawnPoint(), minutes);
                else SpawnFlockBat(minutes);
            }

            UpdateGolems(minutes);

            float stageTime = minutes * 60f;
            if (stageTime >= nextEliteTime)
            {
                nextEliteTime += eliteInterval;
                SpawnEliteWave(minutes);
            }
        }

        private float WyvernShare(float minutes) =>
            minutes < wyvernFromMinute ? 0f : Mathf.Min(wyvernShareMax, wyvernShareStart + wyvernSharePerMinute * (minutes - wyvernFromMinute));

        private void SpawnFlockBat(float minutes)
        {
            if (flockLeft <= 0)
            {
                flockLeft = Random.Range(flockSize.x, flockSize.y + 1);
                flockCentre = PickSpawnPoint();
            }
            flockLeft--;
            SpawnFlyer(batPrefab, flockCentre + Random.insideUnitSphere * flockSpread, minutes);
        }

        private EnemyBase SpawnFlyer(EnemyBase prefab, Vector3 position, float minutes)
        {
            EnemyBase enemy = PoolFor(prefab).Get();
            enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
            enemy.Init(this, player, HealthScaleAt(minutes), DamageScaleAt(minutes));
            if (!enemy.CanEnterBossZone) StageDirector.KeepBelowZone(enemy.transform);
            return enemy;
        }

        // ---------- Golems ----------

        private void UpdateGolems(float minutes)
        {
            if (golemPrefab == null || minutes < golemFromMinute) return;
            golemTimer -= Time.deltaTime;
            if (golemTimer > 0f) return;
            golemTimer = golemInterval;

            int golems = 0;
            foreach (EnemyBase e in EnemyBase.Active) if (e is Golem) golems++;
            if (golems < golemsMaxBase + Mathf.FloorToInt(golemsPerMinute * (minutes - golemFromMinute)) && AliveCount < maxAlive)
                SpawnGolem(minutes);
        }

        private Golem SpawnGolem(float minutes)
        {
            Island island = FindFreeIsland();
            if (island == null) return null;
            var golem = (Golem)PoolFor(golemPrefab).Get();
            PlaceGolem(golem, island);
            golem.Init(this, player, HealthScaleAt(minutes), DamageScaleAt(minutes));
            return golem;
        }

        private void PlaceGolem(Golem golem, Island island)
        {
            if (golem.HomeIsland != null && golem.HomeIsland.Occupant == golem) golem.HomeIsland.Occupant = null;
            Vector2 offset = Random.insideUnitCircle * island.Width * 0.2f;
            golem.transform.SetPositionAndRotation(island.Top + new Vector3(offset.x, 0f, offset.y), Quaternion.identity);
            golem.HomeIsland = island;
            island.Occupant = golem;
        }

        private Island FindFreeIsland()
        {
            Island best = null;
            float bestScore = float.MaxValue;
            Vector3 p = player.transform.position;
            foreach (Island island in Island.All)
            {
                if (island.Kind != IslandKind.Inner || island.Occupant != null) continue;
                if (player.CurrentIsland == island) continue;
                float d = Vector3.Distance(p, island.Top);
                if (d > golemIslandMaxDistance) continue;
                float score = d + Random.Range(0f, 40f);   // nearby, but not always the nearest
                if (score < bestScore) { bestScore = score; best = island; }
            }
            return best;
        }

        // ---------- Elite waves ----------

        private void SpawnEliteWave(float minutes)
        {
            Vector3 centre = PickSpawnPoint();
            for (int i = 0; i < eliteBats && AliveCount < maxAlive; i++)
                MakeElite(SpawnFlyer(batPrefab, centre + Random.insideUnitSphere * flockSpread * 1.5f, minutes));
            if (wyvernPrefab != null)
                for (int i = 0; i < eliteWyverns && AliveCount < maxAlive; i++)
                    MakeElite(SpawnFlyer(wyvernPrefab, PickSpawnPoint(), minutes));
            if (minutes >= golemFromMinute)
                for (int i = 0; i < eliteGolems; i++)
                {
                    Golem golem = SpawnGolem(minutes);
                    if (golem != null) MakeElite(golem);
                }
            EliteWave?.Invoke();
        }

        /// <summary>The boss calls its escort: a wyvern or a bat at a point near it (elites may enter the boss zone).</summary>
        public EnemyBase SpawnEscort(bool wyvern, Vector3 position, bool elite)
        {
            EnemyBase prefab = wyvern && wyvernPrefab != null ? wyvernPrefab : batPrefab;
            if (prefab == null) return null;
            EnemyBase enemy = SpawnFlyer(prefab, position, MinutesSurvived);
            if (elite) MakeElite(enemy);
            enemy.transform.position = position;   // an elite may stay up in the boss zone
            return enemy;
        }

        /// <summary>The boss itself: not pooled, scaled like the stage's enemies.</summary>
        public void InitBoss(EnemyBase boss) => boss.Init(this, player, StageHealth, StageDamage);

        /// <summary>A new stage: every enemy leaves the sky without a reward, and the waves start over.</summary>
        public void ClearForStage()
        {
            foreach (EnemyBase enemy in EnemyBase.Active.ToArray()) enemy.Die(false);
            spawnBudget = 0f;
            flockLeft = 0;
            golemTimer = 0f;
            nextEliteTime = eliteInterval;
        }

        private void MakeElite(EnemyBase enemy) =>
            enemy.MakeElite(eliteHealthMultiplier, eliteDamageMultiplier, eliteScale, eliteRewardMultiplier, eliteGlow);

        // ---------- Placement and fallbacks ----------

        /// <summary>Ahead of the player's flight path most of the time, otherwise around them and out of camera view.</summary>
        private Vector3 PickSpawnPoint()
        {
            Vector3 velocity = player.Body.linearVelocity;
            Vector3 origin = player.transform.position;

            if (velocity.sqrMagnitude > 25f && Random.value < aheadSpawnShare)
            {
                Vector3 ahead = Quaternion.AngleAxis(Random.Range(-60f, 60f), Vector3.up) * velocity.normalized;
                ahead.y = Mathf.Clamp(ahead.y, -0.3f, 0.5f);
                return InsideArena(origin + ahead.normalized * aheadSpawnDistance);
            }

            Camera cam = Camera.main;
            Vector3 point = origin;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Clamp(direction.y, -0.3f, 0.5f);
                point = origin + direction.normalized * spawnDistance;
                if (cam == null) break;
                Vector3 v = cam.WorldToViewportPoint(point);
                bool visible = v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
                if (!visible) break;
            }
            return InsideArena(point);
        }

        private Vector3 InsideArena(Vector3 p)
        {
            var flat = new Vector2(p.x, p.z);
            if (flat.magnitude <= arenaRadius) return p;
            flat = flat.normalized * arenaRadius;
            return new Vector3(flat.x, p.y, flat.y);
        }

        public void Relocate(EnemyBase enemy)
        {
            if (enemy is Golem golem)
            {
                Island island = FindFreeIsland();
                if (island != null) PlaceGolem(golem, island);
                else enemy.Die(false);
                return;
            }
            enemy.transform.position = PickSpawnPoint();
        }

        /// <summary>Called by EnemyBase.Die: returns the enemy to its pool.</summary>
        public void Release(EnemyBase enemy, bool killed)
        {
            Died?.Invoke(enemy, killed);
            if (enemy.SourcePrefab != null && pools.TryGetValue(enemy.SourcePrefab, out ObjectPool<EnemyBase> pool)) pool.Release(enemy);
            else Destroy(enemy.gameObject);
        }

        public void SpawnBoulder(Vector3 position, Vector3 velocity, float damage)
        {
            if (boulderPrefab == null) return;
            boulders.Get().Launch(this, playerStats, position, velocity, damage);
        }

        public void ReleaseBoulder(Boulder boulder, bool broke)
        {
            if (broke) BoulderBroke?.Invoke(boulder.transform.position);
            boulders.Release(boulder);
        }

        private ObjectPool<EnemyBase> PoolFor(EnemyBase prefab)
        {
            if (pools.TryGetValue(prefab, out ObjectPool<EnemyBase> pool)) return pool;
            pool = new ObjectPool<EnemyBase>(
                () =>
                {
                    EnemyBase e = Instantiate(prefab, transform);
                    e.SourcePrefab = prefab;
                    return e;
                },
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => Destroy(e.gameObject),
                false, 64, maxAlive + 50);
            pools[prefab] = pool;
            return pool;
        }

        private void OnDestroy()
        {
            EnemyBase.Active.Clear();
            Hit = null;
            Died = null;
            Slammed = null;
            BoulderBroke = null;
            EliteWave = null;
        }
    }
}
