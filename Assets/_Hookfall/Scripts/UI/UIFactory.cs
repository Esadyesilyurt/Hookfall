// Hookfall - builds uGUI + TextMeshPro elements from code, so every screen is plain C# with no hand-wired prefabs.
// Uses the generated UI skin (UISkin): sliced rounded panels, buttons, pills, diamonds, key caps and icons.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07, reworked the same day for the UI skin.
// Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public static class UIFactory
    {
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

        /// <summary>A rounded skin panel, centred.</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Color color)
        {
            RectTransform rt = Centre(parent, name, Vector2.zero, size);
            Image image = Image(rt, color, UISkin.Panel);
            image.raycastTarget = true;   // panels swallow clicks meant for what is behind them
            return image;
        }

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
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            if (outline && UISkin.OutlineFont != null) tmp.fontSharedMaterial = UISkin.OutlineFont;
            return tmp;
        }

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

        /// <summary>A skin button with a hover grow and a cyan press flash.</summary>
        public static Button Button(Transform parent, string label, Action onClick, float width = 360f, float height = 64f,
            Color? tint = null)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = Image((RectTransform)go.transform, Color.white, UISkin.Button);
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            Color baseColor = tint ?? UITheme.Button;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, UITheme.Player, 0.45f);
            colors.selectedColor = baseColor;
            colors.pressedColor = UITheme.Player;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            go.AddComponent<UIHover>();

            TextMeshProUGUI text = Text(go.transform, "Label", label, 28f, UITheme.Text);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 2f;
            Stretch((RectTransform)text.transform);
            Sized(button, width, height);
            return button;
        }

        public static Slider Slider(Transform parent, string label, float min, float max, float value, Action<float> onChange)
        {
            var row = new GameObject(label + " Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Row(row.GetComponent<RectTransform>(), 20f, 0);
            Sized(row.GetComponent<RectTransform>(), -1f, 48f);

            Sized(Text(row.transform, "Label", label, 26f, UITheme.Text, TextAlignmentOptions.Left), 300f, 48f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(row.transform, false);
            Sized(sliderGo.GetComponent<RectTransform>(), 380f, 48f);

            RectTransform track = Rect(sliderGo.transform, "Track", new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, Vector2.zero);
            Image(track, UITheme.Track, UISkin.Pill);
            RectTransform fillArea = Rect(sliderGo.transform, "Fill Area", new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), Vector2.zero, new Vector2(-24f, 0f));
            RectTransform fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(24f, 0f));
            Image(fill, UITheme.Player, UISkin.Pill);
            RectTransform handleArea = Rect(sliderGo.transform, "Handle Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f));
            RectTransform handle = Rect(handleArea, "Handle", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(30f, 0f));
            Image handleImage = Image(handle, UITheme.Text, UISkin.Dot);
            handleImage.raycastTarget = true;
            handleImage.preserveAspect = true;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        /// <summary>A rounded pill bar; returns the fill rect, set with SetFill(fill, 0..1).</summary>
        public static RectTransform Bar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Color back, Color fill, Vector2? pivot = null)
        {
            RectTransform rt = Rect(parent, name, anchorMin, anchorMax, position, size, pivot);
            Image(rt, back, UISkin.Pill);
            RectTransform fillRect = Rect(rt, "Fill", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            Image(fillRect, fill, UISkin.Pill);
            RectTransform frame = Rect(rt, "Frame", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(frame, new Color(1f, 1f, 1f, 0.35f), UISkin.PillFrame);
            return fillRect;
        }

        public static void SetFill(RectTransform fill, float amount) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Icon(Transform parent, Sprite sprite, float size, Color? tint = null)
        {
            RectTransform rt = Centre(parent, "Icon", Vector2.zero, new Vector2(size, size));
            Sized(rt, size, size);
            Image image = Image(rt, tint ?? Color.white, sprite);
            image.preserveAspect = true;
            return image;
        }

        /// <summary>A diamond frame (mood board reference 9) holding an icon, or a glyph when there is no icon.</summary>
        public static RectTransform Diamond(Transform parent, Sprite icon, string glyph, float size, Color color)
        {
            RectTransform holder = Centre(parent, "Diamond", Vector2.zero, new Vector2(size, size));
            Sized(holder, size, size);
            RectTransform glow = Centre(holder, "Glow", Vector2.zero, new Vector2(size * 1.5f, size * 1.5f));
            Image(glow, new Color(color.r, color.g, color.b, 0.35f), UISkin.Glow);
            RectTransform frame = Fill(holder, "Frame");
            Image(frame, color, UISkin.Diamond);
            if (icon != null)
            {
                RectTransform rt = Centre(holder, "Icon", Vector2.zero, new Vector2(size * 0.62f, size * 0.62f));
                Image(rt, Color.white, icon).preserveAspect = true;
            }
            else
            {
                TextMeshProUGUI text = Text(holder, "Glyph", glyph, size * 0.32f, UITheme.Text);
                Stretch((RectTransform)text.transform);
            }
            return holder;
        }

        /// <summary>A key cap with a key name, e.g. [E] or [1].</summary>
        public static RectTransform KeyCap(Transform parent, string key, float size = 44f)
        {
            RectTransform cap = Centre(parent, "Key " + key, Vector2.zero, new Vector2(Mathf.Max(size, key.Length * size * 0.42f + 20f), size));
            Sized(cap, cap.sizeDelta.x, size);
            Image(cap, UITheme.Text, UISkin.KeyCap);
            TextMeshProUGUI text = Text(cap, "Label", key, size * 0.5f, UITheme.Text);
            text.fontStyle = FontStyles.Bold;
            Stretch((RectTransform)text.transform);
            return cap;
        }

        /// <summary>Small level pips under an item icon: filled up to level, out of max.</summary>
        public static RectTransform Pips(Transform parent, int level, int max, float size, Color on, Color off)
        {
            RectTransform row = Rect(parent, "Pips", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(max * (size + 3f), size), new Vector2(0.5f, 0f));
            for (int i = 0; i < max; i++)
            {
                RectTransform pip = Rect(row, "Pip", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * (size + 3f) + size / 2f, 0f), new Vector2(size, size));
                pip.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image(pip, i < level ? on : off, UISkin.Pill);
            }
            return row;
        }
    }
}
