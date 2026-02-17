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

    [Header("Questions (runtime-filled from DB)")]
    public List<Question> questions = new List<Question>();

    [Header("Scoring")]
    public TriviaScoreUI scoreUI;

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
        // ✅ USE IDS, NOT PRETTY NAMES
        string categoryId    = TriviaSessionData.selectedCategoryId;    // e.g. "science"
        string subcategoryId = TriviaSessionData.selectedSubcategoryId; // e.g. "biology"

        Debug.Log($"[TriviaQuestionManager] Loading trivia for " +
                  $"{TriviaSessionData.selectedCategory} ({categoryId}) / " +
                  $"{TriviaSessionData.selectedSubcategory} ({subcategoryId})");

        // Ask the DB for entries
        List<TriviaEntry> dbTrivia = GameDatabaseAPI.GetTrivia(categoryId, subcategoryId);

        // Convert DB entries into our local Question list
        questions.Clear();

        if (dbTrivia != null && dbTrivia.Count > 0)
        {
            foreach (var entry in dbTrivia)
            {
                Question q = BuildQuestionFromEntry(entry);  // 🔹 shuffles + sets correctIndex
                questions.Add(q);
            }
        }

        else
        {
            Debug.LogWarning($"[TriviaQuestionManager] No trivia found for {categoryId}/{subcategoryId}.");
        }

        if (questions.Count == 0)
        {
            Debug.LogError("[TriviaQuestionManager] No questions available after DB load.");
            return;
        }

        // Clamp index, set total, and load first question
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

    // ... keep the rest of your script (LoadQuestion, OnAnswerClicked, OnHintPressed, etc.) as-is ...
    
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
    
    // Builds a Question from a TriviaEntry and shuffles the answers.
    private Question BuildQuestionFromEntry(TriviaEntry entry)
    {
        Question q = new Question
        {
            questionText = entry.questionText,
            answers      = (string[])entry.answers.Clone(),
            correctIndex = entry.correctIndex
        };

        // Shuffle answers and update correctIndex to match
        ShuffleAnswers(q.answers, ref q.correctIndex);
        return q;
    }

// Fisher–Yates shuffle that also tracks where the correct index moves.
    private void ShuffleAnswers(string[] answers, ref int correctIndex)
    {
        if (answers == null || answers.Length <= 1) return;

        for (int i = 0; i < answers.Length; i++)
        {
            int j = Random.Range(i, answers.Length); // UnityEngine.Random

            if (i == j) continue;

            // swap answers[i] and answers[j]
            string tmp = answers[i];
            answers[i] = answers[j];
            answers[j] = tmp;

            // update correctIndex if we touched it
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

}
