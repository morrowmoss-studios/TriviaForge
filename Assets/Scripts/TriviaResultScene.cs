using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TriviaResultScene : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private void Start()
    {
        // Title: Correct / Wrong
        titleText.text = TriviaSessionData.wasCorrect ? "Correct!" : "Wrong!";

        int chosen  = TriviaSessionData.chosenIndex;
        int correct = TriviaSessionData.correctIndex;

        string chosenLetter  = IndexToLetter(chosen);
        string correctLetter = IndexToLetter(correct);

        string chosenText  = TriviaSessionData.answers[chosen];
        string correctText = TriviaSessionData.answers[correct];

        // You can style this however you want later
        bodyText.text =
            $"You chose:\n<b>{chosenLetter}. {chosenText}</b>\n\n" +
            $"Correct answer:\n<b>{correctLetter}. {correctText}</b>";
    }

    private string IndexToLetter(int index)
    {
        switch (index)
        {
            case 0: return "A";
            case 1: return "B";
            case 2: return "C";
            case 3: return "D";
            default: return "?";
        }
    }

    // called by Next button
    public void OnNextPressed()
    {
        TriviaSessionData.currentQuestionIndex++;

        if (TriviaSessionData.currentQuestionIndex >= TriviaSessionData.totalQuestions)
        {
            // out of questions -> send them back to mode select or main menu
            SceneManager.LoadScene("ModeSelect");   // <- change to your scene name
        }
        else
        {
            // go back to TriviaMode, which will load the next question
            SceneManager.LoadScene("TriviaMode");   // <- exact scene name
        }
    }

    // called by Quit button
    public void OnQuitPressed()
    {
        SceneManager.LoadScene("ModeSelect");       // or "MainMenu"
    }
}