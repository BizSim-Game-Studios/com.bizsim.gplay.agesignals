# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability in this package, please report it responsibly:

1. **Do not** open a public GitHub issue
2. Email: **security@bizsim.com**
3. Include: package name, version, description of the vulnerability, and steps to reproduce

We will acknowledge your report within 48 hours and provide a fix timeline within 7 days.

## Scope

This package handles age verification data with the following security considerations:

- **Raw age data** (`AgeSignalsResult`) is kept in memory only — never persisted to disk
- **Behavior flags** (`AgeRestrictionFlags`) are cached in PlayerPrefs — contain no raw age data
- **Analytics events** log only binary success/failure — no age information is transmitted
- **Firebase integration** is optional and gated behind `AGESIGNALS_FIREBASE` scripting define
- **ProGuard rules** are embedded to prevent reverse engineering of the Java bridge
