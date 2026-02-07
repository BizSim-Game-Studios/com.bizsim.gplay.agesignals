// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="AgeSignalsMockConfig"/> that provides a visual
    /// mock response editor, error simulation controls, a JSON preview card,
    /// and a reference guide for verification statuses.
    /// </summary>
    [CustomEditor(typeof(AgeSignalsMockConfig))]
    public class AgeSignalsMockConfigEditor : UnityEditor.Editor
    {
        // ── Palette ──
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color AccentDim = new(0.35f, 0.61f, 1f, 0.08f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color GreenDim = new(0.24f, 0.78f, 0.42f, 0.14f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color RedDim = new(0.92f, 0.34f, 0.34f, 0.14f);
        private static readonly Color Warn = new(1f, 0.82f, 0.22f);
        private static readonly Color WarnDim = new(1f, 0.82f, 0.22f, 0.12f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);

        // ── Serialized Properties ──
        private SerializedProperty _mockStatus;
        private SerializedProperty _mockAge;
        private SerializedProperty _simulateError;
        private SerializedProperty _simulatedErrorCode;

        private void OnEnable()
        {
            _mockStatus = serializedObject.FindProperty("MockStatus");
            _mockAge = serializedObject.FindProperty("MockAge");
            _simulateError = serializedObject.FindProperty("SimulateError");
            _simulatedErrorCode = serializedObject.FindProperty("SimulatedErrorCode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (AgeSignalsMockConfig)target;

            // ═══════════════════════════════════════════
            // Mock Response
            // ═══════════════════════════════════════════
            DrawMockResponseCard(config);

            EditorGUILayout.Space(6);

            // ═══════════════════════════════════════════
            // Error Simulation
            // ═══════════════════════════════════════════
            DrawErrorSimulationCard();

            EditorGUILayout.Space(6);

            // ═══════════════════════════════════════════
            // Preview — what the API would return
            // ═══════════════════════════════════════════
            DrawPreviewCard(config);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // ═══════════════════════════════════════════
            // What is this?
            // ═══════════════════════════════════════════
            DrawInfoCard();
        }

        // ─────────────────────────────────────────────
        // Mock Response Card
        // ─────────────────────────────────────────────

        private void DrawMockResponseCard(AgeSignalsMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Mock API Response", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_mockStatus, new GUIContent("Status", "Verification status the API would return."));

            var status = (AgeVerificationStatus)_mockStatus.enumValueIndex;
            bool isSupervised = status == AgeVerificationStatus.Supervised ||
                                status == AgeVerificationStatus.SupervisedApprovalPending ||
                                status == AgeVerificationStatus.SupervisedApprovalDenied;

            // Age slider — only relevant for supervised statuses
            using (new EditorGUI.DisabledGroupScope(!isSupervised))
            {
                EditorGUILayout.IntSlider(_mockAge, 5, 25,
                    new GUIContent("Age", "Simulated age. Only used for supervised statuses."));
            }

            // Computed age range preview
            GUILayout.Space(4);
            string rangeText;
            Color rangeColor;
            if (status == AgeVerificationStatus.Verified)
            {
                rangeText = "Age range: 18 – 150  (confirmed adult)";
                rangeColor = Green;
            }
            else if (isSupervised)
            {
                int age = _mockAge.intValue;
                int lo = Mathf.Max(0, age - 2);
                int hi = age + 2;
                rangeText = $"Age range: {lo} – {hi}  (±2 year bucket)";
                rangeColor = age < 13 ? Red : age < 18 ? Warn : Green;
            }
            else if (status == AgeVerificationStatus.Unknown)
            {
                rangeText = "Age range: unknown  (no age data, needs verification)";
                rangeColor = Warn;
            }
            else
            {
                rangeText = "Age range: N/A  (outside supported jurisdiction)";
                rangeColor = Muted;
            }

            var rangeRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rangeRect, new Color(rangeColor.r, rangeColor.g, rangeColor.b, 0.08f));
            EditorGUI.DrawRect(new Rect(rangeRect.x, rangeRect.y, 3, rangeRect.height), rangeColor);
            EditorGUI.LabelField(new Rect(rangeRect.x + 10, rangeRect.y, rangeRect.width - 12, rangeRect.height),
                rangeText, new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = rangeColor }
                });

            // Access denied warning
            if (status == AgeVerificationStatus.SupervisedApprovalDenied)
            {
                GUILayout.Space(4);
                var warnRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.DrawRect(warnRect, RedDim);
                EditorGUI.DrawRect(new Rect(warnRect.x, warnRect.y, 3, warnRect.height), Red);
                EditorGUI.LabelField(new Rect(warnRect.x + 10, warnRect.y, warnRect.width - 12, warnRect.height),
                    "⚠  This status blocks all access (parental approval denied).",
                    new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = Red }
                    });
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Error Simulation Card
        // ─────────────────────────────────────────────

        private void DrawErrorSimulationCard()
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Error Simulation", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_simulateError,
                new GUIContent("Simulate Error", "Return an error instead of a successful response."));

            if (_simulateError.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_simulatedErrorCode,
                    new GUIContent("Error Code", "API error code to simulate."));

                GUILayout.Space(4);

                // Error code reference
                int code = _simulatedErrorCode.intValue;
                // Reuse AgeSignalsError for consistent name resolution
                var tempError = new AgeSignalsError { errorCode = code };
                string codeName = tempError.ErrorCodeName;
                bool retryable = AgeSignalsError.IsRetryableCode(code);

                var infoRect = EditorGUILayout.GetControlRect(false, 20);
                Color infoColor = retryable ? Warn : Red;
                EditorGUI.DrawRect(infoRect, new Color(infoColor.r, infoColor.g, infoColor.b, 0.08f));
                EditorGUI.DrawRect(new Rect(infoRect.x, infoRect.y, 3, infoRect.height), infoColor);
                string retryText = retryable ? "retryable" : "not retryable";
                EditorGUI.LabelField(
                    new Rect(infoRect.x + 10, infoRect.y, infoRect.width - 12, infoRect.height),
                    $"{codeName}  •  {retryText}",
                    new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        normal = { textColor = infoColor }
                    });

                EditorGUI.indentLevel--;
            }
            else
            {
                DrawNote("Enable to test how your game handles API failures.");
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Preview Card — simulated JSON output
        // ─────────────────────────────────────────────

        private void DrawPreviewCard(AgeSignalsMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            DrawNote("This is what the controller will receive in Play Mode.");
            GUILayout.Space(4);

            var codeStyle = new GUIStyle(EditorStyles.label)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                fontSize = 11,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.86f, 0.68f) },
                padding = new RectOffset(8, 8, 6, 6)
            };
            var codeBg = new Color(0.12f, 0.12f, 0.12f, 0.8f);

            string json;
            if (config.SimulateError)
            {
                int code = config.SimulatedErrorCode;
                var tempError = new AgeSignalsError { errorCode = code };
                string codeName = tempError.ErrorCodeName;
                bool retryable = AgeSignalsError.IsRetryableCode(code);
                json = $"<color=#569cd6>Error Response</color>\n" +
                       $"  errorCode: <color=#b5cea8>{code}</color>\n" +
                       $"  errorMessage: <color=#ce9178>\"Simulated error (mock config)\"</color>\n" +
                       $"  isRetryable: <color=#569cd6>{retryable.ToString().ToLower()}</color>\n" +
                       $"  codeName: <color=#ce9178>\"{codeName}\"</color>";
            }
            else
            {
                var status = config.MockStatus;
                string statusStr = status switch
                {
                    AgeVerificationStatus.Verified => "VERIFIED",
                    AgeVerificationStatus.Supervised => "SUPERVISED",
                    AgeVerificationStatus.SupervisedApprovalPending => "SUPERVISED_APPROVAL_PENDING",
                    AgeVerificationStatus.SupervisedApprovalDenied => "SUPERVISED_APPROVAL_DENIED",
                    AgeVerificationStatus.Unknown => "UNKNOWN",
                    _ => "null"
                };
                string loStr = config.AgeLower < 0 ? "null" : config.AgeLower.ToString();
                string hiStr = config.AgeUpper < 0 ? "null" : config.AgeUpper.ToString();

                json = $"<color=#569cd6>Success Response</color>\n" +
                       $"  userStatus: <color=#ce9178>\"{statusStr}\"</color>\n" +
                       $"  ageLower: <color=#b5cea8>{loStr}</color>\n" +
                       $"  ageUpper: <color=#b5cea8>{hiStr}</color>\n" +
                       $"  installId: <color=#569cd6>null</color>\n" +
                       $"  mostRecentApprovalDate: <color=#b5cea8>0</color>";
            }

            var codeRect = EditorGUILayout.GetControlRect(false, 100);
            EditorGUI.DrawRect(codeRect, codeBg);
            EditorGUI.LabelField(codeRect, json, codeStyle);

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // What is this?
        // ─────────────────────────────────────────────

        private static void DrawInfoCard()
        {
            var outer = EditorGUILayout.BeginVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outer, AccentDim);
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, 3, outer.height), Accent);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("What is this?", EditorStyles.boldLabel);
            GUILayout.Space(2);

            EditorGUILayout.LabelField(
                "This asset lets you test Age Signals without a real device or Google Play account.\n\n" +
                "It simulates what the Google Play Age Signals API would return — a user's verification\n" +
                "status and age range. The Age Signals Controller uses this mock data in Play Mode\n" +
                "instead of calling the real API.\n\n" +
                "This is Editor-only. It has no effect in actual builds.",
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                });

            GUILayout.Space(6);

            EditorGUILayout.LabelField("How to use:", EditorStyles.boldLabel);
            DrawBullet("Pick a Status — this is the user type you want to simulate.");
            DrawBullet("Set an Age — only matters for supervised (child/teen) statuses.");
            DrawBullet("Enable \"Simulate Error\" to test error handling in your game.");
            DrawBullet("Assign this asset to the Mock Config field on the Age Signals Controller.");
            DrawBullet("Enter Play Mode → the controller reads this config instead of calling the API.");

            GUILayout.Space(6);

            EditorGUILayout.LabelField("Status reference:", EditorStyles.boldLabel);
            DrawStatusRef("Verified", "Confirmed 18+ adult (official ID, credit card, or age estimation).");
            DrawStatusRef("Supervised", "Child/teen managed through Google Family Link.");
            DrawStatusRef("Supervised Approval Pending", "Parental approval requested, waiting for response.");
            DrawStatusRef("Supervised Approval Denied", "Parent explicitly denied access — blocks everything.");
            DrawStatusRef("Unknown", "User exists but hasn't been verified yet.");
            DrawStatusRef("Not Applicable", "Outside supported jurisdiction — no age data available.");

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private static void DrawCardBg(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, CardBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), SepColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), SepColor);
        }

        private static void BeginPadded()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();
        }

        private static void EndPadded()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(14);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawNote(string text)
        {
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontStyle = FontStyle.Italic
            });
        }

        private static void DrawBullet(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField("•", GUILayout.Width(10));
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.78f, 0.78f) }
            });
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawStatusRef(string status, string desc)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            EditorGUILayout.LabelField($"<b>{status}</b>", new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11
            }, GUILayout.Width(200));
            EditorGUILayout.LabelField(desc, new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.72f, 0.72f, 0.72f) }
            });
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
