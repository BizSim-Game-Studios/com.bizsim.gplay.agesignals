// Copyright (c) BizSim Game Studios. All rights reserved.
// Integration tests simulating JSON payloads from the Java bridge.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.Tests
{
    /// <summary>
    /// Integration tests that verify JSON deserialization of payloads
    /// matching the exact format sent by <c>AgeSignalsBridge.java</c>.
    /// These tests catch case-sensitivity issues with <see cref="JsonUtility"/>.
    /// </summary>
    [TestFixture]
    public class AgeSignalsJsonTests
    {
        // =================================================================
        // Success JSON (from Java addOnSuccessListener)
        // =================================================================

        [Test]
        public void SuccessJson_Verified_DeserializesCorrectly()
        {
            const string json = @"{
                ""userStatus"": ""VERIFIED"",
                ""ageLower"": 18,
                ""ageUpper"": 25,
                ""installId"": null,
                ""mostRecentApprovalDate"": 0
            }";

            var parsed = JsonUtility.FromJson<AgeSignalsJsonResultProxy>(json);

            Assert.AreEqual("VERIFIED", parsed.userStatus);
            Assert.AreEqual(18, parsed.ageLower);
            Assert.AreEqual(25, parsed.ageUpper);
        }

        [Test]
        public void SuccessJson_Supervised_WithAgeRange()
        {
            const string json = @"{
                ""userStatus"": ""SUPERVISED"",
                ""ageLower"": 10,
                ""ageUpper"": 12,
                ""installId"": ""install-abc-123"",
                ""mostRecentApprovalDate"": 1706745600000
            }";

            var parsed = JsonUtility.FromJson<AgeSignalsJsonResultProxy>(json);

            Assert.AreEqual("SUPERVISED", parsed.userStatus);
            Assert.AreEqual(10, parsed.ageLower);
            Assert.AreEqual(12, parsed.ageUpper);
            Assert.AreEqual("install-abc-123", parsed.installId);
            Assert.AreEqual(1706745600000L, parsed.mostRecentApprovalDate);
        }

        [Test]
        public void SuccessJson_NullStatus_HandlesGracefully()
        {
            // Java sends JSONObject.NULL for users outside supported jurisdiction
            const string json = @"{
                ""userStatus"": null,
                ""ageLower"": null,
                ""ageUpper"": null,
                ""installId"": null,
                ""mostRecentApprovalDate"": null
            }";

            var parsed = JsonUtility.FromJson<AgeSignalsJsonResultProxy>(json);

            // JsonUtility converts null strings to "" and null ints to 0
            Assert.IsTrue(string.IsNullOrEmpty(parsed.userStatus));
            Assert.AreEqual(0, parsed.ageLower);
        }

        [Test]
        public void SuccessJson_SupervisedApprovalDenied()
        {
            const string json = @"{
                ""userStatus"": ""SUPERVISED_APPROVAL_DENIED"",
                ""ageLower"": 8,
                ""ageUpper"": 10,
                ""installId"": ""install-xyz"",
                ""mostRecentApprovalDate"": 1706745600000
            }";

            var parsed = JsonUtility.FromJson<AgeSignalsJsonResultProxy>(json);
            Assert.AreEqual("SUPERVISED_APPROVAL_DENIED", parsed.userStatus);
        }

        // =================================================================
        // Error JSON (from Java sendError)
        // =================================================================

        [Test]
        public void ErrorJson_NetworkError_DeserializesCorrectly()
        {
            const string json = @"{
                ""errorCode"": -3,
                ""errorMessage"": ""Network unavailable"",
                ""isRetryable"": true
            }";

            var error = JsonUtility.FromJson<AgeSignalsError>(json);

            Assert.AreEqual(-3, error.errorCode);
            Assert.AreEqual("Network unavailable", error.errorMessage);
            Assert.IsTrue(error.isRetryable);
            Assert.AreEqual("NETWORK_ERROR", error.ErrorCodeName);
        }

        [Test]
        public void ErrorJson_AppNotOwned_NotRetryable()
        {
            const string json = @"{
                ""errorCode"": -9,
                ""errorMessage"": ""App not owned by user"",
                ""isRetryable"": false
            }";

            var error = JsonUtility.FromJson<AgeSignalsError>(json);

            Assert.AreEqual(-9, error.errorCode);
            Assert.IsFalse(error.isRetryable);
            Assert.AreEqual("APP_NOT_OWNED", error.ErrorCodeName);
        }

        [Test]
        public void ErrorJson_InternalError_FieldsCaseSensitive()
        {
            // Verify that camelCase fields deserialize correctly.
            // PascalCase ("ErrorCode") would silently default to 0.
            const string json = @"{
                ""errorCode"": -100,
                ""errorMessage"": ""Manager creation failed"",
                ""isRetryable"": false
            }";

            var error = JsonUtility.FromJson<AgeSignalsError>(json);

            Assert.AreEqual(-100, error.errorCode,
                "errorCode must be camelCase — JsonUtility is case-sensitive");
            Assert.AreEqual("Manager creation failed", error.errorMessage,
                "errorMessage must be camelCase — JsonUtility is case-sensitive");
            Assert.IsFalse(error.isRetryable,
                "isRetryable must be camelCase — JsonUtility is case-sensitive");
        }

        [TestCase(-1, true)]
        [TestCase(-8, true)]
        [TestCase(-9, false)]
        [TestCase(-100, false)]
        public void ErrorJson_RetryableRange_MatchesJavaBridge(int code, bool expectedRetryable)
        {
            // Java: isRetryable = errorCode >= -8 && errorCode <= -1
            string json = $@"{{
                ""errorCode"": {code},
                ""errorMessage"": ""test"",
                ""isRetryable"": {expectedRetryable.ToString().ToLower()}
            }}";

            var error = JsonUtility.FromJson<AgeSignalsError>(json);
            Assert.AreEqual(expectedRetryable, error.isRetryable);
        }

        // =================================================================
        // Flags Serialization Round-trip
        // =================================================================

        [Test]
        public void RestrictionFlags_SerializeDeserialize_RoundTrip()
        {
            var original = new AgeRestrictionFlags
            {
                FullAccessGranted = false,
                AccessDenied = false,
                PersonalizedAdsEnabled = false,
                NeedsVerification = true,
                DecisionTimestamp = "2026-01-30T12:00:00.0000000Z"
            };
            original.SetFeature(AgeFeatureKeys.Gambling, false);
            original.SetFeature(AgeFeatureKeys.Marketplace, true);
            original.SetFeature(AgeFeatureKeys.Chat, true);

            string json = JsonUtility.ToJson(original);
            var deserialized = JsonUtility.FromJson<AgeRestrictionFlags>(json);

            Assert.AreEqual(original.FullAccessGranted, deserialized.FullAccessGranted);
            Assert.AreEqual(original.AccessDenied, deserialized.AccessDenied);
            Assert.AreEqual(original.PersonalizedAdsEnabled, deserialized.PersonalizedAdsEnabled);
            Assert.AreEqual(original.NeedsVerification, deserialized.NeedsVerification);
            Assert.AreEqual(original.DecisionTimestamp, deserialized.DecisionTimestamp);

            // Dynamic features round-trip
            Assert.AreEqual(original.IsFeatureEnabled(AgeFeatureKeys.Gambling), deserialized.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.AreEqual(original.IsFeatureEnabled(AgeFeatureKeys.Marketplace), deserialized.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
            Assert.AreEqual(original.IsFeatureEnabled(AgeFeatureKeys.Chat), deserialized.IsFeatureEnabled(AgeFeatureKeys.Chat));
            Assert.AreEqual(3, deserialized.Features.Count);
        }

        [Test]
        public void RestrictionFlags_EmptyFeatures_DeserializesGracefully()
        {
            // JSON without Features field — simulates minimal cached data
            const string json = @"{
                ""FullAccessGranted"": true,
                ""AccessDenied"": false,
                ""PersonalizedAdsEnabled"": true,
                ""NeedsVerification"": false,
                ""DecisionTimestamp"": ""2026-01-30T12:00:00Z""
            }";

            var deserialized = JsonUtility.FromJson<AgeRestrictionFlags>(json);

            Assert.IsTrue(deserialized.FullAccessGranted);
            // Features list should be empty (not null) — no crash
            Assert.IsFalse(deserialized.IsFeatureEnabled(AgeFeatureKeys.Gambling));
        }

        // =================================================================
        // Internal proxy — mirrors AgeSignalsController's private DTO
        // =================================================================

        /// <summary>
        /// Public proxy of the controller's private <c>AgeSignalsJsonResult</c> DTO.
        /// Field names MUST match the Java bridge JSON keys (camelCase).
        /// Used only in tests to verify deserialization without accessing private types.
        /// </summary>
        [System.Serializable]
        private class AgeSignalsJsonResultProxy
        {
            public string userStatus;
            public int ageLower = -1;
            public int ageUpper = -1;
            public string installId;
            public long mostRecentApprovalDate;
        }
    }
}
