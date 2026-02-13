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

        // displaying right or wrong answers
        if (TriviaSessionData.wasCorrect)
        {
            bodyText.text = $"You chose: <b>{chosenLetter}. {chosenText}</b>";
        }
        else
        {
            bodyText.text =
                $"You chose: <b>{chosenLetter}. {chosenText}</b>\n" +
                $"Correct answer: <b>{correctLetter}. {correctText}</b>";
        }
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
        // move to the next question index
        TriviaSessionData.currentQuestionIndex++;

        if (TriviaSessionData.currentQuestionIndex >= TriviaSessionData.totalQuestions)
        {
            // ✅ Out of questions -> go to Game Over popup instead of ModeSelect
            SceneManager.LoadScene("GameOver_PopUp");
        }
        else
        {
            // still have questions -> go back to TriviaMode for the next one
            SceneManager.LoadScene("TriviaMode");
        }
    }

    // called by Quit button
    public void OnQuitPressed()
    {
        // still fine to send them back to ModeSelect / MainMenu on manual quit
        SceneManager.LoadScene("ModeSelect");       // or "MainMenu" if you prefer
    }
}
