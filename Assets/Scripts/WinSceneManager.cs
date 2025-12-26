using UnityEngine;
using UnityEngine.SceneManagement;

public class WinSceneManager : MonoBehaviour
{
    public void OnNextPressed()
    {
        // Reload the last game mode scene with same settings
        switch (TriviaSessionData.selectedGameMode)
        {
            case "Trivia":
                SceneManager.LoadScene("TriviaMode");
                break;
            case "Wordoku":
                SceneManager.LoadScene("WordokuMode");
                break;
            case "Crossword":
                SceneManager.LoadScene("CrosswordMode");
                break;
            default:
                // Fallback to mode select if something went weird
                SceneManager.LoadScene("ModeSelect");
                break;
        }
    }

    public void OnQuitPressed()
    {
        // Back to main menu, not hard-quit the app
        SceneManager.LoadScene("Quit_PopUp");
    }
}