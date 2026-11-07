// Hookfall - Golem Ground slam: a 0.4 s red ring, then 25 damage in a 3 m radius, then Cooldown (GDD 5.5).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class GolemSlamState : IEnemyState
    {
        private readonly Golem golem;
        private float timer;

        public GolemSlamState(Golem golem) => this.golem = golem;
        public string Name => "Ground slam";

        public void Enter()
        {
            timer = golem.SlamWindUp;
            golem.ShowSlamRing(true);
            golem.SetWarning(true);
            golem.ArmRaise = 1f;
        }

        public void Exit()
        {
            golem.ShowSlamRing(false);
            golem.SetWarning(false);
            golem.ArmRaise = 0f;
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;
            golem.DoSlam();
            EnemyDirector.RaiseSlam(golem);
            golem.ChangeState(golem.Cooldown);
        }
    }
}
