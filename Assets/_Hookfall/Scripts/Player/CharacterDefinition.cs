// Hookfall - one playable character (2026-11-08): name, look, starting stats and items, air moves, and the quest that
// unlocks it. The assets (SO_Character_*) are written by the scene builder; tune them in the Inspector or there.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using UnityEngine;

namespace Hookfall.Player
{
    [CreateAssetMenu(menuName = "Hookfall/Character", fileName = "SO_Character_")]
    public class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Stable id: saved as the selected character and sent to the leaderboard. Also the model and portrait name.")]
        public string id = "Warden";
        public string displayName = "Warden";
        [TextArea(2, 4)] public string description = "";
        [Tooltip("What sets the character apart, one line each, shown at a glance on its card (e.g. \"+20% Damage\").")]
        public string[] perks = new string[0];

        [Tooltip("The school this character leans to: it counts as one more item of that school for set bonuses.")]
        public School affinity = School.Guardian;

        [Header("Start of a run")]
        [Tooltip("Max health is multiplied by this.")]
        public float maxHealthMultiplier = 1f;
        [Tooltip("Stat cards applied at the start, as if picked on level-up.")]
        public StatType[] statPicks = new StatType[0];
        [Tooltip("Replaces the Warden's Orbit Blades.")]
        public ItemBase[] startingItems = new ItemBase[0];
        public int airJumps = 1;
        public int airDashes = 1;

        [Header("Unlock quest")]
        public UnlockKind unlock = UnlockKind.None;
        public float unlockTarget;
        public string unlockText = "";
    }
}
