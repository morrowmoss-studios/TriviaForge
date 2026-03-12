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
        titleText.text = TriviaSessionData.wasCorrect ? "Correct!" : "Wrong!";

        int chosen  = TriviaSessionData.chosenIndex;
        int correct = TriviaSessionData.correctIndex;

        string chosenLetter  = IndexToLetter(chosen);
        string correctLetter = IndexToLetter(correct);

        string chosenText  = TriviaSessionData.answers[chosen];
        string correctText = TriviaSessionData.answers[correct];

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
        // Strikes or out of questions -> game over
        if (TriviaSessionData.roundOver)
        {
            SceneManager.LoadScene("GameOver_PopUp");
            return;
        }

        // TriviaQuestionManager already advanced currentQuestionIndex when
        // the answer was clicked — don't increment again here
        SceneManager.LoadScene("TriviaMode");
    }

    // called by Quit button
    public void OnQuitPressed()
    {
        SceneManager.LoadScene("ModeSelect");
    }
}