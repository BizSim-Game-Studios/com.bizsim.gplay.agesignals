# Mock Config Presets

Pre-configured `AgeSignalsMockConfig` assets for common testing scenarios.

## Included Presets

| Preset | Status | Age Range | Use Case |
|--------|--------|-----------|----------|
| **VerifiedAdult** | Verified | 25–35 | Full access, all features enabled |
| **SupervisedChild** | Supervised | 10–12 | Under-13 restrictions, no personalized ads |
| **SupervisedTeen** | Supervised | 14–16 | 13+ features enabled, 18+ restricted |
| **UnknownUser** | Unknown | -1 (none) | Needs verification prompt |
| **AccessDenied** | SupervisedApprovalDenied | 8–10 | Parental approval rejected — block access |
| **NetworkError** | — | — | Simulates transient network error (retryable) |

## Setup

1. Import this sample via **Package Manager → Samples → Mock Presets → Import**
2. Assign any preset to the `AgeSignalsController`'s **Mock Config** field in the Inspector
3. Enter Play Mode — the controller returns mock data matching the preset
