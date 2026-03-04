using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Optional: Feedback Text")]
    [SerializeField] private TMP_Text statusText;

    [Header("Popup Root (what to hide on cancel/success)")]
    [SerializeField] private GameObject popupRoot;

    [Header("Optional: Create Account Panel")]
    [SerializeField] private GameObject createAccountPanel;

    [SerializeField] private string createAccountSceneName = "CreateAccount_PopUp";

    [Header("Navigation")]
    [Tooltip("Scene to go to after login/guest. Leave blank to just close popup.")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Tooltip("If true, Cancel will load nextSceneName (or MainMenu). If false, it just hides popupRoot.")]
    [SerializeField] private bool cancelLoadsScene = false;

    // ---------------------------------------
    // TEMP LOCAL AUTH (for testing only)
    // ---------------------------------------
    private const string PrefKey_User = "TF_USER_";
    private const string PrefKey_Pass = "TF_PASS_";

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        SetStatus("");
    }

    // CONFIRM button
    public void OnConfirmPressed()
    {
        string user = usernameInput != null ? usernameInput.text.Trim() : "";
        string pass = passwordInput != null ? passwordInput.text : "";

        if (string.IsNullOrWhiteSpace(user))
        {
            SetStatus("Username required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            SetStatus("Password required.");
            return;
        }

        // TEMP: local login using PlayerPrefs
        if (!HasUser(user))
        {
            SetStatus("Account not found. Use Create New Account.");
            return;
        }

        if (!PasswordMatches(user, pass))
        {
            SetStatus("Incorrect password.");
            return;
        }

        // Success
        SetStatus("Login successful!");
        SetSessionLoggedIn(user, isGuest: false);

        GoNextOrClose();
    }

    // CANCEL button
    public void OnCancelPressed()
    {
        SetStatus("");

        if (cancelLoadsScene)
        {
            LoadSceneSafe(string.IsNullOrWhiteSpace(nextSceneName) ? "MainMenu" : nextSceneName);
            return;
        }

        // Hide the popup (best for your "Login_PopUp" overlay approach)
        if (popupRoot != null) popupRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    // "Create New Account" text/button
    public void OnCreateAccountPressed()
    {
        SetStatus("");
        LoadSceneSafe(createAccountSceneName);
    }

    // "Play as Guest" text/button
    public void OnGuestPressed()
    {
        SetStatus("Playing as Guest...");
        SetSessionLoggedIn("Guest", isGuest: true);
        GoNextOrClose();
    }

    // ---------------------------------------
    // PUBLIC helper for a Create Account script
    // ---------------------------------------
    public void CreateLocalAccount(string username, string password)
    {
        username = username.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Username + password required.");
            return;
        }

        if (HasUser(username))
        {
            SetStatus("That username already exists.");
            return;
        }

        PlayerPrefs.SetString(PrefKey_User + username, username);
        PlayerPrefs.SetString(PrefKey_Pass + username, password);
        PlayerPrefs.Save();

        SetStatus("Account created! Now log in.");
    }

    // ---------------------------------------
    // Internal helpers
    // ---------------------------------------
    private void GoNextOrClose()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            LoadSceneSafe(nextSceneName);
            return;
        }

        // No scene specified, just close popup
        if (popupRoot != null) popupRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[LoginPopupUI] No scene name provided.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    private static bool HasUser(string username)
    {
        return PlayerPrefs.HasKey(PrefKey_User + username);
    }

    private static bool PasswordMatches(string username, string password)
    {
        string stored = PlayerPrefs.GetString(PrefKey_Pass + username, "");
        return stored == password;
    }

    private static void SetSessionLoggedIn(string username, bool isGuest)
    {
        // If you already have PlayerDatabase / TriviaSessionData, swap these to your real system.
        PlayerPrefs.SetString("TF_CurrentUser", username);
        PlayerPrefs.SetInt("TF_IsGuest", isGuest ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[LoginPopupUI] Logged in as '{username}' (Guest={isGuest})");
    }
}