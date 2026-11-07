// Hookfall - level-up screen: the game pauses and shows 3 cards; pick by click or 1, 2, 3 (GDD 9.1).
// Cards pop in one after another; items are framed in reward gold, stats in player cyan, each with its icon.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Player;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private LevelUpSystem levelUp;
        [SerializeField] private PlayerStats stats;

        private GameObject root;
        private RectTransform cardRow, rays;
        private TextMeshProUGUI title, subtitle;
        private readonly List<GameObject> cards = new List<GameObject>();

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Level Up Canvas", 20);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0.02f, 0.04f, 0.08f, 0.72f));

            rays = UIFactory.Centre(canvas.transform, "Rays", new Vector2(0f, 300f), new Vector2(900f, 900f));
            UIFactory.Image(rays, new Color(0.35f, 0.9f, 1f, 0.18f), UISkin.Rays);
            title = UIFactory.Text(UIFactory.Centre(canvas.transform, "Title", new Vector2(0f, 330f), new Vector2(900f, 90f)),
                "LEVEL UP", 72f, UITheme.Player, TextAlignmentOptions.Center, true);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 14f;
            UIPop.Add(title, 0f, 1.4f);
            subtitle = UIFactory.Text(UIFactory.Centre(canvas.transform, "Subtitle", new Vector2(0f, 270f), new Vector2(900f, 40f)),
                "Choose one", 26f, UITheme.TextMuted, TextAlignmentOptions.Center, true);

            cardRow = UIFactory.Centre(canvas.transform, "Cards", new Vector2(0f, -30f), new Vector2(1320f, 470f));
            UIFactory.Row(cardRow, 44f, 0);
            root.SetActive(false);
        }

        private void OnEnable()
        {
            if (levelUp != null)
            {
                levelUp.OfferReady += Show;
                levelUp.Picked += OnPicked;
            }
        }

        private void OnDisable()
        {
            if (levelUp != null)
            {
                levelUp.OfferReady -= Show;
                levelUp.Picked -= OnPicked;
            }
        }

        // However the card was chosen, the screen closes; a queued level opens it again right after.
        private void OnPicked(LevelUpOption option) => root.SetActive(false);

        private void Show()
        {
            foreach (GameObject card in cards) Destroy(card);
            cards.Clear();

            IReadOnlyList<LevelUpOption> offer = levelUp.Offer;
            for (int i = 0; i < offer.Count; i++) cards.Add(BuildCard(i, offer[i]));
            subtitle.text = levelUp.PendingLevels > 1 ? $"Choose one  ·  {levelUp.PendingLevels} levels to spend" : "Choose one";
            root.SetActive(true);
            title.gameObject.SetActive(false);
            title.gameObject.SetActive(true);   // replays the title pop
        }

        private GameObject BuildCard(int index, LevelUpOption option)
        {
            int captured = index;
            Color accent = option.IsItem ? UITheme.Gold : UITheme.Player;

            var cardGo = new GameObject($"Card {index + 1}", typeof(RectTransform));
            cardGo.transform.SetParent(cardRow, false);
            var rect = (RectTransform)cardGo.transform;
            Image frame = UIFactory.Image(rect, Color.Lerp(accent, UITheme.Panel, 0.55f), UISkin.Card);
            frame.raycastTarget = true;
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = frame;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(1.5f, 1.5f, 1.5f);
            colors.colorMultiplier = 1.5f;
            button.colors = colors;
            button.onClick.AddListener(() => Pick(captured));
            UIFactory.Sized(rect, 400f, 470f);
            UIFactory.Column(rect, 12f, 30, TextAnchor.UpperCenter);
            UIPop.Add(rect, 0.08f + index * 0.09f, 0.6f);
            var hover = cardGo.AddComponent<UIHover>();
            hover.hoverScale = 1.05f;

            string name = option.IsItem ? option.Item.DisplayName : PlayerStats.NameOf(option.Stat);
            string tag = option.IsItem ? $"ITEM  ·  LEVEL {option.Item.Level} → {option.Item.Level + 1}" : "STAT";
            string value = option.IsItem ? option.Item.DescribeNextLevel() : stats.Describe(option.Stat);
            Sprite icon = option.IsItem ? UISkin.Icon(option.Item) : UISkin.Icon(option.Stat);

            RectTransform keyRow = UIFactory.Rect(rect, "Key", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            UIFactory.Sized(keyRow, -1f, 40f);
            UIFactory.Row(keyRow, 0f, 0, TextAnchor.MiddleCenter);
            UIFactory.KeyCap(keyRow, (index + 1).ToString(), 38f);
            UIFactory.Diamond(rect, icon, name.Substring(0, 1), 150f, accent);
            UIFactory.Sized(UIFactory.Text(rect, "Name", name, 36f, UITheme.Text, TextAlignmentOptions.Center, true), -1f, 50f).fontStyle = FontStyles.Bold;
            TextMeshProUGUI tagText = UIFactory.Sized(UIFactory.Text(rect, "Tag", tag, 18f, accent), -1f, 26f);
            tagText.characterSpacing = 4f;
            tagText.fontStyle = FontStyles.Bold;
            UIFactory.Sized(UIFactory.Text(rect, "Value", value, 30f, UITheme.Text, TextAlignmentOptions.Center, true), -1f, 90f);
            return cardGo;
        }

        private void Update()
        {
            if (!root.activeSelf) return;
            rays.Rotate(0f, 0f, -12f * Time.unscaledDeltaTime);
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) Pick(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) Pick(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) Pick(2);
        }

        private void Pick(int index)
        {
            if (!root.activeSelf || index >= levelUp.Offer.Count) return;
            root.SetActive(false);
            levelUp.Pick(index);   // may raise OfferReady again for a queued level
        }
    }
}
