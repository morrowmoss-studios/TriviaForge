using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject in TriviaMode.
/// Wire Streak_Text and Multiplier_Text in the inspector.
/// </summary>
public class StreakUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI multiplierText;

    // Flame sprite sitting as a child of Multiplier_Text
    private Image flameImage;

    private void Start()
    {
        // Auto-find by name if not assigned in inspector
        if (streakText == null)
        {
            var all = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in all)
                if (t.gameObject.name == "Streak_Text") { streakText = t; break; }
        }

        if (multiplierText == null)
        {
            var all = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in all)
                if (t.gameObject.name == "Multiplier_Text") { multiplierText = t; break; }
        }

        // Grab the flame Image child of Multiplier_Text
        if (multiplierText != null)
            flameImage = multiplierText.GetComponentInChildren<Image>(true);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= Refresh;
    }

    private void Refresh()
    {
        if (ScoreManager.Instance == null) return;

        int streak       = ScoreManager.Instance.CurrentStreak;
        float multiplier = ScoreManager.Instance.CurrentMultiplier;

        // Streak text — hide at 0
        if (streakText != null)
        {
            streakText.text = streak > 0 ? streak.ToString() : "";
            streakText.gameObject.SetActive(streak > 0);
        }

        // Flame image — only visible when streak is active
        if (flameImage != null)
            flameImage.gameObject.SetActive(streak > 0);

        // Multiplier text — blank at 1x
        if (multiplierText != null)
        {
            if (multiplier >= 2f)
                multiplierText.text = "x2";
            else if (multiplier >= 1.5f)
                multiplierText.text = "x1.5";
            else
                multiplierText.text = "";
        }
    }
}