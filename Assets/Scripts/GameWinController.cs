using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameWinController
{
    // What just finished
    public static string lastGameMode;
    public static string lastDifficulty;
    public static string lastWord;

    // Call this from any game mode when the win condition is met
    public static void TriggerWin(string gameMode, string word = null)
    {
        lastGameMode   = gameMode;
        lastDifficulty = TriviaSessionData.selectedDifficulty;
        lastWord       = word;

        // Register mode-specific stats
        if (gameMode == "Wordoku")
            PlayerDatabaseAPI.RegisterWordokuStats(
                TriviaSessionData.wordokuWrongPlacements,
                TriviaSessionData.wordokuTimeSeconds);

        if (gameMode == "Crossword")
            PlayerDatabaseAPI.RegisterCrosswordScarletLetters(
                TriviaSessionData.crosswordWrongPlacements);

        // Show interstitial if ready, then load game over scene
        if (TriviaForgeAdManager.Instance != null && TriviaForgeAdManager.Instance.IsInterstitialReady)
        {
            TriviaForgeAdManager.Instance.ShowInterstitial(onClosed: () =>
            {
                SceneManager.LoadScene("GameOver_PopUp");
            });
        }
        else
        {
            SceneManager.LoadScene("GameOver_PopUp");
        }
    }
}