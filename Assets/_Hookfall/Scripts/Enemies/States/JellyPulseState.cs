// Hookfall - Gloom Jelly state: the pulse. It stops and glows red for a moment (the warning), then shocks everything
// within reach of its bell and drifts on.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class JellyPulseState : IEnemyState
    {
        private readonly GloomJelly jelly;
        private float timer;

        public JellyPulseState(GloomJelly jelly) => this.jelly = jelly;
        public string Name => "Pulse";

        public void Enter()
        {
            timer = jelly.PulseWindUp;
            jelly.Stop();
            jelly.SetWarning(true);
        }

        public void Exit() => jelly.SetWarning(false);

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer > 0f) return;
            jelly.Shock();
            jelly.ChangeState(jelly.Drift);
        }
    }
}
