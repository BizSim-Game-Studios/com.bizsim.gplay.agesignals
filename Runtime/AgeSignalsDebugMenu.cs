// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Runtime debug menu for testing Age Signals behavior on-device.
    /// Only active in <b>debug builds</b> (<c>Debug.isDebugBuild == true</c>).
    /// Stripped entirely from release builds.
    ///
    /// <b>Usage:</b> Add this component to any persistent GameObject.
    /// Open the menu at runtime by tapping the top-left corner 5 times
    /// or pressing <c>F9</c> on keyboard.
    ///
    /// <b>Features:</b>
    /// <list type="bullet">
    /// <item>Simulate any <see cref="AgeVerificationStatus"/> at runtime</item>
    /// <item>Inject custom age ranges and trigger <c>CheckAgeSignals()</c></item>
    /// <item>View current <see cref="AgeRestrictionFlags"/> in real-time</item>
    /// <item>Clear cached flags from <c>PlayerPrefs</c></item>
    /// </list>
    /// </summary>
    [HelpURL("https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals#step-3--editor-testing-with-mock-data")]
    [AddComponentMenu("BizSim/Age Signals Debug Menu")]
    public class AgeSignalsDebugMenu : MonoBehaviour
    {
        private bool _showMenu;
        private int _tapCount;
        private float _lastTapTime;

        // Mock selection state
        private int _selectedStatusIndex;
        private int _mockAgeLower = 13;
        private int _mockAgeUpper = 17;
        private string _ageLowerInput = "13";
        private string _ageUpperInput = "17";

        private Vector2 _scrollPos;

        private static readonly string[] StatusNames = Enum.GetNames(typeof(AgeVerificationStatus));

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
#endif

        private void Update()
        {
            if (!Debug.isDebugBuild) return;

#if ENABLE_INPUT_SYSTEM
            // F9 toggle on keyboard (New Input System)
            if (Keyboard.current != null && Keyboard.current[Key.F9].wasPressedThisFrame)
                _showMenu = !_showMenu;

            // 5-tap toggle on mobile (top-left 100x100 area)
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
                var pos = touch.screenPosition;
                if (pos.x < 100 && pos.y > Screen.height - 100)
                {
                    if (Time.unscaledTime - _lastTapTime > 2f)
                        _tapCount = 0;

                    _tapCount++;
                    _lastTapTime = Time.unscaledTime;

                    if (_tapCount >= 5)
                    {
                        _showMenu = !_showMenu;
                        _tapCount = 0;
                    }
                }
                break; // only process first touch
            }
#else
            // F9 toggle on keyboard (Legacy Input)
            if (Input.GetKeyDown(KeyCode.F9))
                _showMenu = !_showMenu;

            // 5-tap toggle on mobile (top-left 100x100 area)
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                var pos = Input.GetTouch(0).position;
                if (pos.x < 100 && pos.y > Screen.height - 100)
                {
                    if (Time.unscaledTime - _lastTapTime > 2f)
                        _tapCount = 0;

                    _tapCount++;
                    _lastTapTime = Time.unscaledTime;

                    if (_tapCount >= 5)
                    {
                        _showMenu = !_showMenu;
                        _tapCount = 0;
                    }
                }
            }
