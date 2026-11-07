// Hookfall - the edge of the arena: a slowly turning wall of storm clouds where the Heartstone's pull ends.
// Flying into it, the wind pushes the Warden back towards the tower; lightning flickers inside it.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using UnityEngine;

namespace Hookfall.World
{
    public class StormWall : MonoBehaviour
    {
        [SerializeField] private Rigidbody player;
        [SerializeField] private GameObject[] cloudPrefabs;
        [SerializeField] private Material lightningMaterial;

        [Header("Shape")]
        [SerializeField] private float radius = 195f;
        [SerializeField] private int cloudsPerLayer = 24;
        [Tooltip("Cloud layers stop below the horizon line seen from the islands, so the sky and far scenery stay visible above.")]
        [SerializeField] private float[] layerHeights = { -120f, -80f, -40f, 0f, 30f };
        [SerializeField] private Vector2 cloudScale = new Vector2(6f, 10f);
        [SerializeField] private float turnSpeed = 1.5f;

        [Header("Wind")]
        [Tooltip("The wind starts pushing at this distance from the tower (horizontal).")]
        [SerializeField] private float pushFrom = 170f;
        [Tooltip("Inward acceleration at the wall, in m/s².")]
        [SerializeField] private float pushStrength = 80f;

        [Header("Lightning")]
        [SerializeField] private Vector2 lightningInterval = new Vector2(2.5f, 7f);
        [SerializeField] private float lightningTime = 0.18f;

        private Transform ring;
        private LineRenderer bolt;
        private Light flash;
        private float nextBolt;
        private float boltTimer;

        public static float Pressure { get; private set; }
        public float Radius => radius;

        private void Awake()
        {
            Pressure = 0f;
            ring = new GameObject("Storm Ring").transform;
            ring.SetParent(transform, false);
            if (cloudPrefabs != null && cloudPrefabs.Length > 0)
            {
                // Seeded so the wall looks the same every run; it is scenery, not layout.
                var rnd = new System.Random(7);
                for (int layer = 0; layer < layerHeights.Length; layer++)
                    for (int i = 0; i < cloudsPerLayer; i++)
                    {
                        float a = (i + (float)rnd.NextDouble() * 0.6f + layer * 0.5f) / cloudsPerLayer * Mathf.PI * 2f;
                        float r = radius + (float)rnd.NextDouble() * 18f;
                        GameObject cloud = Instantiate(cloudPrefabs[rnd.Next(cloudPrefabs.Length)], ring);
                        cloud.transform.localPosition = new Vector3(Mathf.Cos(a) * r, layerHeights[layer] + (float)rnd.NextDouble() * 14f, Mathf.Sin(a) * r);
                        cloud.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                        float s = Mathf.Lerp(cloudScale.x, cloudScale.y, (float)rnd.NextDouble());
                        cloud.transform.localScale = new Vector3(s, s * 1.3f, s);
                    }
            }

            var boltGo = new GameObject("Lightning") { layer = Layers.NoOutline };
            boltGo.transform.SetParent(transform, false);
            bolt = boltGo.AddComponent<LineRenderer>();
            bolt.sharedMaterial = lightningMaterial;
            bolt.positionCount = 10;
            bolt.startWidth = 1.2f;
            bolt.endWidth = 0.3f;
            bolt.startColor = bolt.endColor = new Color(0.8f, 0.9f, 1f);
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bolt.enabled = false;
            flash = boltGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.range = 160f;
            flash.color = new Color(0.75f, 0.85f, 1f);
            flash.intensity = 0f;
            nextBolt = Random.Range(lightningInterval.x, lightningInterval.y);
        }

        private void Update()
        {
            ring.Rotate(Vector3.up, turnSpeed * Time.deltaTime, Space.World);

            nextBolt -= Time.deltaTime;
            if (nextBolt <= 0f)
            {
                nextBolt = Random.Range(lightningInterval.x, lightningInterval.y);
                Strike();
            }
            if (boltTimer > 0f)
            {
                boltTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(boltTimer / lightningTime);
                flash.intensity = 40f * t;
                if (boltTimer <= 0f) bolt.enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (player == null) return;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float d = offset.magnitude;
            Pressure = Mathf.Clamp01((d - pushFrom) / (radius - pushFrom));
            if (Pressure <= 0f) return;

            Vector3 inward = -offset / d;
            player.AddForce(inward * (pushStrength * Pressure * Pressure), ForceMode.Acceleration);
            // Inside the wall itself, outward speed is taken away so nobody can punch through.
            if (d > radius)
            {
                float outward = Vector3.Dot(player.linearVelocity, -inward);
                if (outward > 0f) player.linearVelocity += inward * outward;
            }
        }

        private void Strike()
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 top = new Vector3(Mathf.Cos(a) * radius, Random.Range(40f, 100f), Mathf.Sin(a) * radius);
            Vector3 bottom = top + new Vector3(Random.Range(-15f, 15f), -Random.Range(60f, 110f), Random.Range(-15f, 15f));
            for (int i = 0; i < bolt.positionCount; i++)
            {
                float t = i / (bolt.positionCount - 1f);
                Vector3 p = Vector3.Lerp(top, bottom, t);
                if (i > 0 && i < bolt.positionCount - 1) p += Random.insideUnitSphere * 6f;
                bolt.SetPosition(i, transform.position + p);
            }
            flash.transform.position = transform.position + (top + bottom) * 0.5f;
            bolt.enabled = true;
            boltTimer = lightningTime;
        }
    }
}
