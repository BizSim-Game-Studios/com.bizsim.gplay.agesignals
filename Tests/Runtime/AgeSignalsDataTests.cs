// Copyright (c) BizSim Game Studios. All rights reserved.
// Tests for AgeSignalsData models and decision logic.

using System;
using NUnit.Framework;

namespace BizSim.GPlay.AgeSignals.Tests
{
    /// <summary>
    /// Unit tests for <see cref="AgeSignalsResult"/> properties and helper methods.
    /// </summary>
    [TestFixture]
    public class AgeSignalsResultTests
    {
        [Test]
        public void Verified_IsAdult_ReturnsTrue()
        {
            var result = new AgeSignalsResult { UserStatus = AgeVerificationStatus.Verified };
            Assert.IsTrue(result.IsAdult);
            Assert.IsTrue(result.HasAgeData);
            Assert.IsFalse(result.IsSupervised);
        }

        [Test]
        public void NotApplicable_HasAgeData_ReturnsFalse()
        {
            var result = new AgeSignalsResult { UserStatus = AgeVerificationStatus.NotApplicable };
            Assert.IsFalse(result.HasAgeData);
            Assert.IsFalse(result.IsAdult);
        }

        [TestCase(AgeVerificationStatus.Supervised)]
        [TestCase(AgeVerificationStatus.SupervisedApprovalPending)]
        [TestCase(AgeVerificationStatus.SupervisedApprovalDenied)]
        public void SupervisedStatuses_IsSupervised_ReturnsTrue(AgeVerificationStatus status)
        {
            var result = new AgeSignalsResult { UserStatus = status };
            Assert.IsTrue(result.IsSupervised);
        }

        [Test]
        public void SupervisedApprovalDenied_IsAccessDenied_ReturnsTrue()
        {
            var result = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.SupervisedApprovalDenied
            };
            Assert.IsTrue(result.IsAccessDenied);
        }

        [TestCase(10, 12, 13, true)]   // age range 10-12, checking IsUnder(13) → true
        [TestCase(10, 14, 13, false)]   // age range 10-14, checking IsUnder(13) → false (upper >= 13)
        [TestCase(-1, -1, 13, false)]   // no age data → false
        [TestCase(15, 17, 18, true)]    // age range 15-17, checking IsUnder(18) → true
        [TestCase(18, 25, 18, false)]   // age range 18-25, checking IsUnder(18) → false
        public void IsUnder_VariousRanges_ReturnsExpected(
            int lower, int upper, int threshold, bool expected)
        {
            var result = new AgeSignalsResult { AgeLower = lower, AgeUpper = upper };
            Assert.AreEqual(expected, result.IsUnder(threshold));
        }
    }

    /// <summary>
    /// Unit tests for <see cref="AgeSignalsError"/> code name mapping.
    /// </summary>
    [TestFixture]
    public class AgeSignalsErrorTests
    {
        [TestCase(-1, "API_NOT_AVAILABLE")]
        [TestCase(-3, "NETWORK_ERROR")]
        [TestCase(-9, "APP_NOT_OWNED")]
        [TestCase(-100, "INTERNAL_ERROR")]
        [TestCase(-999, "UNKNOWN_-999")]
        public void ErrorCodeName_MapsCorrectly(int code, string expectedName)
        {
            var error = new AgeSignalsError { errorCode = code };
            Assert.AreEqual(expectedName, error.ErrorCodeName);
        }
    }

    /// <summary>
    /// Unit tests for <see cref="AgeRestrictionFlags"/> behavior.
    /// Validates that flag combinations are logically consistent.
    /// </summary>
    [TestFixture]
    public class AgeRestrictionFlagsTests
    {
        [Test]
        public void FullAccess_AllFeaturesEnabled()
        {
            var flags = new AgeRestrictionFlags
            {
                FullAccessGranted = true,
                PersonalizedAdsEnabled = true,
                NeedsVerification = false
            };
            flags.SetFeature(AgeFeatureKeys.Gambling, true);
            flags.SetFeature(AgeFeatureKeys.Marketplace, true);
            flags.SetFeature(AgeFeatureKeys.Chat, true);

            Assert.IsTrue(flags.FullAccessGranted);
            Assert.IsTrue(flags.PersonalizedAdsEnabled);
            Assert.IsFalse(flags.AccessDenied);
        }

        [Test]
        public void AccessDenied_NotFullAccess()
        {
            var flags = new AgeRestrictionFlags
            {
                AccessDenied = true,
                FullAccessGranted = false
            };

            Assert.IsTrue(flags.AccessDenied);
            Assert.IsFalse(flags.FullAccessGranted);
        }

        [Test]
        public void IsFeatureEnabled_ReturnsCorrectValue()
        {
            var flags = new AgeRestrictionFlags();
            flags.SetFeature(AgeFeatureKeys.Gambling, true);
            flags.SetFeature(AgeFeatureKeys.Chat, false);

            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.IsFalse(flags.IsFeatureEnabled("nonexistent"));
        }

        [Test]
        public void SetFeature_UpdatesExistingEntry()
        {
            var flags = new AgeRestrictionFlags();
            flags.SetFeature(AgeFeatureKeys.Gambling, true);
            Assert.IsTrue(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));

            flags.SetFeature(AgeFeatureKeys.Gambling, false);
            Assert.IsFalse(flags.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.AreEqual(1, flags.Features.Count); // no duplicate
        }

        [Test]
        public void DeprecatedFields_StillWork()
        {
#pragma warning disable CS0618
            var flags = new AgeRestrictionFlags { FeatureAEnabled = true };
            Assert.IsTrue(flags.FeatureAEnabled);
#pragma warning restore CS0618
        }
    }
}
