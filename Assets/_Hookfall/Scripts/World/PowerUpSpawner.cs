// Hookfall - power-ups in the open sky (2026-11-08): every 18-30 s one appears 20-40 m from the Warden (at most 3 at
// once, gone after 45 s); golems drop one 40% of the time. Touching one uses it at once:
// Treasure Gale pulls every gem and coin to you, Healing Dew heals 60%, Berserk Rage doubles your fury for 10 s,
// Timefrost freezes every enemy for 5 s, and the very rare Heartstone Nova destroys every enemy in the sky.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using Hookfall.Audio;
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Feel;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using Hookfall.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hookfall.World
{
    public class PowerUpSpawner : MonoBehaviour
    {
        public static PowerUpSpawner Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private VfxManager vfx;
        [SerializeField] private FeelDirector feel;
        [SerializeField] private Material particleMaterial;
        [Tooltip("Orb material per kind, in PowerUpKind order.")]
        [SerializeField] private Material[] orbMaterials = new Material[5];

        [Header("Spawning")]
        [SerializeField] private Vector2 interval = new Vector2(18f, 30f);
        [SerializeField] private Vector2 distance = new Vector2(20f, 40f);
        [SerializeField] private int maxAlive = 3;
        [SerializeField] private float lifetime = 45f;
        [SerializeField] private float touchRadius = 3f;
        [Tooltip("Chance weights in PowerUpKind order: Treasure Gale, Healing Dew, Berserk Rage, Timefrost, Heartstone Nova.")]
        [SerializeField] private float[] weights = { 28f, 24f, 22f, 20f, 4f };
        [SerializeField, Range(0f, 1f)] private float golemDropChance = 0.4f;

        [Header("Effects")]
        [SerializeField] private float healShare = 0.6f;
        [SerializeField] private float rageSeconds = 10f;
        [SerializeField] private float freezeSeconds = 5f;

        private readonly List<PowerUp> alive = new List<PowerUp>();
        private float timer;

        private void Awake()
        {
            Instance = this;
            timer = Random.Range(interval.x, interval.y);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (player == null) return;
            alive.RemoveAll(p => p == null);
            if ((timer -= Time.deltaTime) > 0f) return;
            timer = Random.Range(interval.x, interval.y);
            if (alive.Count >= maxAlive) return;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 d = Random.insideUnitCircle.normalized * Random.Range(distance.x, distance.y);
                Vector3 at = player.transform.position + new Vector3(d.x, Random.Range(-4f, 10f), d.y);
                if (Physics.CheckSphere(at, 3f, 1 << Layers.World, QueryTriggerInteraction.Ignore)) continue;
                SpawnAt(at, RollKind());
                return;
            }
        }

        public PowerUpKind RollKind()
        {
            float total = 0f;
            foreach (float w in weights) total += w;
            float roll = Random.value * total;
            for (int i = 0; i < weights.Length; i++)
                if ((roll -= weights[i]) <= 0f) return (PowerUpKind)i;
            return PowerUpKind.TreasureGale;
        }

        /// <summary>A golem fell: sometimes it leaves a power-up where it stood (a reward for fighting on its island).</summary>
        public void GolemDrop(Vector3 at)
        {
            if (Random.value < golemDropChance) SpawnAt(at + Vector3.up * 3f, RollKind());
        }

        public PowerUp SpawnAt(Vector3 position, PowerUpKind kind)
        {
            var root = new GameObject("Power-up " + kind);
            root.transform.position = position;
            Color color = ColorOf(kind);

            GameObject orb = GameObject.CreatePrimitive(kind == PowerUpKind.HeartstoneNova ? PrimitiveType.Sphere : PrimitiveType.Cube);
            DestroyImmediate(orb.GetComponent<Collider>());
            orb.name = "Orb";
            orb.transform.SetParent(root.transform, false);
            orb.transform.localScale = Vector3.one * (kind == PowerUpKind.HeartstoneNova ? 1.8f : 1.1f);
            orb.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var renderer = orb.GetComponent<Renderer>();
            Material material = orbMaterials != null && (int)kind < orbMaterials.Length ? orbMaterials[(int)kind] : null;
            if (material != null) renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var ringGo = new GameObject("Ring") { layer = Layers.NoOutline };
            ringGo.transform.SetParent(orb.transform, false);
            var ring = ringGo.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI * 2f / 32f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.4f);
            }
            if (particleMaterial != null)
            {
                ring.material = new Material(particleMaterial);
                ring.material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }
            ring.widthMultiplier = 0.12f;
            ring.startColor = ring.endColor = color;
            ring.shadowCastingMode = ShadowCastingMode.Off;

            var labelGo = new GameObject("Label") { layer = Layers.NoOutline };
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = Vector3.up * 2.2f;
            var label = labelGo.AddComponent<TextMeshPro>();
            label.text = NameOf(kind);
            label.fontSize = 6f;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(12f, 2f);

            var power = root.AddComponent<PowerUp>();
            power.Init(this, player, kind, orb.transform, labelGo.transform, touchRadius, lifetime);
            alive.Add(power);
            return power;
        }

        /// <summary>The Warden touched a power-up: its effect now.</summary>
        public void Use(PowerUpKind kind, Vector3 at)
        {
            var stats = player != null ? player.GetComponent<PlayerStats>() : null;
            switch (kind)
            {
                case PowerUpKind.TreasureGale:
                    if (PickupSpawner.Instance != null) PickupSpawner.Instance.MagnetiseAll();
                    break;
                case PowerUpKind.HealingDew:
                    if (stats != null) stats.Heal(stats.MaxHealth * healShare);
                    break;
                case PowerUpKind.BerserkRage:
                    var synergy = player != null ? player.GetComponent<SynergySystem>() : null;
                    if (synergy != null) synergy.StartFrenzy(rageSeconds);
                    break;
                case PowerUpKind.Timefrost:
                    foreach (EnemyBase enemy in EnemyBase.Active.ToArray()) enemy.Freeze(freezeSeconds);
                    break;
                case PowerUpKind.HeartstoneNova:
                    foreach (EnemyBase enemy in EnemyBase.Active.ToArray()) enemy.Die(true);
                    break;
            }
            HUD.Announce(NameOf(kind), ColorOf(kind));
            if (vfx != null) vfx.Ring(at, kind == PowerUpKind.HeartstoneNova ? 80 : 30, ColorOf(kind), kind == PowerUpKind.HeartstoneNova ? 40f : 12f);
            if (feel != null) feel.Shake(kind == PowerUpKind.HeartstoneNova ? 1.2f : 0.25f);
            if (audioManager != null) audioManager.Play(kind == PowerUpKind.HeartstoneNova ? SoundId.EliteWave : SoundId.LevelUp, at);
        }

        public static string NameOf(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.TreasureGale: return Loc.T("Treasure Gale");
                case PowerUpKind.HealingDew: return Loc.T("Healing Dew");
                case PowerUpKind.BerserkRage: return Loc.T("Berserk Rage");
                case PowerUpKind.Timefrost: return Loc.T("Timefrost");
                default: return Loc.T("Heartstone Nova");
            }
        }

        public static Color ColorOf(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.TreasureGale: return new Color(1f, 0.82f, 0.25f);
                case PowerUpKind.HealingDew: return new Color(0.45f, 1f, 0.5f);
                case PowerUpKind.BerserkRage: return new Color(1f, 0.25f, 0.3f);
                case PowerUpKind.Timefrost: return new Color(0.5f, 0.85f, 1f);
                default: return new Color(0.75f, 1f, 1f);
            }
        }
    }
}
