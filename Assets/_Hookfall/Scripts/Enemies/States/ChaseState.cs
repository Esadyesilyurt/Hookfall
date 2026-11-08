// Hookfall - Bat Chase state (GDD 5.3).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton; the look-ahead
// shrinks with distance since 2026-11-09. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    /// <summary>Pursues the player's predicted position at normal speed while the player is airborne.</summary>
    public class ChaseState : IEnemyState
    {
        private readonly Bat bat;
        public ChaseState(Bat bat) => this.bat = bat;
        public string Name => "Chase";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (!bat.Player.IsAirborne) { bat.ChangeState(bat.Frenzy); return; }
            if (bat.DistanceToPlayer <= bat.BiteRange) { bat.ChangeState(bat.Bite); return; }
            // Look ahead by the time it takes to close the gap, at most PursuitLookAhead. A fixed 0.5 s put the aim 19 m
            // in front of a Warden at full speed, and the horde circled that point instead of reaching the Warden.
            float lookAhead = Mathf.Min(bat.PursuitLookAhead, bat.DistanceToPlayer / Mathf.Max(1f, bat.Player.Speed));
            bat.Steer(bat.PredictedPlayerPosition(lookAhead), bat.ChaseSpeed(bat.MoveSpeed));
        }
    }
}
