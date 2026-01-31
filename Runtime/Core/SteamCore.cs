using System;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Main Steam Toolkit singleton manager.
    /// Initializes and manages all Steam services.
    /// </summary>
    public class SteamCore : MonoBehaviour
    {
        #region Singleton

        private static SteamCore _instance;
        private static bool _isQuitting;

        public static SteamCore Instance
        {
            get
            {
                if (_isQuitting)
                    return null;

                if (_instance == null)
                {
                    _instance = FindObjectOfType<SteamCore>();

                    if (_instance == null)
                    {
                        var go = new GameObject("[SteamToolkit]");
                        _instance = go.AddComponent<SteamCore>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        #endregion

        #region Events

        public event Action OnInitialized;
        public event Action<string> OnInitializationFailed;
        public event Action OnShutdown;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public SteamConfig Config => _config;

#if !DISABLESTEAMWORKS
        public CSteamID SteamId { get; private set; }
        public string DisplayName { get; private set; }
        public string SteamIdString => SteamId.m_SteamID.ToString();
#else
        public ulong SteamId => 0;
        public string DisplayName => "Steam Disabled";
        public string SteamIdString => "0";
#endif

        #endregion

        #region Services

#if !DISABLESTEAMWORKS
        public SteamAuthService Auth { get; private set; }
#endif

        #endregion

        #region Private Fields

        private SteamConfig _config;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Load config
            _config = SteamConfig.Instance;

            // Auto initialize if enabled
            if (_config != null && _config.AutoInitialize)
            {
                Initialize();
            }
        }

        private void Update()
        {
#if !DISABLESTEAMWORKS
            if (IsInitialized)
            {
                SteamAPI.RunCallbacks();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            Shutdown();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Manually initialize Steam.
        /// Use this if AutoInitialize is disabled.
        /// </summary>
        public void Initialize()
        {
#if DISABLESTEAMWORKS
            LogWarning("Steamworks is disabled! DISABLESTEAMWORKS is defined.");
            if (_config != null && _config.AllowWithoutSteam)
            {
                Log("AllowWithoutSteam enabled, continuing in offline mode.");
                return;
            }
            OnInitializationFailed?.Invoke("Steamworks is disabled.");
            return;
#else
            if (IsInitialized)
            {
                Log("Already initialized.");
                return;
            }

            if (_config == null)
            {
                _config = SteamConfig.Instance;
                if (_config == null)
                {
                    LogError("SteamConfig not found!");
                    OnInitializationFailed?.Invoke("SteamConfig not found!");
                    return;
                }
            }

            if (!_config.IsValid())
            {
                OnInitializationFailed?.Invoke("SteamConfig is invalid!");
                return;
            }

            Log($"Initializing... AppId: {_config.AppId}");

            try
            {
                // Check RestartAppIfNecessary
                if (_config.CheckRestartApp)
                {
                    if (SteamAPI.RestartAppIfNecessary(new AppId_t(_config.AppId)))
                    {
                        LogWarning("Game must be launched through Steam!");
                        Application.Quit();
                        return;
                    }
                }

                // Initialize Steam
                if (!SteamAPI.Init())
                {
                    if (_config.AllowWithoutSteam)
                    {
                        LogWarning("Steam failed to initialize but AllowWithoutSteam enabled. Continuing in offline mode.");
                        return;
                    }

                    LogError("SteamAPI.Init() failed! Is Steam running?");
                    OnInitializationFailed?.Invoke("Steam failed to initialize. Is Steam client running?");
                    return;
                }

                // Get user info
                IsInitialized = true;
                SteamId = SteamUser.GetSteamID();
                DisplayName = SteamFriends.GetPersonaName();

                // Initialize services
                InitializeServices();

                Log($"Initialized! User: {DisplayName} ({SteamIdString})");
                OnInitialized?.Invoke();
            }
            catch (DllNotFoundException ex)
            {
                LogError($"Steam DLL not found: {ex.Message}");
                OnInitializationFailed?.Invoke("Steam DLL not found. Is Steamworks.NET installed?");
            }
            catch (Exception ex)
            {
                LogError($"Initialization error: {ex.Message}");
                OnInitializationFailed?.Invoke(ex.Message);
            }
#endif
        }

        private void InitializeServices()
        {
#if !DISABLESTEAMWORKS
            // Auth service is always active
            Auth = new SteamAuthService();
            Auth.Initialize();
#endif
        }

        /// <summary>
        /// Shutdown Steam.
        /// </summary>
        public void Shutdown()
        {
            if (!IsInitialized) return;

            Log("Shutting down...");

#if !DISABLESTEAMWORKS
            // Dispose services
            Auth?.Dispose();

            // Shutdown Steam
            SteamAPI.Shutdown();
#endif
            
            IsInitialized = false;
            OnShutdown?.Invoke();
            Log("Shutdown complete.");
        }

        #endregion

        #region User Info

#if !DISABLESTEAMWORKS
        /// <summary>
        /// Get another user's display name.
        /// </summary>
        public string GetPersonaName(CSteamID steamId)
        {
            if (!IsInitialized) return "Unknown";
            return SteamFriends.GetFriendPersonaName(steamId);
        }

        /// <summary>
        /// Get user's avatar texture.
        /// </summary>
        public Texture2D GetAvatar(CSteamID steamId, AvatarSize size = AvatarSize.Medium)
        {
            if (!IsInitialized) return null;

            int imageId = size switch
            {
                AvatarSize.Small => SteamFriends.GetSmallFriendAvatar(steamId),
                AvatarSize.Medium => SteamFriends.GetMediumFriendAvatar(steamId),
                AvatarSize.Large => SteamFriends.GetLargeFriendAvatar(steamId),
                _ => SteamFriends.GetMediumFriendAvatar(steamId)
            };

            if (imageId == -1 || imageId == 0) return null;

            if (!SteamUtils.GetImageSize(imageId, out uint width, out uint height)) return null;

            byte[] imageData = new byte[width * height * 4];
            if (!SteamUtils.GetImageRGBA(imageId, imageData, imageData.Length)) return null;

            var texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(imageData);
            FlipTextureVertically(texture);
            texture.Apply();

            return texture;
        }

        private void FlipTextureVertically(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            var flipped = new Color[pixels.Length];

            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    flipped[(height - 1 - y) * width + x] = pixels[y * width + x];
                }
            }

            texture.SetPixels(flipped);
        }
#endif

        public enum AvatarSize
        {
            Small,  // 32x32
            Medium, // 64x64
            Large   // 184x184
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (_config == null || _config.EnableDebugLogs)
                Debug.Log($"[SteamToolkit] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SteamToolkit] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit] {message}");
        }

        #endregion
    }
}