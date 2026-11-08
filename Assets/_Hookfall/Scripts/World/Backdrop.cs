// Hookfall - far scenery beyond the storm wall: huge floating spires, the broken fortress and a fallen arch,
// drawn without fog in pale haze colours so they read as distant silhouettes the player can never reach.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
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
        }

        private void Update() => transform.Rotate(Vector3.up, driftSpeed * Time.deltaTime, Space.World);
    }
}
