// Hookfall - chest result: one item card in its rarity colour, marked as a new item or a level-up (GDD 9.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.UI
{
    public class ChestResultUI : MonoBehaviour
    {
        public static ChestResultUI Instance { get; private set; }

        private GameObject root;
        private RectTransform card;
        private float shownAt;

        private void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.Canvas(transform, "Chest Canvas", 25);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), UITheme.Dim);
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Pauses the game and shows what the chest gave. Returns false if another screen is open.</summary>
        public bool Show(ChestReward reward)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.OpenModal(RunState.ChestResult)) return false;

            if (card != null) Destroy(card.gameObject);
            Color color = UITheme.ForRarity(reward.Rarity);
            var panel = UIFactory.Panel(root.transform, "Card", new Vector2(460f, 560f), UITheme.Panel);
            card = (RectTransform)panel.transform;
            UIFactory.Column(panel, 18f, 34, TextAnchor.UpperCenter);

            string name = reward.Item != null ? reward.Item.DisplayName : PlayerStats.NameOf(reward.FallbackStat);
            string kind = reward.IsNewItem ? $"New item  ·  Level {reward.Item.Level}"
                : reward.Item != null ? $"Level up  +{reward.LevelsGained}  ·  Level {reward.Item.Level}"
                : $"All items maxed  ·  {name} ×{reward.LevelsGained}";

            UIFactory.Sized(UIFactory.Text(panel.transform, "Rarity", reward.Rarity.ToString().ToUpper(), 30f, color), -1f, 40f).fontStyle = FontStyles.Bold;
            UIFactory.Diamond(panel.transform, name.Substring(0, 1), 150f, color);
            UIFactory.Sized(UIFactory.Text(panel.transform, "Name", name, 40f, UITheme.Text), -1f, 56f);
            UIFactory.Sized(UIFactory.Text(panel.transform, "Kind", kind, 24f, color), -1f, 36f);
            if (reward.Item != null)
                UIFactory.Sized(UIFactory.Text(panel.transform, "Lore", $"<i>{reward.Item.Lore}</i>", 22f, UITheme.TextMuted), -1f, 90f);
            UIFactory.Button(panel.transform, "Continue", Close, 300f, 60f);

            shownAt = Time.unscaledTime;
            root.SetActive(true);
            return true;
        }

        private void Update()
        {
            if (!root.activeSelf || Time.unscaledTime - shownAt < 0.3f) return;
            Keyboard k = Keyboard.current;
            if (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) Close();
        }

        private void Close()
        {
            root.SetActive(false);
            GameManager.Instance?.CloseModal();
        }
    }
}
