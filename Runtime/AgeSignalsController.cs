// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BizSim.GPlay.AgeSignals
{
    /// <summary>
    /// Singleton controller for the Google Play Age Signals API.
    /// Attach to a persistent GameObject (e.g., one marked with <c>DontDestroyOnLoad</c>).
    ///
    /// <b>Privacy Policy Compliance:</b>
    /// <list type="bullet">
    /// <item>Raw age data (<see cref="AgeSignalsResult"/>) is kept in memory only — never persisted.</item>
    /// <item>Only behavior flags (<see cref="AgeRestrictionFlags"/>) are saved to <c>PlayerPrefs</c>
    ///       and automatically cleared if older than <see cref="FLAGS_MAX_AGE_HOURS"/> hours.</item>
    /// <item>The API is queried on every app launch — results are not cached across sessions.</item>
    /// <item>Analytics events contain only "success"/"error" strings — no age information.</item>
    /// </list>
    ///
    /// <b>Usage:</b>
    /// <code>
    /// AgeSignalsController.Instance.OnRestrictionsUpdated += HandleRestrictions;
    /// AgeSignalsController.Instance.OnError += HandleError;
    /// AgeSignalsController.Instance.CheckAgeSignals();
    /// </code>
    /// </summary>
    [HelpURL("https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals#quick-start-tutorial")]
    [AddComponentMenu("BizSim/Age Signals Controller")]
    public class AgeSignalsController : MonoBehaviour, IAgeSignalsProvider
    {
        public static AgeSignalsController Instance { get; private set; }

        // --- Events ---

        /// <summary>Fired when restriction flags are updated after a successful API call.</summary>
        public event Action<AgeRestrictionFlags> OnRestrictionsUpdated;

        /// <summary>Fired when the API returns an error or the bridge call fails.</summary>
        public event Action<AgeSignalsError> OnError;

        // --- Public State ---

        /// <summary>
        /// Current restriction flags. Loaded from <c>PlayerPrefs</c> on startup
        /// and updated after each successful API call.
        /// </summary>
        public AgeRestrictionFlags CurrentFlags { get; private set; }

        /// <summary>Whether an age signals check is currently in progress.</summary>
        public bool IsChecking { get; private set; }

        // --- Configuration ---
        /// <summary>PlayerPrefs key used to persist restriction flags between sessions.</summary>
        public const string FLAGS_PREFS_KEY = "AgeRestriction_Flags";
        private const int MAX_RETRIES = 3;
        private const float RETRY_BASE_DELAY = 2f; // seconds

        /// <summary>
        /// Maximum age (in hours) for cached restriction flags in PlayerPrefs.
        /// Flags older than this are discarded on startup to comply with Google's
        /// policy against long-term storage of age-signal-derived data.
        /// </summary>
        private const int FLAGS_MAX_AGE_HOURS = 24;

        private int _retryCount;

        // --- Decision Logic ---

        /// <summary>
        /// Optional pluggable decision logic ScriptableObject.
        /// When assigned, delegates flag computation to <see cref="AgeSignalsDecisionLogic.ComputeFlags"/>.
        /// When null, uses the built-in default logic (same thresholds as the default SO).
        /// Create a custom subclass to override age thresholds per project.
        /// </summary>
        [Header("Decision Logic")]
        [Tooltip("Optional: assign a custom AgeSignalsDecisionLogic asset to override default age thresholds.")]
        [SerializeField] private AgeSignalsDecisionLogic _decisionLogic;

        // --- Editor Mock Config ---
#if UNITY_EDITOR
        /// <summary>
        /// Optional ScriptableObject for configuring mock responses in the Editor.
        /// Create via Assets → Create → BizSim → Age Signals Mock Config.
        /// When assigned, the controller uses this config instead of returning a
        /// hardcoded NotApplicable result.
        /// </summary>
        [Header("Editor Mock Config")]
        [Tooltip("Assign an AgeSignalsMockConfig asset to test different scenarios in the Editor.")]
        [SerializeField] private AgeSignalsMockConfig _mockConfig;
#endif

        // --- Test Mode (debug builds only, Android device) ---
        [Header("Test Mode (Debug Builds Only)")]
        [Tooltip("Enable to use Google's FakeAgeSignalsManager on-device. Only works in debug builds.")]
        [SerializeField] private bool _useFakeForTesting = false;

        [Tooltip("Verification status to simulate on-device with FakeAgeSignalsManager.")]
        [SerializeField] private AgeVerificationStatus _fakeStatus = AgeVerificationStatus.Supervised;

        [Tooltip("Simulated age for supervised/unknown users (8–99). Ignored for Verified and NotApplicable.")]
        [Range(5, 25)]
        [SerializeField] private int _fakeAge = 14;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridgeClass;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadRestrictionFlags(); // Load previous session flags as fallback
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            _bridgeClass?.Dispose();
            _bridgeClass = null;
#endif
        }

        // =================================================================
        // Public API
        // =================================================================

        /// <summary>
        /// Initiates an age signals check against the Google Play API.
        /// Should be called on every app launch — does NOT return cached results.
        /// Results are delivered asynchronously via <see cref="OnRestrictionsUpdated"/>
        /// and <see cref="OnError"/> events.
        /// </summary>
        public void CheckAgeSignals()
        {
            if (IsChecking) return;
            _retryCount = 0;
            ExecuteCheck();
        }

        /// <summary>
        /// Async/await version of <see cref="CheckAgeSignals"/>.
        /// Awaits until restriction flags are resolved or the check fails after all retries.
        ///
        /// Includes a safety timeout (default 30 seconds) to prevent the
        /// <see cref="TaskCompletionSource{T}"/> from hanging indefinitely
        /// if a JNI callback is never delivered.
        ///
        /// <b>Thread safety:</b> The underlying <c>UnitySendMessage</c> callback always
        /// arrives on the main thread. The <see cref="TaskCompletionSource{T}"/> is
        /// completed from that same callback, so the continuation runs on the main thread.
        /// </summary>
        /// <param name="timeoutSeconds">
        /// Maximum time to wait for a response before cancelling.
        /// Default is 30 seconds (enough for 3 retries with exponential backoff: 2+4+8 = 14s + API time).
        /// </param>
        /// <returns>The resulting <see cref="AgeRestrictionFlags"/>.</returns>
        /// <exception cref="AgeSignalsException">Thrown when the check fails after all retries.</exception>
        /// <exception cref="TimeoutException">Thrown when no callback is received within the timeout.</exception>
        public async Task<AgeRestrictionFlags> CheckAgeSignalsAsync(float timeoutSeconds = 30f)
        {
            var tcs = new TaskCompletionSource<AgeRestrictionFlags>();

            // One-shot event handlers that auto-unsubscribe after first invocation.
            Action<AgeRestrictionFlags> onSuccess = null;
            Action<AgeSignalsError> onError = null;

            void Cleanup()
            {
                OnRestrictionsUpdated -= onSuccess;
                OnError -= onError;
            }

            onSuccess = flags =>
            {
                Cleanup();
                tcs.TrySetResult(flags);
            };

            onError = error =>
            {
                Cleanup();
                tcs.TrySetException(new AgeSignalsException(error));
            };

            OnRestrictionsUpdated += onSuccess;
            OnError += onError;

            CheckAgeSignals();

            // If CheckAgeSignals() resolved synchronously (e.g., Editor mock),
            // the TCS is already completed before we reach the timeout check.
            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))
            );

            if (completedTask != tcs.Task)
            {
                // Timeout — clean up dangling subscriptions
                Cleanup();
                tcs.TrySetException(new TimeoutException(
                    $"[AgeSignals] No callback received within {timeoutSeconds}s. " +
                    "The JNI bridge may have failed silently."));
            }

            return await tcs.Task;
        }

        /// <inheritdoc/>
        public async Task<AgeRestrictionFlags> CheckAgeSignalsAsync(CancellationToken cancellationToken, float timeoutSeconds = 30f)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tcs = new TaskCompletionSource<AgeRestrictionFlags>();

            Action<AgeRestrictionFlags> onSuccess = null;
            Action<AgeSignalsError> onError = null;

            void Cleanup()
            {
                OnRestrictionsUpdated -= onSuccess;
                OnError -= onError;
            }

            onSuccess = flags =>
            {
                Cleanup();
                tcs.TrySetResult(flags);
            };

            onError = error =>
            {
                Cleanup();
                tcs.TrySetException(new AgeSignalsException(error));
            };

            // Register cancellation callback (after Cleanup is defined)
            using var registration = cancellationToken.Register(() =>
            {
                Cleanup();
                tcs.TrySetCanceled(cancellationToken);
            });

            OnRestrictionsUpdated += onSuccess;
            OnError += onError;

            CheckAgeSignals();

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken)
            );

            if (completedTask != tcs.Task)
            {
                Cleanup();
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);

                tcs.TrySetException(new TimeoutException(
                    $"[AgeSignals] No callback received within {timeoutSeconds}s."));
            }

            return await tcs.Task;
        }

        // =================================================================
        // Bridge Execution
        // =================================================================

        private void ExecuteCheck()
        {
            IsChecking = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (_bridgeClass == null)
                    _bridgeClass = new AndroidJavaClass(
                        "com.bizsim.gplay.agesignals.AgeSignalsBridge");

                bool useFake = Debug.isDebugBuild && _useFakeForTesting;

                if (useFake)
                {
                    GetFakeAgeRange(_fakeStatus, _fakeAge, out int fakeLower, out int fakeUpper);
                    _bridgeClass.CallStatic("checkAgeSignalsWithFake",
                        gameObject.name,
                        nameof(OnAgeSignalsResult),
                        nameof(OnAgeSignalsError),
                        true,
                        StatusToJavaString(_fakeStatus),
                        fakeLower,
                        fakeUpper);
                }
                else
                {
                    _bridgeClass.CallStatic("checkAgeSignals",
                        gameObject.name,
                        nameof(OnAgeSignalsResult),
                        nameof(OnAgeSignalsError));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AgeSignals] Java bridge call failed: {e.Message}");
                IsChecking = false;
                OnError?.Invoke(new AgeSignalsError
                {
                    errorCode = -100,
                    errorMessage = $"JNI call failed: {e.Message}",
                    isRetryable = false
                });
            }
