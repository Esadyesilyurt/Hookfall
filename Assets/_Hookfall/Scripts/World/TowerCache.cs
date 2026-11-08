// Hookfall - the tower cache: appears on top of the tower every 3 minutes with a beam of light; opens after 5 s of
// standing next to it; free; always Rare (60%) or Epic (40%) (GDD 7.2).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Combat;
using Hookfall.Player;
using Hookfall.UI;
using UnityEngine;

namespace Hookfall.World
{
    public class TowerCache : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private IslandGenerator islands;
        [SerializeField] private GameObject cacheVisual;
        [SerializeField] private GameObject beam;

        [SerializeField] private float interval = 180f;
        [SerializeField] private float openTime = 5f;
        [SerializeField] private float openDistance = 3f;
        [Tooltip("Progress lost per second while the player is away.")]
        [SerializeField] private float decayPerSecond = 0.5f;
        [Tooltip("Rarity weights (Common, Rare, Epic, Legendary); never Common.")]
        [SerializeField] private Vector4 rarityWeights = new Vector4(0f, 45f, 40f, 15f);   // unused since 2026-11-08: always Legendary
        [Tooltip("Enemies within this distance are destroyed when the cache opens.")]
        [SerializeField] private float clearRadius = 45f;
        [Tooltip("Legendary picks the cache gives, one after another.")]
        [SerializeField] private int picks = 3;

        private PlayerStats stats;
        private CombatSystem combat;
        private float timer;
        private float progress;

        public bool Ready { get; private set; }

        public event Action<Vector3, Rarity> Opened;
        public event Action Appeared;

        private void Awake()
        {
            stats = player.GetComponent<PlayerStats>();
            combat = player.GetComponent<CombatSystem>();
            timer = interval;
            SetReady(false);
        }

        private void Start()
        {
            if (islands != null) transform.position = islands.TowerTop;
        }

        private void Update()
        {
            if (!Ready)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f) SetReady(true);
                HUD.TowerCacheProgress = -1f;
                return;
            }

            HUD.TowerCacheMarker = transform.position;
            bool standing = !player.IsAirborne && Vector3.Distance(player.transform.position, transform.position) <= openDistance;
            progress = standing ? progress + Time.deltaTime / openTime : Mathf.Max(0f, progress - decayPerSecond * Time.deltaTime / openTime);
            HUD.TowerCacheProgress = progress > 0f ? progress : -1f;
            if (standing) HUD.InteractPrompt = Hookfall.Core.Loc.T("Opening the tower cache... hold your ground");
            if (progress < 1f) return;

            // The long hold pays: every enemy close by is destroyed, then three Legendary picks (2026-11-08).
            foreach (Hookfall.Enemies.EnemyBase enemy in Hookfall.Enemies.EnemyBase.Active.ToArray())
                if ((enemy.transform.position - transform.position).sqrMagnitude <= clearRadius * clearRadius) enemy.Die(true);
            Opened?.Invoke(transform.position, Rarity.Legendary);
            SetReady(false);
            HUD.Announce(Hookfall.Core.Loc.T("The Heartstone answers!"), new Color(1f, 0.62f, 0.15f));
            ChestResultUI.OfferSeries(combat, stats, Rarity.Legendary, picks, Hookfall.Core.Loc.T("TOWER CACHE"));
        }

        private void SetReady(bool ready)
        {
            Ready = ready;
            progress = 0f;
            timer = interval;
            if (cacheVisual != null) cacheVisual.SetActive(ready);
            if (beam != null) beam.SetActive(ready);
            if (!ready)
            {
                HUD.TowerCacheMarker = null;
                HUD.TowerCacheProgress = -1f;
            }
            else Appeared?.Invoke();
        }
    }
}
