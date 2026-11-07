// Hookfall - Golem Aim state: shows a red aim line for 0.4 s, then throws a boulder; cancels if it loses sight (GDD 5.5, 5.7).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class GolemAimState : IEnemyState
    {
        private readonly Golem golem;
        private float timer;
        private Vector3 target;

        public GolemAimState(Golem golem) => this.golem = golem;
        public string Name => "Aim";

        public void Enter()
        {
            timer = golem.AimTime;
            target = golem.AimPoint();
            golem.ShowAimLine(true, target);
            golem.SetWarning(true);
            golem.ArmRaise = 1f;
        }

        public void Exit()
        {
            golem.ShowAimLine(false, Vector3.zero);
            golem.SetWarning(false);
        }

        public void Tick(float deltaTime)
        {
            if (golem.PlayerOnMyIsland) { golem.ChangeState(golem.Slam); return; }
            if (!golem.PlayerInRangeAndSight()) { golem.ChangeState(golem.Idle); return; }   // fallback: lost sight

            target = golem.AimPoint();
            golem.ShowAimLine(true, target);
            timer -= deltaTime;
            if (timer > 0f) return;

            golem.Throw(target);
            golem.ChangeState(golem.Cooldown);
        }
    }
}
