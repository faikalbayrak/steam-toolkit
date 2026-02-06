using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamToolkit
{
    /// <summary>
    /// Steam Build configuration for SteamPipe uploads.
    /// App and Depot info comes from SteamConfig.
    /// Create via: Create > Steam Toolkit > Build Config
    /// </summary>
    [CreateAssetMenu(fileName = "SteamBuildConfig", menuName = "Steam Toolkit/Build Config", order = 2)]
    public class SteamBuildConfig : ScriptableObject
    {
        [Header("Steam Account")]
        [Tooltip("Steam account username for SteamCMD login")]
        public string Username = "";

        [Tooltip("Store password in config (not recommended for shared projects)")]
        public bool StorePassword = false;

        [Tooltip("Steam account password (only if StorePassword is true)")]
        public string Password = "";

        [Header("SteamCMD")]
        [Tooltip("Path to SteamCMD executable")]
        public string SteamCmdPath = "";

        [Tooltip("Path to ContentBuilder folder (contains scripts, content, output)")]
        public string ContentBuilderPath = "";

        [Header("Build Settings")]
        [Tooltip("Set live after successful upload")]
        public bool SetLiveOnUpload = false;

        [Tooltip("Preview upload without actually uploading")]
        public bool PreviewOnly = false;

        [Tooltip("Build description template")]
        public string DescriptionTemplate = "Build {version} - {date}";

        [Header("File Exclusions")]
        [Tooltip("File patterns to exclude from upload")]
        public List<string> GlobalExclusions = new List<string> { "*.pdb", "*.log" };

        #region Properties from SteamConfig

        /// <summary>
        /// Get the active App ID from SteamConfig.
        /// </summary>
        public uint AppId => SteamConfig.Instance?.AppId ?? 480;

        /// <summary>
        /// Get the active Depot ID from SteamConfig.
        /// </summary>
        public uint DepotId => SteamConfig.Instance?.DepotId ?? 481;

        /// <summary>
        /// Get the active app entry from SteamConfig.
        /// </summary>
        public SteamAppEntry ActiveApp => SteamConfig.Instance?.ActiveApp;

        /// <summary>
        /// Get the default branch for the active app.
        /// </summary>
        public string DefaultBranch => SteamConfig.Instance?.ActiveApp?.DefaultBranch ?? "default";

        /// <summary>
        /// Get available branches for the active app.
        /// </summary>
        public List<string> Branches => SteamConfig.Instance?.ActiveApp?.Branches ?? new List<string> { "default" };

        /// <summary>
        /// Get depots for the active app.
        /// </summary>
        public List<DepotConfig> Depots => SteamConfig.Instance?.ActiveApp?.Depots ?? new List<DepotConfig>();

        /// <summary>
        /// Get the active app name.
        /// </summary>
        public string AppName => SteamConfig.Instance?.ActiveApp?.Name ?? "Unknown App";

        /// <summary>
        /// Get the active app type.
        /// </summary>
        public SteamAppType AppType => SteamConfig.Instance?.ActiveApp?.Type ?? SteamAppType.Main;

        #endregion
    }
}