#else
            // --- Editor Mock Path ---
            IsChecking = false;
            BuildEditorMockResult();
#endif
        }

#if !UNITY_ANDROID || UNITY_EDITOR
        /// <summary>
        /// Builds a mock <see cref="AgeSignalsResult"/> from the assigned
        /// <see cref="_mockConfig"/> ScriptableObject, or falls back to
        /// a default NotApplicable result if no config is assigned.
        /// </summary>
        private void BuildEditorMockResult()
        {
#if UNITY_EDITOR
            // Priority 1: Test Mode fields (fake status/age from Inspector)
            if (_useFakeForTesting)
            {
                GetFakeAgeRange(_fakeStatus, _fakeAge, out int fakeLower, out int fakeUpper);
                Debug.Log($"[AgeSignals] Editor test mode — status={_fakeStatus}, " +
                          $"age=[{fakeLower}-{fakeUpper}]");
                var fakeResult = new AgeSignalsResult
                {
                    UserStatus = _fakeStatus,
                    AgeLower = fakeLower,
                    AgeUpper = fakeUpper,
                    InstallId = null,
                    MostRecentApprovalDateMs = 0
                };
                ProcessResult(fakeResult);
                return;
            }

            // Priority 2: Mock Config ScriptableObject
            if (_mockConfig != null)
            {
                // Simulate an error if configured
                if (_mockConfig.SimulateError)
                {
                    Debug.Log($"[AgeSignals] Editor mock — simulating error code {_mockConfig.SimulatedErrorCode}");
                    var error = new AgeSignalsError
                    {
                        errorCode = _mockConfig.SimulatedErrorCode,
                        errorMessage = $"Simulated error (mock config)",
                        isRetryable = _mockConfig.SimulatedErrorCode >= -8 && _mockConfig.SimulatedErrorCode <= -1
                    };
                    LogApiCallResult(false);
                    OnError?.Invoke(error);
                    return;
                }

                // Build result from mock config (age range computed from status + MockAge)
                Debug.Log($"[AgeSignals] Editor mock — status={_mockConfig.MockStatus}, " +
                          $"age=[{_mockConfig.AgeLower}-{_mockConfig.AgeUpper}]");
                var mockResult = new AgeSignalsResult
                {
                    UserStatus = _mockConfig.MockStatus,
                    AgeLower = _mockConfig.AgeLower,
                    AgeUpper = _mockConfig.AgeUpper,
                    InstallId = null,
                    MostRecentApprovalDateMs = 0
                };
                ProcessResult(mockResult);
                return;
            }
#endif
            // Default fallback: simulate a user outside supported jurisdiction
            Debug.Log("[AgeSignals] Editor mode — no mock config assigned, returning NotApplicable");
            var defaultResult = new AgeSignalsResult
            {
                UserStatus = AgeVerificationStatus.NotApplicable,
                AgeLower = -1,
                AgeUpper = -1,
                InstallId = null,
                MostRecentApprovalDateMs = 0
            };
            ProcessResult(defaultResult);
        }
