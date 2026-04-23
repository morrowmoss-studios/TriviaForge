using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CrosswordModeUI : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string cluesSceneName      = "Clues_Crossword";
    [SerializeField] private string modeSelectSceneName = "ModeSelect";
    [SerializeField] private string settingsSceneName   = "Settings";
    [SerializeField] private string quitPopupSceneName  = "Quit_PopUp";

    [Header("Board Ref (for hint, reset, etc.)")]
    [SerializeField] private CrosswordBoardManager boardManager;

    [Header("Hint UI")]
    [SerializeField] private TMP_Text hintCountText;

    private void Start()
    {
        if (boardManager != null)
        {
            boardManager.OnHintsChanged += UpdateHintDisplay;
            UpdateHintDisplay(3);
        }
    }

    private void OnDestroy()
    {
        if (boardManager != null)
            boardManager.OnHintsChanged -= UpdateHintDisplay;
    }

    private void UpdateHintDisplay(int hintsRemaining)
    {
        if (hintCountText != null)
            hintCountText.text = hintsRemaining.ToString();
    }

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

    // RESET — clears all player-entered letters
    public void OnResetButton()
    {
        if (boardManager != null)
            boardManager.ResetPuzzle();
        else
            Debug.LogWarning("CrosswordModeUI: No boardManager wired for reset.");
    }

    // BACK
    public void OnBackButton()
    {
        // Clear session so a fresh puzzle is generated next time
        CrosswordSession.currentWords = null;
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // SETTINGS
    public void OnSettingsPressed()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene("Settings");
    }

    // QUIT -> load the Quit_PopUp scene
    public void OnQuitButton()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene(quitPopupSceneName);
    }
}