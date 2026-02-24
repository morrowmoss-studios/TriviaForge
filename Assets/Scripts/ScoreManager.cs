using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring Settings")]
    [SerializeField] private int pointsForCorrectNoHint   = 5;
    [SerializeField] private int pointsForCorrectWithHint = 3;

    private int currentScore;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[ScoreManager] Awake OK. Singleton set + DontDestroyOnLoad.");
    }

    public void ResetScore()
    {
        currentScore = 0;
        Debug.Log("[ScoreManager] ResetScore -> 0");
    }

    /// <summary>
    /// Adds points based on correctness + whether a hint was used.
    /// Wrong answers always give 0.
    /// </summary>
    public void RegisterAnswer(bool isCorrect, bool usedHint)
    {
        if (!isCorrect)
        {
            Debug.Log("[ScoreManager] RegisterAnswer: wrong answer (+0). Total=" + currentScore);
            return;
        }

        int add = usedHint ? pointsForCorrectWithHint : pointsForCorrectNoHint;
        currentScore += add;

        Debug.Log($"[ScoreManager] RegisterAnswer: correct={isCorrect} hint={usedHint} +{add} Total={currentScore}");
    }

    public int GetCurrentScore() => currentScore;
}