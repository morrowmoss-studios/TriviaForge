using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScorePopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreValueText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName    = "MainMenu";
    [SerializeField] private string leaderboardSceneName = "Leaderboard_PopUp";
    [SerializeField] private string scoreSceneName       = "Scores_PopUp";
    [SerializeField] private string triviaSceneName      = "TriviaMode";
    [SerializeField] private string modeSelectSceneName  = "ModeSelect";

    private int finalScore;

    private void Start()
    {
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
            Debug.Log("[ScorePopupUI] No scoreValueText assigned in this scene. Skipping score display.");
        }
    }

    // --- Scores_PopUp: buttons ---

    public void OnBackToMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnGoToLeaderboard()
    {
        if (!string.IsNullOrEmpty(leaderboardSceneName))
            SceneManager.LoadScene(leaderboardSceneName);
    }

    // --- GameOver_PopUp: Next -> Scores_PopUp ---

    public void OnGoToScoreScene()
    {
        if (!string.IsNullOrEmpty(scoreSceneName))
            SceneManager.LoadScene(scoreSceneName);
        else
            Debug.LogWarning("[ScorePopupUI] scoreSceneName is empty.");
    }

    // --- Leaderboard: Continue -> fresh TriviaMode run ---

    public void OnGoToTriviaMode()
    {
        // Full reset for a fresh run
        TriviaSessionData.currentQuestionIndex = 0;
        TriviaSessionData.strikes              = 0;
        TriviaSessionData.roundOver            = false;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetScore();

        if (!string.IsNullOrEmpty(triviaSceneName))
            SceneManager.LoadScene(triviaSceneName);
        else
            Debug.LogWarning("[ScorePopupUI] triviaSceneName is empty.");
    }

    // --- Leaderboard: Mode Select ---

    public void OnGoToModeSelect()
    {
        if (!string.IsNullOrEmpty(modeSelectSceneName))
            SceneManager.LoadScene(modeSelectSceneName);
        else
            Debug.LogWarning("[ScorePopupUI] modeSelectSceneName is empty.");
    }
}