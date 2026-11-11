// Hookfall - the row of buttons under a set of cards (level-up, chest): Reroll [R], Banish [B] and, where there is
// one, Skip [X], each with what is left of it. Banish is a mode: press it, then pick the card to banish.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using Hookfall.Core;
using Hookfall.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class ChoiceToolbar
    {
        private readonly Button reroll, banish, skip;
        private readonly TextMeshProUGUI rerollLabel, banishLabel, skipLabel;
        private readonly Image banishImage;
        private readonly Color banishColor;   // the button's own colour, to go back to after banish mode
        private readonly Action onReroll, onSkip;

        /// <summary>True while the next card picked is banished instead of taken.</summary>
        public bool Banishing { get; private set; }

        /// <param name="onSkip">Null for a screen that cannot be skipped (a chest that was paid for).</param>
        public ChoiceToolbar(Transform canvas, Vector2 position, Action onReroll, Action onSkip)
        {
            this.onReroll = onReroll;
            this.onSkip = onSkip;
            RectTransform row = UIFactory.Centre(canvas, "Tools", position, new Vector2(1200f, 60f));
            UIFactory.Row(row, 22f, 0).childForceExpandHeight = false;
            reroll = UIFactory.Button(row, "Reroll", () => onReroll(), 340f, 56f, null, UISkin.Icon("Reroll"));
            banish = UIFactory.Button(row, "Banish", ToggleBanish, 340f, 56f, null, UISkin.Icon("Banish"));
            banishImage = banish.GetComponent<Image>();
            banishColor = banishImage.color;
            rerollLabel = reroll.GetComponentInChildren<TextMeshProUGUI>();
            banishLabel = banish.GetComponentInChildren<TextMeshProUGUI>();
            if (onSkip == null) return;
            skip = UIFactory.Button(row, "Skip", () => onSkip(), 340f, 56f, null, UISkin.Icon("Skip"));
            skipLabel = skip.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <summary>Shows what is left; call when the screen opens and after every use.</summary>
        public void Refresh(ChoiceTokens tokens)
        {
            shown = tokens;
            int rerolls = tokens != null ? tokens.Rerolls : 0, banishes = tokens != null ? tokens.Banishes : 0;
            reroll.interactable = rerolls > 0;
            banish.interactable = banishes > 0;
            if (banishes <= 0) Banishing = false;
            rerollLabel.text = Loc.F("[R]  Reroll  ·  {0}", rerolls);
            banishLabel.text = Banishing ? Loc.T("Pick a card to banish") : Loc.F("[B]  Banish  ·  {0}", banishes);
            banishImage.color = Banishing ? UITheme.ButtonDanger : banishColor;
            if (skipLabel != null) skipLabel.text = Loc.F("[X]  Skip  ·  +{0} gold", tokens != null ? tokens.SkipGold : 0);
        }

        public void SetBanishing(bool on, ChoiceTokens tokens)
        {
            Banishing = on && tokens != null && tokens.Banishes > 0;
            Refresh(tokens);
        }

        private ChoiceTokens shown;

        private void ToggleBanish() => SetBanishing(!Banishing, shown);

        /// <summary>The keys of the row; call every frame while the screen is open.</summary>
        public void HandleKeys(Keyboard keyboard, ChoiceTokens tokens)
        {
            shown = tokens;
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame && reroll.interactable) onReroll();
            else if (keyboard.bKey.wasPressedThisFrame && banish.interactable) SetBanishing(!Banishing, tokens);
            else if (keyboard.xKey.wasPressedThisFrame && onSkip != null) onSkip();
        }
    }
}
