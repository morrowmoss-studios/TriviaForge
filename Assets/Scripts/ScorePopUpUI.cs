using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScorePopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreValueText;  // Number_Text in Scores_PopUp

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName   = "MainMenu";
    [SerializeField] private string leaderboardSceneName = "Leaderboard_PopUp";
    [SerializeField] private string scoreSceneName       = "Scores_PopUp";
    [SerializeField] private string triviaSceneName      = "TriviaMode";
    [SerializeField] private string modeSelectSceneName  = "ModeSelect";

    private int finalScore;

    private void Start()
    {
        // Only do score display if we actually have a text box assigned
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
            // Totally fine in GameOver_PopUp or Leaderboard where we just use this as a nav helper
            Debug.Log("[ScorePopupUI] No scoreValueText assigned in this scene. Skipping score display.");
        }
    }

    // --- Scores_PopUp: buttons ---

    public void OnBackToMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void OnGoToLeaderboard()
    {
        if (!string.IsNullOrEmpty(leaderboardSceneName))
        {
            SceneManager.LoadScene(leaderboardSceneName);
        }
    }

    // --- GameOver_PopUp: Next -> Scores_PopUp ---

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

    // --- Leaderboard: Continue / Mode Select ---

    // Continue -> back into TriviaMode for a new run
    public void OnGoToTriviaMode()
    {
        // start a fresh run
        TriviaSessionData.currentQuestionIndex = 0;

        if (!string.IsNullOrEmpty(triviaSceneName))
        {
            SceneManager.LoadScene(triviaSceneName);
        }
        else
        {
            Debug.LogWarning("[ScorePopupUI] triviaSceneName is empty.");
        }
    }

    // Mode Select -> back to ModeSelect scene
    public void OnGoToModeSelect()
    {
        if (!string.IsNullOrEmpty(modeSelectSceneName))
        {
            SceneManager.LoadScene(modeSelectSceneName);
        }
        else
        {
            Debug.LogWarning("[ScorePopupUI] modeSelectSceneName is empty.");
        }
    }
}
