// Hookfall - one row of the global leaderboard: rank (1 = best), player name, best watch time and character.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
namespace Hookfall.Online
{
    public readonly struct LeaderboardRow
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly float Seconds;
        public readonly string Character;

        public LeaderboardRow(int rank, string name, float seconds, string character)
        {
            Rank = rank;
            Name = name;
            Seconds = seconds;
            Character = character;
        }
    }
}
