// Hookfall - the in-run HUD (GDD 9): XP bar with a level badge, health bar with a damage trail, watch-time plaque,
// gold and kills, crosshair with the Zip cooldown ring and charges, the ATTACKS ON/OFF pill, item slots with icons
// and level pips, control hints, the tower cache marker and the storm warning. Built in uGUI from code.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as an IMGUI placeholder, rewritten on 2026-11-07 in
// uGUI and again the same day with the UI skin and icons; the stage, the boss bar, the boss's health and an objective
// marker were added on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
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
    public class HUD : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private XPSystem xp;
        [SerializeField] private GoldWallet gold;
        [SerializeField] private CombatSystem combat;
        [SerializeField] private GrappleHook hook;

        /// <summary>Set every frame by world objects the player can use (chests); cleared after drawing.</summary>
        public static string InteractPrompt;

        private static string announceText;
        private static Color announceColor;
        private static float announceTime = -999f;
        private TextMeshProUGUI announce;

        /// <summary>A big line across the screen for a moment: power-ups, duos, the tower cache.</summary>
        public static void Announce(string text, Color color)
        {
            announceText = text;
            announceColor = color;
            announceTime = Time.unscaledTime;
        }
        /// <summary>Set by the tower cache while it waits on the tower; null hides the marker.</summary>
        public static Vector3? TowerCacheMarker;
        /// <summary>0..1 while the tower cache is being opened, otherwise negative.</summary>
        public static float TowerCacheProgress = -1f;

        private static Vector3? objective;
        private static string objectiveLabel;
        private static Color objectiveColor;

        /// <summary>Where the run goes next (the boss, the portal); null hides the marker.</summary>
        public static void SetObjective(Vector3? position, string label, Color color)
        {
            objective = position;
            objectiveLabel = label;
            objectiveColor = color;
        }

        private GameObject root;
        private RectTransform xpFill, healthFill, trailFill, cacheMarker, cacheArrow, cacheProgressFill, levelBadge, attackRect;
        private TextMeshProUGUI synergyText;
        private TextMeshProUGUI stageText, bossNameText, objectiveText;
        private RectTransform bossGoldFill, bossHealthFill, bossTrailFill, bossBar, objectiveMarker, objectiveArrow;
        private Image objectivePanel;
        private float bossTrail = 1f;
        private SynergySystem synergy;
        private int synergyHash = -1;
        private TextMeshProUGUI levelText, healthText, timeText, goldText, killsText, attackText, promptText, stormText;
        private Image crosshair, zipRing, attackBack, xpGlow;
        private RectTransform promptRow;
        private readonly Image[] zipPips = new Image[3];
        private const int SlotCount = CombatSystem.MaxItems + CombatSystem.MaxPassives;   // attack slots, then passive slots
        private readonly Image[] slotIcons = new Image[SlotCount];
        private readonly Image[] slotFrames = new Image[SlotCount];
        private readonly TextMeshProUGUI[] slotGlyphs = new TextMeshProUGUI[SlotCount];
        private readonly RectTransform[] slotPips = new RectTransform[SlotCount];
        private readonly ItemBase[] slotItem = new ItemBase[SlotCount];
        private readonly int[] slotLevel = new int[SlotCount];
        private float trail = 1f;
        private int shownLevel = 1;
        private float levelFlash, attackPulse;
        private bool lastAttacks;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "HUD Canvas", 0);
            root = canvas.gameObject;
            Transform c = canvas.transform;

            // Top edge: XP bar with a glow that flashes on level-up, and the level badge.
            xpFill = UIFactory.Bar(c, "XP Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-260f, 16f),
                UITheme.Track, UITheme.Xp, new Vector2(0.5f, 1f));
            var xpRect = (RectTransform)xpFill.parent;
            xpRect.offsetMin = new Vector2(130f, xpRect.offsetMin.y);   // starts right of the level badge
            xpRect.offsetMax = new Vector2(-24f, xpRect.offsetMax.y);
            xpGlow = UIFactory.Image(UIFactory.Rect(xpFill.parent, "Glow", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(40f, 40f)), new Color(0.4f, 0.95f, 1f, 0f), UISkin.Glow);
            levelBadge = UIFactory.Diamond(c, null, "", 104f, UITheme.Player);
            levelBadge.anchorMin = levelBadge.anchorMax = new Vector2(0f, 1f);
            levelBadge.anchoredPosition = new Vector2(70f, -62f);
            levelText = UIFactory.Text(levelBadge, "Level", "1", 40f, UITheme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Stretch((RectTransform)levelText.transform);
            levelText.fontStyle = FontStyles.Bold;

            // Health under the badge: heart, bar with a white damage trail, numbers.
            RectTransform healthBar = UIFactory.Rect(c, "Health", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -44f), new Vector2(380f, 30f), new Vector2(0f, 1f));
            UIFactory.Image(healthBar, UITheme.Track, UISkin.Pill);
            trailFill = UIFactory.Rect(healthBar, "Trail", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            UIFactory.Image(trailFill, new Color(1f, 0.92f, 0.85f, 0.9f), UISkin.Pill);
            healthFill = UIFactory.Rect(healthBar, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            UIFactory.Image(healthFill, UITheme.Health, UISkin.Pill);
            UIFactory.Image(UIFactory.Fill(healthBar, "Frame"), new Color(1f, 1f, 1f, 0.35f), UISkin.PillFrame);
            healthText = UIFactory.Text(healthBar, "Value", "100 / 100", 20f, UITheme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Stretch((RectTransform)healthText.transform);
            Image heart = UIFactory.Icon(c, UISkin.Icon("Health"), 44f);
            var heartRect = (RectTransform)heart.transform;
            heartRect.anchorMin = heartRect.anchorMax = new Vector2(0f, 1f);
            heartRect.anchoredPosition = new Vector2(146f, -59f);   // caps the left end of the bar

            // Top centre: watch time plaque.
            Image timePlaque = UIFactory.Panel(c, "Time", new Vector2(250f, 70f), UITheme.Panel);
            var timeRect = (RectTransform)timePlaque.transform;
            timeRect.anchorMin = timeRect.anchorMax = new Vector2(0.5f, 1f);
            timeRect.anchoredPosition = new Vector2(0f, -66f);
            Image hourglass = UIFactory.Icon(timePlaque.transform, UISkin.Icon("Timer"), 40f);
            ((RectTransform)hourglass.transform).anchoredPosition = new Vector2(-82f, 0f);
            timeText = UIFactory.Text(timePlaque.transform, "Value", "00:00", 42f, UITheme.Text, TextAlignmentOptions.Center, true);
            var timeTextRect = (RectTransform)timeText.transform;
            UIFactory.Stretch(timeTextRect);
            timeTextRect.offsetMin = new Vector2(36f, 0f);
            timeText.fontStyle = FontStyles.Bold;
            // Under the time: the stage and the boss bar, which fills with the gold earned on the stage (2026-11-08).
            stageText = UIFactory.Text(UIFactory.Rect(c, "Stage", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(640f, 30f)),
                "", 22f, UITheme.Text, TextAlignmentOptions.Center, true);
            bossGoldFill = UIFactory.Bar(c, "Boss Bar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -144f), new Vector2(320f, 12f),
                UITheme.Track, new Color(1f, 0.35f, 0.85f));
            // The boss's health across the top while it fights.
            bossBar = UIFactory.Rect(c, "Boss Health", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(820f, 26f));
            UIFactory.Image(bossBar, UITheme.Track, UISkin.Pill);
            bossTrailFill = UIFactory.Rect(bossBar, "Trail", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            UIFactory.Image(bossTrailFill, new Color(1f, 0.92f, 0.85f, 0.9f), UISkin.Pill);
            bossHealthFill = UIFactory.Rect(bossBar, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            UIFactory.Image(bossHealthFill, new Color(0.9f, 0.22f, 0.62f), UISkin.Pill);
            UIFactory.Image(UIFactory.Fill(bossBar, "Frame"), new Color(1f, 1f, 1f, 0.4f), UISkin.PillFrame);
            foreach (float mark in new[] { 0.7f, 0.35f })   // where it calls its escort
                UIFactory.Image(UIFactory.Rect(bossBar, "Mark", new Vector2(mark, 0f), new Vector2(mark, 1f), Vector2.zero, new Vector2(3f, 0f)), new Color(1f, 1f, 1f, 0.6f));
            bossNameText = UIFactory.Text(UIFactory.Rect(bossBar, "Name", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 20f), new Vector2(0f, 34f)),
                "", 26f, new Color(1f, 0.8f, 0.95f), TextAlignmentOptions.Center, true);
            bossNameText.fontStyle = FontStyles.Bold;
            bossNameText.characterSpacing = 6f;
            stormText = UIFactory.Text(UIFactory.Rect(c, "Storm", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(900f, 40f)),
                "The storm pushes you back", 28f, new Color(0.7f, 0.82f, 1f), TextAlignmentOptions.Center, true);

            // Top right: gold and kills.
            announce = UIFactory.Text(UIFactory.Rect(c, "Announce", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(1400f, 80f)),
                "", 54f, UITheme.Gold, TextAlignmentOptions.Center, true);
            announce.fontStyle = FontStyles.Bold;
            announce.characterSpacing = 6f;

            // Under the counters: the schools the build is made of, and how far each set is.
            synergyText = UIFactory.Text(UIFactory.Rect(c, "Synergies", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -170f), new Vector2(360f, 260f), new Vector2(1f, 1f)),
                "", 22f, UITheme.Text, TextAlignmentOptions.TopRight, true);

            Image counters = UIFactory.Panel(c, "Counters", new Vector2(250f, 110f), UITheme.Panel);
            var countersRect = (RectTransform)counters.transform;
            countersRect.anchorMin = countersRect.anchorMax = new Vector2(1f, 1f);
            countersRect.anchoredPosition = new Vector2(-150f, -96f);
            goldText = Counter(counters.transform, "Gold", 22f, UITheme.Gold);
            killsText = Counter(counters.transform, "Kills", -26f, UITheme.Text);

            // Centre: crosshair, Zip ring and charges, attack pill, prompt.
            RectTransform cross = UIFactory.Centre(c, "Crosshair", Vector2.zero, new Vector2(46f, 46f));
            crosshair = UIFactory.Image(cross, Color.white, UISkin.Crosshair);
            RectTransform ring = UIFactory.Centre(c, "Zip Ring", Vector2.zero, new Vector2(92f, 92f));
            zipRing = UIFactory.Image(ring, UITheme.Player, UISkin.Ring);
            zipRing.type = Image.Type.Filled;
            zipRing.fillMethod = Image.FillMethod.Radial360;
            zipRing.fillOrigin = (int)Image.Origin360.Top;
            zipRing.fillClockwise = true;
            for (int i = 0; i < zipPips.Length; i++)
            {
                RectTransform pip = UIFactory.Centre(c, "Zip Charge", new Vector2((i - 1) * 16f, -60f), new Vector2(11f, 11f));
                zipPips[i] = UIFactory.Image(pip, UITheme.Player, UISkin.Dot);
            }
            attackRect = UIFactory.Centre(c, "Attack Pill", new Vector2(0f, -96f), new Vector2(230f, 40f));
            attackBack = UIFactory.Image(attackRect, UITheme.Panel, UISkin.Pill);
            attackText = UIFactory.Text(attackRect, "Label", "ATTACKS OFF", 20f, UITheme.TextMuted);   // a child: the pill already has an Image
            UIFactory.Stretch((RectTransform)attackText.transform);
            attackText.fontStyle = FontStyles.Bold;
            attackText.characterSpacing = 4f;

            promptRow = UIFactory.Centre(c, "Prompt", new Vector2(0f, -160f), new Vector2(700f, 50f));
            UIFactory.Row(promptRow, 14f, 0);
            UIFactory.KeyCap(promptRow, "E");
            promptText = UIFactory.Sized(UIFactory.Text(promptRow, "Text", "", 26f, UITheme.Gold, TextAlignmentOptions.Left, true), 560f, 46f);

            // Bottom left: six item slots with icons and level pips, and six smaller passive slots above them.
            for (int i = 0; i < SlotCount; i++)
            {
                bool passive = i >= CombatSystem.MaxItems;
                int column = passive ? i - CombatSystem.MaxItems : i;
                float size = passive ? 66f : 94f;
                Vector2 position = passive ? new Vector2(24f + column * 74f, 128f) : new Vector2(24f + column * 102f, 24f);
                RectTransform slot = UIFactory.Rect(c, $"Slot {i + 1}", Vector2.zero, Vector2.zero, position, new Vector2(size, size), Vector2.zero);
                slotFrames[i] = UIFactory.Image(slot, UITheme.Panel, UISkin.Card);
                RectTransform iconRect = UIFactory.Centre(slot, "Icon", new Vector2(0f, size * 0.06f), new Vector2(size * 0.64f, size * 0.64f));
                slotIcons[i] = UIFactory.Image(iconRect, Color.white);
                slotIcons[i].preserveAspect = true;
                // Items with no icon yet show their first letter instead.
                slotGlyphs[i] = UIFactory.Text(UIFactory.Centre(slot, "Glyph", new Vector2(0f, size * 0.06f), new Vector2(size, size * 0.6f)), "", size * 0.4f, UITheme.Text, TextAlignmentOptions.Center, true);
                slotLevel[i] = -1;
            }

            // Bottom right: control hints.
            RectTransform hints = UIFactory.Rect(c, "Hints", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(560f, 40f), new Vector2(1f, 0f));
            UIFactory.Row(hints, 10f, 0, TextAnchor.MiddleRight);
            Hint(hints, "LMB", "Hook");
            Hint(hints, "RMB", "Zip");
            Hint(hints, "Esc", "Pause");

            // Tower cache marker that slides along the screen edge towards the tower.
            Image marker = UIFactory.Panel(c, "Tower Cache Marker", new Vector2(270f, 64f), UITheme.Panel);
            cacheMarker = (RectTransform)marker.transform;
            Image cacheIcon = UIFactory.Icon(cacheMarker, UISkin.Icon("Gold"), 36f);
            ((RectTransform)cacheIcon.transform).anchoredPosition = new Vector2(-100f, 0f);
            TextMeshProUGUI cacheText = UIFactory.Text(cacheMarker, "Label", "Tower cache ready", 22f, UITheme.Gold);
            UIFactory.Stretch((RectTransform)cacheText.transform);
            ((RectTransform)cacheText.transform).offsetMin = new Vector2(40f, 0f);
            cacheArrow = UIFactory.Centre(cacheMarker, "Arrow", Vector2.zero, new Vector2(40f, 40f));
            UIFactory.Text(cacheArrow, ">", 40f, UITheme.Gold, TextAlignmentOptions.Center, true).fontStyle = FontStyles.Bold;
            // The objective marker (the boss, the portal): over its target on screen, on the screen edge when not.
            objectivePanel = UIFactory.Panel(c, "Objective Marker", new Vector2(190f, 52f), UITheme.Panel);
            objectiveMarker = (RectTransform)objectivePanel.transform;
            objectiveText = UIFactory.Text(objectiveMarker, "Label", "", 24f, UITheme.Text);
            UIFactory.Stretch((RectTransform)objectiveText.transform);
            objectiveText.fontStyle = FontStyles.Bold;
            objectiveArrow = UIFactory.Centre(objectiveMarker, "Arrow", Vector2.zero, new Vector2(36f, 36f));
            UIFactory.Text(objectiveArrow, ">", 36f, UITheme.Text, TextAlignmentOptions.Center, true).fontStyle = FontStyles.Bold;
            cacheProgressFill = UIFactory.Bar(c, "Cache Progress", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(380f, 18f),
                UITheme.Track, UITheme.Gold);
        }

        private static TextMeshProUGUI Counter(Transform parent, string icon, float y, Color color)
        {
            Image image = UIFactory.Icon(parent, UISkin.Icon(icon), 40f);
            ((RectTransform)image.transform).anchoredPosition = new Vector2(-80f, y);
            TextMeshProUGUI text = UIFactory.Text(UIFactory.Centre(parent, icon, new Vector2(24f, y), new Vector2(160f, 44f)), "0", 32f, color, TextAlignmentOptions.Right, true);
            text.fontStyle = FontStyles.Bold;
            return text;
        }

        private static void Hint(Transform parent, string key, string label)
        {
            UIFactory.KeyCap(parent, key, 34f);
            UIFactory.Sized(UIFactory.Text(parent, label, label, 20f, UITheme.TextMuted, TextAlignmentOptions.Left, true), 70f, 34f);
        }

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            root.SetActive(gm == null || (gm.State != RunState.Dead && gm.State != RunState.Cutscene));
            float dt = Time.unscaledDeltaTime;

            if (xp != null)
            {
                UIFactory.SetFill(xpFill, xp.CurrentXP / Mathf.Max(1f, xp.XPToNextLevel));
                if (xp.Level != shownLevel) { shownLevel = xp.Level; levelFlash = 1f; }
                levelText.text = xp.Level.ToString();
            }
            levelFlash = Mathf.MoveTowards(levelFlash, 0f, dt * 1.5f);
            xpGlow.color = new Color(0.4f, 0.95f, 1f, levelFlash);
            levelBadge.localScale = Vector3.one * (1f + levelFlash * 0.25f);

            if (stats != null)
            {
                float health = stats.CurrentHealth / Mathf.Max(1f, stats.MaxHealth);
                UIFactory.SetFill(healthFill, health);
                trail = health < trail ? Mathf.MoveTowards(trail, health, dt * 0.6f) : health;
                UIFactory.SetFill(trailFill, trail);
                healthText.text = $"{stats.CurrentHealth:0} / {stats.MaxHealth:0}";
            }
            if (gm != null)
            {
                timeText.text = RunRecord.FormatTime(gm.RunTime);
                killsText.text = gm.Kills.ToString();
            }
            if (gold != null) goldText.text = gold.Gold.ToString();

            DrawCrosshair(dt);
            DrawAttackPill(dt);

            bool prompt = !string.IsNullOrEmpty(InteractPrompt);
            promptRow.gameObject.SetActive(prompt);
            if (prompt) promptText.text = InteractPrompt;
            InteractPrompt = null;

            float storm = StormWall.Pressure;
            stormText.gameObject.SetActive(storm > 0.15f);
            stormText.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);

            DrawSlots();
            DrawCacheMarker();
            DrawStage(dt);
            DrawObjective();

            float age = Time.unscaledTime - announceTime;
            announce.gameObject.SetActive(age < 2.6f);
            if (age < 2.6f)
            {
                if (announce.text != announceText) announce.text = announceText;
                Color c = announceColor;
                c.a = Mathf.Clamp01((2.6f - age) / 0.6f);
                announce.color = c;
                announce.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Clamp01(1f - age * 4f));
            }
        }

        private void DrawCrosshair(float dt)
        {
            bool inRange = hook != null && hook.HasTargetInRange;
            bool zipTarget = hook != null && hook.HasZipTarget && hook.ZipCharges > 0;
            crosshair.color = inRange ? UITheme.Player : new Color(1f, 1f, 1f, 0.55f);
            crosshair.rectTransform.localScale = Vector3.one * Mathf.MoveTowards(crosshair.rectTransform.localScale.x, inRange ? 1.15f : 0.9f, dt * 3f);
            if (hook == null) return;
            zipRing.fillAmount = hook.ZipCharges > 0 ? 1f : hook.ZipRecharge01;
            zipRing.color = hook.ZipCharges > 0 ? new Color(0.3f, 0.95f, 1f, zipTarget ? 0.9f : 0.45f) : new Color(1f, 1f, 1f, 0.3f);
            for (int i = 0; i < zipPips.Length; i++)
            {
                zipPips[i].gameObject.SetActive(i < hook.ZipMaxCharges);
                zipPips[i].color = i < hook.ZipCharges ? UITheme.Player : new Color(1f, 1f, 1f, 0.25f);
            }
        }

        private void DrawAttackPill(float dt)
        {
            bool attacks = combat != null && combat.AttacksActive;
            if (attacks != lastAttacks) { attackPulse = 1f; lastAttacks = attacks; }
            attackPulse = Mathf.MoveTowards(attackPulse, 0f, dt * 4f);
            attackRect.localScale = Vector3.one * (1f + attackPulse * 0.18f);
            attackText.text = Hookfall.Core.Loc.T(attacks ? "ATTACKS ON" : "ATTACKS OFF");
            attackText.color = attacks ? new Color(0.05f, 0.12f, 0.16f) : UITheme.TextMuted;
            attackBack.color = attacks ? UITheme.Player : new Color(0.12f, 0.15f, 0.2f, 0.85f);
        }

        private void DrawSynergies()
        {
            if (synergy == null && combat != null)
            {
                synergy = combat.GetComponent<SynergySystem>();
                if (synergy != null) synergy.DuoUnlocked += duo => Announce(Hookfall.Core.Loc.F("DUO: {0}", DuoInfo.Name(duo)), UITheme.Gold);
            }
            if (synergy == null) return;
            int hash = 0;
            foreach (School school in SchoolInfo.All) hash = hash * 7 + Mathf.Min(synergy.Count(school), 5);
            if (hash == synergyHash) return;   // rebuild the text only when a count changes
            synergyHash = hash;
            var text = new System.Text.StringBuilder();
            foreach (School school in SchoolInfo.All)
            {
                int count = synergy.Count(school);
                if (count == 0) continue;
                string next = count < SynergySystem.FirstTier ? "/2" : count < SynergySystem.SecondTier ? "/4" : "  " + Hookfall.Core.Loc.T("MAX");
                text.Append($"{SchoolInfo.Colored(school)}  <b>{count}</b>{next}\n");
            }
            foreach (Duo duo in DuoInfo.All)
                if (synergy.HasDuo(duo)) text.Append($"<color=#FFD24D>+ {DuoInfo.Name(duo)}</color>\n");
            synergyText.text = text.ToString();
        }

        private void DrawSlots()
        {
            DrawSynergies();
            for (int i = 0; i < SlotCount; i++)
            {
                ItemBase item = SlotItem(i);
                int level = item != null ? item.Level : 0;
                if (slotItem[i] == item && slotLevel[i] == level) continue;   // redraw only when the slot changes
                slotItem[i] = item;
                slotLevel[i] = level;
                slotIcons[i].sprite = UISkin.Icon(item);
                slotIcons[i].enabled = item != null && slotIcons[i].sprite != null;
                slotGlyphs[i].text = item != null && slotIcons[i].sprite == null ? item.DisplayName.Substring(0, 1) : "";
                slotFrames[i].color = item != null ? Color.Lerp(UITheme.PanelLight, UITheme.ForRarity(item.Rarity), 0.35f) : new Color(0.15f, 0.19f, 0.26f, 0.7f);
                if (slotPips[i] != null) Destroy(slotPips[i].gameObject);
                slotPips[i] = item != null
                    ? UIFactory.Pips(slotFrames[i].transform, item.Level, ItemBase.MaxLevel, i < CombatSystem.MaxItems ? 10f : 7f, UITheme.Player, new Color(1f, 1f, 1f, 0.2f))
                    : null;
            }
        }

        /// <summary>The item shown in a slot: the n-th attack item, or for the upper row the n-th passive.</summary>
        private ItemBase SlotItem(int slot)
        {
            if (combat == null) return null;
            bool passive = slot >= CombatSystem.MaxItems;
            int index = passive ? slot - CombatSystem.MaxItems : slot;
            foreach (ItemBase item in combat.Items)
                if (item.IsPassive == passive && index-- == 0) return item;
            return null;
        }

        /// <summary>The stage, the gold the boss still needs, and the boss's health while it fights.</summary>
        private void DrawStage(float dt)
        {
            StageDirector stage = StageDirector.Instance;
            bool gathering = stage != null && stage.Current == StageDirector.Phase.Gathering;
            stageText.gameObject.SetActive(stage != null);
            bossGoldFill.parent.gameObject.SetActive(gathering);
            if (stage != null)
            {
                string text = Hookfall.Core.Loc.F("STAGE {0}", stage.Stage);
                if (gathering) text += "  ·  " + Hookfall.Core.Loc.F("Boss {0} / {1} gold", Mathf.Min(stage.GoldProgress, stage.GoldTarget), stage.GoldTarget);
                else if (stage.Current == StageDirector.Phase.Portal) text += "  ·  " + Hookfall.Core.Loc.T("The portal is open");
                if (stageText.text != text) stageText.text = text;
                if (gathering) UIFactory.SetFill(bossGoldFill, stage.GoldProgress / (float)Mathf.Max(1, stage.GoldTarget));
            }
            Hookfall.Enemies.Boss boss = stage != null ? stage.Boss : null;
            bool fight = boss != null && boss.Fighting && !boss.IsDead;
            bossBar.gameObject.SetActive(fight);
            if (!fight) { bossTrail = 1f; return; }
            float health = boss.Health / Mathf.Max(1f, boss.MaxHealth);
            UIFactory.SetFill(bossHealthFill, health);
            bossTrail = health < bossTrail ? Mathf.MoveTowards(bossTrail, health, dt * 0.4f) : health;
            UIFactory.SetFill(bossTrailFill, bossTrail);
            string name = Hookfall.Core.Loc.Upper(stage.BossName) + (boss.Raging ? "  ·  " + Hookfall.Core.Loc.T("ENRAGED") : "");
            if (bossNameText.text != name) bossNameText.text = name;
        }

        private void DrawObjective()
        {
            bool show = objective.HasValue && Camera.main != null;
            objectiveMarker.gameObject.SetActive(show);
            if (!show) return;
            if (objectiveText.text != objectiveLabel) objectiveText.text = objectiveLabel;
            objectiveText.color = objectiveColor;
            objectivePanel.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            Camera cam = Camera.main;
            Vector3 local = cam.transform.InverseTransformPoint(objective.Value);
            var canvasRect = (RectTransform)objectiveMarker.parent;
            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(130f, 80f);
            bool onScreen = false;
            if (local.z > 0f)
            {
                Vector3 viewport = cam.WorldToViewportPoint(objective.Value);
                Vector2 p = new Vector2((viewport.x - 0.5f) * canvasRect.rect.width, (viewport.y - 0.5f) * canvasRect.rect.height);
                if (Mathf.Abs(p.x) < half.x && Mathf.Abs(p.y) < half.y)
                {
                    onScreen = true;
                    objectiveMarker.anchoredPosition = p + new Vector2(0f, 60f);
                    objectiveArrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    objectiveArrow.anchoredPosition = new Vector2(0f, -40f);
                }
            }
            if (onScreen) return;
            Vector2 dir = new Vector2(local.x, local.y);
            if (local.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            objectiveMarker.anchoredPosition = dir * scale;
            objectiveArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            objectiveArrow.anchoredPosition = dir * 120f;
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
            Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(160f, 90f);
            float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            bool onScreen = local.z > 0f && Mathf.Abs(local.x / local.z) < 0.9f && Mathf.Abs(local.y / local.z) < 0.5f;
            cacheMarker.anchoredPosition = onScreen ? new Vector2(half.x, 0f) : dir * scale;
            cacheArrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            cacheArrow.anchoredPosition = dir * 155f;
        }
    }
}
