// Hookfall - Razorwing state: the attack run. The bird aims at a point beyond where the Warden will be and flies
// straight through; its wings cut once on the way (Razorwing.FrameUpdate). Past the Warden it goes into Wheel.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class RazorStrafeState : IEnemyState
    {
        private readonly Razorwing bird;
        private float time;

        public RazorStrafeState(Razorwing bird) => this.bird = bird;
        public string Name => "Strafe";

        public void Enter()
        {
            time = 0f;
            bird.SlashArmed = true;
        }

        public void Exit() => bird.SlashArmed = false;

        public void Tick(float deltaTime)
        {
            time += deltaTime;
            float speed = bird.StrafeSpeed;
            Vector3 meet = bird.PredictedPlayerPosition(Mathf.Min(0.6f, bird.DistanceToPlayer / Mathf.Max(1f, speed)));
            Vector3 toMeet = meet - bird.transform.position;
            // Past the Warden (they are behind the bird now), or the run has gone on too long: wheel round.
            bool passed = Vector3.Dot(toMeet, bird.Velocity) < 0f && toMeet.sqrMagnitude > 64f;
            if (passed || time > 4f) { bird.ChangeState(bird.Wheel); return; }
            bird.Steer(meet + toMeet.normalized * bird.PassBeyond, speed);
        }
    }
}
