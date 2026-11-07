// Hookfall - the in-run HUD (GDD 9): XP bar, level, health, watch time, gold, kills, crosshair, attack status,
// item slots and the tower cache marker. Built in uGUI from code.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as an IMGUI placeholder,
// rewritten on 2026-11-07 in uGUI. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private XPSystem xp;
        [SerializeField] private GoldWallet gold;
        [SerializeField] private CombatSystem combat;
        [SerializeField] private GrappleHook hook;

        /// <summary>Set every frame by world objects the player can use (chests); cleared after drawing.</summary>
        public static string InteractPrompt;
        /// <summary>Set by the tower cache while it waits on the tower; null hides the marker.</summary>
        public static Vector3? TowerCacheMarker;
        /// <summary>0..1 while the tower cache is being opened, otherwise negative.</summary>
        public static float TowerCacheProgress = -1f;

        private RectTransform xpFill, healthFill, cacheMarker, cacheArrow, cacheProgressFill;
        private TextMeshProUGUI levelText, healthText, timeText, goldText, killsText, crosshair, attackPill, promptText, cacheText;
        private Image attackPillBack;
        private readonly TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[CombatSystem.MaxItems];
        private readonly Image[] slotFrames = new Image[CombatSystem.MaxItems];
        private GameObject root;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "HUD Canvas", 0);
            root = canvas.gameObject;
            Transform c = canvas.transform;

            // Top edge: XP bar across the screen.
            xpFill = UIFactory.Bar(c, "XP Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 12f),
                new Color(0f, 0f, 0f, 0.5f), UITheme.Player, new Vector2(0.5f, 1f));

            // Top left: level and health.
            levelText = UIFactory.Text(UIFactory.Rect(c, "Level", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(300f, 40f), new Vector2(0f, 1f)),
                "Lv 1", 32f, UITheme.Text, TextAlignmentOptions.Left);
            healthFill = UIFactory.Bar(c, "Health Bar", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(360f, 26f),
                new Color(0f, 0f, 0f, 0.55f), UITheme.Health, new Vector2(0f, 1f));
            healthText = UIFactory.Text(UIFactory.Rect(c, "Health", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -100f), new Vector2(360f, 32f), new Vector2(0f, 1f)),
                "Health 100 / 100", 22f, UITheme.Text, TextAlignmentOptions.Left);

            // Top centre: watch time.
            timeText = UIFactory.Text(UIFactory.Rect(c, "Time", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(300f, 56f), new Vector2(0.5f, 1f)),
                "00:00", 46f, UITheme.Text);
            timeText.fontStyle = FontStyles.Bold;

            // Top right: gold and kills.
            goldText = UIFactory.Text(UIFactory.Rect(c, "Gold", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(300f, 40f), new Vector2(1f, 1f)),
                "Gold 0", 30f, UITheme.Reward, TextAlignmentOptions.Right);
            killsText = UIFactory.Text(UIFactory.Rect(c, "Kills", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -64f), new Vector2(300f, 36f), new Vector2(1f, 1f)),
                "Kills 0", 26f, UITheme.Text, TextAlignmentOptions.Right);

            // Centre: crosshair and the ATTACKS ON / OFF pill.
            crosshair = UIFactory.Text(UIFactory.Rect(c, "Crosshair", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f)),
                "+", 48f, UITheme.Text);
            RectTransform pill = UIFactory.Rect(c, "Attack Pill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(220f, 38f));
            attackPillBack = UIFactory.Image(pill, UITheme.Panel);
            attackPill = UIFactory.Text(pill.transform, "Label", "ATTACKS OFF", 22f, UITheme.TextMuted);
            UIFactory.Stretch((RectTransform)attackPill.transform);
            attackPill.fontStyle = FontStyles.Bold;

            promptText = UIFactory.Text(UIFactory.Rect(c, "Prompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(600f, 40f)),
                "", 26f, UITheme.Reward);

            // Bottom left: six item slots.
            for (int i = 0; i < CombatSystem.MaxItems; i++)
            {
                RectTransform slot = UIFactory.Rect(c, $"Slot {i + 1}", Vector2.zero, Vector2.zero, new Vector2(24f + i * 92f, 24f), new Vector2(84f, 84f), Vector2.zero);
                slotFrames[i] = UIFactory.Image(slot, UITheme.Panel);
                slotTexts[i] = UIFactory.Text(slot.transform, "Label", "", 20f, UITheme.Text);
                UIFactory.Stretch((RectTransform)slotTexts[i].transform);
            }

            // Bottom right.
            UIFactory.Text(UIFactory.Rect(c, "Pause Hint", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(200f, 36f), new Vector2(1f, 0f)),
                "Esc  Pause", 22f, UITheme.TextMuted, TextAlignmentOptions.Right);

            // Right edge: tower cache marker that slides along the edge towards the tower.
            cacheMarker = UIFactory.Rect(c, "Tower Cache Marker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 70f));
            UIFactory.Image(cacheMarker, UITheme.Panel);
            cacheText = UIFactory.Text(cacheMarker.transform, "Label", "Tower cache ready", 22f, UITheme.Reward);
            UIFactory.Stretch((RectTransform)cacheText.transform);
            cacheArrow = UIFactory.Rect(cacheMarker, "Arrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            UIFactory.Text(cacheArrow, ">", 36f, UITheme.Reward);
            cacheProgressFill = UIFactory.Bar(c, "Cache Progress", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(360f, 18f),
                new Color(0f, 0f, 0f, 0.6f), UITheme.Reward);
        }

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            root.SetActive(gm == null || gm.State != RunState.Dead);

            if (xp != null)
            {
                UIFactory.SetFill(xpFill, xp.CurrentXP / Mathf.Max(1f, xp.XPToNextLevel));
                levelText.text = $"Lv {xp.Level}";
            }
            if (stats != null)
            {
                UIFactory.SetFill(healthFill, stats.CurrentHealth / Mathf.Max(1f, stats.MaxHealth));
                healthText.text = $"Health {stats.CurrentHealth:0} / {stats.MaxHealth:0}";
            }
            if (gm != null)
            {
                timeText.text = RunRecord.FormatTime(gm.RunTime);
                killsText.text = $"Kills {gm.Kills}";
            }
            if (gold != null) goldText.text = $"Gold {gold.Gold}";

            bool inRange = hook != null && hook.HasTargetInRange;
            crosshair.color = inRange ? UITheme.Player : new Color(1f, 1f, 1f, 0.45f);

            bool attacks = combat != null && combat.AttacksActive;
            attackPill.text = attacks ? "ATTACKS ON" : "ATTACKS OFF";
            attackPill.color = attacks ? UITheme.Panel : UITheme.TextMuted;
            attackPillBack.color = attacks ? UITheme.Player : UITheme.Panel;

            promptText.text = InteractPrompt ?? "";
            InteractPrompt = null;

            DrawSlots();
            DrawCacheMarker();
        }

        private void DrawSlots()
        {
            for (int i = 0; i < CombatSystem.MaxItems; i++)
            {
                ItemBase item = combat != null && i < combat.Items.Count ? combat.Items[i] : null;
                slotTexts[i].text = item != null ? $"{Abbreviate(item.DisplayName)}\n<size=16>Lv {item.Level}</size>" : "";
                slotFrames[i].color = item != null ? UITheme.PanelLight : UITheme.Panel;
            }
        }

        private void DrawCacheMarker()
        {
            bool showMarker = TowerCacheMarker.HasValue && Camera.main != null;
            cacheMarker.gameObject.SetActive(showMarker);
            cacheProgressFill.parent.gameObject.SetActive(TowerCacheProgress >= 0f);
            if (TowerCacheProgress >= 0f) UIFactory.SetFill(cacheProgressFill, TowerCacheProgress);
            if (!showMarker) return;

            // Point along the screen edge towards the tower, even when it is behind the camera.
            Vector3 local = Camera.main.transform.InverseTransformPoint(TowerCacheMarker.Value);
            Vector2 dir = new Vector2(local.x, local.y);
            if (local.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            var canvasRect = (RectTransform)cacheMarker.parent;
            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(140f, 60f);
            float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            bool onScreen = local.z > 0f && Mathf.Abs(local.x / local.z) < 0.9f && Mathf.Abs(local.y / local.z) < 0.5f;
            cacheMarker.anchoredPosition = onScreen ? new Vector2(half.x, 0f) : dir * scale;
            cacheArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            cacheArrow.anchoredPosition = dir * 140f;
        }

        private static string Abbreviate(string name)
        {
            string[] words = name.Split(' ');
            return words.Length >= 2 ? $"{words[0][0]}{words[1][0]}" : name.Substring(0, Mathf.Min(2, name.Length));
        }
    }
}
