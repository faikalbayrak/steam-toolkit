using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Stats service.
    /// Handles reading and writing player statistics.
    /// </summary>
    public class SteamStatsService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when stats are received from Steam.
        /// </summary>
        public event Action OnStatsReceived;

        /// <summary>
        /// Fired when stats are stored to Steam.
        /// </summary>
        public event Action OnStatsStored;

        /// <summary>
        /// Fired when stats store fails.
        /// </summary>
        public event Action<string> OnStatsStoreFailed;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public bool StatsReceived { get; private set; }

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private Callback<UserStatsReceived_t> _userStatsReceivedCallback;
        private Callback<UserStatsStored_t> _userStatsStoredCallback;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            _userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);

            // Request current stats from Steam
            RequestStats();
#endif

            IsInitialized = true;
            Log("Stats service initialized.");
        }

        public void Dispose()
        {
#if !DISABLESTEAMWORKS
            _userStatsReceivedCallback = null;
            _userStatsStoredCallback = null;
#endif

            IsInitialized = false;
            StatsReceived = false;
            Log("Stats service disposed.");
        }

        #endregion

        #region Request Stats

        /// <summary>
        /// Request stats from Steam.
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

        #endregion

        #region Get Stats

        /// <summary>
        /// Get an integer stat value.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <returns>Stat value or 0 if not found</returns>
        public int GetStatInt(string statName)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return 0;

            if (SteamUserStats.GetStat(statName, out int value))
            {
                return value;
            }

            LogError($"Failed to get int stat: {statName}");
#endif
            return 0;
        }

        /// <summary>
        /// Get a float stat value.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <returns>Stat value or 0 if not found</returns>
        public float GetStatFloat(string statName)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return 0f;

            if (SteamUserStats.GetStat(statName, out float value))
            {
                return value;
            }

            LogError($"Failed to get float stat: {statName}");
#endif
            return 0f;
        }

        /// <summary>
        /// Try to get an integer stat value.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="value">Output value</param>
        /// <returns>True if successful</returns>
        public bool TryGetStatInt(string statName, out int value)
        {
            value = 0;
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            return SteamUserStats.GetStat(statName, out value);
#else
            return false;
#endif
        }

        /// <summary>
        /// Try to get a float stat value.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="value">Output value</param>
        /// <returns>True if successful</returns>
        public bool TryGetStatFloat(string statName, out float value)
        {
            value = 0f;
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            return SteamUserStats.GetStat(statName, out value);
#else
            return false;
#endif
        }

        #endregion

        #region Set Stats

        /// <summary>
        /// Set an integer stat value.
        /// Call StoreStats() to save to Steam.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="value">New value</param>
        /// <param name="autoStore">Automatically store to Steam (default: false)</param>
        /// <returns>True if successful</returns>
        public bool SetStatInt(string statName, int value, bool autoStore = false)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.SetStat(statName, value))
            {
                Log($"Stat set: {statName} = {value}");

                if (autoStore)
                {
                    StoreStats();
                }

                return true;
            }

            LogError($"Failed to set int stat: {statName}");
#endif
            return false;
        }

        /// <summary>
        /// Set a float stat value.
        /// Call StoreStats() to save to Steam.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="value">New value</param>
        /// <param name="autoStore">Automatically store to Steam (default: false)</param>
        /// <returns>True if successful</returns>
        public bool SetStatFloat(string statName, float value, bool autoStore = false)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.SetStat(statName, value))
            {
                Log($"Stat set: {statName} = {value}");

                if (autoStore)
                {
                    StoreStats();
                }

                return true;
            }

            LogError($"Failed to set float stat: {statName}");
#endif
            return false;
        }

        /// <summary>
        /// Increment an integer stat by a given amount.
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="amount">Amount to add (can be negative)</param>
        /// <param name="autoStore">Automatically store to Steam (default: false)</param>
        /// <returns>New value or -1 if failed</returns>
        public int IncrementStat(string statName, int amount = 1, bool autoStore = false)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return -1;

            int currentValue = GetStatInt(statName);
            int newValue = currentValue + amount;

            if (SetStatInt(statName, newValue, autoStore))
            {
                return newValue;
            }
#endif
            return -1;
        }

        /// <summary>
        /// Update average rate stat.
        /// Used for stats like "average speed" or "average score".
        /// </summary>
        /// <param name="statName">Stat API name</param>
        /// <param name="countThisSession">Value for this session</param>
        /// <param name="sessionLength">Session length (time or count)</param>
        /// <returns>True if successful</returns>
        public bool UpdateAvgRateStat(string statName, float countThisSession, double sessionLength)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.UpdateAvgRateStat(statName, countThisSession, sessionLength))
            {
                Log($"Avg rate stat updated: {statName}");
                return true;
            }

            LogError($"Failed to update avg rate stat: {statName}");
#endif
            return false;
        }

        #endregion

        #region Store Stats

        /// <summary>
        /// Store stats to Steam.
        /// Call this after setting stats to save them.
        /// </summary>
        /// <returns>True if request was sent</returns>
        public bool StoreStats()
        {
#if !DISABLESTEAMWORKS
            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
                return false;
            }

            if (SteamUserStats.StoreStats())
            {
                Log("Stats store requested.");
                return true;
            }

            LogError("Failed to request stats store.");
#endif
            return false;
        }

        /// <summary>
        /// Reset all stats for current user.
        /// Only works in test mode.
        /// </summary>
        /// <param name="achievementsToo">Also reset achievements</param>
        /// <returns>True if successful</returns>
        public bool ResetAllStats(bool achievementsToo = false)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState()) return false;

            if (SteamUserStats.ResetAllStats(achievementsToo))
            {
                Log($"All stats reset (achievements: {achievementsToo})");
                RequestStats();
                return true;
            }

            LogError("Failed to reset stats.");
#endif
            return false;
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
                var error = $"Failed to store stats: {result.m_eResult}";
                LogError(error);
                OnStatsStoreFailed?.Invoke(error);
            }
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
                Debug.Log($"[SteamToolkit.Stats] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Stats] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Stat information container for Editor display.
    /// </summary>
    [Serializable]
    public class StatInfo
    {
        public string Name;
        public string DisplayName;
        public StatType Type;
        public int IntValue;
        public float FloatValue;

        public enum StatType
        {
            Int,
            Float
        }
    }
}