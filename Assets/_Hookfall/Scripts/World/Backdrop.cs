// Hookfall - far scenery beyond the storm wall: huge floating spires, the broken fortress and a fallen arch,
// drawn without fog in pale haze colours so they read as distant silhouettes the player can never reach.
// They take the tint of the stage's sky (Atmosphere), or at night they would glow pale against the dark.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07; the sky tint on 2026-11-10.
// Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.World
{
    public class Backdrop : MonoBehaviour
    {
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private int count = 22;   // 14 at 430-600 m until the map doubled (2026-11-08)
        [SerializeField] private Vector2 radius = new Vector2(700f, 920f);
        [SerializeField] private Vector2 height = new Vector2(-160f, 120f);
        [SerializeField] private Vector2 scale = new Vector2(2.4f, 4.6f);
        [SerializeField] private float driftSpeed = 0.4f;

        private static readonly Color DaySky = new Color(0.55f, 0.76f, 0.93f);
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private Renderer[] renderers = new Renderer[0];
        private MaterialPropertyBlock block;
        private Color shownSky = DaySky;

        private void Awake()
        {
            if (prefabs == null || prefabs.Length == 0) return;
            for (int i = 0; i < count; i++)
            {
                // Evenly around the horizon with jitter, so no direction is empty.
                float a = (i + Random.Range(-0.3f, 0.3f)) / count * Mathf.PI * 2f;
                float r = Random.Range(radius.x, radius.y);
                GameObject piece = Instantiate(prefabs[i % prefabs.Length], transform);
                piece.transform.localPosition = new Vector3(Mathf.Cos(a) * r, Random.Range(height.x, height.y), Mathf.Sin(a) * r);
                piece.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                piece.transform.localScale = Vector3.one * Random.Range(scale.x, scale.y);
            }
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, driftSpeed * Time.deltaTime, Space.World);
            if (Atmosphere.SkyColor != shownSky) Tint(Atmosphere.SkyColor);
        }

        /// <summary>Relights the haze colours for another sky: each material's own colour times how the sky differs from day.</summary>
        private void Tint(Color sky)
        {
            shownSky = sky;
            var shift = new Color(sky.r / DaySky.r, sky.g / DaySky.g, sky.b / DaySky.b);
            foreach (Renderer r in renderers)
            {
                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty(BaseColor)) continue;
                    Color own = materials[i].GetColor(BaseColor);
                    r.GetPropertyBlock(block, i);
                    block.SetColor(BaseColor, new Color(Mathf.Min(1f, own.r * shift.r), Mathf.Min(1f, own.g * shift.g), Mathf.Min(1f, own.b * shift.b), own.a));
                    r.SetPropertyBlock(block, i);
                }
            }
        }
    }
}
