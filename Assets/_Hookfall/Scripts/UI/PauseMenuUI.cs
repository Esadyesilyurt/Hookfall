// Hookfall - pause menu: Resume, Settings, Quit to menu; owned items with icons, levels and lore lines, every stat
// with its icon, and the bestiary (GDD 6, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System;
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

        private GameObject root, menu, settings, info;
        private RectTransform itemList, statGrid;
        private TextMeshProUGUI bestiary;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Pause Canvas", 30);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0.02f, 0.04f, 0.08f, 0.75f));

            Image menuPanel = UIFactory.Panel(canvas.transform, "Menu", new Vector2(440f, 470f), UITheme.Panel);
            ((RectTransform)menuPanel.transform).anchoredPosition = new Vector2(-600f, 0f);
            UIFactory.Column(menuPanel, 22f, 44, TextAnchor.UpperCenter);
            TextMeshProUGUI title = UIFactory.Sized(UIFactory.Text(menuPanel.transform, "Title", "PAUSED", 54f, UITheme.Player, TextAlignmentOptions.Center, true), -1f, 80f);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 10f;
            UIFactory.Button(menuPanel.transform, "Resume", () => GameManager.Instance?.SetPaused(false));
            UIFactory.Button(menuPanel.transform, "Settings", () => ShowSettings(true));
            UIFactory.Button(menuPanel.transform, "Quit to menu", () => GameManager.Instance?.QuitToMenu(), tint: new Color(0.45f, 0.25f, 0.28f));
            UIPop.Add(menuPanel, 0f, 0.85f, new Vector2(-60f, 0f));
            menu = menuPanel.gameObject;

            Image infoPanel = UIFactory.Panel(canvas.transform, "Info", new Vector2(1000f, 900f), UITheme.Panel);
            ((RectTransform)infoPanel.transform).anchoredPosition = new Vector2(200f, 0f);
            UIFactory.Column(infoPanel, 10f, 34, TextAnchor.UpperLeft);
            UIPop.Add(infoPanel, 0.05f, 0.9f, new Vector2(60f, 0f));
            Header(infoPanel.transform, "ITEMS");
            itemList = UIFactory.Rect(infoPanel.transform, "Items", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Column(itemList, 6f, 0, TextAnchor.UpperLeft);
            Header(infoPanel.transform, "STATS");
            statGrid = UIFactory.Rect(infoPanel.transform, "Stats", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var grid = statGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(450f, 40f);
            grid.spacing = new Vector2(16f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            UIFactory.Sized(statGrid, -1f, 7 * 44f);
            // The bestiary made way for the set bonuses on 2026-11-08: builds are what the player needs here.
            Header(infoPanel.transform, "SYNERGIES");
            bestiary = UIFactory.Sized(UIFactory.Text(infoPanel.transform, "Synergies", "", 15f, UITheme.TextMuted, TextAlignmentOptions.TopLeft), -1f, 470f);
            info = infoPanel.gameObject;

            settings = SettingsPanel.Build(canvas.transform, () => ShowSettings(false));
            settings.SetActive(false);
            root.SetActive(false);
        }

        private static void Header(Transform parent, string text)
        {
            TextMeshProUGUI t = UIFactory.Sized(UIFactory.Text(parent, text, text, 24f, UITheme.Player, TextAlignmentOptions.Left), -1f, 34f);
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 8f;
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
            Fill();
        }

        private void ShowSettings(bool show)
        {
            settings.SetActive(show);
            menu.SetActive(!show);
            info.SetActive(!show);
        }

        private void Fill()
        {
            foreach (Transform child in itemList) Destroy(child.gameObject);
            foreach (Transform child in statGrid) Destroy(child.gameObject);

            int rows = 0;
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                {
                    RectTransform row = UIFactory.Rect(itemList, item.DisplayName, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    UIFactory.Sized(row, -1f, 56f);
                    UIFactory.Row(row, 14f, 0, TextAnchor.MiddleLeft);
                    UIFactory.Icon(row, UISkin.Icon(item), 52f);
                    UIFactory.Sized(UIFactory.Text(row, "Name", $"<b>{item.DisplayName}</b>  <color=#4DF2FF>Lv {item.Level}</color>", 24f, UITheme.Text, TextAlignmentOptions.Left), 330f, 56f);
                    UIFactory.Sized(UIFactory.Text(row, "Lore", $"<i>{item.Lore}</i>", 17f, UITheme.TextMuted, TextAlignmentOptions.Left), 520f, 56f);
                    rows++;
                }
            UIFactory.Sized(itemList, -1f, Mathf.Max(1, rows) * 62f);
            // Rows report flexible height; without this the list would swallow the panel's spare space.
            itemList.GetComponent<LayoutElement>().flexibleHeight = 0f;

            if (stats != null)
                foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                {
                    RectTransform cell = UIFactory.Rect(statGrid, stat.ToString(), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    UIFactory.Row(cell, 10f, 0, TextAnchor.MiddleLeft);
                    UIFactory.Icon(cell, UISkin.Icon(stat), 34f);
                    UIFactory.Sized(UIFactory.Text(cell, "Name", PlayerStats.NameOf(stat), 21f, UITheme.TextMuted, TextAlignmentOptions.Left), 220f, 40f);
                    UIFactory.Sized(UIFactory.Text(cell, "Value", Value(stat), 22f, UITheme.Text, TextAlignmentOptions.Right), 150f, 40f).fontStyle = FontStyles.Bold;
                }

            bestiary.text = SynergyText();
        }

        /// <summary>Every school: owned count, and its two set bonuses (white once reached).</summary>
        private string SynergyText()
        {
            var synergy = combat != null ? combat.GetComponent<SynergySystem>() : null;
            var text = new System.Text.StringBuilder();
            foreach (School school in SchoolInfo.All)
            {
                int count = synergy != null ? synergy.Count(school) : 0;
                string first = Tier(count >= SynergySystem.FirstTier, "2: " + SchoolInfo.Bonus(school, 1));
                string second = Tier(count >= SynergySystem.SecondTier, "4: " + SchoolInfo.Bonus(school, 2));
                text.Append($"{SchoolInfo.Colored(school)} <b>{count}</b>   {first}   ·   {second}\n");
            }
            text.Append("\n");
            foreach (Duo duo in DuoInfo.All)
            {
                bool on = synergy != null && synergy.HasDuo(duo);
                text.Append(Tier(on, $"<b>{DuoInfo.Name(duo)}</b> ({DuoInfo.Pair(duo)}): {DuoInfo.Effect(duo)}") + "\n");
            }
            return text.ToString();
        }

        private static string Tier(bool reached, string text) => reached ? $"<color=#F2F5FA>{text}</color>" : text;

        private string Value(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHealth: return $"{stats.CurrentHealth:0} / {stats.MaxHealth:0}";
                case StatType.Armor: return $"{stats.Armor:0}";
                case StatType.Damage: return $"×{stats.DamageMultiplier:0.00}";
                case StatType.AttackSpeed: return $"×{stats.AttackSpeedMultiplier:0.00}";
                case StatType.WalkSpeed: return $"×{stats.WalkSpeedMultiplier:0.00}";
                case StatType.FlightSpeed: return $"×{stats.FlightSpeedMultiplier:0.00}";
                case StatType.HookRange: return $"{stats.HookRange:0} m";
                case StatType.PickupRadius: return $"{stats.PickupRadius:0.0} m";
                case StatType.Regen: return $"{stats.Regen:0.0}/s";
                case StatType.XPGain: return $"×{stats.XPMultiplier:0.00}";
                case StatType.Luck: return $"×{stats.LuckMultiplier:0.00}";
                case StatType.ZipCooldown: return $"{stats.ZipCooldown:0.0} s · {stats.ZipCharges}×";
                case StatType.Lifesteal: return $"{stats.Lifesteal * 100f:0}%";
                default: return $"{stats.ZipRange:0} m";
            }
        }
    }
}
