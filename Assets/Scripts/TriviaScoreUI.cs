using TMPro;
using UnityEngine;

public class TriviaScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreNumbersText;

    private void Awake()
    {
        // Auto-grab from same object if not assigned in Inspector
        if (scoreNumbersText == null)
        {
            scoreNumbersText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        // On first question of a run, reset score
        if (ScoreManager.Instance != null)
        {
            if (TriviaSessionData.currentQuestionIndex == 0)
            {
                ScoreManager.Instance.ResetScore();
            }
        }

        UpdateScoreText();
    }

    public void UpdateScoreText()
    {
        if (scoreNumbersText == null)
        {
            Debug.LogWarning("[TriviaScoreUI] scoreNumbersText is NULL, cannot display score.");
            return;
        }

        int score = 0;
        if (ScoreManager.Instance != null)
        {
            score = ScoreManager.Instance.GetCurrentScore();
        }
        else
        {
            Debug.LogWarning("[TriviaScoreUI] ScoreManager.Instance is NULL.");
        }

        scoreNumbersText.text = score.ToString();
        Debug.Log($"[TriviaScoreUI] Score text updated to {score}");
    }
}