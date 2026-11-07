// Hookfall - Golem Cooldown state: 1.5 s between throws, then Aim again (or Slam if the player is on its island) (GDD 5.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class GolemCooldownState : IEnemyState
    {
        private readonly Golem golem;
        private float timer;

        public GolemCooldownState(Golem golem) => this.golem = golem;
        public string Name => "Cooldown";

        public void Enter()
        {
            timer = golem.ThrowCooldown;
            golem.ArmRaise = 0f;
        }

        public void Exit() { }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;
            if (golem.PlayerOnMyIsland) golem.ChangeState(golem.Slam);
            else if (golem.PlayerInRangeAndSight()) golem.ChangeState(golem.Aim);
            else golem.ChangeState(golem.Idle);
        }
    }
}
