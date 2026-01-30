// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

// This script creates mock config preset assets when run from the Unity Editor menu.
// It is included in the Samples~ folder and only available after importing the sample.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using BizSim.GPlay.AgeSignals;

namespace BizSim.GPlay.AgeSignals.Samples
{
    /// <summary>
    /// Editor utility to generate pre-configured <see cref="AgeSignalsMockConfig"/> assets
    /// for common testing scenarios. Run via <b>Assets → Create → BizSim → Age Signals Mock Presets</b>.
    /// </summary>
    public static class CreateMockPresets
    {
        [MenuItem("Assets/Create/BizSim/Age Signals Mock Presets")]
        public static void Create()
        {
            string folder = GetSelectedFolder();

            CreatePreset(folder, "MockPreset_VerifiedAdult", new PresetData
            {
                Status = AgeVerificationStatus.Verified
            });

            CreatePreset(folder, "MockPreset_SupervisedChild", new PresetData
            {
                Status = AgeVerificationStatus.Supervised,
                Age = 10
            });

            CreatePreset(folder, "MockPreset_SupervisedTeen", new PresetData
            {
                Status = AgeVerificationStatus.Supervised,
                Age = 15
            });

            CreatePreset(folder, "MockPreset_UnknownUser", new PresetData
            {
                Status = AgeVerificationStatus.Unknown
            });

            CreatePreset(folder, "MockPreset_AccessDenied", new PresetData
            {
                Status = AgeVerificationStatus.SupervisedApprovalDenied,
                Age = 9
            });

            CreatePreset(folder, "MockPreset_NetworkError", new PresetData
            {
                SimulateError = true, ErrorCode = -3
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AgeSignals] Created 6 mock config presets in " + folder);
        }

        private struct PresetData
        {
            public AgeVerificationStatus Status;
            public int Age;
            public bool SimulateError;
            public int ErrorCode;
        }

        private static void CreatePreset(string folder, string name, PresetData data)
        {
            var asset = ScriptableObject.CreateInstance<AgeSignalsMockConfig>();
            asset.MockStatus = data.Status;
            asset.MockAge = data.Age > 0 ? data.Age : 14;
            asset.SimulateError = data.SimulateError;
            if (data.SimulateError)
                asset.SimulatedErrorCode = data.ErrorCode;

            AssetDatabase.CreateAsset(asset, $"{folder}/{name}.asset");
        }

        private static string GetSelectedFolder()
        {
            string path = "Assets";
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!System.IO.Directory.Exists(path))
                    path = System.IO.Path.GetDirectoryName(path);
                break;
            }
            return path;
        }
    }
}
#endif
