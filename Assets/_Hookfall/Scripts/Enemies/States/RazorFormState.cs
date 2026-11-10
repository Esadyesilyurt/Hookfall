// Hookfall - Razorwing state: cruise in formation. The leader pursues the Warden; every other bird steers for its
// slot behind the leader (offset pursuit). Close enough, each breaks off into its attack run.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class RazorFormState : IEnemyState
    {
        private readonly Razorwing bird;
        public RazorFormState(Razorwing bird) => this.bird = bird;
        public string Name => "Form";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (bird.DistanceToPlayer <= bird.AttackRange) { bird.ChangeState(bird.Strafe); return; }
            Vector3 slot = bird.SlotPosition();
            // A bird behind its slot flies a little faster to close up.
            float behind = Mathf.Min(10f, Vector3.Distance(bird.transform.position, slot));
            bird.Steer(slot, bird.ChaseSpeed(bird.MoveSpeed) + behind);
        }
    }
}
