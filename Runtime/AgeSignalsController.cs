// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

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
    [AddComponentMenu("BizSim/Age Signals/Age Signals Controller")]
    public class AgeSignalsController : MonoBehaviour, IAgeSignalsProvider
    {
        /// <summary>
        /// Lazy singleton. If no instance exists in the scene, one is automatically
        /// created on a new GameObject marked with <c>DontDestroyOnLoad</c>.
        /// </summary>
        public static AgeSignalsController Instance
        {
            get
            {
                if (_applicationIsQuitting)
                    return null;

                if (!Application.isPlaying)
                    return null;

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AgeSignalsController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AgeSignalsController");
                        _instance = go.AddComponent<AgeSignalsController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static AgeSignalsController _instance;
        private static bool _applicationIsQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _applicationIsQuitting = false;
            _instance = null;
        }

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
        private const int MAX_RETRIES = 3;
        private const float RETRY_BASE_DELAY = 2f; // seconds

        /// <summary>
        /// Maximum age (in hours) for cached restriction flags in PlayerPrefs.
        /// Flags older than this are discarded on startup to comply with Google's
        /// policy against long-term storage of age-signal-derived data.
        /// </summary>
        private const int FLAGS_MAX_AGE_HOURS = 24;

        private int _retryCount;
        private CancellationTokenSource _destroyCts;

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

        [Header("Cache")]
        [Tooltip("Use AES-256 encrypted PlayerPrefs cache instead of plain JSON.")]
        [SerializeField] private bool _useEncryptedCache = false;

        [Header("Logging")]
        [Tooltip("Minimum log level. Silent suppresses all output including errors.")]
        [SerializeField] private LogLevel _logLevel = LogLevel.Verbose;

        private IAgeSignalsCacheProvider _cacheProvider;
        private IAgeSignalsAnalyticsAdapter _analyticsAdapter;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridgeClass;
#endif

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Destroy only this component, not the entire GameObject
                // (this component may be added to another singleton's GameObject)
                Destroy(this);
                return;
            }
            Instance = this;
            _destroyCts = new CancellationTokenSource();
            BizSimLogger.MinLevel = _logLevel;

            if (_cacheProvider == null)
            {
                _cacheProvider = _useEncryptedCache
                    ? new EncryptedPlayerPrefsCacheProvider()
                    : new PlayerPrefsCacheProvider();
            }

            LoadRestrictionFlags(); // Load previous session flags as fallback
        }

        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;

            StopAllCoroutines();
            IsChecking = false;

            // Null out all event delegates to release subscriber references.
            // Prevents memory leaks when external objects subscribed via +=
            // but forgot to -= before the controller was destroyed.
            OnRestrictionsUpdated = null;
            OnError = null;

            // Release injected adapters so GC can collect them independently.
            _analyticsAdapter = null;
            _cacheProvider = null;

            if (Instance == this) Instance = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Clean up Java bridge state (static fields survive Domain Reload in Editor)
            try { _bridgeClass?.CallStatic("cleanup"); }
            catch { /* best-effort — JNI may fail during Force Stop or shutdown */ }

            _bridgeClass?.Dispose();
            _bridgeClass = null;
