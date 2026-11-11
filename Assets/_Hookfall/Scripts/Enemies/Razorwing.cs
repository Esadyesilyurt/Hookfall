// Hookfall - the Razorwing (added 2026-11-10): a fast bird that hunts in a V formation. The flock cruises towards the
// Warden in formation (offset pursuit: every follower holds a slot behind the leader), breaks into an attack run when
// it is close, flies straight through the Warden's path with its wings as blades, overshoots, wheels round in a wide
// arc and comes again. It never hovers and never backs off: it only ever passes. Three states: Form, Strafe, Wheel.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Razorwing : EnemyBase
    {
        [Header("Razorwing")]
        [Tooltip("Speed of the attack run; never less than a little over the Warden's own speed.")]
        [SerializeField] private float strafeSpeed = 34f;
        [Tooltip("Inside this distance the flock leaves formation for its pass.")]
        [SerializeField] private float attackRange = 48f;
        [Tooltip("The wings cut when they pass this close.")]
        [SerializeField] private float hitDistance = 1.9f;
        [Tooltip("The run aims this far beyond the Warden, so the bird flies through and does not stop.")]
        [SerializeField] private float passBeyond = 26f;
        [Tooltip("Seconds it flies on after a pass before it turns back.")]
        [SerializeField] private float wheelTime = 0.9f;
        [Tooltip("Distance between formation slots.")]
        [SerializeField] private float slotSpacing = 2.8f;
        [Tooltip("Speed multiplier while the Warden stands on an island.")]
        [SerializeField] private float frenzySpeedMultiplier = 1.3f;

        public RazorFormState Form { get; private set; }
        public RazorStrafeState Strafe { get; private set; }
        public RazorWheelState Wheel { get; private set; }

        public Flock Flock { get; set; }
        /// <summary>True during a run until the wings have cut once.</summary>
        public bool SlashArmed { get; set; }
        public float AttackRange => attackRange;
        public float PassBeyond => passBeyond;
        public float WheelTime => Player != null && !Player.IsAirborne ? wheelTime * 0.5f : wheelTime;

        /// <summary>Run speed: its own (more for a Swift elite), at least a little over the Warden's, and faster
        /// still while the Warden stands on an island.</summary>
        public float StrafeSpeed =>
            Mathf.Max(strafeSpeed * SpeedTrait, Player.Speed * 1.1f) * (Player.IsAirborne ? 1f : frenzySpeedMultiplier);

        // On its run the wings are held out like blades.
        protected override float FlapScale => SlashArmed ? 0.15f : 1f;

        protected override IEnemyState DefaultState => Form;

        protected override void BuildStates()
        {
            Form = new RazorFormState(this);
            Strafe = new RazorStrafeState(this);
            Wheel = new RazorWheelState(this);
        }

        protected override void OnSpawned() => SlashArmed = false;

        protected override void OnDied(bool killed)
        {
            Flock?.Remove(this);
            Flock = null;
        }

        protected override void FrameUpdate(float deltaTime)
        {
            if (!SlashArmed || PlayerStats == null) return;
            if (DistanceToPlayer > hitDistance * SizeFactor) return;
            PlayerStats.TakeDamageFrom(Damage, transform.position);
            SlashArmed = false;
        }

        /// <summary>Where this bird belongs in the V: behind the leader, alternately to its left and right.</summary>
        public Vector3 SlotPosition()
        {
            Razorwing leader = Flock?.Leader;
            if (leader == null || leader == this) return PredictedPlayerPosition(0.4f);
            int slot = Flock.SlotOf(this);
            int rank = (slot + 1) / 2;
            float side = slot % 2 == 1 ? -1f : 1f;
            Vector3 forward = leader.Velocity.sqrMagnitude > 1f ? leader.Velocity.normalized : leader.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return leader.transform.position - forward * (rank * slotSpacing) + right * (side * rank * slotSpacing);
        }
    }
}
