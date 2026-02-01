using UnityEngine;

namespace SteamToolkit
{
    /// <summary>
    /// Steam configuration asset.
    /// Stores all Steam settings centrally.
    /// </summary>
    [CreateAssetMenu(fileName = "SteamConfig", menuName = "Steam Toolkit/Config")]
    public class SteamConfig : ScriptableObject
    {
        [Header("App Settings")]
        [Tooltip("Steam App ID (Use 480 for testing)")]
        public uint AppId = 480;
        
        [Tooltip("Game name (should match Steam)")]
        public string GameName = "My Game";

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
            if (AppId == 0)
            {
                Debug.LogError("[SteamToolkit] App ID cannot be 0!");
                return false;
            }
            
            return true;
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
            AppId = 480;
            GameName = "My Game";
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
#endif

        #endregion
    }
}