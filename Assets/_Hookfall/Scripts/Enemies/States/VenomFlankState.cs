// Hookfall - Venomwing state: hold a place on the Warden's flank and keep pace with them. When its venom is ready and
// the Warden is in range, it winds up to spit.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class VenomFlankState : IEnemyState
    {
        private readonly Venomwing wasp;
        public VenomFlankState(Venomwing wasp) => this.wasp = wasp;
        public string Name => "Flank";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (wasp.CooldownLeft <= 0f && wasp.DistanceToPlayer <= wasp.SpitRange && !wasp.Frozen)
            {
                wasp.ChangeState(wasp.Spit);
                return;
            }
            wasp.Steer(wasp.FlankPoint(), wasp.ChaseSpeed(wasp.MoveSpeed));
        }
    }
}
