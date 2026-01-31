using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Achievement service.
    /// Handles achievement unlock, progress, and reset operations.
    /// </summary>
    public class SteamAchievementService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when stats and achievements are received from Steam.
        /// </summary>
        public event Action OnStatsReceived;

        /// <summary>
        /// Fired when stats are stored to Steam.
        /// </summary>
        public event Action OnStatsStored;

        /// <summary>
        /// Fired when an achievement is unlocked.
        /// </summary>
        public event Action<string> OnAchievementUnlocked;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public bool StatsReceived { get; private set; }

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private Callback<UserStatsReceived_t> _userStatsReceivedCallback;
        private Callback<UserStatsStored_t> _userStatsStoredCallback;
        private Callback<UserAchievementStored_t> _achievementStoredCallback;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
            _achievementStoredCallback = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

            // Request current stats from Steam
            RequestStats();
#endif

            IsInitialized = true;
            Log("Achievement service initialized.");
        }

        public void Dispose()
        {
#if !DISABLESTEAMWORKS
            _userStatsReceivedCallback = null;
            _userStatsStoredCallback = null;
            _achievementStoredCallback = null;
#endif

            IsInitialized = false;
            StatsReceived = false;
            Log("Achievement service disposed.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Request stats and achievements from Steam.
        /// Called automatically on Initialize.
        /// </summary>
        public void RequestStats()
        {
#if !DISABLESTEAMWORKS
            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
                return;
            }

            SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            Log("Stats requested.");
#endif
        }

        /// <summary>
        /// Check if an achievement is unlocked.
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <returns>True if unlocked</returns>
        public bool IsAchievementUnlocked(string achievementId)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.GetAchievement(achievementId, out bool achieved))
            {
                return achieved;
            }

            LogError($"Failed to get achievement: {achievementId}");
#endif
            return false;
        }

        /// <summary>
        /// Get achievement unlock status and unlock time.
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <param name="unlocked">Whether achievement is unlocked</param>
        /// <param name="unlockTime">Unix timestamp when unlocked (0 if not unlocked)</param>
        /// <returns>True if successful</returns>
        public bool GetAchievement(string achievementId, out bool unlocked, out uint unlockTime)
        {
            unlocked = false;
            unlockTime = 0;

#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            return SteamUserStats.GetAchievementAndUnlockTime(achievementId, out unlocked, out unlockTime);
#else
            return false;
#endif
        }

        /// <summary>
        /// Unlock an achievement.
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <param name="autoStore">Automatically store to Steam (default: true)</param>
        /// <returns>True if successful</returns>
        public bool UnlockAchievement(string achievementId, bool autoStore = true)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            // Check if already unlocked
            if (IsAchievementUnlocked(achievementId))
            {
                Log($"Achievement already unlocked: {achievementId}");
                return true;
            }

            if (SteamUserStats.SetAchievement(achievementId))
            {
                Log($"Achievement unlocked: {achievementId}");

                if (autoStore)
                {
                    StoreStats();
                }

                return true;
            }

            LogError($"Failed to unlock achievement: {achievementId}");
#endif
            return false;
        }

        /// <summary>
        /// Lock/reset an achievement (only works in test mode).
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <param name="autoStore">Automatically store to Steam (default: true)</param>
        /// <returns>True if successful</returns>
        public bool LockAchievement(string achievementId, bool autoStore = true)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.ClearAchievement(achievementId))
            {
                Log($"Achievement locked: {achievementId}");

                if (autoStore)
                {
                    StoreStats();
                }

                return true;
            }

            LogError($"Failed to lock achievement: {achievementId}");
#endif
            return false;
        }

        /// <summary>
        /// Show achievement progress notification.
        /// Use for achievements with progress (e.g., "Kill 100 enemies").
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <param name="currentProgress">Current progress value</param>
        /// <param name="maxProgress">Maximum progress value</param>
        /// <returns>True if successful</returns>
        public bool IndicateProgress(string achievementId, uint currentProgress, uint maxProgress)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.IndicateAchievementProgress(achievementId, currentProgress, maxProgress))
            {
                Log($"Achievement progress: {achievementId} = {currentProgress}/{maxProgress}");
                return true;
            }

            LogError($"Failed to indicate progress: {achievementId}");
#endif
            return false;
        }

        /// <summary>
        /// Get achievement display name.
        /// </summary>
        public string GetAchievementDisplayName(string achievementId)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return achievementId;

            return SteamUserStats.GetAchievementDisplayAttribute(achievementId, "name");
#else
            return achievementId;
#endif
        }

        /// <summary>
        /// Get achievement description.
        /// </summary>
        public string GetAchievementDescription(string achievementId)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return "";

            return SteamUserStats.GetAchievementDisplayAttribute(achievementId, "desc");
