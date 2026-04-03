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
        if (AudioManager.Instance != null) AudioManager.Instance.OnMenuScene();

        int chosen  = TriviaSessionData.chosenIndex;
        int correct = TriviaSessionData.correctIndex;

        bool timedOut = chosen == -1;

        string correctLetter = IndexToLetter(correct);
        string correctText   = TriviaSessionData.answers[correct];

        if (TriviaSessionData.wasCorrect)
        {
            titleText.text = "Correct!";
            string chosenLetter = IndexToLetter(chosen);
            string chosenText   = TriviaSessionData.answers[chosen];
            bodyText.text = $"You chose: <b>{chosenLetter}. {chosenText}</b>";
        }
        else if (timedOut)
        {
            titleText.text = "Out of Time!";
            bodyText.text  = $"Correct answer: <b>{correctLetter}. {correctText}</b>";
        }
        else
        {
            titleText.text = "Wrong!";
            string chosenLetter = IndexToLetter(chosen);
            string chosenText   = TriviaSessionData.answers[chosen];
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
        if (TriviaSessionData.roundOver)
        {
            SceneManager.LoadScene("GameOver_PopUp");
            return;
        }

        SceneManager.LoadScene("TriviaMode");
    }

    // called by Quit button
    public void OnQuitPressed()
    {
        SceneManager.LoadScene("ModeSelect");
    }
}