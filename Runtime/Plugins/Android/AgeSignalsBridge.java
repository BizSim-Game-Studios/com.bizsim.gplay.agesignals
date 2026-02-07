// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

package com.bizsim.gplay.agesignals;

import android.app.Activity;
import android.util.Log;

import com.google.android.play.agesignals.AgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsManagerFactory;
import com.google.android.play.agesignals.AgeSignalsRequest;
import com.google.android.play.agesignals.AgeSignalsResult;
import com.google.android.play.agesignals.AgeSignalsException;
import com.google.android.play.agesignals.model.AgeSignalsVerificationStatus;
// NOTE: FakeAgeSignalsManager is loaded via reflection to avoid a hard dependency
// on the testing artifact, which may be stripped from production AAR builds.
// A static import would cause NoClassDefFoundError / VerifyError at class-load time.

import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

/**
 * Java bridge for the Google Play Age Signals API.
 * Called from Unity C# via {@code AndroidJavaClass} JNI calls.
 * Results are returned asynchronously through {@code UnityPlayer.UnitySendMessage}.
 *
 * <p><b>Privacy:</b> This bridge forwards raw age data to C#.
 * The C# layer is responsible for converting it to behavior flags
 * WITHOUT persisting the raw data.</p>
 */
public class AgeSignalsBridge {

    private static final String TAG = "AgeSignalsBridge";

    // =================================================================
    // PRODUCTION — Real API call
    // =================================================================

    /**
     * Checks age signals for the current user using the real Google Play API.
     *
     * @param gameObjectName Unity GameObject name that will receive callbacks
     * @param successMethod  C# method name for the success callback
     * @param errorMethod    C# method name for the error callback
     */
    public static void checkAgeSignals(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        checkAgeSignalsInternal(gameObjectName, successMethod, errorMethod,
                false, null, -1, -1);
    }

    // =================================================================
    // TESTING — FakeAgeSignalsManager for controlled responses
    // =================================================================

    /**
     * Checks age signals using Google's {@code FakeAgeSignalsManager} for testing.
     * Exercises the full Java-to-C# bridge path on a real device without requiring
     * an actual age-verified Google account.
     *
     * @param gameObjectName Unity GameObject name that will receive callbacks
     * @param successMethod  C# method name for the success callback
     * @param errorMethod    C# method name for the error callback
     * @param useFake        true to use FakeAgeSignalsManager
     * @param fakeStatus     Status to return ("VERIFIED", "SUPERVISED", etc.)
     * @param fakeAgeLower   Lower age bound (-1 for null)
     * @param fakeAgeUpper   Upper age bound (-1 for null)
     */
    public static void checkAgeSignalsWithFake(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeStatus,
            final int fakeAgeLower,
            final int fakeAgeUpper) {

        checkAgeSignalsInternal(gameObjectName, successMethod, errorMethod,
                useFake, fakeStatus, fakeAgeLower, fakeAgeUpper);
    }

    // =================================================================
    // INTERNAL IMPLEMENTATION
    // =================================================================

    private static void checkAgeSignalsInternal(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeStatus,
            final int fakeAgeLower,
            final int fakeAgeUpper) {

        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            sendError(gameObjectName, errorMethod, -100, "Unity activity is null");
            return;
        }

