// Hookfall - the Gloom Jelly (added 2026-11-10): a slow sky jellyfish with a lot of health. It does not keep up with
// the Warden; it drifts at them and hangs in their way, and anything within reach of its bell gets a shock after a
// short red warning. Killed, it breaks into three small, quick young. The horde's area denial: fly round it, or burn
// it down from range. Two states: Drift and Pulse.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class GloomJelly : EnemyBase
    {
        [Header("Gloom Jelly")]
        [Tooltip("The shock reaches this far from the jelly.")]
        [SerializeField] private float pulseRange = 7f;
        [SerializeField] private float pulseWindUp = 0.4f;
        [SerializeField] private float pulseDamage = 14f;
        [SerializeField] private float pulseCooldown = 2.6f;
        [Header("Young")]
        [Tooltip("How many young a killed jelly leaves.")]
        [SerializeField] private int young = 3;
        [SerializeField] private float youngScale = 0.45f;
        [Tooltip("A young jelly's health and shock as a share of the parent's.")]
        [SerializeField] private float youngStrength = 0.15f;
        [SerializeField] private float youngSpeed = 2.2f;
        [Tooltip("Tentacles of the model; they sway and trail behind as it moves.")]
        [SerializeField] private Transform[] tentacles = new Transform[0];

        private Quaternion[] rest;
        private float phase;

        public JellyDriftState Drift { get; private set; }
        public JellyPulseState Pulse { get; private set; }

        /// <summary>One of the small jellies a big one leaves behind: quick, weak, and it leaves none itself.</summary>
        public bool IsYoung { get; private set; }
        public float PulseRange => pulseRange * SizeFactor;
        public float PulseWindUp => pulseWindUp;
        public float DriftSpeed => MoveSpeed * (IsYoung ? youngSpeed : 1f);
        public float CooldownLeft { get; set; }

        protected override IEnemyState DefaultState => Drift;

        protected override void Awake()
        {
            base.Awake();
            rest = new Quaternion[tentacles.Length];
            for (int i = 0; i < tentacles.Length; i++) rest[i] = tentacles[i] != null ? tentacles[i].localRotation : Quaternion.identity;
            phase = Random.value * 10f;
        }

        protected override void BuildStates()
        {
            Drift = new JellyDriftState(this);
            Pulse = new JellyPulseState(this);
        }

        protected override void OnSpawned()
        {
            IsYoung = false;
            CooldownLeft = 1f;
        }

        /// <summary>Turns a freshly spawned jelly into one of the young.</summary>
        public void MakeYoung()
        {
            IsYoung = true;
            MakeLesser(youngStrength, youngScale, 0.2f);
        }

        protected override void OnDied(bool killed)
        {
            if (killed && !IsYoung && Director != null) Director.SpawnYoung(this, young);
        }

        protected override void FrameUpdate(float deltaTime)
        {
            CooldownLeft -= deltaTime;
            // The tentacles sway each on its own beat and trail behind the way the jelly moves.
            Vector3 drag = transform.InverseTransformDirection(Velocity) * 2.5f;
            float time = Time.time + phase;
            for (int i = 0; i < tentacles.Length; i++)
            {
                if (tentacles[i] == null) continue;
                float x = Mathf.Sin(time * 1.7f + i * 1.1f) * 12f - Mathf.Clamp(drag.z, -30f, 30f);
                float z = Mathf.Cos(time * 1.3f + i * 0.7f) * 12f + Mathf.Clamp(drag.x, -30f, 30f);
                tentacles[i].localRotation = rest[i] * Quaternion.Euler(x, 0f, z);
            }
        }

        /// <summary>The shock: hurts the Warden if they are within reach.</summary>
        public void Shock()
        {
            CooldownLeft = pulseCooldown;
            if (PlayerStats != null && DistanceToPlayer <= PulseRange)
                PlayerStats.TakeDamageFrom(pulseDamage * DamageScale * (IsYoung ? 0.5f : 1f), transform.position);
            EnemyDirector.RaisePulse(transform.position, PulseRange);
        }
    }
}
