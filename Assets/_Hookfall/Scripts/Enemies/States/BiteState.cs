// Hookfall - Bat Bite state (GDD 5.3).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    /// <summary>Bites once on contact, then goes straight back to hunting. No limit on how many bats bite at once.</summary>
    public class BiteState : IEnemyState
    {
        private readonly Bat bat;
        private float timer;
        public BiteState(Bat bat) => this.bat = bat;
        public string Name => "Bite";

        public void Enter()
        {
            timer = bat.BiteRecovery;
            if (bat.PlayerStats != null) bat.PlayerStats.TakeDamage(bat.Damage);
        }

        public void Exit() { }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f) { bat.ChangeState(bat.HuntingState); return; }
            bat.Steer(bat.Player.transform.position, bat.MoveSpeed);
        }
    }
}
