using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TriviaQuestionManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [TextArea] public string questionText;
        public string[] answers = new string[4];
        public int correctIndex;
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questionLabel;
    [SerializeField] private AnswerButtonUI[] answerButtons;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Color normalTimerColor = Color.yellow;
    [SerializeField] private Color urgentTimerColor = Color.red;
    [SerializeField] private float urgentThreshold  = 5f;

    [Header("Outline Sprites")]
    [SerializeField] public Sprite rightOutlineSprite;
    [SerializeField] public Sprite wrongOutlineSprite;

    [Header("Scoring")]
    [SerializeField] private TriviaScoreUI   scoreUI;
    [SerializeField] private TriviaStrikesUI strikesUI;

    private List<Question> questions = new List<Question>();

    private int   currentQuestionIndex = 0;
    private bool  questionLocked       = false;
    private bool  hintUsed             = false;

    private float timeRemaining = 20f;
    private bool  timerRunning  = false;

    // ── Timer duration by difficulty ──────────────────────────────────────

    private float GetTimerDuration()
    {
        switch (TriviaSessionData.selectedDifficulty)
        {
            case "Easy":     return 20f;
            case "Medium":   return 15f;
            case "Hard":     return 12f;
            case "Insanity": return 8f;
            case "Mixed":    return 15f;
            default:         return 20f;
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (scoreUI == null)
            scoreUI = FindObjectOfType<TriviaScoreUI>();

        // Auto-find Timer_Text by name if not assigned in inspector
        if (timerText == null)
        {
            foreach (var t in FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t.gameObject.name == "Timer_Text")
                {
                    timerText = t;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        // ── Build question list only once per game session ──────────────────
        if (TriviaSessionData.sessionQuestions == null)
        {
            string categoryId    = TriviaSessionData.selectedCategoryId;
            string subcategoryId = TriviaSessionData.selectedSubcategoryId;
            string difficulty    = TriviaSessionData.selectedDifficulty;

            Debug.Log($"[TriviaQuestionManager] Building fresh question list for " +
                      $"{TriviaSessionData.selectedCategory} / {TriviaSessionData.selectedSubcategory} " +
                      $"at difficulty {difficulty}");

            var tierOrder = new List<string> { "easy", "medium", "hard", "insanity" };

            string selectedDiff = difficulty.ToLowerInvariant();
            bool isMixedDiff    = selectedDiff == "mixed";

            List<TriviaEntry> dbTrivia;

            if (string.Equals(categoryId, "mixed_all", System.StringComparison.OrdinalIgnoreCase))
                dbTrivia = GameDatabaseAPI.GetAllTrivia();
            else
                dbTrivia = GameDatabaseAPI.GetTrivia(categoryId, subcategoryId);

            if (dbTrivia == null) dbTrivia = new List<TriviaEntry>();

            List<TriviaEntry> orderedQuestions = new List<TriviaEntry>();

            if (isMixedDiff)
            {
                dbTrivia = SeenContentTracker.FilterSeenTrivia(dbTrivia);
                ShuffleTriviaEntries(dbTrivia);
                orderedQuestions = dbTrivia;
            }
            else
            {
                int startTier = tierOrder.IndexOf(selectedDiff);
                if (startTier < 0) startTier = 0;

                for (int t = startTier; t < tierOrder.Count; t++)
                {
                    string tier = tierOrder[t];
                    var tierQuestions = dbTrivia
                        .Where(e => string.Equals(e.difficulty, tier,
                                                  System.StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    tierQuestions = SeenContentTracker.FilterSeenTrivia(tierQuestions);
                    ShuffleTriviaEntries(tierQuestions);
                    orderedQuestions.AddRange(tierQuestions);
                }
            }

            TriviaSessionData.sessionQuestions     = orderedQuestions;
            TriviaSessionData.currentQuestionIndex = 0;
            TriviaSessionData.totalQuestions       = orderedQuestions.Count;

            Debug.Log($"[TriviaQuestionManager] {orderedQuestions.Count} questions cached " +
                      $"(starting at {selectedDiff}, cascading up).");
        }
        else
        {
            Debug.Log($"[TriviaQuestionManager] Resuming session at question " +
                      $"{TriviaSessionData.currentQuestionIndex + 1} / " +
                      $"{TriviaSessionData.sessionQuestions.Count}");
        }

        questions.Clear();
        foreach (var entry in TriviaSessionData.sessionQuestions)
            questions.Add(BuildQuestionFromEntry(entry));

        if (questions.Count == 0)
        {
            Debug.LogError("[TriviaQuestionManager] No questions available.");
            return;
        }

        currentQuestionIndex = TriviaSessionData.currentQuestionIndex;
        LoadQuestion(currentQuestionIndex);

        // Restore timer if returning from Settings mid-question
        if (TriviaSessionData.savedTimeRemaining > 0f)
        {
            timeRemaining = TriviaSessionData.savedTimeRemaining;
            TriviaSessionData.savedTimeRemaining = -1f;

            // Update the display immediately so it doesn't flicker to full
            if (timerText != null)
            {
                timerText.text  = Mathf.CeilToInt(timeRemaining).ToString();
                timerText.color = timeRemaining <= urgentThreshold ? urgentTimerColor : normalTimerColor;
            }
        }

        if (strikesUI != null) strikesUI.Refresh();
    }

    // ── Timer update ──────────────────────────────────────────────────────

    private void Update()
    {
        if (!timerRunning || questionLocked) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text  = Mathf.CeilToInt(Mathf.Max(timeRemaining, 0f)).ToString();
            timerText.color = timeRemaining <= urgentThreshold ? urgentTimerColor : normalTimerColor;
        }

        if (timeRemaining <= 0f)
        {
            timerRunning = false;
            OnTimeExpired();
        }
    }

    // ── Settings button ───────────────────────────────────────────────────

    /// <summary>
    /// Call this from the Settings button OnClick BEFORE UIManager.OpenSettingsScene.
    /// Saves the current time and pauses the timer so it survives the scene reload.
    /// </summary>
    public void OnSettingsPressed()
    {
        TriviaSessionData.savedTimeRemaining = timeRemaining;
        timerRunning = false;
        Debug.Log($"[TriviaQuestionManager] Timer paused at {timeRemaining:F2}s before Settings.");
    }

    // ── Time expired ──────────────────────────────────────────────────────

    private void OnTimeExpired()
    {
        if (questionLocked) return;
        questionLocked = true;

        Question q = questions[currentQuestionIndex];

        TriviaSessionData.questionText = q.questionText;
        TriviaSessionData.correctIndex = q.correctIndex;
        TriviaSessionData.chosenIndex  = -1; // -1 = timed out
        TriviaSessionData.wasCorrect   = false;

        TriviaSessionData.currentQuestionIndex = currentQuestionIndex + 1;

        for (int i = 0; i < q.answers.Length && i < TriviaSessionData.answers.Length; i++)
            TriviaSessionData.answers[i] = q.answers[i];

        TriviaSessionData.roundOver = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrong();

        TriviaSessionData.strikes++;
        if (strikesUI != null) strikesUI.Refresh();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayStrike();

        Debug.Log($"[Trivia] Time expired — Strike {TriviaSessionData.strikes}/{TriviaSessionData.maxStrikes}");

        if (TriviaSessionData.strikes >= TriviaSessionData.maxStrikes)
            TriviaSessionData.roundOver = true;

        if (TriviaSessionData.currentQuestionIndex >= questions.Count)
            TriviaSessionData.roundOver = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterAnswer(false, hintUsed);

        if (scoreUI != null)
            scoreUI.UpdateScoreText();

        SceneManager.LoadScene("TriviaResult");
    }

    // ---------- QUESTION BUILDING ----------

    private Question BuildQuestionFromEntry(TriviaEntry entry)
    {
        Question q = new Question
        {
            questionText = entry.questionText,
            answers      = (string[])entry.answers.Clone(),
            correctIndex = entry.correctIndex
        };
        ShuffleAnswers(q.answers, ref q.correctIndex);
        return q;
    }

    private void ShuffleAnswers(string[] answers, ref int correctIndex)
    {
        if (answers == null || answers.Length <= 1) return;

        for (int i = 0; i < answers.Length; i++)
        {
            int j = Random.Range(i, answers.Length);
            if (i == j) continue;

            string tmp = answers[i];
            answers[i] = answers[j];
            answers[j] = tmp;

            if      (i == correctIndex) correctIndex = j;
            else if (j == correctIndex) correctIndex = i;
        }
    }

    private void ShuffleTriviaEntries(List<TriviaEntry> list)
    {
        if (list == null || list.Count <= 1) return;

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            if (i == j) continue;

            TriviaEntry temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ---------- LOAD QUESTION ----------

    private void LoadQuestion(int index)
    {
        questionLocked = false;
        hintUsed       = false;

        if (index < 0 || index >= questions.Count)
        {
            Debug.LogError($"[TriviaQuestionManager] LoadQuestion index out of range: {index}");
            return;
        }

        currentQuestionIndex = index;

        Question q = questions[index];

        if (questionLabel != null)
            questionLabel.text = q.questionText;

        string[] letters = { "A.", "B.", "C.", "D." };

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].ResetOutline();
                string answerText = i < q.answers.Length ? q.answers[i] : "";
                answerButtons[i].Init(this, i, letters[i], answerText);
            }
        }

        // Start the timer fresh for this question
        timeRemaining = GetTimerDuration();
        timerRunning  = true;

        if (timerText != null)
        {
            timerText.text  = Mathf.CeilToInt(timeRemaining).ToString();
            timerText.color = normalTimerColor;
        }
    }

    // ---------- ANSWER CLICK ----------

    public void OnAnswerClicked(AnswerButtonUI button)
    {
        if (questionLocked) return;
        questionLocked = true;
        timerRunning   = false;

        if (button == null)
        {
            Debug.LogError("[TriviaQuestionManager] OnAnswerClicked got null button.");
            return;
        }

        Question q = questions[currentQuestionIndex];

        TriviaSessionData.questionText = q.questionText;
        TriviaSessionData.correctIndex = q.correctIndex;
        TriviaSessionData.chosenIndex  = button.answerIndex;
        TriviaSessionData.wasCorrect   = (button.answerIndex == q.correctIndex);

        TriviaSessionData.currentQuestionIndex = currentQuestionIndex + 1;

        if (TriviaSessionData.sessionQuestions != null &&
            currentQuestionIndex < TriviaSessionData.sessionQuestions.Count)
        {
            SeenContentTracker.MarkTriviaAsSeen(
                TriviaSessionData.sessionQuestions[currentQuestionIndex]);
        }

        for (int i = 0; i < q.answers.Length && i < TriviaSessionData.answers.Length; i++)
            TriviaSessionData.answers[i] = q.answers[i];

        bool isCorrect = (button.answerIndex == q.correctIndex);

        TriviaSessionData.roundOver = false;

        if (isCorrect)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCorrect();
            button.ShowAsCorrect();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWrong();
            button.ShowAsWrong();

            TriviaSessionData.strikes++;
            if (strikesUI != null) strikesUI.Refresh();

            Debug.Log($"[Trivia] Strike {TriviaSessionData.strikes}/{TriviaSessionData.maxStrikes}");

            if (AudioManager.Instance != null) AudioManager.Instance.PlayStrike();

            if (TriviaSessionData.strikes >= TriviaSessionData.maxStrikes)
                TriviaSessionData.roundOver = true;
        }

        if (TriviaSessionData.currentQuestionIndex >= questions.Count)
            TriviaSessionData.roundOver = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterAnswer(isCorrect, hintUsed);

        if (scoreUI != null)
            scoreUI.UpdateScoreText();

        SceneManager.LoadScene("TriviaResult");
    }

    // ---------- HINT ----------

    public void OnHintPressed()
    {
        if (hintUsed || questionLocked) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIClick();

        Question q = questions[currentQuestionIndex];
        List<int> wrongIndexes = new List<int>();

        for (int i = 0; i < q.answers.Length; i++)
            if (i != q.correctIndex) wrongIndexes.Add(i);

        if (wrongIndexes.Count == 0) return;

        int eliminateIndex = wrongIndexes[Random.Range(0, wrongIndexes.Count)];

        if (eliminateIndex >= 0 && eliminateIndex < answerButtons.Length &&
            answerButtons[eliminateIndex] != null)
        {
            answerButtons[eliminateIndex].DisableAnswer();
        }

        hintUsed = true;
    }
}