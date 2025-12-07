using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int currentQuestionIndex = 0;
    private bool questionLocked = false;
    
    private string BuildAnswerLabel(Question q, int index)
    {
        string[] letters = { "A.", "B.", "C.", "D." };
        return $"{letters[index]} {q.answers[index]}";
    }
    
    void Start()
    {
        if (questions.Count > 0)
        {
            // clamp the index just in case
            if (TriviaSessionData.currentQuestionIndex < 0 || 
                TriviaSessionData.currentQuestionIndex >= questions.Count)
            {
                TriviaSessionData.currentQuestionIndex = 0;
            }

            TriviaSessionData.totalQuestions = questions.Count; // ✅ add this line

            LoadQuestion(TriviaSessionData.currentQuestionIndex);
        }
        else
        {
            Debug.LogWarning("No questions set up on TriviaQuestionManager.");
        }
    }



    void LoadQuestion(int index)
    {
        questionLocked = false;
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

        if (button.answerIndex == q.correctIndex)
        {
            Debug.Log("Correct!");
            button.ShowAsCorrect();
        }
        else
        {
            Debug.Log("Wrong!");
            button.ShowAsWrong();
        }

        // 🔁 Jump to the result screen
        SceneManager.LoadScene("TriviaResult");
    }



}
