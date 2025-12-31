using UnityEngine;
using UnityEngine.SceneManagement;

public class CrosswordModeUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string cluesSceneName      = "Clues_Crossword";
    [SerializeField] private string modeSelectSceneName = "ModeSelect";      // whatever yours is
    [SerializeField] private string settingsSceneName   = "Settings";   // adjust
    [SerializeField] private string quitPopupSceneName  = "Quit_PopUp";      // <- your popup scene

    [Header("Board Ref (for hint etc.)")]
    [SerializeField] private CrosswordBoardManager boardManager;

    // CLUES
    public void OnCluesButton()
    {
        SceneManager.LoadScene(cluesSceneName);
    }

    // HINT
    public void OnHintButton()
    {
        if (boardManager != null)
            boardManager.RequestHint();
        else
            Debug.LogWarning("CrosswordModeUI: No boardManager wired for hints.");
    }

    // BACK
    public void OnBackButton()
    {
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // SETTINGS
    public void OnSettingsButton()
    {
        SceneManager.LoadScene(settingsSceneName);
    }

    // QUIT -> load the Quit_PopUp scene
    public void OnQuitButton()
    {
        SceneManager.LoadScene(quitPopupSceneName);
    }
}