#endif
        }

        // =================================================================
        // Public Configuration API
        // =================================================================

        /// <summary>Sets the minimum log level at runtime. Overrides the Inspector value.</summary>
        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
            BizSimLogger.MinLevel = level;
        }

        /// <summary>
        /// Replaces the cache provider used for persisting restriction flags.
        /// Must be called before <see cref="CheckAgeSignals"/> or during initialization.
        /// </summary>
        /// <param name="provider">Custom cache provider implementation.</param>
        public void SetCacheProvider(IAgeSignalsCacheProvider provider)
        {
            _cacheProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Clears cached restriction flags. Useful for GDPR right-to-erasure compliance
        /// or when the user logs out.
        /// </summary>
        public void ClearCachedData()
        {
            _cacheProvider?.Clear();
            BizSimLogger.Info("Cached flags cleared");
        }

        /// <summary>
        /// Sets a custom analytics adapter for logging age signals events.
        /// Replaces the default logger-only behavior.
        /// </summary>
        /// <param name="adapter">Custom analytics adapter implementation.</param>
        public void SetAnalyticsAdapter(IAgeSignalsAnalyticsAdapter adapter)
        {
            _analyticsAdapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
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
            var destroyToken = _destroyCts?.Token ?? CancellationToken.None;

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), destroyToken)
            );

            if (completedTask != tcs.Task)
            {
                // Timeout or destroy — clean up dangling subscriptions
                Cleanup();
                if (destroyToken.IsCancellationRequested)
                    tcs.TrySetCanceled(destroyToken);
                else
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

            // Link caller token + destroy token so either can cancel.
            // Prevents zombie Task.Delay if MonoBehaviour is destroyed but caller doesn't cancel.
            using var linkedCts = _destroyCts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCts.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

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
            using var registration = linkedToken.Register(() =>
            {
                Cleanup();
                tcs.TrySetCanceled(cancellationToken.IsCancellationRequested
                    ? cancellationToken
                    : linkedToken);
            });

            OnRestrictionsUpdated += onSuccess;
            OnError += onError;

            CheckAgeSignals();

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), linkedToken)
            );

            if (completedTask != tcs.Task)
            {
                Cleanup();

                if (linkedToken.IsCancellationRequested)
                {
                    BizSimLogger.Info("Check cancelled via CancellationToken or MonoBehaviour destroyed");
                    throw new OperationCanceledException(cancellationToken.IsCancellationRequested
                        ? cancellationToken
                        : linkedToken);
                }

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
                BizSimLogger.Error($"Java bridge call failed: {e.Message}");
                IsChecking = false;
                OnError?.Invoke(new AgeSignalsError
                {
                    errorCode = (int)AgeSignalsErrorCode.InternalError,
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
                BizSimLogger.Info($"Editor test mode — status={_fakeStatus}, " +
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
                    BizSimLogger.Info($"Editor mock — simulating error code {_mockConfig.SimulatedErrorCode}");
                    var error = new AgeSignalsError
                    {
                        errorCode = _mockConfig.SimulatedErrorCode,
                        errorMessage = $"Simulated error (mock config)",
                        isRetryable = AgeSignalsError.IsRetryableCode(_mockConfig.SimulatedErrorCode)
                    };
                    LogApiCallResult(false);
                    _analyticsAdapter?.LogError(error);
                    OnError?.Invoke(error);
                    return;
                }

                // Build result from mock config (age range computed from status + MockAge)
                BizSimLogger.Info($"Editor mock — status={_mockConfig.MockStatus}, " +
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
            BizSimLogger.Info("Editor mode — no mock config assigned, returning NotApplicable");
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
        [Preserve]
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

                BizSimLogger.Info($"Result: status={result.UserStatus}, " +
                          $"age=[{result.AgeLower}-{result.AgeUpper}]");

                ProcessResult(result);
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Failed to parse result: {e.Message}\nJSON: {json}");
                OnError?.Invoke(new AgeSignalsError
                {
                    errorCode = (int)AgeSignalsErrorCode.InternalError,
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
            _analyticsAdapter?.LogRestrictionsUpdated(flags);

            OnRestrictionsUpdated?.Invoke(flags);
        }

        /// <summary>
        /// Called by the Java bridge when the API returns an error.
        /// Handles automatic retry with exponential backoff for transient errors.
        /// </summary>
        [Preserve]
        private void OnAgeSignalsError(string json)
        {
            IsChecking = false;

            try
            {
                var error = JsonUtility.FromJson<AgeSignalsError>(json);

                BizSimLogger.Info($"Error: {error.ErrorCodeName} ({error.errorCode})" +
                                $" — {error.errorMessage} — retryable={error.isRetryable}");

                // Automatic retry with exponential backoff for transient errors
                // Skip retry for ApiNotAvailable — API absence is permanent on device
                if (error.isRetryable
                    && error.ErrorCodeEnum != AgeSignalsErrorCode.ApiNotAvailable
                    && _retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    float delay = RETRY_BASE_DELAY * Mathf.Pow(2, _retryCount - 1);
                    BizSimLogger.Info($"Retry {_retryCount}/{MAX_RETRIES} in {delay}s");
                    StartCoroutine(RetryAfterDelay(delay));
                    return;
                }

                LogApiCallResult(false);
                _analyticsAdapter?.LogError(error);

                // Fall back to previous session flags if available
                if (CurrentFlags != null)
                {
                    BizSimLogger.Info("Using previous session flags as fallback");
                    OnRestrictionsUpdated?.Invoke(CurrentFlags);
                }

                OnError?.Invoke(error);
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Failed to parse error: {e.Message}\nJSON: {json}");
            }
        }

        private IEnumerator RetryAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (this == null || _destroyCts == null || _destroyCts.IsCancellationRequested)
                yield break;
            ExecuteCheck();
        }

        // =================================================================
        // Decision Engine
        // =================================================================

        /// <summary>
        /// Converts raw age signal data into application-level restriction flags.
        /// Delegates entirely to <see cref="AgeSignalsDecisionLogic.ComputeFlags"/>.
        /// If no custom logic is assigned, a default instance with standard thresholds is used.
        /// </summary>
        private AgeRestrictionFlags MakeRestrictionDecisions(AgeSignalsResult result)
        {
            var flags = new AgeRestrictionFlags();
            EnsureDecisionLogic();
            _decisionLogic.ComputeFlags(result, flags);
            AgeSignalsCacheLogic.StampFlags(flags, _decisionLogic.ComputeConfigHash(), PackageVersion.Current);
            return flags;
        }

        /// <summary>
        /// Lazily creates a default <see cref="AgeSignalsDecisionLogic"/> instance
        /// when none is assigned via the Inspector. Uses default thresholds:
        /// gambling 18+, marketplace 16+, chat 13+, personalized ads 13+.
        /// </summary>
        private void EnsureDecisionLogic()
        {
            if (_decisionLogic != null) return;
            _decisionLogic = ScriptableObject.CreateInstance<AgeSignalsDecisionLogic>();
            _decisionLogic.hideFlags = HideFlags.HideAndDontSave;
        }

        // =================================================================
        // Flag Persistence — via IAgeSignalsCacheProvider + TTL expiration
        // =================================================================

        private void SaveRestrictionFlags(AgeRestrictionFlags flags)
        {
            _cacheProvider.Save(flags);
        }

        /// <summary>
        /// Loads restriction flags from the previous session as fallback.
        /// If no valid cache exists or it is older than <see cref="FLAGS_MAX_AGE_HOURS"/>,
        /// defaults to full access (adult assumption) to avoid restricting users
        /// before the API responds.
        /// </summary>
        private void LoadRestrictionFlags()
        {
            var loaded = _cacheProvider.Load();

            if (loaded != null)
            {
                EnsureDecisionLogic();
                string configHash = _decisionLogic.ComputeConfigHash();

                if (AgeSignalsCacheLogic.IsCacheValid(loaded, FLAGS_MAX_AGE_HOURS, configHash, PackageVersion.Current))
                {
                    CurrentFlags = loaded;
                    double ageHours = AgeSignalsCacheLogic.GetCacheAgeHours(loaded);
                    BizSimLogger.Info($"Loaded cached flags ({ageHours:F1}h old)");
                    return;
                }

                double expiredAge = AgeSignalsCacheLogic.GetCacheAgeHours(loaded);
                BizSimLogger.Info(expiredAge >= 0
                    ? $"Cached flags expired ({expiredAge:F1}h > {FLAGS_MAX_AGE_HOURS}h), using defaults"
                    : "Cached flags have invalid timestamp, using defaults");
                _cacheProvider.Clear();
            }

            // Default: restrictive (fail-safe) until API confirms age
            CurrentFlags = AgeSignalsCacheLogic.CreateDefaultFlags();
        }

        // =================================================================
        // Analytics
        // =================================================================

        /// <summary>
        /// Logs a binary success/error event via the pluggable analytics adapter.
        /// Falls back to <see cref="BizSimLogger"/> when no adapter is assigned.
        /// </summary>
        private void LogApiCallResult(bool success)
        {
            if (_analyticsAdapter != null)
            {
                _analyticsAdapter.LogApiCallResult(success);
            }
            else
            {
                BizSimLogger.Info($"API call result: {(success ? "success" : "error")}");
            }
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
