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

        public static void SubmitRun(float seconds, string character)
        {
            // Batch runs are the automated tests: their made-up runs must never reach the real board.
            if (Application.isBatchMode) return;
            LastSubmit = Current.SubmitAsync(seconds, character);
        }
    }
}
