using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamToolkit
{
    /// <summary>
    /// App type enumeration for Steam apps.
    /// </summary>
    public enum SteamAppType
    {
        Main,
        Demo,
        Playtest,
        DLC,
        Beta
    }

    /// <summary>
    /// Depot configuration for a specific platform/content type.
    /// </summary>
    [Serializable]
    public class DepotConfig
    {
        public string Name = "Windows";
        public uint DepotId = 481;
        public string ContentRoot = "Build/Windows";
        public string LocalPath = "*";
        public string DepotPath = ".";
        public bool Recursive = true;
        public List<string> Exclude = new List<string> { "*.pdb", "*.log" };
    }

    /// <summary>
    /// Represents a Steam app entry (Main, Demo, Playtest, etc.)
    /// </summary>
    [Serializable]
    public class SteamAppEntry
    {
        [Tooltip("Display name for this app")]
        public string Name = "My Game";

        [Tooltip("Type of this app")]
        public SteamAppType Type = SteamAppType.Main;

        [Tooltip("Steam App ID")]
        public uint AppId = 480;

        [Tooltip("Default branch for uploads")]
        public string DefaultBranch = "default";

        [Tooltip("Available branches")]
        public List<string> Branches = new List<string> { "default", "beta", "playtest" };

        [Tooltip("Depot configurations for this app")]
        public List<DepotConfig> Depots = new List<DepotConfig>();

        [Tooltip("Notes about this app")]
        [TextArea(2, 4)]
        public string Notes = "";

        /// <summary>
        /// Get the default depot ID (first depot or AppId + 1).
        /// </summary>
        public uint DefaultDepotId => Depots.Count > 0 ? Depots[0].DepotId : AppId + 1;
    }

    /// <summary>
    /// Steam configuration asset.
    /// Stores all Steam settings centrally.
    /// </summary>
    [CreateAssetMenu(fileName = "SteamConfig", menuName = "Steam Toolkit/Config")]
    public class SteamConfig : ScriptableObject
    {
        [Header("Apps")]
        [Tooltip("List of Steam apps (Main, Demo, Playtest, etc.)")]
        public List<SteamAppEntry> Apps = new List<SteamAppEntry>();

        [Tooltip("Index of the active app")]
        public int ActiveAppIndex = 0;

        /// <summary>
        /// Get the currently active app entry.
        /// </summary>
        public SteamAppEntry ActiveApp
        {
            get
            {
                if (Apps == null || Apps.Count == 0) return null;
                if (ActiveAppIndex < 0 || ActiveAppIndex >= Apps.Count)
                    ActiveAppIndex = 0;
                return Apps[ActiveAppIndex];
            }
        }

        /// <summary>
        /// Get the active App ID.
        /// </summary>
        public uint AppId => ActiveApp?.AppId ?? 480;

        /// <summary>
        /// Get the active Depot ID.
        /// </summary>
        public uint DepotId => ActiveApp?.DefaultDepotId ?? 481;

        /// <summary>
        /// Get the active game name.
        /// </summary>
        public string GameName => ActiveApp?.Name ?? "My Game";

        [Header("Initialization")]
        [Tooltip("Auto initialize when game starts")]
        public bool AutoInitialize = true;
        
        [Tooltip("Allow running without Steam (for development)")]
        public bool AllowWithoutSteam = true;
        
        [Tooltip("Check RestartAppIfNecessary")]
        public bool CheckRestartApp = true;

        [Header("Debug")]
        [Tooltip("Enable debug logs")]
        public bool EnableDebugLogs = true;
        
        [Tooltip("Run achievements in test mode (can be reset)")]
        public bool TestMode = false;

        [Header("Web API")]
        [Tooltip("Steam Publisher API Key (get from https://partner.steamgames.com/pub/webapi)\nRequired for: Achievements, Stats, Inventory, Leaderboards in Edit Mode")]
        public string PublisherApiKey = "";

        [Header("Services")]
        [Tooltip("Enable Achievement service")]
        public bool EnableAchievements = true;
        
        [Tooltip("Enable Stats service")]
        public bool EnableStats = true;
        
        [Tooltip("Enable Inventory service")]
        public bool EnableInventory = false;
        
        [Tooltip("Enable Leaderboard service")]
        public bool EnableLeaderboards = false;
        
        [Tooltip("Enable Cloud Save service")]
        public bool EnableCloudSave = false;
        
        [Tooltip("Enable Workshop service")]
        public bool EnableWorkshop = false;

        #region Singleton Instance

        private static SteamConfig _instance;
        
        public static SteamConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<SteamConfig>("SteamConfig");
                    
                    if (_instance == null)
                    {
                        Debug.LogWarning("[SteamToolkit] Config not found! Create Resources/SteamConfig.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Validation

        public bool IsValid()
        {
            if (ActiveApp == null)
            {
                Debug.LogError("[SteamToolkit] No active app configured!");
                return false;
            }
            
            if (ActiveApp.AppId == 0)
            {
                Debug.LogError("[SteamToolkit] App ID cannot be 0!");
                return false;
            }
            
            return true;
        }

        #endregion

        #region App Management

        /// <summary>
        /// Add a new app entry.
        /// </summary>
        public SteamAppEntry AddApp(string name, SteamAppType type, uint appId)
        {
            var entry = new SteamAppEntry
            {
                Name = name,
                Type = type,
                AppId = appId,
                DefaultBranch = "default",
                Branches = new List<string> { "default", "beta", "playtest" },
                Depots = new List<DepotConfig>
                {
                    new DepotConfig
                    {
                        Name = "Windows",
                        DepotId = appId + 1,
                        ContentRoot = "Build/Windows"
                    }
                }
            };
            Apps.Add(entry);
            return entry;
        }

        /// <summary>
        /// Set the active app by index.
        /// </summary>
        public void SetActiveApp(int index)
        {
            if (index >= 0 && index < Apps.Count)
            {
                ActiveAppIndex = index;
            }
        }

        /// <summary>
        /// Get app entry by App ID.
        /// </summary>
        public SteamAppEntry GetAppById(uint appId)
        {
            return Apps.Find(a => a.AppId == appId);
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Open Steamworks Settings")]
        private void OpenSteamworksSettings()
        {
            Application.OpenURL($"https://partner.steamgames.com/apps/landing/{AppId}");
        }

        [ContextMenu("Reset to Defaults")]
        private void ResetToDefaults()
        {
            Apps.Clear();
            Apps.Add(new SteamAppEntry
            {
                Name = "My Game",
                Type = SteamAppType.Main,
                AppId = 480,
                DefaultBranch = "default",
                Branches = new List<string> { "default", "beta", "playtest" },
                Depots = new List<DepotConfig>
                {
                    new DepotConfig
                    {
                        Name = "Windows",
                        DepotId = 481,
                        ContentRoot = "Build/Windows"
                    }
                }
            });
            ActiveAppIndex = 0;
            AutoInitialize = true;
            AllowWithoutSteam = true;
            CheckRestartApp = true;
            EnableDebugLogs = true;
            TestMode = false;
            EnableAchievements = true;
            EnableStats = true;
            EnableInventory = false;
            EnableLeaderboards = false;
            EnableCloudSave = false;
            EnableWorkshop = false;
        }

        private void OnValidate()
        {
            // Ensure at least one app exists
            if (Apps == null) Apps = new List<SteamAppEntry>();
            if (Apps.Count == 0)
            {
                Apps.Add(new SteamAppEntry
                {
                    Name = "My Game",
                    Type = SteamAppType.Main,
                    AppId = 480,
                    Depots = new List<DepotConfig>
                    {
                        new DepotConfig { Name = "Windows", DepotId = 481, ContentRoot = "Build/Windows" }
                    }
                });
            }

            // Ensure each app has at least one depot
            foreach (var app in Apps)
            {
                if (app.Depots == null) app.Depots = new List<DepotConfig>();
                if (app.Depots.Count == 0)
                {
                    app.Depots.Add(new DepotConfig
                    {
                        Name = "Windows",
                        DepotId = app.AppId + 1,
                        ContentRoot = "Build/Windows"
                    });
                }

                if (app.Branches == null || app.Branches.Count == 0)
                {
                    app.Branches = new List<string> { "default", "beta", "playtest" };
                }
            }
            
            // Clamp active index
            if (ActiveAppIndex < 0) ActiveAppIndex = 0;
            if (ActiveAppIndex >= Apps.Count) ActiveAppIndex = Apps.Count - 1;
        }
#endif

        #endregion
    }
}