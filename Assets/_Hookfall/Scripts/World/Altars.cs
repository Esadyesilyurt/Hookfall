// Hookfall - the altars (2026-11-12). The stage boss no longer comes by itself when the gold bar is full: its altar
// stands on top of the tower, and the Warden calls the boss there (E) once the gold is earned. Out towards the edge
// of the map stand a few lesser altars, each calling a mini boss for a price in gold (since 2026-11-13; rising with
// every one called in the run): far easier than the stage boss, and worth more gold than it cost, a lot of XP, an
// Epic pick, full health and one more reroll and banish. The idea: hunt the mini bosses, grow, then call the boss.
// AI disclosure: written with Claude (Anthropic) on 2026-11-12. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace Hookfall.World
{
    public class Altars : MonoBehaviour
    {
        private static readonly Color MiniColor = new Color(1f, 0.55f, 0.2f);

        [SerializeField] private PlayerController player;
        [SerializeField] private IslandGenerator islands;
        [SerializeField] private EnemyDirector enemies;
        [SerializeField] private GameObject bossAltarPrefab;
        [SerializeField] private GameObject miniAltarPrefab;

        [Tooltip("Where the boss's altar stands, from the top of the tower.")]
        [SerializeField] private Vector3 towerOffset = new Vector3(1.9f, 0f, 1.9f);
        [Tooltip("The Warden uses an altar from within this distance.")]
        [SerializeField] private float useDistance = 5f;

        [Header("Mini bosses")]
        [SerializeField] private int miniPerStage = 2;
        [Tooltip("Mini-boss altars stand on islands at least this far from the tower, and this wide.")]
        [SerializeField] private float miniMinDistance = 170f;
        [SerializeField] private float miniMinIslandWidth = 5.5f;   // 8 until 2026-11-13: outer islands are 4-8 m wide, so no altar ever stood
        [Tooltip("Seconds into the stage before the altars appear (the islands of a new stage are built first).")]
        [SerializeField] private float appearAfter = 4f;
        [Tooltip("A mini boss is a wyvern with this many times its health, damage, size and reward.")]
        [SerializeField] private float miniHealth = 28f;
        [SerializeField] private float miniDamage = 1.5f;
        [SerializeField] private float miniScale = 2.4f;
        [SerializeField] private float miniReward = 90f;   // 180 gold and 270 XP before the stage's scaling (50 until 2026-11-13)
        [Tooltip("Gold to call a mini boss, and how much more each further one costs in the run.")]
        [SerializeField] private int miniCost = 120;
        [SerializeField] private int miniCostIncrease = 60;
        [Tooltip("Rerolls and banishes a mini boss's death gives.")]
        [SerializeField] private int miniTokens = 1;

        private readonly List<Transform> miniAltars = new List<Transform>();
        private PlayerStats stats;
        private CombatSystem combat;
        private Transform bossAltar;
        private EnemyBase miniBoss;
        private Hookfall.Progression.GoldWallet wallet;
        private Hookfall.Progression.ChoiceTokens tokens;
        private int miniCalled;
        private float timer;

        /// <summary>What the next mini boss costs.</summary>
        public int MiniCost => miniCost + miniCostIncrease * miniCalled;

        public static Altars Instance { get; private set; }
        /// <summary>The mini-boss altars not used yet on this stage.</summary>
        public IReadOnlyList<Transform> MiniAltars => miniAltars;
        /// <summary>Where the boss is called, or null before the altar stands.</summary>
        public static Vector3? BossAltar => Instance != null && Instance.bossAltar != null ? Instance.bossAltar.position + Vector3.up * 3f : (Vector3?)null;
        /// <summary>The mini boss that is alive, if one was called.</summary>
        public EnemyBase MiniBoss => miniBoss != null && !miniBoss.IsDead ? miniBoss : null;

        private void Awake()
        {
            Instance = this;
            stats = player.GetComponent<PlayerStats>();
            combat = player.GetComponent<CombatSystem>();
            wallet = player.GetComponent<Hookfall.Progression.GoldWallet>();
            tokens = player.GetComponent<Hookfall.Progression.ChoiceTokens>();
            timer = appearAfter;
            StageDirector.StageStarted += OnStage;
        }

        private void OnEnable() => EnemyDirector.Died += OnEnemyDied;
        private void OnDisable() => EnemyDirector.Died -= OnEnemyDied;

        private void OnDestroy()
        {
            StageDirector.StageStarted -= OnStage;
            if (Instance == this) Instance = null;
        }

        // A new stage: the old altars go with the old map.
        private void OnStage(int stage)
        {
            foreach (Transform altar in miniAltars) if (altar != null) Destroy(altar.gameObject);
            miniAltars.Clear();
            if (bossAltar != null) Destroy(bossAltar.gameObject);
            miniBoss = null;
            timer = appearAfter;
        }

        private void Update()
        {
            if (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f) Place();
                return;
            }
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.State != RunState.Playing) return;
            bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            Vector3 p = player.transform.position;
            StageDirector stage = StageDirector.Instance;

            // The boss's altar: only once the stage's gold is earned.
            if (bossAltar != null && stage != null && stage.Current == StageDirector.Phase.Gathering && Near(bossAltar, p))
            {
                if (stage.BossReady)
                {
                    HUD.InteractPrompt = Loc.F("Call {0}", stage.BossName);
                    if (pressed) stage.SummonBoss();
                }
                else
                {
                    HUD.InteractPrompt = "<color=#F2403A>" + Loc.F("The altar asks for {0} more gold", stage.GoldTarget - stage.GoldProgress) + "</color>";
                    HUD.InteractWithoutKey = true;
                }
                return;
            }

            // A lesser altar: paid in gold, one mini boss at a time, and not while the stage boss fights.
            if (MiniBoss != null || stage == null || stage.Current != StageDirector.Phase.Gathering) return;
            for (int i = 0; i < miniAltars.Count; i++)
            {
                if (!Near(miniAltars[i], p)) continue;
                bool canPay = wallet != null && wallet.Gold >= MiniCost;
                HUD.InteractPrompt = canPay ? Loc.F("Call a mini boss  ·  {0} gold", MiniCost)
                    : "<color=#F2403A>" + Loc.F("Mini boss  ·  {0} gold needed", MiniCost) + "</color>";
                if (!canPay) HUD.InteractWithoutKey = true;
                if (!pressed || !canPay || !wallet.TrySpend(MiniCost)) return;
                miniCalled++;
                miniBoss = enemies.SpawnMiniBoss(miniAltars[i].position + Vector3.up * 14f, miniHealth, miniDamage, miniScale, miniReward);
                HUD.Announce(Loc.T("A Wyvern Matriarch answers the altar!"), MiniColor);
                Destroy(miniAltars[i].gameObject);
                miniAltars.RemoveAt(i);
                return;
            }
        }

        private bool Near(Transform altar, Vector3 p) => (altar.position - p).sqrMagnitude <= useDistance * useDistance;

        private void OnEnemyDied(EnemyBase enemy, bool killed)
        {
            if (miniBoss == null || enemy != miniBoss) return;
            miniBoss = null;
            if (!killed) return;
            // Its gold and XP drop like any enemy's (miniReward); on top: full health, a reroll and a banish, an Epic pick.
            HUD.Announce(Loc.T("The Matriarch falls!"), MiniColor);
            stats.Heal(stats.MaxHealth);
            if (tokens != null) tokens.Add(miniTokens, miniTokens);
            ChestResultUI.OfferSeries(combat, stats, Rarity.Epic, 1, Loc.T("MATRIARCH HOARD"));
        }

        private void Place()
        {
            Vector3 top = islands != null ? islands.TowerTop : Vector3.up * 20f;
            bossAltar = Instantiate(bossAltarPrefab, top + towerOffset, Quaternion.identity, transform).transform;

            // Inner and outer islands wide enough to stand on, the farthest first; the far ones are picked at random, and
            // if too few are far enough the farthest of the rest are taken, so the altars always stand.
            float Far(Island island) => new Vector2(island.Top.x - top.x, island.Top.z - top.z).sqrMagnitude;
            var candidates = new List<Island>();
            foreach (Island island in Island.All)
                if ((island.Kind == IslandKind.Outer || island.Kind == IslandKind.Inner) && island.Width >= miniMinIslandWidth) candidates.Add(island);
            candidates.Sort((a, b) => Far(b).CompareTo(Far(a)));
            int far = candidates.FindAll(island => Far(island) >= miniMinDistance * miniMinDistance).Count;
            for (int i = 0; i < miniPerStage && candidates.Count > 0; i++)
            {
                int pick = far > 0 ? Random.Range(0, far--) : 0;
                Island island = candidates[pick];
                candidates.RemoveAt(pick);
                miniAltars.Add(Instantiate(miniAltarPrefab, island.Top, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform).transform);
            }
        }
    }
}
