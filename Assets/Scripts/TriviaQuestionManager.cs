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

    [Header("Ads")]
    [SerializeField] private AdsPopUpController adsPopUp;
    
    [Header("Back Confirm Panel")]
    [SerializeField] private GameObject backConfirmPanel;
    [SerializeField] private string modeSelectSceneName = "ModeSelect";

    private List<Question> questions = new List<Question>();

    private int   currentQuestionIndex = 0;
    private bool  questionLocked       = false;
    private bool  hintUsed             = false;

    private float timeRemaining = 20f;
    private bool  timerRunning  = false;
    private int   _lastHapticSecond = -1;

    // ── Timer duration by difficulty ──────────────────────────────────────

    private float GetTimerDuration(string difficulty = null)
    {
        string diff = difficulty ?? TriviaSessionData.selectedDifficulty;
        switch (diff.ToLowerInvariant())
        {
            case "easy":     return 30f;
            case "medium":   return 25f;
            case "hard":     return 20f;
            case "insanity": return 15f;
            case "mixed":    return 22f;
            default:         return 30f;
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (scoreUI == null)
            scoreUI = FindObjectOfType<TriviaScoreUI>();

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
        if (!HowToTrivia.LaunchedFromSettings && HowToTrivia.ShouldShowHowTo())
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("HowTo_Trivia");
            return;
        }
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

            // Deduplicate by ID first, then by question text as a fallback
            // This catches same-ID dupes and same-text dupes with different IDs
            var seenIds   = new HashSet<string>();
            var seenTexts = new HashSet<string>();
            var deduped   = new List<TriviaEntry>();

            foreach (var entry in orderedQuestions)
            {
                string normText = entry.questionText?.Trim().ToLowerInvariant() ?? "";

                if (!string.IsNullOrEmpty(entry.id) && seenIds.Contains(entry.id))   continue;
                if (!string.IsNullOrEmpty(normText)  && seenTexts.Contains(normText)) continue;

                if (!string.IsNullOrEmpty(entry.id))   seenIds.Add(entry.id);
                if (!string.IsNullOrEmpty(normText))   seenTexts.Add(normText);

                deduped.Add(entry);
            }

            if (deduped.Count < orderedQuestions.Count)
                Debug.Log($"[TriviaQuestionManager] Removed {orderedQuestions.Count - deduped.Count} duplicate question(s) from session.");

            TriviaSessionData.sessionQuestions     = deduped;
            TriviaSessionData.currentQuestionIndex = 0;
            TriviaSessionData.totalQuestions       = deduped.Count;

            Debug.Log($"[TriviaQuestionManager] {deduped.Count} questions cached " +
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

        if (TriviaSessionData.savedTimeRemaining > 0f)
        {
            timeRemaining = TriviaSessionData.savedTimeRemaining;
            TriviaSessionData.savedTimeRemaining = -1f;

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
        else if (timeRemaining <= 5f)
        {
            int currentSecond = Mathf.CeilToInt(timeRemaining);
            if (currentSecond != _lastHapticSecond && currentSecond >= 1 && currentSecond <= 5)
            {
                _lastHapticSecond = currentSecond;
                HapticManager.CountdownTick();
            }
        }
    }

    // ── Settings button ───────────────────────────────────────────────────

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
        TriviaSessionData.chosenIndex  = -1;
        TriviaSessionData.wasCorrect   = false;

        int justAnswered = currentQuestionIndex;
        TriviaSessionData.currentQuestionIndex = currentQuestionIndex + 1;

        if (TriviaSessionData.sessionQuestions != null &&
            justAnswered < TriviaSessionData.sessionQuestions.Count)
        {
            SeenContentTracker.MarkTriviaAsSeen(
                TriviaSessionData.sessionQuestions[justAnswered]);
        }

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

    // ── Question building ─────────────────────────────────────────────────

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

    // ── Load question ─────────────────────────────────────────────────────

    private void LoadQuestion(int index)
    {
        questionLocked     = false;
        hintUsed           = false;
        _lastHapticSecond  = -1;

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

        // Use the actual question's difficulty for the timer so cascading questions get the right time
        string questionDifficulty = null;
        if (TriviaSessionData.sessionQuestions != null && index < TriviaSessionData.sessionQuestions.Count)
            questionDifficulty = TriviaSessionData.sessionQuestions[index].difficulty;

        timeRemaining = GetTimerDuration(questionDifficulty);
        timerRunning  = true;

        if (timerText != null)
        {
            timerText.text  = Mathf.CeilToInt(timeRemaining).ToString();
            timerText.color = normalTimerColor;
        }
    }

    // ── Answer click ──────────────────────────────────────────────────────

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

        int justAnswered = currentQuestionIndex;
        TriviaSessionData.currentQuestionIndex = currentQuestionIndex + 1;

        if (TriviaSessionData.sessionQuestions != null &&
            justAnswered < TriviaSessionData.sessionQuestions.Count)
        {
            SeenContentTracker.MarkTriviaAsSeen(
                TriviaSessionData.sessionQuestions[justAnswered]);
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

    // ── Hint ─────────────────────────────────────────────────────────────

    public void OnHintPressed()
    {
        if (questionLocked) return;

        if (hintUsed)
        {
            if (adsPopUp != null)
                adsPopUp.Show(AdsPopUpController.GameMode.Trivia, GrantAdHint);
            else
                Debug.Log("[TriviaQuestionManager] Hint already used and no ad popup wired.");
            return;
        }

        UseHint();
    }

    private void GrantAdHint()
    {
        hintUsed = false;
        UseHint();
    }

    private void UseHint()
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
    
    public void OnBackButton()
    {
        if (backConfirmPanel != null)
            backConfirmPanel.SetActive(true);
        else
            SceneManager.LoadScene(modeSelectSceneName);
    }

    public void OnBackConfirmPressed()
    {
        if (backConfirmPanel != null)
            backConfirmPanel.SetActive(false);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    public void OnBackCancelPressed()
    {
        if (backConfirmPanel != null)
            backConfirmPanel.SetActive(false);
    }
}