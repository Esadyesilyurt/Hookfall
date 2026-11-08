// Hookfall - the leaderboard the game uses (Unity Gaming Services until the Steam build swaps it), and the last run's
// submission, which the run-end screen waits on to show the player's rank.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Threading.Tasks;
using UnityEngine;

namespace Hookfall.Online
{
    public static class Leaderboard
    {
        public static ILeaderboard Current { get; set; } = new UgsLeaderboard();

        /// <summary>The submission of the last finished run; null when nothing was sent.</summary>
        public static Task LastSubmit { get; private set; }

        /// <summary>The board ranks the stage first, then the time: the score is (stage - 1) x 100000 + seconds, so a
        /// stage 1 run scores its seconds as it did before stages existed (2026-11-08).</summary>
        public static float Score(int stage, float seconds) => (Mathf.Max(1, stage) - 1) * 100000f + Mathf.Min(seconds, 99999f);

        public static void SubmitRun(float seconds, string character, int stage = 1)
        {
            // Batch runs are the automated tests: their made-up runs must never reach the real board.
            if (Application.isBatchMode) return;
            LastSubmit = Current.SubmitAsync(Score(stage, seconds), character);
        }
    }
}
