# Steam Toolkit for Unity

Comprehensive Steam integration toolkit for Unity.

## Features

- **Core** - Steam initialization, user info, avatar
- **Auth** - Session ticket, authentication
- **Achievements** - Unlock, progress, reset
- **Stats** - Get/Set player stats
- **Leaderboards** - Upload/download scores, rankings
- **Inventory** - Item management (coming soon)
- **Cloud Save** - Remote storage (coming soon)
- **Workshop** - UGC support (coming soon)
- **Build/Deploy** - SteamPipe integration (coming soon)

## Requirements

- Unity 2021.3+
- **Steamworks.NET** (must be installed separately)

> ⚠️ **IMPORTANT:** Steam Toolkit requires Steamworks.NET to work.
> If Steamworks.NET is not installed, you will get compile errors.
> Follow Step 1 below to install it first.

## Installation (3 Steps)

### Step 1: Install Steamworks.NET

**Option A - GitHub (Recommended):**
1. Download `.unitypackage` from [Steamworks.NET Releases](https://github.com/rlabrecque/Steamworks.NET/releases)
2. In Unity: Assets → Import Package → Custom Package

**Option B - UPM (Git URL):**
```
https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net
```

### Step 2: Install Steam Toolkit

**Option A - Unity Package Manager (Git URL):**
1. Window → Package Manager
2. "+" → "Add package from git URL"
3. Paste: `https://github.com/faikalbayrak/steam-toolkit.git`

**Option B - Manual:**
1. Copy `SteamToolkit` folder to `Assets/`

### Step 3: Configuration

1. Open `Tools → Steam Toolkit` menu (or `Ctrl+Shift+S`)
2. Click "Create SteamConfig" button
3. Enter your App ID (use 480 for testing)
4. Place `steam_appid.txt` file in project root (next to Assets)

## Quick Start

### steam_appid.txt

Create `steam_appid.txt` in project root (next to Assets folder):
```
480
```
> 480 = Spacewar (Valve's test game). Replace with your App ID when you have one.

### Basic Usage

```csharp
using SteamToolkit;

void Start()
{
    // Check Steam status
    if (SteamCore.Instance.IsInitialized)
    {
        Debug.Log($"Hello {SteamCore.Instance.DisplayName}!");
        Debug.Log($"SteamID: {SteamCore.Instance.SteamIdString}");
    }

    // Events
    SteamCore.Instance.OnInitialized += () => Debug.Log("Steam ready!");
    SteamCore.Instance.OnInitializationFailed += (error) => Debug.LogError(error);
}
```

### Auth Ticket

```csharp
// Async (recommended)
SteamCore.Instance.Auth.GetAuthSessionTicket(
    ticket => {
        // Send to UGS or backend server
        SendToServer(ticket);
    },
    error => Debug.LogError(error)
);
```

### Avatar

```csharp
// Get your avatar
var myAvatar = SteamCore.Instance.GetAvatar(
    SteamCore.Instance.SteamId, 
    SteamCore.AvatarSize.Large
);

// Display in UI
avatarImage.texture = myAvatar;
```

### Leaderboards

```csharp
// Upload score (keeps best)
SteamCore.Instance.Leaderboards.UploadScore("HighScores", 5000, ScoreUploadMethod.KeepBest, 
    success => Debug.Log($"Upload: {success}"));

// Download top 10
SteamCore.Instance.Leaderboards.DownloadTopScores("HighScores", 10, entries =>
{
    foreach (var entry in entries)
    {
        Debug.Log($"#{entry.Rank} {entry.PlayerName}: {entry.Score}");
    }
});

// Download scores around current user
SteamCore.Instance.Leaderboards.DownloadScoresAroundUser("HighScores", 5, entries => { });

// Download friend scores
SteamCore.Instance.Leaderboards.DownloadFriendsScores("HighScores", entries => { });
```

## Configuration

Settings available in SteamConfig asset:

| Setting | Description | Default |
|---------|-------------|---------|
| App ID | Steam App ID | 480 (test) |
| Auto Initialize | Auto start on launch | true |
| Allow Without Steam | Run without Steam | true |
| Check Restart App | Steam launch check | true |
| Enable Debug Logs | Debug logging | true |
| Test Mode | Allow achievement reset | false |

## Project Structure

```
SteamToolkit/
├── package.json
├── README.md
├── Editor/
│   ├── SteamToolkit.Editor.asmdef
│   ├── SteamToolkitWindow.cs
│   └── SteamWebAPI.cs
└── Runtime/
    ├── SteamToolkit.Runtime.asmdef
    ├── Core/
    │   ├── SteamConfig.cs
    │   └── SteamCore.cs
    └── Services/
        ├── SteamAuthService.cs
        ├── SteamAchievementService.cs
        ├── SteamStatsService.cs
        └── SteamLeaderboardService.cs
```

## FAQ

### Getting compile errors after installing Steam Toolkit?

Make sure Steamworks.NET is installed first! Steam Toolkit depends on it.
See "Step 1: Install Steamworks.NET" above.

### Why isn't Steamworks.NET included?

1. **Licensing:** Steamworks.NET is MIT licensed but Valve's SDK has different terms
2. **Updates:** Can be updated independently
3. **Flexibility:** You can use any version you prefer
4. **Size:** Lighter package

### Getting "Steam not initialized" error

1. Is Steam client running?
2. Is `steam_appid.txt` in the correct location?
3. Is the App ID correct?

### Works in Editor but not in build

1. Is `steam_api64.dll` (or `steam_api.dll`) copied to build folder?
2. Is `steam_appid.txt` in build folder?

## License

MIT License

## Contributing

Pull requests are welcome!