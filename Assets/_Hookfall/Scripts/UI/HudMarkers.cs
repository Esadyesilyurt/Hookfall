// Hookfall - the HUD's world markers: a round badge with an icon over a target that is on screen, or on the screen's
// edge with an arrow when it is not, with the distance in metres. The HUD calls Begin, Show for every target, then End
// each frame; badges are pooled.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class HudMarkers
    {
        private class Marker
        {
            public RectTransform root, arrow;
            public Image back, ring, icon, arrowImage;
            public TextMeshProUGUI distance, label;
        }

        private readonly List<Marker> pool = new List<Marker>();
        private readonly RectTransform layer;
        private int used;
        private Camera cam;

        public HudMarkers(Transform canvas)
        {
            layer = UIFactory.Fill(canvas, "Markers");
        }

        public void Begin()
        {
            used = 0;
            cam = Camera.main;
        }

        /// <summary>Marks a world position. Within hideWithin metres nothing is drawn; label is optional text under the badge.</summary>
        public void Show(Vector3 world, Sprite icon, Color color, float hideWithin = 0f, string label = null, float alpha = 1f)
        {
            if (cam == null) return;
            float metres = Vector3.Distance(cam.transform.position, world);
            if (metres < hideWithin) return;
            Marker m = Next();
            m.icon.sprite = icon;
            m.icon.enabled = icon != null;
            m.ring.color = UITheme.WithAlpha(color, alpha);
            m.back.color = new Color(0.04f, 0.06f, 0.12f, 0.82f * alpha);
            m.icon.color = new Color(1f, 1f, 1f, alpha);
            m.arrowImage.color = UITheme.WithAlpha(color, alpha);
            m.distance.text = Mathf.RoundToInt(metres) + " m";
            m.distance.color = UITheme.WithAlpha(UITheme.Text, alpha);
            m.label.gameObject.SetActive(!string.IsNullOrEmpty(label));
            if (!string.IsNullOrEmpty(label) && m.label.text != label) m.label.text = label;
            m.label.color = UITheme.WithAlpha(color, alpha);

            Vector2 size = layer.rect.size;
            Vector2 half = size * 0.5f - new Vector2(90f, 96f);
            Vector3 local = cam.transform.InverseTransformPoint(world);
            if (local.z > 0f)
            {
                Vector3 viewport = cam.WorldToViewportPoint(world);
                var p = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
                if (Mathf.Abs(p.x) < half.x && Mathf.Abs(p.y) < half.y)
                {
                    // On screen: the badge floats above the target, its arrow pointing down at it.
                    m.root.anchoredPosition = p + new Vector2(0f, 58f);
                    m.arrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    m.arrow.anchoredPosition = new Vector2(0f, -40f);
                    return;
                }
            }
            var dir = new Vector2(local.x, local.y);
            if (local.z < 0f) dir = -dir;   // behind the camera: mirror, so it points the short way round
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();
            float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            m.root.anchoredPosition = dir * scale;
            m.arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            m.arrow.anchoredPosition = dir * 44f;
        }

        public void End()
        {
            for (int i = used; i < pool.Count; i++)
                if (pool[i].root.gameObject.activeSelf) pool[i].root.gameObject.SetActive(false);
        }

        private Marker Next()
        {
            if (used == pool.Count) pool.Add(Build());
            Marker m = pool[used++];
            if (!m.root.gameObject.activeSelf) m.root.gameObject.SetActive(true);
            return m;
        }

        private Marker Build()
        {
            var m = new Marker { root = UIFactory.Centre(layer, "Marker", Vector2.zero, new Vector2(56f, 56f)) };
            m.back = UIFactory.Image(m.root, Color.black, UISkin.Circle);
            m.ring = UIFactory.Image(UIFactory.Fill(m.root, "Ring"), Color.white, UISkin.CircleFrame);
            m.icon = UIFactory.Image(UIFactory.Centre(m.root, "Icon", Vector2.zero, new Vector2(36f, 36f)), Color.white);
            m.icon.preserveAspect = true;
            m.arrow = UIFactory.Centre(m.root, "Arrow", Vector2.zero, new Vector2(22f, 22f));
            m.arrowImage = UIFactory.Image(m.arrow, Color.white, UISkin.Icon("Arrow"));
            m.distance = UIFactory.Text(UIFactory.Centre(m.root, "Distance", new Vector2(0f, 42f), new Vector2(120f, 24f)), "", 18f, UITheme.Text, TextAlignmentOptions.Center, true);
            m.distance.fontStyle = FontStyles.Bold;
            m.label = UIFactory.Text(UIFactory.Centre(m.root, "Label", new Vector2(0f, 64f), new Vector2(260f, 26f)), "", 20f, UITheme.Text, TextAlignmentOptions.Center, true);
            m.label.fontStyle = FontStyles.Bold;
            m.label.textWrappingMode = TextWrappingModes.NoWrap;
            return m;
        }
    }
}
