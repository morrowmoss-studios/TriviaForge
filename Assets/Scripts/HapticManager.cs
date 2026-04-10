using UnityEngine;

/// <summary>
/// Centralised haptic feedback manager.
/// All haptics are gated by the TF_VibrationOn PlayerPref so the
/// Settings toggle works automatically without any extra wiring.
///
/// Usage from any script:
///   HapticManager.LightTap();
///   HapticManager.WrongAnswer();
///   HapticManager.TimerWarning();
/// </summary>
public static class HapticManager
{
    private const string VibKey = "TF_VibrationOn";

    private static bool IsEnabled =>
        PlayerPrefs.GetInt(VibKey, 1) == 1;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Short tap for buttons and tile placement.</summary>
    public static void LightTap()
    {
        if (!IsEnabled) return;
        Vibrate(25);
    }

    /// <summary>Stronger buzz for correct answers.</summary>
    public static void CorrectAnswer()
    {
        if (!IsEnabled) return;
        Vibrate(50);
    }

    /// <summary>Heavy buzz for wrong answers and strikes.</summary>
    public static void WrongAnswer()
    {
        if (!IsEnabled) return;
        Vibrate(100);
    }

    /// <summary>
    /// Three short pulses for the timer warning (last 3 seconds).
    /// Call this once when the timer hits 3 -- it fires three buzzes
    /// with short gaps using a coroutine on a temporary GameObject.
    /// </summary>
    public static void TimerWarning()
    {
        if (!IsEnabled) return;

        // Fire on a temp MonoBehaviour since static classes can't run coroutines
        var go = new GameObject("HapticPulse");
        var runner = go.AddComponent<HapticPulseRunner>();
        runner.Run(3, 80, 0.25f);
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

/// <summary>
/// Temporary MonoBehaviour spawned by HapticManager.TimerWarning()
/// to run the pulse coroutine. Self-destructs when done.
/// </summary>
public class HapticPulseRunner : MonoBehaviour
{
    public void Run(int pulses, long durationMs, float gapSeconds)
    {
        StartCoroutine(PulseCoroutine(pulses, durationMs, gapSeconds));
    }

    private System.Collections.IEnumerator PulseCoroutine(
        int pulses, long durationMs, float gapSeconds)
    {
        for (int i = 0; i < pulses; i++)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator =
                    activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
            catch { }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
            yield return new WaitForSeconds(gapSeconds);
        }

        Destroy(gameObject);
    }
}