// Hookfall - red wedges around the crosshair that point at where a hit came from and fade out: ahead of the Warden is
// the top of the ring, behind them the bottom. In a sky full of enemies this is how the player finds what bit them.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;
using UnityEngine.UI;

namespace Hookfall.UI
{
    public class HudDamageRing
    {
        private const int Count = 4;
        private const float Radius = 190f, Life = 0.9f;

        private readonly RectTransform[] wedges = new RectTransform[Count];
        private readonly Image[] images = new Image[Count];
        private readonly float[] age = new float[Count];
        private int next;

        public HudDamageRing(Transform canvas)
        {
            RectTransform layer = UIFactory.Centre(canvas, "Damage Ring", Vector2.zero, Vector2.zero);
            for (int i = 0; i < Count; i++)
            {
                wedges[i] = UIFactory.Centre(layer, "Wedge", Vector2.zero, new Vector2(190f, 84f));
                images[i] = UIFactory.Image(wedges[i], new Color(1f, 0.2f, 0.18f, 0f), UISkin.Wedge);
                age[i] = Life;
            }
        }

        /// <summary>Shows a wedge towards a world position, as seen from the camera.</summary>
        public void Hit(Vector3 source, Transform cam)
        {
            if (cam == null) return;
            Vector3 local = cam.InverseTransformPoint(source);
            var flat = new Vector2(local.x, local.z);
            if (flat.sqrMagnitude < 0.01f) flat = Vector2.up;
            float angle = Mathf.Atan2(flat.x, flat.y) * Mathf.Rad2Deg;   // 0 ahead, 90 to the right
            int i = next;
            next = (next + 1) % Count;
            wedges[i].localRotation = Quaternion.Euler(0f, 0f, -angle);
            wedges[i].anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad)) * Radius;
            age[i] = 0f;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < Count; i++)
            {
                if (age[i] >= Life) continue;
                age[i] += dt;
                Color c = images[i].color;
                c.a = Mathf.Clamp01(1f - age[i] / Life) * 0.9f;
                images[i].color = c;
            }
        }
    }
}
