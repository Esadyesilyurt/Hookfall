// Hookfall - Bat Chase state (GDD 5.3).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
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
            bat.Steer(bat.PredictedPlayerPosition(bat.PursuitLookAhead), bat.ChaseSpeed(bat.MoveSpeed));
        }
    }
}
