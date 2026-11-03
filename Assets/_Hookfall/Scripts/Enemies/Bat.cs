// Hookfall - the Bat: horde flyer with Chase, Bite and Frenzy states (GDD 5.3).
// Wyvern and Golem follow the same pattern in Week 11.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Enemies.States;
using UnityEngine;

namespace Hookfall.Enemies
{
    public class Bat : EnemyBase
    {
        [Header("Bat")]
        [SerializeField] private float biteRange = 1.5f;
        [SerializeField] private float biteRecovery = 0.25f;
        [SerializeField] private float pursuitLookAhead = 0.5f;
        [Tooltip("Speed multiplier while the player stands on an island.")]
        [SerializeField] private float frenzySpeedMultiplier = 1.3f;

        public ChaseState Chase { get; private set; }
        public FrenzyState Frenzy { get; private set; }
        public BiteState Bite { get; private set; }

        public float BiteRange => biteRange;
        public float BiteRecovery => biteRecovery;
        public float PursuitLookAhead => pursuitLookAhead;
        public float FrenzySpeedMultiplier => frenzySpeedMultiplier;

        protected override IEnemyState DefaultState => Chase;

        protected override void BuildStates()
        {
            Chase = new ChaseState(this);
            Frenzy = new FrenzyState(this);
            Bite = new BiteState(this);
        }

        /// <summary>The state to return to after a bite: Frenzy if the player is on an island, else Chase.</summary>
        public IEnemyState HuntingState => Player != null && !Player.IsAirborne ? (IEnemyState)Frenzy : Chase;
    }
}
