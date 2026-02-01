# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.1] - 2026-02-01

Professional quality improvements for release builds and developer experience.

### Added
- **BizSimLogger** — Zero-allocation conditional logging with `[Conditional("DEBUG")]`
  - Verbose, Info, Warning, Error log levels
  - Compiler strips all Info/Warning calls in Release builds
  - Configurable `MinLevel` for runtime filtering
- **AgeSignalsErrorCode** enum — Type-safe error handling with 11 error types
  - Replaces magic number error codes with readable enum values
  - `AgeSignalsError.Type` property for switch/if statements
  - IntelliSense support for error handling

### Changed
- Replaced 16 `Debug.Log` calls with `BizSimLogger` throughout `AgeSignalsController`
  - 10 Info-level logs (normal flow, results, cache)
  - 3 Warning-level logs (retries, analytics failures)
  - 3 Error-level logs (JNI exceptions, parse failures)
- Updated `AgeSignalsError.ErrorCodeName` to use enum-based ToString()

### Performance
- **Zero logging overhead** in Release builds (all Info/Warning calls compiled out)
- **Zero string allocation** for disabled log levels

## [0.1.0] - 2026-01-30

Initial release with full Age Signals API bridge, dynamic feature system, and Editor tooling.

### Added

#### Core
- Java bridge (`AgeSignalsBridge.java`) for Google Play Age Signals API v0.0.2
- C# data models: `AgeVerificationStatus`, `AgeSignalsResult`, `AgeRestrictionFlags`, `AgeSignalsError`, `AgeSignalsException`
- `AgeSignalsController` — singleton MonoBehaviour with automatic retry and exponential backoff
- `IAgeSignalsProvider` — interface for dependency injection and unit testing
- `CheckAgeSignals()` and `CheckAgeSignalsAsync()` with timeout and `CancellationToken` support
- Decision engine converting raw age data into policy-compliant behavior flags
- PlayerPrefs persistence with 24-hour TTL expiration

#### Dynamic Feature System
- `AgeFeature` and `FeatureFlagEntry` data classes
- `AgeRestrictionFlags.IsFeatureEnabled(key)` and `SetFeature(key, enabled)` API
- `AgeFeatureKeys` — static class with well-known feature key constants (`Gambling`, `Marketplace`, `Chat`)
- `AgeSignalsDecisionLogic` — pluggable ScriptableObject with configurable feature list
- Runtime debug validation for unknown feature keys (Editor / Development builds)

#### Editor Tooling
- Custom Inspector for `AgeSignalsController` with card-based layout and live results
- Custom Inspector for `AgeSignalsDecisionLogic` with feature summary table and usage guide
- Custom Inspector for `AgeSignalsMockConfig` with JSON preview and error simulation
- `AgeSignalsMockConfig` — ScriptableObject for Editor-only mock API responses
- `AgeSignalsDebugMenu` — runtime IMGUI overlay (F9 / 5-tap toggle, debug builds only)

#### Build & Integration
- EDM4U dependency declaration (`Dependencies.xml`)
- ProGuard / R8 keep rules via `.androidlib`
- Optional Firebase Analytics via `AGESIGNALS_FIREBASE` scripting define symbol
- Compatible with both legacy Input Manager and new Input System via `versionDefines`
- Java bridge compatible with Age Signals SDK v0.0.2 `@IntDef` API (no enum dependency)

#### Documentation & Samples
- `README.md` with quick start, DI examples, and API reference
- `SECURITY.md` vulnerability reporting policy
- `NOTICES.md` third-party attribution (Apache 2.0)
- Sample: BasicIntegration (`AgeGateSample.cs`)
- Sample: MockPresets (`CreateMockPresets.cs`)
- Test suite: data model, JSON round-trip, and decision logic tests
