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
        public string[] answers = new string[4];   // A, B, C, D
        public int correctIndex;                   // 0–3
    }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI questionLabel;      // text at the top of the frame
    [SerializeField] private AnswerButtonUI[] answerButtons;     // size 4, A–D in order

    [Header("Outline Sprites")]
    [SerializeField] public Sprite rightOutlineSprite;          // teal (Answer_Bubble_Right)
    [SerializeField] public Sprite wrongOutlineSprite;          // orange (Answer_Bubble_Wrong)

    [Header("Scoring")]
    [SerializeField] private TriviaScoreUI scoreUI;              // drag ScoreNumbers (with TriviaScoreUI) here

    private List<Question> questions = new List<Question>();

    private int currentQuestionIndex = 0;
    private bool questionLocked = false;
    private bool hintUsed = false;

    private void Awake()
    {
        if (scoreUI == null)
        {
            scoreUI = FindObjectOfType<TriviaScoreUI>();
            if (scoreUI == null)
            {
                Debug.LogWarning("[TriviaQuestionManager] Could not find TriviaScoreUI in scene.");
            }
        }
    }

    private void Start()
    {
        string categoryId    = TriviaSessionData.selectedCategoryId;    // e.g. "science" or "mixed_all"
        string subcategoryId = TriviaSessionData.selectedSubcategoryId; // e.g. "biology"
        string difficulty    = TriviaSessionData.selectedDifficulty;    // "Easy", "Medium", "Mixed", etc.

        Debug.Log($"[TriviaQuestionManager] Loading trivia for " +
                  $"{TriviaSessionData.selectedCategory} ({categoryId}) / " +
                  $"{TriviaSessionData.selectedSubcategory} ({subcategoryId}) " +
                  $"at difficulty {difficulty}");

        List<TriviaEntry> dbTrivia;

        // Mixed category: pull from EVERYWHERE
        if (string.Equals(categoryId, "mixed_all", System.StringComparison.OrdinalIgnoreCase))
        {
            dbTrivia = GameDatabaseAPI.GetAllTrivia();
        }
        else
        {
            dbTrivia = GameDatabaseAPI.GetTrivia(categoryId, subcategoryId);
        }

        // Difficulty filter (except "Mixed")
        if (!string.Equals(difficulty, "Mixed", System.StringComparison.OrdinalIgnoreCase))
        {
            dbTrivia = dbTrivia
                .Where(e => string.Equals(e.difficulty, difficulty, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        questions.Clear();

        if (dbTrivia != null && dbTrivia.Count > 0)
        {
            foreach (var entry in dbTrivia)
            {
                Question q = BuildQuestionFromEntry(entry); // shuffles answers
                questions.Add(q);
            }
        }

        if (questions.Count == 0)
        {
            Debug.LogError("[TriviaQuestionManager] No questions available after DB load + difficulty filter.");
            return;
        }

        // Randomize the order of questions
        ShuffleQuestions(questions);

        TriviaSessionData.currentQuestionIndex = 0;
        TriviaSessionData.totalQuestions      = questions.Count;

        LoadQuestion(TriviaSessionData.currentQuestionIndex);

        Debug.Log("Game Mode: " + TriviaSessionData.selectedGameMode);
        Debug.Log("Category: " + TriviaSessionData.selectedCategory);
        Debug.Log("Subcategory: " + TriviaSessionData.selectedSubcategory);
    }

    // ---------- QUESTION BUILDING & SHUFFLING ----------

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

            if (i == correctIndex)
            {
                correctIndex = j;
            }
            else if (j == correctIndex)
            {
                correctIndex = i;
            }
        }
    }

    private void ShuffleQuestions(List<Question> list)
    {
        if (list == null || list.Count <= 1) return;

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            if (i == j) continue;

            Question temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ---------- LOAD QUESTION INTO UI ----------

    private void LoadQuestion(int index)
    {
        questionLocked = false;
        hintUsed = false;

        if (index < 0 || index >= questions.Count)
        {
            Debug.LogError($"[TriviaQuestionManager] LoadQuestion index out of range: {index}");
            return;
        }

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

        // Optional debug:
        // Debug.Log($"Q: {q.questionText}\nCorrect: {q.answers[q.correctIndex]} (index {q.correctIndex})");
    }

    // ---------- ANSWER CLICK ----------

    public void OnAnswerClicked(AnswerButtonUI button)
    {
        if (questionLocked) return;
        questionLocked = true;

        if (button == null)
        {
            Debug.LogError("[TriviaQuestionManager] OnAnswerClicked got null button.");
            return;
        }

        Question q = questions[currentQuestionIndex];

        // Save stuff for the result scene
        TriviaSessionData.questionText = q.questionText;
        TriviaSessionData.correctIndex = q.correctIndex;
        TriviaSessionData.chosenIndex  = button.answerIndex;
        TriviaSessionData.wasCorrect   = (button.answerIndex == q.correctIndex);
        TriviaSessionData.currentQuestionIndex = currentQuestionIndex;

        for (int i = 0; i < q.answers.Length && i < TriviaSessionData.answers.Length; i++)
        {
            TriviaSessionData.answers[i] = q.answers[i];
        }

        // Scoring
        bool isCorrect = (button.answerIndex == q.correctIndex);

        if (ScoreManager.Instance != null)
        {
            // correct no hint -> +5, correct with hint -> +3, wrong -> 0
            ScoreManager.Instance.RegisterAnswer(isCorrect, hintUsed);
        }

        if (scoreUI != null)
        {
            scoreUI.UpdateScoreText();
        }

        // Visual feedback
        if (isCorrect)
        {
            button.ShowAsCorrect();
        }
        else
        {
            button.ShowAsWrong();
        }

        // Jump to the result screen
        SceneManager.LoadScene("TriviaResult");
    }

    // ---------- HINT BUTTON ----------

    public void OnHintPressed()
    {
        if (hintUsed || questionLocked) return;

        Question q = questions[currentQuestionIndex];
        List<int> wrongIndexes = new List<int>();

        // Find all wrong answers
        for (int i = 0; i < q.answers.Length; i++)
        {
            if (i != q.correctIndex)
                wrongIndexes.Add(i);
        }

        if (wrongIndexes.Count == 0)
        {
            Debug.LogWarning("[TriviaQuestionManager] No wrong answers to eliminate.");
            return;
        }

        // Pick one wrong answer to disable
        int eliminateIndex = wrongIndexes[Random.Range(0, wrongIndexes.Count)];

        if (eliminateIndex >= 0 && eliminateIndex < answerButtons.Length &&
            answerButtons[eliminateIndex] != null)
        {
            answerButtons[eliminateIndex].DisableAnswer();
            Debug.Log($"[TriviaQuestionManager] Hint used! Disabled answer at index {eliminateIndex}");
        }

        hintUsed = true;
    }
}