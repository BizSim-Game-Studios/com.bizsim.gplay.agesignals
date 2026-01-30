# Basic Integration Sample

This sample demonstrates a minimal Age Signals integration for a Unity game.

## Contents

| File | Description |
|------|-------------|
| `AgeGateSample.cs` | MonoBehaviour that checks age signals on launch and configures features |

## Setup

1. Import this sample via **Package Manager → Google Play Age Signals Bridge → Samples → Import**
2. Create a new scene or open your startup scene
3. Add a GameObject with `AgeSignalsController` component
4. Add another GameObject with `AgeGateSample` component
5. (Optional) Assign UI panels to the `_accessDeniedScreen` and `_verificationPrompt` fields
6. Enter Play Mode — the controller returns mock data in the Editor

## What This Sample Shows

- Subscribing to `OnRestrictionsUpdated` and `OnError` events
- Using `IAgeSignalsProvider` interface for testability
- Feature gating with `AgeFeatureKeys` constants (`IsFeatureEnabled(AgeFeatureKeys.Gambling)`)
- Graceful error handling with fallback to cached flags
