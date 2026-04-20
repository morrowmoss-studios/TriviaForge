using UnityEngine;

/// <summary>
/// Attach to the Ads_PopUp_Panel in each game mode scene.
/// Wire Confirm_Button OnClick to OnConfirmPressed()
/// Wire Cancel_Button OnClick to OnCancelPressed()
/// Set the mode via SetMode() before showing.
/// </summary>
public class AdsPopUpController : MonoBehaviour
{
    public enum GameMode { Trivia, Crossword, Wordoku }

    [Header("References")]
    [SerializeField] private GameObject panel;

    private GameMode _mode;
    private System.Action _onHintGranted;

    // ── Show / Hide ───────────────────────────────────────────────────────

    public void Show(GameMode mode, System.Action onHintGranted)
    {
        _mode          = mode;
        _onHintGranted = onHintGranted;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    // ── Button handlers ───────────────────────────────────────────────────

    public void OnConfirmPressed()
    {
        Hide();

        if (TriviaForgeAdManager.Instance == null)
        {
            Debug.LogWarning("[AdsPopUp] No TriviaForgeAdManager found.");
            return;
        }

        TriviaForgeAdManager.Instance.ShowRewarded(
            onRewarded: () =>
            {
                Debug.Log("[AdsPopUp] Reward earned — granting hint.");
                _onHintGranted?.Invoke();
                _onHintGranted = null;
            },
            onClosed: null
        );
    }

    public void OnCancelPressed()
    {
        Hide();
    }
}