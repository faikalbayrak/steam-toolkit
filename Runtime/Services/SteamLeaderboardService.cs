using System;
using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Leaderboard service.
    /// Handles score upload, download, and leaderboard queries.
    /// </summary>
    public class SteamLeaderboardService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a leaderboard is found/created.
        /// </summary>
        public event Action<string> OnLeaderboardFound;

        /// <summary>
        /// Fired when score upload completes.
        /// </summary>
        public event Action<string, int, bool> OnScoreUploaded; // leaderboardName, score, changed

        /// <summary>
        /// Fired when scores are downloaded.
        /// </summary>
        public event Action<string, List<LeaderboardEntry>> OnScoresDownloaded;

        /// <summary>
        /// Fired on any error.
        /// </summary>
        public event Action<string> OnError;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

#if !DISABLESTEAMWORKS
        private Dictionary<string, SteamLeaderboard_t> _leaderboardCache = new Dictionary<string, SteamLeaderboard_t>();
        
        private CallResult<LeaderboardFindResult_t> _findLeaderboardCallResult;
        private CallResult<LeaderboardScoreUploaded_t> _uploadScoreCallResult;
        private CallResult<LeaderboardScoresDownloaded_t> _downloadScoresCallResult;

        // Pending operations
        private Action<SteamLeaderboard_t> _onLeaderboardFoundCallback;
        private Action<string> _onLeaderboardFoundErrorCallback;
        private string _pendingLeaderboardName;
        private string _pendingDownloadLeaderboardName;
#endif

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

#if !DISABLESTEAMWORKS
            _findLeaderboardCallResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFindResult);
            _uploadScoreCallResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploadResult);
            _downloadScoresCallResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnScoresDownloadResult);
#endif

            IsInitialized = true;
            Log("Leaderboard service initialized.");
        }

        public void Dispose()
        {
#if !DISABLESTEAMWORKS
            _leaderboardCache.Clear();
            _findLeaderboardCallResult = null;
            _uploadScoreCallResult = null;
            _downloadScoresCallResult = null;
#endif

            IsInitialized = false;
            Log("Leaderboard service disposed.");
        }

        #endregion

        #region Find Leaderboard

        /// <summary>
        /// Find or create a leaderboard.
        /// </summary>
        /// <param name="leaderboardName">Leaderboard name (must match Steam)</param>
        /// <param name="onFound">Called when found</param>
        /// <param name="onError">Called on error</param>
        public void FindLeaderboard(string leaderboardName, Action<SteamLeaderboard_t> onFound = null, Action<string> onError = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onError?.Invoke("Service not ready");
                return;
            }

            // Check cache first
            if (_leaderboardCache.TryGetValue(leaderboardName, out var cached))
            {
                Log($"Leaderboard found in cache: {leaderboardName}");
                onFound?.Invoke(cached);
                return;
            }

            _pendingLeaderboardName = leaderboardName;
            _onLeaderboardFoundCallback = onFound;
            _onLeaderboardFoundErrorCallback = onError;

            var handle = SteamUserStats.FindLeaderboard(leaderboardName);
            _findLeaderboardCallResult.Set(handle);

            Log($"Finding leaderboard: {leaderboardName}");
#else
            onError?.Invoke("Steamworks disabled");
#endif
        }

        /// <summary>
        /// Find or create a leaderboard with specific settings.
        /// </summary>
        public void FindOrCreateLeaderboard(string leaderboardName, LeaderboardSortMethod sortMethod, LeaderboardDisplayType displayType, Action<SteamLeaderboard_t> onFound = null, Action<string> onError = null)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                onError?.Invoke("Service not ready");
                return;
            }

            _pendingLeaderboardName = leaderboardName;
            _onLeaderboardFoundCallback = onFound;
            _onLeaderboardFoundErrorCallback = onError;

            var steamSort = sortMethod == LeaderboardSortMethod.Ascending 
                ? ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending 
                : ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending;

            var steamDisplay = displayType switch
            {
                LeaderboardDisplayType.Numeric => ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric,
                LeaderboardDisplayType.TimeSeconds => ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds,
                LeaderboardDisplayType.TimeMilliSeconds => ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds,
                _ => ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric
            };

            var handle = SteamUserStats.FindOrCreateLeaderboard(leaderboardName, steamSort, steamDisplay);
            _findLeaderboardCallResult.Set(handle);

            Log($"Finding/creating leaderboard: {leaderboardName}");
#else
            onError?.Invoke("Steamworks disabled");
#endif
        }

