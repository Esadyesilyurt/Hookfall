// Hookfall - level-up screen: the game pauses and shows 3 cards; pick by click or 1, 2, 3 (GDD 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
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
        private RectTransform cardRow;
        private TextMeshProUGUI title;
        private readonly List<GameObject> cards = new List<GameObject>();

        private void Awake()
        {
            Canvas canvas = UIFactory.Canvas(transform, "Level Up Canvas", 20);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), UITheme.Dim);
            title = UIFactory.Text(UIFactory.Rect(canvas.transform, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(900f, 80f)),
                "Level up", 56f, UITheme.Player);
            title.fontStyle = FontStyles.Bold;
            cardRow = UIFactory.Rect(canvas.transform, "Cards", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1300f, 440f));
            UIFactory.Row(cardRow, 40f, 0);
            root.SetActive(false);
        }

        private void OnEnable()
        {
            if (levelUp != null) levelUp.OfferReady += Show;
        }

        private void OnDisable()
        {
            if (levelUp != null) levelUp.OfferReady -= Show;
        }

        private void Show()
        {
            foreach (GameObject card in cards) Destroy(card);
            cards.Clear();

            IReadOnlyList<LevelUpOption> offer = levelUp.Offer;
            for (int i = 0; i < offer.Count; i++) cards.Add(BuildCard(i, offer[i]));
            title.text = levelUp.PendingLevels > 1 ? $"Level up  (+{levelUp.PendingLevels})" : "Level up";
            root.SetActive(true);
        }

        private GameObject BuildCard(int index, LevelUpOption option)
        {
            int captured = index;
            Button button = UIFactory.Button(cardRow, "", () => Pick(captured), 380f, 440f);
            Destroy(button.transform.Find("Label").gameObject);
            UIFactory.Column(button, 16f, 28, TextAnchor.UpperCenter);

            Color accent = option.IsItem ? UITheme.Reward : UITheme.Player;
            string name = option.IsItem ? option.Item.DisplayName : PlayerStats.NameOf(option.Stat);
            string kind = option.IsItem ? $"Item level {option.Item.Level} → {option.Item.Level + 1}" : "Stat";
            string value = option.IsItem ? option.Item.DescribeNextLevel() : stats.Describe(option.Stat);

            UIFactory.Sized(UIFactory.Text(button.transform, "Key", $"[{index + 1}]", 24f, UITheme.TextMuted), -1f, 30f);
            UIFactory.Diamond(button.transform, name.Substring(0, 1), 110f, accent);
            UIFactory.Sized(UIFactory.Text(button.transform, "Name", name, 34f, UITheme.Text), -1f, 50f);
            UIFactory.Sized(UIFactory.Text(button.transform, "Kind", kind, 22f, accent), -1f, 30f);
            UIFactory.Sized(UIFactory.Text(button.transform, "Value", value, 28f, UITheme.Text), -1f, 80f);
            return button.gameObject;
        }

        private void Update()
        {
            if (!root.activeSelf) return;
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
