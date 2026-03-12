using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateAccountPopUp : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField confirmUsernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("Status Text (optional)")]
    [SerializeField] private TMP_Text statusText;

    [Header("Scene Names")]
    [SerializeField] private string loginSceneName = "Login_PopUp";

    private const string PrefKey_User = "TF_USER_";
    private const string PrefKey_Pass = "TF_PASS_";

    private void Awake()
    {
        SetStatus("");
    }

    // CONFIRM button
    public void OnConfirmPressed()
    {
        string username        = usernameInput        != null ? usernameInput.text.Trim()        : "";
        string confirmUsername = confirmUsernameInput != null ? confirmUsernameInput.text.Trim() : "";
        string password        = passwordInput        != null ? passwordInput.text               : "";
        string confirmPassword = confirmPasswordInput != null ? confirmPasswordInput.text        : "";

        // ── Validation ───────────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Username is required.");
            return;
        }

        if (username != confirmUsername)
        {
            SetStatus("Usernames do not match.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Password is required.");
            return;
        }

        if (password != confirmPassword)
        {
            SetStatus("Passwords do not match.");
            return;
        }

        if (password.Length < 6)
        {
            SetStatus("Password must be at least 6 characters.");
            return;
        }

        if (PlayerPrefs.HasKey(PrefKey_User + username))
        {
            SetStatus("That username is already taken.");
            return;
        }

        // ── Create account ───────────────────────────────────────────────────

        // Save credentials locally (temp auth, same as LoginPopupUI)
        PlayerPrefs.SetString(PrefKey_User + username, username);
        PlayerPrefs.SetString(PrefKey_Pass + username, password);
        PlayerPrefs.Save();

        // Create player profile in the persistent player database
        PlayerDatabaseAPI.Load();
        PlayerDatabaseAPI.GetOrCreatePlayer(username);

        Debug.Log($"[CreateAccountPopUp] Account created for '{username}'.");

        // Head to login so they can sign in with their new credentials
        SceneManager.LoadScene(loginSceneName);
    }

    // CANCEL button
    public void OnCancelPressed()
    {
        SceneManager.LoadScene(loginSceneName);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}