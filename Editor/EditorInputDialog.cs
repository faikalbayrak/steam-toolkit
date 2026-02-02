#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SteamToolkit.Editor
{
    /// <summary>
    /// Simple input dialog for Editor.
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _value = "";
        private string _message = "";
        private bool _isPassword;
        private bool _confirmed;
        private bool _initialized;

        private static string _result;

        /// <summary>
        /// Show input dialog and return result.
        /// Returns null if cancelled.
        /// </summary>
        public static string Show(string title, string message, string defaultValue = "", bool isPassword = false)
        {
            _result = null;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window._message = message;
            window._value = defaultValue;
            window._isPassword = isPassword;
            window._confirmed = false;
            window._initialized = false;

            window.minSize = new Vector2(300, 120);
            window.maxSize = new Vector2(400, 120);

            window.ShowModalUtility();

            return _result;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            GUILayout.Label(_message, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(5);

            GUI.SetNextControlName("InputField");

            if (_isPassword)
            {
                _value = EditorGUILayout.PasswordField(_value);
            }
            else
            {
                _value = EditorGUILayout.TextField(_value);
            }

            if (!_initialized)
            {
                EditorGUI.FocusTextInControl("InputField");
                _initialized = true;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                _result = null;
                Close();
            }

            if (GUILayout.Button("OK", GUILayout.Width(80)))
            {
                _result = _value;
                _confirmed = true;
                Close();
            }

            EditorGUILayout.EndHorizontal();

            // Handle Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                _result = _value;
                _confirmed = true;
                Close();
            }

            // Handle Escape key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                _result = null;
                Close();
            }
        }
    }
}
#endif