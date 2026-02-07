# Third-Party Notices

This package depends on third-party libraries that are **not bundled** with the package.
They are resolved at build time via EDM4U (External Dependency Manager for Unity) or
Gradle dependency injection.

---

## Google Play Age Signals SDK

- **Library:** `com.google.android.play:age-signals:0.0.2`
- **Copyright:** Copyright Google LLC
- **Status:** Beta
- **License:** [Play Core Software Development Kit Terms of Service](https://developer.android.com/guide/playcore/license)
  (in addition to the [Google APIs Terms of Service](https://developers.google.com/terms))

By using this package, you agree to the Play Core SDK Terms of Service. Key terms:

- You may copy and distribute the Redistributable Code solely for inclusion as part of your app
- You may **not** modify, translate, or create derivative works of the Redistributable Code
- Google and its licensors retain all intellectual property rights

The Age Signals SDK is resolved at build time via EDM4U and the Google Maven repository
(`maven.google.com`). It is not shipped with this package.

---

## Unity Editor APIs

This package uses Unity Editor APIs (`UnityEditor` namespace) for custom inspectors and
configuration windows. These APIs are subject to the
[Unity Software Additional Terms](https://unity.com/legal/terms-of-service/software).

---

## Open Source Notices in Your App

Google requires that apps using Google Play libraries display open source notices to end users.
See [Include open source notices](https://developers.google.com/android/guides/opensource)
for instructions on using the `oss-licenses-plugin` Gradle plugin.