        try {
            AgeSignalsManager manager;

            if (useFake) {
                // Test mode: use Google's official FakeAgeSignalsManager via reflection.
                // The testing class may be stripped from the production AAR, so we load it
                // dynamically to avoid NoClassDefFoundError at class-verification time.
                try {
                    Class<?> fakeClass = Class.forName(
                            "com.google.android.play.agesignals.testing.FakeAgeSignalsManager");
                    Object fakeInstance = fakeClass.getDeclaredConstructor().newInstance();

                    // Build the fake result
                    AgeSignalsResult.Builder builder = AgeSignalsResult.builder();
                    int status = parseVerificationStatus(fakeStatus);
                    if (status >= 0) {
                        builder.setUserStatus(status);
                    }
                    if (fakeAgeLower >= 0) builder.setAgeLower(fakeAgeLower);
                    if (fakeAgeUpper >= 0) builder.setAgeUpper(fakeAgeUpper);

                    // fakeManager.setNextAgeSignalsResult(result)
                    java.lang.reflect.Method setResult = fakeClass.getMethod(
                            "setNextAgeSignalsResult", AgeSignalsResult.class);
                    setResult.invoke(fakeInstance, builder.build());

                    manager = (AgeSignalsManager) fakeInstance;

                    Log.d(TAG, "Using FakeAgeSignalsManager (reflection): status=" + fakeStatus
                            + " age=[" + fakeAgeLower + "-" + fakeAgeUpper + "]");
                } catch (Exception e) {
                    Log.e(TAG, "FakeAgeSignalsManager not available (testing artifact may be stripped)", e);
                    sendError(gameObjectName, errorMethod, -100,
                            "FakeAgeSignalsManager not available: " + e.getMessage());
                    return;
                }
            } else {
                manager = AgeSignalsManagerFactory.create(activity);
            }

            manager.checkAgeSignals(AgeSignalsRequest.builder().build())
                .addOnSuccessListener(activity, result -> {
                    try {
                        JSONObject json = new JSONObject();

                        // userStatus — explicit mapping for forward compatibility
                        // (avoids depending on enum.toString() behavior)
                        json.put("userStatus",
                            result.userStatus() != null
                                ? mapUserStatus(result.userStatus())
                                : JSONObject.NULL);

                        // Age range — only meaningful for supervised accounts
                        json.put("ageLower",
                            result.ageLower() != null ? result.ageLower() : JSONObject.NULL);
                        json.put("ageUpper",
                            result.ageUpper() != null ? result.ageUpper() : JSONObject.NULL);

                        // Install ID — only present for supervised installs
                        json.put("installId",
                            result.installId() != null ? result.installId() : JSONObject.NULL);

                        // Most recent parental approval date
                        json.put("mostRecentApprovalDate",
                            result.mostRecentApprovalDate() != null
                                ? result.mostRecentApprovalDate().getTime()
                                : JSONObject.NULL);

                        Log.d(TAG, "Age signals result: " + json.toString());
                        UnityPlayer.UnitySendMessage(
                            gameObjectName, successMethod, json.toString());

                    } catch (Exception e) {
                        Log.e(TAG, "Result serialization failed", e);
                        sendError(gameObjectName, errorMethod, -100,
                            "Serialization error: " + e.getMessage());
                    }
                })
                .addOnFailureListener(activity, exception -> {
                    int errorCode = -100;
                    String errorMessage = exception.getMessage();

                    if (exception instanceof AgeSignalsException) {
                        errorCode = ((AgeSignalsException) exception).getErrorCode();
                    }

                    Log.e(TAG, "Age signals error: code=" + errorCode
                        + " msg=" + errorMessage);
                    sendError(gameObjectName, errorMethod, errorCode, errorMessage);
                });

        } catch (Throwable e) {
            // Catches both Exception and Error (e.g., NoClassDefFoundError
            // when the Google Play SDK is not bundled via EDM4U).
            Log.e(TAG, "Failed to create AgeSignalsManager", e);
            sendError(gameObjectName, errorMethod, -100,
                "Manager creation failed: " + e.getMessage());
        }
    }

    // =================================================================
    // LIFECYCLE
    // =================================================================

    /**
     * Called from Unity's OnDestroy to release any held references.
     * Currently a no-op since the bridge is stateless, but provided
     * as a stable API for future resource cleanup.
     */
    public static void cleanup() {
        Log.d(TAG, "cleanup() called");
    }

    // =================================================================
    // HELPER METHODS
    // =================================================================

    /**
     * Maps the SDK verification status int constant to a stable string representation.
     * Uses explicit mapping for forward compatibility with future SDK versions.
     */
    private static String mapUserStatus(Integer status) {
        if (status == null) return null;

        if (status == AgeSignalsVerificationStatus.VERIFIED)
            return "VERIFIED";
        if (status == AgeSignalsVerificationStatus.SUPERVISED)
            return "SUPERVISED";
        if (status == AgeSignalsVerificationStatus.SUPERVISED_APPROVAL_PENDING)
            return "SUPERVISED_APPROVAL_PENDING";
        if (status == AgeSignalsVerificationStatus.SUPERVISED_APPROVAL_DENIED)
            return "SUPERVISED_APPROVAL_DENIED";
        if (status == AgeSignalsVerificationStatus.UNKNOWN)
            return "UNKNOWN";

        Log.w(TAG, "Unknown userStatus: " + status);
        return String.valueOf(status); // Forward-compatible: pass through unknown values
    }

    /**
     * Parses a string status into the SDK's {@code AgeSignalsVerificationStatus} int constant.
     * Used only for the fake/test path.
     *
     * @return the matching int constant, or -1 if unrecognized
     */
    private static int parseVerificationStatus(String status) {
        if (status == null) return -1;
        switch (status) {
            case "VERIFIED":
                return AgeSignalsVerificationStatus.VERIFIED;
            case "SUPERVISED":
                return AgeSignalsVerificationStatus.SUPERVISED;
            case "SUPERVISED_APPROVAL_PENDING":
                return AgeSignalsVerificationStatus.SUPERVISED_APPROVAL_PENDING;
            case "SUPERVISED_APPROVAL_DENIED":
                return AgeSignalsVerificationStatus.SUPERVISED_APPROVAL_DENIED;
            case "UNKNOWN":
                return AgeSignalsVerificationStatus.UNKNOWN;
            default:
                return -1;
        }
    }

    /**
     * Sends an error response back to Unity via {@code UnitySendMessage}.
     * The JSON payload contains {@code errorCode}, {@code errorMessage}, and {@code isRetryable}.
     */
    private static void sendError(String gameObjectName, String errorMethod,
                                   int errorCode, String errorMessage) {
        try {
            JSONObject json = new JSONObject();
            json.put("errorCode", errorCode);
            json.put("errorMessage", errorMessage != null ? errorMessage : "Unknown error");
            json.put("isRetryable", isRetryable(errorCode));
            UnityPlayer.UnitySendMessage(gameObjectName, errorMethod, json.toString());
        } catch (Exception e) {
            Log.e(TAG, "Failed to send error to Unity", e);
        }
    }

    /**
     * Determines if an error code represents a transient failure that can be retried.
     * Error codes -1 through -8 are considered transient (network, service binding, etc.).
     * Error codes -9 (APP_NOT_OWNED) and -100 (INTERNAL_ERROR) are permanent.
     */
    private static boolean isRetryable(int errorCode) {
        return errorCode >= -8 && errorCode <= -1;
    }
}
