using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameWinController
{
    public static string lastGameMode;
    public static string lastDifficulty;
    public static string lastWord;

    private static float _lastInterstitialTime = -300f;

    public static void TriggerWin(string gameMode, string word = null)
    {
        lastGameMode   = gameMode;
        lastDifficulty = TriviaSessionData.selectedDifficulty;
        lastWord       = word;

        if (gameMode == "Wordoku")
            PlayerDatabaseAPI.RegisterWordokuStats(
                TriviaSessionData.wordokuWrongPlacements,
                TriviaSessionData.wordokuTimeSeconds);

        if (gameMode == "Crossword")
            PlayerDatabaseAPI.RegisterCrosswordScarletLetters(
                TriviaSessionData.crosswordWrongPlacements);

        bool timeGatePassed = (Time.realtimeSinceStartup - _lastInterstitialTime) >= 300f;

        if (timeGatePassed && TriviaForgeAdManager.Instance != null && TriviaForgeAdManager.Instance.IsInterstitialReady)
        {
            _lastInterstitialTime = Time.realtimeSinceStartup;
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