using TMPro;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateAccountPopUp : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField confirmUsernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("UI")]
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Names")]
    [SerializeField] private string loginSceneName = "Login_PopUp";

    private void Awake()
    {
        SetStatus("");

        // Tab/Enter advances to next field
        if (usernameInput        != null) usernameInput.onSubmit.AddListener(_        => confirmUsernameInput?.ActivateInputField());
        if (confirmUsernameInput != null) confirmUsernameInput.onSubmit.AddListener(_ => emailInput?.ActivateInputField());
        if (emailInput           != null) emailInput.onSubmit.AddListener(_           => passwordInput?.ActivateInputField());
        if (passwordInput        != null) passwordInput.onSubmit.AddListener(_        => confirmPasswordInput?.ActivateInputField());
        if (confirmPasswordInput != null) confirmPasswordInput.onSubmit.AddListener(_ => OnConfirmPressed());
    }

    // ── Buttons ───────────────────────────────────────────────────────────

    public async void OnConfirmPressed()
    {
        string username        = usernameInput        != null ? usernameInput.text.Trim()        : "";
        string confirmUsername = confirmUsernameInput != null ? confirmUsernameInput.text.Trim() : "";
        string email           = emailInput           != null ? emailInput.text.Trim()           : "";
        string password        = passwordInput        != null ? passwordInput.text               : "";
        string confirmPassword = confirmPasswordInput != null ? confirmPasswordInput.text        : "";

        // ── Validation ───────────────────────────────────────────────────

        if (string.IsNullOrWhiteSpace(username))
            { SetStatus("Username is required."); return; }

        if (username.Length < 3)
            { SetStatus("Username must be at least 3 characters."); return; }

        if (username != confirmUsername)
            { SetStatus("Usernames do not match."); return; }

        if (string.IsNullOrWhiteSpace(email))
            { SetStatus("Email address is required."); return; }

        if (!email.Contains("@") || !email.Contains("."))
            { SetStatus("Please enter a valid email address."); return; }

        if (string.IsNullOrWhiteSpace(password))
            { SetStatus("Password is required."); return; }

        if (password.Length < 6)
            { SetStatus("Password must be at least 6 characters."); return; }

        if (password != confirmPassword)
            { SetStatus("Passwords do not match."); return; }

        // ── Create ───────────────────────────────────────────────────────

        SetLoading(true);
        SetStatus("Creating account...");

        var (success, error) = await PlayerDatabaseAPI.CreateAccountAsync(username, email, password);

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
        if (emailInput              != null) emailInput.interactable              = !on;
        if (passwordInput           != null) passwordInput.interactable           = !on;
        if (confirmPasswordInput    != null) confirmPasswordInput.interactable    = !on;
    }
}