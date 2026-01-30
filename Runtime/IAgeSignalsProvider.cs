// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Abstraction for the Age Signals service.
    ///
    /// Use this interface to decouple your game logic from the concrete
    /// <see cref="AgeSignalsController"/> singleton. This enables:
    /// <list type="bullet">
    /// <item>Unit testing with custom mock implementations</item>
    /// <item>Dependency injection via Zenject, VContainer, or any DI framework</item>
    /// <item>Swapping the real provider with a stub in automated test suites</item>
    /// </list>
    ///
    /// <b>Singleton usage (no DI):</b>
    /// <code>
    /// IAgeSignalsProvider provider = AgeSignalsController.Instance;
    /// provider.CheckAgeSignals();
    /// </code>
    ///
    /// <b>DI usage (Zenject example):</b>
    /// <code>
    /// Container.Bind&lt;IAgeSignalsProvider&gt;()
    ///          .FromComponentInNewPrefab(ageSignalsPrefab)
    ///          .AsSingle();
    /// </code>
    /// </summary>
    public interface IAgeSignalsProvider
    {
        /// <summary>Fired when restriction flags are updated after a successful API call.</summary>
        event Action<AgeRestrictionFlags> OnRestrictionsUpdated;

        /// <summary>Fired when the API returns an error or the bridge call fails.</summary>
        event Action<AgeSignalsError> OnError;

        /// <summary>Current restriction flags (loaded from cache on startup, updated after API call).</summary>
        AgeRestrictionFlags CurrentFlags { get; }

        /// <summary>Whether an age signals check is currently in progress.</summary>
        bool IsChecking { get; }

        /// <summary>
        /// Initiates an age signals check. Should be called on every app launch.
        /// Results are delivered via <see cref="OnRestrictionsUpdated"/> and <see cref="OnError"/>.
        /// </summary>
        void CheckAgeSignals();

        /// <summary>
        /// Async version of <see cref="CheckAgeSignals"/>.
        /// Awaits until restriction flags are resolved or an error occurs.
        /// Includes a safety timeout to prevent indefinite hangs if the JNI callback
        /// is never delivered.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum seconds to wait (default 30).</param>
        /// <exception cref="AgeSignalsException">Check failed after all retries.</exception>
        /// <exception cref="TimeoutException">No callback within timeout.</exception>
        Task<AgeRestrictionFlags> CheckAgeSignalsAsync(float timeoutSeconds = 30f);

        /// <summary>
        /// Cancellable version of <see cref="CheckAgeSignalsAsync(float)"/>.
        /// Supports cooperative cancellation via <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait (default 30).</param>
        /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
        /// <exception cref="AgeSignalsException">Check failed after all retries.</exception>
        Task<AgeRestrictionFlags> CheckAgeSignalsAsync(CancellationToken cancellationToken, float timeoutSeconds = 30f);
    }
}
