# Data Safety Declaration

This document describes the data practices of the **Google Play Age Signals Bridge** package
(`com.bizsim.gplay.agesignals`) for compliance with Google Play Data Safety requirements.

## Data Collected

| Data Type | Collected? | Shared? | Purpose | Retention |
|-----------|-----------|---------|---------|-----------|
| Age range (lower/upper bounds) | Yes (from Google Play API) | No | Age-appropriate content gating | **In-memory only** — never persisted to disk |
| Verification status | Yes (from Google Play API) | No | Age-appropriate content gating | **In-memory only** — never persisted to disk |
| Install ID | Yes (for supervised accounts only) | No | Supervised account identification | **In-memory only** — never persisted to disk |
| Restriction flags (behavior booleans) | Derived locally | No | Feature gating decisions | `PlayerPrefs`, auto-expires after **24 hours** |
| API call success/error | Yes (if `AGESIGNALS_FIREBASE` enabled) | Firebase Analytics | Technical monitoring | Per Firebase Analytics retention policy |

## Data NOT Collected

- ❌ No personal information (name, email, phone)
- ❌ No device identifiers (IMEI, advertising ID)
- ❌ No location data
- ❌ No financial information
- ❌ No health or fitness data
- ❌ No browsing history or search queries

## Key Privacy Principles

### 1. Raw Age Data — Memory Only
The `AgeSignalsResult` object (containing age range, verification status, and install ID)
is **never written to disk**, database, or transmitted to any server. It exists only in
RAM during the decision-making process and is discarded immediately after
`AgeRestrictionFlags` are computed.

### 2. Restriction Flags — 24-Hour TTL
`AgeRestrictionFlags` are saved to `PlayerPrefs` as boolean feature flags.
These flags contain **zero raw age data** — only behavioral decisions like
"can show personalized ads" or "casino feature enabled". Flags are automatically
expired and deleted after 24 hours to comply with Google's policy against
long-term storage of age-signal-derived data.

### 3. Analytics — No Age Data
When Firebase Analytics integration is enabled (`AGESIGNALS_FIREBASE` define symbol),
the only event logged is `age_signals_api_call` with a single parameter: `result`
containing the string `"success"` or `"error"`. No age data, verification status,
or restriction flags are included in analytics events.

### 4. Network Requests
The package makes exactly one network request per app launch to the Google Play
Age Signals API via the device's Google Play Services. This request is initiated
by the Google Play SDK — the package does not make any direct HTTP requests
or connect to any external servers.

## Google Play Data Safety Form

When filling out the Google Play Console Data Safety form:

| Question | Answer |
|----------|--------|
| Does your app collect or share user data? | Yes |
| Data type: Personal info → Age | **Collected** (from Google Play API) |
| Is the data collected, shared, or both? | Collected only |
| Is data processing ephemeral? | **Yes** — raw age data is ephemeral (in-memory only) |
| Is data collection required or optional? | Required (for age-appropriate content compliance) |
| Purpose | App functionality — age-appropriate content gating |

## Contact

For privacy-related questions about this package:
- **Author:** Aşkın Ceyhan (https://github.com/AskinCeyhan)
- **Company:** BizSim Game Studios (https://www.bizsim.com)
