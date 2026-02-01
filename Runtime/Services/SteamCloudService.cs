using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit
{
    /// <summary>
    /// Steam Cloud Save service.
    /// Handles remote storage for save files and game data.
    /// </summary>
    public class SteamCloudService : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a file is written successfully.
        /// </summary>
        public event Action<string> OnFileWritten;

        /// <summary>
        /// Fired when a file is read successfully.
        /// </summary>
        public event Action<string, byte[]> OnFileRead;

        /// <summary>
        /// Fired when a file is deleted.
        /// </summary>
        public event Action<string> OnFileDeleted;

        /// <summary>
        /// Fired on any error.
        /// </summary>
        public event Action<string> OnError;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Is Steam Cloud enabled for this user?
        /// </summary>
        public bool IsCloudEnabled
        {
            get
            {
#if !DISABLESTEAMWORKS
                return SteamRemoteStorage.IsCloudEnabledForAccount() && 
                       SteamRemoteStorage.IsCloudEnabledForApp();
#else
                return false;
#endif
            }
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (IsInitialized) return;

            IsInitialized = true;
            Log("Cloud service initialized.");

#if !DISABLESTEAMWORKS
            if (!IsCloudEnabled)
            {
                LogWarning("Steam Cloud is disabled for this user or app.");
            }
#endif
        }

        public void Dispose()
        {
            IsInitialized = false;
            Log("Cloud service disposed.");
        }

        #endregion

        #region Quota

        /// <summary>
        /// Get cloud storage quota info.
        /// </summary>
        /// <returns>Quota info (total bytes, available bytes)</returns>
        public CloudQuotaInfo GetQuota()
        {
            var info = new CloudQuotaInfo();

#if !DISABLESTEAMWORKS
            if (SteamRemoteStorage.GetQuota(out ulong total, out ulong available))
            {
                info.TotalBytes = total;
                info.AvailableBytes = available;
                info.UsedBytes = total - available;
            }
#endif

            return info;
        }

        #endregion

        #region File List

        /// <summary>
        /// Get list of all files in cloud storage.
        /// </summary>
        /// <returns>List of cloud files</returns>
        public List<CloudFileInfo> GetAllFiles()
        {
            var files = new List<CloudFileInfo>();

#if !DISABLESTEAMWORKS
            int fileCount = SteamRemoteStorage.GetFileCount();

            for (int i = 0; i < fileCount; i++)
            {
                string fileName = SteamRemoteStorage.GetFileNameAndSize(i, out int fileSize);

                if (!string.IsNullOrEmpty(fileName))
                {
                    files.Add(new CloudFileInfo
                    {
                        FileName = fileName,
                        SizeBytes = fileSize,
                        Timestamp = SteamRemoteStorage.GetFileTimestamp(fileName),
                        Exists = SteamRemoteStorage.FileExists(fileName)
                    });
                }
            }
#endif

            return files;
        }

        /// <summary>
        /// Get file count in cloud storage.
        /// </summary>
        public int GetFileCount()
        {
#if !DISABLESTEAMWORKS
            return SteamRemoteStorage.GetFileCount();
#else
            return 0;
#endif
        }

        /// <summary>
        /// Check if a file exists in cloud storage.
        /// </summary>
        public bool FileExists(string fileName)
        {
#if !DISABLESTEAMWORKS
            return SteamRemoteStorage.FileExists(fileName);
#else
            return false;
#endif
        }

        /// <summary>
        /// Get file size in bytes.
        /// </summary>
        public int GetFileSize(string fileName)
        {
#if !DISABLESTEAMWORKS
            return SteamRemoteStorage.GetFileSize(fileName);
#else
            return 0;
#endif
        }

        #endregion

        #region Write Files

        /// <summary>
        /// Write string data to cloud storage.
        /// </summary>
        /// <param name="fileName">File name (e.g., "save1.dat")</param>
        /// <param name="data">String data to write</param>
        /// <returns>True if successful</returns>
        public bool WriteString(string fileName, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return WriteBytes(fileName, new byte[0]);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(data);
            return WriteBytes(fileName, bytes);
        }

        /// <summary>
        /// Write JSON object to cloud storage.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="fileName">File name</param>
        /// <param name="obj">Object to serialize</param>
        /// <returns>True if successful</returns>
        public bool WriteJson<T>(string fileName, T obj)
        {
            try
            {
                string json = JsonUtility.ToJson(obj, true);
                return WriteString(fileName, json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to serialize object: {ex.Message}");
                OnError?.Invoke($"Serialization error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Write byte array to cloud storage.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="data">Byte data to write</param>
        /// <returns>True if successful</returns>
        public bool WriteBytes(string fileName, byte[] data)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                return false;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                LogError("File name cannot be empty.");
                return false;
            }

            if (data == null)
            {
                data = new byte[0];
            }

            bool success = SteamRemoteStorage.FileWrite(fileName, data, data.Length);

            if (success)
            {
                Log($"File written: {fileName} ({data.Length} bytes)");
                OnFileWritten?.Invoke(fileName);
            }
            else
            {
                LogError($"Failed to write file: {fileName}");
                OnError?.Invoke($"Failed to write file: {fileName}");
            }

            return success;
#else
            return false;
#endif
        }

        #endregion

        #region Read Files

        /// <summary>
        /// Read string data from cloud storage.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>String data or null if failed</returns>
        public string ReadString(string fileName)
        {
            byte[] bytes = ReadBytes(fileName);

            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Read JSON object from cloud storage.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="fileName">File name</param>
        /// <returns>Deserialized object or default</returns>
        public T ReadJson<T>(string fileName)
        {
            string json = ReadString(fileName);

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                LogError($"Failed to deserialize object: {ex.Message}");
                OnError?.Invoke($"Deserialization error: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Read byte array from cloud storage.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>Byte data or null if failed</returns>
        public byte[] ReadBytes(string fileName)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                return null;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                LogError("File name cannot be empty.");
                return null;
            }

            if (!SteamRemoteStorage.FileExists(fileName))
            {
                LogError($"File not found: {fileName}");
                return null;
            }

            int fileSize = SteamRemoteStorage.GetFileSize(fileName);
            byte[] data = new byte[fileSize];

            int bytesRead = SteamRemoteStorage.FileRead(fileName, data, fileSize);

            if (bytesRead == fileSize)
            {
                Log($"File read: {fileName} ({bytesRead} bytes)");
                OnFileRead?.Invoke(fileName, data);
                return data;
            }
            else
            {
                LogError($"Failed to read file: {fileName} (read {bytesRead}/{fileSize} bytes)");
                OnError?.Invoke($"Failed to read file: {fileName}");
                return null;
            }
#else
            return null;
#endif
        }

        #endregion

        #region Delete Files

        /// <summary>
        /// Delete a file from cloud storage.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>True if successful</returns>
        public bool DeleteFile(string fileName)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                return false;
            }

            if (string.IsNullOrEmpty(fileName))
            {
                LogError("File name cannot be empty.");
                return false;
            }

            if (!SteamRemoteStorage.FileExists(fileName))
            {
                LogWarning($"File not found: {fileName}");
                return false;
            }

            bool success = SteamRemoteStorage.FileDelete(fileName);

            if (success)
            {
                Log($"File deleted: {fileName}");
                OnFileDeleted?.Invoke(fileName);
            }
            else
            {
                LogError($"Failed to delete file: {fileName}");
                OnError?.Invoke($"Failed to delete file: {fileName}");
            }

            return success;
#else
            return false;
#endif
        }

        /// <summary>
        /// Forget a file (remove from cloud but keep local).
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns>True if successful</returns>
        public bool ForgetFile(string fileName)
        {
#if !DISABLESTEAMWORKS
            if (!ValidateState())
            {
                return false;
            }

            bool success = SteamRemoteStorage.FileForget(fileName);

            if (success)
            {
                Log($"File forgotten: {fileName}");
            }
            else
            {
                LogError($"Failed to forget file: {fileName}");
            }

            return success;
#else
            return false;
#endif
        }

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

            if (!IsCloudEnabled)
            {
                LogError("Steam Cloud is disabled!");
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
                Debug.Log($"[SteamToolkit.Cloud] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SteamToolkit.Cloud] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SteamToolkit.Cloud] {message}");
        }

        #endregion
    }

    #region Data Classes

    /// <summary>
    /// Cloud storage quota information.
    /// </summary>
    [Serializable]
    public class CloudQuotaInfo
    {
        public ulong TotalBytes;
        public ulong AvailableBytes;
        public ulong UsedBytes;

        public string TotalFormatted => FormatBytes(TotalBytes);
        public string AvailableFormatted => FormatBytes(AvailableBytes);
        public string UsedFormatted => FormatBytes(UsedBytes);
        public float UsedPercent => TotalBytes > 0 ? (float)UsedBytes / TotalBytes * 100f : 0f;

        private static string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Cloud file information.
    /// </summary>
    [Serializable]
    public class CloudFileInfo
    {
        public string FileName;
        public int SizeBytes;
        public long Timestamp;
        public bool Exists;

        public string SizeFormatted
        {
            get
            {
                if (SizeBytes < 1024)
                    return $"{SizeBytes} B";
                else if (SizeBytes < 1024 * 1024)
                    return $"{SizeBytes / 1024f:0.#} KB";
                else
                    return $"{SizeBytes / (1024f * 1024f):0.##} MB";
            }
        }

        public DateTime LastModified => DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
    }

    #endregion
}