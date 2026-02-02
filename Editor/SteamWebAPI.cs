#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

namespace SteamToolkit.Editor
{
    /// <summary>
    /// Steam Web API helper for fetching achievement schemas in Editor.
    /// </summary>
    public static class SteamWebAPI
    {
        private const string BASE_URL = "https://api.steampowered.com";

        /// <summary>
        /// Fetch achievement schema from Steam Web API.
        /// </summary>
        public static void GetAchievementSchema(string apiKey, uint appId, Action<List<WebAchievementInfo>> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Publisher API Key is not set. Get one from https://partner.steamgames.com/pub/webapi");
                return;
            }

            string url = $"{BASE_URL}/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appId}";

            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckRequest;

            void CheckRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckRequest;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    request.Dispose();
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<SchemaResponse>(request.downloadHandler.text);
                    
                    if (response?.game?.availableGameStats?.achievements == null)
                    {
                        onError?.Invoke("No achievements found or invalid response.");
                        request.Dispose();
                        return;
                    }

                    var achievements = new List<WebAchievementInfo>();
                    foreach (var ach in response.game.availableGameStats.achievements)
                    {
                        achievements.Add(new WebAchievementInfo
                        {
                            ApiName = ach.name,
                            DisplayName = ach.displayName,
                            Description = ach.description,
                            IconUrl = ach.icon,
                            IconGrayUrl = ach.icongray,
                            Hidden = ach.hidden == 1
                        });
                    }

                    onSuccess?.Invoke(achievements);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Fetch stats schema from Steam Web API.
        /// </summary>
        public static void GetStatsSchema(string apiKey, uint appId, Action<List<WebStatInfo>> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onError?.Invoke("Publisher API Key is not set. Get one from https://partner.steamgames.com/pub/webapi");
                return;
            }

            string url = $"{BASE_URL}/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appId}";

            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckRequest;

            void CheckRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckRequest;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    request.Dispose();
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<SchemaResponse>(request.downloadHandler.text);
                    
                    if (response?.game?.availableGameStats?.stats == null)
                    {
                        onError?.Invoke("No stats found or invalid response.");
                        request.Dispose();
                        return;
                    }

                    var stats = new List<WebStatInfo>();
                    foreach (var stat in response.game.availableGameStats.stats)
                    {
                        stats.Add(new WebStatInfo
                        {
                            ApiName = stat.name,
                            DisplayName = string.IsNullOrEmpty(stat.displayName) ? stat.name : stat.displayName,
                            DefaultValue = stat.defaultvalue
                        });
                    }

                    onSuccess?.Invoke(stats);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Fetch global achievement percentages.
        /// </summary>
        public static void GetGlobalAchievementPercentages(uint appId, Action<Dictionary<string, float>> onSuccess, Action<string> onError)
        {
            string url = $"{BASE_URL}/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?gameid={appId}";

            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckRequest;

            void CheckRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckRequest;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    request.Dispose();
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<GlobalPercentagesResponse>(request.downloadHandler.text);
                    
                    var percentages = new Dictionary<string, float>();
                    if (response?.achievementpercentages?.achievements != null)
                    {
                        foreach (var ach in response.achievementpercentages.achievements)
                        {
                            percentages[ach.name] = ach.percent;
                        }
                    }

                    onSuccess?.Invoke(percentages);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}");
                }

