using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginPopupUI : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("UI")]
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private Toggle     rememberMeToggle;

    [Header("Forgot Password Panel")]
    [SerializeField] private GameObject     forgotPasswordPanel;
    [SerializeField] private TMP_InputField forgotUsernameInput;
    [SerializeField] private TMP_Text       forgotStatusText;
    [SerializeField] private GameObject     loginFrame;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName      = "MainMenu";
    [SerializeField] private string modeSelectSceneName    = "ModeSelect";
    [SerializeField] private string createAccountSceneName = "CreateAccount_PopUp";

    private const string RememberMeKey = "TF_RememberMe";

    private async void Start()
    {
        SetStatus("");

        // Restore remember me toggle state
        if (rememberMeToggle != null)
            rememberMeToggle.isOn = PlayerPrefs.GetInt(RememberMeKey, 0) == 1;

        // Tab/Enter navigation
        if (usernameInput != null)
            usernameInput.onSubmit.AddListener(_ => passwordInput?.ActivateInputField());

        if (passwordInput != null)
            passwordInput.onSubmit.AddListener(_ => OnConfirmPressed());

        // Try auto-login if remember me was checked
        if (PlayerPrefs.GetInt(RememberMeKey, 0) == 1)
        {
            SetLoading(true);
            SetStatus("Signing in...");

            bool autoLoggedIn = await PlayerDatabaseAPI.TryAutoLoginAsync();

            SetLoading(false);

            if (autoLoggedIn)
            {
                SetSession(PlayerDatabaseAPI.CurrentUsername, isGuest: false);
                SceneManager.LoadScene(modeSelectSceneName);
                return;
            }

            SetStatus("");
        }
    }

    // ── Main login ────────────────────────────────────────────────────────

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

        // Save remember me preference
        bool rememberMe = rememberMeToggle != null && rememberMeToggle.isOn;
        PlayerPrefs.SetInt(RememberMeKey, rememberMe ? 1 : 0);
        PlayerPrefs.Save();

        SetSession(user, isGuest: false);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // ── Forgot password ───────────────────────────────────────────────────

    public void OnForgotPasswordPressed()
    {
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(true);
        if (loginFrame          != null) loginFrame.SetActive(false);
    }

    public void OnForgotPasswordCancelPressed()
    {
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (loginFrame          != null) loginFrame.SetActive(true);

        if (forgotStatusText != null) forgotStatusText.text = "";
    }

    public async void OnForgotPasswordSubmitPressed()
    {
        string username = forgotUsernameInput != null ? forgotUsernameInput.text.Trim() : "";

        if (string.IsNullOrWhiteSpace(username))
        {
            if (forgotStatusText != null) forgotStatusText.text = "Please enter your username.";
            return;
        }

        if (forgotStatusText != null) forgotStatusText.text = "Sending reset email...";

        var (success, error) = await PlayerDatabaseAPI.SendPasswordResetAsync(username);

        if (forgotStatusText != null)
            forgotStatusText.text = success
                ? "Reset email sent! Check your inbox."
                : error;
    }

    // ── Other buttons ─────────────────────────────────────────────────────

    public void OnCancelPressed() =>
        SceneManager.LoadScene(mainMenuSceneName);

    public void OnCreateAccountPressed() =>
        SceneManager.LoadScene(createAccountSceneName);

    public void OnGuestPressed()
    {
        PlayerPrefs.SetInt(RememberMeKey, 0);
        PlayerPrefs.Save();
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