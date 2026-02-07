// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)

using NUnit.Framework;

namespace BizSim.GPlay.AgeSignals.EditorTests
{
    /// <summary>
    /// Editor-mode tests for Age Signals package tooling.
    /// </summary>
    public class AgeSignalsEditorTests
    {
        [Test]
        public void AgeFeatureKeys_Constants_AreNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(AgeFeatureKeys.Gambling));
            Assert.IsFalse(string.IsNullOrEmpty(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(string.IsNullOrEmpty(AgeFeatureKeys.Chat));
        }

        [Test]
        public void AgeFeatureKeys_Constants_AreLowercase()
        {
            Assert.AreEqual(AgeFeatureKeys.Gambling, AgeFeatureKeys.Gambling.ToLowerInvariant());
            Assert.AreEqual(AgeFeatureKeys.Marketplace, AgeFeatureKeys.Marketplace.ToLowerInvariant());
            Assert.AreEqual(AgeFeatureKeys.Chat, AgeFeatureKeys.Chat.ToLowerInvariant());
        }

        [Test]
        public void AgeRestrictionFlags_SetFeature_CreatesEntry()
        {
            var flags = new AgeRestrictionFlags();
            flags.SetFeature("test_key", true);

            Assert.IsTrue(flags.IsFeatureEnabled("test_key"));
        }

        [Test]
        public void AgeRestrictionFlags_SetFeature_UpdatesExisting()
        {
            var flags = new AgeRestrictionFlags();
            flags.SetFeature("test_key", true);
            flags.SetFeature("test_key", false);

            Assert.IsFalse(flags.IsFeatureEnabled("test_key"));
        }

        [Test]
        public void AgeRestrictionFlags_IsFeatureEnabled_ReturnsFalse_ForMissingKey()
        {
            var flags = new AgeRestrictionFlags();
            Assert.IsFalse(flags.IsFeatureEnabled("nonexistent"));
        }

        // --- Cache Config Hash Invalidation Tests ---

        [Test]
        public void IsCacheValid_MatchingConfigHash_ReturnsTrue()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                ConfigHash = "v1|ads=13|gambling:18:1|marketplace:16:0|chat:13:0"
            };
            Assert.IsTrue(AgeSignalsCacheLogic.IsCacheValid(
                flags, 24, "v1|ads=13|gambling:18:1|marketplace:16:0|chat:13:0"));
        }

        [Test]
        public void IsCacheValid_MismatchedConfigHash_ReturnsFalse()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                ConfigHash = "v1|ads=13|gambling:18:1|marketplace:16:0|chat:13:0"
            };
            // Simulate gambling threshold changed from 18 to 16
            Assert.IsFalse(AgeSignalsCacheLogic.IsCacheValid(
                flags, 24, "v1|ads=13|gambling:16:1|marketplace:16:0|chat:13:0"));
        }

        [Test]
        public void IsCacheValid_NullConfigHash_SkipsCheck_BackwardCompatible()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                ConfigHash = "v1|ads=13|gambling:18:1"
            };
            // null currentConfigHash = skip check (backward compatible)
            Assert.IsTrue(AgeSignalsCacheLogic.IsCacheValid(flags, 24, null));
        }

        [Test]
        public void IsCacheValid_OldCacheWithoutHash_InvalidatedByNewConfig()
        {
            // Simulates an old cached entry that was saved before ConfigHash was introduced
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                ConfigHash = null
            };
            // New config hash provided but cached flags have no hash → mismatch
            Assert.IsFalse(AgeSignalsCacheLogic.IsCacheValid(
                flags, 24, "v1|ads=13|gambling:18:1|marketplace:16:0|chat:13:0"));
        }

        // --- SDK Version Cache Invalidation Tests ---

        [Test]
        public void IsCacheValid_MatchingSdkVersion_ReturnsTrue()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                SdkVersion = "0.1.0"
            };
            Assert.IsTrue(AgeSignalsCacheLogic.IsCacheValid(flags, 24, null, "0.1.0"));
        }

        [Test]
        public void IsCacheValid_MismatchedSdkVersion_ReturnsFalse()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                SdkVersion = "0.1.0"
            };
            Assert.IsFalse(AgeSignalsCacheLogic.IsCacheValid(flags, 24, null, "0.2.0"));
        }

        [Test]
        public void IsCacheValid_NullSdkVersion_SkipsCheck_BackwardCompatible()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = System.DateTime.UtcNow.ToString("o"),
                SdkVersion = "0.1.0"
            };
            Assert.IsTrue(AgeSignalsCacheLogic.IsCacheValid(flags, 24, null, null));
        }

        // --- Default Flags (Fail-Safe) Tests ---

        [Test]
        public void CreateDefaultFlags_IsFailSafe_NotFullAccess()
        {
            var flags = AgeSignalsCacheLogic.CreateDefaultFlags();

            Assert.IsFalse(flags.FullAccessGranted,
                "Default must NOT grant full access (COPPA fail-safe)");
            Assert.IsFalse(flags.AccessDenied,
                "Default should not deny access — just restrict it");
            Assert.IsTrue(flags.NeedsVerification,
                "Default must prompt for age verification");
            Assert.IsFalse(flags.PersonalizedAdsEnabled,
                "Default must NOT enable personalized ads (COPPA)");
        }

        [Test]
        public void CreateDefaultFlags_AgeGatedFeatures_AreDisabled()
        {
            var flags = AgeSignalsCacheLogic.CreateDefaultFlags();

            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling),
                "Gambling must be disabled by default (18+ only)");
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace),
                "Marketplace must be disabled by default (16+ only)");
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat),
                "Chat must be disabled by default until age is verified");
        }
    }
}
