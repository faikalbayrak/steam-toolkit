using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace SteamToolkit.Editor
{
    /// <summary>
    /// SteamPipe builder for uploading builds to Steam.
    /// </summary>
    public static class SteamPipeBuilder
    {
        #region Public Methods

        /// <summary>
        /// Generate app build VDF file.
        /// </summary>
        public static string GenerateAppVdf(SteamBuildConfig config, string description, string branch)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("\"appbuild\"");
            sb.AppendLine("{");
            sb.AppendLine($"\t\"appid\" \"{config.AppId}\"");
            sb.AppendLine($"\t\"desc\" \"{description}\"");
            sb.AppendLine($"\t\"buildoutput\" \"..\\output\"");
            sb.AppendLine($"\t\"contentroot\" \"..\\content\"");
            sb.AppendLine($"\t\"setlive\" \"{(config.SetLiveOnUpload ? branch : "")}\"");
            sb.AppendLine($"\t\"preview\" \"{(config.PreviewOnly ? "1" : "0")}\"");
            sb.AppendLine("\t\"depots\"");
            sb.AppendLine("\t{");
            
            foreach (var depot in config.Depots)
            {
                sb.AppendLine($"\t\t\"{depot.DepotId}\" \"depot_{depot.DepotId}.vdf\"");
            }
            
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generate depot build VDF file.
        /// </summary>
        public static string GenerateDepotVdf(DepotConfig depot, string contentRoot)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("\"DepotBuildConfig\"");
            sb.AppendLine("{");
            sb.AppendLine($"\t\"DepotID\" \"{depot.DepotId}\"");
            sb.AppendLine($"\t\"contentroot\" \"{contentRoot.Replace("\\", "\\\\")}\"");
            sb.AppendLine("\t\"FileMapping\"");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\t\"LocalPath\" \"{depot.LocalPath}\"");
            sb.AppendLine($"\t\t\"DepotPath\" \"{depot.DepotPath}\"");
            sb.AppendLine($"\t\t\"recursive\" \"{(depot.Recursive ? "1" : "0")}\"");
            sb.AppendLine("\t}");
            
            if (depot.Exclude != null && depot.Exclude.Count > 0)
            {
                sb.AppendLine("\t\"FileExclusion\"");
                sb.AppendLine("\t{");
                foreach (var exclude in depot.Exclude)
                {
                    sb.AppendLine($"\t\t\"{exclude}\"");
                }
                sb.AppendLine("\t}");
            }
            
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Write VDF files to disk.
        /// </summary>
        public static void WriteVdfFiles(SteamBuildConfig config, string description, string branch)
        {
            string scriptsPath = Path.Combine(config.ContentBuilderPath, "scripts");
            
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
            }

            // Write app VDF
            string appVdf = GenerateAppVdf(config, description, branch);
            string appVdfPath = Path.Combine(scriptsPath, $"app_{config.AppId}.vdf");
            File.WriteAllText(appVdfPath, appVdf);
            Debug.Log($"[SteamToolkit] Written: {appVdfPath}");

            // Write depot VDFs
            foreach (var depot in config.Depots)
            {
                string contentRoot = Path.Combine(config.ContentBuilderPath, "content", depot.ContentRoot);
                string depotVdf = GenerateDepotVdf(depot, contentRoot);
                string depotVdfPath = Path.Combine(scriptsPath, $"depot_{depot.DepotId}.vdf");
                File.WriteAllText(depotVdfPath, depotVdf);
                Debug.Log($"[SteamToolkit] Written: {depotVdfPath}");
            }
        }

        /// <summary>
        /// Run SteamCMD to upload build.
        /// </summary>
        public static void RunSteamCmd(SteamBuildConfig config, string password, Action<string> onOutput, Action<int> onComplete)
        {
            if (string.IsNullOrEmpty(config.SteamCmdPath) || !File.Exists(config.SteamCmdPath))
            {
                onOutput?.Invoke("ERROR: SteamCMD path not set or file not found.");
                onComplete?.Invoke(-1);
                return;
            }

            string scriptPath = Path.Combine(config.ContentBuilderPath, "scripts", $"app_{config.AppId}.vdf");
            
            if (!File.Exists(scriptPath))
            {
                onOutput?.Invoke("ERROR: App VDF file not found. Generate VDF files first.");
                onComplete?.Invoke(-1);
                return;
            }

            // Build command arguments
            var args = new StringBuilder();
            args.Append($"+login \"{config.Username}\"");
            
            if (!string.IsNullOrEmpty(password))
            {
                args.Append($" \"{password}\"");
            }
            
            args.Append($" +run_app_build \"{scriptPath}\"");
            args.Append(" +quit");

            // Start process
            var startInfo = new ProcessStartInfo
            {
                FileName = config.SteamCmdPath,
                Arguments = args.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(config.SteamCmdPath)
            };

            try
            {
                var process = new Process { StartInfo = startInfo };
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        onOutput?.Invoke(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        onOutput?.Invoke($"ERROR: {e.Data}");
                    }
                };

                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    onComplete?.Invoke(process.ExitCode);
                    process.Dispose();
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                onOutput?.Invoke($"Started SteamCMD...");
            }
            catch (Exception ex)
            {
                onOutput?.Invoke($"ERROR: Failed to start SteamCMD: {ex.Message}");
                onComplete?.Invoke(-1);
            }
        }

        /// <summary>
        /// Copy build output to ContentBuilder content folder.
        /// </summary>
        public static bool CopyBuildToContent(SteamBuildConfig config, DepotConfig depot, string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"[SteamToolkit] Source path not found: {sourcePath}");
                return false;
            }

            string destPath = Path.Combine(config.ContentBuilderPath, "content", depot.ContentRoot);

            try
            {
                // Clear destination
                if (Directory.Exists(destPath))
                {
                    Directory.Delete(destPath, true);
                }

                // Copy files
                CopyDirectory(sourcePath, destPath);
                
                Debug.Log($"[SteamToolkit] Copied build to: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SteamToolkit] Failed to copy build: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get default SteamCMD path based on OS.
        /// </summary>
        public static string GetDefaultSteamCmdPath()
        {
#if UNITY_EDITOR_WIN
            // Common Windows locations
            var paths = new[]
            {
                @"C:\steamcmd\steamcmd.exe",
                @"C:\SteamCMD\steamcmd.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "steamcmd", "steamcmd.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "steamcmd", "steamcmd.exe")
            };
            
            foreach (var path in paths)
            {
                if (File.Exists(path)) return path;
            }
            
            return @"C:\steamcmd\steamcmd.exe";
#elif UNITY_EDITOR_OSX
            return "/usr/local/bin/steamcmd";
#else
            return "/usr/bin/steamcmd";
#endif
        }

        /// <summary>
        /// Get default ContentBuilder path.
        /// </summary>
        public static string GetDefaultContentBuilderPath()
        {
            return Path.Combine(Application.dataPath, "..", "SteamContentBuilder");
        }

        /// <summary>
        /// Initialize ContentBuilder folder structure.
        /// </summary>
        public static void InitializeContentBuilder(string path)
        {
            var folders = new[]
            {
                path,
                Path.Combine(path, "content"),
                Path.Combine(path, "scripts"),
                Path.Combine(path, "output")
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    Debug.Log($"[SteamToolkit] Created: {folder}");
                }
            }
        }

        /// <summary>
        /// Build description from template.
        /// </summary>
        public static string BuildDescription(string template, string version = null)
        {
            var result = template;
            result = result.Replace("{version}", version ?? Application.version);
            result = result.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            result = result.Replace("{unity}", Application.unityVersion);
            result = result.Replace("{product}", Application.productName);
            return result;
        }

        #endregion

        #region Private Methods

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        #endregion
    }
}