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
    }
}
