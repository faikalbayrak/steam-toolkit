# Steam Toolkit for Unity

Comprehensive Steam integration toolkit for Unity.

## Features

- **Core** - Steam initialization, user info, avatar
- **Auth** - Session ticket, authentication
- **Achievements** - Unlock, progress, reset
- **Stats** - Get/Set player stats
- **Leaderboards** - Upload/download scores, rankings
- **Inventory** - Item management, grants, consumption
- **Cloud Save** - Remote storage for save files
- **Workshop** - UGC creation, subscription, queries
- **Build/Deploy** - SteamPipe integration, one-click upload

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
    (score, changed) => Debug.Log($"Score: {score}, Changed: {changed}"),
    error => Debug.LogError(error));

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

### Inventory

```csharp
// Get all items in user's inventory
SteamCore.Instance.Inventory.GetAllItems(items =>
{
    foreach (var item in items)
    {
        Debug.Log($"{item.Name} x{item.Quantity}");
    }
});

// Grant promotional items
SteamCore.Instance.Inventory.GrantPromoItems(items =>
{
    Debug.Log($"Granted {items.Count} promo items!");
});

// Consume an item
SteamCore.Instance.Inventory.ConsumeItem(itemId, 1, success =>
{
    Debug.Log($"Consumed: {success}");
});

// Get all item definitions
var definitions = SteamCore.Instance.Inventory.GetAllItemDefinitions();
foreach (var def in definitions)
{
    Debug.Log($"Item: {def.Name}, Price: {def.Price}");
}
```

### Cloud Save

```csharp
// Write string to cloud
SteamCore.Instance.Cloud.WriteString("save.txt", "Hello Cloud!");

// Write JSON object
var saveData = new MySaveData { level = 5, score = 1000 };
SteamCore.Instance.Cloud.WriteJson("save.json", saveData);

// Read string from cloud
string content = SteamCore.Instance.Cloud.ReadString("save.txt");

// Read JSON object
var loaded = SteamCore.Instance.Cloud.ReadJson<MySaveData>("save.json");

// Get all cloud files
var files = SteamCore.Instance.Cloud.GetAllFiles();
foreach (var file in files)
{
    Debug.Log($"{file.FileName}: {file.SizeFormatted}");
}

// Get quota info
var quota = SteamCore.Instance.Cloud.GetQuota();
Debug.Log($"Used: {quota.UsedFormatted} / {quota.TotalFormatted}");

// Delete file
SteamCore.Instance.Cloud.DeleteFile("old_save.txt");
```

### Workshop

```csharp
// Query subscribed items
SteamCore.Instance.Workshop.QuerySubscribedItems(items =>
{
    foreach (var item in items)
    {
        Debug.Log($"{item.Title} (ID: {item.ItemId})");
    }
});

// Subscribe to an item
SteamCore.Instance.Workshop.Subscribe(itemId, success =>
{
    Debug.Log($"Subscribed: {success}");
});

// Create and upload a new item
SteamCore.Instance.Workshop.CreateItem(itemId =>
{
    SteamCore.Instance.Workshop.BeginItemUpdate(itemId)
        .SetTitle("My Awesome Mod")
        .SetDescription("This mod adds cool stuff!")
        .SetContent("/path/to/content/folder")
        .SetPreviewImage("/path/to/preview.png")
        .SetVisibility(WorkshopVisibility.Public)
        .SetTags("mod", "gameplay")
        .Submit("Initial release", (id, needsAgreement) =>
        {
            Debug.Log($"Uploaded! ID: {id}");
        });
});

// Get item state
var state = SteamCore.Instance.Workshop.GetItemState(itemId);
Debug.Log($"Installed: {state.IsInstalled}, Needs Update: {state.NeedsUpdate}");

// Get installed item path
var info = SteamCore.Instance.Workshop.GetInstalledItemInfo(itemId);
Debug.Log($"Path: {info.FolderPath}");
```

### Build & Deploy

Build & Deploy is configured through the Editor window (Steam Toolkit > Build & Deploy tab).

**Setup:**
1. Create a Build Config: `Create > Steam Toolkit > Build Config`
2. Set SteamCMD path
3. Initialize ContentBuilder folder
4. Configure depots for each platform
5. Enter Steam credentials

**Usage via Editor:**
1. Build your game normally (File > Build Settings)
2. Copy build to ContentBuilder/content folder
3. Click "Generate VDF" to create upload scripts
4. Click "Upload to Steam" to run SteamCMD

**Programmatic VDF Generation:**
```csharp
// In Editor scripts
using SteamToolkit.Editor;

// Generate VDF files
var config = Resources.Load<SteamBuildConfig>("SteamBuildConfig");
string description = SteamPipeBuilder.BuildDescription(config.DescriptionTemplate);
SteamPipeBuilder.WriteVdfFiles(config, description, "default");

// Copy build to ContentBuilder
SteamPipeBuilder.CopyBuildToContent(config, config.Depots[0], "Build/Windows");
```

## Configuration

### Publisher API Key

Steam Toolkit uses Publisher API Key for Edit Mode features (viewing achievements, stats, inventory without entering Play Mode).

| Feature | Edit Mode | Play Mode |
|---------|-----------|-----------|
| Achievements | ✅ Schema, icons, percentages | ✅ Full runtime control |
| Stats | ✅ Schema, defaults | ✅ Get/Set values |
| Inventory | ✅ Item definitions | ✅ User items, grants, consume |
| Leaderboards | ❌ Not available | ✅ Full access |

**Get your key:** https://partner.steamgames.com/pub/webapi

> **Note:** Publisher API Key is different from the regular Steam Web API Key (steamcommunity.com/dev/apikey). Publisher keys are only available to game developers/publishers and provide access to private game data.

### Settings

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
│   ├── SteamWebAPI.cs
│   ├── SteamPipeBuilder.cs
│   └── EditorInputDialog.cs
└── Runtime/
    ├── SteamToolkit.Runtime.asmdef
    ├── Core/
    │   ├── SteamConfig.cs
    │   ├── SteamCore.cs
    │   └── SteamBuildConfig.cs
    └── Services/
        ├── SteamAuthService.cs
        ├── SteamAchievementService.cs
        ├── SteamStatsService.cs
        ├── SteamLeaderboardService.cs
        ├── SteamInventoryService.cs
        ├── SteamCloudService.cs
        └── SteamWorkshopService.cs
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