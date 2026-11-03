// Hookfall - finite state machine used by every enemy (State pattern, GDD 5.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies
{
    public interface IEnemyState
    {
        string Name { get; }
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    public class StateMachine
    {
        public IEnemyState Current { get; private set; }

        public void ChangeState(IEnemyState next)
        {
            if (next == Current) return;
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
