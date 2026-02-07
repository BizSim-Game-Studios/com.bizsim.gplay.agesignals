// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Pluggable analytics adapter for logging age signals events.
    /// Implement this interface to integrate with your analytics provider
    /// (Firebase, Unity Analytics, Amplitude, etc.).
    ///
    /// <b>Privacy:</b> This adapter intentionally does NOT receive raw age data.
    /// Only binary success/error outcomes and restriction flag summaries are exposed.
    /// This complies with Google's policy against transmitting age-signal-derived data
    /// to third-party analytics services.
    ///
    /// <b>Usage:</b>
    /// <code>
    /// AgeSignalsController.Instance.SetAnalyticsAdapter(myAdapter);
    /// </code>
    /// </summary>
    public interface IAgeSignalsAnalyticsAdapter
    {
        /// <summary>
        /// Called when the age signals API call completes (success or failure).
        /// </summary>
        /// <param name="success">True if the API returned a valid result, false on error.</param>
        void LogApiCallResult(bool success);

        /// <summary>
        /// Called when restriction flags are updated after a successful API call.
        /// Contains only behavior flags — no raw age data.
        /// Default implementation is a no-op for backward compatibility.
        /// </summary>
        /// <param name="flags">The computed restriction flags.</param>
        void LogRestrictionsUpdated(AgeRestrictionFlags flags) { }

        /// <summary>
        /// Called when the API returns an error after all retries are exhausted.
        /// Default implementation is a no-op for backward compatibility.
        /// </summary>
        /// <param name="error">The error details.</param>
        void LogError(AgeSignalsError error) { }
    }
}
