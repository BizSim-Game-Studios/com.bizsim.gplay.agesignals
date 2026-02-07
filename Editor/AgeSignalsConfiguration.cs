using BizSim.GPlay.EditorCore;
using UnityEditor;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Editor
{
    /// <summary>
    /// Configuration window for Age Signals package.
    /// Menu: BizSim → Google Play → Age Signals → Configuration
    /// </summary>
    public class AgeSignalsConfiguration : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("BizSim/Google Play/Age Signals/Configuration", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AgeSignalsConfiguration>("Age Signals Config");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            DrawHeader();

            GUILayout.Space(10);

            // Firebase Section
            DrawFirebaseSection();

            GUILayout.Space(10);

            // Optional: Input System Section (if needed)
            // DrawInputSystemSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Age Signals Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure optional integrations for the Age Signals package.",
                MessageType.Info);
        }

        private void DrawFirebaseSection()
        {
            GUILayout.Label("📦 Firebase Analytics Integration", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Package status
            bool packageInstalled = BizSimDefineManager.IsFirebaseAnalyticsInstalled();
            string version = BizSimDefineManager.GetFirebaseAnalyticsVersion();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Package Status:", GUILayout.Width(120));
            if (packageInstalled)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"✓ Installed (v{version})", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField("✗ Not Found", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Define status
            bool definePresent = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();
            var platformsWithDefine = BizSimDefineManager.GetPlatformsWithFirebaseDefine();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Define Symbol:", GUILayout.Width(120));
            if (definePresent)
            {
                GUI.color = Color.green;
                string platforms = string.Join(", ", platformsWithDefine);
                EditorGUILayout.LabelField($"✓ BIZSIM_FIREBASE ({platforms})", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField("✗ BIZSIM_FIREBASE (Missing)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Status message
            MessageType messageType;
            string statusMessage = BizSimDefineManager.GetFirebaseStatusMessage(out messageType);
            EditorGUILayout.HelpBox(statusMessage, messageType);

            GUILayout.Space(10);

            // Buttons
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = packageInstalled && !definePresent;
            if (GUILayout.Button("Add BIZSIM_FIREBASE", GUILayout.Height(30)))
            {
                BizSimDefineManager.AddFirebaseDefineAllPlatforms();
                Debug.Log("[Age Signals] Firebase Analytics integration enabled.");
                ShowNotification(new GUIContent("✓ BIZSIM_FIREBASE added successfully"));
            }
            GUI.enabled = true;

            GUI.enabled = definePresent;
            if (GUILayout.Button("Remove BIZSIM_FIREBASE", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog(
                    "Remove Firebase Integration",
                    "This will remove BIZSIM_FIREBASE from all platforms. Are you sure?",
                    "Yes, Remove",
                    "Cancel"))
                {
                    BizSimDefineManager.RemoveFirebaseDefineAllPlatforms();
                    Debug.Log("[Age Signals] Firebase Analytics integration disabled.");
                    ShowNotification(new GUIContent("✓ BIZSIM_FIREBASE removed"));
                }
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Help text
            EditorGUILayout.LabelField("ℹ️ When enabled, Age Signals will log analytics events to Firebase Analytics automatically.",
                EditorStyles.wordWrappedMiniLabel);

            if (!packageInstalled)
            {
                GUILayout.Space(5);
                if (GUILayout.Button("Install Firebase Analytics Package"))
                {
                    Application.OpenURL("https://firebase.google.com/docs/unity/setup");
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
