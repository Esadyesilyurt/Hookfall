// Hookfall - run-end screen: how long the Warden held the watch, level, kills, gold and items with icons, a
// "new record" ribbon; Retry (R) or Menu (GDD 6, 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
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
        private RectTransform panel, stats, items;
        private TextMeshProUGUI time, reason, best;
        private GameObject record;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Run End Canvas", 40);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0.02f, 0.03f, 0.06f, 0.82f));

            Image image = UIFactory.Panel(canvas.transform, "Panel", new Vector2(860f, 820f), UITheme.Panel);
            panel = (RectTransform)image.transform;
            UIFactory.Column(image, 14f, 44, TextAnchor.UpperCenter);
            TextMeshProUGUI title = UIFactory.Sized(UIFactory.Text(panel, "Title", "THE WATCH ENDS", 46f, UITheme.Player, TextAlignmentOptions.Center, true), -1f, 60f);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 10f;
            reason = UIFactory.Sized(UIFactory.Text(panel, "Reason", "", 24f, UITheme.TextMuted), -1f, 32f);
            time = UIFactory.Sized(UIFactory.Text(panel, "Time", "00:00", 96f, UITheme.Text, TextAlignmentOptions.Center, true), -1f, 110f);
            time.fontStyle = FontStyles.Bold;

            RectTransform ribbon = UIFactory.Rect(panel, "Record", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(ribbon, -1f, 50f);
            UIFactory.Image(UIFactory.Centre(ribbon, "Back", Vector2.zero, new Vector2(380f, 50f)), UITheme.Gold, UISkin.Pill);
            TextMeshProUGUI recordText = UIFactory.Text(ribbon, "Text", "NEW RECORD", 28f, new Color(0.2f, 0.12f, 0.02f));
            UIFactory.Stretch((RectTransform)recordText.transform);
            recordText.fontStyle = FontStyles.Bold;
            recordText.characterSpacing = 8f;
            record = ribbon.gameObject;

            stats = UIFactory.Rect(panel, "Stats", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(stats, -1f, 90f);
            UIFactory.Row(stats, 50f, 0);
            items = UIFactory.Rect(panel, "Items", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(items, -1f, 76f);
            UIFactory.Row(items, 12f, 0);
            best = UIFactory.Sized(UIFactory.Text(panel, "Best", "", 22f, UITheme.TextMuted), -1f, 34f);

            RectTransform buttons = UIFactory.Rect(panel, "Buttons", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(buttons, -1f, 70f);
            UIFactory.Row(buttons, 30f, 0);
            UIFactory.Button(buttons, "Retry  [R]", () => GameManager.Instance?.Restart(), 300f, 64f, new Color(0.2f, 0.55f, 0.62f));
            UIFactory.Button(buttons, "Menu", () => GameManager.Instance?.QuitToMenu(), 300f, 64f);
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
            reason.text = gm.EndReason;
            record.SetActive(gm.NewRecord);

            foreach (Transform child in stats) Destroy(child.gameObject);
            Stat("XPGain", "Level", (xp != null ? xp.Level : 1).ToString());
            Stat("Kills", "Kills", gm.Kills.ToString());
            Stat("Gold", "Gold", (gold != null ? gold.Earned : 0).ToString());

            foreach (Transform child in items) Destroy(child.gameObject);
            if (combat != null)
                foreach (ItemBase item in combat.Items)
                {
                    RectTransform cell = UIFactory.Rect(items, item.DisplayName, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    UIFactory.Sized(cell, 76f, 76f);
                    UIFactory.Image(cell, UITheme.PanelLight, UISkin.Card);
                    Image icon = UIFactory.Icon(cell, UISkin.Icon(item), 52f);
                    ((RectTransform)icon.transform).anchoredPosition = new Vector2(0f, 6f);
                    UIFactory.Pips(cell, item.Level, ItemBase.MaxLevel, 8f, UITheme.Player, new Color(1f, 1f, 1f, 0.2f));
                }

            best.text = !gm.NewRecord && RunRecord.HasRecord
                ? $"Best watch {RunRecord.FormatTime(RunRecord.BestTime)}  ·  level {RunRecord.BestLevel}  ·  {RunRecord.BestKills} kills"
                : "";
            root.SetActive(true);
        }

        private void Stat(string icon, string label, string value)
        {
            RectTransform cell = UIFactory.Rect(stats, label, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(cell, 190f, 90f);
            UIFactory.Row(cell, 10f, 0);
            UIFactory.Icon(cell, UISkin.Icon(icon), 48f);
            RectTransform text = UIFactory.Rect(cell, "Text", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(text, 120f, 90f);
            UIFactory.Column(text, 0f, 0, TextAnchor.MiddleLeft);
            UIFactory.Sized(UIFactory.Text(text, "Value", value, 40f, UITheme.Text, TextAlignmentOptions.Left, true), -1f, 48f).fontStyle = FontStyles.Bold;
            UIFactory.Sized(UIFactory.Text(text, "Label", label.ToUpper(), 16f, UITheme.TextMuted, TextAlignmentOptions.Left), -1f, 22f).characterSpacing = 4f;
        }
    }
}
