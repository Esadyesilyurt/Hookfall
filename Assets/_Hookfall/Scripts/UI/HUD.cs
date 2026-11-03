// Hookfall - temporary debug HUD drawn with IMGUI: health, level, time, gold, kills, crosshair, attack status.
// Replaced by the uGUI HUD from GDD section 9 in Week 13.
// AI disclosure: written with Claude (Anthropic) on 2026-11-04 as part of the midterm project skeleton.
// Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Combat;
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.Player;
using Hookfall.Progression;
using UnityEngine;

namespace Hookfall.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private XPSystem xp;
        [SerializeField] private GoldWallet gold;
        [SerializeField] private CombatSystem combat;
        [SerializeField] private GrappleHook hook;
        [SerializeField] private EnemyDirector director;

        private GUIStyle label;
        private GUIStyle big;
        private GUIStyle centre;

        private void OnGUI()
        {
            if (label == null) BuildStyles();
            GameManager gm = GameManager.Instance;

            GUILayout.BeginArea(new Rect(16, 12, 420, 200));
            if (xp != null) GUILayout.Label($"Lv {xp.Level}   XP {xp.CurrentXP:0}/{xp.XPToNextLevel:0}", label);
            if (stats != null) GUILayout.Label($"Health {stats.CurrentHealth:0} / {stats.MaxHealth:0}", label);
            if (gold != null) GUILayout.Label($"Gold {gold.Gold}", label);
            if (gm != null) GUILayout.Label($"Kills {gm.Kills}", label);
            if (director != null) GUILayout.Label($"Enemies alive {director.AliveCount}", label);
            GUILayout.EndArea();

            if (gm != null)
            {
                int seconds = Mathf.FloorToInt(gm.RunTime);
                GUI.Label(new Rect(Screen.width / 2f - 100, 12, 200, 30), $"{seconds / 60:00}:{seconds % 60:00}", big);
            }

            // Crosshair: brighter when the hook can reach an island.
            bool inRange = hook != null && hook.HasTargetInRange;
            centre.normal.textColor = inRange ? new Color(0.3f, 0.95f, 1f) : new Color(1f, 1f, 1f, 0.5f);
            GUI.Label(new Rect(Screen.width / 2f - 20, Screen.height / 2f - 20, 40, 40), "+", centre);

            if (combat != null)
            {
                centre.normal.textColor = combat.AttacksActive ? new Color(0.3f, 0.95f, 1f) : new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(Screen.width / 2f - 100, Screen.height / 2f + 40, 200, 30),
                    combat.AttacksActive ? "ATTACKS ON" : "ATTACKS OFF", centre);
            }

            if (gm == null) return;
            if (gm.State == RunState.Paused)
                GUI.Label(new Rect(0, Screen.height / 2f - 90, Screen.width, 40), "Paused (Esc to resume)", big);
            if (gm.State == RunState.Dead)
                GUI.Label(new Rect(0, Screen.height / 2f - 90, Screen.width, 40),
                    $"Run over: {gm.EndReason}. Press R to retry", big);
        }

        private void BuildStyles()
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            label.normal.textColor = Color.white;
            big = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            centre = new GUIStyle(label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
    }
}