#endif
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild || !_showMenu) return;

            float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            int padding = Mathf.RoundToInt(10 * scale);
            int width = Mathf.Min(Mathf.RoundToInt(360 * scale), Screen.width - padding * 2);
            int height = Mathf.Min(Mathf.RoundToInt(520 * scale), Screen.height - padding * 2);

            GUILayout.BeginArea(new Rect(padding, padding, width, height),
                GUI.skin.box);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("<b>Age Signals Debug Menu</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = Mathf.RoundToInt(16 * scale) });

            GUILayout.Space(8);

            // --- Current State ---
            var ctrl = AgeSignalsController.Instance;
            if (ctrl == null)
            {
                GUILayout.Label("⚠ AgeSignalsController not found");
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"IsChecking: {ctrl.IsChecking}");

            var flags = ctrl.CurrentFlags;
            if (flags != null)
            {
                GUILayout.Label("<b>Current Flags:</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                GUILayout.Label($"  FullAccess: {flags.FullAccessGranted}");
                GUILayout.Label($"  AccessDenied: {flags.AccessDenied}");
                if (flags.Features != null && flags.Features.Count > 0)
                {
                    foreach (var f in flags.Features)
                        GUILayout.Label($"  [{f.key}]: {f.enabled}");
                }
                else
                {
                    // Fallback to deprecated fields for old data
#pragma warning disable CS0618
                    GUILayout.Label($"  FeatureA: {flags.FeatureAEnabled}");
                    GUILayout.Label($"  FeatureB: {flags.FeatureBFullAccess}");
                    GUILayout.Label($"  FeatureC: {flags.FeatureCEnabled}");
#pragma warning restore CS0618
                }
                GUILayout.Label($"  PersonalizedAds: {flags.PersonalizedAdsEnabled}");
                GUILayout.Label($"  NeedsVerification: {flags.NeedsVerification}");
                GUILayout.Label($"  Timestamp: {flags.DecisionTimestamp}");
            }
            else
            {
                GUILayout.Label("Flags: null (not loaded)");
            }

            GUILayout.Space(12);

            // --- Mock Injection ---
            GUILayout.Label("<b>Inject Mock Result:</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            _selectedStatusIndex = GUILayout.SelectionGrid(
                _selectedStatusIndex, StatusNames, 2);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Age Lower:", GUILayout.Width(80 * scale));
            _ageLowerInput = GUILayout.TextField(_ageLowerInput, GUILayout.Width(60 * scale));
            GUILayout.Label("Upper:", GUILayout.Width(50 * scale));
            _ageUpperInput = GUILayout.TextField(_ageUpperInput, GUILayout.Width(60 * scale));
            GUILayout.EndHorizontal();

            int.TryParse(_ageLowerInput, out _mockAgeLower);
            int.TryParse(_ageUpperInput, out _mockAgeUpper);

            if (GUILayout.Button("▶ Inject Mock Result"))
            {
                InjectMockResult(
                    (AgeVerificationStatus)_selectedStatusIndex,
                    _mockAgeLower,
                    _mockAgeUpper);
            }

            GUILayout.Space(8);

            if (GUILayout.Button("🔄 Re-run CheckAgeSignals()"))
            {
                ctrl.CheckAgeSignals();
            }

            if (GUILayout.Button("🗑 Clear Cached Flags"))
            {
                PlayerPrefs.DeleteKey(AgeSignalsController.FLAGS_PREFS_KEY);
                PlayerPrefs.Save();
                Debug.Log("[AgeSignals Debug] Cached flags cleared");
            }

            GUILayout.Space(8);

            if (GUILayout.Button("✕ Close"))
                _showMenu = false;

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Injects a mock <see cref="AgeSignalsResult"/> directly into the controller's
        /// processing pipeline, bypassing the Java bridge entirely.
        /// Useful for on-device testing of different age scenarios.
        /// </summary>
        private void InjectMockResult(AgeVerificationStatus status, int ageLower, int ageUpper)
        {
            var ctrl = AgeSignalsController.Instance;
            if (ctrl == null)
            {
                Debug.LogWarning("[AgeSignals Debug] Controller not found");
                return;
            }

            // Build a JSON string matching the Java bridge success format
            // and send it via UnitySendMessage simulation
            string json = $@"{{
                ""userStatus"": ""{StatusToString(status)}"",
                ""ageLower"": {ageLower},
                ""ageUpper"": {ageUpper},
                ""installId"": null,
                ""mostRecentApprovalDate"": 0
            }}";

            Debug.Log($"[AgeSignals Debug] Injecting mock: status={status}, age=[{ageLower}-{ageUpper}]");

            // Invoke the private callback via SendMessage (same path as Java bridge)
            ctrl.SendMessage("OnAgeSignalsResult", json, SendMessageOptions.RequireReceiver);
        }

        private static string StatusToString(AgeVerificationStatus status)
        {
            return status switch
            {
                AgeVerificationStatus.Verified => "VERIFIED",
                AgeVerificationStatus.Supervised => "SUPERVISED",
                AgeVerificationStatus.SupervisedApprovalPending => "SUPERVISED_APPROVAL_PENDING",
                AgeVerificationStatus.SupervisedApprovalDenied => "SUPERVISED_APPROVAL_DENIED",
                AgeVerificationStatus.Unknown => "UNKNOWN",
                _ => "null"
            };
        }
    }
}
