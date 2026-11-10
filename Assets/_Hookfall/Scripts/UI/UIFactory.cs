// Hookfall - builds uGUI + TextMeshPro elements from code, so every screen is plain C# with no hand-wired prefabs.
// Uses the UI skin (UISkin): baked navy panels, tintable frames, bars, the diamond icon frame, key caps, and the two
// fonts (Cinzel for titles, Barlow Semi Condensed for text).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, reworked the same day for the UI skin and on 2026-11-10
// for the new skin, fonts and widgets (titles, chips, tabs, toggles, scroll lists). Prompts and changes are recorded in
// Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using Hookfall.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public static class UIFactory
    {
        // ---------------------------------------------------------------- canvas and rects

        public static Canvas Canvas(Transform owner, string name, int sortOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(owner, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A rect at the centre of its parent.</summary>
        public static RectTransform Centre(Transform parent, string name, Vector2 position, Vector2 size) =>
            Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

        public static RectTransform Fill(Transform parent, string name) =>
            Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        /// <summary>A rect for a layout group to size: no anchors of its own.</summary>
        public static RectTransform Item(Transform parent, string name, float width = -1f, float height = -1f) =>
            Sized(Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), width, height);

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Image(RectTransform rt, Color color, Sprite sprite = null)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            }
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A stretched child image that layout groups skip: the layers of a card, a frame over a box.</summary>
        public static Image Layer(Transform parent, string name, Sprite sprite, Color color, float inset = 0f)
        {
            RectTransform rt = Fill(parent, name);
            Stretch(rt, inset);
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return Image(rt, color, sprite);
        }

        // ---------------------------------------------------------------- panels

        /// <summary>The skin's window panel, centred. The sprite carries a 10 px shadow margin, added to the size here.</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Color color)
        {
            RectTransform rt = Centre(parent, name, Vector2.zero, size + new Vector2(20f, 20f));
            Image image = Image(rt, color, UISkin.Panel);
            image.raycastTarget = true;   // panels swallow clicks meant for what is behind them
            return image;
        }

        /// <summary>A small see-through plaque for the HUD.</summary>
        public static Image Plaque(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, position, size, pivot);
            return Image(rt, Color.white, UISkin.Plaque);
        }

        /// <summary>The dark veil behind a screen that pauses the game: a flat dim and darker corners.</summary>
        public static void Dim(Transform canvas, float alpha = 0.72f)
        {
            Image(Fill(canvas, "Dim"), new Color(0.01f, 0.03f, 0.08f, alpha)).raycastTarget = true;
            Image(Fill(canvas, "Vignette"), new Color(1f, 1f, 1f, 0.9f), UISkin.Vignette);
        }

        // ---------------------------------------------------------------- text

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool outline = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return Text((RectTransform)go.transform, text, size, color, alignment, outline);
        }

        public static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool outline = false)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = UISkin.Body;
            tmp.text = Hookfall.Core.Loc.T(text);   // fixed labels are translated here; dynamic text is translated where it is built
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (outline) tmp.fontSharedMaterial = UISkin.BodyOutline;
            return tmp;
        }

        /// <summary>Title text in the display font: spaced capitals, outlined when it sits over the game world.</summary>
        public static TextMeshProUGUI Title(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool outline = true, bool black = false)
        {
            TextMeshProUGUI tmp = Text(parent, name, text, size, color, alignment);
            tmp.font = black ? UISkin.DisplayBlack : UISkin.Display;
            if (outline) tmp.fontSharedMaterial = UISkin.OutlineFor(tmp.font);
            tmp.characterSpacing = 6f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        /// <summary>Title text on an existing rect.</summary>
        public static TextMeshProUGUI Title(RectTransform rt, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool outline = true, bool black = false)
        {
            TextMeshProUGUI tmp = Text(rt, text, size, color, alignment);
            tmp.font = black ? UISkin.DisplayBlack : UISkin.Display;
            if (outline) tmp.fontSharedMaterial = UISkin.OutlineFor(tmp.font);
            tmp.characterSpacing = 6f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        /// <summary>A screen or section heading: the title between two diamonds and lines that fade outwards.</summary>
        public static TextMeshProUGUI Heading(Transform parent, string text, float size, Color color, float width = -1f)
        {
            RectTransform row = Item(parent, "Heading", width, size * 1.45f);
            HorizontalLayoutGroup layout = Row(row, 14f, 0);
            layout.childForceExpandHeight = false;
            Ornament(row, color, true);
            TextMeshProUGUI title = Title(row, "Title", text, size, color, TextAlignmentOptions.Center, false);
            title.gameObject.AddComponent<LayoutElement>().minHeight = size * 1.3f;
            Ornament(row, color, false);
            return title;
        }

        /// <summary>One side of a heading: a line fading outwards and, next to the title, a diamond.</summary>
        private static void Ornament(Transform row, Color color, bool left, bool pip = true)
        {
            if (!left && pip) Image(Item(row, "Pip", 14f, 14f), color, UISkin.Pip);
            RectTransform line = Item(row, "Line", -1f, 8f);
            line.GetComponent<LayoutElement>().flexibleWidth = 1f;
            line.GetComponent<LayoutElement>().minWidth = 30f;
            Image(line, UITheme.WithAlpha(color, 0.75f), UISkin.FadeLine);
            if (!left) line.localScale = new Vector3(-1f, 1f, 1f);
            if (left && pip) Image(Item(row, "Pip", 14f, 14f), color, UISkin.Pip);
        }

        /// <summary>A thin line with a diamond in the middle, to part two blocks of a card or panel.</summary>
        public static RectTransform Divider(Transform parent, Color color, float height = 14f)
        {
            RectTransform row = Item(parent, "Divider", -1f, height);
            HorizontalLayoutGroup layout = Row(row, 8f, 0);
            layout.childForceExpandHeight = false;
            Ornament(row, color, true);
            Ornament(row, color, false, false);
            return row;
        }

        /// <summary>Shrinks a text down to minSize when it does not fit its box (long translations, many stat lines).</summary>
        public static void FitText(TextMeshProUGUI text, float minSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = minSize;
        }

        // ---------------------------------------------------------------- layout

        public static VerticalLayoutGroup Column(Component target, float spacing, int padding,
            TextAnchor align = TextAnchor.UpperCenter)
        {
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Row(Component target, float spacing, int padding,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static T Sized<T>(T component, float width, float height) where T : Component
        {
            var element = component.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f) element.preferredWidth = width;
            if (height >= 0f) element.preferredHeight = height;
            return component;
        }

        /// <summary>A scrolling column (mouse wheel or drag) inside a fixed box; returns the column to fill
        /// (its box is two levels up: ScrollBox).</summary>
        public static RectTransform Scroll(Transform parent, string name, float width, float height, float spacing = 8f)
        {
            RectTransform box = Item(parent, name, width, height);
            var scroll = box.gameObject.AddComponent<ScrollRect>();
            Image hit = Image(box, new Color(0f, 0f, 0f, 0f));
            hit.raycastTarget = true;   // the wheel needs something under the pointer
            RectTransform viewport = Fill(box, "Viewport");
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect(viewport, "Content", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 1f));
            VerticalLayoutGroup layout = Column(content, spacing, 0, TextAnchor.UpperLeft);
            layout.padding = new RectOffset(0, 14, 0, 0);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform track = Rect(box, "Scrollbar", new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(8f, 0f), new Vector2(1f, 0.5f));
            Image(track, UITheme.Track, UISkin.Pill).raycastTarget = true;
            RectTransform handle = Fill(track, "Handle");
            Image handleImage = Image(handle, UITheme.WithAlpha(UITheme.Player, 0.8f), UISkin.Pill);
            handleImage.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        /// <summary>The outer box of a Scroll column, to place or stretch it.</summary>
        public static RectTransform ScrollBox(RectTransform content) => (RectTransform)content.parent.parent;

        // ---------------------------------------------------------------- controls

        /// <summary>A skin button: a cut-corner plate that brightens and grows under the pointer.</summary>
        public static Button Button(Transform parent, string label, Action onClick, float width = 360f, float height = 64f,
            Color? tint = null, Sprite icon = null)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = Image((RectTransform)go.transform, Color.white, UISkin.Button);
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            Color baseColor = tint ?? UITheme.Button;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.32f);
            colors.selectedColor = baseColor;
            colors.pressedColor = Color.Lerp(baseColor, UITheme.Player, 0.7f);
            colors.disabledColor = new Color(0.3f, 0.33f, 0.4f, 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            go.AddComponent<UIHover>();

            TextMeshProUGUI text = Text(go.transform, "Label", label, Mathf.Min(28f, height * 0.44f), UITheme.Text);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 3f;
            text.fontSharedMaterial = UISkin.BodyShadow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            FitText(text, 14f);
            var textRect = (RectTransform)text.transform;
            Stretch(textRect);
            textRect.offsetMin = new Vector2(icon != null ? height * 0.9f : 14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);
            if (icon != null)
            {
                RectTransform iconRect = Rect(go.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(height * 0.5f + 6f, 0f), Vector2.one * height * 0.62f);
                Image(iconRect, Color.white, icon).preserveAspect = true;
                text.alignment = TextAlignmentOptions.Left;
            }
            Sized(button, width, height);
            return button;
        }

        /// <summary>A one-line text box (the leaderboard name).</summary>
        public static TMP_InputField InputField(Transform parent, string placeholder, float width, float height, int characterLimit = 20)
        {
            var go = new GameObject("Input", typeof(RectTransform));
            go.SetActive(false);   // wire everything before the field's OnEnable runs
            go.transform.SetParent(parent, false);
            Image back = Image((RectTransform)go.transform, Color.white, UISkin.Slot);
            back.raycastTarget = true;
            Layer(go.transform, "Frame", UISkin.SlotFrame, UITheme.WithAlpha(UITheme.PanelLight, 0.8f));
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(go.transform, false);
            var areaRect = (RectTransform)area.transform;
            Stretch(areaRect);
            areaRect.offsetMin = new Vector2(18f, 6f);
            areaRect.offsetMax = new Vector2(-18f, -6f);
            TextMeshProUGUI text = Text(area.transform, "Text", "", height * 0.45f, UITheme.Text, TextAlignmentOptions.Left);
            Stretch((RectTransform)text.transform);
            TextMeshProUGUI hint = Text(area.transform, "Placeholder", placeholder, height * 0.45f, UITheme.TextMuted, TextAlignmentOptions.Left);
            hint.fontStyle = FontStyles.Italic;
            Stretch((RectTransform)hint.transform);
            var field = go.AddComponent<TMP_InputField>();
            field.targetGraphic = back;
            field.textViewport = areaRect;
            field.textComponent = text;
            field.placeholder = hint;
            field.characterLimit = characterLimit;
            Sized(field, width, height);
            go.SetActive(true);
            return field;
        }

        /// <summary>A settings row: icon, label, then whatever control the caller adds to the returned row.</summary>
        public static RectTransform SettingRow(Transform parent, string label, Sprite icon = null, float labelWidth = 300f)
        {
            RectTransform row = Item(parent, label + " Row", -1f, 52f);
            HorizontalLayoutGroup layout = Row(row, 16f, 0, TextAnchor.MiddleLeft);
            layout.childForceExpandHeight = false;
            if (icon != null) Icon(row, icon, 40f);
            Sized(Text(row, "Label", label, 25f, UITheme.Text, TextAlignmentOptions.Left), labelWidth, 46f);
            return row;
        }

        public static Slider Slider(Transform parent, string label, float min, float max, float value, Action<float> onChange, Sprite icon = null)
        {
            RectTransform row = SettingRow(parent, label, icon);
            RectTransform sliderRect = Item(row, "Slider", 330f, 44f);
            RectTransform track = Rect(sliderRect, "Track", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 16f));
            Image(track, Color.white, UISkin.BarBack);
            RectTransform fillArea = Rect(sliderRect, "Fill Area", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-28f, 16f));
            RectTransform fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(28f, 0f));
            Image(fill, UITheme.Player, UISkin.Pill);
            RectTransform handleArea = Rect(sliderRect, "Handle Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f));
            RectTransform handle = Rect(handleArea, "Handle", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(30f, -14f));
            Image handleImage = Image(handle, UITheme.Text, UISkin.Dot);
            handleImage.raycastTarget = true;
            handleImage.preserveAspect = true;
            Image hit = Image(sliderRect, new Color(0f, 0f, 0f, 0f));
            hit.raycastTarget = true;   // a click anywhere on the track moves the handle

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            TextMeshProUGUI number = Sized(Text(row, "Value", "", 22f, UITheme.TextMuted, TextAlignmentOptions.Right), 64f, 44f);
            void Show(float v) => number.text = max <= 1.001f ? Mathf.RoundToInt(v * 100f) + "%" : v.ToString("0.0");
            Show(value);
            slider.onValueChanged.AddListener(v => { Show(v); onChange(v); });
            return slider;
        }

        /// <summary>An on/off switch on a settings row.</summary>
        public static Button Toggle(Transform parent, string label, bool value, Action<bool> onChange, Sprite icon = null)
        {
            RectTransform row = SettingRow(parent, label, icon);
            RectTransform track = Item(row, "Switch", 84f, 40f);
            Image back = Image(track, Color.white, UISkin.BarBack);
            back.raycastTarget = true;
            RectTransform knob = Rect(track, "Knob", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
            Image knobImage = Image(knob, UITheme.Text, UISkin.Dot);
            Layer(track, "Frame", UISkin.PillFrame, new Color(1f, 1f, 1f, 0.35f));
            TextMeshProUGUI state = Sized(Text(row, "State", "", 22f, UITheme.TextMuted, TextAlignmentOptions.Left), 90f, 44f);
            bool on = value;
            void Show()
            {
                knob.anchoredPosition = new Vector2(on ? 62f : 22f, 0f);
                knobImage.color = on ? UITheme.Player : UITheme.TextMuted;
                state.text = Hookfall.Core.Loc.T(on ? "On" : "Off");
            }
            Show();
            var button = track.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.onClick.AddListener(() => { on = !on; Show(); onChange(on); });
            return button;
        }

        /// <summary>A value with arrows either side to step through its choices (resolution, language).</summary>
        public static TextMeshProUGUI Cycler(Transform parent, string label, string value, Action<int> onStep, Sprite icon = null)
        {
            RectTransform row = SettingRow(parent, label, icon);
            Button prev = Button(row, "<", () => onStep(-1), 48f, 44f);
            TextMeshProUGUI text = Sized(Text(row, "Value", value, 24f, UITheme.Text), 250f, 44f);
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            FitText(text, 14f);
            Button(row, ">", () => onStep(1), 48f, 44f);
            prev.name = "Previous";
            return text;
        }

        /// <summary>A row of tabs; onSelect gets the index. Returns a call that selects a tab from code.</summary>
        public static Action<int> Tabs(Transform parent, string[] labels, Action<int> onSelect, float tabWidth = 190f)
        {
            RectTransform row = Item(parent, "Tabs", -1f, 52f);
            HorizontalLayoutGroup layout = Row(row, 6f, 0, TextAnchor.LowerLeft);
            layout.childForceExpandHeight = false;
            var images = new Image[labels.Length];
            var texts = new TextMeshProUGUI[labels.Length];
            void Select(int index)
            {
                for (int i = 0; i < images.Length; i++)
                {
                    images[i].color = i == index ? UITheme.ButtonPrimary : UITheme.WithAlpha(UITheme.Button, 0.55f);
                    texts[i].color = i == index ? UITheme.Text : UITheme.TextMuted;
                }
                onSelect(index);
            }
            for (int i = 0; i < labels.Length; i++)
            {
                int captured = i;
                RectTransform tab = Item(row, labels[i], tabWidth, 48f);
                images[i] = Image(tab, UITheme.Button, UISkin.Tab);
                images[i].raycastTarget = true;
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = images[i];
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Select(captured));
                texts[i] = Text(tab, "Label", labels[i], 22f, UITheme.Text);
                texts[i].fontStyle = FontStyles.Bold;
                texts[i].characterSpacing = 2f;
                texts[i].textWrappingMode = TextWrappingModes.NoWrap;
                FitText(texts[i], 13f);
                Stretch((RectTransform)texts[i].transform, 6f);
            }
            return Select;
        }

        // ---------------------------------------------------------------- bars

        /// <summary>A rounded bar: dark track, fill, shine, rim; returns the fill rect, set with SetFill(fill, 0..1).</summary>
        public static RectTransform Bar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Color back, Color fill, Vector2? pivot = null)
        {
            RectTransform rt = Rect(parent, name, anchorMin, anchorMax, position, size, pivot);
            Image(rt, Color.white, UISkin.BarBack);
            RectTransform fillRect = Rect(rt, "Fill", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            Image(fillRect, fill, UISkin.Pill);
            Image(Fill(fillRect, "Gloss"), Color.white, UISkin.BarGloss);
            Image(Fill(rt, "Frame"), new Color(1f, 1f, 1f, 0.4f), UISkin.PillFrame);
            return fillRect;
        }

        public static void SetFill(RectTransform fill, float amount) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);

        // ---------------------------------------------------------------- icons, frames, chips

        public static Image Icon(Transform parent, Sprite sprite, float size, Color? tint = null)
        {
            RectTransform rt = Centre(parent, "Icon", Vector2.zero, new Vector2(size, size));
            Sized(rt, size, size);
            Image image = Image(rt, tint ?? Color.white, sprite);
            image.preserveAspect = true;
            if (sprite == null) image.enabled = false;
            return image;
        }

        /// <summary>A diamond frame (mood board reference 9) holding an icon, or a glyph when there is no icon.</summary>
        public static RectTransform Diamond(Transform parent, Sprite icon, string glyph, float size, Color color)
        {
            RectTransform holder = Centre(parent, "Diamond", Vector2.zero, new Vector2(size, size));
            Sized(holder, size, size);
            RectTransform glow = Centre(holder, "Glow", Vector2.zero, new Vector2(size * 1.55f, size * 1.55f));
            Image(glow, new Color(color.r, color.g, color.b, 0.32f), UISkin.Glow);
            Image(Fill(holder, "Fill"), Color.white, UISkin.DiamondFill);
            Image(Fill(holder, "Frame"), color, UISkin.Diamond);
            if (icon != null)
            {
                RectTransform rt = Centre(holder, "Icon", Vector2.zero, new Vector2(size * 0.6f, size * 0.6f));
                Image(rt, Color.white, icon).preserveAspect = true;
            }
            else
            {
                TextMeshProUGUI text = Text(holder, "Glyph", glyph, size * 0.32f, UITheme.Text);
                text.fontStyle = FontStyles.Bold;
                Stretch((RectTransform)text.transform);
            }
            return holder;
        }

        /// <summary>A small rounded tag: an optional icon and a short text on a dark chip with a coloured rim.</summary>
        public static RectTransform Chip(Transform parent, Sprite icon, string text, Color color, float height = 30f, float fontSize = 18f)
        {
            RectTransform chip = Item(parent, "Chip", -1f, height);
            Image(chip, new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.26f, 0.92f), UISkin.Chip);
            HorizontalLayoutGroup layout = Row(chip, 6f, 0, TextAnchor.MiddleCenter);
            layout.padding = new RectOffset(10, 12, 2, 2);
            layout.childForceExpandHeight = false;
            if (icon != null) Icon(chip, icon, height * 0.74f);
            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = Text(chip, "Label", text, fontSize, color);
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return chip;
        }

        /// <summary>One chip per school: its icon and its name in its colour.</summary>
        public static RectTransform SchoolChips(Transform parent, IReadOnlyList<School> schools, float height = 30f, bool names = true)
        {
            RectTransform row = Item(parent, "Schools", -1f, height);
            HorizontalLayoutGroup layout = Row(row, 8f, 0);
            layout.childForceExpandHeight = false;
            if (schools != null)
                foreach (School school in schools)
                    Chip(row, UISkin.Icon(school), names ? SchoolInfo.Name(school) : "", SchoolInfo.Color(school), height, height * 0.58f);
            return row;
        }

        /// <summary>A key cap with a key name, e.g. [E] or [1].</summary>
        public static RectTransform KeyCap(Transform parent, string key, float size = 44f)
        {
            RectTransform cap = Centre(parent, "Key " + key, Vector2.zero, new Vector2(Mathf.Max(size, key.Length * size * 0.4f + 22f), size));
            Sized(cap, cap.sizeDelta.x, size);
            Image(cap, UITheme.Text, UISkin.KeyCap);
            TextMeshProUGUI text = Text(cap, "Label", key, size * 0.46f, UITheme.Text);
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var rect = (RectTransform)text.transform;
            Stretch(rect);
            rect.offsetMin = new Vector2(0f, size * 0.1f);   // the cap's face sits above its lower edge
            return cap;
        }

        /// <summary>Small level pips under an item icon: filled up to level, out of max.</summary>
        public static RectTransform Pips(Transform parent, int level, int max, float size, Color on, Color off)
        {
            RectTransform row = Rect(parent, "Pips", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(max * (size + 2f), size), new Vector2(0.5f, 0f));
            row.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            for (int i = 0; i < max; i++)
            {
                RectTransform pip = Rect(row, "Pip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (size + 2f) + size / 2f + 1f, 0f), new Vector2(size, size));
                Image(pip, i < level ? on : off, UISkin.Pip);
            }
            return row;
        }

        // ---------------------------------------------------------------- the choice card

        /// <summary>
        /// A pick-one card (level-up, chest, character): a tag, the icon in its diamond, the name, the schools, the value
        /// lines in bold (stats at a glance), a small detail text (what it does) and the key that picks it. The frame and
        /// the wash at its top take the accent colour; a click calls onPick.
        /// </summary>
        public static RectTransform ChoiceCard(RectTransform row, int index, Color accent, Sprite icon, string name, string tag,
            string value, string detail, Action onPick, IReadOnlyList<School> schools = null)
        {
            var cardGo = new GameObject($"Card {index + 1}", typeof(RectTransform));
            cardGo.transform.SetParent(row, false);
            var rect = (RectTransform)cardGo.transform;
            Image hit = Image(rect, new Color(0f, 0f, 0f, 0f));
            hit.raycastTarget = true;
            var button = cardGo.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onPick());
            Sized(rect, 400f, 560f);

            // Layers, back to front: hover halo, body (the sprites carry a 10 px margin), colour wash, frame.
            Image halo = Layer(rect, "Halo", UISkin.GlowFrame, UITheme.WithAlpha(accent, 0f), -26f);
            Layer(rect, "Fill", UISkin.CardFill, Color.white, -10f);
            RectTransform wash = Rect(rect, "Wash", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 10f), new Vector2(20f, 230f), new Vector2(0.5f, 1f));
            wash.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Image(wash, UITheme.WithAlpha(accent, 0.8f), UISkin.CardHeader);
            Layer(rect, "Frame", UISkin.CardFrame, accent, -10f);

            VerticalLayoutGroup column = Column(rect, 8f, 26, TextAnchor.UpperCenter);
            column.padding = new RectOffset(26, 26, 22, 56);
            UIPop.Add(rect, 0.08f + index * 0.09f, 0.6f);
            var hover = cardGo.AddComponent<UIHover>();
            hover.hoverScale = 1.05f;
            hover.glow = halo;

            TextMeshProUGUI tagText = Sized(Text(rect, "Tag", tag, 18f, Color.Lerp(accent, Color.white, 0.5f)), -1f, 24f);
            tagText.characterSpacing = 4f;
            tagText.fontStyle = FontStyles.Bold;
            tagText.textWrappingMode = TextWrappingModes.NoWrap;
            FitText(tagText, 11f);
            // In a box of its own: the column would stretch the diamond to the card's width.
            Diamond(Item(rect, "Icon", -1f, 136f), icon, string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1), 136f, accent);
            TextMeshProUGUI nameText = Sized(Title(rect, "Name", name, 30f, UITheme.Text, TextAlignmentOptions.Center, false), -1f, 44f);
            nameText.characterSpacing = 2f;   // long (translated) names shrink to one line
            FitText(nameText, 15f);
            bool hasSchools = schools != null && schools.Count > 0;
            if (hasSchools) SchoolChips(rect, schools, 28f);
            Divider(rect, UITheme.WithAlpha(accent, 0.7f));
            TextMeshProUGUI valueText = Sized(Text(rect, "Value", value, 24f, UITheme.Text), -1f, hasSchools ? 108f : 136f);
            valueText.fontStyle = FontStyles.Bold;
            valueText.lineSpacing = -8f;
            FitText(valueText, 14f);
            TextMeshProUGUI detailText = Sized(Text(rect, "Detail", detail, 18f, UITheme.TextMuted), -1f, 80f);
            detailText.lineSpacing = -10f;
            FitText(detailText, 12f);

            RectTransform key = KeyCap(rect, (index + 1).ToString(), 38f);
            key.GetComponent<LayoutElement>().ignoreLayout = true;
            key.anchorMin = key.anchorMax = new Vector2(0.5f, 0f);
            key.anchoredPosition = new Vector2(0f, 30f);
            return rect;
        }
    }
}
