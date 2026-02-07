// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using UnityEngine;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Default cache provider that stores restriction flags as plain JSON
    /// in <c>PlayerPrefs</c>. Used when no custom provider is assigned.
    ///
    /// For encrypted storage, implement <see cref="IAgeSignalsCacheProvider"/>
    /// with AES-256 encryption (see the Install Referrer package's
    /// <c>EncryptedPlayerPrefsCacheProvider</c> for a reference implementation).
    /// </summary>
    public class PlayerPrefsCacheProvider : IAgeSignalsCacheProvider
    {
        private const string PREFS_KEY = "AgeRestriction_Flags";

        /// <inheritdoc />
        public AgeRestrictionFlags Load()
        {
            string json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonUtility.FromJson<AgeRestrictionFlags>(json);
            }
            catch
            {
                // Corrupt data — discard
                return null;
            }
        }

        /// <inheritdoc />
        public void Save(AgeRestrictionFlags flags)
        {
            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(flags));
            PlayerPrefs.Save();
        }

        /// <inheritdoc />
        public void Clear()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY);
            PlayerPrefs.Save();
        }
    }
}