                request.Dispose();
            }
        }

        /// <summary>
        /// Fetch inventory item definitions using Publisher API Key.
        /// Requires Publisher API Key from partner.steamgames.com
        /// </summary>
        public static void GetInventoryItemDefinitions(string publisherKey, uint appId, Action<List<WebInventoryItem>> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrEmpty(publisherKey))
            {
                onError?.Invoke("Publisher API Key is not set. Get one from https://partner.steamgames.com/pub/webapi");
                return;
            }

            string url = $"{BASE_URL}/IInventoryService/GetItemDefMeta/v1/?key={publisherKey}&appid={appId}";

            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckRequest;

            void CheckRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckRequest;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Request failed: {request.error}");
                    request.Dispose();
                    return;
                }

                try
                {
                    // Check for Steam error response
                    if (request.downloadHandler.text.Contains("\"success\":false") || 
                        request.downloadHandler.text.Contains("Access Denied"))
                    {
                        onError?.Invoke("Access denied. Make sure you're using a Publisher API Key (not regular Web API Key) and you have access to this app.");
                        request.Dispose();
                        return;
                    }

                    var response = JsonUtility.FromJson<InventoryItemDefMetaResponse>(request.downloadHandler.text);
                    
                    var items = new List<WebInventoryItem>();
                    
                    if (response?.response?.items != null)
                    {
                        foreach (var item in response.response.items)
                        {
                            items.Add(new WebInventoryItem
                            {
                                ItemDefId = item.itemdefid,
                                Modified = item.modified
                            });
                        }
                    }

                    // If we got items, fetch full definitions
                    if (items.Count > 0)
                    {
                        GetInventoryItemDefinitionsDetail(publisherKey, appId, items, onSuccess, onError);
                    }
                    else
                    {
                        onSuccess?.Invoke(items);
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parse error: {ex.Message}\n\nResponse: {request.downloadHandler.text}");
                }

                request.Dispose();
            }
        }

        private static void GetInventoryItemDefinitionsDetail(string publisherKey, uint appId, List<WebInventoryItem> items, Action<List<WebInventoryItem>> onSuccess, Action<string> onError)
        {
            // Build itemdefids parameter
            var itemDefIds = string.Join(",", items.ConvertAll(i => i.ItemDefId.ToString()));
            string url = $"{BASE_URL}/IInventoryService/GetItemDef/v1/?key={publisherKey}&appid={appId}&itemdefids={itemDefIds}";

            var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckRequest;

            void CheckRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckRequest;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Return basic items without details
                    onSuccess?.Invoke(items);
                    request.Dispose();
                    return;
                }

                try
                {
                    var response = JsonUtility.FromJson<InventoryItemDefResponse>(request.downloadHandler.text);
                    
                    if (response?.response?.itemdefs != null)
                    {
                        foreach (var def in response.response.itemdefs)
                        {
                            var item = items.Find(i => i.ItemDefId == def.itemdefid);
                            if (item != null)
                            {
                                item.Name = def.name;
                                item.Description = def.description;
                                item.Type = def.type;
                                item.Price = def.price;
                                item.IconUrl = def.icon_url;
                                item.Tradable = def.tradable;
                                item.Marketable = def.marketable;
                                item.Commodity = def.commodity;
                            }
                        }
                    }

                    onSuccess?.Invoke(items);
                }
                catch (Exception)
                {
                    // Return basic items without details on parse error
                    onSuccess?.Invoke(items);
                }

                request.Dispose();
            }
        }

        #region JSON Response Classes

        [Serializable]
        private class SchemaResponse
        {
            public GameSchema game;
        }

        [Serializable]
        private class GameSchema
        {
            public string gameName;
            public string gameVersion;
            public AvailableGameStats availableGameStats;
        }

        [Serializable]
        private class AvailableGameStats
        {
            public AchievementSchema[] achievements;
            public StatSchema[] stats;
        }

        [Serializable]
        private class AchievementSchema
        {
            public string name;
            public int defaultvalue;
            public string displayName;
            public int hidden;
            public string description;
            public string icon;
            public string icongray;
        }

        [Serializable]
        private class StatSchema
        {
            public string name;
            public int defaultvalue;
            public string displayName;
        }

        [Serializable]
        private class GlobalPercentagesResponse
        {
            public AchievementPercentages achievementpercentages;
        }

        [Serializable]
        private class AchievementPercentages
        {
            public AchievementPercent[] achievements;
        }

        [Serializable]
        private class AchievementPercent
        {
            public string name;
            public float percent;
        }

        // Inventory responses
        [Serializable]
        private class InventoryItemDefMetaResponse
        {
            public InventoryItemDefMetaResult response;
        }

        [Serializable]
        private class InventoryItemDefMetaResult
        {
            public InventoryItemMeta[] items;
        }

        [Serializable]
        private class InventoryItemMeta
        {
            public int itemdefid;
            public string modified;
        }

        [Serializable]
        private class InventoryItemDefResponse
        {
            public InventoryItemDefResult response;
        }

        [Serializable]
        private class InventoryItemDefResult
        {
            public InventoryItemDef[] itemdefs;
        }

        [Serializable]
        private class InventoryItemDef
        {
            public int itemdefid;
            public string name;
            public string description;
            public string type;
            public string price;
            public string icon_url;
            public bool tradable;
            public bool marketable;
            public bool commodity;
        }

        #endregion
    }

    /// <summary>
    /// Achievement info from Web API.
    /// </summary>
    [Serializable]
    public class WebAchievementInfo
    {
        public string ApiName;
        public string DisplayName;
        public string Description;
        public string IconUrl;
        public string IconGrayUrl;
        public bool Hidden;
        public float GlobalPercent;

        // Cached icon textures
        [NonSerialized] public Texture2D IconTexture;
        [NonSerialized] public Texture2D IconGrayTexture;
    }

    /// <summary>
    /// Stat info from Web API.
    /// </summary>
    [Serializable]
    public class WebStatInfo
    {
        public string ApiName;
        public string DisplayName;
        public int DefaultValue;
    }

    /// <summary>
    /// Inventory item info from Web API (Publisher Key required).
    /// </summary>
    [Serializable]
    public class WebInventoryItem
    {
        public int ItemDefId;
        public string Name;
        public string Description;
        public string Type;
        public string Price;
        public string IconUrl;
        public bool Tradable;
        public bool Marketable;
        public bool Commodity;
        public string Modified;

        // Cached icon texture
        [NonSerialized] public Texture2D IconTexture;
    }
}
#endif