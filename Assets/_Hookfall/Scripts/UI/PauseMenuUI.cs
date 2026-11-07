// Hookfall - pause menu: Resume, Settings, Quit to menu; owned items, stats and lore lines (GDD 6, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Text;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private CombatSystem combat;
        [SerializeField] private PlayerStats stats;
        [Tooltip("One lore line per enemy, shown in the pause menu (GDD 6).")]
        [SerializeField, TextArea] private string[] enemyLore =
        {
            "<b>Bats</b> — small creatures twisted by the Heartstone's magic; they swarm anything near it.",
            "<b>Wyverns</b> — larger beasts drawn by the same power; they charge at intruders.",
            "<b>Golems</b> — stone sentries of the old fortress that now attack anyone, the Warden included."
        };

        private GameObject root, menu, settings;
        private TextMeshProUGUI info;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Pause Canvas", 30);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), UITheme.Dim);

            Image menuPanel = UIFactory.Panel(canvas.transform, "Menu", new Vector2(440f, 420f), UITheme.Panel);
            ((RectTransform)menuPanel.transform).anchoredPosition = new Vector2(-520f, 0f);
            UIFactory.Column(menuPanel, 20f, 40, TextAnchor.UpperCenter);
            UIFactory.Sized(UIFactory.Text(menuPanel.transform, "Title", "Paused", 50f, UITheme.Player), -1f, 70f);
            UIFactory.Button(menuPanel.transform, "Resume", () => GameManager.Instance?.SetPaused(false));
            UIFactory.Button(menuPanel.transform, "Settings", () => ShowSettings(true));
            UIFactory.Button(menuPanel.transform, "Quit to menu", () => GameManager.Instance?.QuitToMenu());
            menu = menuPanel.gameObject;

            Image infoPanel = UIFactory.Panel(canvas.transform, "Info", new Vector2(900f, 820f), UITheme.Panel);
            ((RectTransform)infoPanel.transform).anchoredPosition = new Vector2(240f, 0f);
            info = UIFactory.Text(infoPanel.transform, "Text", "", 22f, UITheme.Text, TextAlignmentOptions.TopLeft);
            RectTransform infoRect = (RectTransform)info.transform;
            UIFactory.Stretch(infoRect);
            infoRect.offsetMin = new Vector2(36f, 30f);
            infoRect.offsetMax = new Vector2(-36f, -30f);

            settings = SettingsPanel.Build(canvas.transform, () => ShowSettings(false));
            settings.SetActive(false);
            root.SetActive(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(RunState state)
        {
            bool paused = state == RunState.Paused;
            root.SetActive(paused);
            if (!paused) return;
            ShowSettings(false);
            info.text = BuildInfo();
        }

        private void ShowSettings(bool show)
        {
            settings.SetActive(show);
            menu.SetActive(!show);
            info.transform.parent.gameObject.SetActive(!show);
        }

        private string BuildInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<size=30><color=#4DF2FF>Items</color></size>");
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                    sb.AppendLine($"<b>{item.DisplayName}</b>  Lv {item.Level}  <color=#A6B3C2><i>{item.Lore}</i></color>");

            if (stats != null)
            {
                sb.AppendLine();
                sb.AppendLine("<size=30><color=#4DF2FF>Stats</color></size>");
                sb.AppendLine($"Health {stats.CurrentHealth:0}/{stats.MaxHealth:0}   Armor {stats.Armor:0}   Regen {stats.Regen:0.0}/s");
                sb.AppendLine($"Damage ×{stats.DamageMultiplier:0.00}   Attack speed ×{stats.AttackSpeedMultiplier:0.00}   XP ×{stats.XPMultiplier:0.00}");
                sb.AppendLine($"Walk ×{stats.WalkSpeedMultiplier:0.00}   Flight ×{stats.FlightSpeedMultiplier:0.00}   Hook {stats.HookRange:0} m");
                sb.AppendLine($"Pickup {stats.PickupRadius:0.0} m   Luck ×{stats.LuckMultiplier:0.00}");
            }

            sb.AppendLine();
            sb.AppendLine("<size=30><color=#4DF2FF>Bestiary</color></size>");
            foreach (string line in enemyLore) sb.AppendLine(line);
            return sb.ToString();
        }
    }
}
