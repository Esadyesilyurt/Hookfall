// Hookfall - glowing rings that keep flowing along a wind feature (up an updraft, along an air channel), fading in at
// the start and out at the end, so the direction of the wind reads from far away. Built by SkyFeatures.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    public class WindRings : MonoBehaviour
    {
        private LineRenderer[] rings;
        private float length, speed;
        private Color color;

        /// <summary>The rings travel along local +Z from 0 to length at this speed.</summary>
        public void Init(LineRenderer[] lines, float travel, float flowSpeed, Color tint)
        {
            rings = lines;
            length = travel;
            speed = flowSpeed;
            color = tint;
        }

        private void Update()
        {
            if (rings == null) return;
            for (int i = 0; i < rings.Length; i++)
            {
                float f = Mathf.Repeat(Time.time * speed / length + (float)i / rings.Length, 1f);
                rings[i].transform.localPosition = Vector3.forward * (f * length);
                Color c = color;
                c.a *= Mathf.Sin(f * Mathf.PI);
                rings[i].startColor = rings[i].endColor = c;
            }
        }
    }
}
