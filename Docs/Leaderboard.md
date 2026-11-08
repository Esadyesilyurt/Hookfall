# Global leaderboard

Hookfall ranks players by their **best watch** (seconds survived). The game talks to the board through `Scripts/Online/ILeaderboard.cs`, so the service behind it can change without touching the menus.

## Now: Unity Gaming Services

The packages `com.unity.services.authentication` and `com.unity.services.leaderboards` are in `Packages/manifest.json`. Players sign in anonymously; nothing is asked of them.

One-time setup (Esad):

1. Open the project in Unity. In **Edit > Project Settings > Services**, sign in with your Unity account and link the project to a Unity Cloud project (create one if needed).
2. Open the [Unity Cloud dashboard](https://cloud.unity.com), choose that project, then **Leaderboards > Create leaderboard**:
   - ID: `best_time` (must match `UgsLeaderboard.LeaderboardId`)
   - Sort order: **High to low** (longer watches rank higher)
   - Update type: **Keep best score**
3. Press Play, finish a run, then open **Leaderboard** in the main menu: your run and your rank appear. **Save name** sets the name shown on the board (the service adds a `#1234` tag to keep names unique).

Until step 1 and 2 are done, the menu shows an "Offline" line and the game plays as before.

Runs from the automated tests (batch mode) are never sent.

## Later: Steam

For the Steam release, add Steamworks.NET, write `SteamLeaderboard : ILeaderboard` with the same five members (Steam's `FindOrCreateLeaderboard`, `UploadLeaderboardScore` with "keep best", `DownloadLeaderboardEntries`; the player name comes from the Steam profile, so `SetNameAsync` returns it unchanged), and set `Leaderboard.Current = new SteamLeaderboard()` at start-up. The menus need no change.
