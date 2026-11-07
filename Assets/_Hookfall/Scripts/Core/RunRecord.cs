// Hookfall - the best run (watch time, level, kills), saved in PlayerPrefs (GDD 3, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Core
{
    public static class RunRecord
    {
        private const string TimeKey = "hf_best_time";
        private const string LevelKey = "hf_best_level";
        private const string KillsKey = "hf_best_kills";

        public static bool HasRecord => PlayerPrefs.HasKey(TimeKey);
        public static float BestTime => PlayerPrefs.GetFloat(TimeKey, 0f);
        public static int BestLevel => PlayerPrefs.GetInt(LevelKey, 0);
        public static int BestKills => PlayerPrefs.GetInt(KillsKey, 0);

        /// <summary>Saves the run if it lasted longer than the record. Returns true for a new record.</summary>
        public static bool Submit(float time, int level, int kills)
        {
            if (HasRecord && time <= BestTime) return false;
            PlayerPrefs.SetFloat(TimeKey, time);
            PlayerPrefs.SetInt(LevelKey, level);
            PlayerPrefs.SetInt(KillsKey, kills);
            PlayerPrefs.Save();
            return true;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
