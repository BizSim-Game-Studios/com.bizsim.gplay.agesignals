// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Pure C# static utility class containing all cache validation logic.
    /// Extracted from <see cref="AgeSignalsController"/> to enable unit testing
    /// without Play Mode or MonoBehaviour dependencies.
    ///
    /// <b>DI / Testing:</b> All methods are static and take explicit parameters —
    /// no hidden state, no Unity API calls, no singletons. Trivially testable
    /// with NUnit in Edit Mode.
    /// </summary>
    internal static class AgeSignalsCacheLogic
    {
        /// <summary>
        /// Determines whether cached restriction flags are still valid.
        /// Flags expire after <paramref name="maxAgeHours"/> to comply with
        /// Google's policy against long-term storage of age-signal-derived data.
        /// Additionally, if the decision logic configuration has changed (detected via
        /// <paramref name="currentConfigHash"/>), the cache is invalidated immediately
        /// to prevent stale restriction flags after an app update.
        /// </summary>
        /// <param name="flags">The cached flags to validate.</param>
        /// <param name="maxAgeHours">Maximum cache age in hours before expiration.</param>
        /// <param name="currentConfigHash">
        /// Hash of the current <see cref="AgeSignalsDecisionLogic"/> configuration.
        /// Pass <c>null</c> to skip config version checking (backward compatible).
        /// </param>
        /// <param name="currentSdkVersion">
        /// Current package version from <see cref="PackageVersion.Current"/>.
        /// Pass <c>null</c> to skip SDK version checking (backward compatible).
        /// </param>
        /// <returns>True if the cache is valid and can be used.</returns>
        public static bool IsCacheValid(AgeRestrictionFlags flags, int maxAgeHours,
            string currentConfigHash = null, string currentSdkVersion = null)
        {
            if (flags == null) return false;

            if (string.IsNullOrEmpty(flags.DecisionTimestamp))
                return false;

            // Config change detection: if a config hash is provided, compare it
            // against the hash stored in the cached flags. A mismatch means the
            // decision logic was modified (e.g., age thresholds changed in a new
            // app version) and the cached flags are stale.
            if (currentConfigHash != null && flags.ConfigHash != currentConfigHash)
                return false;

            // SDK version change detection: if the package was upgraded, the decision
            // logic code may have changed (e.g., a bug fix in threshold computation).
            // Invalidate the cache so flags are recomputed with the new code.
            if (currentSdkVersion != null && flags.SdkVersion != currentSdkVersion)
                return false;

            if (!DateTime.TryParse(flags.DecisionTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return false;

            double ageHours = (DateTime.UtcNow - dt).TotalHours;
            return ageHours <= maxAgeHours;
        }

        /// <summary>
        /// Returns the age (in hours) of cached flags, or -1 if the timestamp is invalid.
        /// Useful for logging and diagnostics.
        /// </summary>
        public static double GetCacheAgeHours(AgeRestrictionFlags flags)
        {
            if (flags == null || string.IsNullOrEmpty(flags.DecisionTimestamp))
                return -1;

            if (!DateTime.TryParse(flags.DecisionTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return -1;

            return (DateTime.UtcNow - dt).TotalHours;
        }

        /// <summary>
        /// Stamps cache metadata (decision timestamp and config hash) onto the given flags.
        /// Centralizes all metadata assignment so the controller only orchestrates,
        /// mirroring the pattern used by <see cref="InstallReferrerCacheLogic.CreateCachedData"/>.
        /// </summary>
        /// <param name="flags">The flags to stamp — modified in-place.</param>
        /// <param name="configHash">
        /// Hash from <see cref="AgeSignalsDecisionLogic.ComputeConfigHash"/>.
        /// </param>
        /// <param name="sdkVersion">
        /// Current package version from <see cref="PackageVersion.Current"/>.
        /// </param>
        public static void StampFlags(AgeRestrictionFlags flags, string configHash,
            string sdkVersion = null)
        {
            flags.DecisionTimestamp = DateTime.UtcNow.ToString("o");
            flags.ConfigHash = configHash;
            flags.SdkVersion = sdkVersion;
        }

        /// <summary>
        /// Creates restrictive "fail-safe" default flags used as fallback when
        /// no valid cache exists and the API has not yet responded.
        ///
        /// <b>COPPA / Google Play Family Policy:</b> Defaults MUST be restrictive.
        /// If the device is offline or the API fails, we must not grant adult-level
        /// access to a potentially underage user. Age-gated features (gambling,
        /// marketplace) and personalized ads remain disabled until the API explicitly
        /// confirms the user's age. Basic features (chat) are allowed at the 13+
        /// COPPA threshold — the API will restrict further if the user is under 13.
        /// </summary>
        public static AgeRestrictionFlags CreateDefaultFlags()
        {
            var flags = new AgeRestrictionFlags
            {
                FullAccessGranted = false,
                AccessDenied = false,           // Not denied — just not yet verified
                PersonalizedAdsEnabled = false,  // COPPA: no personalized ads until verified
                NeedsVerification = true,        // Prompt user to verify age
                DecisionTimestamp = DateTime.UtcNow.ToString("o")
            };

            // Age-gated features default to OFF (fail-safe)
            flags.SetFeature(AgeFeatureKeys.Gambling, false);    // 18+ only
            flags.SetFeature(AgeFeatureKeys.Marketplace, false); // 16+ only
            flags.SetFeature(AgeFeatureKeys.Chat, false);        // 13+ — restricted until verified

            return flags;
        }
    }
}
