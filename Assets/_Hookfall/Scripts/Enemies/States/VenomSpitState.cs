// Hookfall - Venomwing state: spit. A short wind-up (it glows red and its abdomen curls forward), then the venom is
// away and the wasp goes back to its flank.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class VenomSpitState : IEnemyState
    {
        private readonly Venomwing wasp;
        private float timer;

        public VenomSpitState(Venomwing wasp) => this.wasp = wasp;
        public string Name => "Spit";

        public void Enter()
        {
            timer = wasp.WindUp;
            wasp.SetWarning(true);
            wasp.TailCurl = 1f;
        }

        public void Exit()
        {
            wasp.SetWarning(false);
            wasp.TailCurl = 0f;
        }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                wasp.Fire();
                wasp.ChangeState(wasp.Flank);
                return;
            }
            // Keeps pace while it aims, a little slower.
            wasp.Steer(wasp.FlankPoint(), wasp.ChaseSpeed(wasp.MoveSpeed) * 0.7f);
        }
    }
}
