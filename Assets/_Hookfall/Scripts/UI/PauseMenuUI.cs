// Hookfall - pause menu (GDD 9.1): Resume, Settings, Quit to menu and the run so far on the left; on the right four
// tabs: the build (every owned item with its level, schools, numbers and lore), the stats, the synergies (set bonuses
// and duos, lit once reached) and the controls.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin; rebuilt with
// tabs on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private CombatSystem combat;
        [SerializeField] private PlayerStats stats;

        private static readonly string[] TabNames = { "BUILD", "STATS", "SYNERGIES", "CONTROLS" };

        // Key, what it does (English source text; translated where it is drawn).
        private static readonly string[,] Controls =
        {
            { "LMB", "Throw the hook; hold to swing, let go to fly" },
            { "RMB", "Zip: a power swing; keep holding to reel in" },
            { "A  D", "Steer in the air" },
            { "W  S", "Climb up or down the rope; walk on islands" },
            { "Space", "Jump; in the air a double jump; off a wall a wall jump" },
            { "Shift", "Dash in the air" },
            { "E", "Open a chest" },
            { "1  2  3", "Pick a card" },
            { "Esc", "Pause" },
        };

        private GameObject root, menu, settings, info;
        private readonly GameObject[] pages = new GameObject[TabNames.Length];
        private RectTransform buildList, statGrid, synergyList;
        private TextMeshProUGUI summary;
        private Action<int> selectTab;
        private int tab;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Pause Canvas", 30);
            root = canvas.gameObject;
            UIFactory.Dim(canvas.transform, 0.76f);

            Image menuPanel = UIFactory.Panel(canvas.transform, "Menu", new Vector2(420f, 560f), UITheme.Panel);
            ((RectTransform)menuPanel.transform).anchoredPosition = new Vector2(-710f, 0f);
            UIFactory.Column(menuPanel, 16f, 44, TextAnchor.UpperCenter);
            UIFactory.Heading(menuPanel.transform, "PAUSED", 44f, UITheme.Player);
            UIFactory.Button(menuPanel.transform, "Resume", () => GameManager.Instance?.SetPaused(false), -1f, 70f, UITheme.ButtonPrimary, UISkin.Icon("Play"));
            UIFactory.Button(menuPanel.transform, "Settings", () => ShowSettings(true), -1f, 62f, null, UISkin.Icon("Settings"));
            UIFactory.Button(menuPanel.transform, "Quit to menu", () => GameManager.Instance?.QuitToMenu(), -1f, 62f, UITheme.ButtonDanger, UISkin.Icon("Quit"));
            UIFactory.Divider(menuPanel.transform, UITheme.WithAlpha(UITheme.Player, 0.6f), 22f);
            summary = UIFactory.Sized(UIFactory.Text(menuPanel.transform, "Summary", "", 24f, UITheme.Text), -1f, 110f);
            summary.lineSpacing = 6f;
            UIPop.Add(menuPanel, 0f, 0.85f, new Vector2(-60f, 0f));
            menu = menuPanel.gameObject;

            Image infoPanel = UIFactory.Panel(canvas.transform, "Info", new Vector2(1260f, 940f), UITheme.Panel);
            ((RectTransform)infoPanel.transform).anchoredPosition = new Vector2(200f, 0f);
            VerticalLayoutGroup column = UIFactory.Column(infoPanel, 0f, 40, TextAnchor.UpperLeft);
            column.padding = new RectOffset(44, 44, 40, 40);
            UIPop.Add(infoPanel, 0.05f, 0.9f, new Vector2(60f, 0f));
            selectTab = UIFactory.Tabs(infoPanel.transform, TabNames, ShowPage, 230f);
            RectTransform rule = UIFactory.Item(infoPanel.transform, "Rule", -1f, 3f);
            UIFactory.Image(rule, UITheme.ButtonPrimary);
            RectTransform holder = UIFactory.Item(infoPanel.transform, "Pages", -1f, 805f);
            for (int i = 0; i < pages.Length; i++)
            {
                RectTransform page = UIFactory.Fill(holder, TabNames[i]);
                page.offsetMax = new Vector2(0f, -22f);
                pages[i] = page.gameObject;
            }

            buildList = UIFactory.Scroll(pages[0].transform, "Items", -1f, -1f, 10f);
            UIFactory.Stretch(UIFactory.ScrollBox(buildList));

            statGrid = UIFactory.Fill(pages[1].transform, "Stats");
            var grid = statGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(570f, 58f);
            grid.spacing = new Vector2(28f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            synergyList = UIFactory.Scroll(pages[2].transform, "Synergies", -1f, -1f, 8f);
            UIFactory.Stretch(UIFactory.ScrollBox(synergyList));

            BuildControls(pages[3].transform);
            info = infoPanel.gameObject;

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
            Fill();
            selectTab(tab);
        }

        private void ShowSettings(bool show)
        {
            settings.SetActive(show);
            menu.SetActive(!show);
            info.SetActive(!show);
        }

        private void ShowPage(int index)
        {
            tab = index;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
        }

        private void Fill()
        {
            GameManager gm = GameManager.Instance;
            int stage = StageDirector.Instance != null ? StageDirector.Instance.Stage : 1;
            var xp = stats != null ? stats.GetComponent<XPSystem>() : null;
            summary.text = Loc.F("STAGE {0}", stage) + "  ·  " + (gm != null ? RunRecord.FormatTime(gm.RunTime) : "00:00") + "\n" +
                           $"<color=#{UITheme.Hex(UITheme.TextMuted)}>" + Loc.F("Level {0} · {1} kills", xp != null ? xp.Level : 1, gm != null ? gm.Kills : 0) + "</color>";

            foreach (Transform child in buildList) Destroy(child.gameObject);
            foreach (Transform child in statGrid) Destroy(child.gameObject);
            foreach (Transform child in synergyList) Destroy(child.gameObject);

            if (combat != null)
            {
                foreach (ItemBase item in combat.Items) if (!item.IsPassive) ItemRow(item);
                foreach (ItemBase item in combat.Items) if (item.IsPassive) ItemRow(item);
            }
            if (stats != null)
                foreach (StatType stat in Enum.GetValues(typeof(StatType))) StatCell(stat);
            FillSynergies();
        }

        /// <summary>An owned item: icon in its rarity frame with level pips, name, schools, its numbers and its lore.</summary>
        private void ItemRow(ItemBase item)
        {
            Color rarity = UITheme.ForRarity(item.Rarity);
            RectTransform row = UIFactory.Item(buildList, item.name, -1f, 104f);
            UIFactory.Image(row, new Color(1f, 1f, 1f, 0.05f), UISkin.Chip);
            HorizontalLayoutGroup line = UIFactory.Row(row, 18f, 0, TextAnchor.MiddleLeft);
            line.padding = new RectOffset(14, 14, 8, 8);
            line.childForceExpandHeight = false;

            RectTransform slot = UIFactory.Item(row, "Slot", 84f, 84f);
            UIFactory.Image(slot, Color.white, UISkin.Slot);
            UIFactory.Image(UIFactory.Fill(slot, "Frame"), rarity, UISkin.SlotFrame);
            Image icon = UIFactory.Image(UIFactory.Centre(slot, "Icon", new Vector2(0f, 5f), new Vector2(58f, 58f)), Color.white, UISkin.Icon(item));
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            UIFactory.Pips(slot, item.Level, ItemBase.MaxLevel, 10f, UITheme.Player, new Color(1f, 1f, 1f, 0.22f));

            RectTransform text = UIFactory.Item(row, "Text", 520f, 88f);
            VerticalLayoutGroup lines = UIFactory.Column(text, 2f, 0, TextAnchor.MiddleLeft);
            lines.childForceExpandWidth = true;
            RectTransform top = UIFactory.Item(text, "Top", -1f, 32f);
            HorizontalLayoutGroup topRow = UIFactory.Row(top, 10f, 0, TextAnchor.MiddleLeft);
            topRow.childForceExpandHeight = false;
            TextMeshProUGUI name = UIFactory.Text(top, "Name", item.DisplayName, 25f, rarity, TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            foreach (School school in item.Schools)
                UIFactory.Chip(top, UISkin.Icon(school), "", SchoolInfo.Color(school), 26f);
            TextMeshProUGUI numbers = UIFactory.Sized(UIFactory.Text(text, "Numbers", string.Join("   ·   ", item.StatLines()), 20f, UITheme.Text, TextAlignmentOptions.Left), -1f, 26f);
            numbers.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(numbers, 13f);
            TextMeshProUGUI what = UIFactory.Sized(UIFactory.Text(text, "Summary", item.Summary, 17f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 24f);
            what.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(what, 12f);

            TextMeshProUGUI lore = UIFactory.Text(row, "Lore", item.Lore, 16f, UITheme.TextMuted, TextAlignmentOptions.Left);
            lore.fontStyle = FontStyles.Italic;
            UIFactory.Sized(lore, 440f, 84f);
            UIFactory.FitText(lore, 12f);
        }

        private void StatCell(StatType stat)
        {
            RectTransform cell = UIFactory.Item(statGrid, stat.ToString());
            UIFactory.Image(cell, new Color(1f, 1f, 1f, 0.05f), UISkin.Chip);
            HorizontalLayoutGroup line = UIFactory.Row(cell, 14f, 0, TextAnchor.MiddleLeft);
            line.padding = new RectOffset(14, 18, 4, 4);
            line.childForceExpandHeight = false;
            UIFactory.Icon(cell, UISkin.Icon(stat), 42f);
            UIFactory.Sized(UIFactory.Text(cell, "Name", PlayerStats.NameOf(stat), 24f, UITheme.TextMuted, TextAlignmentOptions.Left), 280f, 40f);
            TextMeshProUGUI value = UIFactory.Sized(UIFactory.Text(cell, "Value", Value(stat), 26f, UITheme.Text, TextAlignmentOptions.Right), 190f, 40f);
            value.fontStyle = FontStyles.Bold;
        }

        /// <summary>Every school: owned count and its two set bonuses (lit once reached); then the duos.</summary>
        private void FillSynergies()
        {
            var synergy = combat != null ? combat.GetComponent<SynergySystem>() : null;
            foreach (School school in SchoolInfo.All)
            {
                int count = synergy != null ? synergy.Count(school) : 0;
                Color color = SchoolInfo.Color(school);
                RectTransform row = UIFactory.Item(synergyList, school.ToString(), -1f, 70f);
                UIFactory.Image(row, new Color(color.r, color.g, color.b, count > 0 ? 0.1f : 0.04f), UISkin.Chip);
                HorizontalLayoutGroup line = UIFactory.Row(row, 14f, 0, TextAnchor.MiddleLeft);
                line.padding = new RectOffset(14, 14, 4, 4);
                line.childForceExpandHeight = false;
                UIFactory.Icon(row, UISkin.Icon(school), 48f, count > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f));
                TextMeshProUGUI name = UIFactory.Sized(UIFactory.Text(row, "Name", $"{SchoolInfo.Name(school)}  <b>{count}</b>", 24f, color, TextAlignmentOptions.Left), 170f, 40f);
                name.textWrappingMode = TextWrappingModes.NoWrap;
                string tiers = Tier(count >= SynergySystem.FirstTier, "2  " + SchoolInfo.Bonus(school, 1)) + "\n" +
                               Tier(count >= SynergySystem.SecondTier, "4  " + SchoolInfo.Bonus(school, 2));
                TextMeshProUGUI bonus = UIFactory.Sized(UIFactory.Text(row, "Bonuses", tiers, 19f, UITheme.TextMuted, TextAlignmentOptions.Left), 860f, 60f);
                UIFactory.FitText(bonus, 13f);
            }

            UIFactory.Heading(synergyList, "DUOS", 24f, UITheme.Gold);
            foreach (Duo duo in DuoInfo.All)
            {
                bool on = synergy != null && synergy.HasDuo(duo);
                (School a, School b) = DuoInfo.Schools(duo);
                RectTransform row = UIFactory.Item(synergyList, duo.ToString(), -1f, 44f);
                UIFactory.Image(row, new Color(1f, 0.8f, 0.3f, on ? 0.14f : 0.03f), UISkin.Chip);
                HorizontalLayoutGroup line = UIFactory.Row(row, 10f, 0, TextAnchor.MiddleLeft);
                line.padding = new RectOffset(14, 14, 2, 2);
                line.childForceExpandHeight = false;
                UIFactory.Icon(row, UISkin.Icon(a), 30f);
                UIFactory.Icon(row, UISkin.Icon(b), 30f);
                TextMeshProUGUI name = UIFactory.Sized(UIFactory.Text(row, "Name", DuoInfo.Name(duo), 21f, on ? UITheme.Gold : UITheme.TextMuted, TextAlignmentOptions.Left), 230f, 36f);
                name.fontStyle = FontStyles.Bold;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                TextMeshProUGUI effect = UIFactory.Sized(UIFactory.Text(row, "Effect", DuoInfo.Effect(duo), 18f, on ? UITheme.Text : UITheme.TextMuted, TextAlignmentOptions.Left), 760f, 36f);
                effect.textWrappingMode = TextWrappingModes.NoWrap;
                UIFactory.FitText(effect, 12f);
            }
        }

        private static void BuildControls(Transform page)
        {
            RectTransform column = UIFactory.Fill(page, "Controls");
            UIFactory.Column(column, 10f, 0, TextAnchor.UpperLeft);
            TextMeshProUGUI rule = UIFactory.Sized(UIFactory.Text(column, "Rule", "Your weapons fire only while you are in the air. Islands are safe from the fall, but there you are defenceless.",
                24f, UITheme.Player, TextAlignmentOptions.Left), -1f, 70f);
            rule.fontStyle = FontStyles.Bold;
            for (int i = 0; i < Controls.GetLength(0); i++)
            {
                RectTransform row = UIFactory.Item(column, Controls[i, 0], -1f, 58f);
                UIFactory.Image(row, new Color(1f, 1f, 1f, 0.05f), UISkin.Chip);
                HorizontalLayoutGroup line = UIFactory.Row(row, 18f, 0, TextAnchor.MiddleLeft);
                line.padding = new RectOffset(16, 16, 4, 4);
                line.childForceExpandHeight = false;
                RectTransform keys = UIFactory.Item(row, "Keys", 220f, 46f);
                HorizontalLayoutGroup keyRow = UIFactory.Row(keys, 8f, 0, TextAnchor.MiddleLeft);
                keyRow.childForceExpandHeight = false;
                foreach (string key in Controls[i, 0].Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries)) UIFactory.KeyCap(keys, key, 42f);
                UIFactory.Sized(UIFactory.Text(row, "Action", Controls[i, 1], 24f, UITheme.Text, TextAlignmentOptions.Left), 880f, 46f);
            }
        }

        private static string Tier(bool reached, string text) => reached ? $"<color=#{UITheme.Hex(UITheme.Text)}>{text}</color>" : text;

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