#if !DISABLESTEAMWORKS
        private void OnLeaderboardFindResult(LeaderboardFindResult_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bLeaderboardFound == 0)
            {
                var error = $"Failed to find leaderboard: {_pendingLeaderboardName}";
                LogError(error);
                _onLeaderboardFoundErrorCallback?.Invoke(error);
                OnError?.Invoke(error);
            }
            else
            {
                _leaderboardCache[_pendingLeaderboardName] = result.m_hSteamLeaderboard;
                Log($"Leaderboard found: {_pendingLeaderboardName}");
                _onLeaderboardFoundCallback?.Invoke(result.m_hSteamLeaderboard);
                OnLeaderboardFound?.Invoke(_pendingLeaderboardName);
            }

            _onLeaderboardFoundCallback = null;
            _onLeaderboardFoundErrorCallback = null;
        }
#endif

        #endregion

        #region Upload Score

        /// <summary>
        /// Upload a score to a leaderboard.
        /// </summary>
        /// <param name="leaderboardName">Leaderboard name</param>
        /// <param name="score">Score to upload</param>
        /// <param name="uploadMethod">Keep best or force update</param>
        /// <param name="onComplete">Called when complete (score, changed)</param>
        /// <param name="onError">Called on error</param>
        public void UploadScore(string leaderboardName, int score, ScoreUploadMethod uploadMethod = ScoreUploadMethod.KeepBest, Action<int, bool> onComplete = null, Action<string> onError = null)
        {
#if !DISABLESTEAMWORKS
            FindLeaderboard(leaderboardName,
                leaderboard =>
                {
                    UploadScoreInternal(leaderboardName, leaderboard, score, uploadMethod, onComplete, onError);
                },
                onError
            );
#else
            onError?.Invoke("Steamworks disabled");
#endif
        }

#if !DISABLESTEAMWORKS
        private string _uploadLeaderboardName;
        private Action<int, bool> _uploadOnComplete;
        private Action<string> _uploadOnError;

        private void UploadScoreInternal(string leaderboardName, SteamLeaderboard_t leaderboard, int score, ScoreUploadMethod uploadMethod, Action<int, bool> onComplete, Action<string> onError)
        {
            _uploadLeaderboardName = leaderboardName;
            _uploadOnComplete = onComplete;
            _uploadOnError = onError;

            var method = uploadMethod == ScoreUploadMethod.KeepBest
                ? ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest
                : ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate;

            var handle = SteamUserStats.UploadLeaderboardScore(leaderboard, method, score, null, 0);
            _uploadScoreCallResult.Set(handle);

            Log($"Uploading score {score} to {leaderboardName}");
        }

        private void OnScoreUploadResult(LeaderboardScoreUploaded_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bSuccess == 0)
            {
                var error = $"Failed to upload score to {_uploadLeaderboardName}";
                LogError(error);
                _uploadOnError?.Invoke(error);
                OnError?.Invoke(error);
            }
            else
            {
                var changed = result.m_bScoreChanged != 0;
                Log($"Score uploaded to {_uploadLeaderboardName}: {result.m_nScore} (changed: {changed})");
                _uploadOnComplete?.Invoke(result.m_nScore, changed);
                OnScoreUploaded?.Invoke(_uploadLeaderboardName, result.m_nScore, changed);
            }

            _uploadOnComplete = null;
            _uploadOnError = null;
        }
#endif

        #endregion

        #region Download Scores

        /// <summary>
        /// Download scores from a leaderboard.
        /// </summary>
        /// <param name="leaderboardName">Leaderboard name</param>
        /// <param name="requestType">Type of entries to download</param>
        /// <param name="rangeStart">Start index (for Global/GlobalAroundUser)</param>
        /// <param name="rangeEnd">End index (for Global/GlobalAroundUser)</param>
        /// <param name="onComplete">Called with entries</param>
        /// <param name="onError">Called on error</param>
        public void DownloadScores(string leaderboardName, LeaderboardRequestType requestType, int rangeStart, int rangeEnd, Action<List<LeaderboardEntry>> onComplete = null, Action<string> onError = null)
        {
#if !DISABLESTEAMWORKS
            FindLeaderboard(leaderboardName,
                leaderboard =>
                {
                    DownloadScoresInternal(leaderboardName, leaderboard, requestType, rangeStart, rangeEnd, onComplete, onError);
                },
                onError
            );
#else
            onError?.Invoke("Steamworks disabled");
#endif
        }

        /// <summary>
        /// Download top scores from a leaderboard.
        /// </summary>
        public void DownloadTopScores(string leaderboardName, int count = 10, Action<List<LeaderboardEntry>> onComplete = null, Action<string> onError = null)
        {
            DownloadScores(leaderboardName, LeaderboardRequestType.Global, 1, count, onComplete, onError);
        }

        /// <summary>
        /// Download scores around the current user.
        /// </summary>
        public void DownloadScoresAroundUser(string leaderboardName, int range = 5, Action<List<LeaderboardEntry>> onComplete = null, Action<string> onError = null)
        {
            DownloadScores(leaderboardName, LeaderboardRequestType.GlobalAroundUser, -range, range, onComplete, onError);
        }

        /// <summary>
        /// Download friends' scores.
        /// </summary>
        public void DownloadFriendsScores(string leaderboardName, Action<List<LeaderboardEntry>> onComplete = null, Action<string> onError = null)
        {
            DownloadScores(leaderboardName, LeaderboardRequestType.Friends, 1, 100, onComplete, onError);
        }

