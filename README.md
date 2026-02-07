# Google Play Age Signals Bridge

[![Unity 6000.3+](https://img.shields.io/badge/Unity-6000.3%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-0.1.0-orange.svg)](CHANGELOG.md)

> **⚠️ Unofficial package.** This is a community-built Unity bridge for the [Google Play Age Signals API](https://developer.android.com/games/agesignals). It is **not** an official Google product. The underlying Age Signals SDK (v0.0.2) is currently in **beta**.

**Package:** `com.bizsim.gplay.agesignals`
**Version:** 0.1.0 (Initial Release)
**Namespace:** `BizSim.GPlay.AgeSignals`
**Author:** BizSim Game Studios ([bizsim.com](https://www.bizsim.com))
**License:** MIT

## Table of Contents

- [Overview](#overview)
- [Privacy Policy Compliance](#privacy-policy-compliance)
- [Installation](#installation)
- [Quick Start Tutorial](#quick-start-tutorial)
- [Dependency Injection Support](#dependency-injection-support)
- [API Reference](#api-reference)
- [Data Safety & Privacy](#data-safety--privacy)
- [Requirements](#requirements)

## Overview

This package provides a complete Java-to-C# bridge for querying age verification status on Android via Google Play:

- **Java Bridge** (`AgeSignalsBridge.java`) — Calls the native Age Signals API via JNI
- **C# Controller** (`AgeSignalsController`) — Singleton with async callbacks, retry logic, and decision engine
- **Data Models** — `AgeVerificationStatus`, `AgeSignalsResult`, `AgeRestrictionFlags`, `AgeSignalsError`
- **Editor Mock** — `AgeSignalsMockConfig` ScriptableObject for testing without a device

## Privacy Policy Compliance

- **Raw age data** (`AgeSignalsResult`) is kept **in memory only** — never persisted to disk
- **Only behavior flags** (`AgeRestrictionFlags`) are saved to `PlayerPrefs` — contains zero age data
- **Analytics events** log only technical success/failure — no age info transmitted
- **Firebase integration** is optional via `AGESIGNALS_FIREBASE` scripting define symbol

## Installation

### Option 1: Git URL (recommended)

1. In Unity Editor: **Window → Package Manager → + → Add package from git URL...**
2. Enter:
   ```
   https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals.git
   ```
   Or for a specific version tag:
   ```
   https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals.git#v0.1.0
   ```
3. Or add directly to `Packages/manifest.json`:
   ```json
   "com.bizsim.gplay.agesignals": "https://github.com/BizSim-Game-Studios/com.bizsim.gplay.agesignals.git"
   ```

### Option 2: Local Package (for development)

1. Copy this folder to your project's `Packages/` directory
2. Add to `Packages/manifest.json`:
   ```json
   "com.bizsim.gplay.agesignals": "file:com.bizsim.gplay.agesignals"
   ```

### After Installation

1. In Unity Editor: **Assets → External Dependency Manager → Android Resolver → Force Resolve**
2. (Optional) Add `AGESIGNALS_FIREBASE` to **Scripting Define Symbols** for Firebase Analytics integration
3. **ProGuard rules are applied automatically** — the package includes a `.androidlib` that injects keep rules into your build

## Quick Start Tutorial

### Step 1 — Check age signals on app launch

```csharp
using BizSim.GPlay.AgeSignals;
using UnityEngine;

public class AgeGate : MonoBehaviour
{
    void Start()
    {
        // Subscribe to restriction updates
        AgeSignalsController.Instance.OnRestrictionsUpdated += OnRestrictions;

        // Subscribe to errors (optional)
        AgeSignalsController.Instance.OnError += OnError;

        // Trigger the API call — should run on every app launch
        AgeSignalsController.Instance.CheckAgeSignals();
    }

    void OnRestrictions(AgeRestrictionFlags flags)
    {
        if (flags.FullAccessGranted)
        {
            Debug.Log("Full access — enable all features");
        }
        else if (flags.AccessDenied)
        {
            Debug.Log("Access denied — show restricted screen");
        }
        else if (!flags.PersonalizedAdsEnabled)
        {
            Debug.Log("Personalized ads disabled — show contextual ads only");
        }
    }

    void OnError(AgeSignalsError error)
    {
        Debug.LogWarning($"Age Signals error: {error.ErrorCodeName} — {error.errorMessage}");
        // The controller retries automatically up to 3 times for transient errors
    }

    void OnDestroy()
    {
        if (AgeSignalsController.Instance != null)
        {
            AgeSignalsController.Instance.OnRestrictionsUpdated -= OnRestrictions;
            AgeSignalsController.Instance.OnError -= OnError;
        }
    }
}
```

### Step 2 — Read cached flags anytime

Once `CheckAgeSignals()` completes, restriction flags are cached in `PlayerPrefs`. You can read them anywhere:

```csharp
var flags = AgeSignalsController.Instance.CurrentFlags;

if (flags != null && !flags.PersonalizedAdsEnabled)
{
    // User is under the personalized ads threshold
    ShowContextualAdsOnly();
}
```

### Step 3 — Async/await usage

For modern C# workflows, use the async API with structured error handling:

```csharp
using BizSim.GPlay.AgeSignals;
using UnityEngine;

public class AsyncAgeGate : MonoBehaviour
{
    async void Start()
    {
        try
        {
            var flags = await AgeSignalsController.Instance.CheckAgeSignalsAsync();

            if (flags.FullAccessGranted)
                EnableAllFeatures();
            else if (!flags.PersonalizedAdsEnabled)
                SwitchToContextualAds();
        }
        catch (AgeSignalsException ex)
        {
            Debug.LogWarning($"Age check failed: {ex.Error.ErrorCodeName}");
            // Fallback flags are still available via CurrentFlags
        }
    }
}
```

### Step 4 — Editor testing with mock data

1. Create a mock config: **Assets → Create → BizSim → Age Signals Mock Config**
2. Set the desired `MockStatus` (e.g., `Supervised`, `Unknown`)
3. Assign it to the `AgeSignalsController` component's `MockConfig` field
4. Enter Play Mode — the controller returns mock data instead of calling the real API

## Dependency Injection Support

The controller implements `IAgeSignalsProvider`, allowing you to decouple your game logic from the singleton and inject it via any DI framework.

### Zenject Example

```csharp
using BizSim.GPlay.AgeSignals;
using Zenject;

public class AgeSignalsInstaller : MonoInstaller
{
    [SerializeField] private AgeSignalsController _prefab;

    public override void InstallBindings()
    {
        Container.Bind<IAgeSignalsProvider>()
                 .FromComponentInNewPrefab(_prefab)
                 .AsSingle()
                 .NonLazy();
    }
}
```

### VContainer Example

```csharp
using BizSim.GPlay.AgeSignals;
using VContainer;
using VContainer.Unity;

public class AgeSignalsScope : LifetimeScope
{
    [SerializeField] private AgeSignalsController _prefab;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInNewPrefab<AgeSignalsController>(_prefab)
               .As<IAgeSignalsProvider>();
    }
}
```

### Unit Testing (no DI needed)

```csharp
using BizSim.GPlay.AgeSignals;
using NUnit.Framework;

public class MockAgeProvider : IAgeSignalsProvider
{
    public event Action<AgeRestrictionFlags> OnRestrictionsUpdated;
    public event Action<AgeSignalsError> OnError;
    public AgeRestrictionFlags CurrentFlags { get; set; }
    public bool IsChecking => false;

    public void CheckAgeSignals()
    {
        OnRestrictionsUpdated?.Invoke(CurrentFlags);
    }

    public Task<AgeRestrictionFlags> CheckAgeSignalsAsync(float timeoutSeconds = 30f)
    {
        return Task.FromResult(CurrentFlags);
    }
}

[TestFixture]
public class GameFeatureTests
{
    [Test]
    public void Under13_DisablesPersonalizedAds()
    {
        var mock = new MockAgeProvider
        {
            CurrentFlags = new AgeRestrictionFlags { PersonalizedAdsEnabled = false }
        };

        // Inject mock into your system under test
        var sut = new AdManager(mock);
        Assert.IsFalse(sut.ShouldShowPersonalizedAds);
    }
}
```

## API Reference

| Type | Description |
|------|-------------|
| `IAgeSignalsProvider` | Interface for DI and unit testing — implemented by `AgeSignalsController` |
| `AgeSignalsController` | Singleton MonoBehaviour — entry point (`CheckAgeSignals()` + `CheckAgeSignalsAsync()`) |
| `AgeSignalsException` | Exception thrown by `CheckAgeSignalsAsync()` on failure |
| `AgeVerificationStatus` | Enum: `Verified`, `Supervised`, `SupervisedApprovalPending`, `SupervisedApprovalDenied`, `Unknown`, `NotApplicable` |
| `AgeSignalsResult` | Raw API response (in-memory only, never persisted) |
| `AgeRestrictionFlags` | Behavior flags derived from result (cached in PlayerPrefs) |
| `AgeSignalsError` | Error details with `errorCode`, `errorMessage`, `isRetryable` |
| `AgeSignalsDecisionLogic` | Pluggable ScriptableObject with configurable feature list and age thresholds |
| `AgeFeature` | Feature definition with `key`, `label`, `minAge`, `requiresAdult` |
| `FeatureFlagEntry` | Key-value pair for feature flag state (`key`, `enabled`) |
| `AgeFeatureKeys` | Static class with well-known feature key constants (`Gambling`, `Marketplace`, `Chat`) |
| `AgeSignalsMockConfig` | ScriptableObject for Editor testing |

## Data Safety & Privacy

This package is designed with privacy compliance in mind:
- **Raw age data** is ephemeral (in-memory only)
- **Restriction flags** auto-expire after 24 hours
- **Analytics** log only binary success/error — no age data

For detailed data safety declarations (Google Play Console form), see:
- [`Documentation~/DATA_SAFETY.md`](Documentation~/DATA_SAFETY.md)
- [`Documentation~/PRIVACY_MANIFEST.xml`](Documentation~/PRIVACY_MANIFEST.xml)

## Requirements

- Unity 6000.3+
- Android target platform
- Google Play Age Signals library v0.0.2 (resolved automatically via EDM4U)

### Input System Compatibility

The runtime debug menu (`AgeSignalsDebugMenu`) supports both Unity's legacy Input Manager and the new Input System package. The assembly definition uses `versionDefines` to detect `com.unity.inputsystem` at compile time — no manual configuration needed. Works with all three Player Settings modes: **Old**, **New**, and **Both**.

## ⚠️ Beta Dependency Warning

This package depends on **`com.google.android.play:age-signals:0.0.2`**, which is a **Google Beta SDK**. Be aware of the following risks:

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Breaking API changes** | Java bridge calls may fail after a Google SDK update | Pin the version in `Dependencies.xml`; test after each update |
| **Gradle version conflicts** | Other Google Play libraries may pull a different version | Run **Assets → External Dependency Manager → Android Resolver → Force Resolve** after adding/updating any Google Play dependency |
| **Deprecation without notice** | Beta SDKs have no long-term support guarantee | Monitor [Google Play Age Signals release notes](https://developer.android.com/games/agesignals) for status changes |
| **Limited device coverage** | Beta rollout may not be available on all devices/regions | The controller gracefully falls back to cached flags when the API is unavailable |

### Gradle Force Resolve

If you encounter duplicate class errors or version conflicts after adding this package alongside other Google Play libraries (e.g., Play Integrity, Play Review), run:

1. **Assets → External Dependency Manager → Android Resolver → Force Resolve**
2. Check `Assets/Plugins/Android/mainTemplate.gradle` for conflicting version entries
3. If conflicts persist, add an explicit version override in your project's `Dependencies.xml`:
   ```xml
   <dependencies>
     <androidPackages>
       <androidPackage spec="com.google.android.play:age-signals:0.0.2" />
     </androidPackages>
   </dependencies>
   ```

> **Tip:** Third-party ad network SDKs (IronSource, AdMob, AppLovin, etc.) may transitively pull a different version of `com.google.android.play:*` libraries. After adding or updating any ad SDK, check the Gradle build log for `Duplicate class` or `Could not resolve` warnings. In Android Studio: **Build → Build Output** or run `./gradlew dependencies` to inspect the resolved dependency tree.
