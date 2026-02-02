#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamToolkit.Editor
{
    /// <summary>
    /// Steam Toolkit Editor Window.
    /// Central interface for managing all Steam operations.
    /// </summary>
    public class SteamToolkitWindow : EditorWindow
    {
        #region Tab Enum

        private enum Tab
        {
            AppInfo,
            Achievements,
            Stats,
            Inventory,
            Leaderboards,
            CloudSave,
            Workshop,
            BuildDeploy,
            Settings
        }

        #endregion

        #region Private Fields

        private Tab _currentTab = Tab.AppInfo;
        private Vector2 _scrollPosition;
        private SteamConfig _config;
        private SerializedObject _serializedConfig;

        private GUIContent[] _tabContents;

        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _selectedTabStyle;
        private bool _stylesInitialized;

        #endregion

        #region Window Setup

        [MenuItem("Tools/Steam Toolkit %#s", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<SteamToolkitWindow>();
            window.titleContent = new GUIContent("Steam Toolkit", EditorGUIUtility.IconContent("d_CloudConnect").image);
            window.minSize = new Vector2(550, 450);
            window.Show();
        }

        private void OnEnable()
        {
            LoadConfig();
            InitializeTabContents();
        }

        private void LoadConfig()
        {
            _config = SteamConfig.Instance;

            if (_config == null)
            {
                _config = FindOrCreateConfig();
            }

            if (_config != null)
            {
                _serializedConfig = new SerializedObject(_config);
            }
        }

        private SteamConfig FindOrCreateConfig()
        {
            var guids = AssetDatabase.FindAssets("t:SteamConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<SteamConfig>(path);
            }

            return null;
        }

        private void InitializeTabContents()
        {
            _tabContents = new GUIContent[]
            {
                new GUIContent(" App Info", EditorGUIUtility.IconContent("d_UnityEditor.GameView").image),
                new GUIContent(" Achievements", EditorGUIUtility.IconContent("d_Favorite Icon").image),
                new GUIContent(" Stats", EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image),
                new GUIContent(" Inventory", EditorGUIUtility.IconContent("d_Prefab Icon").image),
                new GUIContent(" Leaderboards", EditorGUIUtility.IconContent("d_AlphabeticalSorting").image),
                new GUIContent(" Cloud Save", EditorGUIUtility.IconContent("d_CloudConnect").image),
                new GUIContent(" Workshop", EditorGUIUtility.IconContent("d_Import").image),
                new GUIContent(" Build/Deploy", EditorGUIUtility.IconContent("d_BuildSettings.Standalone").image),
                new GUIContent(" Settings", EditorGUIUtility.IconContent("d_Settings").image)
            };
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(15, 15, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fixedHeight = 28,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                fontSize = 11
            };

            _selectedTabStyle = new GUIStyle(_tabButtonStyle)
            {
                fontStyle = FontStyle.Bold
            };

            _stylesInitialized = true;
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginHorizontal();

            DrawTabList();
            DrawSeparator();
            DrawTabContent();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1));
            rect.height = position.height;
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        private void DrawTabList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(160));
            
            EditorGUILayout.Space(10);
            GUILayout.Label("Steam Toolkit", _headerStyle);
            EditorGUILayout.Space(5);

            for (int i = 0; i < _tabContents.Length; i++)
            {
                var tab = (Tab)i;
                var isSelected = _currentTab == tab;
                var style = isSelected ? _selectedTabStyle : _tabButtonStyle;
                
                if (isSelected)
                {
                    var rect = GUILayoutUtility.GetRect(_tabContents[i], style);
                    EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.58f, 0.5f));
                    if (GUI.Button(rect, _tabContents[i], style))
                    {
                        _currentTab = tab;
                    }
                }
                else
                {
                    if (GUILayout.Button(_tabContents[i], style))
                    {
                        _currentTab = tab;
                    }
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("v1.0.0", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        private void DrawTabContent()
        {
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.Space(10);

            switch (_currentTab)
            {
                case Tab.AppInfo:
                    DrawAppInfoTab();
                    break;
                case Tab.Achievements:
                    DrawAchievementsTab();
                    break;
                case Tab.Stats:
                    DrawStatsTab();
                    break;
                case Tab.Inventory:
                    DrawInventoryTab();
                    break;
                case Tab.Leaderboards:
                    DrawLeaderboardsTab();
                    break;
                case Tab.CloudSave:
                    DrawCloudSaveTab();
                    break;
                case Tab.Workshop:
                    DrawWorkshopTab();
                    break;
                case Tab.BuildDeploy:
                    DrawBuildDeployTab();
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region App Info Tab

        private void DrawAppInfoTab()
        {
            GUILayout.Label("App Information", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_config == null)
            {
                DrawNoConfigWarning();
                return;
            }

            _serializedConfig.Update();

            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("AppId"), new GUIContent("App ID"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("GameName"), new GUIContent("Game Name"));

            EditorGUILayout.Space(10);

            GUILayout.Label("Quick Links", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Steamworks Partner", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/landing/{_config.AppId}");
            }
            if (GUILayout.Button("Store Page", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://store.steampowered.com/app/{_config.AppId}");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Achievements", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/achievements/{_config.AppId}");
            }
            if (GUILayout.Button("Stats", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/stats/{_config.AppId}");
            }
            if (GUILayout.Button("Leaderboards", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/leaderboards/{_config.AppId}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            DrawSteamStatus();

            _serializedConfig.ApplyModifiedProperties();
        }

        private void DrawSteamStatus()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("Steam Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (Application.isPlaying)
            {
                if (SteamCore.HasInstance && SteamCore.Instance.IsInitialized)
                {
                    EditorGUILayout.HelpBox(
                        $"Connected\n" +
                        $"User: {SteamCore.Instance.DisplayName}\n" +
                        $"SteamID: {SteamCore.Instance.SteamIdString}",
                        MessageType.Info
                    );

                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    
#if !DISABLESTEAMWORKS
                    if (GUILayout.Button("Open Overlay", GUILayout.Height(25)))
                    {
                        SteamFriends.ActivateGameOverlay("Friends");
                    }
                    if (GUILayout.Button("Open Store", GUILayout.Height(25)))
                    {
                        SteamFriends.ActivateGameOverlayToStore(
                            new AppId_t(_config.AppId), 
                            EOverlayToStoreFlag.k_EOverlayToStoreFlag_None
                        );
                    }
#else
                    EditorGUILayout.HelpBox("Steamworks disabled", MessageType.Warning);
#endif
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Not Connected", MessageType.Warning);
                    
                    if (GUILayout.Button("Initialize Steam", GUILayout.Height(25)))
                    {
                        SteamCore.Instance.Initialize();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see Steam status.", MessageType.Info);
                
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(30)))
                {
                    EditorApplication.isPlaying = true;
                }
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Achievements Tab

        private Vector2 _achievementsScrollPosition;
        private List<AchievementInfo> _cachedAchievements = new List<AchievementInfo>();
        private List<WebAchievementInfo> _webAchievements = new List<WebAchievementInfo>();
        private Dictionary<string, float> _globalPercentages = new Dictionary<string, float>();
        private bool _achievementsLoaded;
        private bool _webAchievementsLoading;
        private string _webAchievementsError;
        private Dictionary<string, Texture2D> _achievementIconCache = new Dictionary<string, Texture2D>();

        private void DrawAchievementsTab()
        {
            GUILayout.Label("Achievements", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Play Mode - Runtime API
            if (Application.isPlaying)
            {
                DrawPlayModeAchievements();
            }
            // Edit Mode - Web API
            else
            {
                DrawEditModeAchievements();
            }
        }

        private void DrawEditModeAchievements()
        {
            if (_config == null)
            {
                DrawNoConfigWarning();
                return;
            }

            // API Key check
            if (string.IsNullOrEmpty(_config.PublisherApiKey))
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Publisher API Key required to view achievements in Edit Mode.\n\n" +
                    "1. Go to: https://partner.steamgames.com/pub/webapi\n" +
                    "2. Create a Web API key for your app\n" +
                    "3. Paste it in Settings tab → Publisher API Key",
                    MessageType.Info
                );

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Get API Key", GUILayout.Height(25)))
                {
                    Application.OpenURL("https://partner.steamgames.com/pub/webapi");
                }
                
                if (GUILayout.Button("Go to Settings", GUILayout.Height(25)))
                {
                    _currentTab = Tab.Settings;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Or enter Play Mode to use Runtime API.", MessageType.Info);
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                EditorGUILayout.EndVertical();
                
                return;
            }

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (_webAchievementsLoading)
            {
                GUILayout.Label("Loading...", EditorStyles.boldLabel);
            }
            else
            {
                if (GUILayout.Button("Fetch from Steam", GUILayout.Width(120)))
                {
                    FetchWebAchievements();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_webAchievements.Count} Achievements", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Error message
            if (!string.IsNullOrEmpty(_webAchievementsError))
            {
                EditorGUILayout.HelpBox(_webAchievementsError, MessageType.Error);
            }

            EditorGUILayout.Space(5);

            // Achievement list
            if (_webAchievements.Count == 0 && !_webAchievementsLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Click 'Fetch from Steam' to load achievements.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _achievementsScrollPosition = EditorGUILayout.BeginScrollView(_achievementsScrollPosition);

            foreach (var achievement in _webAchievements)
            {
                DrawWebAchievementItem(achievement);
            }

            EditorGUILayout.EndScrollView();
        }

        private void FetchWebAchievements()
        {
            _webAchievementsLoading = true;
            _webAchievementsError = null;

            SteamWebAPI.GetAchievementSchema(
                _config.PublisherApiKey,
                _config.AppId,
                achievements =>
                {
                    _webAchievements = achievements;
                    _webAchievementsLoading = false;
                    
                    // Also fetch global percentages
                    FetchGlobalPercentages();
                    
                    Repaint();
                },
                error =>
                {
                    _webAchievementsError = error;
                    _webAchievementsLoading = false;
                    Repaint();
                }
            );
        }

        private void FetchGlobalPercentages()
        {
            SteamWebAPI.GetGlobalAchievementPercentages(
                _config.AppId,
                percentages =>
                {
                    _globalPercentages = percentages;
                    
                    // Update achievements with percentages
                    foreach (var ach in _webAchievements)
                    {
                        if (_globalPercentages.TryGetValue(ach.ApiName, out float percent))
                        {
                            ach.GlobalPercent = percent;
                        }
                    }
                    
                    Repaint();
                },
                error =>
                {
                    // Ignore percentage errors
                    Debug.LogWarning($"[SteamToolkit] Could not fetch global percentages: {error}");
                }
            );
        }

        private void DrawWebAchievementItem(WebAchievementInfo achievement)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Icon placeholder
            var iconRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
            
            if (achievement.IconTexture != null)
            {
                GUI.DrawTexture(iconRect, achievement.IconTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f));
                
                // Load icon async
                if (!string.IsNullOrEmpty(achievement.IconUrl) && !_achievementIconCache.ContainsKey(achievement.ApiName))
                {
                    LoadAchievementIcon(achievement);
                }
            }

            GUILayout.Space(10);

            // Info
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(achievement.DisplayName, EditorStyles.boldLabel);
            if (achievement.Hidden)
            {
                GUILayout.Label("[Hidden]", EditorStyles.miniLabel);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Label(achievement.Description, EditorStyles.wordWrappedMiniLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"API Name: {achievement.ApiName}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            if (achievement.GlobalPercent > 0)
            {
                GUILayout.Label($"Global: {achievement.GlobalPercent:F1}%", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Copy button
            if (GUILayout.Button("Copy ID", GUILayout.Width(60), GUILayout.Height(40)))
            {
                EditorGUIUtility.systemCopyBuffer = achievement.ApiName;
                Debug.Log($"[SteamToolkit] Copied: {achievement.ApiName}");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void LoadAchievementIcon(WebAchievementInfo achievement)
        {
            _achievementIconCache[achievement.ApiName] = null; // Mark as loading

            var request = UnityWebRequestTexture.GetTexture(achievement.IconUrl);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckIconRequest;

            void CheckIconRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckIconRequest;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    achievement.IconTexture = texture;
                    _achievementIconCache[achievement.ApiName] = texture;
                    Repaint();
                }

                request.Dispose();
            }
        }

        private void DrawPlayModeAchievements()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Achievements == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Achievement service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            if (!SteamCore.Instance.Achievements.StatsReceived)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Loading achievements from Steam...", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshAchievements();
            }

            if (GUILayout.Button("Unlock All", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Unlock All", 
                    "Are you sure you want to unlock all achievements?", "Yes", "Cancel"))
                {
                    UnlockAllAchievements();
                }
            }

            if (GUILayout.Button("Reset All", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Reset All", 
                    "Are you sure you want to reset all achievements?\nThis only works in test mode.", "Yes", "Cancel"))
                {
                    SteamCore.Instance.Achievements.ResetAllAchievements();
                    RefreshAchievements();
                }
            }

            GUILayout.FlexibleSpace();

            uint total = SteamCore.Instance.Achievements.GetAchievementCount();
            int unlocked = _cachedAchievements.FindAll(a => a.IsUnlocked).Count;
            GUILayout.Label($"{unlocked}/{total} Unlocked", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Load achievements if not loaded
            if (!_achievementsLoaded)
            {
                RefreshAchievements();
            }

            // Achievement list
            _achievementsScrollPosition = EditorGUILayout.BeginScrollView(_achievementsScrollPosition);

            foreach (var achievement in _cachedAchievements)
            {
                DrawAchievementItem(achievement);
            }

            EditorGUILayout.EndScrollView();
#endif
        }

#if !DISABLESTEAMWORKS
        private void RefreshAchievements()
        {
            if (SteamCore.Instance?.Achievements == null) return;

            _cachedAchievements = SteamCore.Instance.Achievements.GetAllAchievements();
            _achievementsLoaded = true;
            Repaint();
        }

        private void UnlockAllAchievements()
        {
            if (SteamCore.Instance?.Achievements == null) return;

            foreach (var achievement in _cachedAchievements)
            {
                if (!achievement.IsUnlocked)
                {
                    SteamCore.Instance.Achievements.UnlockAchievement(achievement.Id, false);
                }
            }

            SteamCore.Instance.Achievements.StoreStats();
            RefreshAchievements();
        }

        private void DrawAchievementItem(AchievementInfo achievement)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Status icon
            var statusIcon = achievement.IsUnlocked ? "✓" : "○";
            var statusColor = achievement.IsUnlocked ? Color.green : Color.gray;
            var oldColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            GUI.color = oldColor;

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Label(achievement.DisplayName, EditorStyles.boldLabel);
            GUILayout.Label(achievement.Description, EditorStyles.wordWrappedMiniLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {achievement.Id}", EditorStyles.miniLabel);
            
            if (achievement.IsUnlocked && achievement.UnlockTime > 0)
            {
                GUILayout.Label($"Unlocked: {achievement.UnlockDateTime:g}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Actions
            EditorGUILayout.BeginVertical(GUILayout.Width(70));
            
            if (achievement.IsUnlocked)
            {
                if (GUILayout.Button("Lock", GUILayout.Height(20)))
                {
                    SteamCore.Instance.Achievements.LockAchievement(achievement.Id);
                    RefreshAchievements();
                }
            }
            else
            {
                if (GUILayout.Button("Unlock", GUILayout.Height(20)))
                {
                    SteamCore.Instance.Achievements.UnlockAchievement(achievement.Id);
                    RefreshAchievements();
                }
            }
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
#endif

        #endregion

        #region Stats Tab

        private Vector2 _statsScrollPosition;
        private List<StatInfo> _cachedStats = new List<StatInfo>();
        private List<WebStatInfo> _webStats = new List<WebStatInfo>();
        private bool _statsLoaded;
        private bool _webStatsLoading;
        private string _webStatsError;
        
        // Manual stat entry
        private string _manualStatName = "";
        private string _manualStatValue = "";
        private int _manualStatType = 0; // 0 = Int, 1 = Float

        private void DrawStatsTab()
        {
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Play Mode - Runtime API
            if (Application.isPlaying)
            {
                DrawPlayModeStats();
            }
            // Edit Mode - Web API
            else
            {
                DrawEditModeStats();
            }
        }

        private void DrawEditModeStats()
        {
            if (_config == null)
            {
                DrawNoConfigWarning();
                return;
            }

            // API Key check
            if (string.IsNullOrEmpty(_config.PublisherApiKey))
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Publisher API Key required to view stats in Edit Mode.\n\n" +
                    "1. Go to: https://partner.steamgames.com/pub/webapi\n" +
                    "2. Create a Web API key for your app\n" +
                    "3. Paste it in Settings tab → Publisher API Key",
                    MessageType.Info
                );

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Get API Key", GUILayout.Height(25)))
                {
                    Application.OpenURL("https://partner.steamgames.com/pub/webapi");
                }
                
                if (GUILayout.Button("Go to Settings", GUILayout.Height(25)))
                {
                    _currentTab = Tab.Settings;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Or enter Play Mode to query stats by name.", MessageType.Info);
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                EditorGUILayout.EndVertical();
                
                return;
            }

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (_webStatsLoading)
            {
                GUILayout.Label("Loading...", EditorStyles.boldLabel);
            }
            else
            {
                if (GUILayout.Button("Fetch from Steam", GUILayout.Width(120)))
                {
                    FetchWebStats();
                }
            }

            if (GUILayout.Button("Open Steamworks", GUILayout.Width(120)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/stats/{_config.AppId}");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_webStats.Count} Stats", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Error message
            if (!string.IsNullOrEmpty(_webStatsError))
            {
                EditorGUILayout.HelpBox(_webStatsError, MessageType.Error);
            }

            EditorGUILayout.Space(5);

            // Stats list
            if (_webStats.Count == 0 && !_webStatsLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Click 'Fetch from Steam' to load stats schema.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _statsScrollPosition = EditorGUILayout.BeginScrollView(_statsScrollPosition);

            foreach (var stat in _webStats)
            {
                DrawWebStatItem(stat);
            }

            EditorGUILayout.EndScrollView();
        }

        private void FetchWebStats()
        {
            _webStatsLoading = true;
            _webStatsError = null;

            SteamWebAPI.GetStatsSchema(
                _config.PublisherApiKey,
                _config.AppId,
                stats =>
                {
                    _webStats = stats;
                    _webStatsLoading = false;
                    Repaint();
                },
                error =>
                {
                    _webStatsError = error;
                    _webStatsLoading = false;
                    Repaint();
                }
            );
        }

        private void DrawWebStatItem(WebStatInfo stat)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"API Name: {stat.ApiName}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Default: {stat.DefaultValue}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Copy button
            if (GUILayout.Button("Copy", GUILayout.Width(50), GUILayout.Height(35)))
            {
                EditorGUIUtility.systemCopyBuffer = stat.ApiName;
                Debug.Log($"[SteamToolkit] Copied: {stat.ApiName}");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawPlayModeStats()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Stats == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Stats service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            if (!SteamCore.Instance.Stats.StatsReceived)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Loading stats from Steam...", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Load stats from Web API if available
            if (_webStats.Count > 0 && _cachedStats.Count == 0)
            {
                LoadStatsFromSchema();
            }

            // Manual stat entry
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Query Stat", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _manualStatName = EditorGUILayout.TextField("Stat Name", _manualStatName);
            _manualStatType = EditorGUILayout.Popup(_manualStatType, new string[] { "Int", "Float" }, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Get Value", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(_manualStatName))
                {
                    if (_manualStatType == 0)
                    {
                        int val = SteamCore.Instance.Stats.GetStatInt(_manualStatName);
                        _manualStatValue = val.ToString();
                        AddOrUpdateCachedStat(_manualStatName, val);
                    }
                    else
                    {
                        float val = SteamCore.Instance.Stats.GetStatFloat(_manualStatName);
                        _manualStatValue = val.ToString("F2");
                        AddOrUpdateCachedStat(_manualStatName, val);
                    }
                }
            }
            
            _manualStatValue = EditorGUILayout.TextField(_manualStatValue, GUILayout.Width(100));
            
            if (GUILayout.Button("Set Value", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(_manualStatName) && !string.IsNullOrEmpty(_manualStatValue))
                {
                    if (_manualStatType == 0 && int.TryParse(_manualStatValue, out int intVal))
                    {
                        SteamCore.Instance.Stats.SetStatInt(_manualStatName, intVal, true);
                        AddOrUpdateCachedStat(_manualStatName, intVal);
                    }
                    else if (_manualStatType == 1 && float.TryParse(_manualStatValue, out float floatVal))
                    {
                        SteamCore.Instance.Stats.SetStatFloat(_manualStatName, floatVal, true);
                        AddOrUpdateCachedStat(_manualStatName, floatVal);
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Schema", GUILayout.Width(100)))
            {
                FetchWebStats();
            }

            if (GUILayout.Button("Refresh All", GUILayout.Width(100)))
            {
                RefreshAllStats();
            }

            if (GUILayout.Button("Store Stats", GUILayout.Width(100)))
            {
                SteamCore.Instance.Stats.StoreStats();
            }

            if (GUILayout.Button("Reset All", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Reset All Stats", 
                    "Are you sure you want to reset all stats?\nThis only works in test mode.", "Yes", "Cancel"))
                {
                    SteamCore.Instance.Stats.ResetAllStats(false);
                    _cachedStats.Clear();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_cachedStats.Count} Stats", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Cached stats list
            if (_cachedStats.Count == 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "No stats loaded yet.\n\n" +
                    "• Click 'Load Schema' to fetch stat definitions from Steam\n" +
                    "• Or enter a stat name above and click 'Get Value'",
                    MessageType.Info
                );
                EditorGUILayout.EndVertical();
                return;
            }

            _statsScrollPosition = EditorGUILayout.BeginScrollView(_statsScrollPosition);

            foreach (var stat in _cachedStats)
            {
                DrawStatItem(stat);
            }

            EditorGUILayout.EndScrollView();
#endif
        }

#if !DISABLESTEAMWORKS
        private void LoadStatsFromSchema()
        {
            _cachedStats.Clear();
            
            foreach (var webStat in _webStats)
            {
                // Try as int first
                if (SteamCore.Instance.Stats.TryGetStatInt(webStat.ApiName, out int intVal))
                {
                    _cachedStats.Add(new StatInfo
                    {
                        Name = webStat.ApiName,
                        DisplayName = webStat.DisplayName,
                        Type = StatInfo.StatType.Int,
                        IntValue = intVal
                    });
                }
                else if (SteamCore.Instance.Stats.TryGetStatFloat(webStat.ApiName, out float floatVal))
                {
                    _cachedStats.Add(new StatInfo
                    {
                        Name = webStat.ApiName,
                        DisplayName = webStat.DisplayName,
                        Type = StatInfo.StatType.Float,
                        FloatValue = floatVal
                    });
                }
            }
            
            Repaint();
        }

        private void AddOrUpdateCachedStat(string name, int value)
        {
            var existing = _cachedStats.Find(s => s.Name == name);
            if (existing != null)
            {
                existing.IntValue = value;
                existing.Type = StatInfo.StatType.Int;
            }
            else
            {
                _cachedStats.Add(new StatInfo
                {
                    Name = name,
                    DisplayName = name,
                    Type = StatInfo.StatType.Int,
                    IntValue = value
                });
            }
            Repaint();
        }

        private void AddOrUpdateCachedStat(string name, float value)
        {
            var existing = _cachedStats.Find(s => s.Name == name);
            if (existing != null)
            {
                existing.FloatValue = value;
                existing.Type = StatInfo.StatType.Float;
            }
            else
            {
                _cachedStats.Add(new StatInfo
                {
                    Name = name,
                    DisplayName = name,
                    Type = StatInfo.StatType.Float,
                    FloatValue = value
                });
            }
            Repaint();
        }

        private void RefreshAllStats()
        {
            // If we have schema, reload from it
            if (_webStats.Count > 0)
            {
                LoadStatsFromSchema();
                return;
            }
            
            // Otherwise refresh existing
            foreach (var stat in _cachedStats)
            {
                if (stat.Type == StatInfo.StatType.Int)
                {
                    stat.IntValue = SteamCore.Instance.Stats.GetStatInt(stat.Name);
                }
                else
                {
                    stat.FloatValue = SteamCore.Instance.Stats.GetStatFloat(stat.Name);
                }
            }
            Repaint();
        }

        private void DrawStatItem(StatInfo stat)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Type indicator
            var typeLabel = stat.Type == StatInfo.StatType.Int ? "[INT]" : "[FLOAT]";
            GUILayout.Label(typeLabel, EditorStyles.miniLabel, GUILayout.Width(45));

            // Name
            EditorGUILayout.BeginVertical();
            GUILayout.Label(stat.DisplayName, EditorStyles.boldLabel);
            if (stat.Name != stat.DisplayName)
            {
                GUILayout.Label($"API: {stat.Name}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            // Value field
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            
            if (stat.Type == StatInfo.StatType.Int)
            {
                int newValue = EditorGUILayout.IntField(stat.IntValue);
                if (newValue != stat.IntValue)
                {
                    stat.IntValue = newValue;
                    SteamCore.Instance.Stats.SetStatInt(stat.Name, newValue);
                }
            }
            else
            {
                float newValue = EditorGUILayout.FloatField(stat.FloatValue);
                if (!Mathf.Approximately(newValue, stat.FloatValue))
                {
                    stat.FloatValue = newValue;
                    SteamCore.Instance.Stats.SetStatFloat(stat.Name, newValue);
                }
            }
            
            EditorGUILayout.EndVertical();

            // Actions
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(20)))
            {
                _cachedStats.Remove(stat);
                Repaint();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
#endif

        #endregion

        #region Other Tabs

        private void DrawInventoryTab()
        {
            GUILayout.Label("Inventory", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Play Mode - Runtime API
            if (Application.isPlaying)
            {
                DrawPlayModeInventory();
            }
            // Edit Mode - Web API
            else
            {
                DrawEditModeInventory();
            }
        }

        private void DrawEditModeInventory()
        {
            if (_config == null)
            {
                DrawNoConfigWarning();
                return;
            }

            // API Key check
            if (string.IsNullOrEmpty(_config.PublisherApiKey))
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Publisher API Key required to view inventory items in Edit Mode.\n\n" +
                    "1. Go to: https://partner.steamgames.com/pub/webapi\n" +
                    "2. Create a Web API key for your app\n" +
                    "3. Paste it in Settings tab → Publisher API Key",
                    MessageType.Info
                );

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Get API Key", GUILayout.Height(25)))
                {
                    Application.OpenURL("https://partner.steamgames.com/pub/webapi");
                }
                
                if (GUILayout.Button("Go to Settings", GUILayout.Height(25)))
                {
                    _currentTab = Tab.Settings;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(10);
                
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Or enter Play Mode to use Runtime API.", MessageType.Info);
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                EditorGUILayout.EndVertical();
                
                return;
            }

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (_webInventoryLoading)
            {
                GUILayout.Label("Loading...", EditorStyles.boldLabel);
            }
            else
            {
                if (GUILayout.Button("Fetch from Steam", GUILayout.Width(120)))
                {
                    FetchWebInventory();
                }
            }

            if (GUILayout.Button("Open Steamworks", GUILayout.Width(120)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/inventoryservice/{_config.AppId}");
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_webInventoryItems.Count} Items", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Error message
            if (!string.IsNullOrEmpty(_webInventoryError))
            {
                EditorGUILayout.HelpBox(_webInventoryError, MessageType.Error);
            }

            EditorGUILayout.Space(5);

            // Items list
            if (_webInventoryItems.Count == 0 && !_webInventoryLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Click 'Fetch from Steam' to load inventory item definitions.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _webInventoryScrollPosition = EditorGUILayout.BeginScrollView(_webInventoryScrollPosition);

            foreach (var item in _webInventoryItems)
            {
                DrawWebInventoryItem(item);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<WebInventoryItem> _webInventoryItems = new List<WebInventoryItem>();
        private bool _webInventoryLoading;
        private string _webInventoryError;
        private Vector2 _webInventoryScrollPosition;
        private Dictionary<int, Texture2D> _inventoryIconCache = new Dictionary<int, Texture2D>();

        private void FetchWebInventory()
        {
            _webInventoryLoading = true;
            _webInventoryError = null;

            SteamWebAPI.GetInventoryItemDefinitions(
                _config.PublisherApiKey,
                _config.AppId,
                items =>
                {
                    _webInventoryItems = items;
                    _webInventoryLoading = false;
                    Repaint();
                },
                error =>
                {
                    _webInventoryError = error;
                    _webInventoryLoading = false;
                    Repaint();
                }
            );
        }

        private void DrawWebInventoryItem(WebInventoryItem item)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Icon placeholder
            var iconRect = GUILayoutUtility.GetRect(48, 48, GUILayout.Width(48), GUILayout.Height(48));
            
            if (item.IconTexture != null)
            {
                GUI.DrawTexture(iconRect, item.IconTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f));
                
                // Load icon async
                if (!string.IsNullOrEmpty(item.IconUrl) && !_inventoryIconCache.ContainsKey(item.ItemDefId))
                {
                    LoadInventoryIcon(item);
                }
            }

            GUILayout.Space(10);

            // Info
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(string.IsNullOrEmpty(item.Name) ? $"Item #{item.ItemDefId}" : item.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(item.Description))
            {
                GUILayout.Label(item.Description, EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {item.ItemDefId}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(item.Type))
                GUILayout.Label($"Type: {item.Type}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(item.Price))
                GUILayout.Label($"Price: {item.Price}", EditorStyles.miniLabel);
                
            GUILayout.FlexibleSpace();
            
            if (item.Tradable) GUILayout.Label("[Tradable]", EditorStyles.miniLabel);
            if (item.Marketable) GUILayout.Label("[Marketable]", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Copy button
            if (GUILayout.Button("Copy ID", GUILayout.Width(60), GUILayout.Height(40)))
            {
                EditorGUIUtility.systemCopyBuffer = item.ItemDefId.ToString();
                Debug.Log($"[SteamToolkit] Copied: {item.ItemDefId}");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void LoadInventoryIcon(WebInventoryItem item)
        {
            _inventoryIconCache[item.ItemDefId] = null; // Mark as loading

            var request = UnityWebRequestTexture.GetTexture(item.IconUrl);
            var operation = request.SendWebRequest();

            EditorApplication.update += CheckIconRequest;

            void CheckIconRequest()
            {
                if (!operation.isDone) return;

                EditorApplication.update -= CheckIconRequest;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    item.IconTexture = texture;
                    _inventoryIconCache[item.ItemDefId] = texture;
                    Repaint();
                }

                request.Dispose();
            }
        }

        #region Inventory Play Mode

        private Vector2 _inventoryScrollPosition;
        private Vector2 _itemDefsScrollPosition;
        private List<InventoryItem> _inventoryItems = new List<InventoryItem>();
        private List<ItemDefinitionInfo> _itemDefinitions = new List<ItemDefinitionInfo>();
        private bool _inventoryLoading;
        private string _inventoryStatus = "";
        private int _inventoryViewMode = 0; // 0 = My Items, 1 = Item Definitions

        private void DrawPlayModeInventory()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Inventory == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Inventory service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // View mode tabs
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(_inventoryViewMode == 0, "My Items", EditorStyles.toolbarButton))
                _inventoryViewMode = 0;
            if (GUILayout.Toggle(_inventoryViewMode == 1, "Item Definitions", EditorStyles.toolbarButton))
                _inventoryViewMode = 1;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (_inventoryViewMode == 0)
            {
                DrawMyItems();
            }
            else
            {
                DrawItemDefinitions();
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private void DrawMyItems()
        {
            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_inventoryLoading;
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshInventory();
            }

            if (GUILayout.Button("Grant Promos", GUILayout.Width(100)))
            {
                GrantPromoItems();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_inventoryItems.Count} Items", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Status
            if (!string.IsNullOrEmpty(_inventoryStatus))
            {
                EditorGUILayout.HelpBox(_inventoryStatus, _inventoryLoading ? MessageType.Info : MessageType.None);
            }

            EditorGUILayout.Space(5);

            // Items list
            if (_inventoryItems.Count == 0 && !_inventoryLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("No items in inventory.\nClick 'Refresh' to load or 'Grant Promos' to get promotional items.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _inventoryScrollPosition = EditorGUILayout.BeginScrollView(_inventoryScrollPosition);

            foreach (var item in _inventoryItems)
            {
                DrawInventoryItem(item);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawItemDefinitions()
        {
            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_inventoryLoading;
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshItemDefinitions();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            
            if (SteamCore.Instance.Inventory.DefinitionsLoaded)
            {
                GUILayout.Label($"{_itemDefinitions.Count} Definitions", EditorStyles.boldLabel);
            }
            else
            {
                GUILayout.Label("Loading definitions...", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Definitions list
            if (!SteamCore.Instance.Inventory.DefinitionsLoaded)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Waiting for item definitions to load from Steam...", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            if (_itemDefinitions.Count == 0)
            {
                RefreshItemDefinitions();
            }

            if (_itemDefinitions.Count == 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("No item definitions found.\nMake sure items are configured in Steamworks Partner site.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _itemDefsScrollPosition = EditorGUILayout.BeginScrollView(_itemDefsScrollPosition);

            foreach (var itemDef in _itemDefinitions)
            {
                DrawItemDefinition(itemDef);
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshInventory()
        {
            _inventoryLoading = true;
            _inventoryStatus = "Loading inventory...";
            Repaint();

            SteamCore.Instance.Inventory.GetAllItems(items =>
            {
                _inventoryItems = items;
                _inventoryLoading = false;
                _inventoryStatus = $"Loaded {items.Count} items.";
                Repaint();
            });

            // Also subscribe to updates
            SteamCore.Instance.Inventory.OnInventoryUpdated -= OnInventoryUpdated;
            SteamCore.Instance.Inventory.OnInventoryUpdated += OnInventoryUpdated;
        }

        private void OnInventoryUpdated(List<InventoryItem> items)
        {
            _inventoryItems = items;
            _inventoryLoading = false;
            _inventoryStatus = $"Inventory updated: {items.Count} items.";
            Repaint();
        }

        private void GrantPromoItems()
        {
            _inventoryLoading = true;
            _inventoryStatus = "Granting promo items...";
            Repaint();

            SteamCore.Instance.Inventory.GrantPromoItems(items =>
            {
                _inventoryLoading = false;
                if (items.Count > 0)
                {
                    _inventoryStatus = $"Granted {items.Count} promo items!";
                    RefreshInventory();
                }
                else
                {
                    _inventoryStatus = "No promo items to grant (already owned or none configured).";
                }
                Repaint();
            });
        }

        private void RefreshItemDefinitions()
        {
            _itemDefinitions = SteamCore.Instance.Inventory.GetAllItemDefinitions();
            Repaint();
        }

        private void DrawInventoryItem(InventoryItem item)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Quantity
            GUILayout.Label($"x{item.Quantity}", EditorStyles.boldLabel, GUILayout.Width(40));

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Label(string.IsNullOrEmpty(item.Name) ? $"Item #{item.ItemDefId}" : item.Name, EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(item.Description))
            {
                GUILayout.Label(item.Description, EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {item.ItemId}", EditorStyles.miniLabel);
            GUILayout.Label($"Def: {item.ItemDefId}", EditorStyles.miniLabel);
            
            if (item.IsNoTrade) GUILayout.Label("[No Trade]", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Consume button
            if (item.Quantity > 0)
            {
                if (GUILayout.Button("Consume", GUILayout.Width(70), GUILayout.Height(35)))
                {
                    SteamCore.Instance.Inventory.ConsumeItem(item.ItemId, 1, success =>
                    {
                        if (success)
                        {
                            _inventoryStatus = $"Consumed 1x {item.Name}";
                            RefreshInventory();
                        }
                        else
                        {
                            _inventoryStatus = "Failed to consume item.";
                        }
                        Repaint();
                    });
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawItemDefinition(ItemDefinitionInfo itemDef)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Label(string.IsNullOrEmpty(itemDef.Name) ? $"Item #{itemDef.ItemDefId}" : itemDef.Name, EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(itemDef.Description))
            {
                GUILayout.Label(itemDef.Description, EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {itemDef.ItemDefId}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(itemDef.Type))
                GUILayout.Label($"Type: {itemDef.Type}", EditorStyles.miniLabel);
            
            if (!string.IsNullOrEmpty(itemDef.Price))
                GUILayout.Label($"Price: {itemDef.Price}", EditorStyles.miniLabel);
            
            if (itemDef.Tradable) GUILayout.Label("[Tradable]", EditorStyles.miniLabel);
            if (itemDef.Marketable) GUILayout.Label("[Marketable]", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Grant button
            if (GUILayout.Button("Grant", GUILayout.Width(60), GUILayout.Height(35)))
            {
                SteamCore.Instance.Inventory.GrantPromoItem(itemDef.ItemDefId, success =>
                {
                    _inventoryStatus = success 
                        ? $"Granted {itemDef.Name}!" 
                        : "Failed to grant (not a promo item or already owned).";
                    Repaint();
                });
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
#endif

        #endregion

        private void DrawLeaderboardsTab()
        {
            GUILayout.Label("Leaderboards", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to manage leaderboards.\n\n" +
                    "Features:\n" +
                    "• Find/Create leaderboards\n" +
                    "• Upload scores\n" +
                    "• Download top scores, friend scores\n" +
                    "• View scores around current user",
                    MessageType.Info
                );
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                if (GUILayout.Button("Open Steamworks", GUILayout.Height(25)))
                {
                    if (_config != null)
                    {
                        Application.OpenURL($"https://partner.steamgames.com/apps/leaderboards/{_config.AppId}");
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                return;
            }

            DrawPlayModeLeaderboards();
        }

        #region Leaderboards Play Mode

        private string _leaderboardName = "Spacewar_GlobalScores";
        private int _uploadScore = 1000;
        private int _downloadCount = 10;
        private List<LeaderboardEntry> _leaderboardEntries = new List<LeaderboardEntry>();
        private Vector2 _leaderboardScrollPosition;
        private bool _leaderboardLoading;
        private string _leaderboardStatus = "";

        private void DrawPlayModeLeaderboards()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Leaderboards == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Leaderboard service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Leaderboard Name
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Leaderboard", EditorStyles.boldLabel);
            _leaderboardName = EditorGUILayout.TextField("Name", _leaderboardName);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Upload Score
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Upload Score", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _uploadScore = EditorGUILayout.IntField("Score", _uploadScore);
            
            GUI.enabled = !_leaderboardLoading;
            if (GUILayout.Button("Upload (Best)", GUILayout.Width(100)))
            {
                UploadScore(ScoreUploadMethod.KeepBest);
            }
            if (GUILayout.Button("Upload (Force)", GUILayout.Width(100)))
            {
                UploadScore(ScoreUploadMethod.ForceUpdate);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Download Scores
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Download Scores", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _downloadCount = EditorGUILayout.IntField("Count", _downloadCount);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !_leaderboardLoading;
            if (GUILayout.Button("Top Scores"))
            {
                DownloadTopScores();
            }
            if (GUILayout.Button("Around Me"))
            {
                DownloadScoresAroundUser();
            }
            if (GUILayout.Button("Friends"))
            {
                DownloadFriendScores();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Status
            if (!string.IsNullOrEmpty(_leaderboardStatus))
            {
                EditorGUILayout.HelpBox(_leaderboardStatus, _leaderboardLoading ? MessageType.Info : MessageType.None);
            }

            // Results
            if (_leaderboardEntries.Count > 0)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label($"Results ({_leaderboardEntries.Count} entries)", EditorStyles.boldLabel);
                
                _leaderboardScrollPosition = EditorGUILayout.BeginScrollView(_leaderboardScrollPosition, GUILayout.MaxHeight(300));

                // Header
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Rank", EditorStyles.boldLabel, GUILayout.Width(50));
                GUILayout.Label("Player", EditorStyles.boldLabel);
                GUILayout.Label("Score", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                // Entries
                foreach (var entry in _leaderboardEntries)
                {
                    DrawLeaderboardEntry(entry);
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private void UploadScore(ScoreUploadMethod method)
        {
            _leaderboardLoading = true;
            _leaderboardStatus = "Uploading score...";
            Repaint();

            SteamCore.Instance.Leaderboards.UploadScore(_leaderboardName, _uploadScore, method, (score, changed) =>
            {
                _leaderboardLoading = false;
                _leaderboardStatus = changed 
                    ? $"Score {score} uploaded successfully!" 
                    : $"Score {score} not changed (existing score is better).";
                Repaint();
            },
            error =>
            {
                _leaderboardLoading = false;
                _leaderboardStatus = $"Error: {error}";
                Repaint();
            });
        }

        private void DownloadTopScores()
        {
            _leaderboardLoading = true;
            _leaderboardStatus = "Downloading top scores...";
            _leaderboardEntries.Clear();
            Repaint();

            SteamCore.Instance.Leaderboards.DownloadTopScores(_leaderboardName, _downloadCount, entries =>
            {
                _leaderboardLoading = false;
                _leaderboardEntries = entries;
                _leaderboardStatus = $"Downloaded {entries.Count} entries.";
                Repaint();
            },
            error =>
            {
                _leaderboardLoading = false;
                _leaderboardStatus = $"Error: {error}";
                Repaint();
            });
        }

        private void DownloadScoresAroundUser()
        {
            _leaderboardLoading = true;
            _leaderboardStatus = "Downloading scores around user...";
            _leaderboardEntries.Clear();
            Repaint();

            int range = _downloadCount / 2;
            SteamCore.Instance.Leaderboards.DownloadScoresAroundUser(_leaderboardName, range, entries =>
            {
                _leaderboardLoading = false;
                _leaderboardEntries = entries;
                _leaderboardStatus = $"Downloaded {entries.Count} entries.";
                Repaint();
            },
            error =>
            {
                _leaderboardLoading = false;
                _leaderboardStatus = $"Error: {error}";
                Repaint();
            });
        }

        private void DownloadFriendScores()
        {
            _leaderboardLoading = true;
            _leaderboardStatus = "Downloading friend scores...";
            _leaderboardEntries.Clear();
            Repaint();

            SteamCore.Instance.Leaderboards.DownloadFriendsScores(_leaderboardName, entries =>
            {
                _leaderboardLoading = false;
                _leaderboardEntries = entries;
                _leaderboardStatus = $"Downloaded {entries.Count} friend entries.";
                Repaint();
            },
            error =>
            {
                _leaderboardLoading = false;
                _leaderboardStatus = $"Error: {error}";
                Repaint();
            });
        }

        private void DrawLeaderboardEntry(LeaderboardEntry entry)
        {
            bool isCurrentUser = entry.SteamId == SteamUser.GetSteamID();
            
            if (isCurrentUser)
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
                EditorGUILayout.BeginHorizontal(_boxStyle);
                GUI.backgroundColor = oldColor;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
            }

            GUILayout.Label($"#{entry.Rank}", GUILayout.Width(50));
            GUILayout.Label(entry.PlayerName + (isCurrentUser ? " (You)" : ""));
            GUILayout.Label(entry.Score.ToString("N0"), GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();
        }
#endif

        #endregion

        private void DrawCloudSaveTab()
        {
            GUILayout.Label("Cloud Save", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to manage cloud saves.\n\n" +
                    "Features:\n" +
                    "• View all cloud files\n" +
                    "• Read/Write files\n" +
                    "• Delete files\n" +
                    "• View quota usage",
                    MessageType.Info
                );
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                if (GUILayout.Button("Open Steamworks", GUILayout.Height(25)))
                {
                    if (_config != null)
                    {
                        Application.OpenURL($"https://partner.steamgames.com/apps/cloud/{_config.AppId}");
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                return;
            }

            DrawPlayModeCloud();
        }

        #region Cloud Play Mode

        private List<CloudFileInfo> _cloudFiles = new List<CloudFileInfo>();
        private CloudQuotaInfo _cloudQuota;
        private Vector2 _cloudScrollPosition;
        private bool _cloudLoading;
        private string _cloudStatus = "";

        // Write file
        private string _cloudWriteFileName = "test.txt";
        private string _cloudWriteContent = "Hello, Steam Cloud!";
        private bool _cloudShowWritePanel;

        // Read file
        private string _cloudReadContent = "";
        private string _cloudSelectedFile = "";

        private void DrawPlayModeCloud()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Cloud == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Cloud service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Cloud status
            if (!SteamCore.Instance.Cloud.IsCloudEnabled)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam Cloud is disabled for this user or app.\n\nCheck Steam settings or app configuration.", MessageType.Warning);
                EditorGUILayout.EndVertical();
            }

            // Quota info
            DrawCloudQuota();

            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshCloudFiles();
            }

            _cloudShowWritePanel = GUILayout.Toggle(_cloudShowWritePanel, "Write File", EditorStyles.toolbarButton, GUILayout.Width(80));

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_cloudFiles.Count} Files", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Write panel
            if (_cloudShowWritePanel)
            {
                DrawCloudWritePanel();
            }

            // Status
            if (!string.IsNullOrEmpty(_cloudStatus))
            {
                EditorGUILayout.HelpBox(_cloudStatus, MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // File list
            if (_cloudFiles.Count == 0 && !_cloudLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("No files in cloud storage.\nClick 'Refresh' to load or use 'Write File' to create one.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _cloudScrollPosition = EditorGUILayout.BeginScrollView(_cloudScrollPosition);

            foreach (var file in _cloudFiles)
            {
                DrawCloudFile(file);
            }

            EditorGUILayout.EndScrollView();

            // Read content panel
            if (!string.IsNullOrEmpty(_cloudSelectedFile) && !string.IsNullOrEmpty(_cloudReadContent))
            {
                DrawCloudReadPanel();
            }
#endif
        }

#if !DISABLESTEAMWORKS
        private void DrawCloudQuota()
        {
            if (_cloudQuota == null)
            {
                _cloudQuota = SteamCore.Instance.Cloud.GetQuota();
            }

            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Cloud Storage", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_cloudQuota.UsedFormatted} / {_cloudQuota.TotalFormatted}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // Progress bar
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(8));
            EditorGUI.ProgressBar(rect, _cloudQuota.UsedPercent / 100f, "");

            EditorGUILayout.EndVertical();
        }

        private void DrawCloudWritePanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Write File", EditorStyles.boldLabel);

            _cloudWriteFileName = EditorGUILayout.TextField("File Name", _cloudWriteFileName);
            
            GUILayout.Label("Content:");
            _cloudWriteContent = EditorGUILayout.TextArea(_cloudWriteContent, GUILayout.Height(60));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Write", GUILayout.Width(80)))
            {
                if (SteamCore.Instance.Cloud.WriteString(_cloudWriteFileName, _cloudWriteContent))
                {
                    _cloudStatus = $"File written: {_cloudWriteFileName}";
                    RefreshCloudFiles();
                }
                else
                {
                    _cloudStatus = "Failed to write file.";
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCloudReadPanel()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Content: {_cloudSelectedFile}", EditorStyles.boldLabel);
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _cloudSelectedFile = "";
                _cloudReadContent = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.TextArea(_cloudReadContent, GUILayout.Height(100));

            EditorGUILayout.EndVertical();
        }

        private void RefreshCloudFiles()
        {
            _cloudFiles = SteamCore.Instance.Cloud.GetAllFiles();
            _cloudQuota = SteamCore.Instance.Cloud.GetQuota();
            Repaint();
        }

        private void DrawCloudFile(CloudFileInfo file)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // File icon
            GUILayout.Label(EditorGUIUtility.IconContent("d_TextAsset Icon"), GUILayout.Width(20), GUILayout.Height(20));

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Label(file.FileName, EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Size: {file.SizeFormatted}", EditorStyles.miniLabel);
            GUILayout.Label($"Modified: {file.LastModified:g}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Buttons
            if (GUILayout.Button("Read", GUILayout.Width(50), GUILayout.Height(30)))
            {
                _cloudSelectedFile = file.FileName;
                _cloudReadContent = SteamCore.Instance.Cloud.ReadString(file.FileName) ?? "(binary or empty)";
                _cloudStatus = $"Read: {file.FileName}";
            }

            if (GUILayout.Button("Delete", GUILayout.Width(50), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete File", 
                    $"Are you sure you want to delete '{file.FileName}'?\n\nThis cannot be undone.", 
                    "Delete", "Cancel"))
                {
                    if (SteamCore.Instance.Cloud.DeleteFile(file.FileName))
                    {
                        _cloudStatus = $"Deleted: {file.FileName}";
                        if (_cloudSelectedFile == file.FileName)
                        {
                            _cloudSelectedFile = "";
                            _cloudReadContent = "";
                        }
                        RefreshCloudFiles();
                    }
                    else
                    {
                        _cloudStatus = "Failed to delete file.";
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
#endif

        #endregion

        private void DrawWorkshopTab()
        {
            GUILayout.Label("Workshop", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to manage Workshop items.\n\n" +
                    "Features:\n" +
                    "• View subscribed items\n" +
                    "• View your published items\n" +
                    "• Create & update items\n" +
                    "• Subscribe/Unsubscribe",
                    MessageType.Info
                );
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enter Play Mode", GUILayout.Height(25)))
                {
                    EditorApplication.isPlaying = true;
                }
                if (GUILayout.Button("Open Workshop", GUILayout.Height(25)))
                {
                    if (_config != null)
                    {
                        Application.OpenURL($"https://steamcommunity.com/app/{_config.AppId}/workshop/");
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                return;
            }

            DrawPlayModeWorkshop();
        }

        #region Workshop Play Mode

        private List<WorkshopItem> _workshopItems = new List<WorkshopItem>();
        private Vector2 _workshopScrollPosition;
        private bool _workshopLoading;
        private string _workshopStatus = "";
        private int _workshopViewMode = 0; // 0 = Subscribed, 1 = Published, 2 = Popular

        // Create item
        private bool _workshopShowCreatePanel;
        private string _workshopNewTitle = "My Workshop Item";
        private string _workshopNewDescription = "Description of my item...";
        private string _workshopNewContentPath = "";
        private string _workshopNewPreviewPath = "";
        private int _workshopNewVisibility = 0; // 0=Public, 1=Friends, 2=Private

        private void DrawPlayModeWorkshop()
        {
            if (!SteamCore.HasInstance || !SteamCore.Instance.IsInitialized)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Steam not initialized.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

#if !DISABLESTEAMWORKS
            if (SteamCore.Instance.Workshop == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox("Workshop service not enabled. Enable it in Settings.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // View mode tabs
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(_workshopViewMode == 0, "Subscribed", EditorStyles.toolbarButton))
            {
                if (_workshopViewMode != 0) { _workshopViewMode = 0; _workshopItems.Clear(); }
            }
            if (GUILayout.Toggle(_workshopViewMode == 1, "My Items", EditorStyles.toolbarButton))
            {
                if (_workshopViewMode != 1) { _workshopViewMode = 1; _workshopItems.Clear(); }
            }
            if (GUILayout.Toggle(_workshopViewMode == 2, "Popular", EditorStyles.toolbarButton))
            {
                if (_workshopViewMode != 2) { _workshopViewMode = 2; _workshopItems.Clear(); }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_workshopLoading;
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                RefreshWorkshopItems();
            }

            if (_workshopViewMode == 1) // My Items
            {
                _workshopShowCreatePanel = GUILayout.Toggle(_workshopShowCreatePanel, "Create New", EditorStyles.toolbarButton, GUILayout.Width(80));
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            var subscribedCount = SteamCore.Instance.Workshop.GetSubscribedItemCount();
            GUILayout.Label($"Subscribed: {subscribedCount}", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Create panel
            if (_workshopShowCreatePanel && _workshopViewMode == 1)
            {
                DrawWorkshopCreatePanel();
            }

            // Status
            if (!string.IsNullOrEmpty(_workshopStatus))
            {
                EditorGUILayout.HelpBox(_workshopStatus, _workshopLoading ? MessageType.Info : MessageType.None);
            }

            EditorGUILayout.Space(5);

            // Items list
            if (_workshopItems.Count == 0 && !_workshopLoading)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                string hint = _workshopViewMode switch
                {
                    0 => "No subscribed items.\nClick 'Refresh' to load.",
                    1 => "No published items.\nCreate a new item to get started.",
                    2 => "No items found.\nClick 'Refresh' to load popular items.",
                    _ => "Click 'Refresh' to load items."
                };
                EditorGUILayout.HelpBox(hint, MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _workshopScrollPosition = EditorGUILayout.BeginScrollView(_workshopScrollPosition);

            foreach (var item in _workshopItems)
            {
                DrawWorkshopItem(item);
            }

            EditorGUILayout.EndScrollView();
#endif
        }

#if !DISABLESTEAMWORKS
        private void DrawWorkshopCreatePanel()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Create Workshop Item", EditorStyles.boldLabel);

            _workshopNewTitle = EditorGUILayout.TextField("Title", _workshopNewTitle);
            
            GUILayout.Label("Description:");
            _workshopNewDescription = EditorGUILayout.TextArea(_workshopNewDescription, GUILayout.Height(60));

            EditorGUILayout.BeginHorizontal();
            _workshopNewContentPath = EditorGUILayout.TextField("Content Folder", _workshopNewContentPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Content Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path)) _workshopNewContentPath = path;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _workshopNewPreviewPath = EditorGUILayout.TextField("Preview Image", _workshopNewPreviewPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFilePanel("Select Preview Image", Application.dataPath, "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path)) _workshopNewPreviewPath = path;
            }
            EditorGUILayout.EndHorizontal();

            _workshopNewVisibility = EditorGUILayout.Popup("Visibility", _workshopNewVisibility, new[] { "Public", "Friends Only", "Private" });

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(_workshopNewTitle) && !string.IsNullOrEmpty(_workshopNewContentPath);
            if (GUILayout.Button("Create & Upload", GUILayout.Height(25)))
            {
                CreateWorkshopItem();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CreateWorkshopItem()
        {
            _workshopLoading = true;
            _workshopStatus = "Creating item...";

            SteamCore.Instance.Workshop.CreateItem(itemId =>
            {
                _workshopStatus = $"Item created: {itemId}. Uploading content...";
                Repaint();

                var visibility = (WorkshopVisibility)_workshopNewVisibility;

                SteamCore.Instance.Workshop.BeginItemUpdate(itemId)
                    .SetTitle(_workshopNewTitle)
                    .SetDescription(_workshopNewDescription)
                    .SetContent(_workshopNewContentPath)
                    .SetVisibility(visibility)
                    .SetPreviewImage(string.IsNullOrEmpty(_workshopNewPreviewPath) ? null : _workshopNewPreviewPath)
                    .Submit("Initial upload", (id, needsAgreement) =>
                    {
                        _workshopLoading = false;
                        
                        if (needsAgreement)
                        {
                            _workshopStatus = $"Item uploaded! Please accept the Workshop agreement.";
                            Application.OpenURL($"https://steamcommunity.com/sharedfiles/workshoplegalagreement");
                        }
                        else
                        {
                            _workshopStatus = $"Item uploaded successfully! ID: {id}";
                        }

                        _workshopShowCreatePanel = false;
                        RefreshWorkshopItems();
                    });
            });
        }

        private void RefreshWorkshopItems()
        {
            _workshopLoading = true;
            _workshopStatus = "Loading...";
            _workshopItems.Clear();
            Repaint();

            Action<List<WorkshopItem>> callback = items =>
            {
                _workshopItems = items;
                _workshopLoading = false;
                _workshopStatus = $"Loaded {items.Count} items.";
                Repaint();
            };

            switch (_workshopViewMode)
            {
                case 0:
                    SteamCore.Instance.Workshop.QuerySubscribedItems(callback);
                    break;
                case 1:
                    SteamCore.Instance.Workshop.QueryPublishedItems(callback);
                    break;
                case 2:
                    SteamCore.Instance.Workshop.QueryPopularItems(callback);
                    break;
            }
        }

        private void DrawWorkshopItem(WorkshopItem item)
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Preview placeholder
            var previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));

            GUILayout.Space(10);

            // Info
            EditorGUILayout.BeginVertical();
            
            GUILayout.Label(item.Title, EditorStyles.boldLabel);
            
            if (!string.IsNullOrEmpty(item.Description))
            {
                var shortDesc = item.Description.Length > 100 
                    ? item.Description.Substring(0, 100) + "..." 
                    : item.Description;
                GUILayout.Label(shortDesc, EditorStyles.wordWrappedMiniLabel);
            }
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {item.ItemId}", EditorStyles.miniLabel);
            GUILayout.Label($"Size: {item.FileSizeFormatted}", EditorStyles.miniLabel);
            GUILayout.Label($"👍 {item.VotesUp} 👎 {item.VotesDown}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Updated: {item.UpdatedTime:g}", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(item.Tags))
            {
                GUILayout.Label($"Tags: {item.Tags}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            // Buttons
            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            var state = SteamCore.Instance.Workshop.GetItemState(item.ItemId);

            if (state.IsSubscribed)
            {
                if (GUILayout.Button("Unsub", GUILayout.Height(25)))
                {
                    SteamCore.Instance.Workshop.Unsubscribe(item.ItemId, success =>
                    {
                        _workshopStatus = success ? "Unsubscribed!" : "Failed to unsubscribe.";
                        RefreshWorkshopItems();
                    });
                }
            }
            else
            {
                if (GUILayout.Button("Subscribe", GUILayout.Height(25)))
                {
                    SteamCore.Instance.Workshop.Subscribe(item.ItemId, success =>
                    {
                        _workshopStatus = success ? "Subscribed!" : "Failed to subscribe.";
                        RefreshWorkshopItems();
                    });
                }
            }

            if (GUILayout.Button("Open", GUILayout.Height(25)))
            {
                Application.OpenURL($"https://steamcommunity.com/sharedfiles/filedetails/?id={item.ItemId}");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
#endif

        #endregion

        private void DrawBuildDeployTab()
        {
            GUILayout.Label("Build & Deploy", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Find or create build config
            if (_buildConfig == null)
            {
                _buildConfig = Resources.Load<SteamBuildConfig>("SteamBuildConfig");
            }

            if (_buildConfig == null)
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "No Build Config found!\n\n" +
                    "Create a SteamBuildConfig asset to configure SteamPipe uploads.",
                    MessageType.Warning
                );

                if (GUILayout.Button("Create Build Config", GUILayout.Height(30)))
                {
                    CreateBuildConfig();
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.HelpBox(
                    "Build & Deploy features:\n" +
                    "• Generate VDF scripts automatically\n" +
                    "• Copy Unity builds to ContentBuilder\n" +
                    "• Run SteamCMD to upload\n" +
                    "• Multi-depot support\n" +
                    "• Branch selection",
                    MessageType.Info
                );

                EditorGUILayout.EndVertical();
                return;
            }

            // Build Config Editor
            if (_serializedBuildConfig == null || _serializedBuildConfig.targetObject != _buildConfig)
            {
                _serializedBuildConfig = new SerializedObject(_buildConfig);
            }

            _serializedBuildConfig.Update();

            // SteamCMD Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("SteamCMD", EditorStyles.boldLabel);

            // Check if installed
            string steamCmdPath = _serializedBuildConfig.FindProperty("SteamCmdPath").stringValue;
            bool isInstalled = SteamPipeBuilder.IsSteamCmdInstalled(steamCmdPath);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("SteamCmdPath"), new GUIContent("SteamCMD Path"));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
#if UNITY_EDITOR_WIN
                var path = EditorUtility.OpenFilePanel("Select SteamCMD", "", "exe");
#else
                var path = EditorUtility.OpenFilePanel("Select SteamCMD", "", "sh");
#endif
                if (!string.IsNullOrEmpty(path))
                {
                    _serializedBuildConfig.FindProperty("SteamCmdPath").stringValue = path;
                }
            }
            if (GUILayout.Button("Auto", GUILayout.Width(40)))
            {
                _serializedBuildConfig.FindProperty("SteamCmdPath").stringValue = SteamPipeBuilder.GetDefaultSteamCmdPath();
            }
            EditorGUILayout.EndHorizontal();

            // Status & Download
            if (_steamCmdDownloading)
            {
                EditorGUILayout.BeginHorizontal();
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(18), GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(rect, _steamCmdDownloadProgress, _steamCmdDownloadStatus);
                EditorGUILayout.EndHorizontal();
            }
            else if (!isInstalled)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("SteamCMD not found. Download or set the correct path.", MessageType.Warning);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Download SteamCMD", GUILayout.Height(25)))
                {
                    DownloadSteamCmd();
                }
                if (GUILayout.Button("Manual Download", GUILayout.Height(25)))
                {
                    Application.OpenURL("https://developer.valvesoftware.com/wiki/SteamCMD#Downloading_SteamCMD");
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("✓ SteamCMD found", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("ContentBuilderPath"), new GUIContent("ContentBuilder"));
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select ContentBuilder Folder", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _serializedBuildConfig.FindProperty("ContentBuilderPath").stringValue = path;
                }
            }
            if (GUILayout.Button("Init", GUILayout.Width(40)))
            {
                var contentPath = _serializedBuildConfig.FindProperty("ContentBuilderPath").stringValue;
                if (string.IsNullOrEmpty(contentPath))
                {
                    contentPath = SteamPipeBuilder.GetDefaultContentBuilderPath();
                    _serializedBuildConfig.FindProperty("ContentBuilderPath").stringValue = contentPath;
                }
                SteamPipeBuilder.InitializeContentBuilder(contentPath);
                _buildDeployStatus = "ContentBuilder folder initialized.";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Account Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Steam Account", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("Username"));
            
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("StorePassword"));
            if (_buildConfig.StorePassword)
            {
                EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("Password"));
                EditorGUILayout.HelpBox("Warning: Storing password in config is not recommended for shared projects.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // App Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("App Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("AppId"));
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("DefaultDepotId"));
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("DefaultBranch"));
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("SetLiveOnUpload"));
            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("PreviewOnly"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Depots
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Depots", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_serializedBuildConfig.FindProperty("Depots"), true);

            if (_buildConfig.Depots.Count == 0)
            {
                if (GUILayout.Button("Add Default Depot"))
                {
                    _buildConfig.Depots.Add(new DepotConfig
                    {
                        Name = "Windows",
                        DepotId = _buildConfig.DefaultDepotId,
                        ContentRoot = "Build/Windows"
                    });
                    EditorUtility.SetDirty(_buildConfig);
                }
            }

            EditorGUILayout.EndVertical();

            _serializedBuildConfig.ApplyModifiedProperties();

            EditorGUILayout.Space(5);

            // Build Actions
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Build Actions", EditorStyles.boldLabel);

            // Description
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Description:", GUILayout.Width(80));
            _buildDescription = EditorGUILayout.TextField(_buildDescription);
            if (GUILayout.Button("Auto", GUILayout.Width(40)))
            {
                _buildDescription = SteamPipeBuilder.BuildDescription(_buildConfig.DescriptionTemplate);
            }
            EditorGUILayout.EndHorizontal();

            // Branch
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Branch:", GUILayout.Width(80));
            int branchIndex = _buildConfig.Branches.IndexOf(_buildBranch);
            if (branchIndex < 0) branchIndex = 0;
            branchIndex = EditorGUILayout.Popup(branchIndex, _buildConfig.Branches.ToArray());
            _buildBranch = _buildConfig.Branches[branchIndex];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate VDF", GUILayout.Height(30)))
            {
                GenerateVdfFiles();
            }

            if (GUILayout.Button("Open ContentBuilder", GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(_buildConfig.ContentBuilderPath))
                {
                    EditorUtility.RevealInFinder(_buildConfig.ContentBuilderPath);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_buildDeployRunning;
            if (GUILayout.Button("Upload to Steam", GUILayout.Height(35)))
            {
                UploadToSteam();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Open Steamworks", GUILayout.Height(35)))
            {
                Application.OpenURL($"https://partner.steamgames.com/apps/builds/{_buildConfig.AppId}");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Status / Output
            if (!string.IsNullOrEmpty(_buildDeployStatus))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(_boxStyle);
                GUILayout.Label("Status", EditorStyles.boldLabel);
                
                _buildDeployScrollPosition = EditorGUILayout.BeginScrollView(_buildDeployScrollPosition, GUILayout.Height(150));
                EditorGUILayout.TextArea(_buildDeployStatus, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Clear", GUILayout.Height(20)))
                {
                    _buildDeployStatus = "";
                }

                EditorGUILayout.EndVertical();
            }
        }

        #region Build Deploy Fields

        private SteamBuildConfig _buildConfig;
        private SerializedObject _serializedBuildConfig;
        private string _buildDescription = "";
        private string _buildBranch = "default";
        private string _buildDeployStatus = "";
        private bool _buildDeployRunning;
        private Vector2 _buildDeployScrollPosition;

        // SteamCMD download
        private bool _steamCmdDownloading;
        private float _steamCmdDownloadProgress;
        private string _steamCmdDownloadStatus = "";

        private void DownloadSteamCmd()
        {
            string installDir = SteamPipeBuilder.GetDefaultSteamCmdDirectory();

            _steamCmdDownloading = true;
            _steamCmdDownloadProgress = 0f;
            _steamCmdDownloadStatus = "Starting download...";

            SteamPipeBuilder.DownloadSteamCmd(installDir,
                (progress, status) =>
                {
                    _steamCmdDownloadProgress = progress;
                    _steamCmdDownloadStatus = status;
                    Repaint();
                },
                (success, result) =>
                {
                    _steamCmdDownloading = false;

                    if (success)
                    {
                        _serializedBuildConfig.FindProperty("SteamCmdPath").stringValue = result;
                        _serializedBuildConfig.ApplyModifiedProperties();
                        _buildDeployStatus = $"SteamCMD installed successfully!\nPath: {result}";
                        Debug.Log($"[SteamToolkit] SteamCMD installed: {result}");
                    }
                    else
                    {
                        _buildDeployStatus = $"SteamCMD installation failed: {result}";
                        Debug.LogError($"[SteamToolkit] SteamCMD installation failed: {result}");
                    }

                    Repaint();
                }
            );
        }

        private void CreateBuildConfig()
        {
            var config = ScriptableObject.CreateInstance<SteamBuildConfig>();

            // Set defaults from main config
            if (_config != null)
            {
                config.AppId = _config.AppId;
                config.DefaultDepotId = _config.AppId + 1;
            }

            config.SteamCmdPath = SteamPipeBuilder.GetDefaultSteamCmdPath();
            config.ContentBuilderPath = SteamPipeBuilder.GetDefaultContentBuilderPath();

            // Save asset
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            string assetPath = $"{resourcesPath}/SteamBuildConfig.asset";
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _buildConfig = config;
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[SteamToolkit] Created: {assetPath}");
        }

        private void GenerateVdfFiles()
        {
            if (string.IsNullOrEmpty(_buildDescription))
            {
                _buildDescription = SteamPipeBuilder.BuildDescription(_buildConfig.DescriptionTemplate);
            }

            try
            {
                SteamPipeBuilder.WriteVdfFiles(_buildConfig, _buildDescription, _buildBranch);
                _buildDeployStatus = "VDF files generated successfully!\n\n" +
                    $"App VDF: app_{_buildConfig.AppId}.vdf\n";
                
                foreach (var depot in _buildConfig.Depots)
                {
                    _buildDeployStatus += $"Depot VDF: depot_{depot.DepotId}.vdf\n";
                }
            }
            catch (Exception ex)
            {
                _buildDeployStatus = $"ERROR: {ex.Message}";
            }
        }

        private void UploadToSteam()
        {
            // Validate
            if (string.IsNullOrEmpty(_buildConfig.Username))
            {
                _buildDeployStatus = "ERROR: Username not set.";
                return;
            }

            if (string.IsNullOrEmpty(_buildConfig.SteamCmdPath))
            {
                _buildDeployStatus = "ERROR: SteamCMD path not set.";
                return;
            }

            // Get password
            string password = _buildConfig.StorePassword ? _buildConfig.Password : "";
            
            if (!_buildConfig.StorePassword)
            {
                password = EditorInputDialog.Show("Steam Password", "Enter Steam password:", "", true);
                if (password == null) return; // Cancelled
            }

            // Generate VDF if needed
            GenerateVdfFiles();

            // Run SteamCMD
            _buildDeployRunning = true;
            _buildDeployStatus = "Starting upload...\n";

            SteamPipeBuilder.RunSteamCmd(_buildConfig, password,
                output =>
                {
                    _buildDeployStatus += output + "\n";
                    Repaint();
                },
                exitCode =>
                {
                    _buildDeployRunning = false;
                    _buildDeployStatus += $"\n=== Completed with exit code: {exitCode} ===";
                    Repaint();
                }
            );
        }

        #endregion

        private void DrawSettingsTab()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (_config == null)
            {
                DrawNoConfigWarning();
                return;
            }

            _serializedConfig.Update();

            // Initialization Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Initialization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("AutoInitialize"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("AllowWithoutSteam"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("CheckRestartApp"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Debug Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableDebugLogs"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("TestMode"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Publisher API Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Publisher API", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("PublisherApiKey"), new GUIContent("API Key"));
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Get API Key", GUILayout.Height(20)))
            {
                Application.OpenURL("https://partner.steamgames.com/pub/webapi");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Publisher API Key enables Edit Mode features:\n" +
                "• View Achievements & Stats schema\n" +
                "• View Inventory item definitions\n" +
                "• Access Leaderboard data\n\n" +
                "Get your key from Steamworks Partner site.", 
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Service Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Services", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableAchievements"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableStats"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableInventory"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableLeaderboards"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableCloudSave"));
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("EnableWorkshop"));
            EditorGUILayout.EndVertical();

            _serializedConfig.ApplyModifiedProperties();

            EditorGUILayout.Space(10);

            // Config Actions
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Config Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Config", GUILayout.Height(25)))
            {
                Selection.activeObject = _config;
                EditorGUIUtility.PingObject(_config);
            }
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Reset Config", 
                    "Are you sure you want to reset all settings to defaults?", 
                    "Yes", "Cancel"))
                {
                    ResetConfigToDefaults();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ResetConfigToDefaults()
        {
            Undo.RecordObject(_config, "Reset Steam Config");
            _config.AppId = 480;
            _config.GameName = "My Game";
            _config.AutoInitialize = true;
            _config.AllowWithoutSteam = true;
            _config.CheckRestartApp = true;
            _config.EnableDebugLogs = true;
            _config.TestMode = false;
            _config.EnableAchievements = true;
            _config.EnableStats = true;
            _config.EnableInventory = false;
            _config.EnableLeaderboards = false;
            _config.EnableCloudSave = false;
            _config.EnableWorkshop = false;
            EditorUtility.SetDirty(_config);
        }

        #endregion

        #region Helpers

        private void DrawNoConfigWarning()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.HelpBox(
                "SteamConfig not found!\n\n" +
                "Please create a SteamConfig in the Resources folder:\n" +
                "Right Click > Create > Steam Toolkit > Config",
                MessageType.Error
            );

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Create SteamConfig", GUILayout.Height(30)))
            {
                CreateConfig();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void CreateConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var config = CreateInstance<SteamConfig>();
            AssetDatabase.CreateAsset(config, "Assets/Resources/SteamConfig.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _config = config;
            _serializedConfig = new SerializedObject(_config);

            EditorUtility.DisplayDialog("Success", "SteamConfig created!\n\nAssets/Resources/SteamConfig.asset", "OK");
        }

        #endregion
    }
}
#endif