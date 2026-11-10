// Hookfall - the main menu's global leaderboard: the top 20 best watches as rows (gold, silver and bronze for the
// first three, the player's own row lit), the player's own score below them without a rank when they are not in the
// top 20, and the name shown on the board. Reloads every time it opens; says so plainly when the service cannot be
// reached.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08; rows instead of one text block on 2026-11-10.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
using Hookfall.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class LeaderboardPanel : MonoBehaviour
    {
        public const int Shown = 20;

        private static readonly Color[] Medals = { new Color(1f, 0.8f, 0.3f), new Color(0.78f, 0.84f, 0.92f), new Color(0.86f, 0.56f, 0.34f) };

        private RectTransform rows;
        private TextMeshProUGUI mine, status, message;
        private TMP_InputField nameField;

        public static GameObject Build(Transform canvas, Action onBack)
        {
            Image image = UIFactory.Panel(canvas, "Leaderboard", new Vector2(1040f, 1010f), UITheme.Panel);
            VerticalLayoutGroup column = UIFactory.Column(image, 10f, 40, TextAnchor.UpperCenter);
            column.padding = new RectOffset(50, 50, 40, 36);
            var panel = image.gameObject.AddComponent<LeaderboardPanel>();

            UIFactory.Heading(image.transform, "GLOBAL LEADERBOARD", 40f, UITheme.Player);
            Row(UIFactory.Item(image.transform, "Header", -1f, 28f), "RANK", "WARDEN", "WATCH", "CHARACTER", UITheme.TextMuted, 18f, true);
            panel.rows = UIFactory.Scroll(image.transform, "Rows", -1f, 560f, 3f);
            panel.message = UIFactory.Text(UIFactory.ScrollBox(panel.rows), "Message", "", 24f, UITheme.TextMuted);
            UIFactory.Stretch((RectTransform)panel.message.transform);
            panel.mine = UIFactory.Sized(UIFactory.Text(image.transform, "Mine", "", 24f, UITheme.Gold), -1f, 34f);
            panel.mine.fontStyle = FontStyles.Bold;

            RectTransform nameRow = UIFactory.Item(image.transform, "Name", -1f, 64f);
            UIFactory.Row(nameRow, 16f, 0).childForceExpandHeight = false;
            panel.nameField = UIFactory.InputField(nameRow, "Your name", 440f, 58f);
            UIFactory.Button(nameRow, "Save name", panel.SaveName, 240f, 58f);

            panel.status = UIFactory.Sized(UIFactory.Text(image.transform, "Status", "", 19f, UITheme.TextMuted), -1f, 44f);
            UIFactory.Button(UIFactory.ButtonBar(image.transform), "Back", () => onBack(), 320f, 62f, null, UISkin.Icon("Back"));
            UIPop.Add(image, 0f, 0.85f);
            return image.gameObject;
        }

        /// <summary>One line of the table: rank, name, watch, character, in fixed columns.</summary>
        private static void Row(RectTransform row, string rank, string name, string watch, string character, Color color, float size, bool header = false)
        {
            HorizontalLayoutGroup line = UIFactory.Row(row, 12f, 0, TextAnchor.MiddleLeft);
            line.padding = new RectOffset(18, 18, 0, 0);
            line.childForceExpandHeight = false;
            (string text, float width)[] cells = { (rank, 90f), (name, 400f), (watch, 190f), (character, 200f) };
            foreach ((string text, float width) in cells)
            {
                TextMeshProUGUI cell = UIFactory.Sized(UIFactory.Text(row, "Cell", text, size, color, TextAlignmentOptions.Left), width, size * 1.4f);
                cell.textWrappingMode = TextWrappingModes.NoWrap;
                cell.overflowMode = TextOverflowModes.Ellipsis;
                if (header) cell.characterSpacing = 4f;
                else if (width < 100f) cell.fontStyle = FontStyles.Bold;
            }
        }

        private void OnEnable()
        {
            if (rows != null) Refresh();
        }

        private async void Refresh()
        {
            foreach (Transform child in rows) Destroy(child.gameObject);
            message.text = Loc.T("Loading...");
            mine.text = "";
            var top = await Leaderboard.Current.TopAsync(Shown);
            LeaderboardRow? me = await Leaderboard.Current.MineAsync();
            if (this == null) return;   // the menu closed while waiting

            foreach (Transform child in rows) Destroy(child.gameObject);
            string myName = Leaderboard.Current.PlayerName;
            for (int i = 0; i < top.Count; i++)
            {
                LeaderboardRow entry = top[i];
                bool own = me.HasValue && me.Value.Rank == entry.Rank;
                Color color = own ? UITheme.Player : entry.Rank <= Medals.Length ? Medals[entry.Rank - 1] : UITheme.Text;
                RectTransform row = UIFactory.Item(rows, "Row", -1f, 26f);
                UIFactory.Image(row, own ? new Color(0.3f, 0.95f, 1f, 0.16f) : new Color(1f, 1f, 1f, i % 2 == 0 ? 0.05f : 0.02f), UISkin.Chip);
                Row(row, "#" + entry.Rank, entry.Name, Loc.F("S{0}", entry.Stage) + "  " + RunRecord.FormatTime(entry.RunSeconds), Loc.T(entry.Character), color, 20f);
            }
            message.text = top.Count > 0 ? "" : Loc.T("No watches on the board yet.");
            mine.text = !me.HasValue ? Loc.T("You have no score on the board yet.")
                : me.Value.Rank <= Shown ? Loc.T("You are in the top 20.")
                : Loc.F("You  ·  {0}  ·  {1}", me.Value.Name, RunRecord.FormatTime(me.Value.Seconds));   // no rank outside the top 20
            status.text = Leaderboard.Current.Status;
            int hash = myName.LastIndexOf('#');   // the service adds a #1234 tag to keep names unique
            if (!nameField.isFocused) nameField.text = hash > 0 ? myName.Substring(0, hash) : myName;
        }

        private async void SaveName()
        {
            string name = nameField.text.Trim().Replace(' ', '_');   // the service does not allow spaces
            if (name.Length == 0) return;
            status.text = Loc.T("Saving...");
            await Leaderboard.Current.SetNameAsync(name);
            if (this != null) Refresh();
        }
    }
}
