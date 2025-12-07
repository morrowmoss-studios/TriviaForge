using System.Collections.Generic;
using UnityEngine;
using TMPro;
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

    void Start()
    {
        if (questions.Count > 0)
        {
            LoadQuestion(0);
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
                string answerText = q.answers[i];
                answerButtons[i].Init(this, i, letters[i], answerText);
            }
        }
    }

    public void OnAnswerClicked(AnswerButtonUI button)
    {
        if (questionLocked) return;
        questionLocked = true;

        Question q = questions[currentQuestionIndex];

        // First reset all outlines
        foreach (var b in answerButtons)
            b.ResetOutline();

        if (button.answerIndex == q.correctIndex)
        {
            Debug.Log("Correct!");
            button.ShowAsCorrect();
            // TODO: NextQuestion() after delay
        }
        else
        {
            Debug.Log("Wrong!");
            button.ShowAsWrong();

            // highlight the real correct one
            var correctButton = answerButtons[q.correctIndex];
            if (correctButton != null)
                correctButton.ShowAsCorrect();
        }
    }

}
