// Hookfall - Blightpuff state: the fuse. It glows red and swells for a moment, still drifting after the Warden, then
// bursts. Once it has started it does not stop: getting away from it is the only answer.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class PuffArmState : IEnemyState
    {
        private readonly Blightpuff puff;
        private float timer, length;

        public PuffArmState(Blightpuff puff) => this.puff = puff;
        public string Name => "Arm";

        public void Enter()
        {
            length = timer = puff.Fuse;
            puff.SetWarning(true);
            puff.Swell(0f, true);
        }

        public void Exit()
        {
            puff.SetWarning(false);
            puff.Swell(0f, false);
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            puff.Swell(1f - timer / length, false);
            if (timer <= 0f) { puff.Detonate(); return; }
            puff.Steer(puff.Player.transform.position, puff.MoveSpeed * 0.5f);
        }
    }
}
