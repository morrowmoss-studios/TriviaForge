using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring Settings")]
    [SerializeField] private int pointsForCorrectNoHint   = 5;
    [SerializeField] private int pointsForCorrectWithHint = 3;

    private int currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetScore()
    {
        currentScore = 0;
    }

    /// <summary>
    /// Adds points based on correctness + whether a hint was used.
    /// Wrong answers always give 0.
    /// </summary>
    public void RegisterAnswer(bool isCorrect, bool usedHint)
    {
        if (!isCorrect) return; // wrong = 0

        if (usedHint)
            currentScore += pointsForCorrectWithHint;
        else
            currentScore += pointsForCorrectNoHint;
    }

    public int GetCurrentScore() => currentScore;
}