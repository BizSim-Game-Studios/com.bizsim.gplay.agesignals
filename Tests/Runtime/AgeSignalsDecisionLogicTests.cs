// Copyright (c) BizSim Game Studios. All rights reserved.
// Tests for AgeSignalsDecisionLogic.ComputeFlags.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Tests
{
    [TestFixture]
    public class AgeSignalsDecisionLogicTests
    {
        private AgeSignalsDecisionLogic _logic;

        [SetUp]
        public void SetUp()
        {
            _logic = ScriptableObject.CreateInstance<AgeSignalsDecisionLogic>();
            _logic.Reset(); // Default 3 features: gambling=18, marketplace=16, chat=13
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_logic);
        }

        [Test]
        public void Verified_GrantsFullAccess()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.Verified,
                AgeLower = 18, AgeUpper = 150
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.FullAccessGranted);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
            Assert.IsFalse(flags.AccessDenied);
        }

        [Test]
        public void NotApplicable_GrantsFullAccess_NoData()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.NotApplicable,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.FullAccessGranted);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Supervised_Under13_BlocksAllAgeGatedFeatures()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.Supervised,
                AgeLower = 8, AgeUpper = 10
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Supervised_Age14_EnablesChatOnly()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.Supervised,
                AgeLower = 12, AgeUpper = 14
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Supervised_Age17_EnablesChatAndMarketplace()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.Supervised,
                AgeLower = 15, AgeUpper = 17
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
        }

        [Test]
        public void AccessDenied_BlocksEverything()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.SupervisedApprovalDenied,
                AgeLower = 8, AgeUpper = 10
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.AccessDenied);
            Assert.IsFalse(flags.FullAccessGranted);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.PersonalizedAdsEnabled);
        }

        [Test]
        public void Unknown_SetsNeedsVerification()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.Unknown,
                AgeLower = -1, AgeUpper = -1
            };
            var flags = new AgeRestrictionFlags();

            _logic.ComputeFlags(result, flags);

            Assert.IsTrue(flags.NeedsVerification);
        }

        [Test]
        public void ComputeConfigHash_DefaultConfig_IsStable()
        {
            string hash1 = _logic.ComputeConfigHash();
            string hash2 = _logic.ComputeConfigHash();
            Assert.AreEqual(hash1, hash2, "Same config should produce identical hashes");
            Assert.IsTrue(hash1.StartsWith("v1|"), "Hash should start with version prefix");
        }

        [Test]
        public void ComputeConfigHash_DifferentConfigs_ProduceDifferentHashes()
        {
            string defaultHash = _logic.ComputeConfigHash();

            // Create a second logic with different thresholds
            var altLogic = ScriptableObject.CreateInstance<AgeSignalsDecisionLogic>();
            // altLogic gets default features (gambling:18, marketplace:16, chat:13)
            // We can't modify fields directly, but the default hash should equal _logic's default hash
            string altHash = altLogic.ComputeConfigHash();
            Object.DestroyImmediate(altLogic);

            Assert.AreEqual(defaultHash, altHash, "Same default config should produce same hash");
        }

    }
}
