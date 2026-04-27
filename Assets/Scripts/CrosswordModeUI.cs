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

    [Header("Keyboard")]
    [SerializeField] private GameObject keyboardButton;

    [Header("Reset Confirm Panel")]
    [SerializeField] private GameObject resetConfirmPanel;

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

    // KEYBOARD TOGGLE
    public void OnKeyboardButton()
    {
        if (boardManager != null)
            boardManager.ToggleKeyboard();
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

    // RESET
    public void OnResetButton()
    {
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(true);
        else if (boardManager != null)
            boardManager.ResetPuzzle();
    }

    public void OnResetConfirmPressed()
    {
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
        if (boardManager != null)
            boardManager.ResetPuzzle();
    }

    public void OnResetCancelPressed()
    {
        if (resetConfirmPanel != null)
            resetConfirmPanel.SetActive(false);
    }

    // BACK
    public void OnBackButton()
    {
        CrosswordSession.currentWords = null;
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // SETTINGS
    public void OnSettingsPressed()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene("Settings");
    }

    // QUIT
    public void OnQuitButton()
    {
        UIManager.SetPreviousScene();
        SceneManager.LoadScene(quitPopupSceneName);
    }
}