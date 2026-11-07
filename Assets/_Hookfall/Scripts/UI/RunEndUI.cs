// Hookfall - run-end screen: how long the Warden held the watch, level, kills, gold, items, new record; Retry or Menu (GDD 6, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Text;
using Hookfall.Combat;
using Hookfall.Core;
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

        private GameObject root;
        private TextMeshProUGUI title, body, record;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Run End Canvas", 40);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0f, 0f, 0f, 0.7f));

            Image panel = UIFactory.Panel(canvas.transform, "Panel", new Vector2(820f, 760f), UITheme.Panel);
            UIFactory.Column(panel, 18f, 44, TextAnchor.UpperCenter);
            title = UIFactory.Sized(UIFactory.Text(panel.transform, "Title", "", 46f, UITheme.Player), -1f, 120f);
            record = UIFactory.Sized(UIFactory.Text(panel.transform, "Record", "New record", 34f, UITheme.Reward), -1f, 46f);
            record.fontStyle = FontStyles.Bold;
            body = UIFactory.Sized(UIFactory.Text(panel.transform, "Body", "", 28f, UITheme.Text), -1f, 330f);

            RectTransform buttons = UIFactory.Rect(panel.transform, "Buttons", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(buttons, -1f, 70f);
            UIFactory.Row(buttons, 30f, 0);
            UIFactory.Button(buttons, "Retry  [R]", () => GameManager.Instance?.Restart(), 300f, 64f);
            UIFactory.Button(buttons, "Menu", () => GameManager.Instance?.QuitToMenu(), 300f, 64f);
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
            title.text = $"The Warden held the watch for {RunRecord.FormatTime(gm.RunTime)}\n<size=26><color=#A6B3C2>{gm.EndReason}</color></size>";
            record.gameObject.SetActive(gm.NewRecord);

            var sb = new StringBuilder();
            sb.AppendLine($"Level {(xp != null ? xp.Level : 1)}");
            sb.AppendLine($"Kills {gm.Kills}");
            sb.AppendLine($"Gold collected {(gold != null ? gold.Earned : 0)}");
            if (combat != null && combat.Items.Count > 0)
            {
                sb.Append("\n<size=24>");
                foreach (ItemBase item in combat.Items) sb.Append($"{item.DisplayName} Lv {item.Level}    ");
                sb.Append("</size>");
            }
            if (!gm.NewRecord && RunRecord.HasRecord)
                sb.AppendLine($"\n<size=22><color=#A6B3C2>Best: {RunRecord.FormatTime(RunRecord.BestTime)}, level {RunRecord.BestLevel}, {RunRecord.BestKills} kills</color></size>");
            body.text = sb.ToString();
            root.SetActive(true);
        }
    }
}
