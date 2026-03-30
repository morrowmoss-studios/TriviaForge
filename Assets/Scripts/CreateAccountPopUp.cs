using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateAccountPopUp : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField confirmUsernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("UI")]
    [SerializeField] private TMP_Text  statusText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Names")]
    [SerializeField] private string loginSceneName = "Login_PopUp";

    private void Awake() => SetStatus("");

    // ── Buttons ───────────────────────────────────────────────────────────

    public async void OnConfirmPressed()
    {
        string username        = usernameInput        != null ? usernameInput.text.Trim()        : "";
        string confirmUsername = confirmUsernameInput != null ? confirmUsernameInput.text.Trim() : "";
        string password        = passwordInput        != null ? passwordInput.text               : "";
        string confirmPassword = confirmPasswordInput != null ? confirmPasswordInput.text        : "";

        // ── Validation ───────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(username))
            { SetStatus("Username is required."); return; }

        if (username.Length < 3)
            { SetStatus("Username must be at least 3 characters."); return; }

        if (username != confirmUsername)
            { SetStatus("Usernames do not match."); return; }

        if (string.IsNullOrWhiteSpace(password))
            { SetStatus("Password is required."); return; }

        if (password.Length < 6)
            { SetStatus("Password must be at least 6 characters."); return; }

        if (password != confirmPassword)
            { SetStatus("Passwords do not match."); return; }

        // ── Create ───────────────────────────────────────────────────────

        SetLoading(true);
        SetStatus("Creating account...");

        var (success, error) = await PlayerDatabaseAPI.CreateAccountAsync(username, password);

        SetLoading(false);

        if (!success) { SetStatus(error); return; }

        SetStatus("Account created! Taking you to login...");
        await Task.Delay(1200);
        SceneManager.LoadScene(loginSceneName);
    }

    public void OnCancelPressed() =>
        SceneManager.LoadScene(loginSceneName);

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private void SetLoading(bool on)
    {
        if (loadingIndicator        != null) loadingIndicator.SetActive(on);
        if (usernameInput           != null) usernameInput.interactable           = !on;
        if (confirmUsernameInput    != null) confirmUsernameInput.interactable    = !on;
        if (passwordInput           != null) passwordInput.interactable           = !on;
        if (confirmPasswordInput    != null) confirmPasswordInput.interactable    = !on;
    }
}