// Hookfall - visual effects (GDD 8.1): hit sparks, enemy deaths, boulder dust, golem slams, pickup and chest bursts,
// speed lines and a trail at high speed, and drifting motes in the tower's low-gravity circle.
// One world-space particle system emits every burst, so hundreds of effects cost one draw call.
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using Hookfall.UI;
using Hookfall.World;
using UnityEngine;

namespace Hookfall.VFX
{
    public class VfxManager : MonoBehaviour
    {
        [SerializeField] private Material particleMaterial;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PickupSpawner pickups;
        [SerializeField] private ChestSpawner chests;
        [SerializeField] private TowerCache towerCache;

        [Header("Limits")]
        [Tooltip("Hit sparks per frame; fewer when the frame rate is low (GDD 5.7).")]
        [SerializeField] private int maxHitSparksPerFrame = 24;

        [Header("Speed")]
        [SerializeField] private float speedLinesFrom = 18f;
        [SerializeField] private float speedLinesFull = 35f;
        [SerializeField] private float maxSpeedLineRate = 90f;
        [SerializeField] private float trailFrom = 14f;

        [Header("Colours (GDD 8.1 palette)")]
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField] private Color enemyColor = new Color(0.78f, 0.3f, 0.9f);
        [SerializeField] private Color rewardColor = new Color(1f, 0.75f, 0.25f);
        [SerializeField] private Color playerColor = new Color(0.3f, 0.95f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color stoneColor = new Color(0.6f, 0.57f, 0.55f);

        private ParticleSystem bursts;
        private ParticleSystem speedLines;
        private ParticleSystem.EmissionModule speedEmission;
        private TrailRenderer trail;
        private int hitsThisFrame;
        private PlayerStats stats;

        private void Awake()
        {
            bursts = CreateSystem("Bursts", transform, ParticleSystemSimulationSpace.World, 4000);
            ParticleSystem.MainModule main = bursts.main;
            main.gravityModifier = 0.6f;

            if (cameraTransform != null) CreateSpeedLines();
            if (player != null)
            {
                stats = player.GetComponent<PlayerStats>();
                CreateTrail();
            }
            CreateMotes();
        }

        private void OnEnable()
        {
            EnemyDirector.Hit += OnHit;
            EnemyDirector.Died += OnDied;
            EnemyDirector.Slammed += OnSlam;
            EnemyDirector.BoulderBroke += OnBoulder;
            if (pickups != null) pickups.Collected += OnCollected;
            if (chests != null) chests.ChestOpened += OnChest;
            if (towerCache != null) towerCache.Opened += OnCache;
            if (stats != null) stats.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            EnemyDirector.Hit -= OnHit;
            EnemyDirector.Died -= OnDied;
            EnemyDirector.Slammed -= OnSlam;
            EnemyDirector.BoulderBroke -= OnBoulder;
            if (pickups != null) pickups.Collected -= OnCollected;
            if (chests != null) chests.ChestOpened -= OnChest;
            if (towerCache != null) towerCache.Opened -= OnCache;
            if (stats != null) stats.Damaged -= OnDamaged;
        }

        private void LateUpdate()
        {
            hitsThisFrame = 0;
            if (player == null) return;
            float speed = player.Speed;
            bool fx = GameSettings.SpeedEffects;
            if (speedLines != null)
                speedEmission.rateOverTime = fx ? Mathf.InverseLerp(speedLinesFrom, speedLinesFull, speed) * maxSpeedLineRate : 0f;
            if (trail != null) trail.emitting = fx && speed > trailFrom;
        }

        // ---------- Event handlers ----------

        private void OnHit(EnemyBase enemy, float damage)
        {
            int limit = EnemyDirector.LowPerformance ? maxHitSparksPerFrame / 4 : maxHitSparksPerFrame;
            if (++hitsThisFrame > limit) return;
            Burst(enemy.transform.position, 3, hitColor, 6f, 0.12f, 0.2f);
        }

        private void OnDied(EnemyBase enemy, bool killed)
        {
            if (!killed) return;
            int count = enemy.IsElite ? 30 : enemy is Golem ? 24 : 10;
            Burst(enemy.transform.position, count, enemyColor, 7f, 0.25f, 0.45f);
        }

        private void OnSlam(Golem golem) => Ring(golem.transform.position + Vector3.up * 0.2f, 28, stoneColor, 9f);
        private void OnBoulder(Vector3 position) => Burst(position, 10, stoneColor, 6f, 0.25f, 0.5f);
        private void OnCollected(PickupKind kind) => Burst(player.transform.position, 3, rewardColor, 3f, 0.12f, 0.3f);
        private void OnChest(Vector3 position, Rarity rarity) => Burst(position + Vector3.up, 40, UITheme.ForRarity(rarity), 8f, 0.25f, 0.9f);
        private void OnCache(Vector3 position, Rarity rarity) => Burst(position + Vector3.up, 80, UITheme.ForRarity(rarity), 12f, 0.3f, 1.2f);
        private void OnDamaged(float amount) => Burst(player.transform.position, 8, dangerColor, 5f, 0.15f, 0.35f);

        // ---------- Emitting ----------

        private void Burst(Vector3 position, int count, Color color, float speed, float size, float life)
        {
            var p = new ParticleSystem.EmitParams { position = position, startColor = color, startLifetime = life, applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                p.velocity = Random.onUnitSphere * (speed * Random.Range(0.4f, 1f));
                p.startSize = size * Random.Range(0.6f, 1.4f);
                bursts.Emit(p, 1);
            }
        }

        private void Ring(Vector3 position, int count, Color color, float speed)
        {
            var p = new ParticleSystem.EmitParams { position = position, startColor = color, startLifetime = 0.5f, startSize = 0.35f };
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                p.velocity = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * speed;
                bursts.Emit(p, 1);
            }
        }

