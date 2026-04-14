using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScorePopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreValueText;

    [Header("Mode Panels")]
    [SerializeField] private GameObject triviaPanel;
    [SerializeField] private GameObject wordokuPanel;
    [SerializeField] private GameObject crosswordPanel;

    [Header("Trivia Panel")]
    [SerializeField] private TextMeshProUGUI scoreNumberText;
    [SerializeField] private TextMeshProUGUI streakNumberText;

    [Header("Wordoku Panel")]
    [SerializeField] private TextMeshProUGUI shameNumText;
    [SerializeField] private TextMeshProUGUI timerNumText;

    [Header("Crossword Panel")]
    [SerializeField] private TextMeshProUGUI cwShameNumText;
    [SerializeField] private TextMeshProUGUI cwPerfectGameText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName    = "MainMenu";
    [SerializeField] private string leaderboardSceneName = "Leaderboard_PopUp";
    [SerializeField] private string scoreSceneName       = "Scores_PopUp";
    [SerializeField] private string triviaSceneName      = "TriviaMode";
    [SerializeField] private string modeSelectSceneName  = "ModeSelect";

    private int finalScore;

    private void Start()
    {
        // Hide all panels first
        if (triviaPanel   != null) triviaPanel.SetActive(false);
        if (wordokuPanel  != null) wordokuPanel.SetActive(false);
        if (crosswordPanel != null) crosswordPanel.SetActive(false);

        string mode = TriviaSessionData.selectedGameMode;

        if (mode == "Trivia")
        {
            if (triviaPanel != null) triviaPanel.SetActive(true);

            finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
            int streak = ScoreManager.Instance != null ? ScoreManager.Instance.MaxStreakThisGame : 0;

            if (scoreNumberText  != null) scoreNumberText.text  = finalScore.ToString();
            if (streakNumberText != null) streakNumberText.text = streak.ToString();

            // Legacy support -- keep scoreValueText updated if it exists
            if (scoreValueText != null) scoreValueText.text = finalScore.ToString();
        }
        else if (mode == "Wordoku")
        {
            if (wordokuPanel != null) wordokuPanel.SetActive(true);

            int   shame   = TriviaSessionData.wordokuWrongPlacements;
            float elapsed = TriviaSessionData.wordokuTimeSeconds;
            int   minutes = Mathf.FloorToInt(elapsed / 60f);
            int   seconds = Mathf.FloorToInt(elapsed % 60f);

            if (shameNumText != null) shameNumText.text = shame.ToString();
            if (timerNumText != null) timerNumText.text = $"{minutes}:{seconds:00}";
        }
        else if (mode == "Crossword")
        {
            if (crosswordPanel != null) crosswordPanel.SetActive(true);

            int  shame   = TriviaSessionData.crosswordWrongPlacements;
            bool perfect = TriviaSessionData.crosswordPerfectGame;

            if (cwShameNumText   != null) cwShameNumText.text   = shame.ToString();
            if (cwPerfectGameText != null) cwPerfectGameText.text = perfect ? "Y" : "N";
        }
        else
        {
            // Fallback -- show trivia panel
            if (triviaPanel != null) triviaPanel.SetActive(true);
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
        TriviaSessionData.ClearSession();

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