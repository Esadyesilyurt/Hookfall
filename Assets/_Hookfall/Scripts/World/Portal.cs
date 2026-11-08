// Hookfall - the portal the boss leaves behind (2026-11-08): it stands on the island that rises where the boss fell.
// Its Heartstone disc breathes, a beam marks it from across the map, and walking into the ring takes the Warden to
// the next stage (StageDirector).
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.World
{
    public class Portal : MonoBehaviour
    {
        [Tooltip("The glowing disc inside the ring (found by name when empty).")]
        [SerializeField] private Transform disc;
        [Tooltip("Height of the ring's centre above the island.")]
        [SerializeField] private float ringHeight = 3.6f;
        [Tooltip("The Warden enters within this distance of the ring's centre.")]
        [SerializeField] private float enterRadius = 3.4f;
        [SerializeField] private Material particleMaterial;

        private StageDirector stage;
        private PlayerController player;
        private Vector3 discScale = Vector3.one;
        private float age, surge;
        private bool used;

        /// <summary>The centre of the ring, where the Warden steps through.</summary>
        public Vector3 Centre => transform.position + Vector3.up * ringHeight;

        private void Awake()
        {
            if (disc == null) disc = transform.Find("Visual/Disc");
            if (disc != null) discScale = disc.localScale;
            if (particleMaterial != null) Swirl();
        }

        /// <summary>Sparks drawn in from a shell around the ring, so the portal looks like it breathes in.</summary>
        private void Swirl()
        {
            var go = new GameObject("Swirl") { layer = Layers.NoOutline };
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * ringHeight;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new Color(0.55f, 0.97f, 1f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 70f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 3.2f;
            shape.radiusThickness = 0f;
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.radial = new ParticleSystem.MinMaxCurve(-2.6f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(1.6f);
            ParticleSystem.ColorOverLifetimeModule fadeColor = ps.colorOverLifetime;
            fadeColor.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            fadeColor.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }

        public void Init(StageDirector owner, PlayerController target)
        {
            stage = owner;
            player = target;
        }

        /// <summary>The disc flares and spins up while the Warden is pulled through (portal cutscene).</summary>
        public void Surge(float amount) => surge = amount;

        private void Update()
        {
            age += Time.unscaledDeltaTime;
            if (disc != null)
            {
                float pulse = 1f + 0.05f * Mathf.Sin(age * 3f) + 0.25f * surge;
                disc.localScale = discScale * pulse;
            }
            if (used || player == null || Time.timeScale <= 0f) return;
            float distance = Vector3.Distance(player.Body.position, Centre);
            if (distance > enterRadius) return;
            used = true;
            stage.EnterPortal(this);
        }
    }
}
