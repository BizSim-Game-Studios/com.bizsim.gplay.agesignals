// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BizSim.GPlay.AgeSignals.UniTaskSupport
{
    /// <summary>
    /// UniTask extension methods for <see cref="AgeSignalsController"/>.
    /// This assembly only compiles when <c>com.cysharp.unitask</c> is installed
    /// (enforced via <c>defineConstraints</c> in the asmdef).
    ///
    /// <b>Usage:</b>
    /// <code>
    /// using BizSim.GPlay.AgeSignals.UniTaskSupport;
    ///
    /// var flags = await AgeSignalsController.Instance.CheckAgeSignalsUniTask();
    /// </code>
    /// </summary>
    public static class AgeSignalsUniTaskExtensions
    {
        /// <summary>
        /// Checks age signals using UniTask. Allocates no <c>Task</c> objects
        /// and integrates with Unity's PlayerLoop for zero-overhead awaiting.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default 30000).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The resulting <see cref="AgeRestrictionFlags"/>.</returns>
        /// <exception cref="AgeSignalsException">Check failed after all retries.</exception>
        /// <exception cref="TimeoutException">No callback within timeout.</exception>
        public static async UniTask<AgeRestrictionFlags> CheckAgeSignalsUniTask(
            this AgeSignalsController controller,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            var utcs = new UniTaskCompletionSource<AgeRestrictionFlags>();

            void OnSuccess(AgeRestrictionFlags flags)
            {
                controller.OnRestrictionsUpdated -= OnSuccess;
                controller.OnError -= OnError;
                utcs.TrySetResult(flags);
            }

            void OnError(AgeSignalsError error)
            {
                controller.OnRestrictionsUpdated -= OnSuccess;
                controller.OnError -= OnError;
                utcs.TrySetException(new AgeSignalsException(error));
            }

            controller.OnRestrictionsUpdated += OnSuccess;
            controller.OnError += OnError;

            controller.CheckAgeSignals();

            var (hasResult, result) = await utcs.Task
                .TimeoutWithoutException(TimeSpan.FromMilliseconds(timeoutMs));

            if (!hasResult)
            {
                controller.OnRestrictionsUpdated -= OnSuccess;
                controller.OnError -= OnError;
                throw new TimeoutException(
                    $"[AgeSignals] No callback received within {timeoutMs}ms.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }
}
