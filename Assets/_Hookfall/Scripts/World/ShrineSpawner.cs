// Hookfall - island shrines (added 2026-11-11): every stage a few shrines stand on islands, each sworn to one stat.
// Standing next to one for a few seconds - on the ground, so with the weapons off and the horde in Frenzy - gives
// that stat's upgrade twice, for free. A reason to land that is not a chest, and a risk to weigh.
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Hookfall.World
{
    public class ShrineSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject shrinePrefab;

        [SerializeField] private int perStage = 3;
        [Tooltip("Seconds into the stage before the shrines appear (the islands of a new stage are built first).")]
        [SerializeField] private float appearAfter = 4f;
        [Tooltip("Shrines stand on islands at least this wide.")]
        [SerializeField] private float minIslandWidth = 7f;
        [Tooltip("Seconds of standing next to a shrine for its blessing.")]
        [SerializeField] private float prayTime = 4f;
        [SerializeField] private float prayDistance = 3.5f;
        [Tooltip("Progress lost per second while the player is away, in shares of prayTime.")]
        [SerializeField] private float decayPerSecond = 0.5f;
        [Tooltip("Stat upgrades a blessing gives.")]
        [SerializeField] private int picks = 2;

        private readonly List<Transform> shrines = new List<Transform>();
        private readonly List<StatType> shrineStats = new List<StatType>();
        private PlayerStats stats;
        private float timer, progress;
        private int praying = -1;

        public static ShrineSpawner Instance { get; private set; }
        public IReadOnlyList<Transform> Shrines => shrines;

        /// <summary>A shrine gave its blessing (position; the rarity is for the listeners that colour by it).</summary>
        public static event Action<Vector3, Rarity> Blessed;

        private void Awake()
        {
            Instance = this;
            stats = player.GetComponent<PlayerStats>();
            timer = appearAfter;
            StageDirector.StageStarted += OnStage;
        }

        private void OnDestroy()
        {
            StageDirector.StageStarted -= OnStage;
            if (Instance == this) Instance = null;
        }

        // A new stage: the old shrines go with the old map.
        private void OnStage(int stage)
        {
            foreach (Transform shrine in shrines) if (shrine != null) Destroy(shrine.gameObject);
            shrines.Clear();
            shrineStats.Clear();
            praying = -1;
            progress = 0f;
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

            // The shrine the Warden stands at, if any.
            int at = -1;
            if (!player.IsAirborne)
                for (int i = 0; i < shrines.Count; i++)
                    if ((shrines[i].position - player.transform.position).sqrMagnitude <= prayDistance * prayDistance) { at = i; break; }
            if (at != praying && at >= 0) progress = 0f;
            if (at >= 0) praying = at;
            progress = at >= 0 ? progress + Time.deltaTime / prayTime : Mathf.Max(0f, progress - decayPerSecond * Time.deltaTime);
            if (progress <= 0f) return;
            HUD.ShrineProgress = progress;
            if (at < 0) return;
            HUD.InteractPrompt = Loc.F("Shrine of {0}: hold your ground", Loc.T(PlayerStats.NameOf(shrineStats[at])));
            HUD.InteractWithoutKey = true;
            if (progress < 1f) return;

            StatType stat = shrineStats[at];
            stats.Apply(stat, picks);
            HUD.Announce(Loc.F("Blessing: {0}", Loc.T(PlayerStats.NameOf(stat))), UITheme.Player);
            Blessed?.Invoke(shrines[at].position, Rarity.Epic);
            Destroy(shrines[at].gameObject);
            shrines.RemoveAt(at);
            shrineStats.RemoveAt(at);
            praying = -1;
            progress = 0f;
        }

        private void Place()
        {
            var candidates = new List<Island>();
            foreach (Island island in Island.All)
                if ((island.Kind == IslandKind.Inner || island.Kind == IslandKind.Outer) && island.Width >= minIslandWidth) candidates.Add(island);
            Array all = Enum.GetValues(typeof(StatType));
            for (int i = 0; i < perStage && candidates.Count > 0; i++)
            {
                int pick = Random.Range(0, candidates.Count);
                Island island = candidates[pick];
                candidates.RemoveAt(pick);
                Vector2 offset = Random.insideUnitCircle.normalized * island.Width * 0.22f;
                GameObject shrine = Instantiate(shrinePrefab, island.Top + new Vector3(offset.x, 0f, offset.y),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
                shrines.Add(shrine.transform);
                shrineStats.Add((StatType)all.GetValue(Random.Range(0, all.Length)));
            }
        }
    }
}