        // ---------- Setup ----------

        private ParticleSystem CreateSystem(string name, Transform parent, ParticleSystemSimulationSpace space, int maxParticles)
        {
            var go = new GameObject(name) { layer = Layers.NoOutline };
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = space;
            main.maxParticles = maxParticles;
            main.playOnAwake = false;
            main.startSpeed = 0f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private void CreateSpeedLines()
        {
            speedLines = CreateSystem("Speed Lines", cameraTransform, ParticleSystemSimulationSpace.Local, 400);
            speedLines.transform.localPosition = new Vector3(0f, 0f, 14f);
            ParticleSystem.MainModule main = speedLines.main;
            main.startLifetime = 0.35f;
            main.startSpeed = -45f;
            main.startSize = 0.06f;
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            ParticleSystem.ShapeModule shape = speedLines.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 7f;
            shape.radiusThickness = 0.2f;   // edge of the circle: lines stream past the screen edges
            var renderer = speedLines.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.08f;
            renderer.lengthScale = 2f;
            speedEmission = speedLines.emission;
        }

        private void CreateTrail()
        {
            var go = new GameObject("Speed Trail") { layer = Layers.NoOutline };
            go.transform.SetParent(player.transform, false);
            trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = particleMaterial;
            trail.time = 0.35f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 0f);
            trail.startColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.7f);
            trail.endColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
        }

        /// <summary>Slow cyan motes rising inside the Heartstone's low-gravity circle (GDD 7.1).</summary>
        private void CreateMotes()
        {
            ParticleSystem motes = CreateSystem("Gravity Motes", transform, ParticleSystemSimulationSpace.World, 600);
            ParticleSystem.MainModule main = motes.main;
            main.startLifetime = 7f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.6f);
            ParticleSystem.ShapeModule shape = motes.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 40f;
            shape.rotation = new Vector3(90f, 0f, 0f);   // lie flat around the tower
            ParticleSystem.VelocityOverLifetimeModule velocity = motes.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            ParticleSystem.EmissionModule emission = motes.emission;
            emission.rateOverTime = 22f;
            motes.transform.position = new Vector3(0f, -12f, 0f);
        }
    }
}
