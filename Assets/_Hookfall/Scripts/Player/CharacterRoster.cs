// Hookfall - the selected character and the progress that unlocks characters, saved in PlayerPrefs: total kills over
// all runs and the highest level reached; the longest watch comes from RunRecord.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using UnityEngine;

namespace Hookfall.Player
{
    public static class CharacterRoster
    {
        public const string SelectedKey = "hf_character";
        public const string TotalKillsKey = "hf_total_kills";
        public const string MaxLevelKey = "hf_max_level";

        public static string SelectedId
        {
            get => PlayerPrefs.GetString(SelectedKey, "Warden");
            set { PlayerPrefs.SetString(SelectedKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>The character playing the current run (set by CharacterLoadout), for the leaderboard.</summary>
        public static string ActiveId { get; set; } = "Warden";

        public static int TotalKills => PlayerPrefs.GetInt(TotalKillsKey, 0);
        public static int MaxLevel => PlayerPrefs.GetInt(MaxLevelKey, 0);

        /// <summary>Called when a run ends: adds its kills and keeps the highest level.</summary>
        public static void RecordRun(int level, int kills)
        {
            PlayerPrefs.SetInt(TotalKillsKey, TotalKills + kills);
            PlayerPrefs.SetInt(MaxLevelKey, Mathf.Max(MaxLevel, level));
            PlayerPrefs.Save();
        }

        /// <summary>How far the unlock quest is, 0..1 (1 = unlocked).</summary>
        public static float Progress(CharacterDefinition character)
        {
            if (character == null || character.unlock == UnlockKind.None || character.unlockTarget <= 0f) return 1f;
            float value = character.unlock == UnlockKind.SurviveSeconds ? (RunRecord.HasRecord ? RunRecord.BestTime : 0f)
                : character.unlock == UnlockKind.TotalKills ? TotalKills
                : MaxLevel;
            return Mathf.Clamp01(value / character.unlockTarget);
        }

        public static bool IsUnlocked(CharacterDefinition character) => Progress(character) >= 1f;

        /// <summary>Quest progress as text, e.g. "812 / 1500".</summary>
        public static string ProgressText(CharacterDefinition character)
        {
            if (IsUnlocked(character)) return "Unlocked";
            float now = Progress(character) * character.unlockTarget;
            return character.unlock == UnlockKind.SurviveSeconds
                ? $"{RunRecord.FormatTime(now)} / {RunRecord.FormatTime(character.unlockTarget)}"
                : $"{Mathf.FloorToInt(now)} / {Mathf.FloorToInt(character.unlockTarget)}";
        }
    }
}
