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

    [Header("Optional: Create Account Panel")]
    [SerializeField] private GameObject createAccountPanel;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName      = "MainMenu";
    [SerializeField] private string modeSelectSceneName    = "ModeSelect";
    [SerializeField] private string createAccountSceneName = "CreateAccount_PopUp";

    private const string PrefKey_User = "TF_USER_";
    private const string PrefKey_Pass = "TF_PASS_";

    private void Awake()
    {
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

        SetSessionLoggedIn(user, isGuest: false);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // CANCEL button — go back to main menu
    public void OnCancelPressed()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // "Create New Account" button
    public void OnCreateAccountPressed()
    {
        SceneManager.LoadScene(createAccountSceneName);
    }

    // "Play as Guest" button
    public void OnGuestPressed()
    {
        SetSessionLoggedIn("Guest", isGuest: true);
        SceneManager.LoadScene(modeSelectSceneName);
    }

    // Public helper for CreateAccount script
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
        PlayerPrefs.SetString("TF_CurrentUser", username);
        PlayerPrefs.SetInt("TF_IsGuest", isGuest ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[LoginPopupUI] Logged in as '{username}' (Guest={isGuest})");
    }
}