#endif

        // =================================================================
        // UnitySendMessage Callbacks (invoked from Java via JNI)
        // =================================================================

        /// <summary>
        /// Called by the Java bridge when the API returns a successful result.
        /// Parses the JSON payload into <see cref="AgeSignalsResult"/> and processes it.
        /// </summary>
        private void OnAgeSignalsResult(string json)
        {
            IsChecking = false;
            _retryCount = 0;

            try
            {
                var parsed = JsonUtility.FromJson<AgeSignalsJsonResult>(json);
                var result = new AgeSignalsResult
                {
                    UserStatus = ParseStatus(parsed.userStatus),
                    AgeLower = parsed.ageLower,
                    AgeUpper = parsed.ageUpper,
                    InstallId = parsed.installId,
                    MostRecentApprovalDateMs = parsed.mostRecentApprovalDate
                };

                Debug.Log($"[AgeSignals] Result: status={result.UserStatus}, " +
                          $"age=[{result.AgeLower}-{result.AgeUpper}]");

                ProcessResult(result);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AgeSignals] Failed to parse result: {e.Message}\nJSON: {json}");
                OnError?.Invoke(new AgeSignalsError
                {
                    errorCode = -100,
                    errorMessage = $"Parse error: {e.Message}",
                    isRetryable = false
                });
            }
        }

        /// <summary>
        /// Converts raw API result into restriction decisions and persists them.
        /// The raw age data is NOT retained beyond this method.
        /// </summary>
        private void ProcessResult(AgeSignalsResult result)
        {
            var flags = MakeRestrictionDecisions(result);

            CurrentFlags = flags;
            SaveRestrictionFlags(flags);

            LogApiCallResult(true);

            OnRestrictionsUpdated?.Invoke(flags);
        }

        /// <summary>
        /// Called by the Java bridge when the API returns an error.
        /// Handles automatic retry with exponential backoff for transient errors.
        /// </summary>
        private void OnAgeSignalsError(string json)
        {
            IsChecking = false;

            try
            {
                var error = JsonUtility.FromJson<AgeSignalsError>(json);

                Debug.LogWarning($"[AgeSignals] Error: {error.ErrorCodeName} ({error.errorCode})" +
                                 $" — {error.errorMessage} — retryable={error.isRetryable}");

                // Automatic retry with exponential backoff for transient errors
                if (error.isRetryable && _retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    float delay = RETRY_BASE_DELAY * Mathf.Pow(2, _retryCount - 1);
                    Debug.Log($"[AgeSignals] Retry {_retryCount}/{MAX_RETRIES} in {delay}s");
                    StartCoroutine(RetryAfterDelay(delay));
                    return;
                }

                LogApiCallResult(false);

                // Fall back to previous session flags if available
                if (CurrentFlags != null)
                {
                    Debug.Log("[AgeSignals] Using previous session flags as fallback");
                    OnRestrictionsUpdated?.Invoke(CurrentFlags);
                }

                OnError?.Invoke(error);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AgeSignals] Failed to parse error: {e.Message}\nJSON: {json}");
            }
        }

        private IEnumerator RetryAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ExecuteCheck();
        }

        // =================================================================
        // Decision Engine
        // =================================================================

        /// <summary>
        /// Converts raw age signal data into application-level restriction flags.
        /// This is the single point where age data is translated into behavior decisions.
        ///
        /// Default thresholds:
        /// <list type="bullet">
        /// <item><b>FeatureA</b> — 18+ only (e.g., gambling, casino)</item>
        /// <item><b>FeatureB</b> — 16+ full access (e.g., marketplace, trading)</item>
        /// <item><b>FeatureC</b> — 13+ access (e.g., chat, social features)</item>
        /// <item><b>PersonalizedAds</b> — 13+ (COPPA compliance)</item>
        /// </list>
        /// </summary>
        private AgeRestrictionFlags MakeRestrictionDecisions(AgeSignalsResult result)
        {
            var flags = new AgeRestrictionFlags();

            if (_decisionLogic != null)
            {
                _decisionLogic.ComputeFlags(result, flags);
            }
            else
            {
                // Built-in defaults — same as AgeSignalsDecisionLogic base with default features
                bool noData = !result.HasAgeData;
                flags.AccessDenied = result.IsAccessDenied;

                if (flags.AccessDenied)
                {
                    flags.FullAccessGranted = false;
                    flags.PersonalizedAdsEnabled = false;
                    flags.NeedsVerification = false;
                    flags.SetFeature(AgeFeatureKeys.Gambling, false);
                    flags.SetFeature(AgeFeatureKeys.Marketplace, false);
                    flags.SetFeature(AgeFeatureKeys.Chat, false);
                }
                else
                {
                    flags.FullAccessGranted = noData || result.IsAdult;
                    flags.SetFeature(AgeFeatureKeys.Gambling, noData || result.IsAdult);
                    flags.SetFeature(AgeFeatureKeys.Marketplace, noData || result.IsAdult || !result.IsUnder(16));
                    flags.SetFeature(AgeFeatureKeys.Chat, noData || !result.IsUnder(13));
                    flags.PersonalizedAdsEnabled = noData || !result.IsUnder(13);
                    flags.NeedsVerification = result.UserStatus == AgeVerificationStatus.Unknown;
                }

                // Sync deprecated fields
#pragma warning disable CS0618
                flags.FeatureAEnabled = flags.IsFeatureEnabled(AgeFeatureKeys.Gambling);
                flags.FeatureBFullAccess = flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace);
                flags.FeatureCEnabled = flags.IsFeatureEnabled(AgeFeatureKeys.Chat);
#pragma warning restore CS0618
            }

            flags.DecisionTimestamp = DateTime.UtcNow.ToString("o");
            return flags;
        }

        // =================================================================
        // Flag Persistence (PlayerPrefs) — with TTL expiration
        // =================================================================

        private void SaveRestrictionFlags(AgeRestrictionFlags flags)
        {
            PlayerPrefs.SetString(FLAGS_PREFS_KEY, JsonUtility.ToJson(flags));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads restriction flags from the previous session as fallback.
        /// If no saved flags exist or they are older than <see cref="FLAGS_MAX_AGE_HOURS"/>,
        /// defaults to full access (adult assumption) to avoid restricting users
        /// before the API responds.
        /// </summary>
        private void LoadRestrictionFlags()
        {
            string json = PlayerPrefs.GetString(FLAGS_PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var loaded = JsonUtility.FromJson<AgeRestrictionFlags>(json);

                    // Expire stale flags to comply with Google's policy against
                    // long-term storage of age-signal-derived data.
                    if (!string.IsNullOrEmpty(loaded.DecisionTimestamp) &&
                        DateTime.TryParse(loaded.DecisionTimestamp, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                    {
                        double ageHours = (DateTime.UtcNow - dt).TotalHours;
                        if (ageHours <= FLAGS_MAX_AGE_HOURS)
                        {
                            CurrentFlags = loaded;
                            Debug.Log($"[AgeSignals] Loaded cached flags ({ageHours:F1}h old)");
                            return;
                        }

                        Debug.Log($"[AgeSignals] Cached flags expired ({ageHours:F1}h > {FLAGS_MAX_AGE_HOURS}h), using defaults");
                        PlayerPrefs.DeleteKey(FLAGS_PREFS_KEY);
                    }
                    else
                    {
                        // No valid timestamp — treat as expired
                        PlayerPrefs.DeleteKey(FLAGS_PREFS_KEY);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AgeSignals] Failed to load cached flags: {e.Message}");
                    PlayerPrefs.DeleteKey(FLAGS_PREFS_KEY);
                }
            }

            // Default: assume adult until API says otherwise
            CurrentFlags = new AgeRestrictionFlags
            {
                FullAccessGranted = true,
                PersonalizedAdsEnabled = true,
                DecisionTimestamp = DateTime.UtcNow.ToString("o")
            };
            CurrentFlags.SetFeature(AgeFeatureKeys.Gambling, true);
            CurrentFlags.SetFeature(AgeFeatureKeys.Marketplace, true);
            CurrentFlags.SetFeature(AgeFeatureKeys.Chat, true);
#pragma warning disable CS0618
            CurrentFlags.FeatureAEnabled = true;
            CurrentFlags.FeatureBFullAccess = true;
            CurrentFlags.FeatureCEnabled = true;
#pragma warning restore CS0618
        }

        // =================================================================
        // Analytics
        // =================================================================

        /// <summary>
        /// Logs a binary success/error event. Uses Firebase Analytics when the
        /// <c>AGESIGNALS_FIREBASE</c> define is set; otherwise falls back to
        /// <see cref="Debug.Log"/> so API call success rates are always observable.
        /// </summary>
        private void LogApiCallResult(bool success)
        {
            string result = success ? "success" : "error";

#if AGESIGNALS_FIREBASE && !UNITY_EDITOR
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("age_signals_api_call",
                    new Firebase.Analytics.Parameter("result", result)
                );
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AgeSignals] Analytics log failed: {e.Message}");
            }
#else
            Debug.Log($"[AgeSignals] API call result: {result}");
#endif
        }

        // =================================================================
        // JSON Parsing Helpers
        // =================================================================

        /// <summary>
        /// Converts a status string from the Java bridge to the corresponding enum value.
        /// Returns <see cref="AgeVerificationStatus.NotApplicable"/> for null or unrecognized values.
        /// </summary>
        private static AgeVerificationStatus ParseStatus(string status)
        {
            if (string.IsNullOrEmpty(status) || status == "null")
                return AgeVerificationStatus.NotApplicable;

            return status switch
            {
                "VERIFIED" => AgeVerificationStatus.Verified,
                "SUPERVISED" => AgeVerificationStatus.Supervised,
                "SUPERVISED_APPROVAL_PENDING" => AgeVerificationStatus.SupervisedApprovalPending,
                "SUPERVISED_APPROVAL_DENIED" => AgeVerificationStatus.SupervisedApprovalDenied,
                "UNKNOWN" => AgeVerificationStatus.Unknown,
                _ => AgeVerificationStatus.NotApplicable
            };
        }

        /// <summary>
        /// Converts an <see cref="AgeVerificationStatus"/> enum to the uppercase string
        /// expected by the Java bridge's FakeAgeSignalsManager.
        /// </summary>
        private static string StatusToJavaString(AgeVerificationStatus status) => status switch
        {
            AgeVerificationStatus.Verified => "VERIFIED",
            AgeVerificationStatus.Supervised => "SUPERVISED",
            AgeVerificationStatus.SupervisedApprovalPending => "SUPERVISED_APPROVAL_PENDING",
            AgeVerificationStatus.SupervisedApprovalDenied => "SUPERVISED_APPROVAL_DENIED",
            AgeVerificationStatus.Unknown => "UNKNOWN",
            _ => "UNKNOWN"
        };

        /// <summary>
        /// Computes realistic age range bounds from a status and single age value.
        /// Follows Google Play Age Signals API conventions:
        /// <list type="bullet">
        /// <item><b>Verified</b> — adult, age range 18–150 (API guarantees 18+).</item>
        /// <item><b>Supervised*</b> — child/teen, ±2 year bucket around the given age.</item>
        /// <item><b>Unknown / NotApplicable</b> — no age data (-1, -1).</item>
        /// </list>
        /// </summary>
        private static void GetFakeAgeRange(AgeVerificationStatus status, int age, out int lower, out int upper)
        {
            switch (status)
            {
                case AgeVerificationStatus.Verified:
                    // API only returns Verified for confirmed 18+ users
                    lower = 18;
                    upper = 150;
                    break;

                case AgeVerificationStatus.Supervised:
                case AgeVerificationStatus.SupervisedApprovalPending:
                case AgeVerificationStatus.SupervisedApprovalDenied:
                    // Supervised accounts: simulate a ±2 year age bucket
                    lower = Mathf.Max(0, age - 2);
                    upper = age + 2;
                    break;

                default: // Unknown, NotApplicable
                    lower = -1;
                    upper = -1;
                    break;
            }
        }

        /// <summary>
        /// Internal DTO for <see cref="JsonUtility"/> deserialization.
        /// Field names must match the JSON keys sent by the Java bridge (camelCase).
        /// </summary>
        [Serializable]
        private class AgeSignalsJsonResult
        {
            public string userStatus;
            public int ageLower = -1;
            public int ageUpper = -1;
            public string installId;
            public long mostRecentApprovalDate;
        }
    }
}
