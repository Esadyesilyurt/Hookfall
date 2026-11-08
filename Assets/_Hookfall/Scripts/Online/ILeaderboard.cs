// Hookfall - what the game needs from a global leaderboard. Unity Gaming Services backs it for now (UgsLeaderboard);
// for the Steam release a SteamLeaderboard implements the same calls and Leaderboard.Current switches to it.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hookfall.Online
{
    public interface ILeaderboard
    {
        /// <summary>Empty while online; otherwise why the leaderboard cannot be reached, for the UI.</summary>
        string Status { get; }
        string PlayerName { get; }

        /// <summary>Sends a finished run; the service keeps each player's best time.</summary>
        Task SubmitAsync(float seconds, string character);
        Task<IReadOnlyList<LeaderboardRow>> TopAsync(int count);
        /// <summary>The player's own row, or null when they have no score yet or the service is offline.</summary>
        Task<LeaderboardRow?> MineAsync();
        /// <summary>Changes the name shown on the board; returns the name the service accepted.</summary>
        Task<string> SetNameAsync(string name);
    }
}
