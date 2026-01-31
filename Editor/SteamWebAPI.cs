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
                onError?.Invoke("Web API Key is not set. Get one from https://steamcommunity.com/dev/apikey");
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
}