// Hookfall - settings panel used by the main menu and the pause menu: mouse sensitivity, music and effects volume,
// speed effects on/off, resolution (GDD 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public static class SettingsPanel
    {
        public static GameObject Build(Transform parent, Action onBack)
        {
            Image panel = UIFactory.Panel(parent, "Settings", new Vector2(800f, 660f), UITheme.Panel);
            UIFactory.Column(panel, 18f, 44, TextAnchor.UpperCenter);
            UIPop.Add(panel, 0f, 0.85f);

            TMPro.TextMeshProUGUI title = UIFactory.Sized(UIFactory.Text(panel.transform, "Title", "SETTINGS", 46f, UITheme.Player, TMPro.TextAlignmentOptions.Center, true), -1f, 64f);
            title.fontStyle = TMPro.FontStyles.Bold;
            title.characterSpacing = 10f;
            UIFactory.Slider(panel.transform, "Mouse sensitivity", 0.1f, 3f, GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v);
            UIFactory.Slider(panel.transform, "Music volume", 0f, 1f, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            UIFactory.Slider(panel.transform, "Effects volume", 0f, 1f, GameSettings.EffectsVolume, v => GameSettings.EffectsVolume = v);

            Button speedFx = null;
            speedFx = UIFactory.Button(panel.transform, SpeedFxLabel(), () =>
            {
                GameSettings.SpeedEffects = !GameSettings.SpeedEffects;
                speedFx.GetComponentInChildren<TextMeshProUGUI>().text = SpeedFxLabel();
            }, -1f, 56f);

            List<Resolution> resolutions = UniqueResolutions();
            int index = resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            Button resolution = null;
            resolution = UIFactory.Button(panel.transform, ResolutionLabel(), () =>
            {
                if (resolutions.Count == 0) return;
                index = (index + 1) % resolutions.Count;
                GameSettings.SetResolution(resolutions[index]);
                resolution.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"Resolution: {resolutions[index].width} × {resolutions[index].height}";
            }, -1f, 56f);

            UIFactory.Button(panel.transform, "Back", onBack, -1f, 60f);
            return panel.gameObject;
        }

        private static string SpeedFxLabel() => $"Speed effects: {(GameSettings.SpeedEffects ? "On" : "Off")}";
        private static string ResolutionLabel() => $"Resolution: {Screen.width} × {Screen.height}";

        private static List<Resolution> UniqueResolutions()
        {
            var list = new List<Resolution>();
            foreach (Resolution r in Screen.resolutions)
                if (!list.Exists(x => x.width == r.width && x.height == r.height)) list.Add(r);
            return list;
        }
    }
}
