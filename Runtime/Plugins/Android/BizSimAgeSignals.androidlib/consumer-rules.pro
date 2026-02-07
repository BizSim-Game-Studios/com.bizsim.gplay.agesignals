# Consumer ProGuard/R8 rules for com.bizsim.gplay.agesignals
# These rules are automatically applied to the consuming app's minification pass.

# Google Play Age Signals API (Google's SDK)
-keep class com.google.android.play.agesignals.** { *; }

# BizSim Age Signals Bridge (JNI bridge class invoked via UnitySendMessage)
# Note: checkAgeSignalsWithFake() is intentionally NOT kept here.
# It is only called from C# in debug builds (Debug.isDebugBuild), so R8 can
# safely tree-shake it and its FakeAgeSignalsManager dependencies from release APKs.
-keep class com.bizsim.gplay.agesignals.AgeSignalsBridge {
    public static void checkAgeSignals(...);
    public static void cleanup();
}
