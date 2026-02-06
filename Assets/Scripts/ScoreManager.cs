using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring Settings")]
    [SerializeField] private int basePointsPerCorrect = 100;
    [SerializeField] private int streakBonusPerQuestion = 10;
    [SerializeField] private int timeBonusMultiplier = 5; // points per second left (if you use timers)

    private int currentScore;
    private int currentStreak;

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
        currentStreak = 0;
    }

    public void RegisterCorrectAnswer(float timeRemainingSeconds = 0f)
    {
        currentStreak++;

        int points = basePointsPerCorrect;

        // streak bonus: 10, 20, 30...
        points += currentStreak * streakBonusPerQuestion;

        // optional time bonus
        if (timeRemainingSeconds > 0f)
        {
            points += Mathf.RoundToInt(timeRemainingSeconds * timeBonusMultiplier);
        }

        currentScore += points;
    }

    public void RegisterWrongAnswer()
    {
        // You can choose to subtract points if you want:
        // currentScore = Mathf.Max(0, currentScore - basePointsPerCorrect / 2);

        currentStreak = 0;
    }

    public int GetCurrentScore() => currentScore;
}