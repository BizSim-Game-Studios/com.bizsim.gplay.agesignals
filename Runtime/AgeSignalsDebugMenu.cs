// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using UnityEngine;

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
    /// <item>View current <see cref="AgeRestrictionFlags"/> in real-time</item>
    /// <item>Trigger <c>CheckAgeSignals()</c> manually</item>
    /// <item>Clear cached flags from <c>PlayerPrefs</c></item>
    /// </list>
    /// </summary>
    [AddComponentMenu("BizSim/Age Signals/Debug Menu")]
    public class AgeSignalsDebugMenu : MonoBehaviour
    {
        private bool _showMenu = false;
        private Vector2 _scrollPosition;

        // Tap detection state
        private float _lastTapTime;
        private int _tapCount;

        private void Update()
        {
            if (!Debug.isDebugBuild) return;

            // F9 toggle on keyboard (Legacy Input - works everywhere)
            if (Input.GetKeyDown(KeyCode.F9))
                _showMenu = !_showMenu;

            // 5-tap toggle on mobile (top-left 100x100 area)
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    var pos = touch.position;
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
            }
        }

        private void OnGUI()
        {
            if (!_showMenu || !Debug.isDebugBuild) return;

            var screenRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
            GUILayout.BeginArea(screenRect, GUI.skin.box);

            GUILayout.Label("🛠️ Age Signals Debug Menu", GUI.skin.box);
            GUILayout.Space(10);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawCurrentStatus();
            GUILayout.Space(15);
            DrawActions();

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close (F9)", GUILayout.Height(40)))
                _showMenu = false;

            GUILayout.EndArea();
        }

        private void DrawCurrentStatus()
        {
            GUILayout.Label("📊 Current Status", GUI.skin.box);

            var flags = AgeSignalsController.Instance.CurrentFlags;
            if (flags == null)
            {
                GUILayout.Label("No cached flags — call CheckAgeSignals() first");
                return;
            }

            GUILayout.Label($"Full Access: {flags.FullAccessGranted}");
            GUILayout.Label($"Access Denied: {flags.AccessDenied}");
            GUILayout.Label($"Personalized Ads: {flags.PersonalizedAdsEnabled}");
            GUILayout.Label($"Needs Verification: {flags.NeedsVerification}");

            if (flags.Features != null && flags.Features.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Feature Flags:");
                foreach (var feature in flags.Features)
                {
                    GUILayout.Label($"  • {feature.key}: {feature.enabled}");
                }
            }

            GUILayout.Space(5);
            GUILayout.Label($"Decision Time: {flags.DecisionTimestamp}", GUI.skin.label);
            GUILayout.Label($"SDK Version: {flags.SdkVersion}", GUI.skin.label);
        }

        private void DrawActions()
        {
            GUILayout.Label("⚡ Actions", GUI.skin.box);

            if (GUILayout.Button("Call CheckAgeSignals()", GUILayout.Height(35)))
            {
                AgeSignalsController.Instance.CheckAgeSignals();
                Debug.Log("[Debug] Triggered Age Signals API call");
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Cached Flags", GUILayout.Height(35)))
            {
                PlayerPrefs.DeleteKey("AgeRestrictionFlags");
                PlayerPrefs.Save();
                Debug.Log("[Debug] Cleared cached flags from PlayerPrefs");
            }

            GUILayout.Space(10);

            var controller = AgeSignalsController.Instance;
            GUILayout.Label($"Is Checking: {controller.IsChecking}");
        }
    }
}
