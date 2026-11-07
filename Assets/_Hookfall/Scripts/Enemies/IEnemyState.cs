// Hookfall - one state of an enemy's finite state machine: Enter, Tick, Exit (State pattern, GDD 5.1).
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Enemies
{
    public interface IEnemyState
    {
        string Name { get; }
        void Enter();
        /// <summary>Called on every think (10 per second near the player, 2 far away) with the time since the last one.</summary>
        void Tick(float deltaTime);
        void Exit();
    }
}
