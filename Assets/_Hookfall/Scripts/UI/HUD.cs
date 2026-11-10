// Hookfall - the in-run HUD (GDD 9). Screen edges only, the centre stays free for the horde:
//   top edge      XP bar; top left the Warden's portrait with the level, health with a damage trail, shield and revive;
//   top centre    watch time, the stage and its boss meter (gold), the boss's health while it fights, announcements;
//   top right     gold and kills, the build's schools and duos (HudBuild);
//   centre        crosshair with the Zip ring, the ATTACKS ON/OFF pill, the interact prompt, the charge bar,
//                 red wedges towards whatever hit the Warden (HudDamageRing);
//   bottom        item and passive slots (left), jump, dash and Zip with their keys (centre), key hints (right);
//   world         badges for chests, the tower cache and the objective, on the screen edge when out of view (HudMarkers).
// Built in uGUI from code.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as an IMGUI placeholder, rewritten on 2026-11-07 in
// uGUI and again the same day with the UI skin and icons; the stage, the boss bar, the boss's health and an objective
// marker were added on 2026-11-08; rebuilt on 2026-11-10 with the new skin, fonts, portrait, abilities, world markers
// and damage wedges. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
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

        /// <summary>Set with InteractPrompt when the line is a status, not something a key does (the tower cache opening).</summary>
        public static bool InteractWithoutKey;

        private static Vector3? objective;
        private static string objectiveLabel, objectiveIcon = "Boss";
        private static Color objectiveColor;

        /// <summary>Where the run goes next (the boss, the portal); null hides the marker. icon names its badge's icon.</summary>
        public static void SetObjective(Vector3? position, string label, Color color, string icon = "Boss")
        {
            objective = position;
            objectiveLabel = label;
            objectiveColor = color;
            objectiveIcon = icon;
        }

        private static readonly Vector2 TopLeft = new Vector2(0f, 1f), TopCentre = new Vector2(0.5f, 1f), TopRight = new Vector2(1f, 1f),
            BottomLeft = Vector2.zero, BottomCentre = new Vector2(0.5f, 0f), BottomRight = new Vector2(1f, 0f), Middle = new Vector2(0.5f, 0.5f);

        private const int SlotCount = CombatSystem.MaxItems + CombatSystem.MaxPassives;   // attack slots, then passive slots

        private GameObject root;
        private PlayerController controller;
        private SynergySystem synergy;
        private ChestSpawner chests;
        private HudMarkers markers;
        private HudBuild build;
        private HudDamageRing damageRing;

        private UIBar xpBar, healthBar, bossGoldBar, bossBar, chargeBar;
        private Image portrait, xpGlow, crosshair, zipRing, attackBack;
        private RectTransform levelBadge, attackRect, promptRow, statusRow, bossBlock, bossMeter;
        private TextMeshProUGUI levelText, healthText, timeText, goldText, killsText, attackText, promptText, stormText, announce;
        private TextMeshProUGUI stageText, bossGoldText, bossNameText, shieldText, reviveText;
        private GameObject shieldChip, reviveChip, promptKey;
        private readonly Image[] zipPips = new Image[3];

        private readonly Image[] slotIcons = new Image[SlotCount];
        private readonly Image[] slotFrames = new Image[SlotCount];
        private readonly RectTransform[] slotRects = new RectTransform[SlotCount];
        private readonly TextMeshProUGUI[] slotGlyphs = new TextMeshProUGUI[SlotCount];
        private readonly RectTransform[] slotPips = new RectTransform[SlotCount];
        private readonly ItemBase[] slotItem = new ItemBase[SlotCount];
        private readonly int[] slotLevel = new int[SlotCount];
        private readonly float[] slotPunch = new float[SlotCount];

        // Jump, dash, Zip: an icon each, lit while a charge is left.
        private Image jumpIcon, dashIcon, zipIcon, zipCooldown;
        private readonly List<Image> jumpPips = new List<Image>(), dashPips = new List<Image>();

        private int shownLevel = 1, shownGold = -1, shownKills = -1;
        private float levelFlash, attackPulse, goldPunch, killsPunch;
        private bool lastAttacks;
        private bool duoHooked;

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "HUD Canvas", 0);
            root = canvas.gameObject;
            Transform c = canvas.transform;
            if (hook != null) controller = hook.GetComponent<PlayerController>();
            if (combat != null) synergy = combat.GetComponent<SynergySystem>();

            // Soft shades so the edge rows read over a bright sky.
            RectTransform topShade = UIFactory.Rect(c, "Top Shade", TopLeft, TopRight, Vector2.zero, new Vector2(0f, 170f), new Vector2(0.5f, 1f));
            UIFactory.Image(topShade, new Color(0.02f, 0.04f, 0.1f, 0.42f), UISkin.ShadeDown);
            RectTransform bottomShade = UIFactory.Rect(c, "Bottom Shade", BottomLeft, BottomRight, new Vector2(0f, 95f), new Vector2(0f, 190f));
            UIFactory.Image(bottomShade, new Color(0.02f, 0.04f, 0.1f, 0.36f), UISkin.ShadeDown);
            bottomShade.localRotation = Quaternion.Euler(0f, 0f, 180f);   // the shade sprite is dark at its top

            markers = new HudMarkers(c);
            BuildVitals(c);
            BuildCentreTop(c);
            BuildCounters(c);
            build = new HudBuild(c, new Vector2(-26f, -112f));
            BuildCentre(c);
            BuildSlots(c);
            BuildAbilities(c);
            BuildHints(c);
            damageRing = new HudDamageRing(c);

            announce = UIFactory.Title(UIFactory.Rect(c, "Announce", TopCentre, TopCentre, new Vector2(0f, -262f), new Vector2(1500f, 84f)),
                "Text", "", 54f, UITheme.Gold);
            UIFactory.Stretch((RectTransform)announce.transform);
            announce.gameObject.SetActive(false);
        }

        private void Start()
        {
            chests = FindAnyObjectByType<ChestSpawner>();
            Sprite face = UISkin.Icon(CharacterRoster.ActiveId);
            portrait.sprite = face;
            portrait.enabled = face != null;
            if (stats != null) stats.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (stats != null) stats.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount)
        {
            if (stats.LastDamageSource.HasValue && Camera.main != null) damageRing.Hit(stats.LastDamageSource.Value, Camera.main.transform);
        }

        // ---------------------------------------------------------------- building

        /// <summary>Top left: portrait with the level badge, the XP bar along the top edge, health, shield and revive.</summary>
        private void BuildVitals(Transform c)
        {
            xpBar = UIBar.Create(c, "XP Bar", TopLeft, Vector2.zero, Vector2.zero, UITheme.Xp, false, new Vector2(0.5f, 1f));
            RectTransform xpRect = xpBar.Rect;
            xpRect.anchorMin = TopLeft;
            xpRect.anchorMax = TopRight;
            xpRect.offsetMin = new Vector2(150f, -26f);   // starts right of the portrait
            xpRect.offsetMax = new Vector2(-26f, -10f);
            xpGlow = UIFactory.Image(UIFactory.Rect(xpRect, "Glow", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(60f, 50f)), new Color(0.4f, 0.95f, 1f, 0f), UISkin.GlowFrame);

            RectTransform medal = UIFactory.Rect(c, "Portrait", TopLeft, TopLeft, new Vector2(26f, -22f), new Vector2(112f, 112f), TopLeft);
            UIFactory.Image(medal, new Color(0.07f, 0.11f, 0.2f, 0.95f), UISkin.Circle);
            portrait = UIFactory.Image(UIFactory.Centre(medal, "Face", new Vector2(0f, -4f), new Vector2(104f, 104f)), Color.white);
            portrait.preserveAspect = true;
            UIFactory.Image(UIFactory.Fill(medal, "Frame"), UITheme.Player, UISkin.CircleFrame);
            levelBadge = UIFactory.Rect(medal, "Level", BottomRight, BottomRight, new Vector2(4f, -4f), new Vector2(58f, 58f));
            UIFactory.Image(UIFactory.Fill(levelBadge, "Fill"), Color.white, UISkin.DiamondFill);
            UIFactory.Image(UIFactory.Fill(levelBadge, "Frame"), UITheme.Gold, UISkin.Diamond);
            levelText = UIFactory.Text(levelBadge, "Value", "1", 25f, UITheme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Stretch((RectTransform)levelText.transform);
            levelText.fontStyle = FontStyles.Bold;

            healthBar = UIBar.Create(c, "Health", TopLeft, new Vector2(150f, -36f), new Vector2(430f, 34f), UITheme.Health, true, TopLeft);
            Image heart = UIFactory.Icon(healthBar.transform, UISkin.Icon("Health"), 30f);
            var heartRect = (RectTransform)heart.transform;
            heartRect.anchorMin = heartRect.anchorMax = new Vector2(0f, 0.5f);
            heartRect.anchoredPosition = new Vector2(22f, 0f);
            healthText = UIFactory.Text(healthBar.transform, "Value", "100 / 100", 21f, UITheme.Text, TextAlignmentOptions.Center, true);
            UIFactory.Stretch((RectTransform)healthText.transform);
            healthText.fontStyle = FontStyles.Bold;

            // Under the bar: what stands between the Warden and death besides health.
            statusRow = UIFactory.Rect(c, "Status", TopLeft, TopLeft, new Vector2(152f, -76f), new Vector2(430f, 32f), TopLeft);
            HorizontalLayoutGroup row = UIFactory.Row(statusRow, 8f, 0, TextAnchor.MiddleLeft);
            row.childForceExpandHeight = false;
            RectTransform shield = UIFactory.Chip(statusRow, UISkin.Icon("WardShell"), "1", UITheme.Player, 32f, 20f);
            shieldChip = shield.gameObject;
            shieldText = shield.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform revive = UIFactory.Chip(statusRow, UISkin.Icon("PhoenixFeather"), "1", UITheme.Gold, 32f, 20f);
            reviveChip = revive.gameObject;
            reviveText = revive.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>Top centre: the watch time, the stage with its boss meter, and the boss's health while it fights.</summary>
        private void BuildCentreTop(Transform c)
        {
            Image time = UIFactory.Plaque(c, "Time", TopCentre, new Vector2(0f, -34f), new Vector2(236f, 66f), TopCentre);
            Image hourglass = UIFactory.Icon(time.transform, UISkin.Icon("Timer"), 40f);
            ((RectTransform)hourglass.transform).anchoredPosition = new Vector2(-78f, 0f);
            timeText = UIFactory.Text(time.transform, "Value", "00:00", 42f, UITheme.Text);
            var timeRect = (RectTransform)timeText.transform;
            UIFactory.Stretch(timeRect);
            timeRect.offsetMin = new Vector2(40f, 0f);
            timeText.fontStyle = FontStyles.Bold;
            timeText.fontSharedMaterial = UISkin.BodyShadow;

            stageText = UIFactory.Title(UIFactory.Rect(c, "Stage", TopCentre, TopCentre, new Vector2(0f, -118f), new Vector2(700f, 30f)), "Text", "", 21f, UITheme.Text);
            UIFactory.Stretch((RectTransform)stageText.transform);

            // The boss meter: gold in, the boss at the end of it.
            bossMeter = UIFactory.Rect(c, "Boss Meter", TopCentre, TopCentre, new Vector2(0f, -150f), new Vector2(420f, 34f));
            bossGoldBar = UIBar.Create(bossMeter, "Bar", Middle, Vector2.zero, new Vector2(330f, 16f), UITheme.Enemy);
            Image coin = UIFactory.Icon(bossMeter, UISkin.Icon("Gold"), 30f);
            ((RectTransform)coin.transform).anchoredPosition = new Vector2(-186f, 0f);
            Image skull = UIFactory.Icon(bossMeter, UISkin.Icon("Boss"), 38f);
            ((RectTransform)skull.transform).anchoredPosition = new Vector2(190f, 0f);
            bossGoldText = UIFactory.Text(UIFactory.Centre(bossMeter, "Value", new Vector2(0f, -20f), new Vector2(330f, 22f)), "", 17f, UITheme.Text, TextAlignmentOptions.Center, true);
            bossGoldText.fontStyle = FontStyles.Bold;

            // The boss's health across the top while it fights.
            bossBlock = UIFactory.Rect(c, "Boss", TopCentre, TopCentre, new Vector2(0f, -150f), new Vector2(900f, 70f), TopCentre);
            bossNameText = UIFactory.Title(UIFactory.Rect(bossBlock, "Name", TopCentre, TopCentre, new Vector2(0f, -18f), new Vector2(900f, 40f)), "Text", "", 30f,
                new Color(1f, 0.82f, 0.96f), TextAlignmentOptions.Center, true, true);
            UIFactory.Stretch((RectTransform)bossNameText.transform);
            bossNameText.characterSpacing = 10f;
            bossBar = UIBar.Create(bossBlock, "Health", TopCentre, new Vector2(0f, -40f), new Vector2(860f, 26f), new Color(0.9f, 0.24f, 0.66f), true, TopCentre);
            bossBar.trailSpeed = 0.4f;
            foreach (float mark in new[] { 0.7f, 0.35f })   // where it calls its escort
                UIFactory.Image(UIFactory.Rect(bossBar.transform, "Mark", new Vector2(mark, 0f), new Vector2(mark, 1f), Vector2.zero, new Vector2(3f, 0f)), new Color(1f, 1f, 1f, 0.7f));
            bossBlock.gameObject.SetActive(false);

            stormText = UIFactory.Text(UIFactory.Rect(c, "Storm", TopCentre, TopCentre, new Vector2(0f, -330f), new Vector2(900f, 40f)),
                "The storm pushes you back", 28f, new Color(0.7f, 0.82f, 1f), TextAlignmentOptions.Center, true);
            stormText.fontStyle = FontStyles.Bold;
        }

        /// <summary>Top right: gold and kills.</summary>
        private void BuildCounters(Transform c)
        {
            Image counters = UIFactory.Plaque(c, "Counters", TopRight, new Vector2(-26f, -34f), new Vector2(330f, 66f), TopRight);
            goldText = Counter(counters.transform, "Gold", -128f, UITheme.Gold);
            killsText = Counter(counters.transform, "Kills", 30f, UITheme.Text);
        }

        private static TextMeshProUGUI Counter(Transform parent, string icon, float x, Color color)
        {
            Image image = UIFactory.Icon(parent, UISkin.Icon(icon), 38f);
            ((RectTransform)image.transform).anchoredPosition = new Vector2(x, 0f);
            TextMeshProUGUI text = UIFactory.Text(UIFactory.Centre(parent, icon, new Vector2(x + 76f, 0f), new Vector2(104f, 44f)), "0", 32f, color, TextAlignmentOptions.Left);
            text.fontStyle = FontStyles.Bold;
            text.fontSharedMaterial = UISkin.BodyShadow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UIFactory.FitText(text, 18f);
            return text;
        }

        /// <summary>Centre: crosshair, Zip ring and charges, attack pill, prompt, charge bar.</summary>
        private void BuildCentre(Transform c)
        {
            RectTransform ring = UIFactory.Centre(c, "Zip Ring", Vector2.zero, new Vector2(96f, 96f));
            zipRing = UIFactory.Image(ring, UITheme.Player, UISkin.Ring);
            zipRing.type = Image.Type.Filled;
            zipRing.fillMethod = Image.FillMethod.Radial360;
            zipRing.fillOrigin = (int)Image.Origin360.Top;
            zipRing.fillClockwise = true;
            RectTransform cross = UIFactory.Centre(c, "Crosshair", Vector2.zero, new Vector2(52f, 52f));
            crosshair = UIFactory.Image(cross, Color.white, UISkin.Crosshair);
            for (int i = 0; i < zipPips.Length; i++)
            {
                RectTransform pip = UIFactory.Centre(c, "Zip Charge", new Vector2((i - 1) * 15f, -62f), new Vector2(11f, 11f));
                zipPips[i] = UIFactory.Image(pip, UITheme.Player, UISkin.Pip);
            }
            attackRect = UIFactory.Centre(c, "Attack Pill", new Vector2(0f, -98f), new Vector2(206f, 34f));
            attackBack = UIFactory.Image(attackRect, UITheme.PanelDeep, UISkin.Pill);
            attackText = UIFactory.Text(attackRect, "Label", "ATTACKS OFF", 18f, UITheme.TextMuted);   // a child: the pill already has an Image
            UIFactory.Stretch((RectTransform)attackText.transform);
            attackText.fontStyle = FontStyles.Bold;
            attackText.characterSpacing = 4f;

            promptRow = UIFactory.Centre(c, "Prompt", new Vector2(0f, -164f), new Vector2(760f, 50f));
            UIFactory.Row(promptRow, 14f, 0);
            promptKey = UIFactory.KeyCap(promptRow, "E").gameObject;
            promptText = UIFactory.Text(promptRow, "Text", "", 26f, UITheme.Gold, TextAlignmentOptions.Left, true);
            promptText.fontStyle = FontStyles.Bold;
            promptText.textWrappingMode = TextWrappingModes.NoWrap;

            chargeBar = UIBar.Create(c, "Charge", Middle, new Vector2(0f, -214f), new Vector2(380f, 18f), UITheme.Gold);
        }

        /// <summary>Bottom left: six item slots with icons and level pips, and six smaller passive slots above them.</summary>
        private void BuildSlots(Transform c)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                bool passive = i >= CombatSystem.MaxItems;
                int column = passive ? i - CombatSystem.MaxItems : i;
                float size = passive ? 62f : 88f;
                Vector2 centre = passive ? new Vector2(26f + size / 2f + column * 68f, 152f) : new Vector2(26f + size / 2f + column * 94f, 26f + size / 2f);
                RectTransform slot = UIFactory.Rect(c, $"Slot {i + 1}", BottomLeft, BottomLeft, centre, new Vector2(size, size));
                slotRects[i] = slot;
                UIFactory.Image(slot, Color.white, UISkin.Slot);
                slotFrames[i] = UIFactory.Image(UIFactory.Fill(slot, "Frame"), new Color(1f, 1f, 1f, 0.2f), UISkin.SlotFrame);
                RectTransform iconRect = UIFactory.Centre(slot, "Icon", new Vector2(0f, size * 0.07f), new Vector2(size * 0.68f, size * 0.68f));
                slotIcons[i] = UIFactory.Image(iconRect, Color.white);
                slotIcons[i].preserveAspect = true;
                slotIcons[i].enabled = false;
                // Items with no icon show their first letter instead.
                slotGlyphs[i] = UIFactory.Text(UIFactory.Centre(slot, "Glyph", new Vector2(0f, size * 0.06f), new Vector2(size, size * 0.6f)), "", size * 0.4f, UITheme.Text, TextAlignmentOptions.Center, true);
                slotLevel[i] = -1;
            }
        }

        /// <summary>Bottom centre: what the Warden can still do in this flight, each with its key.</summary>
        private void BuildAbilities(Transform c)
        {
            RectTransform row = UIFactory.Rect(c, "Abilities", BottomCentre, BottomCentre, new Vector2(0f, 26f), new Vector2(520f, 96f), BottomCentre);
            jumpIcon = Ability(row, -150f, "Jump", "Space", jumpPips, 3);
            dashIcon = Ability(row, 0f, "Dash", "Shift", dashPips, 3);
            zipIcon = Ability(row, 150f, "Zip", "RMB", null, 0);
            zipCooldown = UIFactory.Image(UIFactory.Centre(zipIcon.transform.parent, "Cooldown", Vector2.zero, new Vector2(66f, 66f)), new Color(0.02f, 0.04f, 0.1f, 0.78f), UISkin.Circle);
            zipCooldown.type = Image.Type.Filled;
            zipCooldown.fillMethod = Image.FillMethod.Radial360;
            zipCooldown.fillOrigin = (int)Image.Origin360.Top;
            zipCooldown.fillClockwise = false;
        }

        private static Image Ability(Transform row, float x, string icon, string key, List<Image> pips, int maxPips)
        {
            RectTransform holder = UIFactory.Centre(row, icon, new Vector2(x, 14f), new Vector2(66f, 66f));
            UIFactory.Image(holder, new Color(0.05f, 0.08f, 0.16f, 0.85f), UISkin.Circle);
            Image image = UIFactory.Image(UIFactory.Centre(holder, "Icon", Vector2.zero, new Vector2(44f, 44f)), Color.white, UISkin.Icon(icon));
            image.preserveAspect = true;
            UIFactory.Image(UIFactory.Fill(holder, "Frame"), new Color(1f, 1f, 1f, 0.35f), UISkin.CircleFrame);
            RectTransform cap = UIFactory.KeyCap(holder, key, 26f);
            cap.anchoredPosition = new Vector2(0f, -44f);
            if (pips != null)
                for (int i = 0; i < maxPips; i++)
                {
                    RectTransform pip = UIFactory.Centre(holder, "Charge", new Vector2(42f, 18f - i * 15f), new Vector2(12f, 12f));
                    pips.Add(UIFactory.Image(pip, UITheme.Player, UISkin.Pip));
                }
            return image;
        }

        /// <summary>Bottom right: the keys that are not shown anywhere else.</summary>
        private static void BuildHints(Transform c)
        {
            RectTransform hints = UIFactory.Rect(c, "Hints", BottomRight, BottomRight, new Vector2(-26f, 26f), new Vector2(520f, 36f), BottomRight);
            HorizontalLayoutGroup row = UIFactory.Row(hints, 10f, 0, TextAnchor.MiddleRight);
            row.childForceExpandHeight = false;
            Hint(hints, "LMB", "Hook");
            Hint(hints, "E", "Open");
            Hint(hints, "Esc", "Pause");
        }

        private static void Hint(Transform parent, string key, string label)
        {
            UIFactory.KeyCap(parent, key, 32f);
            TextMeshProUGUI text = UIFactory.Text(parent, label, label, 19f, UITheme.TextMuted, TextAlignmentOptions.Left, true);
            text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        // ---------------------------------------------------------------- drawing

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            root.SetActive(gm == null || (gm.State != RunState.Dead && gm.State != RunState.Cutscene));
            float dt = Time.unscaledDeltaTime;

            if (xp != null)
            {
                xpBar.Set(xp.CurrentXP / Mathf.Max(1f, xp.XPToNextLevel));
                if (xp.Level != shownLevel) { shownLevel = xp.Level; levelFlash = 1f; }
                levelText.text = xp.Level.ToString();
            }
            levelFlash = Mathf.MoveTowards(levelFlash, 0f, dt * 1.5f);
            xpGlow.color = new Color(0.4f, 0.95f, 1f, levelFlash);
            levelBadge.localScale = Vector3.one * (1f + levelFlash * 0.35f);

            if (stats != null)
            {
                float health = stats.CurrentHealth / Mathf.Max(1f, stats.MaxHealth);
                healthBar.Set(health);
                healthBar.FillImage.color = health < 0.3f ? Color.Lerp(UITheme.Health, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 9f)) : UITheme.Health;
                healthText.text = $"{stats.CurrentHealth:0} / {stats.MaxHealth:0}";
                if (shieldChip.activeSelf != stats.ShieldCharges > 0) shieldChip.SetActive(stats.ShieldCharges > 0);
                if (stats.ShieldCharges > 0) shieldText.text = stats.ShieldCharges.ToString();
                if (reviveChip.activeSelf != stats.Revives > 0) reviveChip.SetActive(stats.Revives > 0);
                if (stats.Revives > 0) reviveText.text = stats.Revives.ToString();
            }
            if (gm != null)
            {
                timeText.text = RunRecord.FormatTime(gm.RunTime);
                if (gm.Kills != shownKills) { if (shownKills >= 0) killsPunch = 1f; shownKills = gm.Kills; killsText.text = gm.Kills.ToString(); }
            }
            if (gold != null && gold.Gold != shownGold) { if (shownGold >= 0 && gold.Gold > shownGold) goldPunch = 1f; shownGold = gold.Gold; goldText.text = gold.Gold.ToString(); }
            goldPunch = Mathf.MoveTowards(goldPunch, 0f, dt * 5f);
            killsPunch = Mathf.MoveTowards(killsPunch, 0f, dt * 5f);
            goldText.transform.localScale = Vector3.one * (1f + goldPunch * 0.22f);
            killsText.transform.localScale = Vector3.one * (1f + killsPunch * 0.16f);

            DrawCrosshair(dt);
            DrawAttackPill(dt);
            DrawAbilities();

            bool prompt = !string.IsNullOrEmpty(InteractPrompt);
            promptRow.gameObject.SetActive(prompt);
            if (prompt)
            {
                promptText.text = InteractPrompt;
                if (promptKey.activeSelf == InteractWithoutKey) promptKey.SetActive(!InteractWithoutKey);
            }
            InteractPrompt = null;
            InteractWithoutKey = false;

            chargeBar.gameObject.SetActive(TowerCacheProgress >= 0f);
            if (TowerCacheProgress >= 0f) chargeBar.Set(TowerCacheProgress);

            float storm = StormWall.Pressure;
            stormText.gameObject.SetActive(storm > 0.15f);
            stormText.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);

            DrawSlots(dt);
            DrawStage();
            DrawMarkers();
            damageRing.Tick(dt);

            float age = Time.unscaledTime - announceTime;
            announce.gameObject.SetActive(age < 2.6f);
            if (age < 2.6f)
            {
                if (announce.text != announceText) announce.text = announceText;
                Color c = announceColor;
                c.a = Mathf.Clamp01((2.6f - age) / 0.6f);
                announce.color = c;
                announce.transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Clamp01(1f - age * 4f));
            }
        }

        private void DrawCrosshair(float dt)
        {
            bool inRange = hook != null && hook.HasTargetInRange;
            bool zipTarget = hook != null && hook.HasZipTarget && hook.ZipCharges > 0;
            crosshair.color = inRange ? UITheme.Player : new Color(1f, 1f, 1f, 0.6f);
            crosshair.rectTransform.localScale = Vector3.one * Mathf.MoveTowards(crosshair.rectTransform.localScale.x, inRange ? 1.15f : 0.9f, dt * 3f);
            if (hook == null) return;
            zipRing.fillAmount = hook.ZipCharges > 0 ? 1f : hook.ZipRecharge01;
            zipRing.color = hook.ZipCharges > 0 ? new Color(0.3f, 0.95f, 1f, zipTarget ? 0.9f : 0.4f) : new Color(1f, 1f, 1f, 0.3f);
            for (int i = 0; i < zipPips.Length; i++)
            {
                zipPips[i].gameObject.SetActive(hook.ZipMaxCharges > 1 && i < hook.ZipMaxCharges);
                zipPips[i].color = i < hook.ZipCharges ? UITheme.Player : new Color(1f, 1f, 1f, 0.25f);
            }
        }

        private void DrawAttackPill(float dt)
        {
            bool attacks = combat != null && combat.AttacksActive;
            if (attacks != lastAttacks) { attackPulse = 1f; lastAttacks = attacks; }
            attackPulse = Mathf.MoveTowards(attackPulse, 0f, dt * 4f);
            attackRect.localScale = Vector3.one * (1f + attackPulse * 0.18f);
            attackText.text = Loc.T(attacks ? "ATTACKS ON" : "ATTACKS OFF");
            attackText.color = attacks ? UITheme.TextDark : UITheme.TextMuted;
            attackBack.color = attacks ? UITheme.Player : UITheme.PanelDeep;
        }

        private void DrawAbilities()
        {
            if (controller != null)
            {
                Charges(jumpIcon, jumpPips, controller.AirJumpsLeft, controller.AirJumps);
                Charges(dashIcon, dashPips, controller.DashesLeft, controller.AirDashes);
            }
            if (hook == null) return;
            bool ready = hook.ZipCharges > 0;
            zipIcon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            zipCooldown.fillAmount = ready ? 0f : 1f - hook.ZipRecharge01;
        }

        private static void Charges(Image icon, List<Image> pips, int left, int max)
        {
            icon.color = left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            for (int i = 0; i < pips.Count; i++)
            {
                bool shown = max > 1 && i < max;
                if (pips[i].gameObject.activeSelf != shown) pips[i].gameObject.SetActive(shown);
                if (shown) pips[i].color = i < left ? UITheme.Player : new Color(1f, 1f, 1f, 0.22f);
            }
        }

        private void DrawSlots(float dt)
        {
            if (synergy != null && !duoHooked)
            {
                duoHooked = true;
                synergy.DuoUnlocked += duo => Announce(Loc.F("DUO: {0}", DuoInfo.Name(duo)), UITheme.Gold);
            }
            build.Refresh(synergy);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotPunch[i] > 0f)
                {
                    slotPunch[i] = Mathf.MoveTowards(slotPunch[i], 0f, dt * 4f);
                    slotRects[i].localScale = Vector3.one * (1f + slotPunch[i] * 0.3f);
                }
                ItemBase item = SlotItem(i);
                int level = item != null ? item.Level : 0;
                if (slotItem[i] == item && slotLevel[i] == level) continue;   // redraw only when the slot changes
                if (slotLevel[i] >= 0 && item != null) slotPunch[i] = 1f;     // a new item or a new level: a little jump
                slotItem[i] = item;
                slotLevel[i] = level;
                slotIcons[i].sprite = UISkin.Icon(item);
                slotIcons[i].enabled = item != null && slotIcons[i].sprite != null;
                slotGlyphs[i].text = item != null && slotIcons[i].sprite == null ? item.DisplayName.Substring(0, 1) : "";
                slotFrames[i].color = item != null ? UITheme.ForRarity(item.Rarity) : new Color(1f, 1f, 1f, 0.2f);
                if (slotPips[i] != null) Destroy(slotPips[i].gameObject);
                slotPips[i] = item != null
                    ? UIFactory.Pips(slotRects[i], item.Level, ItemBase.MaxLevel, i < CombatSystem.MaxItems ? 11f : 8f, UITheme.Player, new Color(1f, 1f, 1f, 0.22f))
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
        private void DrawStage()
        {
            StageDirector stage = StageDirector.Instance;
            bool gathering = stage != null && stage.Current == StageDirector.Phase.Gathering;
            Hookfall.Enemies.Boss boss = stage != null ? stage.Boss : null;
            bool fight = boss != null && boss.Fighting && !boss.IsDead;
            stageText.gameObject.SetActive(stage != null && !fight);
            bossMeter.gameObject.SetActive(gathering);
            if (bossBlock.gameObject.activeSelf != fight)
            {
                bossBlock.gameObject.SetActive(fight);
                if (fight) bossBar.Set(1f, true);
            }
            if (stage == null) return;

            string text = Loc.F("STAGE {0}", stage.Stage);
            if (stage.Current == StageDirector.Phase.Portal) text += "  ·  " + Loc.T("The portal is open");
            if (stageText.text != text) stageText.text = text;
            if (gathering)
            {
                bossGoldBar.Set(stage.GoldProgress / (float)Mathf.Max(1, stage.GoldTarget));
                string goldLine = Mathf.Min(stage.GoldProgress, stage.GoldTarget) + " / " + stage.GoldTarget;
                if (bossGoldText.text != goldLine) bossGoldText.text = goldLine;
            }
            if (!fight) return;
            bossBar.Set(boss.Health / Mathf.Max(1f, boss.MaxHealth));
            string name = Loc.Upper(stage.BossName) + (boss.Raging ? "  ·  " + Loc.T("ENRAGED") : "");
            if (bossNameText.text != name) bossNameText.text = name;
        }

        /// <summary>Badges for the nearest chests, the tower cache and the objective.</summary>
        private void DrawMarkers()
        {
            markers.Begin();
            Camera cam = Camera.main;
            if (cam != null && chests != null)
            {
                // The three nearest chests; dim while the Warden cannot pay for one.
                Vector3 p = cam.transform.position;
                Chest a = null, b = null, third = null;
                float da = float.MaxValue, db = float.MaxValue, dc = float.MaxValue;
                foreach (Chest chest in chests.Chests)
                {
                    if (chest == null) continue;
                    float d = (chest.transform.position - p).sqrMagnitude;
                    if (d < da) { third = b; dc = db; b = a; db = da; a = chest; da = d; }
                    else if (d < db) { third = b; dc = db; b = chest; db = d; }
                    else if (d < dc) { third = chest; dc = d; }
                }
                float alpha = chests.CanAfford ? 1f : 0.5f;
                Sprite icon = UISkin.Icon("Chest");
                if (a != null) markers.Show(a.transform.position + Vector3.up * 2f, icon, UITheme.Gold, 14f, null, alpha);
                if (b != null) markers.Show(b.transform.position + Vector3.up * 2f, icon, UITheme.Gold, 14f, null, alpha * 0.8f);
                if (third != null) markers.Show(third.transform.position + Vector3.up * 2f, icon, UITheme.Gold, 14f, null, alpha * 0.6f);
            }
            if (TowerCacheMarker.HasValue)
                markers.Show(TowerCacheMarker.Value, UISkin.Icon("Tower"), UITheme.ForRarity(Rarity.Legendary), 10f, Loc.T("Tower cache ready"));
            if (objective.HasValue)
                markers.Show(objective.Value, UISkin.Icon(objectiveIcon), objectiveColor, 0f, objectiveLabel);
            markers.End();
        }
    }
}
