// Hookfall - main menu (Play, Settings, Credits, Quit, best run) and the intro shown on the first Play of a session (GDD 6, 9.1).
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
        private GameObject main, settings, creditsPanel, intro;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Canvas canvas = UIFactory.Canvas(transform, "Menu Canvas", 0);
            Transform c = canvas.transform;

            TextMeshProUGUI logo = UIFactory.Text(UIFactory.Rect(c, "Logo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -80f), new Vector2(1000f, 160f), new Vector2(0.5f, 1f)),
                "HOOKFALL", 120f, UITheme.Player);
            logo.fontStyle = FontStyles.Bold;
            logo.characterSpacing = 18f;
            logo.textWrappingMode = TextWrappingModes.NoWrap;

            Image panel = UIFactory.Panel(c, "Main", new Vector2(460f, 520f), UITheme.Panel);
            ((RectTransform)panel.transform).anchoredPosition = new Vector2(-560f, -120f);
            UIFactory.Column(panel, 20f, 40, TextAnchor.UpperCenter);
            UIFactory.Button(panel.transform, "Play", OnPlay);
            UIFactory.Button(panel.transform, "Settings", () => Show(settings));
            UIFactory.Button(panel.transform, "Credits", () => Show(creditsPanel));
            UIFactory.Button(panel.transform, "Quit", Quit);
            string best = RunRecord.HasRecord
                ? $"Best watch {RunRecord.FormatTime(RunRecord.BestTime)}\nLevel {RunRecord.BestLevel} · {RunRecord.BestKills} kills"
                : "No watch held yet";
            UIFactory.Sized(UIFactory.Text(panel.transform, "Best", best, 24f, UITheme.Reward), -1f, 70f);
            main = panel.gameObject;

            settings = SettingsPanel.Build(c, () => Show(main));

            Image creditsImage = UIFactory.Panel(c, "Credits", new Vector2(900f, 560f), UITheme.Panel);
            UIFactory.Column(creditsImage, 20f, 44, TextAnchor.UpperCenter);
            UIFactory.Sized(UIFactory.Text(creditsImage.transform, "Text", credits, 26f, UITheme.Text), -1f, 380f);
            UIFactory.Button(creditsImage.transform, "Back", () => Show(main));
            creditsPanel = creditsImage.gameObject;

            Image introImage = UIFactory.Panel(c, "Intro", new Vector2(1100f, 600f), UITheme.Panel);
            UIFactory.Column(introImage, 26f, 54, TextAnchor.UpperCenter);
            foreach (string line in introLines)
                UIFactory.Sized(UIFactory.Text(introImage.transform, "Line", line, 30f, UITheme.Text), -1f, 110f);
            UIFactory.Button(introImage.transform, "Hold the watch", StartRun, 420f, 70f);
            UIFactory.Button(introImage.transform, "Skip", StartRun, 220f, 50f);
            intro = introImage.gameObject;

            Show(main);
        }

        private void Show(GameObject panel)
        {
            main.SetActive(panel == main);
            settings.SetActive(panel == settings);
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
