// Hookfall - puts the selected character on the player at the start of a run: shows its model (all four are built into
// the scene, each with its own WardenAnimator), sets its air moves, health, stat picks and starting items. Runs before
// everything else so the sound and feel systems find the model that is actually shown.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using UnityEngine;

namespace Hookfall.Player
{
    [DefaultExecutionOrder(-100)]
    public class CharacterLoadout : MonoBehaviour
    {
        [Tooltip("Every character; index 0 is the default (the Warden).")]
        [SerializeField] private CharacterDefinition[] characters = new CharacterDefinition[0];
        [Tooltip("Their models under the player, in the same order.")]
        [SerializeField] private GameObject[] models = new GameObject[0];

        public CharacterDefinition Current { get; private set; }

        private void Awake()
        {
            if (characters.Length == 0) return;
            int index = 0;
            for (int i = 0; i < characters.Length; i++)
                if (characters[i] != null && characters[i].id == CharacterRoster.SelectedId && CharacterRoster.IsUnlocked(characters[i])) index = i;
            Current = characters[index];
            CharacterRoster.ActiveId = Current.id;
            for (int i = 0; i < models.Length; i++)
                if (models[i] != null) models[i].SetActive(i == index);

            if (TryGetComponent(out PlayerController controller)) controller.SetAirMoves(Current.airJumps, Current.airDashes);
        }

        // In Start: PlayerStats and CombatSystem have set themselves up in Awake.
        private void Start()
        {
            if (Current == null) return;
            if (TryGetComponent(out PlayerStats stats))
            {
                stats.ScaleMaxHealth(Current.maxHealthMultiplier);
                foreach (StatType stat in Current.statPicks) stats.Apply(stat);
            }
            if (Current.startingItems.Length > 0 && TryGetComponent(out CombatSystem combat)) combat.SetStartingItems(Current.startingItems);
        }
    }
}
