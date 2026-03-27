using TMPro;
using UnityEngine;

/// <summary>
/// Attach to any GameObject in TriviaMode.
/// Wire Streak_Text and Multiplier_Text in the inspector.
/// </summary>
public class StreakUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private TextMeshProUGUI multiplierText;

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

        // Streak display — hide at 0, show flame + count above 0
        if (streakText != null)
        {
            if (streak > 0)
            {
                streakText.text = $"🔥 {streak}";
                streakText.gameObject.SetActive(true);
            }
            else
            {
                streakText.text = "";
                streakText.gameObject.SetActive(false);
            }
        }

        // Multiplier display — hide at 1x, show at 1.5x and 2x
        if (multiplierText != null)
        {
            if (multiplier > 1f)
                multiplierText.text = $"{multiplier}x";
            else
                multiplierText.text = "";
        }
    }
}