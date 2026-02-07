// Copyright (c) BizSim Game Studios. All rights reserved.
// Integration tests for cache providers. These tests use PlayerPrefs
// and MUST clean up after themselves to avoid polluting the Editor.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals.EditorTests
{
    /// <summary>
    /// Integration tests for <see cref="PlayerPrefsCacheProvider"/> (plain JSON).
    /// Exercises the full Save → Load → Clear cycle using real PlayerPrefs.
    /// <para>
    /// <b>Important:</b> All tests clean up PlayerPrefs keys in <c>[TearDown]</c>
    /// to prevent cross-test contamination and Editor state pollution.
    /// </para>
    /// </summary>
    [TestFixture]
    public class PlainCacheProviderTests
    {
        private PlayerPrefsCacheProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new PlayerPrefsCacheProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Clear();
            PlayerPrefs.DeleteKey("AgeRestriction_Flags");
            PlayerPrefs.Save();
        }

        [Test]
        public void Load_WhenEmpty_ReturnsNull()
        {
            Assert.IsNull(_provider.Load());
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesData()
        {
            var original = new AgeRestrictionFlags
            {
                FullAccessGranted = true,
                PersonalizedAdsEnabled = true,
                NeedsVerification = false,
                DecisionTimestamp = "2026-01-30T12:00:00Z",
                ConfigHash = "v1|ads=13|gambling:18:1"
            };
            original.SetFeature(AgeFeatureKeys.Gambling, true);
            original.SetFeature(AgeFeatureKeys.Chat, false);

            _provider.Save(original);
            var loaded = _provider.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(original.FullAccessGranted, loaded.FullAccessGranted);
            Assert.AreEqual(original.PersonalizedAdsEnabled, loaded.PersonalizedAdsEnabled);
            Assert.AreEqual(original.DecisionTimestamp, loaded.DecisionTimestamp);
            Assert.AreEqual(original.ConfigHash, loaded.ConfigHash);
            Assert.IsTrue(loaded.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsFalse(loaded.IsFeatureEnabled(AgeFeatureKeys.Chat));
        }

        [Test]
        public void Clear_RemovesCachedFlags()
        {
            var flags = new AgeRestrictionFlags { FullAccessGranted = true };
            _provider.Save(flags);

            _provider.Clear();

            Assert.IsNull(_provider.Load());
        }

        [Test]
        public void Load_CorruptedJson_ReturnsNull()
        {
            PlayerPrefs.SetString("AgeRestriction_Flags", "{not valid json!!!");
            PlayerPrefs.Save();

            var result = _provider.Load();
            Assert.IsNull(result, "Corrupted JSON should return null (not throw)");
        }
    }

    /// <summary>
    /// Integration tests for <see cref="EncryptedPlayerPrefsCacheProvider"/> (AES-256-CBC).
    /// Exercises the full Save → Load → Clear cycle using real PlayerPrefs.
    /// <para>
    /// <b>Important:</b> All tests clean up PlayerPrefs keys in <c>[TearDown]</c>
    /// to prevent cross-test contamination and Editor state pollution.
    /// </para>
    /// </summary>
    [TestFixture]
    public class EncryptedCacheProviderTests
    {
        private const string TestSalt = "AgeSignals_UnitTest_Salt";

        private EncryptedPlayerPrefsCacheProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new EncryptedPlayerPrefsCacheProvider(TestSalt);
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Clear();
            PlayerPrefs.DeleteKey("AgeSignals_Cache_Enc");
            PlayerPrefs.DeleteKey("AgeSignals_KeyId");
            PlayerPrefs.Save();
        }

        [Test]
        public void Load_WhenEmpty_ReturnsNull()
        {
            Assert.IsNull(_provider.Load());
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesData()
        {
            var original = new AgeRestrictionFlags
            {
                FullAccessGranted = false,
                AccessDenied = false,
                PersonalizedAdsEnabled = true,
                NeedsVerification = true,
                DecisionTimestamp = "2026-01-30T12:00:00Z",
                ConfigHash = "v1|ads=13|gambling:18:1|marketplace:16:0|chat:13:0"
            };
            original.SetFeature(AgeFeatureKeys.Gambling, false);
            original.SetFeature(AgeFeatureKeys.Marketplace, true);
            original.SetFeature(AgeFeatureKeys.Chat, true);

            _provider.Save(original);
            var loaded = _provider.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(original.FullAccessGranted, loaded.FullAccessGranted);
            Assert.AreEqual(original.PersonalizedAdsEnabled, loaded.PersonalizedAdsEnabled);
            Assert.AreEqual(original.NeedsVerification, loaded.NeedsVerification);
            Assert.AreEqual(original.ConfigHash, loaded.ConfigHash);
            Assert.IsFalse(loaded.IsFeatureEnabled(AgeFeatureKeys.Gambling));
            Assert.IsTrue(loaded.IsFeatureEnabled(AgeFeatureKeys.Marketplace));
        }

        [Test]
        public void Clear_RemovesCachedFlags()
        {
            var flags = new AgeRestrictionFlags { FullAccessGranted = true };
            _provider.Save(flags);

            _provider.Clear();

            Assert.IsNull(_provider.Load());
        }

        [Test]
        public void Save_EncryptsData_NotPlaintext()
        {
            var flags = new AgeRestrictionFlags
            {
                DecisionTimestamp = "2026-01-30T12:00:00Z",
                ConfigHash = "SECRET_HASH_VALUE"
            };
            _provider.Save(flags);

            string raw = PlayerPrefs.GetString("AgeSignals_Cache_Enc", "");
            Assert.IsFalse(string.IsNullOrEmpty(raw));
            Assert.IsFalse(raw.Contains("SECRET_HASH_VALUE"),
                "Raw PlayerPrefs value must not contain plaintext data");
        }

        [Test]
        public void Load_CorruptedData_ReturnsNull()
        {
            PlayerPrefs.SetString("AgeSignals_Cache_Enc", "not-valid-base64!@#$");
            PlayerPrefs.Save();

            var result = _provider.Load();
            Assert.IsNull(result, "Corrupted data should return null (not throw)");
        }
    }
}
