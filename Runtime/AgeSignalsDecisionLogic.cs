// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System.Collections.Generic;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Pluggable decision logic for converting raw <see cref="AgeSignalsResult"/>
    /// into <see cref="AgeRestrictionFlags"/>.
    ///
    /// Features are configurable from the Inspector via a dynamic list.
    /// Subclass and override <see cref="ComputeFlags"/> for fully custom logic.
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Age Signals Decision Logic")]
    public class AgeSignalsDecisionLogic : ScriptableObject
    {
        [Header("Feature Definitions")]
        [Tooltip("List of age-gated features. Each defines a key, label, minimum age, and whether adult verification is required.")]
        [SerializeField] private List<AgeFeature> _features = new()
        {
            new AgeFeature { key = "gambling", label = "Gambling / Casino", minAge = 18, requiresAdult = true },
            new AgeFeature { key = "marketplace", label = "Trading / Marketplace", minAge = 16, requiresAdult = false },
            new AgeFeature { key = "chat", label = "Chat / Social", minAge = 13, requiresAdult = false }
        };

        [Header("Ads Threshold")]
        [Tooltip("Minimum age for personalized advertising (COPPA compliance = 13).")]
        [Range(5, 25)]
        [SerializeField] private int _personalizedAdsMinAge = 13;

        /// <summary>Read-only access to the configured features list (for Editor UI).</summary>
        public IReadOnlyList<AgeFeature> Features => _features;

        /// <summary>Configured threshold for personalized ads.</summary>
        public int PersonalizedAdsMinAge => _personalizedAdsMinAge;

        /// <summary>
        /// Resets features to the default set (gambling 18+, marketplace 16+, chat 13+).
        /// Useful for testing or factory reset scenarios.
        /// </summary>
        public void Reset()
        {
            _features = new List<AgeFeature>
            {
                new() { key = "gambling", label = "Gambling / Casino", minAge = 18, requiresAdult = true },
                new() { key = "marketplace", label = "Trading / Marketplace", minAge = 16, requiresAdult = false },
                new() { key = "chat", label = "Chat / Social", minAge = 13, requiresAdult = false }
            };
            _personalizedAdsMinAge = 13;
        }

        /// <summary>
        /// Populates <paramref name="flags"/> based on the raw <paramref name="result"/>
        /// using the configured feature list and age thresholds.
        /// Override in a subclass for fully custom logic.
        /// </summary>
        public virtual void ComputeFlags(AgeSignalsResult result, AgeRestrictionFlags flags)
        {
            bool noData = !result.HasAgeData;

            flags.AccessDenied = result.IsAccessDenied;

            // Access denied → block everything
            if (flags.AccessDenied)
            {
                flags.FullAccessGranted = false;
                flags.PersonalizedAdsEnabled = false;
                flags.NeedsVerification = false;
                // Block all dynamic features
                foreach (var feature in _features)
                    flags.SetFeature(feature.key, false);
                SyncDeprecatedFields(flags);
                return;
            }

            flags.FullAccessGranted = noData || result.IsAdult;

            // Dynamic feature evaluation
            foreach (var feature in _features)
            {
                bool enabled;
                if (feature.requiresAdult)
                    enabled = noData || result.IsAdult;
                else
                    enabled = noData || result.IsAdult || !result.IsUnder(feature.minAge);

                flags.SetFeature(feature.key, enabled);
            }

            flags.PersonalizedAdsEnabled = noData || !result.IsUnder(_personalizedAdsMinAge);
            flags.NeedsVerification = result.UserStatus == AgeVerificationStatus.Unknown;

            SyncDeprecatedFields(flags);
        }

        /// <summary>
        /// Syncs deprecated fields from dynamic Features list for backward compatibility.
        /// </summary>
        private static void SyncDeprecatedFields(AgeRestrictionFlags flags)
        {
#pragma warning disable CS0618 // Obsolete member access intentional for backward compat
            flags.FeatureAEnabled = flags.IsFeatureEnabled(AgeFeatureKeys.Gambling);
            flags.FeatureBFullAccess = flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace);
            flags.FeatureCEnabled = flags.IsFeatureEnabled(AgeFeatureKeys.Chat);
#pragma warning restore CS0618
        }

        private void OnValidate()
        {
            if (_features == null) return;
            var seen = new HashSet<string>();
            foreach (var f in _features)
            {
                if (f == null) continue;
                if (string.IsNullOrWhiteSpace(f.key))
                {
                    Debug.LogError($"[AgeSignals] Feature with label \"{f.label}\" has an empty key in {name}. Keys must not be empty.", this);
                    continue;
                }
                if (!seen.Add(f.key))
                    Debug.LogWarning($"[AgeSignals] Duplicate feature key \"{f.key}\" in {name}. Each key must be unique.", this);
            }
        }
    }
}
