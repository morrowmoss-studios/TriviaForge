using UnityEngine;
using UnityEngine.SceneManagement;

public class WinSceneManager : MonoBehaviour
{
    // Prevents double-registration if this scene is visited more than once
    private static bool _scoreRegisteredThisSession = false;

    private void Start()
    {
        RegisterSessionToFirebase();
    }

    // ── Firebase score submission ─────────────────────────────────────────

    private void RegisterSessionToFirebase()
    {
        if (_scoreRegisteredThisSession) return;
        if (PlayerDatabaseAPI.IsGuest)   return;

        var player = PlayerDatabaseAPI.GetCurrentPlayer();
        if (player == null) return;

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;

        // Core score registration
        PlayerDatabaseAPI.RegisterScore(
            player.playerId,
            player.displayName,
            score,
            TriviaSessionData.selectedGameMode,
            TriviaSessionData.selectedCategoryId,
            TriviaSessionData.selectedSubcategoryId ?? ""
        );

        // Game-specific stats
        int  maxStreak       = ScoreManager.Instance != null ? ScoreManager.Instance.MaxStreakThisGame      : 0;
        int  correct         = ScoreManager.Instance != null ? ScoreManager.Instance.CorrectAnswersThisGame : 0;
        int  total           = ScoreManager.Instance != null ? ScoreManager.Instance.TotalAnswersThisGame   : 0;
        bool isPerfect       = IsPerfectSolve();

        PlayerDatabaseAPI.RegisterGameStats(
            highestStreakThisGame:  maxStreak,
            perfectSolve:          isPerfect,
            correctAnswers:        correct,
            totalAnswers:          total,
            crosswordTimeSeconds:  0   // wire up timer here when ready
        );

        _scoreRegisteredThisSession = true;

        Debug.Log($"[WinSceneManager] Session registered — score={score} streak={maxStreak} perfect={isPerfect}");
    }

    /// <summary>
    /// A perfect solve is a crossword or wordoku completed with zero hints used.
    /// Trivia does not count as a perfect solve.
    /// </summary>
    private bool IsPerfectSolve()
    {
        string mode = TriviaSessionData.selectedGameMode;

        if (mode == "Crossword")
        {
            var board = FindObjectOfType<CrosswordBoardManager>();
            return board != null && board.UsedNoHints;
        }

        if (mode == "Wordoku")
        {
            // Hook up WordokuBoardManager.UsedNoHints here when that script exists
            return false;
        }

        return false;
    }

    // ── Buttons ───────────────────────────────────────────────────────────

    public void OnNextPressed()
    {
        // Reset the registration flag for the next game
        _scoreRegisteredThisSession = false;

        // Clear session so a fresh question list is built next round
        TriviaSessionData.sessionQuestions     = null;
        TriviaSessionData.currentQuestionIndex = 0;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetScore();

        switch (TriviaSessionData.selectedGameMode)
        {
            case "Trivia":
                SceneManager.LoadScene("TriviaMode");
                break;
            case "Wordoku":
                SceneManager.LoadScene("WordokuMode");
                break;
            case "Crossword":
                SceneManager.LoadScene("CrosswordMode");
                break;
            default:
                SceneManager.LoadScene("ModeSelect");
                break;
        }
    }

    public void OnQuitPressed()
    {
        _scoreRegisteredThisSession = false;
        SceneManager.LoadScene("Quit_PopUp");
    }
}