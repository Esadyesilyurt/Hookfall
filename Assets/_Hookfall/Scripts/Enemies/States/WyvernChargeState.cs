// Hookfall - Wyvern Charge state: 0.3 s red wind-up (0.15 s if the player is on an island), then a straight dash at 30 m/s (GDD 5.4).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class WyvernChargeState : IEnemyState
    {
        private readonly Wyvern wyvern;
        private float windUpLeft;
        private float dashLeft;
        private bool dashing;

        public WyvernChargeState(Wyvern wyvern) => this.wyvern = wyvern;
        public string Name => dashing ? "Charge (dash)" : "Charge (wind-up)";

        public void Enter()
        {
            dashing = false;
            windUpLeft = wyvern.Player.IsAirborne ? wyvern.WindUp : wyvern.WindUpOnIsland;
            wyvern.Stop();
            wyvern.SetWarning(true);
        }

        public void Exit()
        {
            wyvern.SetWarning(false);
            wyvern.DashArmed = false;
        }

        public void Tick(float deltaTime)
        {
            if (!dashing)
            {
                windUpLeft -= deltaTime;
                if (windUpLeft > 0f) return;

                // Aim at where the player will be halfway through the dash.
                float travel = wyvern.DistanceToPlayer / wyvern.DashSpeed;
                Vector3 target = wyvern.PredictedPlayerPosition(travel * 0.5f);
                Vector3 direction = (target - wyvern.transform.position).normalized;
                wyvern.SetVelocityNow(direction * wyvern.DashSpeed);
                wyvern.SetWarning(false);
                wyvern.DashArmed = true;
                dashing = true;
                dashLeft = wyvern.DashDuration;
                return;
            }

            dashLeft -= deltaTime;
            if (dashLeft <= 0f) wyvern.ChangeState(wyvern.Recover);
        }
    }
}
