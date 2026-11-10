// Hookfall - Blightpuff state: chase. It flies at where the Warden will be, like a bat; in reach it starts to swell.
// AI disclosure: written with Claude (Anthropic) on 2026-11-10. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Enemies.States
{
    public class PuffChaseState : IEnemyState
    {
        private readonly Blightpuff puff;
        public PuffChaseState(Blightpuff puff) => this.puff = puff;
        public string Name => "Chase";

        public void Enter() { }
        public void Exit() { }

        public void Tick(float deltaTime)
        {
            if (puff.DistanceToPlayer <= puff.ArmRange && !puff.Frozen) { puff.ChangeState(puff.Arm); return; }
            float lookAhead = Mathf.Min(puff.PursuitLookAhead, puff.DistanceToPlayer / Mathf.Max(1f, puff.Player.Speed));
            puff.Steer(puff.PredictedPlayerPosition(lookAhead), puff.ChaseSpeed(puff.MoveSpeed));
        }
    }
}
