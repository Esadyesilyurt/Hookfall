// Hookfall - the boss's only state: it never steers, so its attacks run every frame in Boss.FrameUpdate instead of on
// the slower think ticks.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies.States
{
    public class BossState : IEnemyState
    {
        private readonly Boss boss;

        public BossState(Boss owner) => boss = owner;

        public string Name => boss.Fighting ? "Fight" : "Entrance";
        public void Enter() => boss.Stop();
        public void Tick(float deltaTime) { }
        public void Exit() { }
    }
}
