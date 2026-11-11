// Hookfall - main menu (GDD 6, 9.1): the logo and the menu on the left over the live tower scene (Play, Leaderboard,
// Settings, Credits, Quit, the best run), the character select (the roster on the left, the chosen character in full on
// the right) and the intro shown on the first Play of a session.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; rebuilt on 2026-11-10 with the new skin and a roster
// with a detail view. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
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

        [Tooltip("The playable characters, set by the scene builder.")]
        [SerializeField] private CharacterDefinition[] characters = new CharacterDefinition[0];

        private static bool introShown;
        private GameObject select;
        private GameObject main, settings, creditsPanel, intro, leaderboard, shop;

        // Character select: the roster's tiles and the detail view of the one that is picked.
        private readonly List<Image> tileFrames = new List<Image>();
        private RectTransform detail;
        private Button startButton;
        private CharacterDefinition picked;

        private void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Canvas canvas = UIFactory.Canvas(transform, "Menu Canvas", 0);
            Transform c = canvas.transform;

            // A dark wash down the left side, so the menu reads over the bright sky.
            RectTransform shade = UIFactory.Rect(c, "Shade", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(1300f, 0f), new Vector2(0f, 0.5f));
            UIFactory.Image(shade, new Color(0.02f, 0.05f, 0.12f, 0.92f), UISkin.ShadeRight);

            main = UIFactory.Fill(c, "Main").gameObject;
            Transform m = main.transform;
            TextMeshProUGUI logo = UIFactory.Title(UIFactory.Rect(m, "Logo", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(104f, -84f), new Vector2(1100f, 170f), new Vector2(0f, 1f)),
                "HOOKFALL", 150f, Color.white, TextAlignmentOptions.Left, true, true);
            logo.characterSpacing = 10f;
            logo.enableVertexGradient = true;
            logo.colorGradient = new VertexGradient(new Color(0.75f, 1f, 1f), new Color(0.75f, 1f, 1f), UITheme.Player, new Color(0.16f, 0.72f, 0.86f));
            UIPop.Add(logo, 0f, 1.15f, new Vector2(-80f, 0f));
            TextMeshProUGUI tagline = UIFactory.Text(UIFactory.Rect(m, "Tagline", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -252f), new Vector2(1000f, 40f), new Vector2(0f, 1f)),
                "Hold the watch above the abyss", 30f, UITheme.Text, TextAlignmentOptions.Left, true);
            tagline.characterSpacing = 6f;
            UIPop.Add(tagline, 0.15f, 1f, new Vector2(-60f, 0f));

            RectTransform menu = UIFactory.Rect(m, "Menu", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, -110f), new Vector2(420f, 540f), new Vector2(0f, 0.5f));
            UIFactory.Column(menu, 14f, 0, TextAnchor.UpperLeft);
            UIFactory.Button(menu, "Play", OnPlay, -1f, 84f, UITheme.ButtonPrimary, UISkin.Icon("Play"));
            UIFactory.Button(menu, "Upgrades", () => Show(shop), -1f, 62f, null, UISkin.Icon("Shard"));
            UIFactory.Button(menu, "Leaderboard", () => Show(leaderboard), -1f, 62f, null, UISkin.Icon("Leaderboard"));
            UIFactory.Button(menu, "Settings", () => Show(settings), -1f, 62f, null, UISkin.Icon("Settings"));
            UIFactory.Button(menu, "Credits", () => Show(creditsPanel), -1f, 62f, null, UISkin.Icon("Credits"));
            UIFactory.Button(menu, "Quit", Quit, -1f, 62f, UITheme.ButtonDanger, UISkin.Icon("Quit"));
            UIPop.Add(menu, 0.25f, 0.9f, new Vector2(-60f, 0f));

            // The record to beat, under the menu.
            Image best = UIFactory.Plaque(m, "Best", Vector2.zero, new Vector2(110f, 70f), new Vector2(520f, 96f), Vector2.zero);
            HorizontalLayoutGroup bestRow = UIFactory.Row(best, 16f, 0, TextAnchor.MiddleLeft);
            bestRow.padding = new RectOffset(22, 20, 8, 8);
            bestRow.childForceExpandHeight = false;
            UIFactory.Icon(best.transform, UISkin.Icon("Trophy"), 60f);
            string record = RunRecord.HasRecord
                ? Loc.F("<b>Best: stage {0} · {1}</b>\nLevel {2} · {3} kills", RunRecord.BestStage, RunRecord.FormatTime(RunRecord.BestTime), RunRecord.BestLevel, RunRecord.BestKills)
                : "No watch held yet";
            UIFactory.Sized(UIFactory.Text(best.transform, "Text", record, 24f, UITheme.Gold, TextAlignmentOptions.Left), 400f, 76f);
            UIPop.Add(best, 0.4f, 0.9f, new Vector2(-60f, 0f));

            TextMeshProUGUI foot = UIFactory.Text(UIFactory.Rect(m, "Foot", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 24f), new Vector2(900f, 32f), new Vector2(1f, 0f)),
                "Esad Yesilyurt  ·  Istanbul Kent University", 23f, UITheme.Text, TextAlignmentOptions.Right, true);
            foot.fontStyle = FontStyles.Bold;

            settings = SettingsPanel.Build(c, () => Show(main));
            leaderboard = LeaderboardPanel.Build(c, () => Show(main));
            shop = ShopPanel.Build(c, () => Show(main));
            select = BuildSelect(c);

            Image creditsImage = UIFactory.Panel(c, "Credits", new Vector2(980f, 640f), UITheme.Panel);
            UIFactory.Column(creditsImage, 20f, 48, TextAnchor.UpperCenter);
            UIFactory.Heading(creditsImage.transform, "CREDITS", 42f, UITheme.Player);
            UIFactory.Sized(UIFactory.Text(creditsImage.transform, "Text", credits, 25f, UITheme.Text), -1f, 380f);
            UIFactory.Button(UIFactory.ButtonBar(creditsImage.transform), "Back", () => Show(main), 320f, 62f, null, UISkin.Icon("Back"));
            UIPop.Add(creditsImage, 0f, 0.85f);
            creditsPanel = creditsImage.gameObject;

            Image introImage = UIFactory.Panel(c, "Intro", new Vector2(1200f, 700f), UITheme.Panel);
            UIFactory.Column(introImage, 20f, 56, TextAnchor.UpperCenter);
            UIFactory.Diamond(UIFactory.Item(introImage.transform, "Emblem", -1f, 120f), UISkin.Icon("Zip"), "", 120f, UITheme.Player);
            for (int i = 0; i < introLines.Length; i++)
            {
                TextMeshProUGUI line = UIFactory.Sized(UIFactory.Text(introImage.transform, "Line", introLines[i], 30f, UITheme.Text), -1f, 96f);
                UIPop.Add(line, 0.2f + i * 0.5f, 1f);
            }
            RectTransform introButtons = UIFactory.Item(introImage.transform, "Buttons", -1f, 74f);
            UIFactory.Row(introButtons, 24f, 0).childForceExpandHeight = false;
            UIFactory.Button(introButtons, "Hold the watch", StartRun, 440f, 72f, UITheme.ButtonPrimary, UISkin.Icon("Play"));
            UIFactory.Button(introButtons, "Skip", StartRun, 220f, 72f);
            UIPop.Add(introImage, 0f, 0.9f);
            intro = introImage.gameObject;

            Show(main);
        }

        private void Show(GameObject panel)
        {
            main.SetActive(panel == main);
            settings.SetActive(panel == settings);
            leaderboard.SetActive(panel == leaderboard);
            shop.SetActive(panel == shop);
            select.SetActive(panel == select);
            creditsPanel.SetActive(panel == creditsPanel);
            intro.SetActive(panel == intro);
        }

        private void OnPlay() => Show(characters.Length > 0 ? select : intro);

        // ---------------------------------------------------------------- character select

        /// <summary>The roster as tiles on the left; on the right the picked character: what it is, what it starts with,
        /// or its unlock quest and how far it is.</summary>
        private GameObject BuildSelect(Transform canvas)
        {
            Image image = UIFactory.Panel(canvas, "Characters", new Vector2(1720f, 980f), UITheme.Panel);
            VerticalLayoutGroup column = UIFactory.Column(image, 16f, 44, TextAnchor.UpperCenter);
            column.padding = new RectOffset(50, 50, 40, 36);
            UIFactory.Heading(image.transform, "CHOOSE YOUR WARDEN", 44f, UITheme.Player);

            RectTransform body = UIFactory.Item(image.transform, "Body", -1f, 730f);
            HorizontalLayoutGroup split = UIFactory.Row(body, 34f, 0, TextAnchor.UpperLeft);
            split.childForceExpandHeight = false;

            RectTransform roster = UIFactory.Item(body, "Roster", 716f, 730f);
            var grid = roster.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(168f, 232f);
            grid.spacing = new Vector2(14f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            for (int i = 0; i < characters.Length; i++) Tile(roster, characters[i], i);

            RectTransform side = UIFactory.Item(body, "Detail", 870f, 730f);
            UIFactory.Image(side, Color.white, UISkin.Plaque);
            detail = UIFactory.Fill(side, "Content");
            VerticalLayoutGroup lines = UIFactory.Column(detail, 10f, 0, TextAnchor.UpperLeft);
            lines.padding = new RectOffset(34, 34, 28, 24);

            RectTransform buttons = UIFactory.Item(image.transform, "Buttons", -1f, 72f);
            UIFactory.Row(buttons, 24f, 0).childForceExpandHeight = false;
            UIFactory.Button(buttons, "Back", () => Show(main), 300f, 68f, null, UISkin.Icon("Back"));
            startButton = UIFactory.Button(buttons, "Hold the watch", () => Choose(picked), 460f, 68f, UITheme.ButtonPrimary, UISkin.Icon("Play"));
            UIPop.Add(image, 0f, 0.85f);

            // Open on the character played last (or the first one).
            CharacterDefinition first = characters.Length > 0 ? characters[0] : null;
            foreach (CharacterDefinition character in characters)
                if (character != null && character.id == CharacterRoster.SelectedId && CharacterRoster.IsUnlocked(character)) first = character;
            if (first != null) Pick(first);
            return image.gameObject;
        }

        private void Tile(Transform roster, CharacterDefinition character, int index)
        {
            bool open = CharacterRoster.IsUnlocked(character);
            RectTransform tile = UIFactory.Item(roster, character.id);
            Image back = UIFactory.Image(tile, Color.white, UISkin.Slot);
            back.raycastTarget = true;
            tileFrames.Add(UIFactory.Image(UIFactory.Fill(tile, "Frame"), new Color(1f, 1f, 1f, 0.25f), UISkin.SlotFrame));
            Image face = UIFactory.Image(UIFactory.Rect(tile, "Face", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(150f, 150f)), open ? Color.white : new Color(0.2f, 0.24f, 0.32f, 0.9f), UISkin.Icon(character.id));
            face.preserveAspect = true;
            face.enabled = face.sprite != null;
            TextMeshProUGUI name = UIFactory.Text(UIFactory.Rect(tile, "Name", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 44f), new Vector2(-12f, 30f)),
                Loc.T(character.displayName), 22f, open ? UITheme.Text : UITheme.TextMuted);
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(name, 13f);
            TextMeshProUGUI state = UIFactory.Text(UIFactory.Rect(tile, "State", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 20f), new Vector2(-12f, 22f)),
                open ? SchoolInfo.Name(character.affinity) : CharacterRoster.ProgressText(character), 16f, open ? SchoolInfo.Color(character.affinity) : UITheme.TextMuted);
            state.textWrappingMode = TextWrappingModes.NoWrap;
            if (!open)
            {
                Image padlock = UIFactory.Image(UIFactory.Rect(tile, "Lock", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(64f, 64f)), Color.white, UISkin.Icon("Lock"));
                padlock.preserveAspect = true;
            }
            var button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.onClick.AddListener(() => Pick(character));
            tile.gameObject.AddComponent<UIHover>().hoverScale = 1.04f;
            UIPop.Add(tile, 0.05f + index * 0.04f, 0.8f);
        }

        /// <summary>Shows a character on the right and lights its tile.</summary>
        private void Pick(CharacterDefinition character)
        {
            picked = character;
            bool open = CharacterRoster.IsUnlocked(character);
            for (int i = 0; i < tileFrames.Count; i++)
                tileFrames[i].color = characters[i] == character ? UITheme.Player : new Color(1f, 1f, 1f, 0.25f);
            startButton.interactable = open;

            foreach (Transform child in detail) Destroy(child.gameObject);
            Color accent = open ? SchoolInfo.Color(character.affinity) : UITheme.TextMuted;

            // Portrait beside the name, the school, the health and the air moves.
            RectTransform head = UIFactory.Item(detail, "Head", -1f, 220f);
            HorizontalLayoutGroup headRow = UIFactory.Row(head, 26f, 0, TextAnchor.MiddleLeft);
            headRow.childForceExpandHeight = false;
            UIFactory.Diamond(UIFactory.Item(head, "Portrait", 210f, 210f), UISkin.Icon(character.id), "?", 210f, accent);
            RectTransform facts = UIFactory.Item(head, "Facts", 560f, 200f);
            UIFactory.Column(facts, 8f, 0, TextAnchor.MiddleLeft);
            TextMeshProUGUI name = UIFactory.Sized(UIFactory.Title(facts, "Name", Loc.T(character.displayName), 50f, UITheme.Text, TextAlignmentOptions.Left, false), -1f, 62f);
            UIFactory.FitText(name, 24f);
            RectTransform chips = UIFactory.SchoolChips(facts, new[] { character.affinity }, 34f);
            chips.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Fact(facts, "Health", Loc.F("Health {0}", (100f * character.maxHealthMultiplier).ToString("0")));
            Fact(facts, "Jump", Loc.F("{0} double jump · {1} dash", character.airJumps, character.airDashes));

            TextMeshProUGUI about = UIFactory.Sized(UIFactory.Text(detail, "About", open ? Loc.T(character.description) : Loc.T(character.unlockText),
                23f, open ? UITheme.Text : UITheme.Gold, TextAlignmentOptions.TopLeft), -1f, 78f);
            UIFactory.FitText(about, 15f);

            if (!open)
            {
                // The unlock quest and how far it is.
                RectTransform holder = UIFactory.Item(detail, "Quest", -1f, 26f);
                UIBar bar = UIBar.Create(holder, "Bar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 22f), UITheme.Gold);
                bar.Set(CharacterRoster.Progress(character), true);
                UIFactory.Sized(UIFactory.Text(detail, "Progress", Loc.F("LOCKED  ·  {0}", CharacterRoster.ProgressText(character)), 22f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 30f);
                return;
            }

            UIFactory.Heading(detail, "PERKS", 22f, accent);
            foreach (string perk in character.perks)
            {
                RectTransform row = UIFactory.Item(detail, "Perk", -1f, 30f);
                HorizontalLayoutGroup line = UIFactory.Row(row, 12f, 0, TextAnchor.MiddleLeft);
                line.childForceExpandHeight = false;
                UIFactory.Image(UIFactory.Item(row, "Pip", 14f, 14f), accent, UISkin.Pip);
                UIFactory.Text(row, "Text", perk, 23f, UITheme.Text, TextAlignmentOptions.Left).textWrappingMode = TextWrappingModes.NoWrap;
            }

            UIFactory.Heading(detail, "STARTS WITH", 22f, accent);
            RectTransform items = UIFactory.Item(detail, "Items", -1f, 92f);
            HorizontalLayoutGroup itemRow = UIFactory.Row(items, 16f, 0, TextAnchor.MiddleLeft);
            itemRow.childForceExpandHeight = false;
            foreach (ItemBase item in character.startingItems)
            {
                if (item == null) continue;
                RectTransform cell = UIFactory.Item(items, item.name, 390f, 86f);
                UIFactory.Image(cell, new Color(1f, 1f, 1f, 0.06f), UISkin.Chip);
                HorizontalLayoutGroup cellRow = UIFactory.Row(cell, 14f, 0, TextAnchor.MiddleLeft);
                cellRow.padding = new RectOffset(10, 10, 6, 6);
                cellRow.childForceExpandHeight = false;
                RectTransform slot = UIFactory.Item(cell, "Slot", 70f, 70f);
                UIFactory.Image(slot, Color.white, UISkin.Slot);
                UIFactory.Image(UIFactory.Fill(slot, "Frame"), UITheme.ForRarity(item.Rarity), UISkin.SlotFrame);
                Image icon = UIFactory.Image(UIFactory.Centre(slot, "Icon", Vector2.zero, new Vector2(52f, 52f)), Color.white, UISkin.Icon(item));
                icon.preserveAspect = true;
                icon.enabled = icon.sprite != null;
                RectTransform text = UIFactory.Item(cell, "Text", 280f, 70f);
                UIFactory.Column(text, 0f, 0, TextAnchor.MiddleLeft);
                TextMeshProUGUI itemName = UIFactory.Sized(UIFactory.Text(text, "Name", item.DisplayName, 22f, UITheme.ForRarity(item.Rarity), TextAlignmentOptions.Left), -1f, 30f);
                itemName.fontStyle = FontStyles.Bold;
                itemName.textWrappingMode = TextWrappingModes.NoWrap;
                UIFactory.FitText(itemName, 13f);
                TextMeshProUGUI what = UIFactory.Sized(UIFactory.Text(text, "Summary", item.Summary, 16f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 40f);
                UIFactory.FitText(what, 11f);
            }
        }

        private static void Fact(Transform parent, string icon, string text)
        {
            RectTransform row = UIFactory.Item(parent, icon, -1f, 34f);
            HorizontalLayoutGroup line = UIFactory.Row(row, 10f, 0, TextAnchor.MiddleLeft);
            line.childForceExpandHeight = false;
            UIFactory.Icon(row, UISkin.Icon(icon), 30f);
            TextMeshProUGUI label = UIFactory.Text(row, "Text", text, 24f, UITheme.Text, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void Choose(CharacterDefinition character)
        {
            if (character == null || !CharacterRoster.IsUnlocked(character)) return;
            CharacterRoster.SelectedId = character.id;
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
