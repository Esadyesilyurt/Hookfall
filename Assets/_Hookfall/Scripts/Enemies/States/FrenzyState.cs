// Hookfall - Bat Frenzy state (GDD 5.3).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    /// <summary>The player is on an island: the whole flock speeds up.</summary>
    public class FrenzyState : IEnemyState
    {
        private readonly Bat bat;
        public FrenzyState(Bat bat) => this.bat = bat;
        public string Name => "Frenzy";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (bat.Player.IsAirborne) { bat.ChangeState(bat.Chase); return; }
            if (bat.DistanceToPlayer <= bat.BiteRange) { bat.ChangeState(bat.Bite); return; }
            bat.SteerTowards(bat.Player.transform.position, bat.MoveSpeed * bat.FrenzySpeedMultiplier, deltaTime);
        }
    }
}
