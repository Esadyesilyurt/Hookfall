// Hookfall - the Blightpuff (added 2026-11-10): a spiked ball that chases the Warden and, once close, swells and
// bursts. It bursts when it is killed too, a moment after it dies. The burst hurts everything in reach, other enemies
// included, so a Warden at speed is long gone and has turned it against the horde, while one who lingers pays.
// Two states: Chase and Arm.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Blightpuff : EnemyBase
    {
        [Header("Blightpuff")]
        [Tooltip("It starts to swell when the Warden is this close.")]
        [SerializeField] private float armRange = 4.5f;
        [Tooltip("Seconds it swells (glowing red) before it bursts by itself.")]
        [SerializeField] private float fuse = 0.4f;
        [Tooltip("The fuse while the Warden stands on an island.")]
        [SerializeField] private float fuseOnIsland = 0.25f;
        [SerializeField] private float blastRadius = 5.5f;
        [SerializeField] private float blastDamage = 18f;
        [Tooltip("Damage of the burst to other enemies.")]
        [SerializeField] private float blastEnemyDamage = 40f;
        [Tooltip("Seconds between its death and the burst when it was killed: the warning rings show for this long.")]
        [SerializeField] private float blastDelay = 0.35f;
        [SerializeField] private float pursuitLookAhead = 0.5f;
        [Tooltip("How much it grows while it swells.")]
        [SerializeField] private float swell = 0.45f;

        private Vector3 restScale;
        private bool fuseBurned;

        public PuffChaseState Chase { get; private set; }
        public PuffArmState Arm { get; private set; }

        public float ArmRange => armRange * SizeFactor;
        public float Fuse => Player != null && !Player.IsAirborne ? fuseOnIsland : fuse;
        public float PursuitLookAhead => pursuitLookAhead;

        protected override IEnemyState DefaultState => Chase;

        protected override void BuildStates()
        {
            Chase = new PuffChaseState(this);
            Arm = new PuffArmState(this);
        }

        protected override void OnSpawned() => fuseBurned = false;

        /// <summary>Called by the Arm state: 0 at the start of the fuse, 1 when it burns out.</summary>
        public void Swell(float amount, bool begin)
        {
            if (begin) restScale = transform.localScale;
            transform.localScale = restScale * (1f + swell * Mathf.Clamp01(amount));
        }

        /// <summary>The fuse has burned out: it bursts at once, with no reward for anybody.</summary>
        public void Detonate()
        {
            fuseBurned = true;
            Die(false);
        }

        protected override void OnDied(bool killed)
        {
            if (Director == null) return;
            // Killed: the rings warn for a moment first. Self-set-off: the swelling was the warning.
            Director.Explode(transform.position, blastRadius * SizeFactor, blastDamage * DamageScale, blastEnemyDamage, fuseBurned ? 0.05f : blastDelay);
        }
    }
}
