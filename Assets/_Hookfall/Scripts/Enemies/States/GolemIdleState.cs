// Hookfall - Golem Idle state: waits until the player is in range and sight, or lands on its island (GDD 5.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class GolemIdleState : IEnemyState
    {
        private readonly Golem golem;
        public GolemIdleState(Golem golem) => this.golem = golem;
        public string Name => "Idle";

        public void Enter()
        {
            golem.Stop();
            golem.ArmRaise = 0f;
        }

        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (golem.PlayerOnMyIsland) golem.ChangeState(golem.Slam);
            else if (golem.PlayerInRangeAndSight()) golem.ChangeState(golem.Aim);
        }
    }
}
