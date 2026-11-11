// Hookfall - chest screen: the game pauses and shows 3 choices in the chest's rarity colour, with turning light rays
// behind them; pick one by click or 1, 2, 3, like the level-up screen (GDD 9.1). Choices are new items, levels for
// owned items, or stats once every item is maxed. A new item picked with its slots full opens a second step: choose
// the owned item it replaces (or go back).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin, and turned
// into a 3-choice screen on 2026-11-08, with the replace step the same day. Prompts and changes are recorded in Docs/PromptLog.md.
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
        private TextMeshProUGUI title, subtitle;
        private GameObject backButton;
        private ChoiceToolbar tools;
        private CombatSystem combat;
        private ChestReward? replacing;   // the new item waiting for a slot
        private List<ItemBase> replaceOptions;
        private Rarity shownRarity;
        private string shownTitle;
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
                if (Instance != null) Instance.combat = combat;
                string title = seriesTitle == null ? null : count > 1 ? $"{seriesTitle}  ·  {i + 1}/{count}" : seriesTitle;
                if (Instance == null || !Instance.Show(rarity, rolled, choice => combat.Claim(choice, stats), title))
                {
                    combat.Claim(rolled[0], stats, combat.WeakestIfFull(rolled[0]));
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
            this.combat = combat;
            List<ChestReward> rolled = combat.RollChest(rarity);
            string title = $"{seriesTitle}  ·  {index + 1}/{count}";
            if (!Show(rarity, rolled, choice => combat.Claim(choice, stats), title)) combat.Claim(rolled[0], stats, combat.WeakestIfFull(rolled[0]));
        }

        private void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.Canvas(transform, "Chest Canvas", 25);
            root = canvas.gameObject;
            UIFactory.Dim(canvas.transform, 0.66f);
            rays = UIFactory.Centre(canvas.transform, "Rays", new Vector2(0f, 300f), new Vector2(900f, 900f));
            raysImage = UIFactory.Image(rays, Color.white, UISkin.Rays);
            title = UIFactory.Title(UIFactory.Centre(canvas.transform, "Title", new Vector2(0f, 350f), new Vector2(1300f, 96f)),
                "Text", "CHEST", 68f, UITheme.Gold, TextAlignmentOptions.Center, true, true);
            UIFactory.Stretch((RectTransform)title.transform);
            title.characterSpacing = 12f;
            UIPop.Add(title, 0f, 1.4f);
            subtitle = UIFactory.Text(UIFactory.Centre(canvas.transform, "Subtitle", new Vector2(0f, 290f), new Vector2(1200f, 40f)),
                "Choose one", 26f, UITheme.TextMuted, TextAlignmentOptions.Center, true);
            cardRow = UIFactory.Centre(canvas.transform, "Cards", new Vector2(0f, -30f), new Vector2(1780f, 560f));   // room for 4 (Fortune set)
            UIFactory.Row(cardRow, 44f, 0);
            tools = new ChoiceToolbar(canvas.transform, new Vector2(0f, -384f), Reroll, null);   // a chest was paid for: no skip
            RectTransform back = UIFactory.Centre(canvas.transform, "Back", new Vector2(0f, -462f), new Vector2(360f, 64f));
            backButton = UIFactory.Button(back, "Back", BackToChoices).gameObject;
            backButton.SetActive(false);
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
            shownRarity = rarity;
            shownTitle = titleText;
            ShowChoices();

            shownAt = Time.unscaledTime;
            root.SetActive(true);
            title.gameObject.SetActive(false);
            title.gameObject.SetActive(true);   // replays the title pop
            return true;
        }

        private Hookfall.Progression.ChoiceTokens Tokens => combat != null ? combat.GetComponent<Hookfall.Progression.ChoiceTokens>() : null;

        /// <summary>Rolls the chest again; costs a reroll.</summary>
        private void Reroll()
        {
            if (replacing != null || choices == null || Tokens == null || !Tokens.TryReroll()) return;
            Redeal();
        }

        private void Redeal()
        {
            choices = combat.RollChest(shownRarity);
            ShowChoices();
            shownAt = Time.unscaledTime;
        }

        private void ClearCards()
        {
            foreach (GameObject card in cards) Destroy(card);
            cards.Clear();
        }

        private void ShowChoices()
        {
            ClearCards();
            replacing = null;
            replaceOptions = null;
            backButton.SetActive(false);
            cardRow.localScale = Vector3.one;
            Color color = UITheme.ForRarity(shownRarity);
            raysImage.color = new Color(color.r, color.g, color.b, 0.3f);
            tools.Refresh(Tokens);
            title.text = shownTitle ?? Loc.T(shownRarity.ToString().ToUpperInvariant() + " CHEST");
            title.color = color;
            subtitle.text = Loc.T("Choose one");
            for (int i = 0; i < choices.Count; i++)
                cards.Add(BuildCard(i, choices[i], choices[i].Item != null ? UITheme.ForRarity(choices[i].Item.Rarity) : color));
        }

        /// <summary>The slots are full: the owned items of the new one's kind, to pick the one it replaces.</summary>
        private void ShowReplace(ChestReward choice)
        {
            ClearCards();
            replacing = choice;
            replaceOptions = combat.ItemsOfKind(choice.Item.IsPassive);
            backButton.SetActive(true);
            cardRow.localScale = Vector3.one * Mathf.Min(1f, 1780f / (replaceOptions.Count * 444f));   // six cards fit the row
            title.text = Loc.T("REPLACE AN ITEM");
            title.color = UITheme.Gold;
            subtitle.text = Loc.F("Choose the item {0} replaces", choice.Item.DisplayName);
            for (int i = 0; i < replaceOptions.Count; i++)
            {
                ItemBase item = replaceOptions[i];
                int captured = i;
                string value = string.Join("\n", item.StatLines());
                cards.Add(UIFactory.ChoiceCard(cardRow, i, UITheme.ForRarity(item.Rarity), UISkin.Icon(item), item.DisplayName,
                    Loc.F("LEVEL {0}", item.Level), value, item.Summary, () => Pick(captured), item.Schools).gameObject);
            }
            shownAt = Time.unscaledTime;
        }

        private void BackToChoices()
        {
            if (replacing == null) return;
            ShowChoices();
            shownAt = Time.unscaledTime;
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
                    value = string.Join("\n", choice.Item.StatLines());
                }
                else
                {
                    tag = Loc.F("LEVEL {0} → {1}", choice.Item.Level, Mathf.Min(choice.Item.Level + choice.LevelsGained, ItemBase.MaxLevel));
                    value = choice.Item.DescribeNextLevel();
                }
            }
            int captured = index;
            return UIFactory.ChoiceCard(cardRow, index, color, icon, name, tag, value, detail, () => Pick(captured), choice.Item != null ? choice.Item.Schools : null).gameObject;
        }

        private void Update()
        {
            if (!root.activeSelf) return;
            rays.Rotate(0f, 0f, -18f * Time.unscaledDeltaTime);
            Keyboard k = Keyboard.current;
            if (k == null) return;
            for (int i = 0; i < 6; i++)
                if (k[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame || k[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame) { Pick(i); return; }
            if (k.backspaceKey.wasPressedThisFrame) BackToChoices();
            else if (replacing == null) tools.HandleKeys(k, Tokens);
        }

        private void Pick(int index)
        {
            // A short guard so a click or key held from before the chest opened does not pick for the player.
            if (!root.activeSelf || choices == null || Time.unscaledTime - shownAt < 0.3f) return;
            if (tools.Banishing && replacing == null)
            {
                // The card goes for the rest of the run, and the chest is rolled again at no cost.
                if (index >= choices.Count) return;
                ChestReward gone = choices[index];
                Hookfall.Progression.ChoiceTokens tokens = Tokens;
                bool done = tokens != null && (gone.Item != null ? tokens.TryBanish(gone.Item) : tokens.TryBanish(gone.FallbackStat));
                tools.SetBanishing(false, tokens);
                if (done) Redeal();
                return;
            }
            ItemBase replaced = null;
            ChestReward choice;
            if (replacing != null)
            {
                if (index >= replaceOptions.Count) return;
                choice = replacing.Value;
                replaced = replaceOptions[index];
            }
            else
            {
                if (index >= choices.Count) return;
                choice = choices[index];
                if (choice.IsNewItem && combat != null && !combat.HasFreeSlot(choice.Item.IsPassive))
                {
                    ShowReplace(choice);
                    return;
                }
            }
            root.SetActive(false);
            choices = null;
            replacing = null;
            if (replaced != null) combat.Claim(choice, FindAnyObjectByType<PlayerStats>(), replaced);
            else claim?.Invoke(choice);
            GameManager.Instance?.CloseModal();
            if (pending > 0 && next != null) next();
            else next = null;
        }
    }
}
