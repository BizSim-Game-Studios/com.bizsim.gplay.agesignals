// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Pluggable cache provider for persisting age restriction flags.
    /// The default implementation uses <c>PlayerPrefs</c> (plain JSON).
    /// Implement this interface to use a custom storage backend
    /// (e.g., SQLite, file system, encrypted storage).
    ///
    /// <b>Usage:</b>
    /// <code>
    /// AgeSignalsController.Instance.SetCacheProvider(myProvider);
    /// </code>
    ///
    /// <b>Privacy note:</b> Only <see cref="AgeRestrictionFlags"/> (behavior decisions)
    /// are cached — never raw age data. This complies with Google's policy against
    /// long-term storage of age-signal-derived data.
    /// </summary>
    public interface IAgeSignalsCacheProvider
    {
        /// <summary>
        /// Loads cached restriction flags from storage.
        /// </summary>
        /// <returns>The cached flags, or null if no valid cache exists.</returns>
        AgeRestrictionFlags Load();

        /// <summary>
        /// Saves restriction flags to persistent storage.
        /// </summary>
        /// <param name="flags">The restriction flags to cache.</param>
        void Save(AgeRestrictionFlags flags);

        /// <summary>
        /// Clears the cached restriction flags from storage.
        /// </summary>
        void Clear();
    }
}
