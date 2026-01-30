# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in this package, please report it responsibly:

1. **DO NOT** open a public GitHub issue
2. Email **security@bizsim.com** with:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact assessment
3. You will receive an acknowledgment within **48 hours**
4. A fix will be prioritized based on severity

## Scope

This package handles age verification data from Google Play. Security concerns include:

- **Data leakage** — Raw age data (`AgeSignalsResult`) must never be persisted or transmitted
- **PlayerPrefs tampering** — Restriction flags stored locally can be modified by rooted devices
- **JNI bridge injection** — Malicious apps could send fake `UnitySendMessage` callbacks

## Design Mitigations

| Concern | Mitigation |
|---------|-----------|
| Age data persistence | `AgeSignalsResult` is in-memory only — never written to disk |
| Stale cached flags | Auto-expire after 24 hours (`FLAGS_MAX_AGE_HOURS`) |
| Analytics leakage | Only binary success/error logged — zero age data in events |
| Flag manipulation | Server-side validation recommended for critical features |

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | ✅ Current |
| < 0.1.0 | ❌ No longer supported |
