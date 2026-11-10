// Hookfall - level-up screen: the game pauses and shows 3 cards; pick by click or 1, 2, 3 (GDD 9.1).
// Cards pop in one after another; items are framed in reward gold, stats in player cyan, each with its icon.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Core;
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
            UIFactory.Dim(canvas.transform, 0.66f);

            rays = UIFactory.Centre(canvas.transform, "Rays", new Vector2(0f, 300f), new Vector2(900f, 900f));
            UIFactory.Image(rays, new Color(0.35f, 0.9f, 1f, 0.18f), UISkin.Rays);
            title = UIFactory.Title(UIFactory.Centre(canvas.transform, "Title", new Vector2(0f, 330f), new Vector2(1100f, 96f)),
                "Text", "LEVEL UP", 76f, UITheme.Player, TextAlignmentOptions.Center, true, true);
            UIFactory.Stretch((RectTransform)title.transform);
            title.characterSpacing = 14f;
            UIPop.Add(title, 0f, 1.4f);
            subtitle = UIFactory.Text(UIFactory.Centre(canvas.transform, "Subtitle", new Vector2(0f, 270f), new Vector2(900f, 40f)),
                "Choose one", 26f, UITheme.TextMuted, TextAlignmentOptions.Center, true);

            cardRow = UIFactory.Centre(canvas.transform, "Cards", new Vector2(0f, -60f), new Vector2(1320f, 560f));
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
            subtitle.text = levelUp.PendingLevels > 1 ? Loc.F("Choose one  ·  {0} levels to spend", levelUp.PendingLevels) : Loc.T("Choose one");
            root.SetActive(true);
            title.gameObject.SetActive(false);
            title.gameObject.SetActive(true);   // replays the title pop
        }

        private GameObject BuildCard(int index, LevelUpOption option)
        {
            int captured = index;
            Color accent = option.IsItem ? UITheme.Gold : UITheme.Player;
            string name = option.IsItem ? option.Item.DisplayName : Loc.T(PlayerStats.NameOf(option.Stat));
            string tag = option.IsItem ? Loc.F("ITEM  ·  LEVEL {0} → {1}", option.Item.Level, option.Item.Level + 1) : Loc.T("STAT");
            string value = option.IsItem ? option.Item.DescribeNextLevel() : stats.PickText(option.Stat);
            string detail = option.IsItem ? option.Item.Summary : Loc.F("Now {0}", stats.Describe(option.Stat));
            Sprite icon = option.IsItem ? UISkin.Icon(option.Item) : UISkin.Icon(option.Stat);
            return UIFactory.ChoiceCard(cardRow, index, accent, icon, name, tag, value, detail, () => Pick(captured), option.IsItem ? option.Item.Schools : null).gameObject;
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
