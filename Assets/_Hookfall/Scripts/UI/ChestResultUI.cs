// Hookfall - chest screen: the game pauses and shows 3 choices in the chest's rarity colour, with turning light rays
// behind them; pick one by click or 1, 2, 3, like the level-up screen (GDD 9.1). Choices are new items, levels for
// owned items, or stats once every item is maxed.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin, and turned
// into a 3-choice screen on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class ChestResultUI : MonoBehaviour
    {
        public static ChestResultUI Instance { get; private set; }

        private GameObject root;
        private RectTransform cardRow, rays;
        private Image raysImage;
        private TextMeshProUGUI title;
        private readonly List<GameObject> cards = new List<GameObject>();
        private IReadOnlyList<ChestReward> choices;
        private Action<ChestReward> claim;
        private float shownAt;

        /// <summary>Rolls a chest's choices and shows them; with no screen available the first choice is given at once.</summary>
        public static void Offer(CombatSystem combat, PlayerStats stats, Rarity rarity) => OfferSeries(combat, stats, rarity, 1, null);

        /// <summary>Several picks in a row (the tower cache: three Legendary picks), titled "title · 1/3".</summary>
        public static void OfferSeries(CombatSystem combat, PlayerStats stats, Rarity rarity, int count, string seriesTitle)
        {
            for (int i = 0; i < count; i++)
            {
                List<ChestReward> rolled = combat.RollChest(rarity);
                string title = seriesTitle == null ? null : count > 1 ? $"{seriesTitle}  ·  {i + 1}/{count}" : seriesTitle;
                if (Instance == null || !Instance.Show(rarity, rolled, choice => combat.Claim(choice, stats), title))
                {
                    combat.Claim(rolled[0], stats);
                    continue;
                }
                // The rest follow, one after each pick.
                Instance.pending = count - i - 1;
                Instance.next = () => Instance.ContinueSeries(combat, stats, rarity, count, seriesTitle);
                return;
            }
        }

        private int pending;
        private Action next;

        private void ContinueSeries(CombatSystem combat, PlayerStats stats, Rarity rarity, int count, string seriesTitle)
        {
            int index = count - pending;
            pending--;
            List<ChestReward> rolled = combat.RollChest(rarity);
            string title = $"{seriesTitle}  ·  {index + 1}/{count}";
            if (!Show(rarity, rolled, choice => combat.Claim(choice, stats), title)) combat.Claim(rolled[0], stats);
        }

        private void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.Canvas(transform, "Chest Canvas", 25);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0.02f, 0.04f, 0.08f, 0.72f));
            rays = UIFactory.Centre(canvas.transform, "Rays", new Vector2(0f, 300f), new Vector2(900f, 900f));
            raysImage = UIFactory.Image(rays, Color.white, UISkin.Rays);
            title = UIFactory.Text(UIFactory.Centre(canvas.transform, "Title", new Vector2(0f, 330f), new Vector2(900f, 90f)),
                "CHEST", 64f, UITheme.Gold, TextAlignmentOptions.Center, true);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 12f;
            UIPop.Add(title, 0f, 1.4f);
            UIFactory.Text(UIFactory.Centre(canvas.transform, "Subtitle", new Vector2(0f, 270f), new Vector2(900f, 40f)),
                "Choose one", 26f, UITheme.TextMuted, TextAlignmentOptions.Center, true);
            cardRow = UIFactory.Centre(canvas.transform, "Cards", new Vector2(0f, -60f), new Vector2(1780f, 560f));   // room for 4 (Fortune set)
            UIFactory.Row(cardRow, 44f, 0);
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Pauses the game and shows the choices; onClaim gets the picked one. Returns false if another screen is open.</summary>
        public bool Show(Rarity rarity, IReadOnlyList<ChestReward> options, Action<ChestReward> onClaim, string titleText = null)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.OpenModal(RunState.ChestResult)) return false;

            choices = options;
            claim = onClaim;
            foreach (GameObject card in cards) Destroy(card);
            cards.Clear();

            Color color = UITheme.ForRarity(rarity);
            raysImage.color = new Color(color.r, color.g, color.b, 0.3f);
            title.text = titleText ?? Loc.T(rarity.ToString().ToUpperInvariant() + " CHEST");
            title.color = color;
            for (int i = 0; i < options.Count; i++)
                cards.Add(BuildCard(i, options[i], options[i].Item != null ? UITheme.ForRarity(options[i].Item.Rarity) : color));

            shownAt = Time.unscaledTime;
            root.SetActive(true);
            title.gameObject.SetActive(false);
            title.gameObject.SetActive(true);   // replays the title pop
            return true;
        }

        private GameObject BuildCard(int index, ChestReward choice, Color color)
        {
            string name, tag, value, detail;
            Sprite icon;
            if (choice.Item == null)
            {
                var stats = FindAnyObjectByType<PlayerStats>();
                name = Loc.T(PlayerStats.NameOf(choice.FallbackStat));
                tag = Loc.T("STAT");
                value = stats != null ? stats.PickText(choice.FallbackStat) : name;
                detail = Loc.T("Every item is maxed");
                icon = UISkin.Icon(choice.FallbackStat);
            }
            else
            {
                name = choice.Item.DisplayName;
                icon = UISkin.Icon(choice.Item);
                detail = choice.Item.Summary;
                string kind = Loc.T(choice.Item.Rarity.ToString().ToUpperInvariant() + (choice.Item.IsPassive ? " PASSIVE" : " ITEM"));
                if (choice.IsNewItem)
                {
                    tag = Loc.F("NEW  ·  {0}", kind);
                    value = SchoolInfo.Line(choice.Item.Schools) + "\n" + string.Join("\n", choice.Item.StatLines());
                }
                else
                {
                    tag = Loc.F("LEVEL {0} → {1}", choice.Item.Level, Mathf.Min(choice.Item.Level + choice.LevelsGained, ItemBase.MaxLevel));
                    value = SchoolInfo.Line(choice.Item.Schools) + "\n" + choice.Item.DescribeNextLevel();
                }
            }
            int captured = index;
            return UIFactory.ChoiceCard(cardRow, index, color, icon, name, tag, value, detail, () => Pick(captured)).gameObject;
        }

        private void Update()
        {
            if (!root.activeSelf) return;
            rays.Rotate(0f, 0f, -18f * Time.unscaledDeltaTime);
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Pick(0);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Pick(1);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Pick(2);
        }

        private void Pick(int index)
        {
            // A short guard so a click or key held from before the chest opened does not pick for the player.
            if (!root.activeSelf || choices == null || index >= choices.Count || Time.unscaledTime - shownAt < 0.3f) return;
            root.SetActive(false);
            ChestReward choice = choices[index];
            choices = null;
            claim?.Invoke(choice);
            GameManager.Instance?.CloseModal();
            if (pending > 0 && next != null) next();
            else next = null;
        }
    }
}
