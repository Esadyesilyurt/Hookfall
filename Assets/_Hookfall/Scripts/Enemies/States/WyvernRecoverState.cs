// Hookfall - Wyvern Recover state: slows down for 0.5 s, then straight back to Chase (GDD 5.4).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class WyvernRecoverState : IEnemyState
    {
        private readonly Wyvern wyvern;
        private float timer;

        public WyvernRecoverState(Wyvern wyvern) => this.wyvern = wyvern;
        public string Name => "Recover";

        public void Enter()
        {
            timer = wyvern.RecoverTime;
            wyvern.SetVelocityNow(wyvern.Velocity * wyvern.RecoverSpeedFactor);
        }

        public void Exit() { }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f) { wyvern.ChangeState(wyvern.Chase); return; }
            wyvern.Steer(wyvern.Player.transform.position, wyvern.MoveSpeed * wyvern.RecoverSpeedFactor);
        }
    }
}
