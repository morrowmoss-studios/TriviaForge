using UnityEngine;

/// <summary>
/// Centralised haptic feedback manager.
/// Haptics only fire for meaningful negative/urgent feedback:
///   - Wrong answers and scarlet letters of shame
///   - Timer countdown (5 to 1)
///
/// All haptics are gated by the TF_VibrationOn PlayerPref so the
/// Settings toggle works automatically without any extra wiring.
/// </summary>
public static class HapticManager
{
    private const string VibKey = "TF_VibrationOn";

    private static bool IsEnabled =>
        PlayerPrefs.GetInt(VibKey, 1) == 1;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Heavy buzz for wrong answers and scarlet letters of shame.</summary>
    public static void WrongAnswer()
    {
        if (!IsEnabled) return;
        Vibrate(100);
    }

    /// <summary>Single short pulse for each countdown tick (5, 4, 3, 2, 1).</summary>
    public static void CountdownTick()
    {
        if (!IsEnabled) return;
        Vibrate(60);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private static void Vibrate(long milliseconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidVibrate(milliseconds);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#else
        Debug.Log($"[HapticManager] Vibrate({milliseconds}ms) — editor, skipped.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void AndroidVibrate(long milliseconds)
    {
        try
        {
            using (AndroidJavaClass unityPlayer =
                new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject vibrator =
                        activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HapticManager] Android vibrate failed: {ex.Message}");
        }
    }
#endif
}