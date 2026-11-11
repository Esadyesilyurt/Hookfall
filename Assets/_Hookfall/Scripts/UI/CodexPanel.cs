// Hookfall - the main menu's codex: three tabs. ENEMIES: every kind of enemy with what it does and how many the
// player has killed, hidden until the first kill. ITEMS: every item with its schools and what it does, hidden until
// it has been owned once. SCHOOLS: the set bonuses and the duos, always open.
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Combat;
using Hookfall.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class CodexPanel : MonoBehaviour
    {
        private static readonly string[] TabNames = { "ENEMIES", "ITEMS", "SCHOOLS" };

        // The class of the enemy (what Codex counts kills by), its name, and what to know about it.
        private static readonly (string type, string name, string lore)[] Enemies =
        {
            ("Bat", "Abyss Bat", "The horde itself. Weak alone, never alone. Bites when it reaches you."),
            ("Wyvern", "Wyvern", "Rears back for a breath, then charges in a straight line. Step aside and it flies past."),
            ("Golem", "Island Golem", "Stands on an island and hurls boulders. Slams the ground when you land next to it."),
            ("Razorwing", "Razorwing", "Hunts in a flock that holds formation, makes a strafing run through you and wheels round."),
            ("Blightpuff", "Blightpuff", "Drifts up to you, swells and bursts. The blast hurts the horde as well: use it."),
            ("Venomwing", "Venomwing", "Keeps to your flank and spits venom from range. It never comes close on its own."),
            ("GloomJelly", "Gloom Jelly", "Slow and tough. Its pulse shocks everything near it, and it leaves three young behind."),
            ("Boss", "Colossus", "The lord of a stage. Gather enough gold and it comes for the Heartstone."),
            ("TempestWyrm", "Tempest Wyrm", "The lord of every second stage. It never holds still: strike the head, and move when it glows red."),
        };

        private readonly GameObject[] pages = new GameObject[TabNames.Length];
        private RectTransform enemyRows, itemRows, schoolRows;
        private TextMeshProUGUI found;
        private ItemBase[] items;
        private int tab;

        public static GameObject Build(Transform canvas, Action onBack, ItemBase[] items)
        {
            Image image = UIFactory.Panel(canvas, "Codex", new Vector2(1260f, 1000f), UITheme.Panel);
            VerticalLayoutGroup column = UIFactory.Column(image, 8f, 40, TextAnchor.UpperCenter);
            column.padding = new RectOffset(44, 44, 36, 32);
            var panel = image.gameObject.AddComponent<CodexPanel>();
            panel.items = items ?? new ItemBase[0];

            UIFactory.Heading(image.transform, "CODEX", 40f, UITheme.Player);
            Action<int> select = UIFactory.Tabs(image.transform, TabNames, panel.ShowPage, 230f);
            UIFactory.Image(UIFactory.Item(image.transform, "Rule", -1f, 3f), UITheme.ButtonPrimary);
            RectTransform holder = UIFactory.Item(image.transform, "Pages", -1f, 690f);
            RectTransform[] lists = new RectTransform[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                RectTransform page = UIFactory.Fill(holder, TabNames[i]);
                UIFactory.Column(page, 0f, 0, TextAnchor.UpperCenter).padding = new RectOffset(0, 0, 12, 0);
                lists[i] = UIFactory.Scroll(page, "Rows", -1f, 676f, 6f);
                panel.pages[i] = page.gameObject;
            }
            panel.enemyRows = lists[0];
            panel.itemRows = lists[1];
            panel.schoolRows = lists[2];
            panel.found = UIFactory.Sized(UIFactory.Text(image.transform, "Found", "", 21f, UITheme.TextMuted), -1f, 28f);
            UIFactory.Button(UIFactory.ButtonBar(image.transform), "Back", () => onBack(), 320f, 62f, null, UISkin.Icon("Back"));
            UIPop.Add(image, 0f, 0.85f);
            select(0);
            return image.gameObject;
        }

        private void OnEnable()
        {
            if (enemyRows == null) return;
            Codex.Reload();
            Clear(enemyRows);
            Clear(itemRows);
            Clear(schoolRows);
            FillSchools(schoolRows);
            foreach ((string type, string name, string lore) in Enemies)
            {
                int kills = Codex.Kills(type);
                Row(enemyRows, UISkin.Icon(type == "Boss" || type == "TempestWyrm" ? "Boss" : "Kills"), kills > 0 ? UITheme.Enemy : UITheme.TextMuted,
                    kills > 0 ? Loc.T(name) : "???", kills > 0 ? Loc.F("{0} killed", kills) : "",
                    kills > 0 ? Loc.T(lore) : Loc.T("Not met yet"), kills > 0);
            }
            foreach (ItemBase item in items)
            {
                bool known = Codex.HasItem(item.name);
                Color color = UITheme.ForRarity(item.Rarity);
                string kind = Loc.T(item.Rarity.ToString().ToUpperInvariant() + (item.IsPassive ? " PASSIVE" : " ITEM"));
                Row(itemRows, UISkin.Icon(item), known ? color : UITheme.TextMuted, known ? item.DisplayName : "???",
                    known ? SchoolInfo.Line(item.Schools) : kind, known ? item.Summary : Loc.T("Not found yet"), known);
            }
            ShowPage(tab);
        }

        private void ShowPage(int index)
        {
            tab = index;
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
            if (found == null) return;
            int have = 0, all = index == 0 ? Enemies.Length : items.Length;
            if (index == 0) { foreach ((string type, string name, string lore) in Enemies) if (Codex.Kills(type) > 0) have++; }
            else if (index == 1) { foreach (ItemBase item in items) if (Codex.HasItem(item.name)) have++; }
            found.text = index == 2 ? "" : Loc.F("Discovered {0} / {1}", have, all);
        }

        private static void Clear(RectTransform rows)
        {
            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
        }

        /// <summary>One entry: icon, name with a note under it, and the text.</summary>
        private static void Row(RectTransform rows, Sprite icon, Color color, string name, string note, string text, bool known)
        {
            RectTransform row = UIFactory.Item(rows, name, -1f, 78f);
            UIFactory.Image(row, new Color(1f, 1f, 1f, 0.06f), UISkin.Slot);
            HorizontalLayoutGroup line = UIFactory.Row(row, 18f, 0, TextAnchor.MiddleLeft);
            line.padding = new RectOffset(16, 16, 6, 6);
            line.childForceExpandHeight = false;
            UIFactory.Icon(row, icon, 56f, known ? Color.white : new Color(1f, 1f, 1f, 0.25f));
            RectTransform title = UIFactory.Item(row, "Title", 320f, 62f);
            UIFactory.Column(title, 2f, 0, TextAnchor.MiddleLeft);
            TextMeshProUGUI nameText = UIFactory.Sized(UIFactory.Text(title, "Name", name, 25f, color, TextAlignmentOptions.Left), -1f, 32f);
            nameText.fontStyle = FontStyles.Bold;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Sized(UIFactory.Text(title, "Note", note, 18f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 24f).textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.Sized(UIFactory.Text(row, "Text", text, 21f, known ? UITheme.Text : UITheme.TextMuted, TextAlignmentOptions.Left), 690f, 62f);
        }

        private static void FillSchools(RectTransform rows)
        {
            foreach (School school in SchoolInfo.All)
                Row(rows, UISkin.Icon(school), SchoolInfo.Color(school), SchoolInfo.Name(school), Loc.T("2 items  ·  4 items"),
                    SchoolInfo.Bonus(school, 1) + "\n" + SchoolInfo.Bonus(school, 2), true);
            foreach (Duo duo in DuoInfo.All)
                Row(rows, UISkin.Icon("Quest"), UITheme.Gold, DuoInfo.Name(duo), DuoInfo.Pair(duo), DuoInfo.Effect(duo), true);
        }
    }
}
