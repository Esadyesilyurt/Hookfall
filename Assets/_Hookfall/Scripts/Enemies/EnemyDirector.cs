// Hookfall - spawns the horde ahead of and around the player, mixes in wyverns and island golems, sends elite waves,
// scales difficulty over time and pools every enemy (GDD 4.8, 5.1, 5.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton,
// rewritten on 2026-11-07 (pooling, wyverns, golems, elites, performance fallback); stages on 2026-11-08: time and
// growth restart every stage on top of the StageDirector's multipliers, the boss calls escorts, a new stage clears
// the sky; 2026-11-09: a busier first stage, a low frame rate holds the horde instead of emptying the sky, and flyers
// never appear inside an island or on a large one; 2026-11-10: four more kinds of enemy join the mix as a stage goes
// on (Blightpuffs, Razorwing flocks, Venomwings, Gloom Jellies), elites carry a trait, and bursts are pooled here.
// Prompts and changes are recorded in Docs/PromptLog.md.
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
        [Header("The wider horde (2026-11-10)")]
        [SerializeField] private Blightpuff blightpuffPrefab;
        [SerializeField] private Razorwing razorwingPrefab;
        [SerializeField] private Venomwing venomwingPrefab;
        [SerializeField] private GloomJelly jellyPrefab;
        [Tooltip("Material of the rings that warn of a burst.")]
        [SerializeField] private Material warningMaterial;

        [Header("Spawning (GDD 4.8)")]
        [Tooltip("GDD 4.8: 2 per second. Raised to 5 on 2026-11-07, back to 2 on 2026-11-08 (too many enemies), " +
                 "3 on 2026-11-09 (too few on the doubled map).")]
        [SerializeField] private float baseSpawnPerSecond = 3f;
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

        [Header("Blightpuffs: a share of the spawns, like wyverns")]
        [SerializeField] private float puffFromMinute = 1.5f;
        [SerializeField] private float puffShareStart = 0.05f;
        [SerializeField] private float puffSharePerMinute = 0.02f;
        [SerializeField] private float puffShareMax = 0.12f;

        [Header("Venomwings")]
        [SerializeField] private float venomFromMinute = 3.5f;
        [SerializeField] private float venomShareStart = 0.04f;
        [SerializeField] private float venomSharePerMinute = 0.02f;
        [SerializeField] private float venomShareMax = 0.1f;

        [Header("Gloom Jellies")]
        [SerializeField] private float jellyFromMinute = 4.5f;
        [SerializeField] private float jellyShareStart = 0.02f;
        [SerializeField] private float jellySharePerMinute = 0.01f;
        [SerializeField] private float jellyShareMax = 0.05f;
        [Tooltip("At most this many grown jellies at once.")]
        [SerializeField] private int jellyMaxAlive = 6;

        [Header("Razorwings: a whole flock for one spawn")]
        [SerializeField] private float razorFromMinute = 2.5f;
        [SerializeField] private float razorChanceStart = 0.02f;
        [SerializeField] private float razorChancePerMinute = 0.008f;
        [SerializeField] private float razorChanceMax = 0.05f;
        [SerializeField] private Vector2Int razorFlockSize = new Vector2Int(5, 7);

        [Tooltip("On later stages every kind arrives this many minutes earlier per stage.")]
        [SerializeField] private float stageHeadStart = 2f;

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
        [Tooltip("Glow of each elite trait, in EliteAffix order after None: Shielded, Swift, Explosive, Splitting.")]
        [SerializeField, ColorUsage(false, true)] private Color[] affixGlow =
        {
            new Color(0.3f, 0.75f, 1.7f), new Color(1.7f, 1.35f, 0.2f), new Color(1.9f, 0.55f, 0.12f), new Color(0.45f, 1.6f, 0.45f)
        };
        [Tooltip("An Explosive elite's burst: reach, damage to the Warden, damage to enemies, seconds of warning.")]
        [SerializeField] private Vector4 eliteBurst = new Vector4(6.5f, 22f, 60f, 0.4f);

        [Header("Scaling")]
        [SerializeField] private float healthGrowthPerMinute = 0.15f;
        [SerializeField] private float damageGrowthPerMinute = 0.08f;
        [Tooltip("Extra enemy damage per Warden level above 1, so lifesteal and armor do not make the horde harmless.")]
        [SerializeField] private float damageGrowthPerLevel = 0.1f;

        [Header("Performance fallback (GDD 5.7)")]
        [SerializeField] private float minFps = 50f;
        [Tooltip("Below minFps the horde stops growing past this size. Until 2026-11-09 spawning stopped outright, " +
                 "which could leave the sky almost empty in the editor.")]
        [SerializeField] private int lowPerformanceCap = 60;

        [Header("Arena")]
        [Tooltip("Spawns stay inside this horizontal radius (inside the storm wall).")]
        [SerializeField] private float arenaRadius = 370f;   // 180 until the map doubled (2026-11-08)

        private readonly Dictionary<EnemyBase, ObjectPool<EnemyBase>> pools = new Dictionary<EnemyBase, ObjectPool<EnemyBase>>();
        private readonly Dictionary<Boulder, ObjectPool<Boulder>> shots = new Dictionary<Boulder, ObjectPool<Boulder>>();
        private ObjectPool<Blast> blasts;
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
        /// <summary>A burst went off: where, and how far it reached.</summary>
        public static event Action<Vector3, float> Exploded;
        /// <summary>A jelly's shock: where, and how far it reached.</summary>
        public static event Action<Vector3, float> Pulsed;
        /// <summary>A Shielded elite's barrier broke.</summary>
        public static event Action<EnemyBase> ShieldBroken;

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
        private static int Stage => StageDirector.Instance != null ? StageDirector.Instance.Stage : 1;

        /// <summary>Enemy health at spawn: the stage's base, growing with time (faster on later stages).</summary>
        private float HealthScaleAt(float minutes) => StageHealth * (1f + healthGrowthPerMinute * StageGrowth * minutes);

        /// <summary>Enemy damage at spawn: the stage's base, growing with time and with the Warden's level.</summary>
        private float DamageScaleAt(float minutes) =>
            StageDamage * (1f + StageGrowth * (damageGrowthPerMinute * minutes + damageGrowthPerLevel * ((playerXP != null ? playerXP.Level : 1) - 1)));

        public static void RaiseHit(EnemyBase enemy, float damage) => Hit?.Invoke(enemy, damage);
        public static void RaiseSlam(Golem golem) => Slammed?.Invoke(golem);
        public static void RaisePulse(Vector3 position, float radius) => Pulsed?.Invoke(position, radius);
        public static void RaiseShieldBroken(EnemyBase enemy) => ShieldBroken?.Invoke(enemy);

        private void Awake()
        {
            EnemyBase.Active.Clear();
            LowPerformance = false;
            nextEliteTime = eliteInterval;
            if (player != null) playerStats = player.GetComponent<PlayerStats>();
            if (player != null) playerXP = player.GetComponent<Hookfall.Progression.XPSystem>();
            if (batPrefab == null) Debug.LogWarning("[Hookfall] EnemyDirector has no Bat prefab; no enemies will spawn.");
            blasts = new ObjectPool<Blast>(
                () =>
                {
                    var blast = new GameObject("Blast").AddComponent<Blast>();
                    blast.transform.SetParent(transform, false);
                    blast.Build(warningMaterial);
                    return blast;
                },
                b => b.gameObject.SetActive(true),
                b => b.gameObject.SetActive(false),
                b => Destroy(b.gameObject), false, 8, 64);
        }

        private void Update()
        {
            if (player == null || batPrefab == null || Time.timeScale <= 0f) return;

            smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            LowPerformance = smoothedFps < minFps;

            float minutes = MinutesSurvived;
            int cap = LowPerformance ? Mathf.Min(maxAlive, lowPerformanceCap) : maxAlive;
            if (AliveCount < cap)
            {
                float spawnRate = baseSpawnPerSecond * StageSpawns * Mathf.Pow(1f + spawnGrowthPerMinute, minutes);
                spawnBudget += spawnRate * Time.deltaTime;
            }
            else spawnBudget = 0f;   // cap reached (a lower one while the frame rate is low): wait

            while (spawnBudget >= 1f && AliveCount < cap)
            {
                spawnBudget -= 1f;
                SpawnOne(minutes);
            }

            UpdateGolems(minutes);

            float stageTime = minutes * 60f;
            if (stageTime >= nextEliteTime)
            {
                nextEliteTime += eliteInterval;
                SpawnEliteWave(minutes);
            }
        }

        /// <summary>One spawn of the budget: which kind it is depends on how far the stage has gone.</summary>
        private void SpawnOne(float minutes)
        {
            // Every kind has its minute; on later stages they all come sooner.
            float clock = minutes + (Stage - 1) * stageHeadStart;
            float roll = Random.value;
            float edge = wyvernPrefab != null ? Share(clock, wyvernFromMinute, wyvernShareStart, wyvernSharePerMinute, wyvernShareMax) : 0f;
            if (roll < edge) { SpawnFlyer(wyvernPrefab, PickSpawnPoint(), minutes); return; }
            edge += blightpuffPrefab != null ? Share(clock, puffFromMinute, puffShareStart, puffSharePerMinute, puffShareMax) : 0f;
            if (roll < edge) { SpawnFlyer(blightpuffPrefab, PickSpawnPoint(), minutes); return; }
            edge += venomwingPrefab != null ? Share(clock, venomFromMinute, venomShareStart, venomSharePerMinute, venomShareMax) : 0f;
            if (roll < edge) { SpawnFlyer(venomwingPrefab, PickSpawnPoint(), minutes); return; }
            edge += jellyPrefab != null && GrownJellies() < jellyMaxAlive ? Share(clock, jellyFromMinute, jellyShareStart, jellySharePerMinute, jellyShareMax) : 0f;
            if (roll < edge) { SpawnFlyer(jellyPrefab, PickSpawnPoint(), minutes); return; }
            edge += razorwingPrefab != null ? Share(clock, razorFromMinute, razorChanceStart, razorChancePerMinute, razorChanceMax) : 0f;
            if (roll < edge)
            {
                // A flock is several enemies for one roll: it uses up that much of the budget.
                spawnBudget -= SpawnRazorFlock(minutes, false) - 1;
                return;
            }
            SpawnFlockBat(minutes);
        }

        /// <summary>A kind's share of the spawns: nothing before its minute, then growing to its ceiling.</summary>
        private static float Share(float clock, float fromMinute, float start, float perMinute, float max) =>
            clock < fromMinute ? 0f : Mathf.Min(max, start + perMinute * (clock - fromMinute));

        private static int GrownJellies()
        {
            int count = 0;
            foreach (EnemyBase e in EnemyBase.Active) if (e is GloomJelly jelly && !jelly.IsYoung) count++;
            return count;
        }

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

        /// <summary>A V of Razorwings pointed at the Warden; returns how many were spawned.</summary>
        private int SpawnRazorFlock(float minutes, bool elite)
        {
            int size = Random.Range(razorFlockSize.x, razorFlockSize.y + 1);
            Vector3 lead = PickSpawnPoint();
            Vector3 forward = Vector3.ProjectOnPlane(player.transform.position - lead, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            var flock = new Flock();
            int spawned = 0;
            for (int i = 0; i < size && AliveCount < maxAlive; i++)
            {
                int rank = (i + 1) / 2;
                float side = i % 2 == 1 ? -1f : 1f;
                var bird = (Razorwing)SpawnFlyer(razorwingPrefab, lead - forward * (rank * 3f) + right * (side * rank * 3f), minutes);
                bird.transform.rotation = Quaternion.LookRotation(forward);
                flock.Add(bird);
                if (elite) MakeElite(bird);
                spawned++;
            }
            return Mathf.Max(1, spawned);
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
            // From the second stage on an elite flock of Razorwings comes with the wave.
            if (razorwingPrefab != null && Stage > 1 && AliveCount < maxAlive) SpawnRazorFlock(minutes, true);
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

        /// <summary>A mini boss (Altars): a wyvern many times as tough, big and rewarding, with no elite trait.</summary>
        public EnemyBase SpawnMiniBoss(Vector3 position, float healthMult, float damageMult, float scale, float rewardMult)
        {
            EnemyBase prefab = wyvernPrefab != null ? wyvernPrefab : batPrefab;
            if (prefab == null) return null;
            EnemyBase enemy = SpawnFlyer(prefab, position, MinutesSurvived);
            enemy.MakeElite(healthMult, damageMult, scale, rewardMult, eliteGlow, EliteAffix.None, eliteGlow);
            enemy.transform.position = position;
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

        /// <summary>Makes an enemy an elite with a trait of its own. Golems stand still, so they are never Swift, and
        /// they do not split.</summary>
        private void MakeElite(EnemyBase enemy)
        {
            EliteAffix affix = enemy is Golem
                ? (Random.value < 0.5f ? EliteAffix.Shielded : EliteAffix.Explosive)
                : (EliteAffix)Random.Range(1, 5);
            enemy.MakeElite(eliteHealthMultiplier, eliteDamageMultiplier, eliteScale, eliteRewardMultiplier, GlowOf(affix), affix, eliteGlow);
        }

        private Color GlowOf(EliteAffix affix)
        {
            int index = (int)affix - 1;
            return index >= 0 && index < affixGlow.Length ? affixGlow[index] : eliteGlow;
        }

        /// <summary>A Splitting elite died: two ordinary enemies of its kind take its place.</summary>
        public void Split(EnemyBase elite)
        {
            if (elite.SourcePrefab == null || elite is Golem) return;
            for (int i = 0; i < 2 && AliveCount < maxAlive + 10; i++)
            {
                EnemyBase child = SpawnFlyer(elite.SourcePrefab, elite.transform.position + Random.insideUnitSphere * 1.5f, MinutesSurvived);
                if (child is Razorwing bird) new Flock().Add(bird);   // a flock of its own, or it has no slot to fly to
            }
        }

        /// <summary>An Explosive elite died: it bursts after a short warning.</summary>
        public void EliteBurst(EnemyBase elite) =>
            Explode(elite.transform.position, eliteBurst.x * elite.SizeFactor, eliteBurst.y * elite.DamageScale, eliteBurst.z, eliteBurst.w);

        /// <summary>A grown jelly died: its young drift out of it.</summary>
        public void SpawnYoung(GloomJelly parent, int count)
        {
            if (parent.SourcePrefab == null) return;
            for (int i = 0; i < count && AliveCount < maxAlive + 10; i++)
            {
                var child = (GloomJelly)SpawnFlyer(parent.SourcePrefab, parent.transform.position + Random.insideUnitSphere * 1.8f, MinutesSurvived);
                child.MakeYoung();
            }
        }

        // ---------- Bursts and shots ----------

        /// <summary>A burst at a point: rings warn for `delay` seconds, then everything within `radius` is hurt.</summary>
        public void Explode(Vector3 position, float radius, float playerDamage, float enemyDamage, float delay) =>
            blasts.Get().Begin(this, playerStats, position, radius, playerDamage, enemyDamage, delay);

        /// <summary>Called by a burst when it has gone off.</summary>
        public void FinishBlast(Blast blast, float radius)
        {
            Exploded?.Invoke(blast.transform.position, radius);
            blasts.Release(blast);
        }

        public void SpawnBoulder(Vector3 position, Vector3 velocity, float damage) => SpawnShot(boulderPrefab, position, velocity, damage);

        /// <summary>Launches a pooled shot (a golem's boulder, a wasp's venom).</summary>
        public void SpawnShot(Boulder prefab, Vector3 position, Vector3 velocity, float damage)
        {
            if (prefab == null) return;
            if (!shots.TryGetValue(prefab, out ObjectPool<Boulder> pool))
            {
                pool = new ObjectPool<Boulder>(
                    () =>
                    {
                        Boulder shot = Instantiate(prefab, transform);
                        shot.SourcePrefab = prefab;
                        return shot;
                    },
                    b => b.gameObject.SetActive(true),
                    b => b.gameObject.SetActive(false),
                    b => Destroy(b.gameObject), false, 16, 200);
                shots[prefab] = pool;
            }
            pool.Get().Launch(this, playerStats, position, velocity, damage);
        }

        public void ReleaseBoulder(Boulder boulder, bool broke)
        {
            if (broke && !boulder.Quiet) BoulderBroke?.Invoke(boulder.transform.position);
            if (boulder.SourcePrefab != null && shots.TryGetValue(boulder.SourcePrefab, out ObjectPool<Boulder> pool)) pool.Release(boulder);
            else Destroy(boulder.gameObject);
        }

        // ---------- Placement and fallbacks ----------

        /// <summary>A spawn point in open sky: never inside an island's rock or on a large island (kept empty).</summary>
        private Vector3 PickSpawnPoint()
        {
            Vector3 point = SpawnCandidate();
            for (int attempt = 0; attempt < 6 && InsideIsland(point); attempt++) point = SpawnCandidate();
            return point;
        }

        /// <summary>Inside an island (its rock hangs about its width below the top) or just above a large island.</summary>
        private static bool InsideIsland(Vector3 p)
        {
            foreach (Island island in Island.All)
            {
                Vector3 top = island.Top;
                float radius = island.Width * 0.6f + 2f;
                float dx = p.x - top.x, dz = p.z - top.z;
                if (dx * dx + dz * dz > radius * radius) continue;
                float above = island.Kind == IslandKind.Large ? 14f : 3f;
                if (p.y < top.y + above && p.y > top.y - island.Width - 2f) return true;
            }
            return false;
        }

        /// <summary>Ahead of the player's flight path most of the time, otherwise around them and out of camera view.</summary>
        private Vector3 SpawnCandidate()
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

        /// <summary>Called by EnemyBase.Die at the moment of death (effects and sound hang on it).</summary>
        public void NotifyDeath(EnemyBase enemy, bool killed)
        {
            if (killed) Hookfall.Core.Codex.AddKill(enemy.GetType().Name);   // for the menu's codex
            Died?.Invoke(enemy, killed);
        }

        /// <summary>Called by EnemyBase once the enemy is done (after its death pop): back to its pool.</summary>
        public void Recycle(EnemyBase enemy)
        {
            if (enemy.SourcePrefab != null && pools.TryGetValue(enemy.SourcePrefab, out ObjectPool<EnemyBase> pool)) pool.Release(enemy);
            else Destroy(enemy.gameObject);
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
            Exploded = null;
            Pulsed = null;
            ShieldBroken = null;
        }
    }
}
