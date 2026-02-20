// Copyright (c) BizSim Game Studios. All rights reserved.

package com.bizsim.gplay.agesignals;

import android.app.Activity;
import android.util.Log;

import com.google.android.play.agesignals.AgeSignalsManager;
import com.google.android.play.agesignals.AgeSignalsManagerFactory;
import com.google.android.play.agesignals.AgeSignalsRequest;
import com.google.android.play.agesignals.AgeSignalsResult;
import com.google.android.play.agesignals.AgeSignalsException;
import com.google.android.play.agesignals.model.AgeSignalsVerificationStatus;
import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

public class AgeSignalsBridge {

    private static final String TAG = "AgeSignalsBridge";

    public static void checkAgeSignals(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        checkAgeSignalsInternal(gameObjectName, successMethod, errorMethod,
                false, null, -1, -1);
    }

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
                try {
                    Class<?> fakeClass = Class.forName(
                            "com.google.android.play.agesignals.testing.FakeAgeSignalsManager");
                    Object fakeInstance = fakeClass.getDeclaredConstructor().newInstance();

                    AgeSignalsResult.Builder builder = AgeSignalsResult.builder();
                    int status = parseVerificationStatus(fakeStatus);
                    if (status >= 0) {
                        builder.setUserStatus(status);
                    }
                    if (fakeAgeLower >= 0) builder.setAgeLower(fakeAgeLower);
                    if (fakeAgeUpper >= 0) builder.setAgeUpper(fakeAgeUpper);

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

                        json.put("userStatus",
                            result.userStatus() != null
                                ? mapUserStatus(result.userStatus())
                                : JSONObject.NULL);

                        json.put("ageLower",
                            result.ageLower() != null ? result.ageLower() : JSONObject.NULL);
                        json.put("ageUpper",
                            result.ageUpper() != null ? result.ageUpper() : JSONObject.NULL);

                        json.put("installId",
                            result.installId() != null ? result.installId() : JSONObject.NULL);

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
            Log.e(TAG, "Failed to create AgeSignalsManager", e);
            sendError(gameObjectName, errorMethod, -100,
                "Manager creation failed: " + e.getMessage());
        }
    }

    public static void cleanup() {
        Log.d(TAG, "cleanup() called");
    }

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

    private static boolean isRetryable(int errorCode) {
        return errorCode >= -8 && errorCode <= -1;
    }
}
