// Hookfall - the main menu's global leaderboard: the top ten best watches, the player's own rank, and the name shown on
// the board. Reloads every time it opens; says so plainly when the service cannot be reached.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Text;
using Hookfall.Core;
using Hookfall.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class LeaderboardPanel : MonoBehaviour
    {
        private TextMeshProUGUI rows, mine, status;
        private TMP_InputField nameField;

        public static GameObject Build(Transform canvas, Action onBack)
        {
            Image image = UIFactory.Panel(canvas, "Leaderboard", new Vector2(980f, 780f), UITheme.Panel);
            UIFactory.Column(image, 14f, 40, TextAnchor.UpperCenter);
            var panel = image.gameObject.AddComponent<LeaderboardPanel>();

            TextMeshProUGUI title = UIFactory.Sized(UIFactory.Text(image.transform, "Title", "GLOBAL LEADERBOARD", 42f, UITheme.Player, TextAlignmentOptions.Center, true), -1f, 56f);
            title.characterSpacing = 10f;
            UIFactory.Sized(UIFactory.Text(image.transform, "Header", "<pos=2%>RANK<pos=16%>WARDEN<pos=62%>WATCH<pos=80%>CHARACTER", 20f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 28f);
            panel.rows = UIFactory.Sized(UIFactory.Text(image.transform, "Rows", "", 26f, UITheme.Text, TextAlignmentOptions.TopLeft), -1f, 360f);
            panel.mine = UIFactory.Sized(UIFactory.Text(image.transform, "Mine", "", 26f, UITheme.Gold, TextAlignmentOptions.Center, true), -1f, 36f);

            RectTransform nameRow = UIFactory.Rect(image.transform, "Name", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(nameRow, -1f, 64f);
            UIFactory.Row(nameRow, 16f, 0);
            panel.nameField = UIFactory.InputField(nameRow, "Your name", 440f, 60f);
            UIFactory.Button(nameRow, "Save name", panel.SaveName, 240f, 60f);

            panel.status = UIFactory.Sized(UIFactory.Text(image.transform, "Status", "", 20f, UITheme.TextMuted), -1f, 50f);
            UIFactory.Button(image.transform, "Back", () => onBack());
            UIPop.Add(image, 0f, 0.85f);
            return image.gameObject;
        }

        private void OnEnable()
        {
            if (rows != null) Refresh();
        }

        private async void Refresh()
        {
            rows.text = "Loading...";
            mine.text = "";
            var top = await Leaderboard.Current.TopAsync(10);
            LeaderboardRow? me = await Leaderboard.Current.MineAsync();
            if (this == null) return;   // the menu closed while waiting

            var text = new StringBuilder();
            foreach (LeaderboardRow row in top)
                text.Append($"<pos=2%>#{row.Rank}<pos=16%>{row.Name}<pos=62%>{RunRecord.FormatTime(row.Seconds)}<pos=80%>{row.Character}\n");
            rows.text = top.Count > 0 ? text.ToString() : "No watches on the board yet.";
            mine.text = me.HasValue ? $"You: #{me.Value.Rank}  ·  {RunRecord.FormatTime(me.Value.Seconds)}" : "You have no score on the board yet.";
            status.text = Leaderboard.Current.Status;
            string name = Leaderboard.Current.PlayerName;
            int hash = name.LastIndexOf('#');   // the service adds a #1234 tag to keep names unique
            if (!nameField.isFocused) nameField.text = hash > 0 ? name.Substring(0, hash) : name;
        }

        private async void SaveName()
        {
            string name = nameField.text.Trim().Replace(' ', '_');   // the service does not allow spaces
            if (name.Length == 0) return;
            status.text = "Saving...";
            await Leaderboard.Current.SetNameAsync(name);
            if (this != null) Refresh();
        }
    }
}
