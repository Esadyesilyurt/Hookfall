// Hookfall - test codes typed during a run: BOSSGEL calls the stage boss at once (with its entrance cutscene),
// BOSSOL kills the boss when it is fighting (its fall, the portal and the reward follow as usual). A run that used a
// code saves no record, no unlock progress and no leaderboard score.
// AI disclosure: written with Claude (Anthropic) on 2026-11-09. Prompts and changes are recorded in Docs/PromptLog.md.
using Hookfall.Core;
using Hookfall.Enemies;
using Hookfall.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hookfall.World
{
    public class CheatCodes : MonoBehaviour
    {
        [SerializeField] private string bossTimeCode = "BOSSGEL";
        [SerializeField] private string killBossCode = "BOSSOL";

        private string typed = "";

        /// <summary>True once a code worked in this run; the run is then kept off the record and the board.</summary>
        public static bool Used { get; private set; }

        private void Awake() => Used = false;
        private void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnText; }
        private void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }

        private void OnText(char c)
        {
            if (!char.IsLetter(c)) return;
            typed = (typed + char.ToUpperInvariant(c)).Substring(Mathf.Max(0, typed.Length + 1 - 16));
            if (typed.EndsWith(bossTimeCode)) { typed = ""; BossTime(); }
            else if (typed.EndsWith(killBossCode)) { typed = ""; KillBoss(); }
        }

        private static void BossTime()
        {
            StageDirector stage = StageDirector.Instance;
            if (stage == null || stage.Current != StageDirector.Phase.Gathering) return;
            Used = true;
            stage.SummonBoss();
        }

        private static void KillBoss()
        {
            StageDirector stage = StageDirector.Instance;
            if (stage == null || stage.Boss == null || !stage.Boss.Fighting || stage.Boss.IsDead) return;
            Used = true;
            stage.Boss.Die(true);
            HUD.Announce(Loc.T("Test code: boss killed"), Color.white);
        }
    }
}
