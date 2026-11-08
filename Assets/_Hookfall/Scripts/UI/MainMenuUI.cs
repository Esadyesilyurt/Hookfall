// Hookfall - main menu (Play, Leaderboard, Settings, Credits, Quit, best run) and the intro shown on the first Play of a session (GDD 6, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField, TextArea(3, 6)] private string[] introLines =
        {
            "Long ago, a kingdom raised a fortress around the Heartstone, a relic strong enough to bend gravity.",
            "When the fortress fell, the land broke apart, but the pieces never fell. The Heartstone holds them above an endless abyss.",
            "You are a Warden of the Order of the Tether. Guard the Heartstone for as long as you can."
        };
        [SerializeField, TextArea(5, 12)] private string credits =
            "Hookfall\nA solo project by Esad Yesilyurt\nGame Studio: AI in Game Development, Istanbul Kent University\n\n" +
            "Made with Unity 6 (URP, Shader Graph, Cinemachine) and Blender.\n" +
            "Code, models and writing assisted by Claude (Anthropic); see Docs/PromptLog.md.\n\n" +
            "Sound and music: licensed files listed in Docs/Credits.md.";

        private static bool introShown;
        private GameObject main, settings, creditsPanel, intro, leaderboard;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Canvas canvas = UIFactory.Canvas(transform, "Menu Canvas", 0);
            Transform c = canvas.transform;

            // Left side: logo with a tagline, the menu and the best run; the tower stands on the right.
            RectTransform logoRect = UIFactory.Rect(c, "Logo", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -90f), new Vector2(1000f, 150f), new Vector2(0f, 1f));
            TextMeshProUGUI logo = UIFactory.Text(logoRect, "HOOKFALL", 128f, UITheme.Player, TextAlignmentOptions.Left, true);
            logo.fontStyle = FontStyles.Bold;
            logo.characterSpacing = 16f;
            logo.textWrappingMode = TextWrappingModes.NoWrap;
            UIPop.Add(logo, 0f, 1.2f, new Vector2(-80f, 0f));
            TextMeshProUGUI tagline = UIFactory.Text(UIFactory.Rect(c, "Tagline", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(118f, -238f), new Vector2(1000f, 40f), new Vector2(0f, 1f)),
                "Hold the watch above the abyss", 30f, UITheme.Text, TextAlignmentOptions.Left, true);
            tagline.characterSpacing = 6f;
            UIPop.Add(tagline, 0.15f, 1f, new Vector2(-60f, 0f));

            Image panel = UIFactory.Panel(c, "Main", new Vector2(460f, 640f), UITheme.Panel);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(340f, -110f);
            UIFactory.Column(panel, 18f, 40, TextAnchor.UpperCenter);
            UIFactory.Button(panel.transform, "Play", OnPlay, 380f, 76f, new Color(0.2f, 0.55f, 0.62f));
            UIFactory.Button(panel.transform, "Leaderboard", () => Show(leaderboard));
            UIFactory.Button(panel.transform, "Settings", () => Show(settings));
            UIFactory.Button(panel.transform, "Credits", () => Show(creditsPanel));
            UIFactory.Button(panel.transform, "Quit", Quit, tint: new Color(0.45f, 0.25f, 0.28f));
            RectTransform bestRow = UIFactory.Rect(panel.transform, "Best", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(bestRow, -1f, 80f);
            UIFactory.Row(bestRow, 12f, 0);
            UIFactory.Icon(bestRow, UISkin.Icon("Timer"), 44f);
            string best = RunRecord.HasRecord
                ? $"<b>Best watch {RunRecord.FormatTime(RunRecord.BestTime)}</b>\nLevel {RunRecord.BestLevel} · {RunRecord.BestKills} kills"
                : "No watch held yet";
            UIFactory.Sized(UIFactory.Text(bestRow, "Text", best, 24f, UITheme.Gold, TextAlignmentOptions.Left), 280f, 80f);
            UIPop.Add(panel, 0.25f, 0.9f, new Vector2(-60f, 0f));
            main = panel.gameObject;

            settings = SettingsPanel.Build(c, () => Show(main));
            leaderboard = LeaderboardPanel.Build(c, () => Show(main));

            Image creditsImage = UIFactory.Panel(c, "Credits", new Vector2(940f, 600f), UITheme.Panel);
            UIFactory.Column(creditsImage, 20f, 44, TextAnchor.UpperCenter);
            TextMeshProUGUI creditsTitle = UIFactory.Sized(UIFactory.Text(creditsImage.transform, "Title", "CREDITS", 42f, UITheme.Player, TextAlignmentOptions.Center, true), -1f, 56f);
            creditsTitle.characterSpacing = 10f;
            UIFactory.Sized(UIFactory.Text(creditsImage.transform, "Text", credits, 25f, UITheme.Text), -1f, 360f);
            UIFactory.Button(creditsImage.transform, "Back", () => Show(main));
            UIPop.Add(creditsImage, 0f, 0.85f);
            creditsPanel = creditsImage.gameObject;

            Image introImage = UIFactory.Panel(c, "Intro", new Vector2(1160f, 660f), UITheme.Panel);
            UIFactory.Column(introImage, 22f, 56, TextAnchor.UpperCenter);
            UIFactory.Diamond(introImage.transform, UISkin.Icon("Zip"), "", 110f, UITheme.Player);
            for (int i = 0; i < introLines.Length; i++)
            {
                TextMeshProUGUI line = UIFactory.Sized(UIFactory.Text(introImage.transform, "Line", introLines[i], 30f, UITheme.Text), -1f, 96f);
                UIPop.Add(line, 0.2f + i * 0.5f, 1f);
            }
            RectTransform introButtons = UIFactory.Rect(introImage.transform, "Buttons", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(introButtons, -1f, 74f);
            UIFactory.Row(introButtons, 24f, 0);
            UIFactory.Button(introButtons, "Hold the watch", StartRun, 420f, 72f, new Color(0.2f, 0.55f, 0.62f));
            UIFactory.Button(introButtons, "Skip", StartRun, 200f, 72f);
            UIPop.Add(introImage, 0f, 0.9f);
            intro = introImage.gameObject;

            Show(main);
        }

        private void Show(GameObject panel)
        {
            main.SetActive(panel == main);
            settings.SetActive(panel == settings);
            leaderboard.SetActive(panel == leaderboard);
            creditsPanel.SetActive(panel == creditsPanel);
            intro.SetActive(panel == intro);
        }

        private void OnPlay()
        {
            if (introShown) StartRun();
            else Show(intro);
        }

        private static void StartRun()
        {
            introShown = true;
            SceneManager.LoadScene(GameManager.ArenaScene);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
