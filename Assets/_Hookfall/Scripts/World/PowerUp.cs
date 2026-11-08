// Hookfall - one floating power-up: bobs and turns in the air with its name above it, blinks before it fades, and
// is used by PowerUpSpawner the moment the Warden touches it.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class PowerUp : MonoBehaviour
    {
        private PowerUpSpawner spawner;
        private PlayerController player;
        private Transform orb, label;
        private float radius, life, age;
        private Vector3 basePosition;

        public PowerUpKind Kind { get; private set; }

        public void Init(PowerUpSpawner owner, PlayerController target, PowerUpKind kind, Transform visual, Transform name, float touchRadius, float lifetime)
        {
            spawner = owner;
            player = target;
            Kind = kind;
            orb = visual;
            label = name;
            radius = touchRadius;
            life = lifetime;
            basePosition = transform.position;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age > life) { Destroy(gameObject); return; }
            transform.position = basePosition + Vector3.up * (Mathf.Sin(age * 2f) * 0.4f);
            orb.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            bool visible = life - age > 5f || Mathf.Repeat(age * 6f, 1f) > 0.35f;   // blink in the last 5 s
            orb.gameObject.SetActive(visible);
            Camera cam = Camera.main;
            if (label != null && cam != null) label.rotation = cam.transform.rotation;
            if (player != null && (player.Body.position - transform.position).sqrMagnitude <= radius * radius)
            {
                spawner.Use(Kind, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
