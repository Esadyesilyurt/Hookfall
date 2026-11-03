// Hookfall - level-up rewards: an owned item level or a stat (GDD 4.6, 4.7).
// Placeholder: picks a random stat automatically. The 3-card choice screen comes in Week 10.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Player;
using UnityEngine;

namespace Hookfall.Progression
{
    [RequireComponent(typeof(XPSystem), typeof(PlayerStats))]
    public class LevelUpSystem : MonoBehaviour
    {
        private XPSystem xp;
        private PlayerStats stats;
        private GrappleHook hook;

        public string LastReward { get; private set; } = "";

        private void Awake()
        {
            xp = GetComponent<XPSystem>();
            stats = GetComponent<PlayerStats>();
            hook = GetComponent<GrappleHook>();
        }

        private void OnEnable() => xp.LeveledUp += OnLeveledUp;
        private void OnDisable() => xp.LeveledUp -= OnLeveledUp;

        private void OnLeveledUp(int newLevel)
        {
            // TODO (Week 10): pause and offer 3 cards (item levels and stats) instead of a random stat.
            switch (Random.Range(0, 5))
            {
                case 0: stats.AddMaxHealth(20f); LastReward = "+20 max health"; break;
                case 1: stats.AddArmor(1f); LastReward = "+1 armor"; break;
                case 2: stats.AddDamagePercent(10f); LastReward = "+10% damage"; break;
                case 3: stats.AddAttackSpeedPercent(8f); LastReward = "+8% attack speed"; break;
                default:
                    if (hook != null) { hook.AddRange(5f); LastReward = "+5 m hook range"; }
                    else { stats.AddRegen(0.5f); LastReward = "+0.5 health regen"; }
                    break;
            }
            Debug.Log($"[Hookfall] Level {newLevel}: {LastReward}");
        }
    }
}
