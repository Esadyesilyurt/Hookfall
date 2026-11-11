// Hookfall - the sky of each stage (added 2026-11-10): the first stage keeps the calm daylight of the GDD (8.1), and
// every stage after it has its own light, so going through the portal looks like going somewhere: a golden evening,
// a violet dusk, a moonlit night, a red storm, and round again. Sets the sun, the ambient light, the fog and the
// camera's sky colour, and turns smoothly from one to the next.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using UnityEngine;

namespace Hookfall.World
{
    public class Atmosphere : MonoBehaviour
    {
        [Serializable]
        public struct Sky
        {
            public string name;
            public Color sky;
            public Color ambient;
            public Color sun;
            public float sunIntensity;
            [Tooltip("The sun's rotation: x is its height above the horizon, y its bearing.")]
            public Vector2 sunAngles;
        }

        [Tooltip("Stage 1 uses the first sky, stage 2 the second and so on; after the last it starts again at the second.")]
        [SerializeField] private Sky[] skies =
        {
            new Sky { name = "Calm day", sky = new Color(0.55f, 0.76f, 0.93f), ambient = new Color(0.55f, 0.6f, 0.68f), sun = Color.white, sunIntensity = 1.2f, sunAngles = new Vector2(50f, -30f) },
            new Sky { name = "Golden evening", sky = new Color(0.98f, 0.74f, 0.52f), ambient = new Color(0.64f, 0.53f, 0.5f), sun = new Color(1f, 0.8f, 0.56f), sunIntensity = 1.25f, sunAngles = new Vector2(20f, -62f) },
            new Sky { name = "Violet dusk", sky = new Color(0.5f, 0.43f, 0.74f), ambient = new Color(0.47f, 0.43f, 0.62f), sun = new Color(0.95f, 0.66f, 0.82f), sunIntensity = 1.05f, sunAngles = new Vector2(13f, -104f) },
            new Sky { name = "Moonlit night", sky = new Color(0.11f, 0.16f, 0.33f), ambient = new Color(0.27f, 0.31f, 0.46f), sun = new Color(0.6f, 0.72f, 1f), sunIntensity = 0.85f, sunAngles = new Vector2(42f, 140f) },
            new Sky { name = "Red storm", sky = new Color(0.44f, 0.17f, 0.23f), ambient = new Color(0.52f, 0.36f, 0.4f), sun = new Color(1f, 0.55f, 0.48f), sunIntensity = 1.1f, sunAngles = new Vector2(30f, 24f) },
        };
        [Tooltip("Seconds a change of sky takes.")]
        [SerializeField] private float turnTime = 2.5f;

        private Sky from, to;
        private float t = 1f;
        private Camera cam;

        /// <summary>The sky's colour right now (the fog and the camera's background).</summary>
        public static Color SkyColor { get; private set; } = new Color(0.55f, 0.76f, 0.93f);

        private void Awake()
        {
            from = to = skies[0];
            SkyColor = skies[0].sky;   // static: a new run starts in daylight again
            StageDirector.StageStarted += OnStage;
        }

        private void OnDestroy() => StageDirector.StageStarted -= OnStage;

        /// <summary>The sky of a stage: the first for stage 1, then the others in turn, again and again.</summary>
        public Sky ForStage(int stage) =>
            stage <= 1 || skies.Length < 2 ? skies[0] : skies[1 + (stage - 2) % (skies.Length - 1)];

        private void OnStage(int stage)
        {
            from = Current();
            to = ForStage(stage);
            t = 0f;
        }

        private Sky Current() => new Sky
        {
            sky = Color.Lerp(from.sky, to.sky, t), ambient = Color.Lerp(from.ambient, to.ambient, t), sun = Color.Lerp(from.sun, to.sun, t),
            sunIntensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t), sunAngles = Vector2.Lerp(from.sunAngles, to.sunAngles, t)
        };

        private void LateUpdate()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / Mathf.Max(0.01f, turnTime));
            Sky now = Current();
            SkyColor = now.sky;
            RenderSettings.fogColor = now.sky;
            RenderSettings.ambientLight = now.ambient;
            if (cam == null) cam = Camera.main;
            if (cam != null) cam.backgroundColor = now.sky;
            Light sun = RenderSettings.sun;
            if (sun == null) return;
            sun.color = now.sun;
            sun.intensity = now.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(now.sunAngles.x, now.sunAngles.y, 0f);
        }
    }
}
