// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Editor
{
    [CustomEditor(typeof(AgeSignalsDecisionLogic))]
    public class AgeSignalsDecisionLogicEditor : UnityEditor.Editor
    {
        // ── Palette ──
        private static readonly Color AccentDim = new(0.35f, 0.61f, 1f, 0.08f);
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color BorderColor = new(0.35f, 0.61f, 1f, 0.25f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color HeaderBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color Warn = new(1f, 0.82f, 0.22f);

        public override void OnInspectorGUI()
        {
            var logic = (AgeSignalsDecisionLogic)target;

            // ═══════════════════════════════════════════
            // Default Inspector (features list, ads threshold)
            // ═══════════════════════════════════════════
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            // ═══════════════════════════════════════════
            // Feature Summary Table
            // ═══════════════════════════════════════════
            DrawFeatureSummary(logic);

            EditorGUILayout.Space(8);

            // ═══════════════════════════════════════════
            // Info Card (help / explanation)
            // ═══════════════════════════════════════════
            DrawInfoCard();
        }

        // ─────────────────────────────────────────────
        // Info card — explains what this asset does
        // ─────────────────────────────────────────────

        private static void DrawInfoCard()
        {
            var outer = EditorGUILayout.BeginVertical();

            // Background + border
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outer, AccentDim);
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, 3, outer.height), Accent);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();

            // Title
            EditorGUILayout.LabelField("What is this?", EditorStyles.boldLabel);

            GUILayout.Space(2);

            // Explanation
            EditorGUILayout.LabelField(
                "This asset controls which game features are available based on the player's age.\n\n" +
                "Each feature has a minimum age. When the Google Play Age Signals API reports\n" +
                "a player's age range, this logic decides what to enable or disable.\n\n" +
                "Assign this asset to the Age Signals Controller on your GameObject.",
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                });

            GUILayout.Space(6);

            // How-to bullets
            EditorGUILayout.LabelField("How it works:", EditorStyles.boldLabel);
            DrawBullet("Add features below with a unique key, label, and minimum age.");
            DrawBullet("Toggle \"Requires Adult\" for features that need verified 18+ status.");
            DrawBullet("The Ads Threshold sets the minimum age for personalized advertising (COPPA).");
            DrawBullet("At runtime, each feature is automatically enabled or disabled per player.");

            GUILayout.Space(8);

            // Code example
            EditorGUILayout.LabelField("Usage in code:", EditorStyles.boldLabel);
            GUILayout.Space(2);

            var codeStyle = new GUIStyle(EditorStyles.label)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                fontSize = 11,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.75f, 0.85f, 0.65f) },
                padding = new RectOffset(8, 8, 6, 6)
            };
            var codeBg = new Color(0.12f, 0.12f, 0.12f, 0.8f);

            string code =
                "<color=#569cd6>var</color> flags = controller.CurrentFlags;\n" +
                "\n" +
                "<color=#608b4e>// Check a feature by its key</color>\n" +
                "<color=#569cd6>if</color> (flags.<color=#dcdcaa>IsFeatureEnabled</color>(<color=#ce9178>\"gambling\"</color>))\n" +
                "    EnableCasinoUI();\n" +
                "\n" +
                "<color=#569cd6>if</color> (flags.<color=#dcdcaa>IsFeatureEnabled</color>(<color=#ce9178>\"marketplace\"</color>))\n" +
                "    EnableTrading();\n" +
                "\n" +
                "<color=#569cd6>if</color> (flags.<color=#dcdcaa>IsFeatureEnabled</color>(<color=#ce9178>\"chat\"</color>))\n" +
                "    EnableSocialFeatures();";

            var codeRect = EditorGUILayout.GetControlRect(false, 150);
            EditorGUI.DrawRect(codeRect, codeBg);
            EditorGUI.LabelField(codeRect, code, codeStyle);

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
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

        // ─────────────────────────────────────────────
        // Feature summary table
        // ─────────────────────────────────────────────

        private void DrawFeatureSummary(AgeSignalsDecisionLogic logic)
        {
            if (logic.Features == null || logic.Features.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No features defined. Add at least one feature to the list above.",
                    MessageType.Warning);
                return;
            }

            // Card background
            var outer = EditorGUILayout.BeginVertical();
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outer, HeaderBg);
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, outer.width, 1), SepColor);
            }

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Feature Summary", EditorStyles.boldLabel);
            GUILayout.Space(4);

            // Header row
            var headerRect = EditorGUILayout.GetControlRect(false, 16);
            float col1 = headerRect.x;
            float col2 = headerRect.x + headerRect.width * 0.4f;
            float col3 = headerRect.x + headerRect.width * 0.6f;
            float col4 = headerRect.x + headerRect.width * 0.8f;

            var headerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                normal = { textColor = Muted }
            };
            EditorGUI.LabelField(new Rect(col1, headerRect.y, col2 - col1, headerRect.height), "FEATURE", headerStyle);
            EditorGUI.LabelField(new Rect(col2, headerRect.y, col3 - col2, headerRect.height), "KEY", headerStyle);
            EditorGUI.LabelField(new Rect(col3, headerRect.y, col4 - col3, headerRect.height), "MIN AGE", headerStyle);
            EditorGUI.LabelField(new Rect(col4, headerRect.y, headerRect.xMax - col4, headerRect.height), "ADULT ONLY", headerStyle);

            // Separator
            var sepRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(sepRect, SepColor);

            // Feature rows
            foreach (var f in logic.Features)
            {
                var rowRect = EditorGUILayout.GetControlRect(false, 20);
                col1 = rowRect.x;
                col2 = rowRect.x + rowRect.width * 0.4f;
                col3 = rowRect.x + rowRect.width * 0.6f;
                col4 = rowRect.x + rowRect.width * 0.8f;

                // Validation: empty key
                bool hasKey = !string.IsNullOrEmpty(f.key);
                var labelStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };

                // Label
                string displayLabel = string.IsNullOrEmpty(f.label) ? "(no label)" : f.label;
                EditorGUI.LabelField(new Rect(col1, rowRect.y, col2 - col1 - 4, rowRect.height), displayLabel, labelStyle);

                // Key
                Color keyColor = hasKey ? new Color(0.75f, 0.75f, 0.75f) : Warn;
                var keyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = keyColor },
                    fontStyle = hasKey ? FontStyle.Normal : FontStyle.Italic
                };
                string keyText = hasKey ? f.key : "(empty!)";
                EditorGUI.LabelField(new Rect(col2, rowRect.y, col3 - col2 - 4, rowRect.height), keyText, keyStyle);

                // Min age pill
                string ageText = $"{f.minAge}+";
                Color ageColor = f.minAge >= 18 ? Red : f.minAge >= 16 ? Warn : Green;
                var ageStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 9,
                    normal = { textColor = ageColor }
                };
                var ageRect = new Rect(col3 + 4, rowRect.y + 3, 30, rowRect.height - 6);
                EditorGUI.DrawRect(ageRect, new Color(ageColor.r, ageColor.g, ageColor.b, 0.12f));
                EditorGUI.LabelField(ageRect, ageText, ageStyle);

                // Adult only indicator
                string adultText = f.requiresAdult ? "Yes" : "No";
                Color adultColor = f.requiresAdult ? Red : Muted;
                var adultStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = adultColor },
                    fontStyle = f.requiresAdult ? FontStyle.Bold : FontStyle.Normal
                };
                EditorGUI.LabelField(new Rect(col4, rowRect.y, rowRect.xMax - col4, rowRect.height), adultText, adultStyle);
            }

            // Ads row
            GUILayout.Space(2);
            var adsSepRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(adsSepRect, SepColor);

            var adsRect = EditorGUILayout.GetControlRect(false, 20);
            col1 = adsRect.x;
            col3 = adsRect.x + adsRect.width * 0.6f;
            EditorGUI.LabelField(new Rect(col1, adsRect.y, col3 - col1, adsRect.height),
                "Personalized Ads (COPPA)", new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = Muted },
                    fontStyle = FontStyle.Italic
                });

            var adsAgeRect = new Rect(col3 + 4, adsRect.y + 3, 30, adsRect.height - 6);
            EditorGUI.DrawRect(adsAgeRect, new Color(Green.r, Green.g, Green.b, 0.12f));
            EditorGUI.LabelField(adsAgeRect, $"{logic.PersonalizedAdsMinAge}+", new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                normal = { textColor = Green }
            });

            GUILayout.Space(8);
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
