// Hookfall - the Venomwing (added 2026-11-10): a wasp that does not close in. It flies alongside the Warden at a
// distance, on their flank and a little above (offset pursuit on the Warden's own velocity), and spits venom at where
// they will be. It keeps pace like the rest of the horde and never falls back: the way to deal with it is to turn on
// it, or to keep moving so the slow spit misses. Two states: Flank and Spit (a short red wind-up, GDD 5.6).
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Venomwing : EnemyBase
    {
        [Header("Venomwing")]
        [Tooltip("How far to the side of the Warden it holds its place.")]
        [SerializeField] private float holdDistance = 18f;
        [SerializeField] private float holdHeight = 4f;
        [Tooltip("It spits when the Warden is within this distance.")]
        [SerializeField] private float spitRange = 30f;
        [SerializeField] private float windUp = 0.35f;
        [Tooltip("Wind-up while the Warden stands on an island.")]
        [SerializeField] private float windUpOnIsland = 0.2f;
        [SerializeField] private float spitCooldown = 2.4f;
        [SerializeField] private float spitSpeed = 26f;
        [SerializeField] private float spitDamage = 10f;
        [Tooltip("Random aim error in metres.")]
        [SerializeField] private float aimError = 1.2f;
        [SerializeField] private Boulder spitPrefab;
        [Tooltip("The abdomen: it curls forward while the wasp winds up.")]
        [SerializeField] private Transform tail;

        private Quaternion tailRest = Quaternion.identity;
        private float curl, side = 1f;

        public VenomFlankState Flank { get; private set; }
        public VenomSpitState Spit { get; private set; }

        public float SpitRange => spitRange;
        public float WindUp => Player != null && !Player.IsAirborne ? windUpOnIsland : windUp;
        /// <summary>Seconds until it may spit again.</summary>
        public float CooldownLeft { get; set; }
        /// <summary>1 while winding up: the abdomen curls forward.</summary>
        public float TailCurl { get; set; }

        protected override IEnemyState DefaultState => Flank;

        protected override void Awake()
        {
            base.Awake();
            if (tail != null) tailRest = tail.localRotation;
        }

        protected override void BuildStates()
        {
            Flank = new VenomFlankState(this);
            Spit = new VenomSpitState(this);
        }

        protected override void OnSpawned()
        {
            side = Random.value < 0.5f ? -1f : 1f;
            CooldownLeft = Random.Range(0.6f, spitCooldown);
            TailCurl = curl = 0f;
        }

        protected override void FrameUpdate(float deltaTime)
        {
            CooldownLeft -= deltaTime;
            if (tail == null) return;
            curl = Mathf.MoveTowards(curl, TailCurl, deltaTime * 6f);
            tail.localRotation = tailRest * Quaternion.Euler(Mathf.Lerp(0f, -55f, curl), 0f, 0f);
        }

        /// <summary>Its place beside the Warden: off to one side of their flight, a little above and ahead.</summary>
        public Vector3 FlankPoint()
        {
            Vector3 velocity = Player.Body.linearVelocity;
            Vector3 forward = velocity.sqrMagnitude > 4f ? velocity.normalized : Vector3.ProjectOnPlane(Player.transform.position - transform.position, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return Player.transform.position + right * (side * holdDistance) + Vector3.up * holdHeight + forward * 4f;
        }

        /// <summary>Spits at where the Warden will be when the venom gets there (with a little error).</summary>
        public void Fire()
        {
            CooldownLeft = Player.IsAirborne ? spitCooldown : spitCooldown * 0.5f;
            if (Director == null || spitPrefab == null) return;
            Vector3 mouth = transform.position + transform.forward * (0.8f * SizeFactor);
            float travel = Vector3.Distance(mouth, Player.transform.position) / spitSpeed;
            Vector3 target = PredictedPlayerPosition(travel * 0.8f) + Random.insideUnitSphere * aimError;
            Director.SpawnShot(spitPrefab, mouth, (target - mouth).normalized * spitSpeed, spitDamage * DamageScale);
            Jab(-0.5f);   // recoil
        }
    }
}
