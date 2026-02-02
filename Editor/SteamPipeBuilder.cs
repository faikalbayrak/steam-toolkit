#if UNITY_EDITOR
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

            Debug.Log($"[SteamToolkit] Installing SteamCMD to: {installDir}");

            // Create directory
            try
            {
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                    Debug.Log($"[SteamToolkit] Created directory: {installDir}");
                }
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(false, $"Failed to create directory: {ex.Message}");
                return;
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
            Debug.Log($"[SteamToolkit] Downloading from: {url}");
            Debug.Log($"[SteamToolkit] Archive path: {archivePath}");

            // Start download
            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckDownload;

            void CheckDownload()
            {
                if (!operation.isDone)
                {
                    onProgress?.Invoke(operation.progress * 0.8f, $"Downloading... {operation.progress * 100:F0}%");
                    return;
                }

                EditorApplication.update -= CheckDownload;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[SteamToolkit] Download failed: {request.error}");
                    onComplete?.Invoke(false, $"Download failed: {request.error}");
                    request.Dispose();
                    return;
                }

                Debug.Log($"[SteamToolkit] Download complete. Size: {request.downloadHandler.data.Length} bytes");

                // Save archive
                try
                {
                    onProgress?.Invoke(0.85f, "Saving archive...");
                    File.WriteAllBytes(archivePath, request.downloadHandler.data);
                    Debug.Log($"[SteamToolkit] Archive saved: {archivePath}");

                    if (!File.Exists(archivePath))
                    {
                        throw new Exception("Archive file was not created!");
                    }

                    onProgress?.Invoke(0.9f, "Extracting...");

                    // Extract
#if UNITY_EDITOR_WIN
                    ExtractZipWindows(archivePath, installDir);
#else
                    ExtractTarGz(archivePath, installDir);
#endif

                    string exePath = GetSteamCmdExecutable(installDir);
                    Debug.Log($"[SteamToolkit] Expected executable: {exePath}");

                    if (!File.Exists(exePath))
                    {
                        // List what was extracted
                        var files = Directory.GetFiles(installDir);
                        Debug.LogError($"[SteamToolkit] Executable not found! Files in directory:");
                        foreach (var f in files)
                        {
                            Debug.Log($"  - {f}");
                        }
                        throw new Exception($"SteamCMD executable not found at: {exePath}");
                    }

                    // Cleanup archive
                    if (File.Exists(archivePath))
                    {
                        File.Delete(archivePath);
                    }

                    // Run once to update
                    onProgress?.Invoke(0.95f, "Running first-time setup...");
                    Debug.Log($"[SteamToolkit] Running first-time setup...");
                    
                    RunSteamCmdFirstTime(exePath, (success) =>
                    {
                        if (success)
                        {
                            Debug.Log($"[SteamToolkit] First-time setup complete!");
                            onComplete?.Invoke(true, exePath);
                        }
                        else
                        {
                            // Even if first-time setup fails, the exe exists
                            Debug.LogWarning($"[SteamToolkit] First-time setup had issues, but executable exists.");
                            onComplete?.Invoke(true, exePath);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SteamToolkit] Extraction failed: {ex.Message}\n{ex.StackTrace}");
                    onComplete?.Invoke(false, $"Extraction failed: {ex.Message}");
                }

                request.Dispose();
            }
        }

        private static void ExtractZipWindows(string zipPath, string destDir)
        {
            Debug.Log($"[SteamToolkit] Extracting ZIP: {zipPath} -> {destDir}");
            
            // Use PowerShell for extraction (more reliable than System.IO.Compression in Unity)
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -Path '{zipPath}' -DestinationPath '{destDir}' -Force\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                var process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    Debug.Log($"[SteamToolkit] PowerShell output: {output}");
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning($"[SteamToolkit] PowerShell error: {error}");

                Debug.Log($"[SteamToolkit] PowerShell exit code: {process.ExitCode}");

                if (process.ExitCode != 0)
                {
                    throw new Exception($"PowerShell extraction failed with exit code {process.ExitCode}: {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SteamToolkit] PowerShell extraction failed: {ex.Message}");
                
                // Fallback to System.IO.Compression
                Debug.Log($"[SteamToolkit] Trying System.IO.Compression fallback...");
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, destDir, true);
                    Debug.Log($"[SteamToolkit] System.IO.Compression extraction succeeded.");
                }
                catch (Exception ex2)
                {
                    throw new Exception($"All extraction methods failed. PowerShell: {ex.Message}, System.IO: {ex2.Message}");
                }
            }
        }

        private static void ExtractTarGz(string tarGzPath, string destDir)
        {
            Debug.Log($"[SteamToolkit] Extracting tar.gz: {tarGzPath} -> {destDir}");
            
            // For macOS/Linux, use system tar command
            var startInfo = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{tarGzPath}\" -C \"{destDir}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = Process.Start(startInfo);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"tar extraction failed: {error}");
            }
        }

        private static string GetSteamCmdExecutable(string installDir)
        {
#if UNITY_EDITOR_WIN
            return Path.Combine(installDir, "steamcmd.exe");
#else
            return Path.Combine(installDir, "steamcmd.sh");
#endif
        }

        private static void RunSteamCmdFirstTime(string exePath, Action<bool> onComplete)
        {
            Debug.Log($"[SteamToolkit] Starting SteamCMD first run: {exePath}");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "+quit",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                var process = new Process { StartInfo = startInfo };
                process.EnableRaisingEvents = true;
                
                process.Exited += (s, e) =>
                {
                    // Capture exit code BEFORE dispose
                    int exitCode = -1;
                    try
                    {
                        exitCode = process.ExitCode;
                    }
                    catch { }
                    
                    Debug.Log($"[SteamToolkit] SteamCMD first run exit code: {exitCode}");
                    process.Dispose();
                    
                    // Call on main thread
                    bool success = exitCode == 0 || exitCode == 7 || exitCode == -1;
                    EditorApplication.delayCall += () => onComplete?.Invoke(success);
                };
                
                process.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SteamToolkit] Failed to run SteamCMD: {ex.Message}");
                onComplete?.Invoke(false);
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
                    int exitCode = 0;
                    try { exitCode = process.ExitCode; } catch { }
                    process.Dispose();
                    EditorApplication.delayCall += () => onComplete?.Invoke(exitCode);
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
        /// Run SteamCMD in interactive mode (visible window).
        /// Use this for first-time login or when Steam Guard is required.
        /// </summary>
        public static void RunSteamCmdInteractive(SteamBuildConfig config, string password, Action<string> onOutput)
        {
            if (string.IsNullOrEmpty(config.SteamCmdPath) || !File.Exists(config.SteamCmdPath))
            {
                onOutput?.Invoke("ERROR: SteamCMD path not set or file not found.");
                return;
            }

            string scriptPath = Path.Combine(config.ContentBuilderPath, "scripts", $"app_{config.AppId}.vdf");
            
            if (!File.Exists(scriptPath))
            {
                onOutput?.Invoke("ERROR: App VDF file not found. Generate VDF files first.");
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

            // Start process in interactive mode
            var startInfo = new ProcessStartInfo
            {
                FileName = config.SteamCmdPath,
                Arguments = args.ToString(),
                UseShellExecute = true,  // Opens visible window
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(config.SteamCmdPath)
            };

            try
            {
                Process.Start(startInfo);
                onOutput?.Invoke("SteamCMD window opened. If Steam Guard is required, enter the code in the window.");
                onOutput?.Invoke("Check the SteamCMD window for upload progress.");
            }
            catch (Exception ex)
            {
                onOutput?.Invoke($"ERROR: Failed to start SteamCMD: {ex.Message}");
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
#endif