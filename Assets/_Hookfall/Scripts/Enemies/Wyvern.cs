// Hookfall - the Wyvern: heavy flyer with Chase, Charge (short red wind-up, then a straight dash) and Recover (GDD 5.4).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Wyvern : EnemyBase
    {
        [Header("Wyvern")]
        [SerializeField] private float chargeRange = 15f;
        [SerializeField] private float windUp = 0.3f;
        [Tooltip("Wind-up when the player stands on an island; the wyvern dives from above.")]
        [SerializeField] private float windUpOnIsland = 0.15f;
        [SerializeField] private float dashSpeed = 30f;
        [SerializeField] private float dashDistance = 24f;
        [SerializeField] private float recoverTime = 0.5f;
        [SerializeField] private float recoverSpeedFactor = 0.3f;
        [Tooltip("Height above an island-bound player the wyvern climbs to before diving.")]
        [SerializeField] private float diveHeight = 10f;
        [SerializeField] private float pursuitLookAhead = 0.6f;
        [SerializeField] private float dashHitDistance = 2f;

        public WyvernChaseState Chase { get; private set; }
        public WyvernChargeState Charge { get; private set; }
        public WyvernRecoverState Recover { get; private set; }

        public float ChargeRange => chargeRange;
        public float WindUp => windUp;
        public float WindUpOnIsland => windUpOnIsland;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDistance / Mathf.Max(1f, dashSpeed);
        public float RecoverTime => recoverTime;
        public float RecoverSpeedFactor => recoverSpeedFactor;
        public float DiveHeight => diveHeight;
        public float PursuitLookAhead => pursuitLookAhead;

        /// <summary>True while dashing and the hit has not landed yet.</summary>
        public bool DashArmed { get; set; }
        /// <summary>True during the wind-up before a dash: it rears up and beats its wings wide.</summary>
        public bool WindingUp { get; set; }

        // In the dash the wings are swept up and held; before it they beat wide and the body rears.
        protected override float FlapScale => DashArmed ? 0.12f : WindingUp ? 1.7f : 1f;
        protected override float WingRest => DashArmed ? 38f : 0f;
        protected override float Pitch => WindingUp ? -28f : DashArmed ? 8f : 0f;

        protected override IEnemyState DefaultState => Chase;

        /// <summary>Wyverns fly up to the boss arena; the bats stay below it (2026-11-08).</summary>
        public override bool CanEnterBossZone => true;

        protected override void BuildStates()
        {
            Chase = new WyvernChaseState(this);
            Charge = new WyvernChargeState(this);
            Recover = new WyvernRecoverState(this);
        }

        protected override void OnSpawned() => DashArmed = WindingUp = false;

        // The dash covers 3 m per think, so the hit is checked every frame.
        protected override void FrameUpdate(float deltaTime)
        {
            if (!DashArmed || PlayerStats == null) return;
            if (DistanceToPlayer > dashHitDistance) return;
            PlayerStats.TakeDamageFrom(Damage, transform.position);
            DashArmed = false;
        }
    }
}
