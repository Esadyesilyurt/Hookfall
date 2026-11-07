// Hookfall - chest result: one item card in its rarity colour with turning light rays behind it, marked as a new
// item or a level-up (GDD 9.1). Continue by click, E, Space or Enter.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, restyled the same day with the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
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
        private RectTransform card, rays;
        private Image raysImage;
        private float shownAt;

        private void Awake()
        {
            Instance = this;
            Canvas canvas = UIFactory.Canvas(transform, "Chest Canvas", 25);
            root = canvas.gameObject;
            UIFactory.Image(UIFactory.Fill(canvas.transform, "Dim"), new Color(0.02f, 0.04f, 0.08f, 0.72f));
            rays = UIFactory.Centre(canvas.transform, "Rays", new Vector2(0f, 40f), new Vector2(1100f, 1100f));
            raysImage = UIFactory.Image(rays, Color.white, UISkin.Rays);
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
            raysImage.color = new Color(color.r, color.g, color.b, 0.3f);

            Image panel = UIFactory.Image(UIFactory.Centre(root.transform, "Card", new Vector2(0f, 20f), new Vector2(500f, 640f)),
                Color.Lerp(color, UITheme.Panel, 0.5f), UISkin.Card);
            card = (RectTransform)panel.transform;
            UIFactory.Column(panel, 14f, 36, TextAnchor.UpperCenter);
            UIPop.Add(panel, 0f, 0.4f);

            string name = reward.Item != null ? reward.Item.DisplayName : PlayerStats.NameOf(reward.FallbackStat);
            Sprite icon = reward.Item != null ? UISkin.Icon(reward.Item) : UISkin.Icon(reward.FallbackStat);
            string kind = reward.IsNewItem ? "NEW ITEM"
                : reward.Item != null ? $"LEVEL UP  +{reward.LevelsGained}"
                : $"ALL ITEMS MAXED  ·  ×{reward.LevelsGained}";

            TextMeshProUGUI rarity = UIFactory.Sized(UIFactory.Text(panel.transform, "Rarity", reward.Rarity.ToString().ToUpper(), 34f, color, TextAlignmentOptions.Center, true), -1f, 44f);
            rarity.fontStyle = FontStyles.Bold;
            rarity.characterSpacing = 10f;
            UIFactory.Diamond(panel.transform, icon, name.Substring(0, 1), 190f, color);
            UIFactory.Sized(UIFactory.Text(panel.transform, "Name", name, 42f, UITheme.Text, TextAlignmentOptions.Center, true), -1f, 56f).fontStyle = FontStyles.Bold;
            TextMeshProUGUI kindText = UIFactory.Sized(UIFactory.Text(panel.transform, "Kind", kind, 22f, UITheme.Gold), -1f, 30f);
            kindText.characterSpacing = 4f;
            kindText.fontStyle = FontStyles.Bold;
            if (reward.Item != null)
            {
                UIFactory.Sized(UIFactory.Text(panel.transform, "Level", $"Level {reward.Item.Level}  ·  {reward.Item.DescribeNextLevel()}", 22f, UITheme.Text), -1f, 30f);
                UIFactory.Sized(UIFactory.Text(panel.transform, "Lore", $"<i>{reward.Item.Lore}</i>", 21f, UITheme.TextMuted), -1f, 84f);
            }
            UIFactory.Button(panel.transform, "Continue", Close, 300f, 60f);

            shownAt = Time.unscaledTime;
            root.SetActive(true);
            return true;
        }

        private void Update()
        {
            if (!root.activeSelf) return;
            rays.Rotate(0f, 0f, -18f * Time.unscaledDeltaTime);
            if (Time.unscaledTime - shownAt < 0.3f) return;
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
