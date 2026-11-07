// Hookfall - Wyvern Chase state: pursues like a bat; climbs above the player when they stand on an island (GDD 5.4).
// AI disclosure: written with Claude (Anthropic) on 2026-11-07. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class WyvernChaseState : IEnemyState
    {
        private readonly Wyvern wyvern;
        public WyvernChaseState(Wyvern wyvern) => this.wyvern = wyvern;
        public string Name => "Chase";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            bool onIsland = !wyvern.Player.IsAirborne;
            Vector3 target = onIsland
                ? wyvern.Player.transform.position + Vector3.up * wyvern.DiveHeight
                : wyvern.PredictedPlayerPosition(wyvern.PursuitLookAhead);

            if (Vector3.Distance(wyvern.transform.position, target) <= wyvern.ChargeRange || wyvern.DistanceToPlayer <= wyvern.ChargeRange * 0.6f)
            {
                wyvern.ChangeState(wyvern.Charge);
                return;
            }
            wyvern.Steer(target, wyvern.ChaseSpeed(wyvern.MoveSpeed));
        }
    }
}