#if !DISABLESTEAMWORKS
        private Action<List<LeaderboardEntry>> _downloadOnComplete;
        private Action<string> _downloadOnError;

        private void DownloadScoresInternal(string leaderboardName, SteamLeaderboard_t leaderboard, LeaderboardRequestType requestType, int rangeStart, int rangeEnd, Action<List<LeaderboardEntry>> onComplete, Action<string> onError)
        {
            _pendingDownloadLeaderboardName = leaderboardName;
            _downloadOnComplete = onComplete;
            _downloadOnError = onError;

            var steamRequestType = requestType switch
            {
                LeaderboardRequestType.Global => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                LeaderboardRequestType.GlobalAroundUser => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
                LeaderboardRequestType.Friends => ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
                _ => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal
            };

            var handle = SteamUserStats.DownloadLeaderboardEntries(leaderboard, steamRequestType, rangeStart, rangeEnd);
            _downloadScoresCallResult.Set(handle);

            Log($"Downloading scores from {leaderboardName}");
        }

        private void OnScoresDownloadResult(LeaderboardScoresDownloaded_t result, bool ioFailure)
        {
            var entries = new List<LeaderboardEntry>();

            if (ioFailure)
            {
                var error = $"Failed to download scores from {_pendingDownloadLeaderboardName}";
                LogError(error);
                _downloadOnError?.Invoke(error);
                OnError?.Invoke(error);
            }
            else
            {
                for (int i = 0; i < result.m_cEntryCount; i++)
                {
                    if (SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i, out var entry, null, 0))
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            Rank = entry.m_nGlobalRank,
                            Score = entry.m_nScore,
                            SteamId = entry.m_steamIDUser,
                            PlayerName = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser)
                        });
                    }
                }

                Log($"Downloaded {entries.Count} scores from {_pendingDownloadLeaderboardName}");
                _downloadOnComplete?.Invoke(entries);
                OnScoresDownloaded?.Invoke(_pendingDownloadLeaderboardName, entries);
            }

            _downloadOnComplete = null;
            _downloadOnError = null;
        }
#endif

        #endregion

        #region Helpers

        private bool ValidateState()
        {
#if !DISABLESTEAMWORKS
            if (!IsInitialized)
            {
                LogError("Leaderboard service not initialized!");
                return false;
            }

            if (!SteamCore.Instance.IsInitialized)
            {
                LogError("Steam not initialized!");
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
                Debug.Log($"[SteamToolkit.Leaderboards] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Leaderboards] {message}");
        }

        #endregion
    }

    #region Data Types

    /// <summary>
    /// Leaderboard entry data.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int Rank;
        public int Score;
#if !DISABLESTEAMWORKS
        public CSteamID SteamId;
#endif
        public string PlayerName;
    }

    /// <summary>
    /// Leaderboard sort method.
    /// </summary>
    public enum LeaderboardSortMethod
    {
        Ascending,  // Lower is better (e.g., time)
        Descending  // Higher is better (e.g., score)
    }

    /// <summary>
    /// Leaderboard display type.
    /// </summary>
    public enum LeaderboardDisplayType
    {
        Numeric,
        TimeSeconds,
        TimeMilliSeconds
    }

    /// <summary>
    /// Score upload method.
    /// </summary>
    public enum ScoreUploadMethod
    {
        KeepBest,    // Only update if new score is better
        ForceUpdate  // Always update
    }

    /// <summary>
    /// Leaderboard request type.
    /// </summary>
    public enum LeaderboardRequestType
    {
        Global,           // Top scores
        GlobalAroundUser, // Scores around current user
        Friends           // Friends' scores
    }

    #endregion
}