// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Editor
{
    [CustomEditor(typeof(AgeSignalsController))]
    public class AgeSignalsControllerEditor : UnityEditor.Editor
    {
        // ── Design tokens ──
        private const float CardPad = 14f;
        private const float CardRadius = 6f; // simulated via border insets
        private const float CardSpacing = 6f;
        private const float PillH = 22f;
        private const float PillRadius = 4f;
        private const float BadgeW = 40f;

        // ── Palette (dark-theme safe, WCAG AA contrast) ──
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color CardBorder = new(1f, 1f, 1f, 0.06f);
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color AccentDim = new(0.35f, 0.61f, 1f, 0.12f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color GreenDim = new(0.24f, 0.78f, 0.42f, 0.14f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color RedDim = new(0.92f, 0.34f, 0.34f, 0.14f);
        private static readonly Color Warn = new(1f, 0.82f, 0.22f);
        private static readonly Color WarnDim = new(1f, 0.82f, 0.22f, 0.12f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);

        // ── Serialized Properties ──
        private SerializedProperty _decisionLogic;
        private SerializedProperty _mockConfig;
        private SerializedProperty _useFakeForTesting;
        private SerializedProperty _fakeStatus;
        private SerializedProperty _fakeAge;

        // ── Foldout states (static — survive recompile) ──
        private static bool _foldSetup = true;
        private static bool _foldSim = false;
        private static bool _foldResults = true;
        private static bool _foldDebug = false;
        private static bool _foldQuickStart = false;

        // ── Package info (read from package.json once) ──
        private static string _packageVersion;
        private static string _packageUrl;

        // ── Cached styles ──
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _versionStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _pillText;
        private GUIStyle _mutedLabel;
        private GUIStyle _mutedMini;
        private GUIStyle _monoArea;
        private GUIStyle _badgeStyle;
        private GUIStyle _featureLabelOn;
        private GUIStyle _featureLabelOff;

        // ══════════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            _decisionLogic = serializedObject.FindProperty("_decisionLogic");
            _mockConfig = serializedObject.FindProperty("_mockConfig");
            _useFakeForTesting = serializedObject.FindProperty("_useFakeForTesting");
            _fakeStatus = serializedObject.FindProperty("_fakeStatus");
            _fakeAge = serializedObject.FindProperty("_fakeAge");

            if (_packageVersion == null)
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(AgeSignalsController).Assembly);
                _packageVersion = packageInfo != null ? $"v{packageInfo.version}" : "v?";
                // Strip .git suffix for a clean browser URL
                string repoUrl = packageInfo?.repository?.url;
                if (!string.IsNullOrEmpty(repoUrl) && repoUrl.EndsWith(".git"))
                    repoUrl = repoUrl[..^4];
                _packageUrl = repoUrl ?? "https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals";
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 0, 0)
            };

            _subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                normal = { textColor = Muted }
            };

            _versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) },
                padding = new RectOffset(5, 5, 1, 1)
            };

            _cardTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };

            _pillText = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                padding = new RectOffset(6, 6, 1, 1)
            };

            _mutedLabel = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Muted },
                fontSize = 11
            };

            _mutedMini = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Muted },
                fontStyle = FontStyle.Italic
            };

            _monoArea = new GUIStyle(EditorStyles.textArea)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                fontSize = 11,
                wordWrap = true
            };

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                padding = new RectOffset(4, 4, 1, 1)
            };

            _featureLabelOn = new GUIStyle(EditorStyles.label) { fontSize = 11 };
            _featureLabelOff = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                normal = { textColor = Muted }
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Main Inspector
        // ══════════════════════════════════════════════════════════════════

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            var ctrl = (AgeSignalsController)target;
            var flags = ctrl.CurrentFlags;

            // ── Header ──
            DrawHeader(ctrl, flags);

            EditorGUILayout.Space(CardSpacing);

            // ── Quick Start ──
            DrawQuickStart();

            // ── Setup card ──
            DrawSetupCard();

            // ── Simulation card ──
            DrawSimulationCard();

            serializedObject.ApplyModifiedProperties();

            // ── Run Check card ──
            DrawRunCheckCard(ctrl, flags);

            // ── Results card ──
            DrawResultsCard(ctrl, flags);

            // ── Debug card ──
            DrawDebugCard(flags);

            if (Application.isPlaying)
                Repaint();
        }

        // ══════════════════════════════════════════════════════════════════
        // Header
        // ══════════════════════════════════════════════════════════════════

        private void DrawHeader(AgeSignalsController ctrl, AgeRestrictionFlags flags)
        {
            EditorGUILayout.Space(4);

            // Title row: [Title] [status badge] [version]
            var titleRect = EditorGUILayout.GetControlRect(false, 22);

            // Title
            EditorGUI.LabelField(titleRect, "Age Signals", _titleStyle);

            // Version label (right-aligned, subtle)
            float vw = 36f;
            var vRect = new Rect(titleRect.xMax - vw, titleRect.y + 2, vw, 16);
            var prevC = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.3f);
            EditorGUIUtility.AddCursorRect(vRect, MouseCursor.Link);
            if (GUI.Button(vRect, _packageVersion, _versionStyle))
                Application.OpenURL(_packageUrl);
            GUI.color = prevC;

            // Status badge (next to version)
            string statusLabel;
            Color statusColor;
            GetStatus(ctrl, flags, out statusLabel, out statusColor);

            float sw = _pillText.CalcSize(new GUIContent(statusLabel)).x + 14;
            var sRect = new Rect(vRect.x - sw - 4, titleRect.y + 3, sw, 16);
            DrawPill(sRect, statusLabel, statusColor, new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f));

            // Accent line
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(Accent.r, Accent.g, Accent.b, 0.4f));

            // Last check metadata
            if (flags != null && !string.IsNullOrEmpty(flags.DecisionTimestamp))
            {
                string ts = flags.DecisionTimestamp;
                int tIdx = ts.IndexOf('T');
                if (tIdx >= 0 && ts.Length > tIdx + 9)
                    ts = ts.Substring(tIdx + 1, 8);

                string source = GetSourceLabel();
                var metaRect = EditorGUILayout.GetControlRect(false, 14);
                EditorGUI.LabelField(metaRect, $"Last check: {ts}  •  Source: {source}", _mutedMini);
            }
        }

        private void GetStatus(AgeSignalsController ctrl, AgeRestrictionFlags flags,
            out string label, out Color color)
        {
            if (ctrl.IsChecking)
            {
                label = "Checking";
                color = Accent;
            }
            else if (flags == null)
            {
                label = "Idle";
                color = Muted;
            }
            else if (flags.AccessDenied)
            {
                label = "Denied";
                color = Red;
            }
            else if (flags.NeedsVerification)
            {
                label = "Unverified";
                color = Warn;
            }
            else if (flags.FullAccessGranted)
            {
                label = "Approved";
                color = Green;
            }
            else
            {
                label = "Restricted";
                color = Warn;
            }
        }

        private string GetSourceLabel()
        {
            if (_useFakeForTesting.boolValue) return "Simulation";
            if (_mockConfig.objectReferenceValue != null) return "Mock config";
            return Application.isPlaying ? "API" : "Cached";
        }

        /// <summary>
        /// Returns the age range string from the active input source, or null if unavailable.
        /// </summary>
        private string GetInputAgeRange()
        {
            if (_useFakeForTesting.boolValue)
            {
                var status = (AgeVerificationStatus)_fakeStatus.enumValueIndex;
                int age = _fakeAge.intValue;
                return status switch
                {
                    AgeVerificationStatus.Verified => "Age: 18–150",
                    AgeVerificationStatus.Supervised or
                    AgeVerificationStatus.SupervisedApprovalPending or
                    AgeVerificationStatus.SupervisedApprovalDenied =>
                        $"Age: {Mathf.Max(0, age - 2)}–{age + 2}",
                    _ => "Age: N/A"
                };
            }

            if (_mockConfig.objectReferenceValue is AgeSignalsMockConfig mc && !mc.SimulateError)
            {
                if (mc.AgeLower < 0) return "Age: N/A";
                return $"Age: {mc.AgeLower}–{mc.AgeUpper}";
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════════════
        // Quick Start
        // ══════════════════════════════════════════════════════════════════

        private void DrawQuickStart()
        {
            _foldQuickStart = BeginCard("Quick Start", _foldQuickStart);
            if (_foldQuickStart)
            {
                DrawStep("1", "Setup", "Assign a Decision Logic asset (or use defaults).");
                DrawStep("2", "Play", "Enter Play Mode in the Unity Editor.");
                DrawStep("3", "Check", "Click \"Run Age Check\" to test restrictions.");
                EndCardContent();
            }
            EndCard();
        }

        private void DrawStep(string num, string title, string desc)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            var numRect = new Rect(rect.x, rect.y, 20, rect.height);
            var textRect = new Rect(rect.x + 24, rect.y, rect.width - 24, rect.height);

            // Number circle
            var prevC = GUI.color;
            GUI.color = Accent;
            EditorGUI.LabelField(numRect, num, new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                normal = { textColor = Accent }
            });
            GUI.color = prevC;

            EditorGUI.LabelField(textRect, $"<b>{title}</b>  {desc}",
                new GUIStyle(EditorStyles.label) { richText = true, fontSize = 11 });
        }

        // ══════════════════════════════════════════════════════════════════
        // Setup Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawSetupCard()
        {
            _foldSetup = BeginCard("Setup", _foldSetup);
            if (_foldSetup)
            {
                // Decision Logic
                DrawObjectFieldWithIcon(_decisionLogic, "Decision Logic",
                    "Custom age thresholds. Leave empty for defaults (18+ / 16+ / 13+).",
                    "AgeSignalsDecisionLogic", typeof(AgeSignalsDecisionLogic));

                EditorGUILayout.Space(2);

                // Mock Config
                DrawObjectFieldWithIcon(_mockConfig, "Mock Config",
                    "Editor-only mock responses for Play Mode testing.",
                    "AgeSignalsMockConfig", typeof(AgeSignalsMockConfig));

                EditorGUILayout.Space(4);

                // Validation
                DrawValidation();

                EndCardContent();
            }
            EndCard();
        }

        private void DrawValidation()
        {
            bool hasLogic = _decisionLogic.objectReferenceValue != null;
            bool hasMock = _mockConfig.objectReferenceValue != null;

            if (!hasLogic && !hasMock)
            {
                var rect = EditorGUILayout.GetControlRect(false, 18);
                var prevC = GUI.color;
                GUI.color = Muted;
                EditorGUI.LabelField(rect, "ℹ  Using default thresholds. No mock config assigned.", _mutedMini);
                GUI.color = prevC;
            }
            else if (hasLogic)
            {
                var logic = _decisionLogic.objectReferenceValue as AgeSignalsDecisionLogic;
                if (logic != null && (logic.Features == null || logic.Features.Count == 0))
                {
                    var rect = EditorGUILayout.GetControlRect(false, 18);
                    var prevC = GUI.color;
                    GUI.color = Warn;
                    EditorGUI.LabelField(rect, "⚠  Decision Logic has no features defined.", new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = Warn },
                        fontStyle = FontStyle.Bold
                    });
                    GUI.color = prevC;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Simulation Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawSimulationCard()
        {
            _foldSim = BeginCard("Simulation", _foldSim);
            if (_foldSim)
            {
                EditorGUILayout.PropertyField(_useFakeForTesting,
                    new GUIContent("Enable Simulation",
                        "Uses Google's FakeAgeSignalsManager on-device (debug builds)."));

                if (_useFakeForTesting.boolValue)
                {
                    EditorGUILayout.Space(4);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(_fakeStatus,
                        new GUIContent("Status", "Verification status to simulate."));

                    var status = (AgeVerificationStatus)_fakeStatus.enumValueIndex;
                    bool isSupervised = status == AgeVerificationStatus.Supervised ||
                                       status == AgeVerificationStatus.SupervisedApprovalPending ||
                                       status == AgeVerificationStatus.SupervisedApprovalDenied;

                    if (isSupervised && _fakeAge != null)
                    {
                        EditorGUILayout.IntSlider(_fakeAge, 5, 25,
                            new GUIContent("Age", "Supervised user age. API reports ±2 year bucket."));
                        int age = _fakeAge.intValue;
                        DrawNote($"Age range: {Mathf.Max(0, age - 2)} – {age + 2}");
                    }
                    else if (status == AgeVerificationStatus.Verified)
                        DrawNote("Verified = confirmed 18+ adult. Range: 18 – 150");
                    else
                        DrawNote("No age data for this status.");

                    EditorGUI.indentLevel--;
                }
                else
                {
                    DrawNote("Debug builds only. Enable to simulate age signals on-device.");
                }

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Run Check Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawRunCheckCard(AgeSignalsController ctrl, AgeRestrictionFlags flags)
        {
            EditorGUILayout.Space(CardSpacing);
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBackground(outer);

            // Inner padding
            EditorGUILayout.Space(CardPad - 4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad);

            // Button
            bool canRun = Application.isPlaying && !ctrl.IsChecking;
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = canRun ? Accent : new Color(0.35f, 0.35f, 0.35f);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                fixedHeight = 28,
                padding = new RectOffset(16, 16, 4, 4),
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };

            using (new EditorGUI.DisabledGroupScope(!canRun))
            {
                if (GUILayout.Button("▶  Run Age Check", btnStyle, GUILayout.Width(160)))
                    ctrl.CheckAgeSignals();
            }
            GUI.backgroundColor = prevBg;

            // Inline status
            GUILayout.Space(8);
            if (!Application.isPlaying)
            {
                var infoRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(infoRect, "Enter Play Mode to run checks.", _mutedMini);
            }
            else if (ctrl.IsChecking)
            {
                var infoRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(infoRect, "Checking…", new GUIStyle(_mutedMini) { normal = { textColor = Accent } });
            }

            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(CardPad - 4);
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════════
        // Results Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawResultsCard(AgeSignalsController ctrl, AgeRestrictionFlags flags)
        {
            EditorGUILayout.Space(CardSpacing);
            _foldResults = BeginCard("Results", _foldResults);
            if (_foldResults)
            {
                if (flags == null)
                {
                    DrawNote("No results yet. Run an age check to populate.");
                    EndCardContent();
                    EndCard();
                    return;
                }

                // ── Summary row: status pill + source pill ──
                var summaryRect = EditorGUILayout.GetControlRect(false, PillH + 4);
                float cx = summaryRect.x;

                // Status pill
                string statusLabel;
                Color statusColor;
                GetStatus(ctrl, flags, out statusLabel, out statusColor);
                float sw = _pillText.CalcSize(new GUIContent(statusLabel)).x + 14;
                DrawPill(new Rect(cx, summaryRect.y + 2, sw, PillH),
                    statusLabel, statusColor, new Color(statusColor.r, statusColor.g, statusColor.b, 0.15f));
                cx += sw + 6;

                // Input source pill
                string sourceLabel = GetSourceLabel();
                float srcW = _pillText.CalcSize(new GUIContent(sourceLabel)).x + 14;
                DrawPill(new Rect(cx, summaryRect.y + 2, srcW, PillH),
                    sourceLabel, Muted, new Color(0.5f, 0.5f, 0.5f, 0.12f));
                cx += srcW + 6;

                // Age range pill (from active input source)
                string ageRange = GetInputAgeRange();
                if (ageRange != null)
                {
                    float arW = _pillText.CalcSize(new GUIContent(ageRange)).x + 14;
                    DrawPill(new Rect(cx, summaryRect.y + 2, arW, PillH),
                        ageRange, Accent, AccentDim);
                }

                EditorGUILayout.Space(6);
                DrawSeparator();
                EditorGUILayout.Space(6);

                // ── Access section ──
                DrawSectionLabel("Access");
                DrawFeatureRow("Full access", flags.FullAccessGranted);
                if (flags.AccessDenied)
                    DrawFeatureRow("Access denied", true, isNegative: true);
                if (flags.NeedsVerification)
                    DrawFeatureRow("Needs verification", true, isWarning: true);

                EditorGUILayout.Space(6);

                // ── Features section (dynamic from DecisionLogic) ──
                DrawSectionLabel("Features");
                var features = GetActiveFeatures();
                foreach (var f in features)
                {
                    bool enabled = flags.IsFeatureEnabled(f.key);
                    string ageTag = f.requiresAdult ? "18+ adults only" : $"{f.minAge}+";
                    DrawFeatureRow(f.label, enabled, ageTag: ageTag);
                }

                EditorGUILayout.Space(6);

                // ── Ads section ──
                DrawSectionLabel("Ads");
                DrawFeatureRow("Personalized ads", flags.PersonalizedAdsEnabled, ageTag: "13+ COPPA");

                EndCardContent();
            }
            EndCard();
        }

        private void DrawSectionLabel(string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.LabelField(rect, text.ToUpper(), new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Muted },
                padding = new RectOffset(0, 0, 0, 0)
            });
        }

        private void DrawFeatureRow(string label, bool state,
            bool isNegative = false, bool isWarning = false, string ageTag = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, 22);

            // Left accent bar
            Color barColor;
            if (isWarning) barColor = Warn;
            else if (isNegative) barColor = state ? Red : Green;
            else barColor = state ? Green : new Color(0.4f, 0.4f, 0.4f);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3, 3, rect.height - 6), barColor);

            // Age tag pill (between label and badge)
            float ageTagW = 0f;
            if (!string.IsNullOrEmpty(ageTag))
                ageTagW = _pillText.CalcSize(new GUIContent(ageTag)).x + 10;

            float rightReserved = BadgeW + 4 + ageTagW + (ageTagW > 0 ? 4 : 0);

            // Label
            var labelStyle = state && !isNegative ? _featureLabelOn : _featureLabelOff;
            if (isWarning) labelStyle = new GUIStyle(labelStyle) { normal = { textColor = Warn } };
            if (isNegative && state) labelStyle = new GUIStyle(labelStyle) { normal = { textColor = Red } };
            var labelRect = new Rect(rect.x + 10, rect.y, rect.width - rightReserved - 12, rect.height);
            EditorGUI.LabelField(labelRect, label, labelStyle);

            // Age tag pill
            if (ageTagW > 0)
            {
                var ageRect = new Rect(rect.xMax - BadgeW - 4 - ageTagW - 2, rect.y + 4, ageTagW, rect.height - 8);
                EditorGUI.DrawRect(ageRect, AccentDim);
                _pillText.normal.textColor = Accent;
                EditorGUI.LabelField(ageRect, ageTag, _pillText);
            }

            // Badge
            string badgeText;
            Color badgeColor, badgeBg;
            if (isWarning)
            {
                badgeText = "WARN";
                badgeColor = Warn;
                badgeBg = WarnDim;
            }
            else if (isNegative)
            {
                badgeText = state ? "YES" : "NO";
                badgeColor = state ? Red : Green;
                badgeBg = state ? RedDim : GreenDim;
            }
            else
            {
                badgeText = state ? "ON" : "OFF";
                badgeColor = state ? Green : Muted;
                badgeBg = state ? GreenDim : new Color(0.4f, 0.4f, 0.4f, 0.15f);
            }

            var badgeRect = new Rect(rect.xMax - BadgeW - 2, rect.y + 3, BadgeW, rect.height - 6);
            EditorGUI.DrawRect(badgeRect, badgeBg);
            _badgeStyle.normal.textColor = badgeColor;
            EditorGUI.LabelField(badgeRect, badgeText, _badgeStyle);
        }

        // ══════════════════════════════════════════════════════════════════
        // Debug Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawDebugCard(AgeRestrictionFlags flags)
        {
            EditorGUILayout.Space(CardSpacing);
            _foldDebug = BeginCard("Debug", _foldDebug);
            if (_foldDebug)
            {
                if (flags != null)
                {
                    string json = JsonUtility.ToJson(flags, true);

                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.TextArea(json, _monoArea, GUILayout.MinHeight(80));
                    }

                    EditorGUILayout.Space(4);

                    // Action buttons row
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Copy JSON", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        EditorGUIUtility.systemCopyBuffer = json;
                        Debug.Log("[AgeSignals] JSON copied to clipboard.");
                    }

                    if (GUILayout.Button("Save to file…", EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        string path = EditorUtility.SaveFilePanel("Save Flags JSON", "", "age_signals_flags", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            System.IO.File.WriteAllText(path, json);
                            Debug.Log($"[AgeSignals] Flags saved to {path}");
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Clear cache", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        PlayerPrefs.DeleteKey(AgeSignalsController.FLAGS_PREFS_KEY);
                        PlayerPrefs.Save();
                        Debug.Log("[AgeSignals] Cached flags cleared from PlayerPrefs.");
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    DrawNote("No data available. Run an age check first.");
                }

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Card primitives
        // ══════════════════════════════════════════════════════════════════

        private bool BeginCard(string title, bool foldout)
        {
            EditorGUILayout.Space(CardSpacing);
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBackground(outer);

            // Foldout header inside card
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad - 6);

            foldout = EditorGUILayout.Foldout(foldout, "  " + title, true, new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                fixedHeight = 22
            });

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUILayout.Space(2);
                // Begin inner padded area
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(CardPad);
                EditorGUILayout.BeginVertical();
            }

            return foldout;
        }

        private static void EndCardContent()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
        }

        private static void EndCard()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCardBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            // Main background
            EditorGUI.DrawRect(rect, CardBg);

            // Subtle border (top/bottom/left/right 1px)
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), CardBorder);
        }

        // ══════════════════════════════════════════════════════════════════
        // Object field + create icon button
        // ══════════════════════════════════════════════════════════════════

        private void DrawObjectFieldWithIcon(
            SerializedProperty prop, string label, string tooltip,
            string assetName, System.Type assetType)
        {
            if (prop == null) return;

            const float iconBtnW = 24f;
            const float gap = 2f;
            bool showCreate = prop.objectReferenceValue == null;

            var totalRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            var fieldRect = totalRect;
            if (showCreate)
                fieldRect.width -= iconBtnW + gap;

            EditorGUI.ObjectField(fieldRect, prop, new GUIContent(label, tooltip));

            if (showCreate)
            {
                var btnRect = new Rect(fieldRect.xMax + gap, totalRect.y, iconBtnW, totalRect.height);
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = Accent;

                // "+" icon button
                if (GUI.Button(btnRect, new GUIContent("+", $"Create new {assetName} asset"), EditorStyles.miniButton))
                {
                    string path = EditorUtility.SaveFilePanelInProject(
                        $"Create {assetName}", assetName, "asset",
                        $"Choose where to save the {assetName} asset.");

                    if (!string.IsNullOrEmpty(path))
                    {
                        var asset = ScriptableObject.CreateInstance(assetType);
                        AssetDatabase.CreateAsset(asset, path);
                        AssetDatabase.SaveAssets();
                        prop.objectReferenceValue = asset;
                        serializedObject.ApplyModifiedProperties();
                        EditorGUIUtility.PingObject(asset);
                    }
                }
                GUI.backgroundColor = prevBg;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Feature list resolution
        // ══════════════════════════════════════════════════════════════════

        private static readonly List<AgeFeature> DefaultFeatures = new()
        {
            new AgeFeature { key = AgeFeatureKeys.Gambling, label = "Gambling / Casino", minAge = 18, requiresAdult = true },
            new AgeFeature { key = AgeFeatureKeys.Marketplace, label = "Trading / Marketplace", minAge = 16, requiresAdult = false },
            new AgeFeature { key = AgeFeatureKeys.Chat, label = "Chat / Social", minAge = 13, requiresAdult = false }
        };

        private IReadOnlyList<AgeFeature> GetActiveFeatures()
        {
            if (_decisionLogic.objectReferenceValue is AgeSignalsDecisionLogic logic &&
                logic.Features != null && logic.Features.Count > 0)
                return logic.Features;
            return DefaultFeatures;
        }

        // ══════════════════════════════════════════════════════════════════
        // Drawing utilities
        // ══════════════════════════════════════════════════════════════════

        private void DrawPill(Rect rect, string text, Color textColor, Color bgColor)
        {
            EditorGUI.DrawRect(rect, bgColor);
            _pillText.normal.textColor = textColor;
            EditorGUI.LabelField(rect, text, _pillText);
        }

        private void DrawNote(string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.LabelField(rect, text, _mutedMini);
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, SepColor);
        }
    }
}
#endif
