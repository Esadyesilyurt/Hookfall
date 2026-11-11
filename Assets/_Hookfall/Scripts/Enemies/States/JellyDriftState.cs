// Hookfall - Gloom Jelly state: drift at the Warden, slowly. With them in reach of the bell and its shock ready, it
// starts the pulse.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class JellyDriftState : IEnemyState
    {
        private readonly GloomJelly jelly;
        public JellyDriftState(GloomJelly jelly) => this.jelly = jelly;
        public string Name => "Drift";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (jelly.CooldownLeft <= 0f && jelly.DistanceToPlayer <= jelly.PulseRange * 0.9f && !jelly.Frozen)
            {
                jelly.ChangeState(jelly.Pulse);
                return;
            }
            jelly.Steer(jelly.Player.transform.position, jelly.DriftSpeed);
        }
    }
}
