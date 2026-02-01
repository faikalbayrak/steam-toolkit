using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamToolkit
{
    /// <summary>
    /// Steam Build configuration for SteamPipe uploads.
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

        [Header("App Configuration")]
        [Tooltip("Steam App ID")]
        public uint AppId = 480;

        [Tooltip("Default Depot ID")]
        public uint DefaultDepotId = 481;

        [Header("Branches")]
        [Tooltip("Default branch to upload to")]
        public string DefaultBranch = "default";

        [Tooltip("List of available branches")]
        public List<string> Branches = new List<string> { "default", "beta", "playtest" };

        [Header("Depots")]
        [Tooltip("Depot configurations")]
        public List<DepotConfig> Depots = new List<DepotConfig>();

        [Header("Build Settings")]
        [Tooltip("Set live after successful upload")]
        public bool SetLiveOnUpload = false;

        [Tooltip("Preview upload without actually uploading")]
        public bool PreviewOnly = false;

        [Tooltip("Build description template")]
        public string DescriptionTemplate = "Build {version} - {date}";
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
}