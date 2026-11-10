// Hookfall - the HUD's build list (top right): one row per school the Warden's items belong to, with its icon and four
// pips (two for the first set bonus, four for the second), and a gold tag for every duo that is active.
// Rebuilt only when a count changes.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10 (it replaces the text list of 2026-11-08).
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class HudBuild
    {
        private readonly RectTransform column;
        private readonly GameObject[] rows;
        private readonly Image[][] pips;
        private readonly TextMeshProUGUI[] extra;
        private readonly TextMeshProUGUI duos;
        private int hash = -1;

        public HudBuild(Transform canvas, Vector2 position)
        {
            column = UIFactory.Rect(canvas, "Build", new Vector2(1f, 1f), new Vector2(1f, 1f), position, new Vector2(360f, 440f), new Vector2(1f, 1f));
            VerticalLayoutGroup layout = UIFactory.Column(column, 6f, 0, TextAnchor.UpperRight);
            layout.childForceExpandWidth = false;
            int count = SchoolInfo.All.Length;
            rows = new GameObject[count];
            pips = new Image[count][];
            extra = new TextMeshProUGUI[count];
            for (int i = 0; i < count; i++)
            {
                School school = SchoolInfo.All[i];
                RectTransform row = UIFactory.Item(column, school.ToString(), -1f, 36f);
                UIFactory.Image(row, new Color(0.04f, 0.06f, 0.12f, 0.78f), UISkin.Chip);
                HorizontalLayoutGroup line = UIFactory.Row(row, 5f, 0, TextAnchor.MiddleRight);
                line.padding = new RectOffset(12, 8, 2, 2);
                line.childForceExpandHeight = false;
                extra[i] = UIFactory.Sized(UIFactory.Text(row, "Count", "", 18f, SchoolInfo.Color(school), TextAlignmentOptions.Right), 26f, 26f);
                extra[i].fontStyle = FontStyles.Bold;
                pips[i] = new Image[SynergySystem.SecondTier];
                for (int p = 0; p < pips[i].Length; p++)
                {
                    pips[i][p] = UIFactory.Image(UIFactory.Item(row, "Pip", 16f, 16f), Color.white, UISkin.Pip);
                    if (p == SynergySystem.FirstTier - 1) UIFactory.Item(row, "Gap", 3f, 4f);   // the two sets read apart
                }
                UIFactory.Icon(row, UISkin.Icon(school), 30f);
                rows[i] = row.gameObject;
                row.gameObject.SetActive(false);
            }
            duos = UIFactory.Sized(UIFactory.Text(column, "Duos", "", 20f, UITheme.Gold, TextAlignmentOptions.TopRight, true), 360f, 150f);
            duos.fontStyle = FontStyles.Bold;
            duos.textWrappingMode = TextWrappingModes.NoWrap;
            duos.margin = new Vector4(0f, 0f, 6f, 0f);
            duos.lineSpacing = -6f;
        }

        public void Refresh(SynergySystem synergy)
        {
            if (synergy == null) return;
            int now = 17;
            foreach (School school in SchoolInfo.All) now = now * 7 + Mathf.Min(synergy.Count(school), 6);
            foreach (Duo duo in DuoInfo.All) now = now * 2 + (synergy.HasDuo(duo) ? 1 : 0);
            if (now == hash) return;
            hash = now;

            for (int i = 0; i < rows.Length; i++)
            {
                School school = SchoolInfo.All[i];
                int count = synergy.Count(school);
                rows[i].SetActive(count > 0);
                if (count == 0) continue;
                Color color = SchoolInfo.Color(school);
                for (int p = 0; p < pips[i].Length; p++)
                    pips[i][p].color = p < count ? color : new Color(1f, 1f, 1f, 0.24f);
                extra[i].text = count > SynergySystem.SecondTier ? "+" + (count - SynergySystem.SecondTier) : "";
            }
            var text = new System.Text.StringBuilder();
            foreach (Duo duo in DuoInfo.All)
                if (synergy.HasDuo(duo)) text.Append(DuoInfo.Name(duo)).Append('\n');
            duos.text = text.ToString();
        }
    }
}
