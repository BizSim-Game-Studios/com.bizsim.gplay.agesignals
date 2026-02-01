// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Log severity levels for <see cref="BizSimLogger"/>.
    /// Each level includes all levels above it (e.g., <see cref="Warning"/> includes errors).
    /// </summary>
    public enum LogLevel
    {
        /// <summary>All messages including verbose traces.</summary>
        Verbose = 0,

        /// <summary>Informational messages and above.</summary>
        Info = 1,

        /// <summary>Warnings and errors only.</summary>
        Warning = 2,

        /// <summary>Errors only.</summary>
        Error = 3,

        /// <summary>No logging at all — completely silent, even for errors.</summary>
        Silent = 4
    }

    /// <summary>
    /// Zero-allocation logger for release builds with configurable log level.
    ///
    /// <b>Debug builds:</b> Methods marked with <c>[Conditional("DEBUG")]</c> are stripped
    /// by the compiler in release builds — including all string interpolation at call sites.
    ///
    /// <b>Error handling:</b> <see cref="Error"/> is NOT conditional (always compiled),
    /// but respects <see cref="MinLevel"/>. Set to <see cref="LogLevel.Silent"/> to suppress
    /// errors in production if needed (e.g., for known benign failures).
    ///
    /// <b>Configuration:</b>
    /// <code>
    /// // From code (e.g., during initialization):
    /// BizSimLogger.MinLevel = LogLevel.Warning;
    ///
    /// // Or via controller's Initialize method / ScriptableObject config.
    /// </code>
    ///
    /// <b>Why a static class?</b>
    /// <c>[Conditional]</c> only works on <c>void</c>-returning methods of a class —
    /// the compiler must be able to strip the entire call expression, including all
    /// argument evaluation. A lambda or delegate cannot be conditionally compiled.
    /// </summary>
    internal static class BizSimLogger
    {
        private const string Prefix = "[AgeSignals]";

        /// <summary>
        /// Minimum log level. Messages below this severity are suppressed at runtime.
        /// Defaults to <see cref="LogLevel.Verbose"/> (all messages).
        ///
        /// Note: In release builds, <see cref="Info"/>, <see cref="Verbose"/>, and
        /// <see cref="Warning"/> are already stripped by <c>[Conditional("DEBUG")]</c>.
        /// This property primarily controls <see cref="Error"/> suppression in production
        /// and fine-grained filtering in debug builds.
        /// </summary>
        internal static LogLevel MinLevel { get; set; } = LogLevel.Verbose;

        [Conditional("DEBUG")]
        internal static void Verbose(string message)
        {
            if (MinLevel <= LogLevel.Verbose)
                Debug.Log($"{Prefix} [V] {message}");
        }

        [Conditional("DEBUG")]
        internal static void Info(string message)
        {
            if (MinLevel <= LogLevel.Info)
                Debug.Log($"{Prefix} {message}");
        }

        [Conditional("DEBUG")]
        internal static void Warning(string message)
        {
            if (MinLevel <= LogLevel.Warning)
                Debug.LogWarning($"{Prefix} {message}");
        }

        /// <summary>
        /// Logs an error. NOT conditional — survives compiler stripping in release builds.
        /// Respects <see cref="MinLevel"/>: set to <see cref="LogLevel.Silent"/> to suppress.
        /// </summary>
        internal static void Error(string message)
        {
            if (MinLevel <= LogLevel.Error)
                Debug.LogError($"{Prefix} {message}");
        }
    }
}
