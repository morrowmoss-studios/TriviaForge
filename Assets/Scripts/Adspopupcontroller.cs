using UnityEngine;

public class AdsPopUpController : MonoBehaviour
{
    public enum GameMode { Trivia, Crossword, Wordoku }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject hintAdSprite;

    private GameMode _mode;
    private System.Action _onHintGranted;

    public void Show(GameMode mode, System.Action onHintGranted)
    {
        _mode          = mode;
        _onHintGranted = onHintGranted;
        panel.SetActive(true);

        if (hintAdSprite != null)
            hintAdSprite.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

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

                if (hintAdSprite != null)
                    hintAdSprite.SetActive(false);
            },
            onClosed: null
        );
    }

    public void OnCancelPressed()
    {
        Hide();
    }
}