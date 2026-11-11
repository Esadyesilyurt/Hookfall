// Hookfall - run-end screen (GDD 6, 9.1): how long the Warden held the watch, a "new record" ribbon, stage, level,
// kills and gold, the build with its levels, which items dealt the damage, the best run and the global rank;
// Retry (R) or Menu.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin; rebuilt on
// 2026-11-10 with the damage breakdown. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Online;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class RunEndUI : MonoBehaviour
    {
        [SerializeField] private XPSystem xp;
        [SerializeField] private GoldWallet gold;
        [SerializeField] private CombatSystem combat;
        [Tooltip("How many items the damage list shows, strongest first.")]
        [SerializeField] private int damageRows = 4;

        private GameObject root;
        private RectTransform panel, stats, items, damage;
        private TextMeshProUGUI time, reason, best, rank;
        private GameObject record;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Run End Canvas", 40);
            root = canvas.gameObject;
            UIFactory.Dim(canvas.transform, 0.8f);

            Image image = UIFactory.Panel(canvas.transform, "Panel", new Vector2(1000f, 970f), UITheme.Panel);
            panel = (RectTransform)image.transform;
            VerticalLayoutGroup column = UIFactory.Column(image, 10f, 44, TextAnchor.UpperCenter);
            column.padding = new RectOffset(54, 54, 44, 40);
            UIFactory.Heading(panel, "THE WATCH ENDS", 46f, UITheme.Player);
            reason = UIFactory.Sized(UIFactory.Text(panel, "Reason", "", 23f, UITheme.TextMuted), -1f, 28f);
            time = UIFactory.Sized(UIFactory.Title(panel, "Time", "00:00", 104f, UITheme.Text, TextAlignmentOptions.Center, false, true), -1f, 112f);
            time.characterSpacing = 8f;

            RectTransform ribbon = UIFactory.Item(panel, "Record", -1f, 48f);
            UIFactory.Image(UIFactory.Centre(ribbon, "Back", Vector2.zero, new Vector2(420f, 48f)), UITheme.Gold, UISkin.Ribbon);
            TextMeshProUGUI recordText = UIFactory.Text(ribbon, "Text", "NEW RECORD", 26f, UITheme.TextDark);
            UIFactory.Stretch((RectTransform)recordText.transform);
            recordText.fontStyle = FontStyles.Bold;
            recordText.characterSpacing = 8f;
            record = ribbon.gameObject;

            stats = UIFactory.Item(panel, "Stats", -1f, 92f);
            UIFactory.Row(stats, 16f, 0).childForceExpandHeight = false;
            items = UIFactory.Item(panel, "Items", -1f, 74f);
            UIFactory.Row(items, 6f, 0).childForceExpandHeight = false;

            UIFactory.Heading(panel, "DAMAGE DEALT", 22f, UITheme.TextMuted);
            damage = UIFactory.Item(panel, "Damage", -1f, damageRows * 40f);
            UIFactory.Column(damage, 4f, 0, TextAnchor.UpperCenter);

            best = UIFactory.Sized(UIFactory.Text(panel, "Best", "", 21f, UITheme.TextMuted), -1f, 28f);
            rank = UIFactory.Sized(UIFactory.Text(panel, "Rank", "", 21f, UITheme.Gold), -1f, 28f);

            RectTransform buttons = UIFactory.Item(panel, "Buttons", -1f, 68f);
            UIFactory.Row(buttons, 30f, 0).childForceExpandHeight = false;
            UIFactory.Button(buttons, "Retry  [R]", () => GameManager.Instance?.Restart(), 330f, 66f, UITheme.ButtonPrimary, UISkin.Icon("Reroll"));
            UIFactory.Button(buttons, "Menu", () => GameManager.Instance?.QuitToMenu(), 330f, 66f, null, UISkin.Icon("Back"));
            UIPop.Add(image, 0.15f, 0.8f, new Vector2(0f, -80f));
            root.SetActive(false);
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.RunEnded += Show;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.RunEnded -= Show;
        }

        private void Update()
        {
            if (root.activeSelf && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                GameManager.Instance?.Restart();
        }

        private void Show()
        {
            GameManager gm = GameManager.Instance;
            time.text = RunRecord.FormatTime(gm.RunTime);
            reason.text = Loc.T(gm.EndReason);
            record.SetActive(gm.NewRecord);

            foreach (Transform child in stats) Destroy(child.gameObject);
            int stage = Hookfall.World.StageDirector.Instance != null ? Hookfall.World.StageDirector.Instance.Stage : 1;
            Stat("Stage", "Stage", stage.ToString());   // stages since 2026-11-08
            Stat("Star", "Level", (xp != null ? xp.Level : 1).ToString());
            Stat("Kills", "Kills", gm.Kills.ToString());
            Stat("Gold", "Gold", (gold != null ? gold.Earned : 0).ToString());
            Stat("Shard", "Shards", "+" + gm.ShardsEarned);   // for the menu's upgrade shop (2026-11-11)

            foreach (Transform child in items) Destroy(child.gameObject);
            foreach (Transform child in damage) Destroy(child.gameObject);
            if (combat != null)
            {
                foreach (ItemBase item in combat.Items) ItemCell(item);
                var dealers = new List<ItemBase>();
                foreach (ItemBase item in combat.Items) if (item.DamageDealt > 0f) dealers.Add(item);
                dealers.Sort((a, b) => b.DamageDealt.CompareTo(a.DamageDealt));
                float top = dealers.Count > 0 ? dealers[0].DamageDealt : 1f;
                for (int i = 0; i < dealers.Count && i < damageRows; i++) DamageRow(dealers[i], top);
            }

            best.text = !gm.NewRecord && RunRecord.HasRecord
                ? Loc.F("Best run: stage {0}  ·  {1}  ·  {2} kills", RunRecord.BestStage, RunRecord.FormatTime(RunRecord.BestTime), RunRecord.BestKills)
                : "";
            ShowRank();
            root.SetActive(true);
        }

        /// <summary>Waits for the run to reach the global leaderboard, then shows the player's rank.</summary>
        private async void ShowRank()
        {
            rank.text = Leaderboard.LastSubmit != null ? Loc.T("Sending to the global leaderboard...") : "";
            if (Leaderboard.LastSubmit == null) return;
            await Leaderboard.LastSubmit;
            LeaderboardRow? me = await Leaderboard.Current.MineAsync();
            if (this == null) return;
            rank.text = !me.HasValue ? Leaderboard.Current.Status
                : me.Value.Rank <= LeaderboardPanel.Shown ? Loc.F("Global rank #{0}  ·  your best {1}", me.Value.Rank, Loc.F("S{0}", me.Value.Stage) + " " + RunRecord.FormatTime(me.Value.RunSeconds))
                : Loc.F("On the global leaderboard  ·  your best {0}", Loc.F("S{0}", me.Value.Stage) + " " + RunRecord.FormatTime(me.Value.RunSeconds));   // ranks only inside the top 20
        }

        private void Stat(string icon, string label, string value)
        {
            RectTransform cell = UIFactory.Item(stats, label, 206f, 88f);
            UIFactory.Image(cell, Color.white, UISkin.Plaque);
            HorizontalLayoutGroup row = UIFactory.Row(cell, 12f, 0);
            row.padding = new RectOffset(16, 16, 8, 8);
            row.childForceExpandHeight = false;
            UIFactory.Icon(cell, UISkin.Icon(icon), 50f);
            RectTransform text = UIFactory.Item(cell, "Text", 104f, 70f);
            UIFactory.Column(text, -4f, 0, TextAnchor.MiddleLeft);
            TextMeshProUGUI number = UIFactory.Sized(UIFactory.Text(text, "Value", value, 38f, UITheme.Text, TextAlignmentOptions.Left), -1f, 44f);
            number.fontStyle = FontStyles.Bold;
            number.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(number, 20f);
            UIFactory.Sized(UIFactory.Text(text, "Label", Loc.Upper(Loc.T(label)), 16f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 22f).characterSpacing = 4f;
        }

        private void ItemCell(ItemBase item)
        {
            RectTransform cell = UIFactory.Item(items, item.name, 66f, 66f);
            UIFactory.Image(cell, Color.white, UISkin.Slot);
            UIFactory.Image(UIFactory.Fill(cell, "Frame"), UITheme.ForRarity(item.Rarity), UISkin.SlotFrame);
            Image icon = UIFactory.Image(UIFactory.Centre(cell, "Icon", new Vector2(0f, 4f), new Vector2(46f, 46f)), Color.white, UISkin.Icon(item));
            icon.preserveAspect = true;
            icon.enabled = icon.sprite != null;
            UIFactory.Pips(cell, item.Level, ItemBase.MaxLevel, 8f, UITheme.Player, new Color(1f, 1f, 1f, 0.22f));
        }

        /// <summary>One item's share of the damage: icon, name, a bar against the strongest item, the number.</summary>
        private void DamageRow(ItemBase item, float top)
        {
            RectTransform row = UIFactory.Item(damage, item.name, -1f, 36f);
            HorizontalLayoutGroup line = UIFactory.Row(row, 12f, 0, TextAnchor.MiddleLeft);
            line.childForceExpandHeight = false;
            UIFactory.Icon(row, UISkin.Icon(item), 34f);
            TextMeshProUGUI name = UIFactory.Sized(UIFactory.Text(row, "Name", item.DisplayName, 20f, UITheme.Text, TextAlignmentOptions.Left), 250f, 30f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(name, 12f);
            RectTransform holder = UIFactory.Item(row, "Bar", 430f, 16f);
            UIBar bar = UIBar.Create(holder, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 16f), UITheme.ForRarity(item.Rarity));
            bar.Set(item.DamageDealt / Mathf.Max(1f, top), true);
            TextMeshProUGUI number = UIFactory.Sized(UIFactory.Text(row, "Value", item.DamageDealt.ToString("N0"), 20f, UITheme.Text, TextAlignmentOptions.Right), 120f, 30f);
            number.fontStyle = FontStyles.Bold;
        }
    }
}
