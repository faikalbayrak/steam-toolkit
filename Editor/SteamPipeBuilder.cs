using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace SteamToolkit.Editor
{
    /// <summary>
    /// SteamPipe builder for uploading builds to Steam.
    /// </summary>
    public static class SteamPipeBuilder
    {
        #region SteamCMD Download

        private const string STEAMCMD_WINDOWS_URL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
        private const string STEAMCMD_MACOS_URL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_osx.tar.gz";
        private const string STEAMCMD_LINUX_URL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz";

        /// <summary>
        /// Check if SteamCMD is installed at the given path.
        /// </summary>
        public static bool IsSteamCmdInstalled(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return File.Exists(path);
        }

        /// <summary>
        /// Get the default SteamCMD installation directory.
        /// </summary>
        public static string GetDefaultSteamCmdDirectory()
        {
#if UNITY_EDITOR_WIN
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "steamcmd");
#elif UNITY_EDITOR_OSX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "steamcmd");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "steamcmd");
#endif
        }

        /// <summary>
        /// Download and install SteamCMD.
        /// </summary>
        public static void DownloadSteamCmd(string installDir, Action<float, string> onProgress, Action<bool, string> onComplete)
        {
            if (string.IsNullOrEmpty(installDir))
            {
                installDir = GetDefaultSteamCmdDirectory();
            }

            // Create directory
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

#if UNITY_EDITOR_WIN
            string url = STEAMCMD_WINDOWS_URL;
            string archiveName = "steamcmd.zip";
#elif UNITY_EDITOR_OSX
            string url = STEAMCMD_MACOS_URL;
            string archiveName = "steamcmd_osx.tar.gz";
#else
            string url = STEAMCMD_LINUX_URL;
            string archiveName = "steamcmd_linux.tar.gz";
#endif

            string archivePath = Path.Combine(installDir, archiveName);

            // Start download
            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckDownload;

            void CheckDownload()
            {
                if (!operation.isDone)
                {
                    onProgress?.Invoke(operation.progress, $"Downloading... {operation.progress * 100:F0}%");
                    return;
                }

                EditorApplication.update -= CheckDownload;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(false, $"Download failed: {request.error}");
                    request.Dispose();
                    return;
                }

                // Save archive
                try
                {
                    File.WriteAllBytes(archivePath, request.downloadHandler.data);
                    onProgress?.Invoke(0.9f, "Extracting...");

                    // Extract
#if UNITY_EDITOR_WIN
                    ExtractZip(archivePath, installDir);
#else
                    ExtractTarGz(archivePath, installDir);
#endif

                    // Cleanup
                    File.Delete(archivePath);

                    // Run once to update
                    string exePath = GetSteamCmdExecutable(installDir);
                    onProgress?.Invoke(0.95f, "Running first-time setup...");
                    
                    RunSteamCmdFirstTime(exePath, () =>
                    {
                        onComplete?.Invoke(true, exePath);
                    });
                }
                catch (Exception ex)
                {
                    onComplete?.Invoke(false, $"Extraction failed: {ex.Message}");
                }

                request.Dispose();
            }
        }

        private static void ExtractZip(string zipPath, string destDir)
        {
            ZipFile.ExtractToDirectory(zipPath, destDir, true);
        }

        private static void ExtractTarGz(string tarGzPath, string destDir)
        {
            // For macOS/Linux, use system tar command
            var startInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{tarGzPath}\" -C \"{destDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        private static string GetSteamCmdExecutable(string installDir)
        {
#if UNITY_EDITOR_WIN
            return Path.Combine(installDir, "steamcmd.exe");
#else
            return Path.Combine(installDir, "steamcmd.sh");
#endif
        }

        private static void RunSteamCmdFirstTime(string exePath, Action onComplete)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "+quit",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            try
            {
                var process = new Process { StartInfo = startInfo };
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    process.Dispose();
                    onComplete?.Invoke();
                };
                process.Start();
            }
            catch
            {
                onComplete?.Invoke();
            }
        }

        #endregion

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