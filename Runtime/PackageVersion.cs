// Copyright (c) BizSim Game Studios. All rights reserved.
// IMPORTANT: Update this value manually when bumping the version in package.json.

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Package version constant used for cache invalidation.
    /// When the SDK version changes after an upgrade, cached restriction flags are
    /// invalidated to ensure they are recomputed with the new decision logic code.
    /// <para>
    /// <b>Maintenance:</b> Keep this value in sync with the <c>"version"</c> field
    /// in <c>package.json</c>. Update both files together when releasing a new version.
    /// </para>
    /// </summary>
    internal static class PackageVersion
    {
        /// <summary>Current package version — must match <c>package.json</c>.</summary>
        public const string Current = "0.1.5";
    }
}
