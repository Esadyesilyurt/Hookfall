// Hookfall - the main menu's upgrade shop: the Heartstone shards in hand, and a row per permanent upgrade with its
// level pips, what a level gives and a button with the price (MetaProgress).
// AI disclosure: written with Claude (Anthropic) on 2026-11-11. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
using Hookfall.Player;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class ShopPanel : MonoBehaviour
    {
        private TextMeshProUGUI shards;
        private RectTransform rows;

        public static GameObject Build(Transform canvas, Action onBack)
        {
            Image image = UIFactory.Panel(canvas, "Shop", new Vector2(1100f, 960f), UITheme.Panel);
            VerticalLayoutGroup column = UIFactory.Column(image, 12f, 40, TextAnchor.UpperCenter);
            column.padding = new RectOffset(50, 50, 40, 36);
            var panel = image.gameObject.AddComponent<ShopPanel>();

            UIFactory.Heading(image.transform, "UPGRADES", 40f, UITheme.Player);
            RectTransform purse = UIFactory.Item(image.transform, "Purse", -1f, 50f);
            UIFactory.Row(purse, 12f, 0).childForceExpandHeight = false;
            UIFactory.Icon(purse, UISkin.Icon("Shard"), 44f);
            panel.shards = UIFactory.Sized(UIFactory.Text(purse, "Shards", "", 30f, UITheme.Gold, TextAlignmentOptions.Left), 520f, 44f);
            panel.shards.fontStyle = FontStyles.Bold;
            UIFactory.Sized(UIFactory.Text(image.transform, "Note", "Every run pays Heartstone shards. Upgrades last for every run.", 21f, UITheme.TextMuted), -1f, 30f);
            panel.rows = UIFactory.Scroll(image.transform, "Rows", -1f, 640f, 8f);
            UIFactory.Button(UIFactory.ButtonBar(image.transform), "Back", () => onBack(), 320f, 62f, null, UISkin.Icon("Back"));
            UIPop.Add(image, 0f, 0.85f);
            return image.gameObject;
        }

        private void OnEnable()
        {
            if (rows != null) Refresh();
        }

        private void Refresh()
        {
            shards.text = Loc.F("{0} Heartstone shards", MetaProgress.Shards);
            for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
            foreach (MetaProgress.Upgrade upgrade in MetaProgress.Upgrades)
            {
                RectTransform row = UIFactory.Item(rows, upgrade.Id, -1f, 72f);
                UIFactory.Image(row, new Color(1f, 1f, 1f, 0.06f), UISkin.Slot);
                HorizontalLayoutGroup line = UIFactory.Row(row, 16f, 0, TextAnchor.MiddleLeft);
                line.padding = new RectOffset(16, 12, 6, 6);
                line.childForceExpandHeight = false;
                UIFactory.Icon(row, upgrade.Stat.HasValue ? UISkin.Icon(upgrade.Stat.Value) : UISkin.Icon(upgrade.Icon), 52f);
                TextMeshProUGUI name = UIFactory.Sized(UIFactory.Text(row, "Name", upgrade.Name, 25f, UITheme.Text, TextAlignmentOptions.Left), 200f, 34f);
                name.fontStyle = FontStyles.Bold;
                int level = MetaProgress.LevelOf(upgrade.Id);
                UIFactory.Sized(UIFactory.Text(row, "Level", level + " / " + MetaProgress.MaxLevel, 23f, level > 0 ? UITheme.Gold : UITheme.TextMuted), 60f, 34f).fontStyle = FontStyles.Bold;
                UIFactory.Sized(UIFactory.Text(row, "Gives", Describe(upgrade), 21f, UITheme.TextMuted, TextAlignmentOptions.Left), 380f, 56f);
                int cost = MetaProgress.CostOf(upgrade.Id);
                string id = upgrade.Id;
                Button buy = UIFactory.Button(row, cost < 0 ? Loc.T("MAX") : cost.ToString(), () => Buy(id), 170f, 54f,
                    cost >= 0 && MetaProgress.Shards >= cost ? UITheme.ButtonPrimary : (Color?)null, cost < 0 ? null : UISkin.Icon("Shard"));
                buy.interactable = cost >= 0 && MetaProgress.Shards >= cost;
            }
        }

        /// <summary>What each level gives.</summary>
        private static string Describe(MetaProgress.Upgrade upgrade)
        {
            if (!upgrade.Stat.HasValue) return upgrade.Rerolls > 0 ? Loc.T("+1 reroll every run") : Loc.T("+1 banish every run");
            return Loc.F("A free {0} upgrade at the start", Loc.T(PlayerStats.NameOf(upgrade.Stat.Value)));
        }

        private void Buy(string id)
        {
            if (MetaProgress.TryBuy(id)) Refresh();
        }
    }
}
