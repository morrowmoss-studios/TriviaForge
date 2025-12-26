using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameWinController
{
    // What just finished
    public static string lastGameMode;
    public static string lastDifficulty;
    public static string lastWord;   // for Wordoku / Crossword titles etc.

    // Call this from any game mode when the win condition is met
    public static void TriggerWin(string gameMode, string word = null)
    {
        lastGameMode   = gameMode;
        lastDifficulty = TriviaSessionData.selectedDifficulty; // we already store this
        lastWord       = word;

        SceneManager.LoadScene("Win_PopUp");   // change name if your scene is different
    }
}