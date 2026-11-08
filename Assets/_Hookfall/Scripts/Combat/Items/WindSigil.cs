// Hookfall - Wind Sigil (Epic passive): in the air, refills the double jump and the dash once per cooldown, as soon as
// either has been spent, so long flights get extra moves without landing.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using UnityEngine;

namespace Hookfall.Combat.Items
{
    public class WindSigil : PassiveItem
    {
        [Tooltip("Cooldown at level 1, in seconds; each level takes rechargePerLevel off.")]
        [SerializeField] private float rechargeTime = 10f;
        [SerializeField] private float rechargePerLevel = 1.25f;

        private float timer;

        private float Recharge => Mathf.Max(3f, rechargeTime - rechargePerLevel * (Level - 1));

        protected override void OnActiveTick(float deltaTime)
        {
            timer += deltaTime;
            if (timer >= Recharge && Player != null && Player.IsAirborne && Player.RefillAirMoves()) timer = 0f;
        }

        public override string DescribeNextLevel() =>
            $"Air refill every {Recharge:0.#} s → {Mathf.Max(3f, rechargeTime - rechargePerLevel * Level):0.#} s";
    }
}
