// Hookfall - stages (2026-11-08). Every stage is a fresh map with a boss at its end. Gold earned on the stage fills the
// boss bar; when it is full the Abyss Colossus rises above the tower with its own ring of islands and a way up, and only
// wyverns and elites can follow the Warden up there (the boss zone). Its death raises an island with a portal where it
// stood and offers a Legendary pick; stepping into the portal starts the next stage: a new map, the Warden back to
// level 1 at the foot of the tower with their items, stats and gold, and enemies that start stronger and grow faster.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections;
using Hookfall.Audio;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Feel;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using Hookfall.VFX;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hookfall.World
{
    public class StageDirector : MonoBehaviour
    {
        public enum Phase { Gathering, Boss, Portal, Travel }

        private static readonly Color BossColor = new Color(1f, 0.3f, 0.85f);
        private static readonly Color PortalColor = new Color(0.45f, 0.95f, 1f);
        private static readonly string[] BossNames =
            { "Abyss Colossus", "Storm-Crowned Colossus", "Heartless Colossus", "Colossus of the Last Sky" };

        public static StageDirector Instance { get; private set; }

        [Header("Scene")]
        [SerializeField] private PlayerController player;
        [SerializeField] private IslandGenerator islands;
        [SerializeField] private EnemyDirector enemies;
        [SerializeField] private ChestSpawner chests;
        [SerializeField] private TowerCache towerCache;
        [SerializeField] private SkyFeatures sky;
        [SerializeField] private PowerUpSpawner powerUps;
        [SerializeField] private PickupSpawner pickups;
        [SerializeField] private CutsceneDirector cutscenes;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private VfxManager vfx;
        [SerializeField] private FeelDirector feel;

        [Header("Prefabs")]
        [SerializeField] private Boss bossPrefab;
        [SerializeField] private Portal portalPrefab;
        [Tooltip("Additive material for the boss's beacon and the portal's beam (M_LightBeam).")]
        [SerializeField] private Material beamMaterial;

        [Header("The boss bar: gold earned on the stage")]
        [Tooltip("1500 until 2026-11-09, read off a probe circling the tower; Esad never reached it. A probe crossing the " +
                 "map at full speed (BalanceProbe.AcrossTheMap) puts 1000 at about 4 minutes for the Ember Witch and about " +
                 "8 for a Warden who never opens a chest.")]
        [SerializeField] private int firstBossGold = 1000;
        [SerializeField] private float bossGoldPerStage = 1.6f;

        [Header("Enemies on later stages (each multiplied once per stage after the first)")]
        [SerializeField] private float healthPerStage = 2.5f;
        [SerializeField] private float damagePerStage = 1.8f;
        [Tooltip("Health and damage growth per minute and per Warden level get this much faster every stage.")]
        [SerializeField] private float growthPerStage = 1.35f;
        [SerializeField] private float spawnsPerStage = 1.2f;
        [Tooltip("XP and gold per kill, so the Warden levels up again quickly and the boss bar keeps its pace.")]
        [SerializeField] private float rewardsPerStage = 1.5f;

        [Header("Boss arena")]
        [SerializeField] private Vector3 arenaCentre = new Vector3(0f, 78f, 0f);
        [SerializeField] private int arenaIslands = 6;
        [SerializeField] private float arenaRing = 30f;
        [Tooltip("Island tops relative to the boss (min, max).")]
        [SerializeField] private Vector2 arenaIslandHeight = new Vector2(-24f, -14f);
        [SerializeField] private Vector2 arenaIslandWidth = new Vector2(11f, 15f);
        [Tooltip("The plain horde cannot rise above this far below the boss inside the arena column.")]
        [SerializeField] private float zoneFloorBelowBoss = 30f;
        [SerializeField] private float zoneRadius = 85f;
        [SerializeField] private float portalIslandWidth = 16f;
        [SerializeField] private float portalIslandBelowBoss = 10f;

        public int Stage { get; private set; } = 1;
        /// <summary>Seconds of play on this stage (the EnemyDirector's waves restart with it).</summary>
        public float StageTime { get; private set; }
        public Phase Current { get; private set; } = Phase.Gathering;
        public Boss Boss { get; private set; }
        public Portal Portal { get; private set; }
        public int GoldTarget => Mathf.RoundToInt(firstBossGold * Mathf.Pow(bossGoldPerStage, Stage - 1) / 10f) * 10;
        public int GoldProgress => wallet != null ? wallet.Earned - earnedAtStart : 0;
        public float HealthScale { get; private set; } = 1f;
        public float DamageScale { get; private set; } = 1f;
        public float GrowthScale { get; private set; } = 1f;
        public float SpawnScale { get; private set; } = 1f;
        public Vector3 ArenaCentre => arenaCentre;
        public string BossName => Stage <= BossNames.Length ? Loc.T(BossNames[Stage - 1])
            : $"{Loc.T(BossNames[BossNames.Length - 1])} {Stage - BossNames.Length + 1}";
        public string BossTitle => Loc.F("Guardian of Stage {0}", Stage);

        /// <summary>XP and gold per kill on this stage (EnemyBase.Die).</summary>
        public static float RewardScale => Instance != null ? Instance.rewardScale : 1f;

        public static event Action<int> StageStarted;
        public static event Action BossArrived;
        public static event Action BossDefeated;

        // The boss zone: a column over the arena that the plain horde cannot climb into (read by every enemy, every frame).
        public static bool ZoneActive { get; private set; }
        private static Vector3 zoneCentre;
        private static float zoneRadiusSqr, zoneFloor;

        private GoldWallet wallet;
        private XPSystem xp;
        private PlayerStats stats;
        private CombatSystem combat;
        private GrappleHook hook;
        private int earnedAtStart;
        private float rewardScale = 1f;
        private Vector3 startPosition;
        private GameObject arena;

        /// <summary>Holds an enemy that may not enter the boss zone under its floor.</summary>
        public static void KeepBelowZone(Transform enemy)
        {
            if (!ZoneActive) return;
            Vector3 p = enemy.position;
            if (p.y <= zoneFloor) return;
            float dx = p.x - zoneCentre.x, dz = p.z - zoneCentre.z;
            if (dx * dx + dz * dz > zoneRadiusSqr) return;
            p.y = zoneFloor;
            enemy.position = p;
        }

        private void Awake()
        {
            Instance = this;
            ZoneActive = false;
            wallet = player.GetComponent<GoldWallet>();
            xp = player.GetComponent<XPSystem>();
            stats = player.GetComponent<PlayerStats>();
            combat = player.GetComponent<CombatSystem>();
            hook = player.GetComponent<GrappleHook>();
            startPosition = player.transform.position;
            ApplyScales();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            ZoneActive = false;
            StageStarted = null;
            BossArrived = null;
            BossDefeated = null;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != RunState.Playing) return;
            StageTime += Time.deltaTime;
            switch (Current)
            {
                case Phase.Gathering:
                    HUD.SetObjective(null, null, Color.white);
                    if (GoldProgress >= GoldTarget && bossPrefab != null) StartCoroutine(BossRises());
                    break;
                case Phase.Boss:
                    HUD.SetObjective(Boss != null && Boss.Fighting ? Boss.CorePosition : (Vector3?)null, Loc.T("Boss"), BossColor);
                    if (audioManager != null) audioManager.KeepMusicFull();
                    break;
                case Phase.Portal:
                    HUD.SetObjective(Portal != null ? Portal.Centre : (Vector3?)null, Loc.T("Portal"), PortalColor);
                    break;
            }
        }

        private void ApplyScales()
        {
            int n = Stage - 1;
            HealthScale = Mathf.Pow(healthPerStage, n);
            DamageScale = Mathf.Pow(damagePerStage, n);
            GrowthScale = Mathf.Pow(growthPerStage, n);
            SpawnScale = Mathf.Pow(spawnsPerStage, n);
            rewardScale = Mathf.Pow(rewardsPerStage, n);
        }

        private void SetZone(bool on)
        {
            ZoneActive = on;
            zoneCentre = arenaCentre;
            zoneRadiusSqr = zoneRadius * zoneRadius;
            zoneFloor = arenaCentre.y - zoneFloorBelowBoss;
        }

        // ---------- The boss ----------

        /// <summary>Calls the boss now, whatever the gold (tests, and the gold bar when it fills).</summary>
        public void SummonBoss()
        {
            if (Current == Phase.Gathering && bossPrefab != null) StartCoroutine(BossRises());
        }

        private IEnumerator BossRises()
        {
            Current = Phase.Boss;
            GameManager gm = GameManager.Instance;
            while (gm != null && gm.State != RunState.Playing) yield return null;   // a choice screen finishes first

            BuildArena();
            Boss = Instantiate(bossPrefab, arenaCentre + Vector3.up * 55f, Quaternion.identity);
            Boss.name = "Boss";
            Boss.Bind(vfx, audioManager, feel);
            enemies.InitBoss(Boss);
            Boss.Face(player.transform.position);
            if (beamMaterial != null) LightBeam.Build(Boss.transform, beamMaterial, 260f, 2.2f, new Color(BossColor.r, BossColor.g, BossColor.b, 0.28f));
            SetZone(true);
            BossArrived?.Invoke();
            if (cutscenes != null) yield return cutscenes.BossEntrance(Boss, arena.transform, arenaCentre, BossName, BossTitle);
            else Boss.Descend(arenaCentre, 1f);
            if (Boss != null) Boss.BeginFight();
        }

        /// <summary>The boss's islands in a ring below it and the updrafts and hook anchors that lead up to them.</summary>
        private void BuildArena()
        {
            arena = new GameObject("Boss Arena");
            arena.transform.SetParent(transform, false);
            Random.State previous = Random.state;
            Random.InitState((islands != null ? islands.UsedSeed : 0) + 31 * Stage);
            float start = Random.Range(0f, 360f);
            for (int i = 0; i < arenaIslands; i++)
            {
                float a = (start + i * 360f / arenaIslands + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
                float r = arenaRing + Random.Range(-3f, 5f);
                Vector3 top = arenaCentre + new Vector3(Mathf.Cos(a) * r, Random.Range(arenaIslandHeight.x, arenaIslandHeight.y), Mathf.Sin(a) * r);
                islands.BuildExtraIsland(arena.transform, top, Random.Range(arenaIslandWidth.x, arenaIslandWidth.y), IslandKind.Boss, true);
            }
            if (sky != null) sky.BuildBossAccess(arena.transform, arenaCentre + Vector3.up * arenaIslandHeight.x, arenaRing, start + 180f / arenaIslands);
            Random.state = previous;
        }

        /// <summary>Called by the boss as it dies; a boss cleared away by a new stage (killed false) leaves nothing.</summary>
        public void BossDied(Boss boss, bool killed, Transform remains)
        {
            if (boss != Boss) return;
            Boss = null;
            SetZone(false);
            if (!killed)
            {
                if (remains != null) Destroy(remains.gameObject);
                return;
            }
            Current = Phase.Portal;
            BossDefeated?.Invoke();
            StartCoroutine(BossFalls(remains, boss.transform.position));
        }

        private IEnumerator BossFalls(Transform remains, Vector3 at)
        {
            HUD.Announce(Loc.T("THE COLOSSUS FALLS"), UITheme.Gold);
            if (audioManager != null) audioManager.Play(SoundId.BossDeath, at);
            if (feel != null) feel.Shake(1.4f);
            GameManager gm = GameManager.Instance;
            bool slow = gm == null || gm.State == RunState.Playing;
            if (slow) Time.timeScale = 0.3f;   // a moment of slow motion as it breaks

            if (remains != null)
            {
                Vector3 from = remains.position;
                Quaternion rotation = remains.rotation;
                float t = 0f, nextBurst = 0f;
                while (t < 2.4f)
                {
                    t += Time.unscaledDeltaTime;
                    if (slow && t > 1f) { slow = false; RestoreTime(); }
                    float k = t / 2.4f;
                    remains.position = from + Vector3.down * (k * k * 26f) + Random.insideUnitSphere * (0.5f * (1f - k));
                    remains.rotation = rotation * Quaternion.Euler(k * 25f, 0f, k * 12f);
                    remains.localScale = Vector3.one * (1f - 0.55f * k * k);
                    if (t >= nextBurst && vfx != null)
                    {
                        nextBurst += 0.16f;
                        vfx.Burst(remains.position + Random.insideUnitSphere * 6f, 26, BossColor, 10f, 0.4f, 0.8f);
                    }
                    yield return null;
                }
                if (vfx != null) vfx.Burst(remains.position, 140, BossColor, 18f, 0.5f, 1.2f);
                Destroy(remains.gameObject);
            }
            if (slow) RestoreTime();
            if (pickups != null) pickups.MagnetiseAll();   // the boss's gold and XP fly to the Warden

            yield return RaisePortal(at);
            while (gm != null && gm.State != RunState.Playing) yield return null;
            ChestResultUI.OfferSeries(combat, stats, Rarity.Legendary, 1, Loc.T("BOSS REWARD"));
        }

        private static void RestoreTime()
        {
            GameManager gm = GameManager.Instance;
            if (Time.timeScale > 0f && Time.timeScale < 1f && (gm == null || gm.State == RunState.Playing)) Time.timeScale = 1f;
        }

        /// <summary>An island rises where the boss stood, with the portal on it.</summary>
        private IEnumerator RaisePortal(Vector3 at)
        {
            Vector3 top = new Vector3(arenaCentre.x, arenaCentre.y - portalIslandBelowBoss, arenaCentre.z);
            if (arena == null) arena = new GameObject("Boss Arena");
            Island island = islands.BuildExtraIsland(arena.transform, top, portalIslandWidth, IslandKind.Boss, false);
            Portal = Instantiate(portalPrefab, island.transform);
            Portal.transform.localPosition = Vector3.zero;
            Vector3 flat = player.transform.position - top;
            flat.y = 0f;
            Portal.transform.rotation = flat.sqrMagnitude > 1f ? Quaternion.LookRotation(flat) : Quaternion.identity;
            Portal.Init(this, player);
            if (beamMaterial != null) LightBeam.Build(Portal.transform, beamMaterial, 240f, 1.6f, new Color(PortalColor.r, PortalColor.g, PortalColor.b, 0.3f));
            if (audioManager != null) audioManager.Play(SoundId.PortalOpen, top);
            HUD.Announce(Loc.T("A portal opens"), PortalColor);

            Transform rising = island.transform;
            Vector3 from = top + Vector3.down * 30f;
            float t = 0f;
            while (t < 2f && rising != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 2f);
                rising.position = Vector3.Lerp(from, top, k * k * (3f - 2f * k));
                yield return null;
            }
            if (rising != null) rising.position = top;
            if (vfx != null) vfx.Ring(top + Vector3.up, 60, PortalColor, 14f);
        }

        // ---------- The portal and the next stage ----------

        public void EnterPortal(Portal portal)
        {
            if (Current != Phase.Portal || portal != Portal) return;
            Current = Phase.Travel;
            StartCoroutine(Travel());
        }

        private IEnumerator Travel()
        {
            string title = Loc.F("STAGE {0}", Stage + 1);
            string subtitle = Loc.T("The sky grows darker. The horde grows stronger.");
            if (cutscenes != null) yield return cutscenes.PortalTravel(Portal, NextStage, title, subtitle);
            else NextStage();
            Current = Phase.Gathering;
        }

        /// <summary>The new stage: a new map, the Warden at the foot of the tower at level 1 with everything they own.</summary>
        public void NextStage()
        {
            Stage++;
            StageTime = 0f;
            earnedAtStart = wallet != null ? wallet.Earned : 0;
            ApplyScales();
            SetZone(false);
            if (Boss != null) Boss.Die(false);
            Boss = null;
            Portal = null;
            if (arena != null)
            {
                arena.SetActive(false);   // its islands leave Island.All now, before the new layout is read
                Destroy(arena);
                arena = null;
            }
            if (hook != null) hook.Release();
            enemies.ClearForStage();
            if (pickups != null) pickups.ReleaseAll();
            if (powerUps != null) powerUps.ClearAll();
            if (chests != null) chests.ResetForStage();
            if (towerCache != null) towerCache.ResetForStage();
            islands.Regenerate();
            if (sky != null) sky.Rebuild();
            if (xp != null) xp.ResetLevel();
            if (stats != null) stats.Heal(stats.MaxHealth);
            player.Body.linearVelocity = Vector3.zero;
            player.Body.position = startPosition;
            player.transform.position = startPosition;
            foreach (TrailRenderer trail in player.GetComponentsInChildren<TrailRenderer>()) trail.Clear();   // no streak from the portal down to the tower
            HUD.SetObjective(null, null, Color.white);
            StageStarted?.Invoke(Stage);
        }
    }
}
