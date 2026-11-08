// Hookfall - base class for passive items (added 2026-11-08): they work all the time, on islands too, take one of the
// six passive slots, and either change a value while owned or act on their own cooldown. Passives that deal damage
// still respect the core rule and act only while the Warden is airborne.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;

namespace Hookfall.Combat
{
    public abstract class PassiveItem : ItemBase
    {
        protected PlayerController Player { get; private set; }

        public override bool IsPassive => true;

        // Passive cooldowns are their own: attack speed does not shorten them.
        protected override float EffectiveCooldown => Cooldown;

        protected override void Awake()
        {
            base.Awake();
            Player = GetComponentInParent<PlayerController>();
        }

        protected override void Attack() { }
    }
}
