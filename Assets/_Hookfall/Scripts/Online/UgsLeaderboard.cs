// Hookfall - the global leaderboard on Unity Gaming Services: anonymous sign-in, best watch time per player, with the
// character as metadata. Needs the project linked to a Unity Cloud project and a leaderboard with the id below
// (Docs/Leaderboard.md). Every failure is turned into Status text instead of an error, so the game plays offline.
// AI disclosure: written with Claude (Anthropic) on 2026-11-08. Prompts and changes are recorded in Docs/PromptLog.md.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

namespace Hookfall.Online
{
    public class UgsLeaderboard : ILeaderboard
    {
        public const string LeaderboardId = "best_time";

        [Serializable]
        private class Meta { public string character; }   // stored next to each score

        private Task<bool> ready;

        public string Status { get; private set; } = "Connecting...";
        public string PlayerName => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerName ?? "" : "";

        private Task<bool> Ready() => ready ??= Connect();

        private async Task<bool> Connect()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName)) await AuthenticationService.Instance.GetPlayerNameAsync();
                Status = "";
                return true;
            }
            catch (Exception e)
            {
                Status = "Offline: " + e.Message;
                ready = null;   // try again next time
                return false;
            }
        }

        public async Task SubmitAsync(float seconds, string character)
        {
            if (!await Ready()) return;
            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, seconds,
                    new AddPlayerScoreOptions { Metadata = new Meta { character = character } });
            }
            catch (Exception e) { Status = "Score not sent: " + e.Message; }
        }

        public async Task<IReadOnlyList<LeaderboardRow>> TopAsync(int count)
        {
            var rows = new List<LeaderboardRow>();
            if (!await Ready()) return rows;
            try
            {
                var page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId,
                    new GetScoresOptions { Limit = count, IncludeMetadata = true });
                foreach (LeaderboardEntry entry in page.Results) rows.Add(Row(entry));
            }
            catch (Exception e) { Status = "Leaderboard unavailable: " + e.Message; }
            return rows;
        }

        public async Task<LeaderboardRow?> MineAsync()
        {
            if (!await Ready()) return null;
            try
            {
                return Row(await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId,
                    new GetPlayerScoreOptions { IncludeMetadata = true }));
            }
            catch (Exception) { return null; }   // no score yet
        }

        public async Task<string> SetNameAsync(string name)
        {
            if (!await Ready()) return PlayerName;
            try { return await AuthenticationService.Instance.UpdatePlayerNameAsync(name); }
            catch (Exception e) { Status = "Name not saved: " + e.Message; return PlayerName; }
        }

        private static LeaderboardRow Row(LeaderboardEntry entry)
        {
            string character = "";
            if (!string.IsNullOrEmpty(entry.Metadata))
            {
                try { character = JsonUtility.FromJson<Meta>(entry.Metadata)?.character ?? ""; }
                catch (Exception) { }
            }
            return new LeaderboardRow(entry.Rank + 1, entry.PlayerName, (float)entry.Score, character);
        }
    }
}
