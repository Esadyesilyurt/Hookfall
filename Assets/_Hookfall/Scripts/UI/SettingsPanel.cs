// Hookfall - settings panel used by the main menu and the pause menu: mouse sensitivity, music and effects volume,
// speed effects, full screen, resolution and language (GDD 9.1), each on a row with its icon.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; rebuilt on 2026-11-10 with rows, switches and the
// full-screen switch. Prompts and changes are recorded in Docs/PromptLog.md.
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
            Image panel = UIFactory.Panel(parent, "Settings", new Vector2(900f, 760f), UITheme.Panel);
            VerticalLayoutGroup column = UIFactory.Column(panel, 14f, 44, TextAnchor.UpperCenter);
            column.padding = new RectOffset(64, 64, 44, 40);
            UIPop.Add(panel, 0f, 0.85f);

            UIFactory.Heading(panel.transform, "SETTINGS", 44f, UITheme.Player);
            UIFactory.Slider(panel.transform, "Mouse sensitivity", 0.1f, 3f, GameSettings.MouseSensitivity, v => GameSettings.MouseSensitivity = v, UISkin.Icon("Sensitivity"));
            UIFactory.Slider(panel.transform, "Music volume", 0f, 1f, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v, UISkin.Icon("Music"));
            UIFactory.Slider(panel.transform, "Effects volume", 0f, 1f, GameSettings.EffectsVolume, v => GameSettings.EffectsVolume = v, UISkin.Icon("Sound"));
            UIFactory.Divider(panel.transform, UITheme.WithAlpha(UITheme.Player, 0.5f), 18f);
            UIFactory.Toggle(panel.transform, "Speed effects", GameSettings.SpeedEffects, v => GameSettings.SpeedEffects = v, UISkin.Icon("FlightSpeed"));
            UIFactory.Toggle(panel.transform, "Full screen", Screen.fullScreen, v => Screen.fullScreen = v, UISkin.Icon("Screen"));

            List<Resolution> resolutions = UniqueResolutions();
            int index = resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            TextMeshProUGUI resolution = null;
            resolution = UIFactory.Cycler(panel.transform, "Resolution", $"{Screen.width} × {Screen.height}", step =>
            {
                if (resolutions.Count == 0) return;
                index = ((index < 0 ? 0 : index) + step + resolutions.Count) % resolutions.Count;
                GameSettings.SetResolution(resolutions[index]);
                resolution.text = $"{resolutions[index].width} × {resolutions[index].height}";
            }, UISkin.Icon("Screen"));

            // Language: the menu rebuilds at once; in a run, screens built from now on use it.
            TextMeshProUGUI language = null;
            language = UIFactory.Cycler(panel.transform, "Language", LanguageName(), step =>
            {
                Loc.Turkish = !Loc.Turkish;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == GameManager.MenuScene)
                    UnityEngine.SceneManagement.SceneManager.LoadScene(GameManager.MenuScene);
                else language.text = LanguageName();
            }, UISkin.Icon("Language"));

            RectTransform gap = UIFactory.Item(panel.transform, "Gap", -1f, 8f);
            gap.GetComponent<LayoutElement>().flexibleHeight = 1f;
            UIFactory.Button(UIFactory.ButtonBar(panel.transform), "Back", onBack, 320f, 62f, null, UISkin.Icon("Back"));
            return panel.gameObject;
        }

        private static string LanguageName() => Loc.Turkish ? "Türkçe" : "English";

        private static List<Resolution> UniqueResolutions()
        {
            var list = new List<Resolution>();
            foreach (Resolution r in Screen.resolutions)
                if (!list.Exists(x => x.width == r.width && x.height == r.height)) list.Add(r);
            return list;
        }
    }
}
