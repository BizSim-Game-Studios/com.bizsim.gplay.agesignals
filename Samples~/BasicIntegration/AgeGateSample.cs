// Copyright (c) BizSim Game Studios. All rights reserved.
// Sample: Basic Age Signals integration for a game's main entry point.

using BizSim.GPlay.AgeSignals;
using UnityEngine;

/// <summary>
/// Sample MonoBehaviour demonstrating how to integrate Age Signals into your game.
/// Attach this to a persistent GameObject in your startup scene.
///
/// This sample:
/// 1. Checks age signals on app launch
/// 2. Reacts to restriction flags (full access, denied, under-age)
/// 3. Handles errors gracefully with fallback to cached flags
/// </summary>
public class AgeGateSample : MonoBehaviour
{
    [Header("UI References (assign in Inspector)")]
    [Tooltip("Root canvas or panel to show when access is denied.")]
    [SerializeField] private GameObject _accessDeniedScreen;

    [Tooltip("Root canvas or panel to show when age verification is needed.")]
    [SerializeField] private GameObject _verificationPrompt;

    private IAgeSignalsProvider _provider;

    private void Start()
    {
        // Option A: Use singleton directly
        _provider = AgeSignalsController.Instance;

        // Option B: If using DI, inject IAgeSignalsProvider via constructor/field

        if (_provider == null)
        {
            Debug.LogError("[AgeGate] AgeSignalsController not found. " +
                           "Make sure a GameObject with AgeSignalsController exists in the scene.");
            return;
        }

        _provider.OnRestrictionsUpdated += HandleRestrictions;
        _provider.OnError += HandleError;

        // Trigger the check — should run on every app launch
        _provider.CheckAgeSignals();
    }

    private void HandleRestrictions(AgeRestrictionFlags flags)
    {
        Debug.Log($"[AgeGate] Restrictions received: FullAccess={flags.FullAccessGranted}, " +
                  $"PersonalizedAds={flags.PersonalizedAdsEnabled}");

        if (flags.AccessDenied)
        {
            // Parental approval was denied — block access
            ShowAccessDenied();
            return;
        }

        if (flags.NeedsVerification)
        {
            // User status is Unknown — optionally prompt for verification
            ShowVerificationPrompt();
        }

        // Apply feature gating based on flags
        ConfigureFeatures(flags);
    }

    private void HandleError(AgeSignalsError error)
    {
        Debug.LogWarning($"[AgeGate] Age Signals error: {error.ErrorCodeName} — {error.errorMessage}");

        // The controller already retries transient errors and falls back to cached flags.
        // You only receive this event after all retries are exhausted.
        // At this point, CurrentFlags contains the previous session's flags (or defaults).

        var fallbackFlags = _provider.CurrentFlags;
        if (fallbackFlags != null)
        {
            Debug.Log("[AgeGate] Using fallback flags from previous session");
            ConfigureFeatures(fallbackFlags);
        }
    }

    private void ConfigureFeatures(AgeRestrictionFlags flags)
    {
        // Example: Enable/disable game features based on age restrictions
        // Replace these with your actual feature toggles

        if (flags.IsFeatureEnabled(AgeFeatureKeys.Gambling))
            Debug.Log("[AgeGate] Gambling (18+ content) — ENABLED");
        else
            Debug.Log("[AgeGate] Gambling (18+ content) — DISABLED");

        if (flags.IsFeatureEnabled(AgeFeatureKeys.Marketplace))
            Debug.Log("[AgeGate] Marketplace — FULL ACCESS");
        else
            Debug.Log("[AgeGate] Marketplace — RESTRICTED");

        if (flags.IsFeatureEnabled(AgeFeatureKeys.Chat))
            Debug.Log("[AgeGate] Chat / Social — ENABLED");
        else
            Debug.Log("[AgeGate] Chat / Social — DISABLED");

        if (!flags.PersonalizedAdsEnabled)
        {
            Debug.Log("[AgeGate] Personalized ads disabled — switching to contextual ads");
            // Call your ad SDK to disable personalized ads
            // e.g., MediationSettings.SetPersonalizedAds(false);
        }
    }

    private void ShowAccessDenied()
    {
        if (_accessDeniedScreen != null)
            _accessDeniedScreen.SetActive(true);

        Debug.Log("[AgeGate] Access denied screen shown");
    }

    private void ShowVerificationPrompt()
    {
        if (_verificationPrompt != null)
            _verificationPrompt.SetActive(true);

        Debug.Log("[AgeGate] Verification prompt shown");
    }

    private void OnDestroy()
    {
        if (_provider != null)
        {
            _provider.OnRestrictionsUpdated -= HandleRestrictions;
            _provider.OnError -= HandleError;
        }
    }
}
