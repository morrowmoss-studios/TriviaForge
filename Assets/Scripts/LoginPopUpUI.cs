using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginPopupUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("UI")]
    [SerializeField] private TMP_Text  statusText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName      = "MainMenu";
    [SerializeField] private string modeSelectSceneName    = "ModeSelect";
    [SerializeField] private string createAccountSceneName = "CreateAccount_PopUp";

    private void Awake() => SetStatus("");

    // ── Buttons ───────────────────────────────────────────────────────────

    public async void OnConfirmPressed()
    {
        string user = usernameInput != null ? usernameInput.text.Trim() : "";
        string pass = passwordInput != null ? passwordInput.text        : "";

        if (string.IsNullOrWhiteSpace(user)) { SetStatus("Username required."); return; }
        if (string.IsNullOrWhiteSpace(pass)) { SetStatus("Password required."); return; }

        SetLoading(true);
        SetStatus("Signing in...");

        var (success, error) = await PlayerDatabaseAPI.LoginAsync(user, pass);

        SetLoading(false);

        if (!success) { SetStatus(error); return; }

        SetSession(user, isGuest: false);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    public void OnCancelPressed() =>
        SceneManager.LoadScene(mainMenuSceneName);

    public void OnCreateAccountPressed() =>
        SceneManager.LoadScene(createAccountSceneName);

    public void OnGuestPressed()
    {
        SetSession("Guest", isGuest: true);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void SetLoading(bool on)
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(on);
        if (usernameInput    != null) usernameInput.interactable = !on;
        if (passwordInput    != null) passwordInput.interactable = !on;
    }

    private static void SetSession(string username, bool isGuest)
    {
        PlayerPrefs.SetString("TF_CurrentUser", username);
        PlayerPrefs.SetInt   ("TF_IsGuest",     isGuest ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[LoginPopupUI] Session set: {username} guest={isGuest}");
    }
}