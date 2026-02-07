// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEngine;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// ScriptableObject for configuring mock Age Signals responses in the Unity Editor.
    /// Create via <b>Assets → Create → BizSim → Age Signals Mock Config</b>.
    ///
    /// Age ranges are computed automatically based on status:
    /// <list type="bullet">
    /// <item><b>Verified</b> — 18+ adult (range: 18–150)</item>
    /// <item><b>Supervised*</b> — child/teen, ±2 year bucket around <see cref="MockAge"/></item>
    /// <item><b>Unknown / NotApplicable</b> — no age data (range: -1, -1)</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Age Signals/Age Signals Mock Config")]
    public class AgeSignalsMockConfig : ScriptableObject
    {
        [Header("Mock API Response")]
        [Tooltip("The verification status to simulate.")]
        public AgeVerificationStatus MockStatus = AgeVerificationStatus.NotApplicable;

        [Tooltip("Simulated age for supervised/unknown users (5–25). Ignored for Verified and NotApplicable.")]
        [Range(5, 25)]
        public int MockAge = 14;

        [Header("Error Simulation")]
        [Tooltip("When enabled, simulates an API error instead of a successful response.")]
        public bool SimulateError = false;

        [Tooltip("Error code to simulate. See AgeSignalsErrorCode enum for values.")]
        public int SimulatedErrorCode = (int)AgeSignalsErrorCode.NetworkError;

        /// <summary>Computed lower bound based on status and age.</summary>
        public int AgeLower
        {
            get
            {
                return MockStatus switch
                {
                    AgeVerificationStatus.Verified => 18,
                    AgeVerificationStatus.Supervised
                        or AgeVerificationStatus.SupervisedApprovalPending
                        or AgeVerificationStatus.SupervisedApprovalDenied => Mathf.Max(0, MockAge - 2),
                    _ => -1
                };
            }
        }

        /// <summary>Computed upper bound based on status and age.</summary>
        public int AgeUpper
        {
            get
            {
                return MockStatus switch
                {
                    AgeVerificationStatus.Verified => 150,
                    AgeVerificationStatus.Supervised
                        or AgeVerificationStatus.SupervisedApprovalPending
                        or AgeVerificationStatus.SupervisedApprovalDenied => MockAge + 2,
                    _ => -1
                };
            }
        }
    }
}
#endif
