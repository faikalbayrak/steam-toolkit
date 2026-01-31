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
            window.titleContent = new GUIContent("Steam Toolkit", EditorGUIUtility.IconContent("d_BuildSettings.Steam").image);
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
            if (string.IsNullOrEmpty(_config.WebApiKey))
            {
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.HelpBox(
                    "Steam Web API Key required to view achievements in Edit Mode.\n\n" +
                    "1. Go to: https://steamcommunity.com/dev/apikey\n" +
                    "2. Generate an API key\n" +
                    "3. Paste it in Settings tab → Web API Key",
                    MessageType.Info
                );

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Get API Key", GUILayout.Height(25)))
                {
                    Application.OpenURL("https://steamcommunity.com/dev/apikey");
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
                _config.WebApiKey,
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

        private void DrawStatsTab()
        {
            GUILayout.Label("Stats", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Stats management:\n" +
                "• Read/write Int/Float stats\n" +
                "• Stat definitions\n" +
                "• Test values",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawInventoryTab()
        {
            GUILayout.Label("Inventory", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Inventory management:\n" +
                "• Item definitions\n" +
                "• Grant/Revoke items\n" +
                "• Promo items\n" +
                "• Drop rates",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawLeaderboardsTab()
        {
            GUILayout.Label("Leaderboards", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Leaderboard management:\n" +
                "• Leaderboard list\n" +
                "• Upload/download scores\n" +
                "• View top 10",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCloudSaveTab()
        {
            GUILayout.Label("Cloud Save", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Cloud Save management:\n" +
                "• Remote storage files\n" +
                "• Upload/Download\n" +
                "• Quota info",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawWorkshopTab()
        {
            GUILayout.Label("Workshop", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Workshop (UGC) management:\n" +
                "• Item upload\n" +
                "• Subscription management\n" +
                "• Content browser",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawBuildDeployTab()
        {
            GUILayout.Label("Build & Deploy", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.HelpBox(
                "Build & Deploy:\n" +
                "• SteamPipe integration\n" +
                "• One-click build & upload\n" +
                "• Depot management\n" +
                "• Branch selection",
                MessageType.Info
            );
            
            EditorGUILayout.Space(5);
            GUILayout.Label("Coming soon...", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

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

            // Web API Settings
            EditorGUILayout.BeginVertical(_boxStyle);
            GUILayout.Label("Web API", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedConfig.FindProperty("WebApiKey"), new GUIContent("API Key"));
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Get API Key", GUILayout.Height(20)))
            {
                Application.OpenURL("https://steamcommunity.com/dev/apikey");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox("Web API Key is used to fetch achievement data in Edit Mode. Get one from Steam.", MessageType.Info);
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