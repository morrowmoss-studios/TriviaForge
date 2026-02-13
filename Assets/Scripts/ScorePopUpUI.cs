using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScorePopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreValueText;  // Number_Text in Scores_PopUp

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string leaderboardSceneName = "Leaderboard_PopUp";
    [SerializeField] private string scoreSceneName = "Scores_PopUp";  // for generic use

    private int finalScore;

    private void Start()
    {
        // Only try to show the score if we actually have a text field
        if (scoreValueText != null)
        {
            if (ScoreManager.Instance != null)
            {
                finalScore = ScoreManager.Instance.GetCurrentScore();
            }
            else
            {
                Debug.LogWarning("[ScorePopupUI] ScoreManager.Instance is NULL, defaulting score to 0.");
                finalScore = 0;
            }

            scoreValueText.text = finalScore.ToString();
        }
        else
        {
            // In scenes like GameOver_PopUp where we just reuse this script for navigation,
            // it's totally fine that there's no text.
            Debug.Log("[ScorePopupUI] No scoreValueText assigned in this scene. Skipping score display.");
        }
    }

    // Used in Scores_PopUp for going back to menu
    public void OnBackToMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    // Used in Scores_PopUp for going to leaderboard
    public void OnGoToLeaderboard()
    {
        if (!string.IsNullOrEmpty(leaderboardSceneName))
        {
            SceneManager.LoadScene(leaderboardSceneName);
        }
    }

    // 🔹 NEW: generic "go to score scene" method we can use from GameOver_PopUp
    public void OnGoToScoreScene()
    {
        if (!string.IsNullOrEmpty(scoreSceneName))
        {
            SceneManager.LoadScene(scoreSceneName);
        }
        else
        {
            Debug.LogWarning("[ScorePopupUI] scoreSceneName is empty.");
        }
    }
}
