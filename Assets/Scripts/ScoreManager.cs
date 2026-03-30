using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int   currentScore;
    private int   currentStreak;
    private int   maxStreakThisGame;
    private int   correctAnswersThisGame;
    private int   totalAnswersThisGame;
    private float currentMultiplier = 1f;

    public int   CurrentScore             => currentScore;
    public int   CurrentStreak            => currentStreak;
    public int   MaxStreakThisGame        => maxStreakThisGame;
    public int   CorrectAnswersThisGame   => correctAnswersThisGame;
    public int   TotalAnswersThisGame     => totalAnswersThisGame;
    public float CurrentMultiplier        => currentMultiplier;

    // Fired after every answer so StreakUI can refresh without polling
    public event System.Action OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[ScoreManager] Awake OK.");
    }

    public void ResetScore()
    {
        currentScore             = 0;
        currentStreak            = 0;
        maxStreakThisGame        = 0;
        correctAnswersThisGame   = 0;
        totalAnswersThisGame     = 0;
        currentMultiplier        = 1f;

        OnScoreChanged?.Invoke();
        Debug.Log("[ScoreManager] ResetScore -> 0");
    }

    // ── Base points by difficulty ─────────────────────────────────────────

    private int GetBasePoints()
    {
        switch (TriviaSessionData.selectedDifficulty)
        {
            case "Easy":     return 10;
            case "Medium":   return 20;
            case "Hard":     return 35;
            case "Insanity": return 50;
            case "Mixed":    return 20;
            default:         return 10;
        }
    }

    // ── Multiplier from streak ────────────────────────────────────────────

    private float GetMultiplierForStreak(int streak)
    {
        if (streak >= 5) return 2f;
        if (streak >= 3) return 1.5f;
        return 1f;
    }

    // ── Register answer ───────────────────────────────────────────────────

    /// <summary>
    /// Call this after every trivia answer.
    /// </summary>
    public void RegisterAnswer(bool isCorrect, bool usedHint)
    {
        totalAnswersThisGame++;

        if (!isCorrect)
        {
            currentStreak     = 0;
            currentMultiplier = 1f;

            Debug.Log($"[ScoreManager] Wrong answer — streak reset. Total={currentScore}");
            OnScoreChanged?.Invoke();
            return;
        }

        correctAnswersThisGame++;

        currentStreak++;
        if (currentStreak > maxStreakThisGame)
            maxStreakThisGame = currentStreak;

        currentMultiplier = GetMultiplierForStreak(currentStreak);

        int basePoints  = GetBasePoints();
        int hintPoints  = usedHint ? basePoints / 2 : basePoints;
        int finalPoints = Mathf.RoundToInt(hintPoints * currentMultiplier);

        currentScore += finalPoints;

        Debug.Log($"[ScoreManager] Correct! diff={TriviaSessionData.selectedDifficulty} " +
                  $"base={basePoints} hint={usedHint} streak={currentStreak} " +
                  $"multiplier={currentMultiplier}x +{finalPoints} Total={currentScore}");

        OnScoreChanged?.Invoke();
    }

    public int GetCurrentScore() => currentScore;
}