#else
            return "";
#endif
        }

        /// <summary>
        /// Get achievement icon as Texture2D.
        /// </summary>
        /// <param name="achievementId">Achievement API name</param>
        /// <returns>Icon texture or null</returns>
        public Texture2D GetAchievementIcon(string achievementId)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return null;

            int iconHandle = SteamUserStats.GetAchievementIcon(achievementId);
            if (iconHandle == 0) return null;

            if (!SteamUtils.GetImageSize(iconHandle, out uint width, out uint height))
                return null;

            byte[] imageData = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(iconHandle, imageData, imageData.Length))
                return null;

            var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(imageData);
            texture.Apply();

            return texture;
#else
            return null;
#endif
        }

        /// <summary>
        /// Get total number of achievements for this app.
        /// </summary>
        public uint GetAchievementCount()
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return 0;

            return SteamUserStats.GetNumAchievements();
#else
            return 0;
#endif
        }

        /// <summary>
        /// Get achievement API name by index.
        /// </summary>
        public string GetAchievementNameByIndex(uint index)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return "";

            return SteamUserStats.GetAchievementName(index);
#else
            return "";
#endif
        }

        /// <summary>
        /// Get all achievements info.
        /// </summary>
        public List<AchievementInfo> GetAllAchievements()
        {
            var achievements = new List<AchievementInfo>();

#if !DISABLESTEAMWORKS
            if (!ValidateState()) return achievements;

            uint count = GetAchievementCount();
            for (uint i = 0; i < count; i++)
            {
                string id = GetAchievementNameByIndex(i);
                if (string.IsNullOrEmpty(id)) continue;

                GetAchievement(id, out bool unlocked, out uint unlockTime);

                achievements.Add(new AchievementInfo
                {
                    Id = id,
                    DisplayName = GetAchievementDisplayName(id),
                    Description = GetAchievementDescription(id),
                    IsUnlocked = unlocked,
                    UnlockTime = unlockTime
                });
            }
#endif

            return achievements;
        }

        /// <summary>
        /// Reset all achievements (only works in test mode).
        /// </summary>
        public bool ResetAllAchievements()
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.ResetAllStats(true))
            {
                Log("All achievements reset.");
                RequestStats();
                return true;
            }

            LogError("Failed to reset achievements.");
#endif
            return false;
        }

        /// <summary>
        /// Store stats and achievements to Steam.
        /// Call after unlocking achievements if autoStore is false.
        /// </summary>
        public bool StoreStats()
        {
#if !DISABLESTEAMWORKS
            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
                return false;
            }

            return SteamUserStats.StoreStats();
#else
            return false;
#endif
        }

        #endregion

        #region Callbacks

#if !DISABLESTEAMWORKS
        private void OnUserStatsReceived(UserStatsReceived_t result)
        {
            if (result.m_nGameID != SteamCore.Instance.Config.AppId)
                return;

            if (result.m_eResult == EResult.k_EResultOK)
            {
                StatsReceived = true;
                Log("Stats received from Steam.");
                OnStatsReceived?.Invoke();
            }
            else
            {
                LogError($"Failed to receive stats: {result.m_eResult}");
            }
        }

        private void OnUserStatsStored(UserStatsStored_t result)
        {
            if (result.m_nGameID != SteamCore.Instance.Config.AppId)
                return;

            if (result.m_eResult == EResult.k_EResultOK)
            {
                Log("Stats stored to Steam.");
                OnStatsStored?.Invoke();
            }
            else
            {
                LogError($"Failed to store stats: {result.m_eResult}");
            }
        }

        private void OnAchievementStored(UserAchievementStored_t result)
        {
            if (result.m_nGameID != SteamCore.Instance.Config.AppId)
                return;

            Log($"Achievement stored: {result.m_rgchAchievementName}");
            OnAchievementUnlocked?.Invoke(result.m_rgchAchievementName);
        }
#endif

        #endregion

        #region Helpers

        private bool ValidateState()
        {
#if !DISABLESTEAMWORKS
            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
                return false;
            }

            if (!StatsReceived)
            {
                LogError("Stats not received yet. Wait for OnStatsReceived event.");
                return false;
            }

            return true;
#else
            return false;
#endif
        }

        private void Log(string message)
        {
            if (SteamCore.Instance?.Config?.EnableDebugLogs ?? true)
                Debug.Log($"[SteamToolkit.Achievements] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Achievements] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Achievement information container.
    /// </summary>
    [Serializable]
    public class AchievementInfo
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public bool IsUnlocked;
        public uint UnlockTime;

        public DateTime UnlockDateTime => UnlockTime > 0 
            ? DateTimeOffset.FromUnixTimeSeconds(UnlockTime).LocalDateTime 
            : DateTime.MinValue;
    }
}