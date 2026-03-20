using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Optional")]
    [Tooltip("If true, this UIManager will persist across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    // Static field to track the last visited scene
    public static string previousSceneName;

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        string scene = SceneManager.GetActiveScene().name;

        switch (scene)
        {
            case "GameOver_PopUp":
            case "Scores_PopUp":
            case "Leaderboard_PopUp":
            case "TriviaResult":
            case "ModeSelect":
            case "MainMenu":
            case "AboutGame":
            case "CreateAccount_PopUp":
            case "Credits":
            case "HowToPlay":
            case "PrivacyPolicy":
            case "Quit_PopUp":
            case "Win_PopUp":
            case "TermsOfUse":
            case "Login_PopUp":    
                AudioManager.Instance.OnMenuScene();
                break;

            case "TriviaMode":
            case "CrosswordMode":
            case "WordokuMode":
                AudioManager.Instance.OnGameScene();
                break;

            // All other scenes (Settings, Credits, etc.) leave music as-is
        }
    }

    // -------------------------------------------------------------
    //  Scene Controls
    // -------------------------------------------------------------

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("UIManager.LoadScene called with empty sceneName.");
            return;
        }

        SetPreviousScene();
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public static void SetPreviousScene()
    {
        previousSceneName = SceneManager.GetActiveScene().name;
    }

    public void LoadPreviousScene()
    {
        if (!string.IsNullOrEmpty(previousSceneName))
        {
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            Debug.LogWarning("No previous scene stored. Loading MainMenu as fallback.");
            SceneManager.LoadScene("MainMenu");
        }
    }

    // -------------------------------------------------------------
    //  App Controls
    // -------------------------------------------------------------

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettingsScene()
    {
        SetPreviousScene();
        SceneManager.LoadScene("Settings");
    }

    public void OpenURL(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("UIManager.OpenURL called with empty url.");
            return;
        }

        Application.OpenURL(url);
    }
}