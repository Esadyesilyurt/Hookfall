// Hookfall - builds uGUI + TextMeshPro elements from code, so every screen is plain C# with no hand-wired prefabs.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
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

        /// <summary>A rect that fills its parent.</summary>
        public static RectTransform Fill(Transform parent, string name)
        {
            RectTransform rt = Rect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rt;
        }

        public static Image Image(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Image Panel(Transform parent, string name, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return Image(rt, color);
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static TextMeshProUGUI Text(RectTransform rt, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
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

        public static Button Button(Transform parent, string label, Action onClick, float width = 360f, float height = 64f)
        {
            var go = new GameObject(label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = UITheme.Button;
            colors.highlightedColor = UITheme.ButtonHover;
            colors.selectedColor = UITheme.Button;
            colors.pressedColor = UITheme.Player;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            TextMeshProUGUI text = Text(go.transform, "Label", label, 28f, UITheme.Text);
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

            RectTransform track = Rect(sliderGo.transform, "Track", new Vector2(0f, 0.4f), new Vector2(1f, 0.6f), Vector2.zero, Vector2.zero);
            Image(track, UITheme.Button);
            RectTransform fillArea = Rect(sliderGo.transform, "Fill Area", new Vector2(0f, 0.4f), new Vector2(1f, 0.6f), Vector2.zero, new Vector2(-20f, 0f));
            RectTransform fill = Rect(fillArea, "Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(fill, UITheme.Player);
            RectTransform handleArea = Rect(sliderGo.transform, "Handle Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
            RectTransform handle = Rect(handleArea, "Handle", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(20f, 0f));
            Image handleImage = Image(handle, UITheme.Text);

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

        /// <summary>A bar whose fill is scaled by width; set Fill(bar, 0..1).</summary>
        public static RectTransform Bar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size, Color back, Color fill, Vector2? pivot = null)
        {
            RectTransform rt = Rect(parent, name, anchorMin, anchorMax, position, size, pivot);
            Image(rt, back);
            RectTransform fillRect = Rect(rt, "Fill", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            Image(fillRect, fill);
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

        /// <summary>A diamond-shaped icon frame (mood board reference 9) with a short glyph inside.</summary>
        public static RectTransform Diamond(Transform parent, string glyph, float size, Color color)
        {
            RectTransform holder = Rect(parent, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Sized(holder, size, size);
            RectTransform diamond = Rect(holder, "Diamond", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.68f, size * 0.68f));
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image(diamond, color);
            RectTransform inner = Rect(diamond, "Inner", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -8f));
            Image(inner, UITheme.PanelLight);
            TextMeshProUGUI text = Text(holder, "Glyph", glyph, size * 0.32f, color);
            Stretch((RectTransform)text.transform);
            return holder;
        }
    }
}
