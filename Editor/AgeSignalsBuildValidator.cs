// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Editor
{
    /// <summary>
    /// Pre-build validator that checks for missing EDM4U and duplicate Age Signals AAR files.
    /// Runs before every Android build to prevent runtime <c>ClassNotFoundException</c>
    /// when <c>com.google.android.play:age-signals</c> is not resolved.
    ///
    /// <b>What it checks:</b>
    /// <list type="bullet">
    /// <item>EDM4U (External Dependency Manager) is installed to resolve Dependencies.xml</item>
    /// <item>Multiple AAR files containing Age Signals classes (duplicate conflict prevention)</item>
    /// </list>
    /// </summary>
    public class AgeSignalsBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            CheckEdm4uPresence();
            CheckDuplicateAars();
        }

        private static void CheckEdm4uPresence()
        {
            // EDM4U's PlayServicesResolver lives in Google.JarResolver.dll.
            // Try both possible assembly names for compatibility across EDM4U versions.
            var edm4uType = System.Type.GetType(
                "GooglePlayServices.PlayServicesResolver, Google.JarResolver", false);

            if (edm4uType == null)
            {
                const string message =
                    "EDM4U (External Dependency Manager for Unity) is not installed.\n\n" +
                    "This package uses Editor/Dependencies.xml to resolve " +
                    "'com.google.android.play:age-signals:0.0.2'.\n\n" +
                    "Without EDM4U, the native library will be missing and " +
                    "you'll get ClassNotFoundException at runtime.";

                Debug.LogWarning("[AgeSignals Build Validator] " + message);

                bool openPage = EditorUtility.DisplayDialog(
                    "Age Signals — Missing EDM4U",
                    message,
                    "Download EDM4U",
                    "Ignore");

                if (openPage)
                {
                    Application.OpenURL("https://github.com/googlesamples/unity-jar-resolver");
                }
            }
        }

        private static void CheckDuplicateAars()
        {
            // Search for any AAR files that might contain the Age Signals classes
            var allAars = Directory.GetFiles(Application.dataPath, "*.aar", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(Application.dataPath, "..", "Packages"), "*.aar", SearchOption.AllDirectories))
                .Where(path => path.Replace('\\', '/').ToLower().Contains("age-signals")
                            || path.Replace('\\', '/').ToLower().Contains("agesignals"))
                .ToList();

            if (allAars.Count > 1)
            {
                string files = string.Join("\n  • ", allAars.Select(p => p.Replace('\\', '/')));
                Debug.LogWarning(
                    $"[AgeSignals Build Validator] Found {allAars.Count} potential Age Signals AAR files:\n  • {files}\n" +
                    "This may cause 'duplicate class' errors at build time. " +
                    "If you're using EDM4U (Dependencies.xml), remove any manually added AAR files.");
            }

            // Also check for manually added JAR files
            var jars = Directory.GetFiles(Application.dataPath, "*.jar", SearchOption.AllDirectories)
                .Where(path => path.Replace('\\', '/').ToLower().Contains("age-signals")
                            || path.Replace('\\', '/').ToLower().Contains("agesignals"))
                .ToList();

            if (jars.Count > 0)
            {
                string files = string.Join("\n  • ", jars.Select(p => p.Replace('\\', '/')));
                Debug.LogWarning(
                    $"[AgeSignals Build Validator] Found manually added Age Signals JAR files:\n  • {files}\n" +
                    "The Age Signals library is now resolved via EDM4U (Dependencies.xml). " +
                    "Remove these JAR files to avoid conflicts.");
            }
        }
    }
}
#endif
