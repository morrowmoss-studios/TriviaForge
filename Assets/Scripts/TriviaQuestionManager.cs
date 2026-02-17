using System.Collections.Generic;
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
        public int correctIndex;                   // 0 = A, 1 = B, etc.
    }

    [Header("UI References")]
    public TextMeshProUGUI questionLabel;      // text at the top of the frame
    public AnswerButtonUI[] answerButtons;     // size 4, A–D in order

    [Header("Outline Sprites")]
    public Sprite rightOutlineSprite;          // teal (Answer_Bubble_Right)
    public Sprite wrongOutlineSprite;          // orange (Answer_Bubble_Wrong)

    [Header("Questions")]
    public List<Question> questions = new List<Question>();

    [Header("Scoring")]
    public TriviaScoreUI scoreUI;              // drag ScoreNumbers (with TriviaScoreUI) here

    private int currentQuestionIndex = 0;
    private bool questionLocked = false;
    private bool hintUsed = false;

    // Optional helper (not strictly needed, but here if you want to use it)
    private string BuildAnswerLabel(Question q, int index)
    {
        string[] letters = { "A.", "B.", "C.", "D." };
        return $"{letters[index]} {q.answers[index]}";
    }

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
    // 1) Try to load questions from the JSON database
    string categoryId    = TriviaSessionData.selectedCategory;      // e.g. "science"
    string subcategoryId = TriviaSessionData.selectedSubcategory;   // e.g. "physics_quantum"

    Debug.Log($"[TriviaQuestionManager] Loading trivia for {categoryId}/{subcategoryId}");

    // Clear any inspector leftovers so we're only using DB data
    questions.Clear();

    // Ask the database for trivia entries
    var entries = GameDatabaseAPI.GetTrivia(categoryId, subcategoryId);

    if (entries != null && entries.Count > 0)
    {
        foreach (var e in entries)
        {
            // Safety: make sure we have exactly 4 answers
            if (e.answers == null || e.answers.Length != 4)
            {
                Debug.LogWarning($"[TriviaQuestionManager] Trivia entry '{e.id}' does not have exactly 4 answers. Skipping.");
                continue;
            }

            var q = new Question
            {
                questionText = e.questionText,
                answers      = e.answers,
                correctIndex = e.correctIndex
            };

            questions.Add(q);
        }
    }

    // 2) Now do the old "if questions.Count > 0" logic
    if (questions.Count > 0)
    {
        // clamp the index just in case
        if (TriviaSessionData.currentQuestionIndex < 0 ||
            TriviaSessionData.currentQuestionIndex >= questions.Count)
        {
            TriviaSessionData.currentQuestionIndex = 0;
        }

        TriviaSessionData.totalQuestions = questions.Count;

        LoadQuestion(TriviaSessionData.currentQuestionIndex);

        Debug.Log("Game Mode: " + TriviaSessionData.selectedGameMode);
        Debug.Log("Category: " + TriviaSessionData.selectedCategory);
        Debug.Log("Subcategory: " + TriviaSessionData.selectedSubcategory);
    }
    else
    {
        Debug.LogWarning("[TriviaQuestionManager] No questions available after loading from database.");
    }
}

    private void LoadQuestion(int index)
    {
        questionLocked = false;
        hintUsed = false; // 🔹 reset hint flag for this question
        currentQuestionIndex = index;

        Question q = questions[index];

        if (questionLabel != null)
            questionLabel.text = q.questionText;

        string[] letters = { "A.", "B.", "C.", "D." };

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                // reset outlines and text for each new question
                answerButtons[i].ResetOutline();
                answerButtons[i].Init(this, i, letters[i], q.answers[i]);
            }
        }
    }

    public void OnAnswerClicked(AnswerButtonUI button)
    {
        if (questionLocked) return;
        questionLocked = true;

        Question q = questions[currentQuestionIndex];

        // --- Save stuff for the result scene ---
        TriviaSessionData.questionText = q.questionText;
        TriviaSessionData.correctIndex = q.correctIndex;
        TriviaSessionData.chosenIndex = button.answerIndex;
        TriviaSessionData.wasCorrect = (button.answerIndex == q.correctIndex);
        TriviaSessionData.currentQuestionIndex = currentQuestionIndex;

        // copy answers so the result scene can show them
        for (int i = 0; i < q.answers.Length; i++)
        {
            TriviaSessionData.answers[i] = q.answers[i];
        }
        // --- end save ---

        // ---------- NEW SCORING ----------
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
        // ---------- end scoring ----------

        if (isCorrect)
        {
            Debug.Log("Correct!");
            button.ShowAsCorrect();
        }
        else
        {
            Debug.Log("Wrong!");
            button.ShowAsWrong();
        }

        // Jump to the result screen
        SceneManager.LoadScene("TriviaResult");
    }

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

        // Pick one wrong answer to disable
        int eliminateIndex = wrongIndexes[Random.Range(0, wrongIndexes.Count)];

        if (answerButtons[eliminateIndex] != null)
        {
            answerButtons[eliminateIndex].DisableAnswer();
            Debug.Log($"Hint used! Disabled answer at index {eliminateIndex}");
        }

        hintUsed = true;
    }
}
