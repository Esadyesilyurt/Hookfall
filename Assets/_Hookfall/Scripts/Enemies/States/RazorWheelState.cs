// Hookfall - Razorwing state: after a pass the bird flies on for a moment, then turns back. With its limited
// acceleration the turn is a wide arc, which is what makes the flock sweep through the sky instead of clinging.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class RazorWheelState : IEnemyState
    {
        private readonly Razorwing bird;
        private float timer;

        public RazorWheelState(Razorwing bird) => this.bird = bird;
        public string Name => "Wheel";

        public void Enter() => timer = bird.WheelTime;
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            timer -= deltaTime;
            if (timer <= 0f)
            {
                bird.ChangeState(bird.DistanceToPlayer <= bird.AttackRange ? (IEnemyState)bird.Strafe : bird.Form);
                return;
            }
            Vector3 ahead = bird.Velocity.sqrMagnitude > 1f ? bird.Velocity.normalized : bird.transform.forward;
            bird.Steer(bird.transform.position + ahead * 30f + Vector3.up * 4f, bird.MoveSpeed);
        }
    }
}
