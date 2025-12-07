using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TriviaResultScene : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    void Start()
    {
        // Read from the global backpack
        bool wasCorrect       = TriviaSessionData.lastWasCorrect;
        string playerAnswer   = TriviaSessionData.lastPlayerAnswerLabel;
        string correctAnswer  = TriviaSessionData.lastCorrectAnswerLabel;

        if (titleText != null)
        {
            titleText.text = wasCorrect ? "Correct!" : "Wrong!";
        }

        if (bodyText != null)
        {
            if (wasCorrect)
            {
                bodyText.text =
                    $"You chose:\n{correctAnswer}\n\n" +
                    $"You nailed it.";
            }
            else
            {
                bodyText.text =
                    $"You chose:\n{playerAnswer}\n\n" +
                    $"Correct answer:\n{correctAnswer}";
            }
        }
    }

    public void OnNextPressed()
    {
        // move to the next question
        TriviaSessionData.currentQuestionIndex++;

        SceneManager.LoadScene("TriviaMode");  // name of your trivia gameplay scene
    }

    public void OnQuitPressed()
    {
        // reset or not, up to you
        TriviaSessionData.currentQuestionIndex = 0;

        SceneManager.LoadScene("GameSetup");   // or MainMenu, whatever your hub scene is
    }
}