// Hookfall - player settings saved in PlayerPrefs: mouse sensitivity, volumes, speed effects, resolution (GDD 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using UnityEngine;

namespace Hookfall.Core
{
    public static class GameSettings
    {
        private const string SensitivityKey = "hf_sensitivity";
        private const string MusicKey = "hf_music";
        private const string EffectsKey = "hf_effects";
        private const string SpeedFxKey = "hf_speedfx";

        public static event Action Changed;

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
            set { PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.1f, 3f)); Changed?.Invoke(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.45f);
            set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static float EffectsVolume
        {
            get => PlayerPrefs.GetFloat(EffectsKey, 0.6f);
            set { PlayerPrefs.SetFloat(EffectsKey, Mathf.Clamp01(value)); Changed?.Invoke(); }
        }

        public static bool SpeedEffects
        {
            get => PlayerPrefs.GetInt(SpeedFxKey, 1) == 1;
            set { PlayerPrefs.SetInt(SpeedFxKey, value ? 1 : 0); Changed?.Invoke(); }
        }

        /// <summary>Unity remembers the chosen resolution itself between launches.</summary>
        public static void SetResolution(Resolution resolution)
        {
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRateRatio);
            Changed?.Invoke();
        }
    }
}
