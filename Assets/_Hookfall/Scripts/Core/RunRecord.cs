// Hookfall - the best run (stage, watch time, level, kills), saved in PlayerPrefs (GDD 3, 9.1). A run that reached a
// later stage beats a longer one on an earlier stage (stages since 2026-11-08).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Core
{
    public static class RunRecord
    {
        private const string TimeKey = "hf_best_time";
        private const string LevelKey = "hf_best_level";
        private const string KillsKey = "hf_best_kills";
        private const string StageKey = "hf_best_stage";

        public static bool HasRecord => PlayerPrefs.HasKey(TimeKey);
        public static float BestTime => PlayerPrefs.GetFloat(TimeKey, 0f);
        public static int BestLevel => PlayerPrefs.GetInt(LevelKey, 0);
        public static int BestKills => PlayerPrefs.GetInt(KillsKey, 0);
        public static int BestStage => PlayerPrefs.GetInt(StageKey, 1);

        /// <summary>Saves the run if it reached a later stage, or the same stage for longer. Returns true for a new record.</summary>
        public static bool Submit(float time, int level, int kills, int stage = 1)
        {
            if (HasRecord && (stage < BestStage || (stage == BestStage && time <= BestTime))) return false;
            PlayerPrefs.SetInt(StageKey, stage);
            PlayerPrefs.SetFloat(TimeKey, time);
            PlayerPrefs.SetInt(LevelKey, level);
            PlayerPrefs.SetInt(KillsKey, kills);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Forgets the best run (Hookfall > Clear Best Run in the editor).</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(TimeKey);
            PlayerPrefs.DeleteKey(LevelKey);
            PlayerPrefs.DeleteKey(KillsKey);
            PlayerPrefs.DeleteKey(StageKey);
            PlayerPrefs.Save();
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
